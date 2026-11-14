using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class Game231FriendlyTriggerDamageChecks
{
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue root = document.RootElement;
        bool eaten = root.GetProperty("ModifierScenario").GetString() == "original-eaten-damage";
        string primaryId = eaten ? RelicFriendlyTriggerDamageModel.DataId : RelicFriendlyTriggerDamageModel.BurnoutDataId;
        Require(root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop", "Wrong native ruleset/terminal.");
        if (eaten) RelicEatenDamageChecks.Native(root); else RelicBurnoutDamageChecks.Native(root);
        FixtureValue rules = root.GetProperty("FriendlyTriggerDamageRules");
        string?[] piercingTypes = rules.GetProperty("PiercingEffectTypes").EnumerateArray().Select(item => item.GetString()).ToArray();
        Require(piercingTypes.Contains(RelicFriendlyTriggerDamageModel.PiercingEffect) &&
            !piercingTypes.Contains(RelicFriendlyTriggerDamageModel.Effect), "Native balance piercing classes differ.");
        FixtureValue[] nativeOwners = rules.GetProperty("Owners").EnumerateArray().ToArray();
        Require(nativeOwners.Length == 2 && nativeOwners.Select(row => row.GetProperty("DataId").GetString()).ToHashSet()
            .SetEquals(new[] { RelicFriendlyTriggerDamageModel.DataId, RelicFriendlyTriggerDamageModel.BurnoutDataId }),
            "Both unchanged owners must coexist in the native battle.");
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        FixtureValue[] turns = root.GetProperty("Turns").EnumerateArray().ToArray();
        FixtureValue scenario = root.GetProperty(eaten ? "OriginalEatenDamage" : "OriginalBurnoutDamage");
        Require(scenario.GetProperty("InitialEnemyArmor").EnumerateArray().Select(item => item.GetInt32())
            .SequenceEqual(eaten ? new[] { 7, 7 } : new[] { 3, 0, 0 }), "Missing authored armor controls.");
        if (!eaten) Require(scenario.GetProperty("DeploymentRooms").EnumerateArray().Select(item => item.GetInt32()).SequenceEqual(new[] { 2, 2 }) &&
            actions.Take(2).All(row => row.GetProperty("Action").GetProperty("RoomIndex").GetInt32() == 2),
            "Missing the concentrated guards for the versioned control battle.");
        TrainCombatState train = actions[0].GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train;
        Require(train.Context!.Statistics?.MonsterDamage != null && RelicModel.Validate(train.Context.Relics!) == null,
            "Missing versioned statistics or original owner definitions.");
        foreach (FixtureValue row in nativeOwners)
        {
            string id = row.GetProperty("DataId").GetString()!;
            CombatRelicState owner = train.Context.Relics!.Single(item => item.DataId == id);
            string[] expected = id == RelicFriendlyTriggerDamageModel.DataId
                ? [RelicFriendlyTriggerDamageModel.PiercingEffect] : [RelicFriendlyTriggerDamageModel.Effect, RelicFriendlyTriggerDamageModel.Effect];
            Require(owner.EffectTypes.SequenceEqual(expected) && row.GetProperty("RuntimeEffectTypes").EnumerateArray()
                .Select(item => item.GetString()).SequenceEqual(expected) &&
                row.GetProperty("NativePiercing").GetBoolean() == (id == RelicFriendlyTriggerDamageModel.DataId) &&
                RelicFriendlyTriggerDamageModel.IsPiercing(train.Context, owner) == row.GetProperty("NativePiercing").GetBoolean(),
                "Source-owner piercing differs from the native DamageHelper query.");
        }
        string testsKey = eaten ? "RelicEatenDamageTests" : "RelicBurnoutDamageTests";
        FixtureValue[] tests = root.GetProperty(testsKey).EnumerateArray().ToArray();
        Require(tests.Select(row => row.GetProperty("OwnerDataId").GetString()).ToHashSet()
            .SetEquals(nativeOwners.Select(row => row.GetProperty("DataId").GetString())), "Missing tests for a coexisting owner.");
        bool versionGuardChecked = false;
        foreach (FixtureValue row in tests)
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            int ownerIndex = before.Context!.Relics!.Select((owner, index) => (owner, index))
                .Single(item => item.owner.DataId == row.GetProperty("OwnerDataId").GetString()).index;
            if (!versionGuardChecked && row.GetProperty("OwnerDataId").GetString() == RelicFriendlyTriggerDamageModel.DataId)
            {
                Require(!RelicFriendlyTriggerDamageModel.Select(CardSpellModel.WithContext(before, before.Context.WithStatistics(null)),
                    row.GetProperty("Actor").Deserialize<CombatUnit>()!, row.GetProperty("Trigger").GetString()!,
                    row.GetProperty("RoomIndex").GetInt32(), ownerIndex, row.GetProperty("EffectIndex").GetInt32()).Supported,
                    "Unversioned piercing subclass was accepted.");
                versionGuardChecked = true;
            }
            RelicCardPlayDamageSelection selected = RelicFriendlyTriggerDamageModel.Select(before,
                row.GetProperty("Actor").Deserialize<CombatUnit>()!, row.GetProperty("Trigger").GetString()!,
                row.GetProperty("RoomIndex").GetInt32(), ownerIndex, row.GetProperty("EffectIndex").GetInt32(), row.GetProperty("ParamInt").GetInt32());
            Require(selected.Supported && row.GetProperty("Result").GetBoolean() == (selected.Targets.Count > 0),
                "Coexisting owner eligibility differs: " + selected.UnsupportedReason);
            Equal(selected.State, row.GetProperty("Actual").Deserialize<TrainCombatState>()!, "coexisting owner Test");
            if (selected.Targets.Count > 0) Require(selected.Targets.SequenceEqual(row.GetProperty("CachedTargets").EnumerateArray()
                .Select(item => item.GetInt32())), "Coexisting owner cached targets differ.");
        }
        Require(versionGuardChecked, "Missing version guard control.");
        FixtureValue[] applications = root.GetProperty(eaten ? "RelicEatenDamageApplications" : "RelicBurnoutDamageApplications").EnumerateArray().ToArray();
        Require(applications.Length == (eaten ? 5 : 2) && applications.All(row => row.GetProperty("OwnerDataId").GetString() == primaryId),
            "Unexpected damage application from the other owner.");
        foreach (FixtureValue row in applications)
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            Require(!RelicFriendlyTriggerDamageModel.ApplySelected(before, row.GetProperty("Targets").EnumerateArray()
                .Select(item => item.GetInt32()).ToArray(), true).Supported, "Ambiguous damage source was accepted.");
            Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Ambiguous owner guard mutated its parent.");
            if (!eaten) continue;
            int target = row.GetProperty("Targets").EnumerateArray().Single().GetInt32();
            CombatUnit victim = before.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == target);
            CombatUnit? after = row.GetProperty("Actual").Deserialize<TrainCombatState>()!.Rooms.SelectMany(room => room.Units)
                .SingleOrDefault(unit => unit.Id == target);
            Require(victim.Status("armor")?.Stacks == 7 && (after == null || after.Status("armor")?.Stacks == 7),
                "The piercing owner consumed armor.");
        }
        if (!eaten)
        {
            int target = root.GetProperty("OriginalBurnoutDamage").GetProperty("EnemyUnitIds").EnumerateArray().First().GetInt32();
            CombatUnit Victim(FixtureValue row, string side) => row.GetProperty(side).Deserialize<TrainCombatState>()!.Rooms
                .SelectMany(room => room.Units).Single(unit => unit.Id == target);
            Require(Victim(applications[0], "Before").Health == 12 && Victim(applications[0], "Before").Status("armor")?.Stacks == 3 &&
                Victim(applications[0], "Actual").Health == 10 && (Victim(applications[0], "Actual").Status("armor")?.Stacks ?? 0) == 0 &&
                Victim(applications[1], "Actual").Health == 5, "Ordinary damage must absorb three armor despite the collected piercing owner.");
        }
        // Native authors control enemies/cards after deployment. Replay starts
        // at the first formal-turn input, then never reloads a native checkpoint.
        int Turn(FixtureValue row) => row.GetProperty("Before").GetProperty("Spawn").GetProperty("Turn").GetInt32();
        Require(actions.Count(row => Turn(row) == 0) == 2 && turns.Count(row => Turn(row) == 0) == 1 &&
            actions.Any(row => Turn(row) == 1), "Unexpected authored setup boundary.");
        RoomCombatChecks.NativeRoomOnly(path);
        TrainCombatChecks.Native(root);
        RecordedBattleChecks.Native(root, firstTurn: 1);
        Console.WriteLine($"GAME-231-FRIENDLY-TRIGGER-DAMAGE PASS: {(eaten ? "piercing Eaten" : "ordinary Burnout/death")}, both original owners coexist, native runtime/balance/source queries, armor witness, complete post-setup battle and immutable parallel branches.");
    }
    private static void Equal<T>(T a, T b, string label)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(a, ModelJson.Options), JsonSerializer.Serialize(b, ModelJson.Options));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool condition, string error) { if (!condition) throw new InvalidOperationException(error); }
}

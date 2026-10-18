using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RelicBurnoutDamageChecks
{
    private static CombatRelicState Owner(string id = RelicFriendlyTriggerDamageModel.BurnoutDataId,
        string target = "Room", string[]? required = null) => new(id, RelicFriendlyTriggerDamageModel.BurnoutAssetKey,
        [RelicFriendlyTriggerDamageModel.Effect, RelicFriendlyTriggerDamageModel.Effect],
        resourceEffects: [new(0, RelicFriendlyTriggerDamageModel.Effect, 5, 0, 5, true,
            unitTrigger: new("OnDeath", "Monsters", null, required ?? ["ephemeral"]), unitDamage: new(target)),
            new(1, RelicFriendlyTriggerDamageModel.Effect, 5, 0, 5, true,
                unitTrigger: new("OnBurnout", "Monsters", null, []), unitDamage: new(target))], disallowedInPlacementPhase: false);

    internal static void Run()
    {
        UnityRng rng = UnityRng.Seed(601);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, 10, relics: [Owner()],
            energyState: new(10, 0, 0, "MonsterTurn", true),
            resources: new(3, 0, 5, 10, 10, "pyre", 0, 80, 0, 0, [], [], []));
        CombatUnit Unit(int id, CombatTeam team, int hp, int burnout = 0, bool pyre = false) =>
            new(id, "carrier", team, 0, hp, 80, false, pyre, false, burnout > 0 ? [new("ephemeral", burnout)] : [], []);
        CombatUnit dead = Unit(4, CombatTeam.Player, 0, 1), alive = Unit(5, CombatTeam.Player, 80, 1);
        var train = new TrainCombatState([new(0, false, [Unit(1, CombatTeam.Enemy, 12), Unit(2, CombatTeam.Enemy, 6),
            Unit(3, CombatTeam.Enemy, 0), alive], [], context), new(1, false, [Unit(6, CombatTeam.Enemy, 30)], [], context)], [], 6, context);
        string parent = JsonSerializer.Serialize(train, ModelJson.Options);
        Parallel.For(0, 16, _ => {
            RelicCardPlayDamageSelection death = RelicFriendlyTriggerDamageModel.Select(train, dead, "OnDeath", 0, 0, 0);
            Require(death.Supported && death.Targets.SequenceEqual([1, 2]) && ReferenceEquals(death.State, train),
                "Death must select all living enemies on the actor's floor only: " + death.UnsupportedReason);
            Require(RelicFriendlyTriggerDamageModel.Select(train, Unit(7, CombatTeam.Player, 80), "OnBurnout", 0, 0, 1)
                .Targets.SequenceEqual([1, 2]), "Burnout at zero stacks must still damage the room.");
        });
        Require(RelicFriendlyTriggerDamageModel.Select(train, alive, "OnDeath", 0, 0, 0).Targets.Count == 0 &&
            RelicFriendlyTriggerDamageModel.Select(train, alive, "OnDeath", 0, 0, 0, 1).Targets.SequenceEqual([1, 2]),
            "Alive OnDeath requires native paramInt=1.");
        foreach (CombatUnit actor in new[] { Unit(8, CombatTeam.Player, 0), Unit(9, CombatTeam.Enemy, 0, 1), Unit(10, CombatTeam.Player, 0, 1, true) })
            Require(RelicFriendlyTriggerDamageModel.Select(train, actor, "OnDeath", 0, 0, 0).Targets.Count == 0,
                "No-status, enemy-team and Pyre deaths must not trigger.");
        Require(RelicFriendlyTriggerDamageModel.Select(train, dead, "OnBurnout", 0, 0, 0).Targets.Count == 0 &&
            RelicFriendlyTriggerDamageModel.Select(train, dead, "OnDeath", 0, 0, 1).Targets.Count == 0,
            "Ordered effects must not respond to each other's trigger.");
        var empty = new TrainCombatState([new(0, false, [alive], [], context)], [], 6, context);
        Require(RelicFriendlyTriggerDamageModel.Select(empty, dead, "OnDeath", 0, 0, 0).Targets.Count == 0,
            "Empty room must reset the native target selection.");
        foreach (CombatRelicState changed in new[] { Owner("other"), Owner(target: "Weakest"), Owner(required: []) })
            Require(RelicModel.Validate([changed]) != null, "Unknown owner and changed room/status payloads must be rejected.");
        Require(JsonSerializer.Serialize(train, ModelJson.Options) == parent, "Room damage selection mutated its parent.");
        Console.WriteLine("BURNOUT-DAMAGE-PURE PASS: death/status/alive-param/team/Pyre gates, zero-stack burnout, ordered room targets, empty/dead/off-floor controls, strict definitions and immutable parallel branches without RNG.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 224 && root.GetProperty("OriginalBurnoutDamageSchema").GetInt32() == 224 &&
            root.GetProperty("ModifierScenario").GetString() == "original-burnout-death-damage" &&
            root.GetProperty("OriginalBurnoutDamagePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending", "OriginalBurnoutDamageCaptureFailures",
                "OriginalBurnoutDamageMismatches", "OriginalBurnoutDamageUnsupported", "OriginalBurnoutDamagePending" }
                .All(name => root.GetProperty(name).GetInt32() == 0), "Native burnout/death fixture failed acceptance.");
        FixtureValue evidence = root.GetProperty("OriginalBurnoutDamage");
        int[] cards = evidence.GetProperty("CardIds").EnumerateArray().Select(item => item.GetInt32()).ToArray();
        int[] enemies = evidence.GetProperty("EnemyUnitIds").EnumerateArray().Select(item => item.GetInt32()).ToArray();
        Require(cards.Length == 7 && enemies.Length == 3 && root.GetProperty("Actions").EnumerateArray()
            .Where(row => row.GetProperty("ScenarioAction").GetBoolean()).Select(row => row.GetProperty("Action")
                .GetProperty("CardInstanceId").GetInt32()).SequenceEqual(cards), "Missing the controlled deployment, deaths or burnout carriers.");
        Require(root.GetProperty("Actions").EnumerateArray().Select((row, index) =>
            row.GetProperty("Before").GetProperty("Spawn").GetProperty("Turn").GetInt32() == (index < 2 ? 0 : 1)).All(match => match),
            "Death witnesses must complete on turn one before natural burnout.");
        int death = 0, burnout = 0, wrongTrigger = 0, wrongTeam = 0, noStatus = 0, zeroStackDeath = 0;
        foreach (FixtureValue row in root.GetProperty("RelicBurnoutDamageTests").EnumerateArray())
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            CombatUnit actor = row.GetProperty("Actor").Deserialize<CombatUnit>()!;
            int index = row.GetProperty("EffectIndex").GetInt32(), param = row.GetProperty("ParamInt").GetInt32();
            string trigger = row.GetProperty("Trigger").GetString()!;
            int room = row.GetProperty("RoomIndex").GetInt32();
            int owner = before.Context!.Relics!.Select((item, i) => (item, i)).Single(item => item.item.DataId == RelicFriendlyTriggerDamageModel.BurnoutDataId).i;
            RelicCardPlayDamageSelection selected = RelicFriendlyTriggerDamageModel.Select(before, actor, trigger, room, owner, index, param);
            Require(selected.Supported && row.GetProperty("Result").GetBoolean() == (selected.Targets.Count > 0), "Native damage Test differs: " + selected.UnsupportedReason);
            Equal(selected.State, actual, "Test state");
            Require(before.Context.BattleRng.Equals(actual.Context!.BattleRng), "Deterministic room selection consumed Battle RNG.");
            if (selected.Targets.Count > 0)
            {
                Require(selected.Targets.SequenceEqual(row.GetProperty("CachedTargets").EnumerateArray().Select(item => item.GetInt32())), "Room target order differs.");
                if (index == 0) { death++; Require(actor.Status("ephemeral")?.Stacks > 0 && (actor.Health <= 0 || param == 1), "Death gate bypassed."); }
                else { burnout++; Require(!(actor.Status("ephemeral")?.Stacks > 0), "Natural burnout must run after stack decay."); }
            }
            else if (trigger != (index == 0 ? "OnDeath" : "OnBurnout")) wrongTrigger++;
            else if (actor.Team != CombatTeam.Player) wrongTeam++;
            else if (index == 0 && !(actor.Status("ephemeral")?.Stacks > 0))
            {
                noStatus++;
                if (actor.AssetKey == "PojuBurnoutCarrier") zeroStackDeath++;
            }
        }
        Require(death == 1 && burnout == 1 && wrongTrigger > 0 && wrongTeam > 0 && noStatus > 0 && zeroStackDeath > 0,
            "Missing positive callbacks, team/trigger/status controls or no double damage after burnout.");
        int applications = 0, hits = 0, kills = 0;
        foreach (FixtureValue row in root.GetProperty("RelicBurnoutDamageApplications").EnumerateArray())
        {
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatState actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            int[] targets = row.GetProperty("Targets").EnumerateArray().Select(item => item.GetInt32()).ToArray();
            Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("EffectIndex").GetInt32() == applications &&
                targets.SequenceEqual(applications == 0 ? enemies : enemies.Take(2)), "Missing exact ordered whole-room applications.");
            TrainCombatResult predicted = RelicFriendlyTriggerDamageModel.ApplySelected(before, targets, deferPhysicalRemoval: true);
            Require(predicted.Supported, "Room damage/death queue unsupported: " + predicted.UnsupportedReason);
            Equal(predicted.State, actual, "damage/death queue");
            foreach (int target in targets)
            {
                CombatUnit victim = before.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == target);
                CombatUnit? after = actual.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.Id == target);
                if (after == null || after.Health <= 0) kills++;
                else { Require(victim.Health - after.Health == 5, "Whole-room damage is not five."); hits++; }
            }
            string parent = JsonSerializer.Serialize(before, ModelJson.Options);
            Parallel.For(0, 16, _ => Equal(RelicFriendlyTriggerDamageModel.ApplySelected(before, targets, true).State, actual, "parallel damage"));
            Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Damage branch mutated its parent.");
            applications++;
        }
        Require(applications == 2 && hits == 3 && kills == 2, "Missing three nonlethal hits and two kills across the death/burnout callbacks.");
        Console.WriteLine("ORIGINAL-BURNOUT-DAMAGE-NATIVE PASS: unchanged ordered owner, Ephemeral death and natural zero-stack burnout, complete room targets, three nonlethal hits/two kills, status/team/trigger controls, no duplicate death damage and immutable parallel replay.");
    }
    private static void Equal<T>(T predicted, T actual, string label)
    {
        string a = JsonSerializer.Serialize(predicted, ModelJson.Options), b = JsonSerializer.Serialize(actual, ModelJson.Options);
        Require(a == b, "Burnout " + label + " differs: " + ModelJson.Difference(a, b));
    }
    private static void Require(bool condition, string error) { if (!condition) throw new InvalidOperationException(error); }
}

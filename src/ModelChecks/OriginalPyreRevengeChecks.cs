using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalPyreRevengeChecks
{
    private const string RelicId = "82b22203-9da1-4f83-9e84-826fb8d1d280";
    private const string RelicKey = "PyreRevengeAddPyregel";
    private const string PyreSubtype = "SubtypesData_Pyre";

    internal static void Run()
    {
        var status = new CombatStatus("pyregel", 2, 1, removeStackAtEnd: true);
        var action = new CardActionEffect("AddStatus", "Tower", 0, true, false, [status]);
        var effect = new CombatEffect("CardEffectAddStatusEffect", 0, 0, "", 0, [], false, action: action);
        var trigger = new CombatTrigger("OnHit", false, false, true, 1, [effect], false,
            origin: new CombatTriggerOrigin("", 0, false, false), noCountModifiersAllowed: true);
        var rule = new RelicCharacterTriggers(0, true, true, [trigger], true, PyreSubtype, false,
            subtypeIsPyre: true);
        var owner = new CombatRelicState(RelicId, RelicKey, [RelicModel.AddTrigger], isCovenant: false,
            disallowedInPlacementPhase: false, triggerAdditions: [rule]);
        var counts = new TriggerCountState([new TriggerCountModifier("OnHit", 0)], [], [], []);
        var rng = UnityRng.Seed(23);
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, 10,
            relics: [owner], triggerCounts: counts);
        var pyre = new CombatUnit(1, "PyreHeart", CombatTeam.Player, 5, 100, 100, true, true, true,
            [], subtypes: [PyreSubtype], nextTriggerId: 0);
        var enemy = new CombatUnit(2, "hero", CombatTeam.Enemy, 3, 20, 20, true, false, false, [], isBoss: false);
        var root = new RoomCombatState(0, false, [pyre, enemy], [], context);
        Require(RelicModel.Validate([owner]) == null, "The original Pyre Revenge owner was rejected.");

        string parent = Serialize(root);
        RoomCombatResult installed = RelicSpawnStatusModel.CharacterAdded(root, pyre.Id, 0, false);
        Require(installed.Supported && installed.State!.Units.Single(unit => unit.IsPyre).Triggers.Single() is var added &&
            added.Kind == "OnHit" && added.Effects.Single().Action is { Target: "Tower", AllowEnemy: true, AllowPlayer: false } &&
            added.Effects.Single().Action!.Statuses.Single() is var addedStatus && addedStatus.Id == "pyregel" && addedStatus.Stacks == 2 &&
            installed.State.Units.Single(unit => unit.IsPyre).NextTriggerId == 1,
            installed.UnsupportedReason ?? "The original Pyre-only OnHit trigger was not installed exactly once.");
        RoomCombatState installedState = installed.State!;
        RoomCombatState preview = new(root.RoomIndex, root.Deployment, root.Units, root.ExternalInteractions, root.Context, preview: true);
        RoomCombatResult skippedPreview = RelicSpawnStatusModel.CharacterAdded(preview, pyre.Id, 0, false);
        Require(skippedPreview.Supported && !skippedPreview.State!.Units.Single(unit => unit.IsPyre).Triggers.Any(trigger => trigger.Kind == "OnHit"),
            "Pyre trigger installation leaked into preview.");

        RoomCombatResult fired = TriggeredStatusModel.Apply(installedState, pyre.Id, effect, [enemy.Id]);
        Require(fired.Supported && fired.State!.Units.Single(unit => unit.Id == enemy.Id).Status("pyregel")?.Stacks == 2 &&
            fired.State.Context!.BattleRng.Equals(rng), fired.UnsupportedReason ?? "The OnHit Tower effect did not add two Pyregel to Heroes.");

        var towerRoot = new TrainCombatState([
            new RoomCombatState(0, false, [new CombatUnit(4, "remote-hero", CombatTeam.Enemy, 0, 20, 20, false, false, false, [])], [], installedState.Context),
            new RoomCombatState(1, false, [installedState.Units.Single(unit => unit.IsPyre),
                new CombatUnit(3, "pyre-room-hero", CombatTeam.Enemy, 1, 20, 20, true, false, false, [])], [], installedState.Context)
        ], [], 5, installedState.Context);
        TrainCombatResult towerResult = TrainCombatModel.ResolveCombat(towerRoot);
        string towerDetails = towerResult.State == null ? towerResult.UnsupportedReason ?? "no state" :
            string.Join(";", towerResult.State.Rooms.SelectMany(room => room.Units).Select(unit =>
                unit.Id + "=" + (unit.Status("pyregel")?.Stacks ?? 0)));
        CombatEvent[] towerStatusEvents = towerResult.RoomResults.SelectMany(result => result.Events)
            .Where(item => item.Kind == "TriggeredStatus:pyregel").ToArray();
        towerDetails += " events=" + string.Join(",", towerStatusEvents.Select(item => item.Actor + "->" + item.Target + "x" + item.Amount));
        Require(towerResult.Supported && towerResult.State!.Rooms.Single(room => room.RoomIndex == 0).Units.Single(unit => unit.Id == 4).Status("pyregel")?.Stacks == 1 &&
            towerResult.State.Rooms.Single(room => room.RoomIndex == 1).Units.Single(unit => unit.Id == 3).Status("pyregel")?.Stacks == 1 &&
            towerStatusEvents.Select(item => item.Target).OrderBy(id => id).SequenceEqual(new[] { 3, 4 }) && towerStatusEvents.All(item => item.Amount == 2),
            "A Pyre OnHit did not apply and return Tower status to heroes on both floors: " + towerDetails);
        Parallel.For(0, 32, _ =>
        {
            RoomCombatResult branch = RelicSpawnStatusModel.CharacterAdded(root, pyre.Id, 0, false);
            Require(branch.Supported && Serialize(branch.State) == Serialize(installed.State), "Parallel Pyre trigger installs differed.");
        });
        Require(Serialize(root) == parent, "Pyre trigger installation mutated its immutable input.");
        Console.WriteLine("ORIGINAL-PYRE-REVENGE PURE PASS: Pyre-only OnHit installation, local and cross-floor +2 Tower Heroes Pyregel, preview exclusion and 32 immutable branches.");
    }

    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("ModifierScenario", out var scenario) || scenario.GetString() != "original-pyre-revenge") return;
        Require(fixture.GetProperty("Schema").GetInt32() == 178 && fixture.GetProperty("NativeWon").GetBoolean() &&
            fixture.GetProperty("OriginalPyreRevengePrepared").GetBoolean(), "Native Pyre Revenge battle is incomplete.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(fixture.GetProperty(key).GetInt32() == 0, "Native Pyre Revenge capture has nonzero " + key + ".");

        FixtureValue installation = fixture.GetProperty("OriginalPyreRevengeInstallation");
        Require(installation.GetProperty("Completed").GetBoolean(), "Native Pyre trigger installation capture is incomplete.");
        var before = installation.GetProperty("Before").Deserialize<RoomCombatState>()!;
        var actualInstall = installation.GetProperty("After").Deserialize<RoomCombatState>()!;
        int pyreId = installation.GetProperty("UnitId").GetInt32();
        CombatUnit pyreBefore = before.Units.Single(unit => unit.Id == pyreId);
        CombatUnit pyreAfter = actualInstall.Units.Single(unit => unit.Id == pyreId);
        CombatRelicState owner = before.Context!.Relics!.Single(relic => relic.DataId == RelicId);
        RelicCharacterTriggers rule = owner.TriggerAdditions!.Single();
        Require(owner.AssetKey == RelicKey && owner.EffectTypes.SequenceEqual([RelicModel.AddTrigger]) &&
            owner.IsCovenant == false && owner.DisallowedInPlacementPhase == false && rule.EffectIndex == 0 &&
            rule.SourcePlayer && rule.SourceEnemy && rule.RequiredSubtype == PyreSubtype && !rule.SubtypeIsNone &&
            rule.SubtypeIsPyre && rule.CharacterAssetKeys.Count == 0 && rule.Conditions.Count == 0 &&
            rule.NoCountModifiersAllowed && rule.Triggers.Count == 1,
            "Unchanged Pyre Revenge owner definition or targeting gates changed.");
        CombatTrigger definition = rule.Triggers.Single();
        Require(definition.Kind == "OnHit" && !definition.Once && definition.IgnoreSilence &&
            definition.Effects.Count == 1 && definition.Effects[0].Type == "CardEffectAddStatusEffect" &&
            definition.Effects[0].Action is { Type: "AddStatus", Target: "Tower", AllowEnemy: true, AllowPlayer: false } &&
            definition.Effects[0].Action!.Statuses.Select(status => (status.Id, status.Stacks)).SequenceEqual(new[] { ("pyregel", 2) }),
            "Native Pyre Revenge OnHit effect payload changed.");
        string parent = Serialize(before);
        RoomCombatResult install = RelicCharacterTriggerModel.Apply(before, pyreId, rule);
        Require(install.Supported && ModelJson.Difference(Serialize(install.State), Serialize(actualInstall)) == null &&
            pyreBefore.IsPyre && pyreAfter.Triggers.Count(trigger => trigger.Kind == "OnHit" &&
                trigger.Origin?.UpgradeId == "" && trigger.Effects.SingleOrDefault()?.Action?.Statuses.Any(status =>
                    status.Id == "pyregel" && status.Stacks == 2) == true) ==
                pyreBefore.Triggers.Count(trigger => trigger.Kind == "OnHit" && trigger.Effects.SingleOrDefault()?.Action?.Statuses.Any(status =>
                    status.Id == "pyregel" && status.Stacks == 2) == true) + 1 && Serialize(before) == parent,
            install.UnsupportedReason ?? "Native Pyre trigger installation differs from the independent model.");

        int fired = 0;
        foreach (FixtureValue record in fixture.GetProperty("TriggeredStatuses").EnumerateArray())
        {
            if (record.GetProperty("ActorId").GetInt32() != pyreId || record.GetProperty("Kind").GetString() != "OnHit") continue;
            Require(record.GetProperty("Completed").GetBoolean() && record.GetProperty("Interactions").GetArrayLength() == 0,
                "Native Pyre OnHit status callback is incomplete.");
            var roomBefore = record.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var actual = record.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            var retained = record.GetProperty("BeforeUnits").Deserialize<CombatUnit[]>()!;
            var actualUnits = record.GetProperty("ActualUnits").Deserialize<CombatUnit[]>()!;
            CombatEffect effect = record.GetProperty("Effect").Deserialize<CombatEffect>()!;
            int[] targets = record.GetProperty("Targets").Deserialize<int[]>()!;
            TrainCombatState beforeTrain = record.GetProperty("BeforeTrain").Deserialize<TrainCombatState>()!;
            Require(effect.Type == "CardEffectAddStatusEffect" && effect.Action is { Target: "Tower", AllowEnemy: true, AllowPlayer: false } &&
                effect.Action.Statuses.Select(status => (status.Id, status.Stacks)).SequenceEqual(new[] { ("pyregel", 2) }) && targets.Length > 0,
                "Native Pyre OnHit did not target Heroes throughout the Tower with two Pyregel.");
            int pyreRoomIndex = beforeTrain.Rooms.Single(room => room.Units.Any(unit => unit.Id == pyreId)).RoomIndex;
            CardTargets towerTargets = CardTargetModel.Collect(beforeTrain, pyreRoomIndex, effect.Action!, []);
            Require(towerTargets.Supported && towerTargets.UnitIds.SequenceEqual(targets),
                "Native Pyre Revenge Tower target order differs from complete-train target collection: " +
                string.Join(",", targets) + " vs " + string.Join(",", towerTargets.UnitIds));
            var scope = new RoomCombatState(roomBefore.RoomIndex, roomBefore.Deployment,
                roomBefore.Units.Concat(retained.Where(unit => roomBefore.Units.All(live => live.Id != unit.Id))).ToArray(),
                roomBefore.ExternalInteractions, roomBefore.Context);
            RoomCombatResult result = TriggeredStatusModel.Apply(scope, pyreId, effect, targets,
                record.GetProperty("SourceCardId").GetInt32());
            string? difference = result.Supported ? ModelJson.Difference(Serialize(result.State!.Context), Serialize(actual.Context)) : result.UnsupportedReason;
            foreach (CombatUnit unit in actualUnits)
            {
                difference ??= ModelJson.Difference(Serialize(result.State!.Units.Single(item => item.Id == unit.Id)), Serialize(unit));
                if (difference != null) break;
            }
            Require(result.Supported && difference == null,
                "Pyre Revenge native status callback differs at sequence " + record.GetProperty("Sequence").GetInt32() + ": " + difference);
            foreach (int targetId in targets)
            {
                CombatUnit oldUnit = retained.Single(unit => unit.Id == targetId);
                CombatUnit newUnit = actualUnits.Single(unit => unit.Id == targetId);
                int beforeStacks = oldUnit.Status("pyregel")?.Stacks ?? 0;
                int afterStacks = newUnit.Status("pyregel")?.Stacks ?? 0;
                Require(newUnit.Team == CombatTeam.Enemy && afterStacks - beforeStacks == 2,
                    "Native Pyre Revenge did not add exactly two Pyregel to each selected Hero.");
            }
            fired++;
        }
        Require(fired > 0, "The captured native battle did not fire Pyre Revenge on a live Pyre hit.");
        Console.WriteLine($"ORIGINAL-PYRE-REVENGE NATIVE PASS: unchanged owner install matched; {fired} live Pyre OnHit callbacks applied exactly +2 Tower Heroes Pyregel; full actions and turns matched.");
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool value, string error)
    {
        if (!value) throw new InvalidDataException(error);
    }
}

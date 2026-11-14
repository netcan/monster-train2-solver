using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class DamagedFloorEnergyBattleChecks
{
    private const string OwnerId = "970f699b-052e-4eec-ad98-ce32b9c5dc21";
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue root = document.RootElement;
        Require(root.GetProperty("GameVersion").GetString() == "2.3.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            root.GetProperty("ModifierScenario").GetString() == "damaged-floor-energy-battle" &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            root.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop", "Missing settled native floor-energy victory.");
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(counter).GetInt32() == 0, "Incomplete floor-energy battle: " + counter);
        FixtureValue evidence = root.GetProperty("DamagedFloorEnergyBattle");
        int[] ids = evidence.GetProperty("ControlCardIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(evidence.GetProperty("Prepared").GetBoolean() && evidence.GetProperty("CompletedControlPlays").GetInt32() == 8 &&
            ids.Length == 8 && ids.Distinct().Count() == 8 && actions.Length == 8 && actions.Select(row =>
                row.GetProperty("Action").GetProperty("CardInstanceId").GetInt32()).SequenceEqual(ids), "Missing eight ordered native plays.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatRelicState owner = Before(0).Spawn.Train.Context!.Relics!.Single(relic => relic.DataId == OwnerId);
        Require(owner.AssetKey == "EmberForDamagedPerFloor" && owner.IsCovenant == false && owner.DisallowedInPlacementPhase == false &&
            owner.EffectTypes.SequenceEqual([RelicDamagedFloorEnergyModel.Effect]) && owner.ResourceEffects is { Count: 1 } &&
            owner.ResourceEffects[0].ParamInt == 1 && owner.ResourceEffects[0].Amount == 1 && owner.ResourceEffects[0].Conditions.Count == 0,
            "Missing unchanged original damaged-floor energy owner.");
        FixtureValue definition = evidence.GetProperty("OwnerDefinition");
        Require(definition.GetProperty("Id").GetString() == OwnerId && definition.GetProperty("Key").GetString() == owner.AssetKey &&
            definition.GetProperty("Type").GetString() == RelicDamagedFloorEnergyModel.Effect && definition.GetProperty("Int").GetInt32() == 1 &&
            !definition.GetProperty("Placement").GetBoolean() && definition.GetProperty("Conditions").GetInt32() == 0,
            "Changed original floor-energy definition.");
        for (int floor = 0; floor < 3; floor++)
        {
            Require(After(floor * 2).Spawn.Train.Rooms.Single(room => room.RoomIndex == floor).Units.Single(unit => unit.SpawnerCardId == ids[floor * 2]) is
                { Health: 100, MaxHealth: 100, Attack: 60 }, "Missing three-floor healthy summon control.");
            Require(After(floor * 2 + 1).Spawn.Train.Rooms.Single(room => room.RoomIndex == floor).Units.Single(unit => unit.SpawnerCardId == ids[floor * 2]).Health == 99,
                "Missing three-floor damaged control.");
        }
        Require(Before(6).Spawn.Turn == 1 && Before(6).Energy == 4 && After(6).Energy == 3 &&
            After(6).Spawn.Train.Rooms.Single(room => room.RoomIndex == 1).Units.Single(unit => unit.SpawnerCardId == ids[2]).Health == 100 &&
            Before(7).Spawn.Turn == 2 && Before(7).Energy == 3 && After(7).Energy == 2 &&
            After(7).Spawn.Train.Rooms.Single(room => room.RoomIndex == 1).Units.Single(unit => unit.SpawnerCardId == ids[2]).Health == 99,
            "Native paid heal/reinjury did not disable/re-enable the floor condition.");
        FixtureValue[] turns = root.GetProperty("Turns").EnumerateArray().ToArray();
        Require(turns.Length == 5 && turns.Take(3).Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!.Energy).SequenceEqual([4, 3, 4]),
            "Native next-turn energy did not follow 4/3/4.");
        FixtureValue[] queries = evidence.GetProperty("Queries").EnumerateArray().ToArray();
        Require(queries.Length == 8 && queries.Count(row => row.GetProperty("Native").GetInt32() == 1) == 1,
            "Missing empty/partly healthy/all-damaged/fully healed native query sequence.");
        foreach (FixtureValue row in queries)
        {
            Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("Difference").ValueKind == FixtureKind.Null, "Incomplete native energy query.");
            TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
            string parent = Serialize(before);
            var result = RelicResourceModel.GetEnergyModification(before.Context!, train: before);
            Require(result.Supported && result.Value == row.GetProperty("Native").GetInt32(), "Native floor-energy query differs.");
            Require(parent == Serialize(before), "An energy query mutated its parent.");
        }
        FixtureValue[] operations = evidence.GetProperty("Operations").EnumerateArray().ToArray();
        Require(operations.Length == 3, "Missing the three actual native timing callbacks.");
        foreach (FixtureValue row in operations)
        {
            Require(row.GetProperty("Notifications").GetInt32() == 1, "Native floor-energy timing did not notify exactly once.");
            Apply(row);
        }
        FixtureValue calibration = evidence.GetProperty("Calibration");
        Require(calibration.GetProperty("Completed").GetBoolean() && calibration.GetProperty("LiveContextUnchanged").GetBoolean() &&
            calibration.GetProperty("RestorationDifference").ValueKind == FixtureKind.Null, "Native calibration did not restore the complete live train.");
        Equal(calibration.GetProperty("RngBefore").ToObjectGraph(), calibration.GetProperty("RngAfter").ToObjectGraph(), "all native RNG restoration");
        FixtureValue[] samples = calibration.GetProperty("Samples").EnumerateArray().ToArray();
        Require(samples.Length == 20 && samples.Take(16).Select(row => (row.GetProperty("Mask").GetInt32(),
            row.GetProperty("Before").GetProperty("Context").GetProperty("QueryFrame").GetProperty("Energy").GetInt32())).Distinct().Count() == 16,
            "Missing eight floor combinations with normal/capped energy.");
        foreach (FixtureValue row in samples) Verify(row);
        Parallel.For(0, 32, _ => { foreach (FixtureValue row in samples) Verify(row); });
        RoomCombatChecks.NativeRoomOnly(path); TrainCombatChecks.Native(root); RecordedBattleChecks.Native(root);
        Console.WriteLine("DAMAGED-FLOOR-ENERGY-BATTLE PASS: unchanged original EmberForDamagedPerFloor, eight plays and five EndTurns, 4/3/4 turn income, healed/missing/Pyre controls, 20 native successors and 32 immutable branches, eight pure queries, three notified timing callbacks, complete battle/RNG and 16 independent full chains.");
    }
    private static void Verify(FixtureValue row)
    {
        TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
        string parent = Serialize(before);
        bool expected = row.GetProperty("Mask").GetInt32() == 7 && row.GetProperty("Kind").GetString() == "mask";
        Require(RelicDamagedFloorEnergyModel.Check(before, out bool passed) == null && passed == expected &&
            passed == row.GetProperty("NativeCondition").GetBoolean() && passed == row.GetProperty("NativeTest").GetBoolean(),
            "Native every-floor condition, living roster or Pyre exclusion differs.");
        var query = RelicResourceModel.GetEnergyModification(before.Context!, train: before);
        Require(query.Supported && query.Value == (expected ? 1 : 0) && query.Value == row.GetProperty("NativeQuery").GetInt32(),
            "Native energy query differs from the roster predicate.");
        Require(!RelicResourceModel.GetEnergyModification(before.Context!).Supported &&
            RelicDamagedFloorEnergyModel.Check(new TrainCombatState(before.Rooms.Take(2).ToArray(), before.Movement, before.EnemySlotsPerRoom,
                before.Context), out _) is not null, "Missing/partial native room input was inferred.");
        Apply(row);
        Require(parent == Serialize(before), "A calibrated floor-energy branch mutated its parent.");
    }
    private static void Apply(FixtureValue row)
    {
        Require(row.GetProperty("Completed").GetBoolean() && row.GetProperty("Difference").ValueKind == FixtureKind.Null, "Incomplete native floor-energy successor.");
        TrainCombatState before = row.GetProperty("Before").Deserialize<TrainCombatState>()!;
        TrainCombatState actual = row.GetProperty("Actual").Deserialize<TrainCombatState>()!;
        var result = RelicResourceModel.ApplyEnergyTiming(before.Context!, train: before);
        Require(result.Supported, result.UnsupportedReason ?? "Native floor energy unsupported.");
        Equal(CardSpellModel.WithContext(before, result.Context!), actual, "complete native energy successor");
        Require(RelicDamagedFloorEnergyModel.Check(before, out bool passed) == null && result.Events.Count == (passed ? 1 : 0) &&
            result.Events.All(item => item.RelicId == OwnerId && item.EffectIndex == 0 && item.Notified), "Native timing notification decision differs.");
    }
    private static string Serialize(object? value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Equal(object? a, object? b, string label)
    {
        string? difference = ModelJson.Difference(Serialize(a), Serialize(b));
        Require(difference == null, label + ": " + difference);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

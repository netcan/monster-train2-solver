using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InactiveMushyTwoTurnChecks
{
    internal static void Native(FixtureValue root)
    {
        if (root.GetProperty("Schema").GetInt32() != 1 ||
            root.GetProperty("GameModuleMvid").GetString() != "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" ||
            !root.GetProperty("NativeCompleted").GetBoolean() ||
            !root.GetProperty("InactiveMushyTwoTurns").GetBoolean() ||
            root.GetProperty("Boundary").GetString() != "AfterTwoNativeEndTurnSettlements")
            throw new InvalidOperationException("Missing native two-turn Mushy capture.");
        int id = root.GetProperty("EnemyId").GetInt32();
        BattleTurnState before = root.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState first = root.GetProperty("AfterFirst").Deserialize<BattleTurnState>()!;
        BattleTurnState second = root.GetProperty("AfterSecond").Deserialize<BattleTurnState>()!;
        int Room(BattleTurnState state) => state.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.Id == id)).RoomIndex;
        CombatUnit Unit(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.Id == id);
        CombatUnit original = Unit(before), afterFirst = Unit(first), afterSecond = Unit(second);
        RoomCapacityAttachmentState attachment = before.Spawn.Train.Context!.RoomCapacities!
            .Single(capacity => capacity.RoomIndex == 0).Attachments!.Single();
        if (before.Spawn.Turn != 0 || first.Spawn.Turn != 1 || second.Spawn.Turn != 2 ||
            Room(before) != 0 || Room(first) != 1 || Room(second) != 2 ||
            before.Spawn.Train.Context.QueryFrame is not { MoonPhaseTrackingActive: false, MoonPhase: 1 } ||
            first.Spawn.Train.Context?.QueryFrame is not { MoonPhaseTrackingActive: false, MoonPhase: 2 } ||
            second.Spawn.Train.Context?.QueryFrame is not { MoonPhaseTrackingActive: false, MoonPhase: 1 } ||
            !RoomAttachmentUnitModel.OriginalMushy(attachment) ||
            original is not { AssetKey: "MageT1_Junker", Team: CombatTeam.Enemy,
                SpawnerCardId: 0, MoonPhaseMask: 6 } ||
            original.Triggers.Any(trigger => trigger.Kind.Contains("Moon")) ||
            afterFirst.MoonPhaseMask != 5 || afterSecond.MoonPhaseMask != 5 ||
            original.Modifiers?.Upgrades.Count != 1 || afterFirst.Modifiers?.Upgrades.Count != 0 ||
            afterSecond.Modifiers?.Upgrades.Count != 0)
            throw new InvalidOperationException("Missing original Mushy two-turn ascension witness.");
        string firstKey = NativeBranchObservationChecks.Key(first);
        string secondKey = NativeBranchObservationChecks.Key(second);
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        void Replay()
        {
            BattleTurnResult one = BattleTurnModel.EndTurn(before);
            if (!one.Supported || one.Outcome != RoomOutcome.Exchanged)
                throw new InvalidOperationException("First Mushy turn unsupported: " + one.UnsupportedReason);
            string oneKey = NativeBranchObservationChecks.Key(one.State!);
            if (oneKey != firstKey)
                throw new InvalidOperationException("First Mushy turn differs: " + ModelJson.Difference(oneKey, firstKey));
            BattleTurnResult two = BattleTurnModel.EndTurn(one.State!);
            if (!two.Supported || two.Outcome != RoomOutcome.Exchanged)
                throw new InvalidOperationException("Second Mushy turn unsupported: " + two.UnsupportedReason);
            string twoKey = NativeBranchObservationChecks.Key(two.State!);
            if (twoKey != secondKey)
                throw new InvalidOperationException("Second Mushy turn differs: " + ModelJson.Difference(twoKey, secondKey));
            BattleTurnResult fromNative = BattleTurnModel.EndTurn(first);
            if (!fromNative.Supported || NativeBranchObservationChecks.Key(fromNative.State!) != secondKey)
                throw new InvalidOperationException("Second Mushy turn from native state differs: " + fromNative.UnsupportedReason);
            BattleTurnState planner = BattlePlannerChecks.Replay(before,
                [new BattleSearchStep(null, true), new BattleSearchStep(null, true)], out RoomOutcome outcome);
            if (outcome != RoomOutcome.Exchanged || NativeBranchObservationChecks.Key(planner) != secondKey)
                throw new InvalidOperationException("Two-turn planner replay differs.");
        }
        Replay(); Parallel.For(0, 4, _ => Replay());
        if (immutable != JsonSerializer.Serialize(before, ModelJson.Options))
            throw new InvalidOperationException("Two-turn replay mutated its parent.");
        Console.WriteLine("INACTIVE-MUSHY-TWO-TURN PASS: rooms 0/1/2, masks 6/5/5, complete chained successors/RNG and four immutable branches.");
    }
}

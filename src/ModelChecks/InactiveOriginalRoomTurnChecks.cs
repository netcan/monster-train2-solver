using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InactiveOriginalRoomTurnChecks
{
    internal static void Native(FixtureValue root)
    {
        string key = root.GetProperty("OriginalTurnRoom").GetString()!;
        string dataId = key switch
        {
            "BoilerRoom" => "08001aa4-2e9d-42da-9ba7-ff4b0fd5ce80",
            "SacrificialChamber" => "627f12a8-081e-437d-8a54-a20d719163b7",
            _ => throw new InvalidOperationException("Unknown original room EndTurn capture: " + key)
        };
        Require(root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("NativeCompleted").GetBoolean() &&
            root.GetProperty("Boundary").GetString() == "AfterNativeEndTurnSettlement",
            "Missing native original room EndTurn capture.");
        int id = root.GetProperty("EnemyId").GetInt32();
        BattleTurnState before = root.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = root.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        int Floor(BattleTurnState state) => state.Spawn.Train.Rooms.Single(room =>
            room.Units.Any(unit => unit.Id == id)).RoomIndex;
        CombatUnit Unit(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.Id == id);
        CombatUnit prior = Unit(before), moved = Unit(actual);
        RoomCapacityAttachmentState attachment = before.Spawn.Train.Context!.RoomCapacities!
            .Single(room => room.RoomIndex == 0).Attachments!.Single();
        Require(id > 0 && before.Spawn.Turn == 0 && actual.Spawn.Turn == 1 &&
            Floor(before) == 0 && Floor(actual) == 1 &&
            before.Spawn.Train.Context.QueryFrame is { MoonPhaseTrackingActive: false, MoonPhase: 1 } &&
            actual.Spawn.Train.Context?.QueryFrame is { MoonPhaseTrackingActive: false, MoonPhase: 2 } &&
            attachment.AssetKey == key && attachment.DataId == dataId &&
            RoomAttachmentUnitModel.OriginalTriggerRoom(attachment) &&
            prior is { AssetKey: "MageT1_Junker", Team: CombatTeam.Enemy,
                SpawnerCardId: 0, MoonPhaseMask: 6 } &&
            prior.Triggers.Count == 2 && prior.Triggers.All(trigger => !trigger.Kind.Contains("Moon")) &&
            prior.Modifiers?.Upgrades.Count == 1 &&
            moved.MoonPhaseMask == 5 && moved.Triggers.Count == 1 &&
            moved.Modifiers?.Upgrades.Count == 0,
            "Missing original " + key + " natural ascension witness.");
        string expected = NativeBranchObservationChecks.Key(actual);
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        void Replay()
        {
            BattleTurnResult result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && result.Outcome == RoomOutcome.Exchanged,
                key + " natural EndTurn unsupported: " + result.UnsupportedReason);
            string predicted = NativeBranchObservationChecks.Key(result.State!);
            Require(predicted == expected, key + " natural EndTurn differs: " +
                ModelJson.Difference(predicted, expected));
            BattleTurnState planner = BattlePlannerChecks.Replay(before,
                [new BattleSearchStep(null, true)], out RoomOutcome outcome);
            Require(outcome == RoomOutcome.Exchanged &&
                NativeBranchObservationChecks.Key(planner) == expected,
                key + " one-step planner EndTurn differs from native.");
        }
        Replay(); Parallel.For(0, 4, _ => Replay());
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options),
            key + " natural EndTurn mutated its parent.");
        Console.WriteLine("INACTIVE-ORIGINAL-ROOM-TURN PASS: " + key +
            ", natural ascension, trigger removal, mask 6->5, full successor/RNG and four immutable branches.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}

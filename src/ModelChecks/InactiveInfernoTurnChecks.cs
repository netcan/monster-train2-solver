using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InactiveInfernoTurnChecks
{
    internal static void Native(FixtureValue root)
    {
        if (root.GetProperty("Schema").GetInt32() != 1 ||
            root.GetProperty("GameModuleMvid").GetString() != "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" ||
            !root.GetProperty("NativeCompleted").GetBoolean() ||
            root.GetProperty("InactiveMushyTurn").GetBoolean() ||
            root.GetProperty("Boundary").GetString() != "AfterNativeEndTurnSettlement")
            throw new InvalidOperationException("Missing original inactive Inferno EndTurn capture.");
        int id = root.GetProperty("EnemyId").GetInt32();
        BattleTurnState before = root.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = root.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit prior = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
        CombatUnit moved = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
        int oldRoom = before.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.Id == id)).RoomIndex;
        int newRoom = actual.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.Id == id)).RoomIndex;
        RoomCapacityAttachmentState room = before.Spawn.Train.Context!.RoomCapacities!
            .Single(capacity => capacity.RoomIndex == 0).Attachments!.Single();
        if (before.Spawn.Turn != 0 || actual.Spawn.Turn != 1 || oldRoom != 0 || newRoom != 1 ||
            before.Spawn.Train.Context.QueryFrame is not { MoonPhaseTrackingActive: false, MoonPhase: 1 } ||
            actual.Spawn.Train.Context?.QueryFrame is not { MoonPhaseTrackingActive: false, MoonPhase: 2 } ||
            !RoomAttachmentUnitModel.OriginalInferno(room) ||
            prior is not { AssetKey: "HeavyT3_Basic", Team: CombatTeam.Enemy, SpawnerCardId: 0,
                Health: 325, MoonPhaseMask: 6 } || prior.Triggers.Any(trigger => trigger.Kind.Contains("Moon")) ||
            moved.Health != 325 || moved.MoonPhaseMask != 5 ||
            prior.Modifiers?.Upgrades.Count != 1 || moved.Modifiers?.Upgrades.Count != 0)
            throw new InvalidOperationException("Missing original Inferno natural ascension witness.");
        string expected = NativeBranchObservationChecks.Key(actual);
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        void Replay()
        {
            BattleTurnResult result = BattleTurnModel.EndTurn(before);
            if (!result.Supported || result.Outcome != RoomOutcome.Exchanged)
                throw new InvalidOperationException("Inferno natural EndTurn unsupported: " + result.UnsupportedReason);
            string predicted = NativeBranchObservationChecks.Key(result.State!);
            if (predicted != expected)
                throw new InvalidOperationException("Inferno natural EndTurn differs: " + ModelJson.Difference(predicted, expected));
        }
        Replay(); Parallel.For(0, 4, _ => Replay());
        if (immutable != JsonSerializer.Serialize(before, ModelJson.Options))
            throw new InvalidOperationException("Inferno natural EndTurn mutated its parent.");
        Console.WriteLine("INACTIVE-INFERNO-TURN PASS: original room, natural ascension, mask 6->5, full successor/RNG and four immutable branches.");
    }
}

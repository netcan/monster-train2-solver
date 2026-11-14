using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InactiveMushyTurnChecks
{
    internal static void Native(FixtureValue root)
    {
        if (root.GetProperty("Schema").GetInt32() != 1 ||
            root.GetProperty("GameModuleMvid").GetString() != "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" ||
            !root.GetProperty("NativeCompleted").GetBoolean() ||
            !root.GetProperty("InactiveMushyTurn").GetBoolean() ||
            root.GetProperty("Boundary").GetString() != "AfterNativeEndTurnSettlement")
            throw new InvalidOperationException("Missing original inactive Mushy EndTurn capture.");
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
            !RoomAttachmentUnitModel.OriginalMushy(room) ||
            prior is not { AssetKey: "MageT1_Junker", Team: CombatTeam.Enemy, SpawnerCardId: 0, MoonPhaseMask: 6 } ||
            prior.Triggers.Any(trigger => trigger.Kind.Contains("Moon")) || moved.MoonPhaseMask != 5 ||
            prior.Modifiers?.Upgrades.Count != 1 || moved.Modifiers?.Upgrades.Count != 0)
            throw new InvalidOperationException("Missing original Mushy natural ascension witness: " +
                $"turn={before.Spawn.Turn}->{actual.Spawn.Turn} room={oldRoom}->{newRoom} " +
                $"phase={before.Spawn.Train.Context.QueryFrame?.MoonPhase}/{actual.Spawn.Train.Context?.QueryFrame?.MoonPhase} " +
                $"tracking={before.Spawn.Train.Context.QueryFrame?.MoonPhaseTrackingActive}/{actual.Spawn.Train.Context?.QueryFrame?.MoonPhaseTrackingActive} " +
                $"attachment={RoomAttachmentUnitModel.OriginalMushy(room)} enemy={prior.AssetKey}/{prior.Team}/{prior.SpawnerCardId} " +
                $"mask={prior.MoonPhaseMask}->{moved.MoonPhaseMask} upgrades={prior.Modifiers?.Upgrades.Count}->{moved.Modifiers?.Upgrades.Count} " +
                $"moonCallbacks={prior.Triggers.Any(trigger => trigger.Kind.Contains("Moon"))}");
        string expected = NativeBranchObservationChecks.Key(actual);
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        void Replay()
        {
            BattleTurnResult result = BattleTurnModel.EndTurn(before);
            if (!result.Supported || result.Outcome != RoomOutcome.Exchanged)
                throw new InvalidOperationException("Mushy natural EndTurn unsupported: " + result.UnsupportedReason);
            string predicted = NativeBranchObservationChecks.Key(result.State!);
            if (predicted != expected)
                throw new InvalidOperationException("Mushy natural EndTurn differs: " + ModelJson.Difference(predicted, expected));
        }
        Replay(); Parallel.For(0, 4, _ => Replay());
        if (immutable != JsonSerializer.Serialize(before, ModelJson.Options))
            throw new InvalidOperationException("Mushy natural EndTurn mutated its parent.");
        Console.WriteLine("INACTIVE-MUSHY-TURN PASS: original room, natural ascension, mask 6->5, full successor/RNG and four immutable branches.");
    }
}

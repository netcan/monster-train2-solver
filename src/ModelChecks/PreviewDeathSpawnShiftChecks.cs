using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PreviewDeathSpawnShiftChecks
{
    internal const string Reason = "Battle preview floor 1: Physical shift requires its retained occupant's combat state: unit=35, room=1, preview=True.";

    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 51, "Missing50-input native prefix and51st EndTurn.");
        var row = rows[^1];
        var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(row.GetProperty("IsEndTurn").GetBoolean() && before.Spawn.Turn == 8 && actual.Spawn.Turn == 9,
            "Lost turn8-to9 death-spawn preview boundary.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        string expected = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            var branch = BattlePlannerChecks.Replay(before, [new BattleSearchStep(null, true)], out var outcome);
            Require(outcome == RoomOutcome.Exchanged && NativeBranchObservationChecks.Key(branch) == expected,
                "Death-spawn shift differs from complete native successor/forecast/RNG.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Preview death-spawn mutated its parent.");

        // Missing actors still reject; a physical reference alone cannot supply
        // the health/despawn/destroyed fields needed to shift occupants.
        var context = before.Spawn.Train.Context!.WithEnchantments(null);
        var points = context.SpawnPoints!;
        var group = points.SelectedGroup(1, CombatTeam.Player)!;
        int missing = group.Occupants.First(id => id != 0);
        var scope = new RoomCombatState(1, false, [], [], context, before.Spawn.Train.Rooms[1].Preview);
        var unknown = BattleSpawnPointModel.Apply(points, scope, "ShiftOccupants", CombatTeam.Player);
        Require(!unknown.Supported && unknown.Error == "Physical shift requires its retained occupant's combat state: unit=" +
            missing + ", room=1, preview=" + scope.Preview + ".", "Unknown physical occupant was silently invented.");

        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value);
        }
        Console.WriteLine("PREVIEW-DEATH-SPAWN-SHIFT PASS:51 complete inputs/forecasts/RNG, four immutable EndTurns, unknown-occupant guard; independentPair=" + replay.HasValue);
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TacticalSelectionChecks
{
    internal static void Native(FixtureValue fixture)
    {
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(fixture.GetProperty(counter).GetInt32() == 0, "Unaccepted selection witness: " + counter);
        using var keys = new BattleStateKeys();
        int roots = 0, before = 0, after = 0;
        foreach (var row in fixture.GetProperty("RollingDecisions").EnumerateArray())
        {
            BattleTurnState state = row.GetProperty("Root").Deserialize<BattleTurnState>()!;
            string parent = keys.Key(state);
            int floor = state.Spawn.Train.Rooms.FirstOrDefault(room => room.Units.Any(unit =>
                unit.Team == CombatTeam.Player && unit.Health > 0 && unit.Modifiers?.IsChampion == true))?.RoomIndex ?? 2;
            var reference = new BattleSearchWorkspace(false);
            var optimized = new BattleSearchWorkspace();
            var expected = reference.Choose(state, out var exhaustive, floor);
            var actual = optimized.Choose(state, out var attempted, floor);
            Require(JsonSerializer.Serialize(expected?.Action) == JsonSerializer.Serialize(actual?.Action),
                "Lazy selection changed the reference policy at input " + roots);
            if (expected != null)
                Require(actual != null && expected.Result.Outcome == actual.Result.Outcome &&
                    keys.Key(expected.Result.State!) == keys.Key(actual.Result.State!),
                    "Lazy selection changed a complete successor/RNG at input " + roots);
            Require(attempted.All(attempt => exhaustive.Any(other => other.Reason == attempt.Reason &&
                JsonSerializer.Serialize(other.Action) == JsonSerializer.Serialize(attempt.Action))),
                "Lazy selection lost an attempted unsupported diagnostic.");
            Require(keys.Key(state) == parent, "Selection candidates mutated the native parent.");
            before += reference.Metrics.SimulatedCandidates;
            after += optimized.Metrics.SimulatedCandidates;
            roots++;
        }
        Require(roots > 0 && after < before, "Selection witness did not eliminate candidate simulations.");
        Console.WriteLine($"TACTICAL-SELECTION PASS roots={roots} candidates={before}->{after}: " +
            "identical inputs, complete successors/RNG, immutable native parents and attempted diagnostics; full frontier unchanged.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

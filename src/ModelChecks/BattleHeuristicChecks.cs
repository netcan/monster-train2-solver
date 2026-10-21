using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class BattleHeuristicChecks
{
    private static BattleTurnState CapturedRoot(FixtureValue fixture) =>
        (fixture.TryGetProperty("Root", out var root) ? root :
            fixture.GetProperty("RollingDecisions")[0].GetProperty("Root")).Deserialize<BattleTurnState>()!;

    internal static void RollingForecast(FixtureValue fixture, int maxNodes, int preferredDeploymentRoom = 2)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        BattleTurnState root = CapturedRoot(fixture);
        string original = JsonSerializer.Serialize(root);
        BattleTurnState state = root;
        var steps = new List<BattleSearchStep>();
        int nodes = 0;
        for (int decision = 0; decision < 200; decision++)
        {
            using var keys = new BattleStateKeys();
            BattlePlanResult plan = BattlePlanner.Plan(state, keys.Key,
                new BattlePlannerOptions(maxNodes, 3, 8, timeLimit: TimeSpan.FromSeconds(3),
                    preferredDeploymentRoom: preferredDeploymentRoom));
            if (!plan.CompletedHorizon || plan.Steps.Count == 0) throw new InvalidOperationException("No complete rolling forecast.");
            BattleSearchStep step = plan.Steps[0];
            state = BattlePlannerChecks.Replay(state, new[] { step }, out RoomOutcome outcome);
            steps.Add(step); nodes += plan.ExpandedNodes;
            if (step.IsEndTurn)
                Console.WriteLine($"ROLLING-FORECAST-TURN turn={state.Spawn.Turn} outcome={outcome} pyre={state.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.IsPyre)?.Health ?? 0} decisions={steps.Count}");
            if (outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated or RoomOutcome.Stalemate)
            {
                BattleTurnState replayed = BattlePlannerChecks.Replay(root, steps, out RoomOutcome replayOutcome);
                if (JsonSerializer.Serialize(replayed) != JsonSerializer.Serialize(state) || replayOutcome != outcome ||
                    JsonSerializer.Serialize(root) != original) throw new InvalidOperationException("Rolling forecast replay or source isolation failed.");
                Console.WriteLine($"ROLLING-FORECAST outcome={outcome} decisions={steps.Count} nodes={nodes} elapsedMs={watch.Elapsed.TotalMilliseconds:F1}; model-only diagnosis, native scheduling may choose another bounded path.");
                return;
            }
        }
        throw new InvalidOperationException("Rolling forecast exceeded 200 decisions.");
    }

    // Fast offline strategy diagnosis from a captured native root. This does
    // not count as a native victory or independent native replay acceptance.
    internal static RoomOutcome Forecast(FixtureValue fixture, int preferredDeploymentRoom = 2)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        BattleTurnState root = CapturedRoot(fixture);
        string original = JsonSerializer.Serialize(root);
        BattleTurnState state = root;
        RoomOutcome outcome = RoomOutcome.Exchanged;
        var steps = new List<BattleSearchStep>();
        var omittedReasons = new HashSet<string>();
        for (int decision = 0; decision < 200; decision++)
        {
            var chosen = BattleHeuristicPolicy.Choose(state, out var omitted, preferredDeploymentRoom);
            foreach (var attempt in omitted) omittedReasons.Add(attempt.Reason);
            if (chosen != null)
            {
                state = chosen.Result.State!; outcome = chosen.Result.Outcome;
                steps.Add(new BattleSearchStep(chosen.Action, false));
            }
            else
            {
                var ended = BattleTurnModel.EndTurn(state);
                if (!ended.Supported) throw new InvalidOperationException(ended.UnsupportedReason);
                state = ended.State!; outcome = ended.Outcome;
                steps.Add(new BattleSearchStep(null, true));
            }
            if (outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated or RoomOutcome.Stalemate)
            {
                var replay = BattlePlannerChecks.Replay(root, steps, out var replayOutcome);
                if (JsonSerializer.Serialize(replay) != JsonSerializer.Serialize(state) || outcome != replayOutcome ||
                    JsonSerializer.Serialize(root) != original) throw new InvalidOperationException("Heuristic forecast replay or source immutability failed.");
                Console.WriteLine($"HEURISTIC-FORECAST outcome={outcome} decisions={steps.Count} score={BattlePlanner.Evaluate(state, outcome)} elapsedMs={watch.Elapsed.TotalMilliseconds:F1} omittedReasons={string.Join("; ", omittedReasons)}; model-only diagnosis.");
                return outcome;
            }
        }
        throw new InvalidOperationException("Heuristic forecast exceeded 200 decisions.");
    }
}

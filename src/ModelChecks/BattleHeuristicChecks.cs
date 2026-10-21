using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class BattleHeuristicChecks
{
    // Fast offline strategy diagnosis from a captured native root. This does
    // not count as a native victory or independent native replay acceptance.
    internal static void Forecast(FixtureValue fixture)
    {
        BattleTurnState root = fixture.GetProperty("RollingDecisions").EnumerateArray().First()
            .GetProperty("Root").Deserialize<BattleTurnState>()!;
        string original = JsonSerializer.Serialize(root);
        BattleTurnState state = root;
        RoomOutcome outcome = RoomOutcome.Exchanged;
        var steps = new List<BattleSearchStep>();
        var omittedReasons = new HashSet<string>();
        for (int decision = 0; decision < 200; decision++)
        {
            var chosen = BattleHeuristicPolicy.Choose(state, out var omitted);
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
                Console.WriteLine($"HEURISTIC-FORECAST outcome={outcome} decisions={steps.Count} score={BattlePlanner.Evaluate(state, outcome)} omittedReasons={string.Join("; ", omittedReasons)}; model-only diagnosis.");
                return;
            }
        }
        throw new InvalidOperationException("Heuristic forecast exceeded 200 decisions.");
    }
}

using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class BattlePlannerChecks
{
    internal static void Native(FixtureValue fixture, int turn, int horizon)
    {
        FixtureValue[] rows = fixture.GetProperty("Actions").EnumerateArray().ToArray();
        BattleTurnState root = rows.Select(row => row.GetProperty("Before").Deserialize<BattleTurnState>()!)
            .First(state => state.Spawn.Turn >= turn);
        string original = Key(root);
        using var keys = new BattleStateKeys();
        using var independentKeys = new BattleStateKeys();
        Require(keys.Key(root) == independentKeys.Key(rows.Select(row => row.GetProperty("Before").Deserialize<BattleTurnState>()!)
            .First(state => state.Spawn.Turn >= turn)), "Independent equivalent snapshots must have equal keys.");
        string rootKey = keys.Key(root);
        foreach (string field in new[] { "Energy", "SelectedRoom", "Rng", "History" })
        {
            JsonNode changed = JsonNode.Parse(original)!;
            if (field == "Rng") changed["RngStreams"]![0]!["State"]!["S0"] = root.RngStreams[0].State.S0 ^ 1u;
            else if (field == "History") changed["Spawn"]!["Train"]!["Context"]!["Statistics"]!["HandCardsPlayedLastTurn"] = new JsonArray(123456);
            else changed[field] = changed[field]!.GetValue<int>() + 1;
            BattleTurnState different = changed.Deserialize<BattleTurnState>()!;
            Require(keys.Key(different) != rootKey, "State deduplication omitted " + field + ".");
        }
        var options = new BattlePlannerOptions(maxNodes: 256, maxTurns: horizon, beamWidth: 8,
            timeLimit: TimeSpan.FromSeconds(3));
        BattlePlanResult plan = BattlePlanner.Plan(root, keys.Key, options);
        Require(plan.CompletedHorizon && plan.Steps.Count > 0 && plan.ExpandedNodes <= options.MaxNodes,
            "No bounded complete-horizon recommendation.");
        Require(plan.UnsupportedAttempts.Count == 0, "Planner encountered unsupported branches: " +
            string.Join("; ", plan.UnsupportedAttempts.Select(attempt => attempt.Reason)));
        BattleTurnState replayed = Replay(root, plan.Steps, out RoomOutcome outcome);
        Require(Key(replayed) == Key(plan.State) && outcome == plan.Outcome, "Planned path does not reproduce its complete state.");
        Require(Key(root) == original, "Planning mutated its source snapshot.");

        BattleTurnState baseline = root;
        RoomOutcome baselineOutcome = RoomOutcome.Exchanged;
        for (int i = 0; i < horizon; i++)
        {
            BattleTurnResult ended = BattleTurnModel.EndTurn(baseline);
            Require(ended.Supported, ended.UnsupportedReason ?? "Baseline failed.");
            baseline = ended.State!; baselineOutcome = ended.Outcome;
            if (baselineOutcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated or RoomOutcome.Stalemate) break;
        }
        Require(plan.Score <= BattlePlanner.Evaluate(baseline, baselineOutcome), "Planner lost its executable no-card fallback.");

        var cancelled = new CancellationToken(true);
        BattlePlanResult stopped = BattlePlanner.Plan(root, Key, options, cancellationToken: cancelled);
        Require(stopped.Cancelled && stopped.ExpandedNodes == 0 && stopped.Steps.Count == 0 && !stopped.CompletedHorizon,
            "Pre-cancellation must return an unchanged incomplete root.");
        BattlePlanResult expired = BattlePlanner.Plan(root, Key,
            new BattlePlannerOptions(timeLimit: TimeSpan.FromTicks(1)));
        Require(expired.TimedOut && expired.ExpandedNodes == 0 && !expired.CompletedHorizon,
            "Expired budgets must be reported without claiming a complete plan.");
        BattlePlanResult limited = BattlePlanner.Plan(root, Key,
            new BattlePlannerOptions(1, horizon, 1, timeLimit: TimeSpan.FromSeconds(3)));
        Require(limited.ExpandedNodes == 1 && limited.ReachedNodeLimit && limited.Steps.Count == 1,
            "Fallback must consume the same node budget as alternatives.");
        Require(BattlePlanner.Evaluate(root, RoomOutcome.BattleWon) < BattlePlanner.Evaluate(root, RoomOutcome.Exchanged) &&
            BattlePlanner.Evaluate(root, RoomOutcome.Exchanged) < BattlePlanner.Evaluate(root, RoomOutcome.PlayerDefeated),
            "Survival must never outrank a win or lose to a known defeat.");
        Console.WriteLine($"BATTLE-PLANNER PASS turn={root.Spawn.Turn} horizon={horizon} completed={plan.TurnsCompleted} " +
            $"nodes={plan.ExpandedNodes} duplicates={plan.DuplicateStates} beamPruned={plan.BeamPruned} " +
            $"elapsedMs={plan.Elapsed.TotalMilliseconds:F1} timeout={plan.TimedOut} score={plan.Score} " +
            $"baseline={BattlePlanner.Evaluate(baseline, baselineOutcome)} steps={plan.Steps.Count} outcome={plan.Outcome}");
    }

    internal static BattleTurnState Replay(BattleTurnState root, IReadOnlyList<BattleSearchStep> steps, out RoomOutcome outcome)
    {
        outcome = RoomOutcome.Exchanged;
        foreach (BattleSearchStep step in steps)
        {
            if (step.IsEndTurn)
            {
                BattleTurnResult result = BattleTurnModel.EndTurn(root);
                Require(result.Supported, result.UnsupportedReason ?? "Planned EndTurn rejected.");
                root = result.State!; outcome = result.Outcome;
            }
            else
            {
                BattleActionResult result = BattleActionModel.PlayCard(root, step.Action!);
                Require(result.Supported, result.Reason ?? "Planned play rejected.");
                root = result.State!; outcome = result.Outcome;
            }
        }
        return root;
    }

    internal static string Key(BattleTurnState state) => JsonSerializer.Serialize(state);
    private static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
}

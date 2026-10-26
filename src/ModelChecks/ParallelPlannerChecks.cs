using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ParallelPlannerChecks
{
    internal static void Merge(FixtureValue fixture, FixtureValue alternative)
    {
        foreach (var prediction in new[] { fixture, alternative })
            Require(prediction.GetProperty("EvidenceKind").GetString() == "ModelRootPrediction" &&
                prediction.GetProperty("NativeValidationRequired").GetBoolean(),
                "Merge witnesses must retain their model-only evidence classification.");
        Require(fixture.GetProperty("SourceSHA256").GetString() == alternative.GetProperty("SourceSHA256").GetString(),
            "Merge witnesses must derive from the same source archive.");
        BattleTurnState root = fixture.GetProperty("Root").Deserialize<BattleTurnState>()!;
        using var keys = new BattleStateKeys();
        string parent = keys.Key(root);
        var playedSteps = fixture.GetProperty("Plan").GetProperty("Steps").EnumerateArray().Select(row => new BattleSearchStep(
            row.GetProperty("Action").Deserialize<PlayCardAction>(), row.GetProperty("IsEndTurn").GetBoolean())).ToArray();
        BattleTurnState played = BattlePlannerChecks.Replay(root, playedSteps, out RoomOutcome outcome);
        Require(outcome == RoomOutcome.PlayerDefeated &&
            keys.Key(played) == keys.Key(fixture.GetProperty("Plan").GetProperty("State").Deserialize<BattleTurnState>()!),
            "Played merge witness differs from its independently replayed forecast.");
        Require(keys.Key(alternative.GetProperty("Root").Deserialize<BattleTurnState>()!) == parent,
            "Merge alternative must be an explicitly model-only forecast from the same complete root/RNG.");
        FixtureValue recorded = alternative.GetProperty("Plan");
        var idleSteps = recorded.GetProperty("Steps").EnumerateArray().Select(step => new BattleSearchStep(
            step.GetProperty("Action").Deserialize<PlayCardAction>(), step.GetProperty("IsEndTurn").GetBoolean())).ToArray();
        BattleTurnState idle = BattlePlannerChecks.Replay(root, idleSteps, out RoomOutcome idleOutcome);
        Require(idleOutcome == RoomOutcome.PlayerDefeated && idleSteps.Length < playedSteps.Length &&
            idleSteps.Count(step => step.IsEndTurn) == playedSteps.Count(step => step.IsEndTurn) &&
            keys.Key(idle) == keys.Key(recorded.GetProperty("State").Deserialize<BattleTurnState>()!) &&
            BattlePlanner.PositionCost(played) < BattlePlanner.PositionCost(idle),
            $"Missing shorter, equally scored but worse terminal alternative: outcome={idleOutcome}, " +
            $"steps={idleSteps.Length}/{playedSteps.Length}, turns={idleSteps.Count(step => step.IsEndTurn)}/{playedSteps.Count(step => step.IsEndTurn)}, " +
            $"cost={BattlePlanner.PositionCost(played)}/{BattlePlanner.PositionCost(idle)}, " +
            $"stateMatch={keys.Key(idle) == keys.Key(recorded.GetProperty("State").Deserialize<BattleTurnState>()!)}.");
        BattlePlanResult Plan(BattleTurnState state, IReadOnlyList<BattleSearchStep> steps, double score) =>
            new(state, RoomOutcome.PlayerDefeated, steps, score, steps.Count(step => step.IsEndTurn),
                0, 0, 0, TimeSpan.Zero, false, false, false, false, true,
                Array.Empty<UnsupportedActionAttempt>(), null, searchedToBattleEnd: true);
        var useful = Plan(played, playedSteps, BattlePlanner.Evaluate(played, outcome));
        var empty = Plan(idle, idleSteps, BattlePlanner.Evaluate(idle, idleOutcome));
        Require(useful.Score == empty.Score, "Merge witness must retain the public loss score plateau.");
        foreach (var alternatives in new[] { new[] { useful, empty }, new[] { empty, useful } })
            Require(ReferenceEquals(ParallelBattlePlanner.SelectBest(alternatives), useful),
                "Portfolio merge discarded the useful equal-score route for fewer steps.");
        var primary = Plan(idle, idleSteps, useful.Score - 1);
        Require(ReferenceEquals(ParallelBattlePlanner.SelectBest(new[] { useful, primary }), primary),
            "Positional merge tie-breaker overrode the primary objective.");
        Require(keys.Key(root) == parent, "Merge alternatives mutated their shared native root.");
        Console.WriteLine($"PARALLEL-MERGE PASS: same-root complete losses, played={playedSteps.Length} shorter={idleSteps.Length}, " +
            $"cost={BattlePlanner.PositionCost(played)}<{BattlePlanner.PositionCost(idle)}, order independence, primary score and immutable full-state/RNG replay.");
    }

    internal static void NativePair(FixtureValue source, FixtureValue replay)
    {
        FloorBrowseAdviceChecks.NativePair(source, replay);
        var overlay = source.GetProperty("PlayerAdvisor").GetProperty("Overlay");
        int mainThread = overlay.GetProperty("MainThreadId").GetInt32();
        Require(overlay.GetProperty("WholeBattle").GetBoolean() && overlay.GetProperty("MaxSteps").GetInt32() == 256,
            "Native whole-battle mode/step bound is missing.");
        foreach (var row in overlay.GetProperty("Publications").EnumerateArray())
        {
            int[] threads = row.GetProperty("WorkerThreadIds").EnumerateArray().Select(value => value.GetInt32()).ToArray();
            Require(row.GetProperty("SearchedToBattleEnd").GetBoolean() && threads.Length == 2 &&
                threads.Distinct().Count() == 2 && !threads.Contains(mainThread),
                "A native parallel publication used the wrong mode or Unity's thread.");
        }
        var decisions = source.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(decisions[0].GetProperty("TurnsPlanned").GetInt32() > 3 &&
            decisions.All(row => row.GetProperty("PlannedOutcome").GetInt32() == (int)RoomOutcome.BattleWon) &&
            decisions.Skip(1).All(row => row.GetProperty("ReusedWinningContinuation").GetBoolean() &&
                row.GetProperty("Nodes").GetInt32() == 0),
            "Native execution did not retain its full winning continuation across turns.");
        Console.WriteLine("WHOLE-BATTLE-NATIVE PASS: two background search threads, terminal forecast, zero searches for subsequent verified steps, independent native replay/state/RNG.");
    }

    internal static void Native(FixtureValue fixture)
    {
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(fixture.GetProperty(counter).GetInt32() == 0, "Unaccepted source: " + counter);
        BattleTurnState root = fixture.GetProperty("RollingDecisions")[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
        using var keys = new BattleStateKeys();
        string original = keys.Key(root);
        var options = new BattlePlannerOptions(256, 3, 8, timeLimit: TimeSpan.FromSeconds(3),
            allowShorterHorizon: true, searchToBattleEnd: true, maxSteps: 256);
        foreach (int workers in new[] { 1, 2 })
        {
            var result = ParallelBattlePlanner.Plan(root, options, workers);
            Require(result.SearchedToBattleEnd && result.CompletedHorizon && result.Outcome == RoomOutcome.BattleWon &&
                result.TurnsCompleted > 3 && result.Steps.Count <= 256 && result.ExpandedNodes <= 256 &&
                result.SearchWorkers == Math.Min(workers, Math.Max(1, Environment.ProcessorCount - 1)) &&
                result.WorkerThreadIds.Distinct().Count() == result.SearchWorkers,
                "Whole-battle search did not preserve terminal validity, independent workers or its shared budget.");
            var replay = BattlePlannerChecks.Replay(root, result.Steps, out var outcome);
            Require(keys.Key(replay) == keys.Key(result.State) && outcome == result.Outcome &&
                result.Score == BattlePlanner.Evaluate(replay, outcome), "Returned plan/state/RNG failed independent replay.");
            BattleAdviceSession.Verify(root, result);
            var metrics = result.SearchMetrics;
            Require(metrics.CandidateRequests > 0 && metrics.SimulatedCandidates > 0 &&
                metrics.CandidateRequests == metrics.SimulatedCandidates + metrics.CachedCandidates + metrics.PrefilteredCandidates &&
                metrics.EndTurnRequests == metrics.SimulatedEndTurns + metrics.CachedEndTurns,
                "Parallel transition counters lost or duplicated a worker's simulations.");
            Console.WriteLine($"WHOLE-BATTLE workers={result.SearchWorkers} threads={string.Join(',', result.WorkerThreadIds)} " +
                $"nodes={result.ExpandedNodes} elapsedMs={result.Elapsed.TotalMilliseconds:F1} turns={result.TurnsCompleted} " +
                $"steps={result.Steps.Count} score={result.Score} timeout={result.TimedOut}");
        }
        var capped = ParallelBattlePlanner.Plan(root, new BattlePlannerOptions(32, timeLimit: TimeSpan.FromSeconds(3),
            allowShorterHorizon: true, searchToBattleEnd: true, maxSteps: 1));
        Require(capped.ReachedStepLimit && capped.UsedShorterHorizon && !capped.CompletedHorizon && capped.Steps.Count == 1 &&
            capped.Steps[0].IsEndTurn && capped.Outcome == RoomOutcome.Exchanged,
            "A step cap must retain a complete partial turn and never label it a terminal forecast.");
        BattleAdviceSession.Verify(root, capped);
        var limited = ParallelBattlePlanner.Plan(root, new BattlePlannerOptions(1, timeLimit: TimeSpan.FromSeconds(3),
            allowShorterHorizon: true, searchToBattleEnd: true));
        Require(limited.ExpandedNodes == 1 && limited.ReachedNodeLimit && limited.SearchWorkers == 1 && limited.UsedShorterHorizon,
            "Parallel setup exceeded the total one-node budget.");
        var cancelled = ParallelBattlePlanner.Plan(root, options, cancellationToken: new CancellationToken(true));
        Require(cancelled.Cancelled && cancelled.ExpandedNodes == 0 && cancelled.Steps.Count == 0,
            "Pre-cancelled search did work or retained advice.");
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var interrupted = Task.Run(() => ParallelBattlePlanner.Plan(root, options, cancellationToken: cancellation.Token))
            .WaitAsync(TimeSpan.FromSeconds(15)).GetAwaiter().GetResult();
        Require(interrupted.Cancelled && interrupted.ExpandedNodes <= options.MaxNodes,
            "Parallel search ignored in-flight cancellation or exceeded its shared budget.");
        var expired = ParallelBattlePlanner.Plan(root, new BattlePlannerOptions(timeLimit: TimeSpan.FromTicks(1), searchToBattleEnd: true));
        Require(expired.TimedOut && expired.ExpandedNodes == 0 && !expired.CompletedHorizon,
            "Expired parallel work incorrectly completed a horizon.");
        Require(keys.Key(root) == original, "Parallel branches mutated their native root.");
        Console.WriteLine("PARALLEL-PLANNER PASS: whole-battle win, independent full-state/RNG replay, separate threads, shared nodes, partial step cap, time and cancellation.");
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

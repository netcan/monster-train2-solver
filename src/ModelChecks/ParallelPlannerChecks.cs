using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ParallelPlannerChecks
{
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

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace MonsterTrain2Poju.Model
{
    // A small portfolio of independent, diversified searches. No Unity APIs,
    // mutable branch caches, transposition tables or RNG instances are shared.
    public static class ParallelBattlePlanner
    {
        public static BattlePlanResult Plan(BattleTurnState source, BattlePlannerOptions options,
            int maxWorkers = 2, CancellationToken cancellationToken = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (maxWorkers < 1 || maxWorkers > 2) throw new ArgumentOutOfRangeException(nameof(maxWorkers));
            int count = Math.Min(maxWorkers, Math.Min(options.MaxNodes, Math.Max(1, Environment.ProcessorCount - 1)));
            if (cancellationToken.IsCancellationRequested) count = 1;
            var watch = Stopwatch.StartNew();
            BattleEarlyStop? earlyStop = options.StopAtBestPyreHealth ? BattleEarlyStop.Create(source, cancellationToken) : null;
            var results = new BattlePlanResult[count];
            var errors = new Exception?[count];
            int[] rooms = source.PlayRules?.Rooms.Select(room => room.RoomIndex)
                .Where(room => room != options.PreferredDeploymentRoom).OrderBy(room => room).ToArray() ?? Array.Empty<int>();
            void Run(int index)
            {
                Thread thread = Thread.CurrentThread;
                ThreadPriority priority = thread.Priority;
                try
                {
                    thread.Priority = ThreadPriority.BelowNormal;
                    TimeSpan remaining = options.TimeLimit - watch.Elapsed;
                    var branchOptions = new BattlePlannerOptions(
                        options.MaxNodes / count + (index < options.MaxNodes % count ? 1 : 0),
                        options.MaxTurns, options.BeamWidth, options.MaxActionsPerTurn,
                        remaining > TimeSpan.Zero ? remaining : TimeSpan.FromTicks(1),
                        index == 0 || rooms.Length == 0 ? options.PreferredDeploymentRoom : rooms[0],
                        index == 0 ? options.UseTacticalSeed : !options.UseTacticalSeed,
                        options.AllowShorterHorizon, options.SearchToBattleEnd, options.MaxSteps, earlyStop != null);
                    using var keys = new BattleStateKeys();
                    results[index] = BattlePlanner.PlanCore(source, keys.Key, branchOptions, null, cancellationToken, true, earlyStop);
                }
                catch (Exception error) { errors[index] = error; }
                finally { thread.Priority = priority; }
            }
            // Only the background coordinator joins. The UI session polls its
            // task, cancels stale generations and never waits for these threads.
            Thread? helper = null;
            if (count > 1)
            {
                helper = new Thread(() => Run(1)) { IsBackground = true, Name = "Poju search alternative" };
                helper.Start();
            }
            try { Run(0); }
            finally { helper?.Join(); }
            Exception[] failures = errors.Where(error => error != null).Cast<Exception>().ToArray();
            if (failures.Length > 0) throw new AggregateException("Background battle search failed.", failures);
            BattlePlanResult best = SelectBest(results);
            return new BattlePlanResult(best.State, best.Outcome, best.Steps, best.Score, best.TurnsCompleted,
                results.Sum(result => result.ExpandedNodes), results.Sum(result => result.DuplicateStates),
                results.Sum(result => result.BeamPruned), watch.Elapsed,
                results.Any(result => result.TimedOut), cancellationToken.IsCancellationRequested || results.Any(result => result.Cancelled),
                results.Any(result => result.ReachedNodeLimit), results.Any(result => result.ReachedActionLimit),
                best.CompletedHorizon, results.SelectMany(result => result.UnsupportedAttempts)
                    .GroupBy(attempt => attempt.Reason, StringComparer.Ordinal).Select(group => group.First()).ToArray(),
                best.PolicyBaselineScore, best.RequestedTurns, best.UsedShorterHorizon, best.SearchedToBattleEnd,
                results.Any(result => result.ReachedStepLimit), best.MaxSteps,
                results.SelectMany(result => result.WorkerThreadIds).Distinct().ToArray(),
                BattleSearchMetrics.Sum(results.Select(result => result.SearchMetrics)),
                results.Any(result => result.StoppedEarly), earlyStop?.PyreHealthUpperBound);
        }

        internal static BattlePlanResult SelectBest(System.Collections.Generic.IEnumerable<BattlePlanResult> results) =>
            results.OrderByDescending(result => result.CompletedHorizon || result.UsedShorterHorizon)
                // A terminal win is comparable by Pyre health. Partial whole-
                // battle forecasts must first see equally far: an earlier state
                // has not paid damage from later waves yet. Match PlanCore's
                // completed-prefix ordering without favoring a known defeat.
                .ThenBy(result => result.Outcome == RoomOutcome.BattleWon ? 0 :
                    result.Outcome is RoomOutcome.PlayerDefeated or RoomOutcome.Stalemate or RoomOutcome.Unsupported ? 2 : 1)
                .ThenByDescending(result => result.SearchedToBattleEnd &&
                    result.Outcome is not (RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated or RoomOutcome.Stalemate or RoomOutcome.Unsupported)
                        ? result.TurnsCompleted : 0)
                .ThenBy(result => result.Score).ThenBy(result => BattlePlanner.PositionCost(result.State, result.Outcome))
                .ThenByDescending(result => result.TurnsCompleted).ThenBy(result => result.Steps.Count).First();
    }
}

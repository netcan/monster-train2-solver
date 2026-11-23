using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace MonsterTrain2Poju.Model
{
    public sealed class ExactBattleOptions
    {
        public int MaxNodes { get; }
        public TimeSpan TimeLimit { get; }
        public int? TurnHorizon { get; }
        public ExactBattleOptions(int maxNodes = 100000, TimeSpan? timeLimit = null, int? turnHorizon = null)
        {
            if (maxNodes < 1) throw new ArgumentOutOfRangeException(nameof(maxNodes));
            if (turnHorizon < 0) throw new ArgumentOutOfRangeException(nameof(turnHorizon));
            TimeLimit = timeLimit ?? TimeSpan.FromSeconds(30);
            if (TimeLimit < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeLimit));
            MaxNodes = maxNodes; TurnHorizon = turnHorizon;
        }
    }

    // All certificates refer to the enumerated, supported deterministic model,
    // with the captured deck/RNG. They do not certify native action-space coverage.
    public sealed class ExactBattleResult
    {
        public BattleTurnState State { get; }
        public RoomOutcome Outcome { get; }
        public double Score { get; }
        public IReadOnlyList<BattleSearchStep> Steps { get; }
        public int ExpandedNodes { get; }
        public int DuplicateStates { get; }
        public int TerminalLeaves { get; }
        public int HorizonLeaves { get; }
        public bool ReachedNodeLimit { get; }
        public bool TimedOut { get; }
        public bool Cancelled { get; }
        public TimeSpan Elapsed { get; }
        internal BattleSearchMetrics SearchMetrics { get; }
        internal bool ReachedActionLimit { get; }
        internal bool ReachedStepLimit { get; }
        internal bool StoppedEarly { get; }
        public IReadOnlyList<UnsupportedActionAttempt> UnsupportedAttempts { get; }
        public bool ExhaustedEnumeratedFrontier => !ReachedNodeLimit && !ReachedActionLimit && !ReachedStepLimit &&
            !TimedOut && !Cancelled && !StoppedEarly && UnsupportedAttempts.Count == 0;
        public bool CertifiedTerminalOptimal => ExhaustedEnumeratedFrontier && TerminalLeaves > 0 && HorizonLeaves == 0;
        public bool CertifiedHorizonOptimal => ExhaustedEnumeratedFrontier && HorizonLeaves > 0;
        internal ExactBattleResult(BattleTurnState state, RoomOutcome outcome, double score, IReadOnlyList<BattleSearchStep> steps,
            int nodes, int duplicates, int terminals, int horizons, bool limited, bool timedOut, bool cancelled,
            TimeSpan elapsed, IReadOnlyList<UnsupportedActionAttempt> unsupported, BattleSearchMetrics searchMetrics,
            bool actionLimit, bool stepLimit, bool stoppedEarly)
        {
            State = state; Outcome = outcome; Score = score; Steps = Array.AsReadOnly(steps.ToArray());
            ExpandedNodes = nodes; DuplicateStates = duplicates; TerminalLeaves = terminals; HorizonLeaves = horizons;
            ReachedNodeLimit = limited; TimedOut = timedOut; Cancelled = cancelled; Elapsed = elapsed;
            UnsupportedAttempts = Array.AsReadOnly(unsupported.ToArray());
            SearchMetrics = searchMetrics;
            ReachedActionLimit = actionLimit; ReachedStepLimit = stepLimit;
            StoppedEarly = stoppedEarly;
        }
    }

    public static class ExactBattleSolver
    {
        // Iterative exhaustive traversal: no beam, per-turn action cap, heuristic
        // pruning, or per-child budget allocation. Full-state transpositions are
        // scoped by the remaining horizon. The evaluator must depend only on
        // state/outcome, not the path taken to reach it.
        public static ExactBattleResult Minimize(BattleTurnState source,
            Func<BattleTurnState, RoomOutcome, double>? evaluate = null, ExactBattleOptions? options = null,
            CancellationToken cancellationToken = default)
            => MinimizeCore(source, evaluate, options, cancellationToken);

        internal static ExactBattleResult MinimizeCore(BattleTurnState source,
            Func<BattleTurnState, RoomOutcome, double>? evaluate, ExactBattleOptions? options,
            CancellationToken cancellationToken, Func<bool>? deadlineReached = null,
            int? maxSteps = null, int? maxActionsPerTurn = null, BattleEarlyStop? earlyStop = null,
            bool preferPositionOnTie = false)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            evaluate ??= BattlePlanner.Evaluate; options ??= new ExactBattleOptions();
            var watch = Stopwatch.StartNew();
            deadlineReached ??= () => watch.Elapsed >= options.TimeLimit;
            using var keys = new BattleStateKeys();
            var workspace = new BattleSearchWorkspace(enabled: maxSteps.HasValue || maxActionsPerTurn.HasValue);
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var pending = new Stack<Node>();
            var root = new Node(source, RoomOutcome.Exchanged, null, null, 0);
            pending.Push(root); Node? best = null; Node fallback = root;
            double score = Value(root); int nodes = 0, duplicates = 0, terminals = 0, horizons = 0;
            bool cancelled = false, timedOut = false, limited = false;
            bool actionLimit = false, stepLimit = false, stoppedEarly = false;
            var unsupported = new List<UnsupportedActionAttempt>();
            while (pending.Count > 0)
            {
                cancelled = cancellationToken.IsCancellationRequested;
                timedOut = deadlineReached();
                stoppedEarly = earlyStop?.Reached == true;
                if (cancelled || timedOut || stoppedEarly) break;
                Node node = pending.Pop();
                if (ConsiderLeaf(node)) continue;
                if (node.Depth >= maxSteps) { stepLimit = true; continue; }
                string key = keys.Key(node.State) + (options.TurnHorizon.HasValue ? "/" + node.Turns : "");
                if (maxSteps.HasValue) key += "/depth=" + node.Depth;
                if (maxActionsPerTurn.HasValue) key += "/actions=" + node.Actions;
                if (!visited.Add(key)) { duplicates++; continue; }
                if (nodes >= options.MaxNodes) { limited = true; break; }
                nodes++;
                if (node.Turns > fallback.Turns) fallback = node;
                // Push the complete action frontier, then visit EndTurn first to
                // obtain a terminal witness early. Enumeration order breaks ties.
                if (node.Actions >= maxActionsPerTurn) actionLimit = true;
                else
                {
                    var frontier = BattleActionModel.EnumerateActions(node.State, null, true, workspace);
                    unsupported.AddRange(frontier.UnsupportedAttempts);
                    foreach (var candidate in frontier.SupportedCandidates.Reverse())
                        pending.Push(new Node(candidate.Result.State!, candidate.Result.Outcome, node,
                            new BattleSearchStep(candidate.Action, false), node.Turns));
                }
                BattleEndTurnFrontier ends = BattleEndTurnModel.Enumerate(node.State, Math.Max(0, options.MaxNodes - nodes),
                    () => cancellationToken.IsCancellationRequested || deadlineReached() || earlyStop?.Reached == true,
                    choices => workspace.EndTurn(node.State, choices));
                nodes += ends.ChoiceNodes; limited |= ends.ReachedNodeLimit;
                cancelled |= cancellationToken.IsCancellationRequested;
                timedOut |= deadlineReached();
                unsupported.AddRange(ends.UnsupportedAttempts);
                foreach (BattleEndTurnCandidate end in ends.Candidates.Reverse())
                    pending.Push(new Node(end.Result.State!, end.Result.Outcome, node, end.Step, node.Turns + 1));
            }
            // A cutoff prevents further expansions, but the frontier already
            // contains fully simulated successors. Keep finished leaves in stack order.
            cancelled |= cancellationToken.IsCancellationRequested;
            stoppedEarly |= earlyStop?.Reached == true;
            if (!cancelled && (timedOut || limited || stoppedEarly))
                foreach (Node completed in pending)
                {
                    if (cancellationToken.IsCancellationRequested) { cancelled = true; break; }
                    if (completed.Step != null) ConsiderLeaf(completed);
                }
            watch.Stop();
            Node chosen = best ?? fallback;
            var steps = new List<BattleSearchStep>();
            for (Node? node = chosen; node?.Step != null; node = node.Parent) steps.Add(node.Step);
            steps.Reverse();
            return new ExactBattleResult(chosen.State, chosen.Outcome, Value(chosen), steps, nodes, duplicates,
                terminals, horizons, limited, timedOut, cancelled, watch.Elapsed, unsupported, workspace.Metrics,
                actionLimit, stepLimit, stoppedEarly);

            bool ConsiderLeaf(Node node)
            {
                if (Terminal(node.Outcome))
                {
                    terminals++;
                    earlyStop?.Consider(node.State, node.Outcome);
                }
                else if (options.TurnHorizon.HasValue && node.Turns >= options.TurnHorizon.Value) horizons++;
                else return false;
                double value = Value(node);
                if (best == null || value < score || preferPositionOnTie && value == score &&
                    BattlePlanner.PositionCost(node.State, node.Outcome) < BattlePlanner.PositionCost(best.State, best.Outcome))
                { best = node; score = value; }
                return true;
            }
            double Value(Node node)
            {
                double value = evaluate(node.State, node.Outcome);
                if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidOperationException("Exact solver scores must be finite.");
                return value;
            }
        }
        private static bool Terminal(RoomOutcome outcome) => outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated or RoomOutcome.Stalemate;
        private sealed class Node
        {
            internal readonly BattleTurnState State;
            internal readonly RoomOutcome Outcome;
            internal readonly Node? Parent;
            internal readonly BattleSearchStep? Step;
            internal readonly int Turns;
            internal readonly int Depth;
            internal readonly int Actions;
            internal Node(BattleTurnState state, RoomOutcome outcome, Node? parent, BattleSearchStep? step, int turns)
            {
                State = state; Outcome = outcome; Parent = parent; Step = step; Turns = turns;
                Depth = parent == null ? 0 : parent.Depth + 1;
                Actions = parent == null || step!.IsEndTurn ? 0 : parent.Actions + 1;
            }
        }
    }
}

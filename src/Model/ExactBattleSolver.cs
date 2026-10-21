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
        public IReadOnlyList<UnsupportedActionAttempt> UnsupportedAttempts { get; }
        public bool ExhaustedEnumeratedFrontier => !ReachedNodeLimit && !TimedOut && !Cancelled && UnsupportedAttempts.Count == 0;
        public bool CertifiedTerminalOptimal => ExhaustedEnumeratedFrontier && TerminalLeaves > 0 && HorizonLeaves == 0;
        public bool CertifiedHorizonOptimal => ExhaustedEnumeratedFrontier && HorizonLeaves > 0;
        internal ExactBattleResult(BattleTurnState state, RoomOutcome outcome, double score, IReadOnlyList<BattleSearchStep> steps,
            int nodes, int duplicates, int terminals, int horizons, bool limited, bool timedOut, bool cancelled,
            TimeSpan elapsed, IReadOnlyList<UnsupportedActionAttempt> unsupported)
        {
            State = state; Outcome = outcome; Score = score; Steps = Array.AsReadOnly(steps.ToArray());
            ExpandedNodes = nodes; DuplicateStates = duplicates; TerminalLeaves = terminals; HorizonLeaves = horizons;
            ReachedNodeLimit = limited; TimedOut = timedOut; Cancelled = cancelled; Elapsed = elapsed;
            UnsupportedAttempts = Array.AsReadOnly(unsupported.ToArray());
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
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            evaluate ??= BattlePlanner.Evaluate; options ??= new ExactBattleOptions();
            var watch = Stopwatch.StartNew();
            using var keys = new BattleStateKeys();
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var pending = new Stack<Node>();
            var root = new Node(source, RoomOutcome.Exchanged, null, null, 0);
            pending.Push(root); Node? best = null; Node fallback = root;
            double score = Value(root); int nodes = 0, duplicates = 0, terminals = 0, horizons = 0;
            bool cancelled = false, timedOut = false, limited = false;
            var unsupported = new List<UnsupportedActionAttempt>();
            while (pending.Count > 0)
            {
                cancelled = cancellationToken.IsCancellationRequested;
                timedOut = watch.Elapsed >= options.TimeLimit;
                if (cancelled || timedOut) break;
                Node node = pending.Pop();
                if (Terminal(node.Outcome) || options.TurnHorizon.HasValue && node.Turns >= options.TurnHorizon.Value)
                {
                    if (Terminal(node.Outcome)) terminals++; else horizons++;
                    double value = Value(node);
                    if (best == null || value < score) { best = node; score = value; }
                    continue;
                }
                string key = keys.Key(node.State) + (options.TurnHorizon.HasValue ? "/" + node.Turns : "");
                if (!visited.Add(key)) { duplicates++; continue; }
                if (nodes >= options.MaxNodes) { limited = true; break; }
                nodes++;
                if (node.Turns > fallback.Turns) fallback = node;
                var frontier = BattleActionModel.EnumerateActions(node.State);
                unsupported.AddRange(frontier.UnsupportedAttempts);
                // Push the complete action frontier, then visit EndTurn first to
                // obtain a terminal witness early. Enumeration order breaks ties.
                foreach (var candidate in frontier.SupportedCandidates.Reverse())
                    pending.Push(new Node(candidate.Result.State!, candidate.Result.Outcome, node,
                        new BattleSearchStep(candidate.Action, false), node.Turns));
                var ended = BattleTurnModel.EndTurn(node.State);
                if (ended.Supported)
                    pending.Push(new Node(ended.State!, ended.Outcome, node, new BattleSearchStep(null, true), node.Turns + 1));
                else unsupported.Add(new UnsupportedActionAttempt(null, ended.UnsupportedReason ?? "Unsupported EndTurn."));
            }
            watch.Stop();
            Node chosen = best ?? fallback;
            var steps = new List<BattleSearchStep>();
            for (Node? node = chosen; node?.Step != null; node = node.Parent) steps.Add(node.Step);
            steps.Reverse();
            return new ExactBattleResult(chosen.State, chosen.Outcome, Value(chosen), steps, nodes, duplicates,
                terminals, horizons, limited, timedOut, cancelled, watch.Elapsed, unsupported);
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
            internal Node(BattleTurnState state, RoomOutcome outcome, Node? parent, BattleSearchStep? step, int turns)
            { State = state; Outcome = outcome; Parent = parent; Step = step; Turns = turns; }
        }
    }
}

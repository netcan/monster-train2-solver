using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace MonsterTrain2Poju.Model
{
    public sealed class BattlePlannerOptions
    {
        public int MaxNodes { get; }
        public int MaxTurns { get; }
        public int BeamWidth { get; }
        public int MaxActionsPerTurn { get; }
        public TimeSpan TimeLimit { get; }

        public BattlePlannerOptions(int maxNodes = 2048, int maxTurns = 3, int beamWidth = 16,
            int maxActionsPerTurn = 12, TimeSpan? timeLimit = null)
        {
            if (maxNodes < 1 || maxTurns < 1 || beamWidth < 1 || maxActionsPerTurn < 1)
                throw new ArgumentOutOfRangeException(nameof(maxNodes));
            TimeLimit = timeLimit ?? TimeSpan.FromSeconds(2);
            if (TimeLimit <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeLimit));
            MaxNodes = maxNodes; MaxTurns = maxTurns; BeamWidth = beamWidth; MaxActionsPerTurn = maxActionsPerTurn;
        }
    }

    // A bounded recommendation, never a proof of global optimality. The key must
    // include the complete branch-owned state, including histories and every RNG.
    public sealed class BattlePlanResult
    {
        public BattleTurnState State { get; }
        public RoomOutcome Outcome { get; }
        public IReadOnlyList<BattleSearchStep> Steps { get; }
        public double Score { get; }
        public int TurnsCompleted { get; }
        public int ExpandedNodes { get; }
        public int DuplicateStates { get; }
        public int BeamPruned { get; }
        public TimeSpan Elapsed { get; }
        public bool TimedOut { get; }
        public bool Cancelled { get; }
        public bool ReachedNodeLimit { get; }
        public bool ReachedActionLimit { get; }
        public bool CompletedHorizon { get; }
        public double? PolicyBaselineScore { get; }
        public IReadOnlyList<UnsupportedActionAttempt> UnsupportedAttempts { get; }

        internal BattlePlanResult(BattleTurnState state, RoomOutcome outcome, IReadOnlyList<BattleSearchStep> steps,
            double score, int turns, int nodes, int duplicates, int beamPruned, TimeSpan elapsed,
            bool timedOut, bool cancelled, bool nodeLimit, bool actionLimit, bool completed,
            IReadOnlyList<UnsupportedActionAttempt> unsupported, double? policyBaselineScore)
        {
            State = state; Outcome = outcome; Steps = Array.AsReadOnly(steps.ToArray()); Score = score;
            TurnsCompleted = turns; ExpandedNodes = nodes; DuplicateStates = duplicates; BeamPruned = beamPruned;
            Elapsed = elapsed; TimedOut = timedOut; Cancelled = cancelled; ReachedNodeLimit = nodeLimit;
            ReachedActionLimit = actionLimit; CompletedHorizon = completed;
            UnsupportedAttempts = Array.AsReadOnly(unsupported.ToArray());
            PolicyBaselineScore = policyBaselineScore;
        }
    }

    public static class BattlePlanner
    {
        // Win first, then surviving Pyre health. The small positional term orders
        // nonterminal branches; it does not promise future combat outcomes.
        public static double Evaluate(BattleTurnState state, RoomOutcome outcome)
        {
            CombatUnit[] units = state.Spawn.Train.Rooms.SelectMany(room => room.Units).ToArray();
            int hp = units.Where(unit => unit.IsPyre).Select(unit => unit.Health).DefaultIfEmpty(0).First();
            if (outcome == RoomOutcome.BattleWon) return -1e12 - hp;
            if (outcome == RoomOutcome.PlayerDefeated) return 1e12;
            if (outcome == RoomOutcome.Stalemate) return 5e11;
            double enemy = units.Where(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0).Sum(unit => (double)unit.Health);
            double allies = units.Where(unit => unit.Team == CombatTeam.Player && !unit.IsPyre && unit.Health > 0)
                .Sum(unit => Math.Max(0d, unit.Attack) + Math.Max(0d, unit.Health) * .1);
            // Bound the ordering term so a single Pyre HP always dominates it.
            return -hp * 1000d + Math.Max(-499d, Math.Min(499d, enemy - allies));
        }

        public static BattlePlanResult Plan(BattleTurnState source, Func<BattleTurnState, string> stateKey,
            BattlePlannerOptions? options = null, Func<BattleTurnState, RoomOutcome, double>? evaluate = null,
            CancellationToken cancellationToken = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (stateKey == null) throw new ArgumentNullException(nameof(stateKey));
            options ??= new BattlePlannerOptions(); evaluate ??= Evaluate;
            var watch = Stopwatch.StartNew();
            int nodes = 0, duplicates = 0, pruned = 0;
            bool timedOut = false, cancelled = false, nodeLimit = false, actionLimit = false;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var unsupported = new List<UnsupportedActionAttempt>();
            var unsupportedKeys = new HashSet<string>(StringComparer.Ordinal);
            var root = new Node(source, RoomOutcome.Exchanged, Array.Empty<BattleSearchStep>(), 0, 0, Score(source, RoomOutcome.Exchanged));
            Node fallback = root;
            Node? best = null;
            double? policyBaselineScore = null;

            bool Stop()
            {
                cancelled |= cancellationToken.IsCancellationRequested;
                timedOut |= watch.Elapsed >= options.TimeLimit;
                nodeLimit |= nodes >= options.MaxNodes;
                return cancelled || timedOut || nodeLimit;
            }
            double Score(BattleTurnState state, RoomOutcome outcome)
            {
                double value = evaluate(state, outcome);
                if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidOperationException("Planner scores must be finite.");
                return value;
            }
            void Unsupported(UnsupportedActionAttempt attempt)
            {
                // Keep every reason visible; repeated identical diagnostics need
                // not grow with every branch in a bounded planning run.
                string key = attempt.Reason ?? "";
                if (unsupportedKeys.Add(key)) unsupported.Add(attempt);
            }
            bool Terminal(Node node) => node.Outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated or RoomOutcome.Stalemate;
            void Consider(Node node)
            {
                if (node.Turns > fallback.Turns || node.Turns == fallback.Turns && node.Score < fallback.Score) fallback = node;
                if ((Terminal(node) || node.Turns >= options.MaxTurns) && (best == null || node.Score < best.Score)) best = node;
            }
            Node Child(Node parent, BattleTurnState state, RoomOutcome outcome, BattleSearchStep step)
            {
                var steps = parent.Steps.Concat(new[] { step }).ToArray();
                return new Node(state, outcome, steps, parent.Turns + (step.IsEndTurn ? 1 : 0),
                    step.IsEndTurn ? 0 : parent.Actions + 1, Score(state, outcome));
            }

            // Establish an executable horizon fallback before spending the budget
            // on alternatives. This is charged to the same node/time budget.
            void Rollout(Node baseline)
            {
                while (!Stop() && baseline.Turns < options.MaxTurns && !Terminal(baseline))
                {
                    nodes++;
                    BattleTurnResult ended = BattleTurnModel.EndTurn(baseline.State);
                    if (!ended.Supported) { Unsupported(new UnsupportedActionAttempt(null, ended.UnsupportedReason ?? "Unsupported EndTurn.")); break; }
                    baseline = Child(baseline, ended.State!, ended.Outcome, new BattleSearchStep(null, true));
                    Consider(baseline);
                }
            }
            Rollout(root);

            // Seed the existing fixed play policy as well as the no-card path.
            // Reuse its cached successors rather than applying a selected card
            // twice. Charge all work to the same budgets, retaining partial
            // rollout improvements if the complete seed cannot finish in time.
            Node policy = root;
            while (!Stop() && policy.Turns < options.MaxTurns && !Terminal(policy))
            {
                nodes++;
                SupportedActionCandidate? chosen = null;
                if (policy.Actions < options.MaxActionsPerTurn)
                {
                    chosen = BattleActionModel.ChooseUnitSpellAndJunkCandidate(policy.State, out IReadOnlyList<UnsupportedActionAttempt> omitted);
                    foreach (UnsupportedActionAttempt attempt in omitted) Unsupported(attempt);
                }
                else actionLimit = true;
                if (chosen != null)
                {
                    policy = Child(policy, chosen.Result.State!, chosen.Result.Outcome, new BattleSearchStep(chosen.Action, false));
                    Consider(policy); Rollout(policy);
                }
                else
                {
                    BattleTurnResult ended = BattleTurnModel.EndTurn(policy.State);
                    if (!ended.Supported) { Unsupported(new UnsupportedActionAttempt(null, ended.UnsupportedReason ?? "Unsupported policy EndTurn.")); break; }
                    policy = Child(policy, ended.State!, ended.Outcome, new BattleSearchStep(null, true));
                    Consider(policy);
                }
            }
            if (policy.Turns >= options.MaxTurns || Terminal(policy)) policyBaselineScore = policy.Score;

            var frontier = new List<Node> { root };
            if (!Stop()) seen.Add("0|" + stateKey(source));
            while (frontier.Count > 0 && !Stop())
            {
                var next = new List<Node>();
                foreach (Node node in frontier)
                {
                    if (Stop()) break;
                    nodes++;
                    if (node.Actions < options.MaxActionsPerTurn)
                    {
                        BattleActionEnumeration actions = BattleActionModel.EnumerateActions(node.State);
                        foreach (UnsupportedActionAttempt attempt in actions.UnsupportedAttempts) Unsupported(attempt);
                        var children = new List<Node>();
                        foreach (SupportedActionCandidate candidate in actions.SupportedCandidates)
                        {
                            if (watch.Elapsed >= options.TimeLimit || cancellationToken.IsCancellationRequested) break;
                            Node child = Child(node, candidate.Result.State!, candidate.Result.Outcome, new BattleSearchStep(candidate.Action, false));
                            children.Add(child); Add(child);
                        }
                        // Short deterministic continuations turn promising partial
                        // branches into executable horizon recommendations early.
                        foreach (Node child in children.OrderBy(child => child.Score).Take(2)) Rollout(child);
                    }
                    else actionLimit = true;
                    if (watch.Elapsed >= options.TimeLimit || cancellationToken.IsCancellationRequested) break;
                    BattleTurnResult ended = BattleTurnModel.EndTurn(node.State);
                    if (ended.Supported) Add(Child(node, ended.State!, ended.Outcome, new BattleSearchStep(null, true)));
                    else Unsupported(new UnsupportedActionAttempt(null, ended.UnsupportedReason ?? "Unsupported EndTurn."));
                }
                next = next.OrderBy(node => node.Score).ThenByDescending(node => node.Turns).ThenBy(node => node.Steps.Count).ToList();
                pruned += Math.Max(0, next.Count - options.BeamWidth);
                frontier = next.Take(options.BeamWidth).ToList();

                void Add(Node child)
                {
                    Consider(child);
                    if (Terminal(child) || child.Turns >= options.MaxTurns) return;
                    string key = child.Turns + "|" + stateKey(child.State);
                    if (!seen.Add(key)) { duplicates++; return; }
                    next.Add(child);
                }
            }
            // Cancellation/time can occur inside one atomic model expansion.
            cancelled |= cancellationToken.IsCancellationRequested;
            timedOut |= watch.Elapsed >= options.TimeLimit;
            Node selected = best ?? fallback;
            return new BattlePlanResult(selected.State, selected.Outcome, selected.Steps, selected.Score, selected.Turns,
                nodes, duplicates, pruned, watch.Elapsed, timedOut, cancelled, nodeLimit, actionLimit, best != null, unsupported, policyBaselineScore);
        }

        private sealed class Node
        {
            public BattleTurnState State { get; }
            public RoomOutcome Outcome { get; }
            public IReadOnlyList<BattleSearchStep> Steps { get; }
            public int Turns { get; }
            public int Actions { get; }
            public double Score { get; }
            public Node(BattleTurnState state, RoomOutcome outcome, IReadOnlyList<BattleSearchStep> steps, int turns, int actions, double score)
            { State = state; Outcome = outcome; Steps = steps; Turns = turns; Actions = actions; Score = score; }
        }
    }
}

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
            bool useAdvisorOrdering = false)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            evaluate ??= BattlePlanner.Evaluate; options ??= new ExactBattleOptions();
            var watch = Stopwatch.StartNew();
            deadlineReached ??= () => watch.Elapsed >= options.TimeLimit;
            using var keys = new BattleStateKeys();
            var workspace = new BattleSearchWorkspace(enabled: maxSteps.HasValue || maxActionsPerTurn.HasValue);
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var pending = new Stack<(Node Node, Expansion Work)>();
            var root = new Node(source, RoomOutcome.Exchanged, null, null, 0);
            pending.Push((root, Expansion.Full)); Node? best = null; Node fallback = root;
            double score = Value(root); int nodes = 0, duplicates = 0, terminals = 0, horizons = 0;
            bool cancelled = false, timedOut = false, limited = false;
            bool actionLimit = false, stepLimit = false, stoppedEarly = false;
            var unsupported = new List<UnsupportedActionAttempt>();
            Func<bool>? stopActions = useAdvisorOrdering ? CheckStop : null;
            bool openingDefense = useAdvisorOrdering;
            while (pending.Count > 0)
            {
                if (CheckStop()) break;
                var (node, work) = pending.Pop();
                if (work == Expansion.EndTurn) { ExpandEndTurns(node); continue; }
                if (work != Expansion.Full) openingDefense = false;
                else
                {
                    if (ConsiderLeaf(node)) { openingDefense = false; continue; }
                    if (node.Depth >= maxSteps) { stepLimit = true; continue; }
                    string key = keys.Key(node.State) + (options.TurnHorizon.HasValue ? "/" + node.Turns : "");
                    if (maxSteps.HasValue) key += "/depth=" + node.Depth;
                    if (maxActionsPerTurn.HasValue) key += "/actions=" + node.Actions;
                    if (!visited.Add(key)) { duplicates++; continue; }
                    if (nodes >= options.MaxNodes) { limited = true; break; }
                    nodes++;
                    if (node.Turns > fallback.Turns) fallback = node;
                    if (openingDefense && nodes <= 6 && node.Actions < maxActionsPerTurn)
                    {
                        // Explore one short route first. Leave its full frontier
                        // in the stack so it is still available within the budget.
                        Node? protectedChild = FirstProtectingHeal(node);
                        if (CheckStop())
                        {
                            if (protectedChild != null) pending.Push((protectedChild, Expansion.Full));
                            break;
                        }
                        pending.Push((node, protectedChild == null ? Expansion.Actions : Expansion.ActionsAndEndTurn));
                        if (protectedChild == null) ExpandEndTurns(node);
                        else pending.Push((protectedChild, Expansion.Full));
                        continue;
                    }
                    openingDefense = false;
                }
                // Push the complete action frontier, then visit EndTurn first to
                // obtain a terminal witness early. Enumeration order breaks ties.
                int beforeActions = pending.Count;
                if (node.Actions >= maxActionsPerTurn) actionLimit = true;
                else
                {
                    var frontier = BattleActionModel.EnumerateActions(node.State, null, true, workspace, stopActions);
                    unsupported.AddRange(frontier.UnsupportedAttempts);
                    IEnumerable<SupportedActionCandidate> candidates = useAdvisorOrdering ? frontier.SupportedCandidates
                        .OrderByDescending(candidate => FrontlineProtection(candidate.Result.State!)) : frontier.SupportedCandidates;
                    foreach (var candidate in candidates.Reverse())
                        pending.Push((new Node(candidate.Result.State!, candidate.Result.Outcome, node,
                            new BattleSearchStep(candidate.Action, false), node.Turns), Expansion.Full));
                    if (frontier.Interrupted) break;
                }
                if (work == Expansion.Actions) continue;
                if (useAdvisorOrdering && best?.Outcome == RoomOutcome.BattleWon && pending.Count > beforeActions &&
                    FrontlineProtection(pending.Peek().Node.State) > FrontlineProtection(node.State))
                {
                    // Preserve a terminal witness, then try the improved defense
                    // before simulating the previous position's EndTurn.
                    var protectedChild = pending.Pop();
                    pending.Push((node, Expansion.EndTurn));
                    pending.Push(protectedChild);
                }
                else ExpandEndTurns(node);
            }
            // A cutoff prevents further expansions, but the frontier already
            // contains fully simulated successors. Keep finished leaves in stack order.
            cancelled |= cancellationToken.IsCancellationRequested;
            stoppedEarly |= earlyStop?.Reached == true;
            if (!cancelled && (timedOut || limited || stoppedEarly))
                foreach (var (completed, work) in pending)
                {
                    if (cancellationToken.IsCancellationRequested) { cancelled = true; break; }
                    if (work == Expansion.Full && completed.Step != null) ConsiderLeaf(completed);
                }
            watch.Stop();
            Node chosen = best ?? fallback;
            var steps = new List<BattleSearchStep>();
            for (Node? node = chosen; node?.Step != null; node = node.Parent) steps.Add(node.Step);
            steps.Reverse();
            return new ExactBattleResult(chosen.State, chosen.Outcome, Value(chosen), steps, nodes, duplicates,
                terminals, horizons, limited, timedOut, cancelled, watch.Elapsed, unsupported, workspace.Metrics,
                actionLimit, stepLimit, stoppedEarly);

            bool CheckStop()
            {
                cancelled |= cancellationToken.IsCancellationRequested;
                timedOut |= deadlineReached();
                stoppedEarly |= earlyStop?.Reached == true;
                return cancelled || timedOut || stoppedEarly;
            }

            Node? FirstProtectingHeal(Node node)
            {
                if (node.State.PlayRules == null || node.State.Spawn.Train.Context == null) return null;
                double current = FrontlineProtection(node.State);
                int attempts = 0;
                foreach (var (room, ally, enemy) in ContestedFronts(node.State))
                {
                    if (EffectiveHealth(ally) > enemy.Attack) continue;
                    foreach (var card in node.State.Spawn.Train.Context.Cards.Hand)
                    {
                        var rule = node.State.PlayRules.Cards.FirstOrDefault(item => item.DataId == card.DataId);
                        if (rule?.Effect != "Spell" || !rule.Effects.Any(effect => effect is
                                { Type: "Heal", Target: "DropTargetCharacter", AllowPlayer: true, Value: > 0 })) continue;
                        var targeted = new PlayCardAction(card.InstanceId, room.RoomIndex, targetUnitId: ally.Id);
                        foreach (var action in BattleActionModel.EnumerateRecursionChoices(node.State, targeted, rule))
                        {
                            if (attempts++ >= 3 || CheckStop()) return null;
                            BattleActionResult play = workspace.Play(node.State, action);
                            if (play.Supported && (Terminal(play.Outcome) || FrontlineProtection(play.State!) > current))
                                return new Node(play.State!, play.Outcome, node, new BattleSearchStep(action, false), node.Turns);
                            if (play.Rejection == ActionRejection.Unsupported)
                                unsupported.Add(new UnsupportedActionAttempt(action, play.Reason ?? "Unsupported card action."));
                        }
                    }
                }
                return null;
            }

            void ExpandEndTurns(Node node)
            {
                if (useAdvisorOrdering && CheckStop()) return;
                BattleEndTurnFrontier ends = BattleEndTurnModel.Enumerate(node.State, Math.Max(0, options.MaxNodes - nodes),
                    () => cancellationToken.IsCancellationRequested || deadlineReached() || earlyStop?.Reached == true,
                    choices => workspace.EndTurn(node.State, choices));
                nodes += ends.ChoiceNodes; limited |= ends.ReachedNodeLimit;
                cancelled |= cancellationToken.IsCancellationRequested;
                timedOut |= deadlineReached();
                unsupported.AddRange(ends.UnsupportedAttempts);
                foreach (BattleEndTurnCandidate end in ends.Candidates.Reverse())
                    pending.Push((new Node(end.Result.State!, end.Result.Outcome, node, end.Step, node.Turns + 1), Expansion.Full));
            }

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
                if (best == null || value < score || useAdvisorOrdering && value == score &&
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

        // This only orders the bounded worker's choices. The actual combat
        // transition still decides whether protection prevents Pyre damage.
        private static double FrontlineProtection(BattleTurnState state) =>
            ContestedFronts(state).Sum(front => Math.Min(front.Enemy.Attack + 1d, EffectiveHealth(front.Ally)));

        private static double EffectiveHealth(CombatUnit unit) =>
            unit.Health + (double)(unit.Status("armor")?.Stacks ?? 0);

        private static IEnumerable<(RoomCombatState Room, CombatUnit Ally, CombatUnit Enemy)> ContestedFronts(BattleTurnState state)
        {
            foreach (RoomCombatState room in state.Spawn.Train.Rooms)
            {
                CombatUnit? ally = room.Units.FirstOrDefault(unit => unit.Team == CombatTeam.Player && !unit.IsPyre && unit.Health > 0);
                CombatUnit? enemy = room.Units.FirstOrDefault(unit => unit.Team == CombatTeam.Enemy && unit.CanAttack && unit.Health > 0);
                if (ally != null && enemy?.Attack > 0) yield return (room, ally, enemy);
            }
        }

        private enum Expansion { Full, EndTurn, Actions, ActionsAndEndTurn }

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

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
        public int PreferredDeploymentRoom { get; }
        public bool UseTacticalSeed { get; }
        public bool AllowShorterHorizon { get; }
        public bool SearchToBattleEnd { get; }
        public int MaxSteps { get; }
        public TimeSpan TimeLimit { get; }
        public bool StopAtBestPyreHealth { get; }

        public BattlePlannerOptions(int maxNodes = 2048, int maxTurns = 3, int beamWidth = 16,
            int maxActionsPerTurn = 12, TimeSpan? timeLimit = null, int preferredDeploymentRoom = 2, bool useTacticalSeed = false,
            bool allowShorterHorizon = false, bool searchToBattleEnd = false, int maxSteps = 256, bool stopAtBestPyreHealth = false)
        {
            if (maxNodes < 1 || maxTurns < 1 || beamWidth < 1 || maxActionsPerTurn < 1 || preferredDeploymentRoom < 0 || maxSteps < 1 || maxSteps == int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(maxNodes));
            TimeLimit = timeLimit ?? TimeSpan.FromSeconds(2);
            if (TimeLimit <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeLimit));
            MaxNodes = maxNodes; MaxTurns = maxTurns; BeamWidth = beamWidth; MaxActionsPerTurn = maxActionsPerTurn;
            PreferredDeploymentRoom = preferredDeploymentRoom;
            UseTacticalSeed = useTacticalSeed;
            AllowShorterHorizon = allowShorterHorizon;
            SearchToBattleEnd = searchToBattleEnd; MaxSteps = maxSteps;
            StopAtBestPyreHealth = stopAtBestPyreHealth;
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
        public int RequestedTurns { get; }
        public bool UsedShorterHorizon { get; }
        public bool SearchedToBattleEnd { get; }
        public bool ReachedStepLimit { get; }
        public int MaxSteps { get; }
        public IReadOnlyList<int> WorkerThreadIds { get; }
        public int SearchWorkers => WorkerThreadIds.Count;
        public double? PolicyBaselineScore { get; }
        public IReadOnlyList<UnsupportedActionAttempt> UnsupportedAttempts { get; }
        public BattleSearchMetrics SearchMetrics { get; }
        public bool StoppedEarly { get; }
        public int? PyreHealthUpperBound { get; }

        internal BattlePlanResult(BattleTurnState state, RoomOutcome outcome, IReadOnlyList<BattleSearchStep> steps,
            double score, int turns, int nodes, int duplicates, int beamPruned, TimeSpan elapsed,
            bool timedOut, bool cancelled, bool nodeLimit, bool actionLimit, bool completed,
            IReadOnlyList<UnsupportedActionAttempt> unsupported, double? policyBaselineScore,
            int requestedTurns = 0, bool usedShorterHorizon = false, bool searchedToBattleEnd = false,
            bool stepLimit = false, int maxSteps = 256, IReadOnlyList<int>? workerThreadIds = null,
            BattleSearchMetrics? searchMetrics = null, bool stoppedEarly = false, int? pyreHealthUpperBound = null)
        {
            State = state; Outcome = outcome; Steps = Array.AsReadOnly(steps.ToArray()); Score = score;
            TurnsCompleted = turns; ExpandedNodes = nodes; DuplicateStates = duplicates; BeamPruned = beamPruned;
            Elapsed = elapsed; TimedOut = timedOut; Cancelled = cancelled; ReachedNodeLimit = nodeLimit;
            ReachedActionLimit = actionLimit; CompletedHorizon = completed;
            UnsupportedAttempts = Array.AsReadOnly(unsupported.ToArray());
            PolicyBaselineScore = policyBaselineScore;
            RequestedTurns = requestedTurns; UsedShorterHorizon = usedShorterHorizon;
            SearchedToBattleEnd = searchedToBattleEnd; ReachedStepLimit = stepLimit; MaxSteps = maxSteps;
            WorkerThreadIds = Array.AsReadOnly((workerThreadIds ?? new[] { Thread.CurrentThread.ManagedThreadId }).ToArray());
            SearchMetrics = searchMetrics ?? new BattleSearchMetrics();
            StoppedEarly = stoppedEarly; PyreHealthUpperBound = pyreHealthUpperBound;
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

        // Keep capped public scores comparable across workers as well as within
        // one search. This is a tie-breaker, never a substitute for win/Pyre HP.
        internal static double PositionCost(BattleTurnState state, RoomOutcome outcome = RoomOutcome.Exchanged)
        {
            double material = state.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => unit.Health > 0)
                .Sum(unit => unit.IsPyre ? 0d : unit.Team == CombatTeam.Enemy ?
                    unit.Health + (double)(unit.Status("armor")?.Stacks ?? 0) :
                    -(Math.Max(0d, unit.Attack) * (1d + Math.Max(0, unit.Status("multistrike")?.Stacks ?? 0)) +
                        unit.Health * .1 + Math.Max(0, unit.Status("armor")?.Stacks ?? 0) * .1));
            if (outcome != RoomOutcome.PlayerDefeated) return material;
            // Equal-loss rollouts must not treat an unspawned boss as defeated.
            // This only orders the public loss-score plateau; wins and Pyre HP
            // keep their existing primary scores, as do nonterminal branches.
            double bosses = state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Where(unit => unit.Team == CombatTeam.Enemy && unit.IsBoss == true && unit.Health > 0)
                .Sum(unit => unit.Health / (double)Math.Max(1, unit.MaxHealth));
            bosses += state.Spawn.Waves.Skip(state.Spawn.Phase).Sum(wave => wave.Candidates
                .Select(group => group.Units.Count(unit => unit.Unit.IsBoss == true)).DefaultIfEmpty(0).Max());
            if (state.Spawn.QueuedOuterBoss != null || state.Spawn.QueuedTrueFinalBoss != null) bosses++;
            return bosses * 1000000d + material;
        }

        public static BattlePlanResult Plan(BattleTurnState source, Func<BattleTurnState, string> stateKey,
            BattlePlannerOptions? options = null, Func<BattleTurnState, RoomOutcome, double>? evaluate = null,
            CancellationToken cancellationToken = default)
            => PlanCore(source, stateKey, options, evaluate, cancellationToken, true);

        internal static BattlePlanResult PlanCore(BattleTurnState source, Func<BattleTurnState, string> stateKey,
            BattlePlannerOptions? options, Func<BattleTurnState, RoomOutcome, double>? evaluate,
            CancellationToken cancellationToken, bool optimizeTransitions, BattleEarlyStop? earlyStop = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (stateKey == null) throw new ArgumentNullException(nameof(stateKey));
            options ??= new BattlePlannerOptions();
            var watch = Stopwatch.StartNew();
            if (options.StopAtBestPyreHealth && evaluate == null) earlyStop ??= BattleEarlyStop.Create(source, cancellationToken);
            else earlyStop = null;
            evaluate ??= Evaluate;
            var workspace = new BattleSearchWorkspace(optimizeTransitions);
            int maxTurns = options.SearchToBattleEnd ? options.MaxSteps + 1 : options.MaxTurns;
            int nodes = 0, duplicates = 0, pruned = 0;
            bool timedOut = false, cancelled = false, nodeLimit = false, actionLimit = false, stepLimit = false;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var unsupported = new List<UnsupportedActionAttempt>();
            var unsupportedKeys = new HashSet<string>(StringComparer.Ordinal);
            var root = new Node(source, RoomOutcome.Exchanged, Array.Empty<BattleSearchStep>(), 0, 0, Score(source, RoomOutcome.Exchanged));
            Node fallback = root;
            Node? completedPrefix = null;
            Node? best = null;
            double? policyBaselineScore = null;

            bool Stop()
            {
                cancelled |= cancellationToken.IsCancellationRequested;
                timedOut |= watch.Elapsed >= options.TimeLimit;
                nodeLimit |= nodes >= options.MaxNodes;
                return cancelled || timedOut || nodeLimit || earlyStop?.Reached == true;
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
            bool CanExpand(Node node)
            {
                if (node.Steps.Count < options.MaxSteps) return true;
                stepLimit = true; return false;
            }
            void Consider(Node node)
            {
                if (node.Turns > fallback.Turns || node.Turns == fallback.Turns && Better(node, fallback)) fallback = node;
                if ((Terminal(node) || node.Turns >= maxTurns) && (best == null || Better(node, best))) best = node;
                earlyStop?.Consider(node.State, node.Outcome);
                // A usable shorter window ends after a complete EndTurn. Do
                // not publish an arbitrary truncated action sequence.
                if (node.Turns > 0 && node.Actions == 0 && node.Steps[node.Steps.Count - 1].IsEndTurn &&
                    (completedPrefix == null || node.Turns > completedPrefix.Turns ||
                        node.Turns == completedPrefix.Turns && Better(node, completedPrefix))) completedPrefix = node;
            }
            bool Better(Node left, Node right) => left.Score < right.Score ||
                left.Score == right.Score && left.PositionCost < right.PositionCost;
            Node Child(Node parent, BattleTurnState state, RoomOutcome outcome, BattleSearchStep step)
            {
                var steps = parent.Steps.Concat(new[] { step }).ToArray();
                return new Node(state, outcome, steps, parent.Turns + (step.IsEndTurn ? 1 : 0),
                    step.IsEndTurn ? 0 : parent.Actions + 1, Score(state, outcome));
            }

            // Every rollout shares the node/time budget. With the optional
            // tactical seed, spend it on deployment first: completing an empty
            // tower's defeat can otherwise displace a useful partial deployment
            // when the game runtime reaches its time limit.
            Node Rollout(Node baseline, int? stopAfterTurns = null, bool finishFirstTurn = false)
            {
                while (baseline.Turns < (stopAfterTurns ?? maxTurns) && !Terminal(baseline) && CanExpand(baseline) &&
                    (!Stop() || finishFirstTurn && baseline.Turns == 0 && !cancelled && !nodeLimit && earlyStop?.Reached != true))
                {
                    nodes++;
                    BattleTurnResult ended = workspace.EndTurn(baseline.State);
                    if (!ended.Supported) { Unsupported(new UnsupportedActionAttempt(null, ended.UnsupportedReason ?? "Unsupported EndTurn.")); break; }
                    baseline = Child(baseline, ended.State!, ended.Outcome, new BattleSearchStep(null, true));
                    Consider(baseline);
                }
                return baseline;
            }
            // Build a useful current-turn route before the no-card forecast.
            // Native cold-start EndTurns can consume the whole soft budget;
            // an empty-tower defeat must not prevent trying a legal deployment.
            // With only one available expansion/step, a card cannot reach a
            // publishable turn boundary. Preserve the bounded EndTurn fallback.
            if (options.SearchToBattleEnd && (options.MaxSteps == 1 || options.MaxNodes == 1)) Rollout(root);
            if (options.UseTacticalSeed)
            {
                Node tactical = root;
                while (!Stop() && tactical.Turns < maxTurns && !Terminal(tactical) && CanExpand(tactical))
                {
                    nodes++;
                    SupportedActionCandidate? chosen = null;
                    if (tactical.Actions < options.MaxActionsPerTurn)
                    {
                        chosen = workspace.Choose(tactical.State, out IReadOnlyList<UnsupportedActionAttempt> omitted,
                            options.PreferredDeploymentRoom);
                        foreach (UnsupportedActionAttempt attempt in omitted) Unsupported(attempt);
                    }
                    else actionLimit = true;
                    if (chosen != null)
                        tactical = Child(tactical, chosen.Result.State!, chosen.Result.Outcome, new BattleSearchStep(chosen.Action, false));
                    else
                    {
                        BattleTurnResult ended = workspace.EndTurn(tactical.State);
                        if (!ended.Supported) { Unsupported(new UnsupportedActionAttempt(null, ended.UnsupportedReason ?? "Unsupported tactical-seed EndTurn.")); break; }
                        tactical = Child(tactical, ended.State!, ended.Outcome, new BattleSearchStep(null, true));
                    }
                    Consider(tactical);
                    // Keep a complete forecast whose first action deploys the
                    // chosen unit. A truncated seed alone cannot be published
                    // by the advice verifier, and an empty-tower loss would
                    // discard that useful deployment under the same budget.
                    // A complete one-turn fallback is sufficient when shorter
                    // windows are allowed. Preserve the remaining budget for
                    // the seed's full card sequence instead of an empty tower.
                    // One atomic candidate can consume the soft time budget.
                    // Finish its first turn before publishing a shorter window;
                    // cancellation and the hard node limit still take priority.
                    if (tactical.Steps.Count == 1) Rollout(tactical, options.AllowShorterHorizon ? 1 : maxTurns,
                        finishFirstTurn: options.AllowShorterHorizon);
                }
                // A winning seed remains an incumbent; use the remaining budget
                // to seek a win that preserves more Pyre health.
            }

            // With too few nodes for the requested horizon, retain complete
            // turn boundaries instead of spending the only node on a card
            // that cannot be published. Initial deployment still needs a unit.
            if (!options.SearchToBattleEnd && source.Spawn.Turn > 0 && options.MaxNodes <= maxTurns) Rollout(root);

            // Spend the interactive budget on useful card choices before the
            // no-card baseline. Equipment replacement, tank placement and
            // ability ownership matter even with a short planning horizon;
            // the optional seed above still adds the long-horizon rollout.
            // Every candidate shares the same legality, evaluator and budget.
            Node strength = root;
            bool adaptDeployment = !options.UseTacticalSeed && options.AllowShorterHorizon && options.PreferredDeploymentRoom == 2;
            // The interactive default can improve its opening preference.
            // Explicit other-floor and strict-horizon seeds retain theirs.
            int strengthRoom = adaptDeployment && source.Spawn.Turn > 0
                ? source.Spawn.Train.Rooms.FirstOrDefault(room => room.Units.Any(unit =>
                    unit.Team == CombatTeam.Player && unit.Health > 0 && unit.Modifiers?.IsChampion == true))?.RoomIndex ?? options.PreferredDeploymentRoom
                : options.PreferredDeploymentRoom;
            while (!Stop() && strength.Turns < maxTurns && !Terminal(strength) && CanExpand(strength))
            {
                nodes++;
                SupportedActionCandidate? chosen = null;
                if (strength.Actions < options.MaxActionsPerTurn)
                {
                    chosen = workspace.Choose(strength.State, out IReadOnlyList<UnsupportedActionAttempt> omitted,
                        strengthRoom);
                    foreach (UnsupportedActionAttempt attempt in omitted) Unsupported(attempt);
                }
                else actionLimit = true;
                if (chosen != null)
                {
                    strength = Child(strength, chosen.Result.State!, chosen.Result.Outcome, new BattleSearchStep(chosen.Action, false));
                    Consider(strength);
                    // Deployment needs a cheap partial fallback while placing
                    // several units. Later turns first finish their card sequence:
                    // forecasting immediately after a free ability/draw can spend
                    // the budget before the seed reaches its damaging or X card.
                    if (source.Spawn.Turn == 0 && strength.Steps.Count == 1)
                        Rollout(strength, options.AllowShorterHorizon ? 1 : maxTurns,
                            finishFirstTurn: options.AllowShorterHorizon);
                }
                else
                {
                    BattleTurnResult ended = workspace.EndTurn(strength.State);
                    if (!ended.Supported) { Unsupported(new UnsupportedActionAttempt(null, ended.UnsupportedReason ?? "Unsupported strength-seed EndTurn.")); break; }
                    strength = Child(strength, ended.State!, ended.Outcome, new BattleSearchStep(null, true));
                    Consider(strength);
                    // Retain the seed's complete current-turn card sequence
                    // even if its later card choices exhaust the shared budget.
                    if (strength.Turns == 1)
                    {
                        // Reach the first combat before committing the whole
                        // budget to later empty turns. A verified two-turn
                        // prefix can still be published as a shorter window.
                        Node forecast = Rollout(strength, options.SearchToBattleEnd || adaptDeployment && source.Spawn.Turn == 0
                            ? Math.Min(2, maxTurns) : maxTurns);
                        // Compare other deployments only when the ordinary
                        // opening forecast loses a unit already deployed by
                        // this seed. Each alternative uses real legal actions
                        // and the same complete horizon and shared budgets.
                        // Unthreatened openings keep their existing preference.
                        if (adaptDeployment && source.Spawn.Turn == 0 && forecast.Turns > 1 && !Terminal(forecast) &&
                            strength.State.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit =>
                                unit.Team == CombatTeam.Player && !unit.IsPyre && unit.Health > 0 && unit.SpawnerCardId > 0 &&
                                !forecast.State.Spawn.Train.Rooms.SelectMany(room => room.Units)
                                    .Any(survivor => survivor.Id == unit.Id && survivor.Health > 0)))
                        {
                            foreach (int room in source.PlayRules?.Rooms.Select(room => room.RoomIndex)
                                .Where(room => room != options.PreferredDeploymentRoom).OrderBy(room => room) ?? Enumerable.Empty<int>())
                            {
                                if (Stop()) break;
                                Node alternate = root;
                                while (!Stop() && alternate.Turns == 0 && !Terminal(alternate) && CanExpand(alternate))
                                {
                                    nodes++;
                                    SupportedActionCandidate? move = null;
                                    if (alternate.Actions < options.MaxActionsPerTurn)
                                    {
                                        move = workspace.Choose(alternate.State, out IReadOnlyList<UnsupportedActionAttempt> omitted, room);
                                        foreach (UnsupportedActionAttempt attempt in omitted) Unsupported(attempt);
                                    }
                                    else actionLimit = true;
                                    if (move != null)
                                        alternate = Child(alternate, move.Result.State!, move.Result.Outcome, new BattleSearchStep(move.Action, false));
                                    else
                                    {
                                        BattleTurnResult nextTurn = workspace.EndTurn(alternate.State);
                                        if (!nextTurn.Supported)
                                        { Unsupported(new UnsupportedActionAttempt(null, nextTurn.UnsupportedReason ?? "Unsupported alternative deployment EndTurn.")); break; }
                                        alternate = Child(alternate, nextTurn.State!, nextTurn.Outcome, new BattleSearchStep(null, true));
                                    }
                                    Consider(alternate);
                                }
                                if (alternate.Turns != 1) continue;
                                Node alternateForecast = Rollout(alternate, forecast.Turns);
                                if (alternateForecast.Turns == forecast.Turns && Better(alternateForecast, forecast))
                                {
                                    strength = alternate; strengthRoom = room; forecast = alternateForecast;
                                    // Keep the first complete improvement and
                                    // spend the rest on its requested horizon.
                                    break;
                                }
                            }
                        }
                        // A whole-battle route needs the later turns' card
                        // choices before an empty-hand terminal forecast spends
                        // the remaining interactive budget. Keep its complete
                        // short fallback and continue the same legal policy.
                        if (!options.SearchToBattleEnd) Rollout(forecast);
                    }
                }
            }

            // A later turn can spend its entire soft budget choosing cards.
            // If no complete forecast exists yet, finish that chosen prefix
            // with one EndTurn. Keep strict callers, cancellation and the hard
            // node cap binding, as for the initial tactical fallback above.
            if (options.AllowShorterHorizon && best == null && completedPrefix == null &&
                strength.Turns == 0 && strength.Actions > 0 && timedOut)
                Rollout(strength, 1, finishFirstTurn: true);

            Rollout(root);

            // Seed the existing fixed play policy as well as the no-card path.
            // Reuse its cached successors rather than applying a selected card
            // twice. Forecast the first play once, then spend the budget on
            // completing the seed instead of re-ending the same future turns
            // after every card. The no-card horizon remains an executable
            // fallback if either seed cannot finish in time.
            Node policy = root;
            while (!Stop() && policy.Turns < maxTurns && !Terminal(policy) && CanExpand(policy))
            {
                nodes++;
                SupportedActionCandidate? chosen = null;
                if (policy.Actions < options.MaxActionsPerTurn)
                {
                    chosen = workspace.ChoosePolicy(policy.State, out IReadOnlyList<UnsupportedActionAttempt> omitted);
                    foreach (UnsupportedActionAttempt attempt in omitted) Unsupported(attempt);
                }
                else actionLimit = true;
                if (chosen != null)
                {
                    policy = Child(policy, chosen.Result.State!, chosen.Result.Outcome, new BattleSearchStep(chosen.Action, false));
                    Consider(policy);
                    if (policy.Steps.Count == 1) Rollout(policy);
                }
                else
                {
                    BattleTurnResult ended = workspace.EndTurn(policy.State);
                    if (!ended.Supported) { Unsupported(new UnsupportedActionAttempt(null, ended.UnsupportedReason ?? "Unsupported policy EndTurn.")); break; }
                    policy = Child(policy, ended.State!, ended.Outcome, new BattleSearchStep(null, true));
                    Consider(policy);
                }
            }
            if (policy.Turns >= maxTurns || Terminal(policy)) policyBaselineScore = policy.Score;

            var frontier = new List<Node> { root };
            if (!Stop()) seen.Add("0|" + stateKey(source));
            while (frontier.Count > 0 && !Stop())
            {
                var next = new List<Node>();
                foreach (Node node in frontier)
                {
                    if (Stop()) break;
                    if (!CanExpand(node)) continue;
                    nodes++;
                    if (node.Actions < options.MaxActionsPerTurn)
                    {
                        BattleActionEnumeration actions = workspace.Enumerate(node.State);
                        foreach (UnsupportedActionAttempt attempt in actions.UnsupportedAttempts) Unsupported(attempt);
                        var children = new List<Node>();
                        foreach (SupportedActionCandidate candidate in actions.SupportedCandidates)
                        {
                            if (watch.Elapsed >= options.TimeLimit || cancellationToken.IsCancellationRequested || earlyStop?.Reached == true) break;
                            Node child = Child(node, candidate.Result.State!, candidate.Result.Outcome, new BattleSearchStep(candidate.Action, false));
                            children.Add(child); Add(child);
                        }
                        // Short deterministic continuations turn promising partial
                        // branches into executable horizon recommendations early.
                        foreach (Node child in children.OrderBy(child => child.Score).ThenBy(child => child.PositionCost).Take(2)) Rollout(child);
                    }
                    else actionLimit = true;
                    if (watch.Elapsed >= options.TimeLimit || cancellationToken.IsCancellationRequested || earlyStop?.Reached == true) break;
                    BattleTurnResult ended = workspace.EndTurn(node.State);
                    if (ended.Supported) Add(Child(node, ended.State!, ended.Outcome, new BattleSearchStep(null, true)));
                    else Unsupported(new UnsupportedActionAttempt(null, ended.UnsupportedReason ?? "Unsupported EndTurn."));
                }
                next = next.OrderBy(node => node.Score).ThenBy(node => node.PositionCost)
                    .ThenByDescending(node => node.Turns).ThenBy(node => node.Steps.Count).ToList();
                pruned += Math.Max(0, next.Count - options.BeamWidth);
                frontier = next.Take(options.BeamWidth).ToList();

                void Add(Node child)
                {
                    Consider(child);
                    if (Terminal(child) || child.Turns >= maxTurns || !CanExpand(child)) return;
                    string key = child.Turns + "|" + stateKey(child.State);
                    if (!seen.Add(key)) { duplicates++; return; }
                    next.Add(child);
                }
            }
            // Cancellation/time can occur inside one atomic model expansion.
            cancelled |= cancellationToken.IsCancellationRequested;
            timedOut |= watch.Elapsed >= options.TimeLimit;
            bool shorter = best == null && options.AllowShorterHorizon && completedPrefix != null &&
                !cancelled && (timedOut || nodeLimit || stepLimit);
            Node selected = best ?? (shorter ? completedPrefix! : fallback);
            return new BattlePlanResult(selected.State, selected.Outcome, selected.Steps, selected.Score, selected.Turns,
                nodes, duplicates, pruned, watch.Elapsed, timedOut, cancelled, nodeLimit, actionLimit, best != null, unsupported, policyBaselineScore,
                maxTurns, shorter, options.SearchToBattleEnd, stepLimit, options.MaxSteps, searchMetrics: workspace.Metrics,
                stoppedEarly: earlyStop?.Reached == true, pyreHealthUpperBound: earlyStop?.PyreHealthUpperBound);
        }

        private sealed class Node
        {
            public BattleTurnState State { get; }
            public RoomOutcome Outcome { get; }
            public IReadOnlyList<BattleSearchStep> Steps { get; }
            public int Turns { get; }
            public int Actions { get; }
            public double Score { get; }
            public double PositionCost { get; }
            public Node(BattleTurnState state, RoomOutcome outcome, IReadOnlyList<BattleSearchStep> steps, int turns, int actions, double score)
            {
                State = state; Outcome = outcome; Steps = steps; Turns = turns; Actions = actions; Score = score;
                // The public score caps positional value below a single Pyre
                // HP. Keep useful actions distinguishable on that plateau,
                // without changing archived scores or the survival ordering.
                PositionCost = BattlePlanner.PositionCost(state, outcome);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace MonsterTrain2Poju.Model
{
    public sealed class BattleBeliefOptions
    {
        public int MaxNodes { get; }
        public int MaxDecisions { get; }
        public TimeSpan TimeLimit { get; }
        public BattleBeliefOptions(int maxNodes = 256, int maxDecisions = 3, TimeSpan? timeLimit = null)
        {
            if (maxNodes < 1 || maxDecisions < 0 || maxDecisions > 64) throw new ArgumentOutOfRangeException(nameof(maxNodes));
            TimeLimit = timeLimit ?? TimeSpan.FromSeconds(3);
            if (TimeLimit < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeLimit));
            MaxNodes = maxNodes; MaxDecisions = maxDecisions;
        }
    }
    public sealed class BattleBeliefBranch
    {
        public string Observation { get; }
        public double Probability { get; }
        public BattleBeliefPolicy Policy { get; }
        public BattleBeliefBranch(string observation, double probability, BattleBeliefPolicy policy)
        { Observation = observation; Probability = probability; Policy = policy; }
    }
    public sealed class BattleBeliefPolicy
    {
        public PlayCardAction? Action { get; }
        public bool IsEndTurn { get; }
        public bool IsLeaf { get; }
        public double ExpectedScore { get; }
        public bool CompletedHorizon { get; }
        public bool AllTerminal { get; }
        public IReadOnlyList<BattleBeliefBranch> Branches { get; }
        public BattleBeliefPolicy(PlayCardAction? action, bool isEndTurn, bool isLeaf, double expectedScore, bool completedHorizon,
            bool allTerminal, IReadOnlyList<BattleBeliefBranch> branches)
        { Action = action; IsEndTurn = isEndTurn; IsLeaf = isLeaf; ExpectedScore = expectedScore; CompletedHorizon = completedHorizon;
            AllTerminal = allTerminal; Branches = Array.AsReadOnly(branches.ToArray()); }
    }
    public sealed class BattleBeliefResult
    {
        public BattleBeliefPolicy Policy { get; }
        public int ExpandedWorlds { get; }
        public bool ReachedNodeLimit { get; }
        public bool TimedOut { get; }
        public bool Cancelled { get; }
        public bool ExhaustiveDistribution { get; }
        public TimeSpan Elapsed { get; }
        public IReadOnlyList<string> UnsupportedReasons { get; }
        public bool CertifiedFiniteHorizonOptimal => ExhaustiveDistribution && Policy.CompletedHorizon &&
            !ReachedNodeLimit && !TimedOut && !Cancelled && UnsupportedReasons.Count == 0;
        internal BattleBeliefResult(BattleBeliefPolicy policy, int nodes, bool limited, bool timedOut, bool cancelled,
            bool exhaustive, TimeSpan elapsed, IReadOnlyList<string> unsupported)
        { Policy = policy; ExpandedWorlds = nodes; ReachedNodeLimit = limited; TimedOut = timedOut; Cancelled = cancelled;
            ExhaustiveDistribution = exhaustive; Elapsed = elapsed; UnsupportedReasons = Array.AsReadOnly(unsupported.ToArray()); }
    }
    public static class BattleBeliefPlanner
    {
        // Expectimax over a finite belief. Worlds with the same observation must
        // take one common legal action; only revealed observations branch policy.
        public static BattleBeliefResult Plan(BattleBelief belief, BattleBeliefOptions? options = null,
            Func<BattleTurnState, RoomOutcome, double>? evaluate = null, CancellationToken cancellationToken = default)
        {
            if (belief == null) throw new ArgumentNullException(nameof(belief));
            options ??= new BattleBeliefOptions(); evaluate ??= BattlePlanner.Evaluate;
            using var observations = new BattleObservationKeys(); using var actionKeys = new BattleStateKeys();
            if (belief.Worlds.Select(world => observations.Key(world.State, world.Outcome)).Distinct().Count() != 1)
                throw new ArgumentException("Initial hypotheses must have the same visible observation.", nameof(belief));
            var watch = Stopwatch.StartNew(); int nodes = 0;
            bool limited = false, timedOut = false, cancelled = false;
            var unsupported = new HashSet<string>(StringComparer.Ordinal);
            var policy = Visit(belief.Worlds, options.MaxDecisions); watch.Stop();
            return new BattleBeliefResult(policy, nodes, limited, timedOut, cancelled, belief.ExhaustiveDistribution,
                watch.Elapsed, unsupported.OrderBy(reason => reason, StringComparer.Ordinal).ToArray());

            bool Stop()
            {
                cancelled |= cancellationToken.IsCancellationRequested;
                timedOut |= watch.Elapsed >= options.TimeLimit;
                limited |= nodes >= options.MaxNodes;
                return cancelled || timedOut || limited;
            }
            BattleBeliefPolicy Leaf(IReadOnlyList<BattleBeliefWorld> worlds, bool complete)
            {
                double total = worlds.Sum(world => world.Weight); var value = new CompensatedSum();
                foreach (var world in worlds)
                {
                    double score = evaluate(world.State, world.Outcome);
                    if (double.IsNaN(score) || double.IsInfinity(score)) throw new InvalidOperationException("Belief scores must be finite.");
                    value.Add(world.Weight / total * score);
                }
                return new BattleBeliefPolicy(null, false, true, value.Value, complete,
                    worlds.All(world => Terminal(world.Outcome)), Array.Empty<BattleBeliefBranch>());
            }
            BattleBeliefPolicy Visit(IReadOnlyList<BattleBeliefWorld> worlds, int remaining)
            {
                if (remaining == 0 || worlds.All(world => Terminal(world.Outcome))) return Leaf(worlds, true);
                if (Stop()) return Leaf(worlds, false);
                var frontiers = new List<Dictionary<string, Transition>>();
                foreach (var world in worlds)
                {
                    if (Stop()) return Leaf(worlds, false);
                    nodes++;
                    var frontier = BattleActionModel.EnumerateActions(world.State);
                    foreach (var attempt in frontier.UnsupportedAttempts) unsupported.Add(attempt.Reason);
                    var candidates = new Dictionary<string, Transition>(StringComparer.Ordinal);
                    var ended = BattleTurnModel.EndTurn(world.State);
                    if (ended.Supported) candidates.Add("EndTurn", new Transition(null, true, ended.State!, ended.Outcome, world.Weight));
                    else unsupported.Add(ended.UnsupportedReason ?? "Unsupported belief EndTurn.");
                    foreach (var candidate in frontier.SupportedCandidates)
                        candidates[actionKeys.SnapshotKey(candidate.Action)] = new Transition(candidate.Action, false,
                            candidate.Result.State!, candidate.Result.Outcome, world.Weight);
                    frontiers.Add(candidates);
                }
                var common = frontiers[0].Keys.Where(key => frontiers.All(frontier => frontier.ContainsKey(key))).ToArray();
                if (frontiers.Any(frontier => frontier.Count != common.Length))
                    unsupported.Add("Legal actions differ between indistinguishable hypotheses; only their common frontier is executable.");
                BattleBeliefPolicy? best = null; double mass = worlds.Sum(world => world.Weight);
                foreach (string action in common)
                {
                    if (cancellationToken.IsCancellationRequested || watch.Elapsed >= options.TimeLimit)
                    { cancelled |= cancellationToken.IsCancellationRequested; timedOut |= watch.Elapsed >= options.TimeLimit; break; }
                    var successors = frontiers.Select(frontier => frontier[action]).ToArray();
                    var branches = new List<BattleBeliefBranch>();
                    foreach (var group in successors.GroupBy(next => observations.Key(next.State, next.Outcome)))
                    {
                        var posterior = group.Select(next => new BattleBeliefWorld(next.State, next.Weight, next.Outcome)).ToArray();
                        branches.Add(new BattleBeliefBranch(group.Key, posterior.Sum(world => world.Weight) / mass, Visit(posterior, remaining - 1)));
                    }
                    if (!branches.All(branch => branch.Policy.CompletedHorizon)) continue;
                    var value = new CompensatedSum();
                    foreach (var branch in branches) value.Add(branch.Probability * branch.Policy.ExpectedScore);
                    if (best == null || value.Value < best.ExpectedScore)
                        best = new BattleBeliefPolicy(successors[0].Action, successors[0].EndTurn, false, value.Value, true,
                            branches.All(branch => branch.Policy.AllTerminal), branches);
                }
                return best ?? Leaf(worlds, false);
            }
        }
        private static bool Terminal(RoomOutcome outcome) => outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated or RoomOutcome.Stalemate;
        private sealed class Transition
        {
            internal readonly PlayCardAction? Action;
            internal readonly bool EndTurn;
            internal readonly BattleTurnState State;
            internal readonly RoomOutcome Outcome;
            internal readonly double Weight;
            internal Transition(PlayCardAction? action, bool endTurn, BattleTurnState state, RoomOutcome outcome, double weight)
            { Action = action; EndTurn = endTurn; State = state; Outcome = outcome; Weight = weight; }
        }
    }
}

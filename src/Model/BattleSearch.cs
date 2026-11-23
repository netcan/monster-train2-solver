using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MonsterTrain2Poju.Model
{
    public sealed class BattleSearchOptions
    {
        public int MaxNodes { get; }
        public int MaxTurns { get; }
        public int MaxDegreeOfParallelism { get; }
        public BattleSearchOptions(int maxNodes = 100000, int maxTurns = 100, int maxDegreeOfParallelism = 1)
        {
            if (maxNodes < 1) throw new ArgumentOutOfRangeException(nameof(maxNodes));
            if (maxTurns < 0) throw new ArgumentOutOfRangeException(nameof(maxTurns));
            if (maxDegreeOfParallelism < 1) throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism));
            MaxNodes = maxNodes;
            MaxTurns = maxTurns;
            MaxDegreeOfParallelism = maxDegreeOfParallelism;
        }
    }

    public sealed class BattleSearchStep
    {
        public PlayCardAction? Action { get; }
        public bool IsEndTurn { get; }
        public IReadOnlyList<RelentlessDecision>? RelentlessDecisions { get; }
        public bool ShouldSerializeRelentlessDecisions() => RelentlessDecisions != null;
        public BattleSearchStep(PlayCardAction? action, bool isEndTurn, IReadOnlyList<RelentlessDecision>? relentlessDecisions = null)
        { Action = action; IsEndTurn = isEndTurn;
            if (!isEndTurn && relentlessDecisions?.Count > 0) throw new ArgumentException("Relentless decisions belong to EndTurn.", nameof(relentlessDecisions));
            RelentlessDecisions = relentlessDecisions?.Count > 0 ? Array.AsReadOnly(relentlessDecisions.ToArray()) : null; }
    }

    // Completeness refers only to the enumerated model frontier, not the full native game's action space.
    public sealed class BattleSearchResult
    {
        public BattleTurnState? BestState { get; }
        public RoomOutcome Outcome { get; }
        public double? Score { get; }
        public IReadOnlyList<BattleSearchStep> Steps { get; }
        public int DecisionStatesVisited { get; }
        public int PrunedBranches { get; }
        public bool ReachedNodeLimit { get; }
        public bool ReachedTurnLimit { get; }
        public bool Cancelled { get; }
        public IReadOnlyList<UnsupportedActionAttempt> UnsupportedAttempts { get; }
        public bool HasUnsupportedAttempts => UnsupportedAttempts.Count > 0;
        public bool ExhaustedEnumeratedFrontier => !ReachedNodeLimit && !ReachedTurnLimit && !Cancelled && !HasUnsupportedAttempts;

        internal BattleSearchResult(BattleTurnState? bestState, RoomOutcome outcome, double? score,
            IReadOnlyList<BattleSearchStep> steps, int nodesVisited, int prunedBranches, bool reachedNodeLimit,
            bool reachedTurnLimit, bool cancelled, IReadOnlyList<UnsupportedActionAttempt> unsupportedAttempts)
        {
            BestState = bestState;
            Outcome = outcome;
            Score = score;
            Steps = Array.AsReadOnly(steps.ToArray());
            DecisionStatesVisited = nodesVisited;
            PrunedBranches = prunedBranches;
            ReachedNodeLimit = reachedNodeLimit;
            ReachedTurnLimit = reachedTurnLimit;
            Cancelled = cancelled;
            UnsupportedAttempts = Array.AsReadOnly(unsupportedAttempts.ToArray());
        }
    }

    public static class BattleSearchModel
    {
        // Scores are minimized. A supplied lower bound must be safe for every terminal or horizon leaf reachable from that state.
        // The score and bound delegates may run concurrently when MaxDegreeOfParallelism is greater than one.
        public static BattleSearchResult Minimize(BattleTurnState source,
            Func<BattleTurnState, RoomOutcome, double> score, BattleSearchOptions? options = null,
            Func<BattleTurnState, double?>? admissibleLowerBound = null, CancellationToken cancellationToken = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (score == null) throw new ArgumentNullException(nameof(score));
            options ??= new BattleSearchOptions();
            SearchSummary summary = Explore(source, 0, options.MaxNodes, root: true, score, options,
                admissibleLowerBound, cancellationToken);
            SearchLeaf? best = summary.Best;
            return new BattleSearchResult(best?.State, best?.Outcome ?? RoomOutcome.Unsupported, best?.Score,
                best?.Steps ?? Array.Empty<BattleSearchStep>(), summary.NodesVisited, summary.PrunedBranches,
                summary.ReachedNodeLimit, summary.ReachedTurnLimit, summary.Cancelled, summary.UnsupportedAttempts);
        }

        private static SearchSummary Explore(BattleTurnState state, int turnsElapsed, int budget, bool root,
            Func<BattleTurnState, RoomOutcome, double> score, BattleSearchOptions options,
            Func<BattleTurnState, double?>? lowerBound, CancellationToken cancellationToken)
        {
            var summary = new SearchSummary();
            if (budget <= 0)
            {
                summary.ReachedNodeLimit = true;
                summary.Consider(Leaf(state, RoomOutcome.Exchanged, Array.Empty<BattleSearchStep>(), score),
                    isObjectiveLeaf: turnsElapsed >= options.MaxTurns);
                return summary;
            }
            summary.NodesVisited = 1;
            if (cancellationToken.IsCancellationRequested)
            {
                summary.Cancelled = true;
                summary.Consider(Leaf(state, RoomOutcome.Exchanged, Array.Empty<BattleSearchStep>(), score),
                    isObjectiveLeaf: turnsElapsed >= options.MaxTurns);
                return summary;
            }

            BattleActionEnumeration frontier = BattleActionModel.EnumerateActions(state);
            foreach (UnsupportedActionAttempt attempt in frontier.UnsupportedAttempts) summary.AddUnsupported(attempt);
            var branches = new List<SearchBranch>(frontier.SupportedCandidates.Count + 1);
            foreach (SupportedActionCandidate candidate in frontier.SupportedCandidates)
                branches.Add(new SearchBranch(new BattleSearchStep(candidate.Action, false), candidate.Result.State!,
                    candidate.Result.Outcome, turnsElapsed));

            if (turnsElapsed < options.MaxTurns)
            {
                BattleEndTurnFrontier ends = BattleEndTurnModel.Enumerate(state, Math.Max(0, budget - 1),
                    () => cancellationToken.IsCancellationRequested);
                summary.NodesVisited += ends.ChoiceNodes;
                summary.ReachedNodeLimit |= ends.ReachedNodeLimit;
                summary.Cancelled |= ends.Interrupted;
                foreach (UnsupportedActionAttempt attempt in ends.UnsupportedAttempts) summary.AddUnsupported(attempt);
                foreach (BattleEndTurnCandidate end in ends.Candidates)
                    branches.Add(new SearchBranch(end.Step, end.Result.State!, end.Result.Outcome, turnsElapsed + 1));
            }
            else
            {
                summary.ReachedTurnLimit = true;
                summary.Consider(Leaf(state, RoomOutcome.Exchanged, Array.Empty<BattleSearchStep>(), score),
                    isObjectiveLeaf: true);
            }

            PrioritizeBranches(branches, Math.Max(1, budget - summary.NodesVisited + 1), root, score, options, lowerBound);

            if (branches.Count == 0)
            {
                if (summary.Best == null)
                    summary.Consider(Leaf(state, RoomOutcome.Exchanged, Array.Empty<BattleSearchStep>(), score));
                return summary;
            }

            if (root && options.MaxDegreeOfParallelism > 1)
            {
                int[] childBudgets = AllocateBudget(Math.Max(0, budget - summary.NodesVisited), branches);
                var results = new SearchSummary[branches.Count];
                Parallel.For(0, branches.Count, new ParallelOptions { MaxDegreeOfParallelism = options.MaxDegreeOfParallelism }, index =>
                {
                    results[index] = ExploreBranch(branches[index], childBudgets[index], score, options,
                        lowerBound, cancellationToken);
                });
                for (int index = 0; index < results.Length; index++) summary.Merge(results[index]);
            }
            else
            {
                int remaining = Math.Max(0, budget - summary.NodesVisited);
                int remainingChildren = branches.Count(branch => !IsTerminal(branch.Outcome));
                for (int index = 0; index < branches.Count; index++)
                {
                    SearchBranch branch = branches[index];
                    int childBudget = 0;
                    if (!IsTerminal(branch.Outcome) && remainingChildren > 0)
                    {
                        // Round up without overflowing a caller's maximum Int32 budget.
                        childBudget = remaining == 0 ? 0 : 1 + (remaining - 1) / remainingChildren;
                    }
                    SearchSummary child = ExploreBranch(branch, childBudget, score, options, lowerBound,
                        cancellationToken, summary.PruningScore);
                    summary.Merge(child);
                    if (!IsTerminal(branch.Outcome))
                    {
                        remaining -= child.NodesVisited;
                        remainingChildren--;
                    }
                }
            }
            summary.Cancelled |= cancellationToken.IsCancellationRequested;
            return summary;
        }

        private static void PrioritizeBranches(List<SearchBranch> branches, int budget, bool root,
            Func<BattleTurnState, RoomOutcome, double> score, BattleSearchOptions options,
            Func<BattleTurnState, double?>? lowerBound)
        {
            int nonterminal = branches.Count(branch => !IsTerminal(branch.Outcome));
            int remainder = nonterminal == 0 ? 0 : Math.Max(0, budget - 1) % nonterminal;
            bool canUseBoundForOrdering = lowerBound != null && (!root || options.MaxDegreeOfParallelism == 1);
            if (remainder == 0 && !canUseBoundForOrdering) return;

            var ordered = branches.Select((branch, index) => new
                {
                    Branch = branch,
                    Index = index,
                    Priority = Priority(branch)
                })
                .OrderBy(item => item.Priority)
                .ThenBy(item => item.Index)
                .Select(item => item.Branch)
                .ToArray();
            branches.Clear();
            branches.AddRange(ordered);

            double Priority(SearchBranch branch)
            {
                if (IsTerminal(branch.Outcome)) return Score(branch.State, branch.Outcome);
                double? bound = lowerBound?.Invoke(branch.State);
                if (bound.HasValue)
                {
                    ValidateScore(bound.Value);
                    return bound.Value;
                }
                return Score(branch.State, branch.Outcome);
            }

            double Score(BattleTurnState state, RoomOutcome outcome)
            {
                double value = score(state, outcome);
                ValidateScore(value);
                return value;
            }
        }

        private static SearchSummary ExploreBranch(SearchBranch branch, int budget,
            Func<BattleTurnState, RoomOutcome, double> score, BattleSearchOptions options,
            Func<BattleTurnState, double?>? lowerBound, CancellationToken cancellationToken, double? incumbent = null)
        {
            if (IsTerminal(branch.Outcome))
            {
                var terminal = new SearchSummary();
                terminal.Consider(Leaf(branch.State, branch.Outcome, new[] { branch.Step }, score),
                    isObjectiveLeaf: true);
                return terminal;
            }
            if (incumbent.HasValue && lowerBound != null)
            {
                double? bound = lowerBound(branch.State);
                if (bound.HasValue)
                {
                    ValidateScore(bound.Value);
                    if (bound.Value >= incumbent.Value)
                    {
                        var pruned = new SearchSummary { PrunedBranches = 1 };
                        return pruned;
                    }
                }
            }
            if (budget <= 0)
            {
                var cutoff = new SearchSummary { ReachedNodeLimit = true };
                cutoff.Consider(Leaf(branch.State, RoomOutcome.Exchanged, new[] { branch.Step }, score),
                    isObjectiveLeaf: branch.TurnsElapsed >= options.MaxTurns);
                return cutoff;
            }
            SearchSummary child = Explore(branch.State, branch.TurnsElapsed, budget, root: false, score, options,
                lowerBound, cancellationToken);
            if (child.Best != null) child.Best = Prefix(branch.Step, child.Best);
            return child;
        }

        private static int[] AllocateBudget(int budget, IReadOnlyList<SearchBranch> branches)
        {
            var result = new int[branches.Count];
            int nonterminal = branches.Count(branch => !IsTerminal(branch.Outcome));
            if (nonterminal == 0) return result;
            int each = budget / nonterminal;
            int extra = budget % nonterminal;
            for (int index = 0; index < branches.Count; index++)
            {
                if (IsTerminal(branches[index].Outcome)) continue;
                result[index] = each + (extra-- > 0 ? 1 : 0);
            }
            return result;
        }

        private static SearchLeaf Leaf(BattleTurnState state, RoomOutcome outcome,
            IReadOnlyList<BattleSearchStep> steps, Func<BattleTurnState, RoomOutcome, double> score)
        {
            double value = score(state, outcome);
            ValidateScore(value);
            return new SearchLeaf(state, outcome, value, steps);
        }

        private static SearchLeaf Prefix(BattleSearchStep step, SearchLeaf child)
        {
            var steps = new BattleSearchStep[child.Steps.Count + 1];
            steps[0] = step;
            for (int index = 0; index < child.Steps.Count; index++) steps[index + 1] = child.Steps[index];
            return new SearchLeaf(child.State, child.Outcome, child.Score, steps);
        }

        private static bool IsTerminal(RoomOutcome outcome) => outcome == RoomOutcome.BattleWon ||
            outcome == RoomOutcome.PlayerDefeated || outcome == RoomOutcome.Stalemate;

        private static void ValidateScore(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException("Battle search scores and lower bounds must be finite.");
        }

        private sealed class SearchBranch
        {
            public BattleSearchStep Step { get; }
            public BattleTurnState State { get; }
            public RoomOutcome Outcome { get; }
            public int TurnsElapsed { get; }
            public SearchBranch(BattleSearchStep step, BattleTurnState state, RoomOutcome outcome, int turnsElapsed)
            { Step = step; State = state; Outcome = outcome; TurnsElapsed = turnsElapsed; }
        }

        private sealed class SearchLeaf
        {
            public BattleTurnState State { get; }
            public RoomOutcome Outcome { get; }
            public double Score { get; }
            public IReadOnlyList<BattleSearchStep> Steps { get; }
            public SearchLeaf(BattleTurnState state, RoomOutcome outcome, double score, IReadOnlyList<BattleSearchStep> steps)
            { State = state; Outcome = outcome; Score = score; Steps = steps; }
        }

        private sealed class SearchSummary
        {
            private readonly HashSet<string> unsupportedKeys = new HashSet<string>(StringComparer.Ordinal);
            public SearchLeaf? Best { get; set; }
            // Lower bounds apply to terminal and horizon scores only. Best can also report an earlier fallback.
            public double? PruningScore { get; private set; }
            public int NodesVisited { get; set; }
            public int PrunedBranches { get; set; }
            public bool ReachedNodeLimit { get; set; }
            public bool ReachedTurnLimit { get; set; }
            public bool Cancelled { get; set; }
            public List<UnsupportedActionAttempt> UnsupportedAttempts { get; } = new List<UnsupportedActionAttempt>();

            public void Consider(SearchLeaf candidate, bool isObjectiveLeaf = false)
            {
                if (Best == null || candidate.Score < Best.Score) Best = candidate;
                if (isObjectiveLeaf) ConsiderPruningScore(candidate.Score);
            }

            private void ConsiderPruningScore(double score) => PruningScore = Math.Min(PruningScore ?? score, score);

            public void AddUnsupported(UnsupportedActionAttempt attempt)
            {
                PlayCardAction? action = attempt.Action;
                string key = action == null ? "-" : string.Join(",", action.CardInstanceId, action.RoomIndex,
                    action.PlayerPosition, action.TargetUnitId, action.ActivatorUnitId, action.TargetSpawnPointIndex, action.ActivatorRoomIndex, action.Operation);
                if (action != null && action.RecursionSelections.Count > 0)
                    key += "|" + string.Join(";", action.RecursionSelections.Select(selection =>
                        selection.EffectIndex + ":" + string.Join(",", selection.CardInstanceIds)));
                if (action != null && action.MixSelections.Count > 0)
                    key += "|mix:" + string.Join(";", action.MixSelections.Select(selection =>
                        selection.EffectIndex + ":" + selection.MixNumber + ":" + selection.ChoiceIndex));
                if (action != null && action.SummonSelections.Count > 0)
                    key += "|summon:" + string.Join(";", action.SummonSelections.Select(selection => selection.EffectIndex + ":" + selection.CardDataId));
                key += "|" + attempt.Reason;
                if (unsupportedKeys.Add(key)) UnsupportedAttempts.Add(attempt);
            }

            public void Merge(SearchSummary other)
            {
                if (other.Best != null) Consider(other.Best);
                if (other.PruningScore is double score) ConsiderPruningScore(score);
                NodesVisited += other.NodesVisited;
                PrunedBranches += other.PrunedBranches;
                ReachedNodeLimit |= other.ReachedNodeLimit;
                ReachedTurnLimit |= other.ReachedTurnLimit;
                Cancelled |= other.Cancelled;
                foreach (UnsupportedActionAttempt attempt in other.UnsupportedAttempts) AddUnsupported(attempt);
            }
        }
    }
}

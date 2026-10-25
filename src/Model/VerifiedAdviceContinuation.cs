using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    // Created only after replaying the complete available forecast. Every plan
    // can continue across turns while its full state/RNG matches, including an
    // unchanged root after a cosmetic busy boundary. Only the unused preview
    // allocator is normalized; an exhausted or backward prefix cannot match.
    internal sealed class VerifiedAdviceContinuation
    {
        private readonly BattlePlanResult original;
        private readonly string[] roots;
        private readonly string[] fullRoots;
        private readonly int offset;

        internal bool IsWinning => original.Outcome == RoomOutcome.BattleWon;
        internal bool SamePosition(VerifiedAdviceContinuation other) =>
            ReferenceEquals(original, other.original) && offset == other.offset;
        internal static bool CanRetain(BattlePlanResult plan) => plan.Steps.Count > 0 &&
            plan.Outcome is RoomOutcome.Exchanged or RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated or RoomOutcome.Stalemate;

        internal VerifiedAdviceContinuation(BattlePlanResult original, string[] roots, string[] fullRoots)
            : this(original, roots, fullRoots, 0) { }

        private VerifiedAdviceContinuation(BattlePlanResult original, string[] roots, string[] fullRoots, int offset)
        { this.original = original; this.roots = roots; this.fullRoots = fullRoots; this.offset = offset; }

        internal VerifiedAdviceContinuation? Match(BattleTurnState state)
        {
            using var keys = new BattleStateKeys(ignorePreviewCopyAllocator: true);
            return MatchKey(keys.Key(state));
        }

        // Exact settled checkpoints already prove the forward gameplay path.
        // Do not let recorded view callbacks replace that stronger proof, but
        // reject any observed intermediate state outside the verified suffix.
        internal VerifiedAdviceContinuation? MatchObserved(BattleTurnState state,
            IReadOnlyList<SelectionAdviceReuse.Observation> observations, bool exactFinalState)
        {
            using var keys = new BattleStateKeys(ignorePreviewCopyAllocator: true);
            VerifiedAdviceContinuation? matched = this;
            foreach (var observation in observations)
            {
                if (observation.Root == null) continue;
                matched = matched.MatchKey(keys.Key(observation.Root));
                if (matched == null) return null;
            }
            matched = matched.MatchKey(keys.Key(state));
            if (matched == null || !exactFinalState) return matched;
            // Otherwise use selection replay to update the full final snapshot
            // when even the unused preview allocator differs after browsing.
            using var fullKeys = new BattleStateKeys();
            return matched.fullRoots[matched.offset] == fullKeys.Key(state) ? matched : null;
        }

        private VerifiedAdviceContinuation? MatchKey(string key)
        {
            for (int index = offset; index < roots.Length; index++)
                if (roots[index] == key) return new VerifiedAdviceContinuation(original, roots, fullRoots, index);
            return null;
        }

        internal BattlePlanResult Remaining() => Remaining(original, offset);

        internal static BattlePlanResult Remaining(BattlePlanResult original, int offset)
        {
            BattleSearchStep[] steps = original.Steps.Skip(offset).ToArray();
            return new BattlePlanResult(original.State, original.Outcome, steps, original.Score,
                steps.Count(step => step.IsEndTurn), 0, 0, 0, TimeSpan.Zero,
                original.TimedOut, false, original.ReachedNodeLimit, original.ReachedActionLimit,
                original.CompletedHorizon, original.Outcome == RoomOutcome.BattleWon ? Array.Empty<UnsupportedActionAttempt>() : original.UnsupportedAttempts,
                null, Math.Max(0, original.RequestedTurns - original.Steps.Take(offset).Count(step => step.IsEndTurn)),
                original.UsedShorterHorizon, original.SearchedToBattleEnd, original.ReachedStepLimit,
                original.MaxSteps, original.WorkerThreadIds, stoppedEarly: original.StoppedEarly,
                pyreHealthUpperBound: original.PyreHealthUpperBound);
        }
    }
}

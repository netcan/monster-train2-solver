using System;
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
        private readonly int offset;

        internal bool IsWinning => original.Outcome == RoomOutcome.BattleWon;
        internal bool SamePosition(VerifiedAdviceContinuation other) =>
            ReferenceEquals(original, other.original) && offset == other.offset;
        internal static bool CanRetain(BattlePlanResult plan) => plan.Steps.Count > 0 &&
            plan.Outcome is RoomOutcome.Exchanged or RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated or RoomOutcome.Stalemate;

        internal VerifiedAdviceContinuation(BattlePlanResult original, string[] roots)
            : this(original, roots, 0) { }

        private VerifiedAdviceContinuation(BattlePlanResult original, string[] roots, int offset)
        { this.original = original; this.roots = roots; this.offset = offset; }

        internal VerifiedAdviceContinuation? Match(BattleTurnState state)
        {
            using var keys = new BattleStateKeys(ignorePreviewCopyAllocator: true);
            string key = keys.Key(state);
            for (int index = offset; index < roots.Length; index++)
                if (roots[index] == key) return new VerifiedAdviceContinuation(original, roots, index);
            return null;
        }

        internal BattlePlanResult Remaining()
        {
            BattleSearchStep[] steps = original.Steps.Skip(offset).ToArray();
            return new BattlePlanResult(original.State, original.Outcome, steps, original.Score,
                steps.Count(step => step.IsEndTurn), 0, 0, 0, TimeSpan.Zero,
                original.TimedOut, false, original.ReachedNodeLimit, original.ReachedActionLimit,
                original.CompletedHorizon, IsWinning ? Array.Empty<UnsupportedActionAttempt>() : original.UnsupportedAttempts,
                null, Math.Max(0, original.RequestedTurns - original.Steps.Take(offset).Count(step => step.IsEndTurn)),
                original.UsedShorterHorizon, original.SearchedToBattleEnd, original.ReachedStepLimit,
                original.MaxSteps, original.WorkerThreadIds);
        }
    }
}

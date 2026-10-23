using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    // Created only after replaying a complete forecast. Ordinary plans can
    // advance within this turn, preserving their original lookahead; wins can
    // continue across turns. Only the unused preview allocator is normalized.
    internal sealed class VerifiedAdviceContinuation
    {
        private readonly BattlePlanResult original;
        private readonly string[] roots;
        private readonly int offset;

        internal bool IsWinning => original.Outcome == RoomOutcome.BattleWon;
        internal static bool CanRetain(BattlePlanResult plan) => plan.Outcome is RoomOutcome.BattleWon or RoomOutcome.Exchanged;

        internal VerifiedAdviceContinuation(BattlePlanResult original, string[] roots)
            : this(original, roots, 0) { }

        private VerifiedAdviceContinuation(BattlePlanResult original, string[] roots, int offset)
        { this.original = original; this.roots = roots; this.offset = offset; }

        internal VerifiedAdviceContinuation? Match(BattleTurnState state)
        {
            using var keys = new BattleStateKeys(ignorePreviewCopyAllocator: true);
            string key = keys.Key(state);
            for (int index = IsWinning ? offset : offset + 1; index < roots.Length; index++)
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
                null, original.RequestedTurns,
                original.UsedShorterHorizon);
        }
    }
}

using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    // Created only after replaying a complete win. Keep one immutable line and
    // its state/RNG keys; a later publication can only move forward on it.
    // The unused UI preview-copy allocator is the sole normalized property.
    internal sealed class WinningAdviceContinuation
    {
        private readonly BattlePlanResult original;
        private readonly string[] roots;
        private readonly int offset;

        internal WinningAdviceContinuation(BattlePlanResult original, string[] roots)
            : this(original, roots, 0) { }

        private WinningAdviceContinuation(BattlePlanResult original, string[] roots, int offset)
        { this.original = original; this.roots = roots; this.offset = offset; }

        internal WinningAdviceContinuation? Match(BattleTurnState state)
        {
            using var keys = new BattleStateKeys(ignorePreviewCopyAllocator: true);
            string key = keys.Key(state);
            for (int index = offset; index < roots.Length; index++)
                if (roots[index] == key) return new WinningAdviceContinuation(original, roots, index);
            return null;
        }

        internal BattlePlanResult Remaining()
        {
            BattleSearchStep[] steps = original.Steps.Skip(offset).ToArray();
            return new BattlePlanResult(original.State, RoomOutcome.BattleWon, steps, original.Score,
                steps.Count(step => step.IsEndTurn), 0, 0, 0, TimeSpan.Zero,
                false, false, false, false, true, Array.Empty<UnsupportedActionAttempt>(), null);
        }
    }
}

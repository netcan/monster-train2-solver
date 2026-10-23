using System;
using System.Collections.Generic;
using System.Threading;

namespace MonsterTrain2Poju.Model
{
    // Browsing a floor is not a gameplay input. Keep all costs, histories,
    // existing preview identities and RNG keyed; only the known UI selection
    // and its next unused preview-copy number can differ. A retained line must
    // still reproduce its complete outcome from the newly captured live root.
    internal static class SelectionAdviceReuse
    {
        internal static bool SameBattle(BattleTurnState left, BattleTurnState right)
        {
            using var keys = new BattleStateKeys(true, ignoreRoomSelection: true);
            return keys.Key(left) == keys.Key(right);
        }

        internal static bool TryRebase(BattleAdvice advice, BattleTurnState root, bool retainWinningPlans,
            CancellationToken token, out BattlePlanResult plan, out WinningAdviceContinuation? proof)
        {
            plan = advice.Plan; proof = null;
            if (!SameBattle(advice.Root, root)) return false;
            BattleTurnState state = root;
            RoomOutcome outcome = RoomOutcome.Exchanged;
            using var continuationKeys = new BattleStateKeys(ignorePreviewCopyAllocator: true);
            var roots = retainWinningPlans && plan.Outcome == RoomOutcome.BattleWon ? new List<string>() : null;
            foreach (BattleSearchStep step in plan.Steps)
            {
                token.ThrowIfCancellationRequested();
                roots?.Add(continuationKeys.Key(state));
                if (step.IsEndTurn)
                {
                    BattleTurnResult next = BattleTurnModel.EndTurn(state);
                    if (!next.Supported) return false;
                    state = next.State!; outcome = next.Outcome;
                }
                else
                {
                    if (step.Action == null) return false;
                    BattleActionResult next = BattleActionModel.PlayCard(state, step.Action);
                    if (!next.Supported) return false;
                    state = next.State!; outcome = next.Outcome;
                }
            }
            if (outcome != plan.Outcome || !SameBattle(state, plan.State)) return false;
            plan = new BattlePlanResult(state, outcome, plan.Steps, plan.Score, plan.TurnsCompleted,
                0, 0, 0, TimeSpan.Zero, plan.TimedOut, false, plan.ReachedNodeLimit, plan.ReachedActionLimit,
                plan.CompletedHorizon, plan.UnsupportedAttempts, null, plan.RequestedTurns, plan.UsedShorterHorizon);
            if (roots != null) proof = new WinningAdviceContinuation(plan, roots.ToArray());
            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;

namespace MonsterTrain2Poju.Model
{
    // Browsing a floor is not a gameplay input. Keep costs, histories, existing
    // preview identities and RNG keyed; prove any captured selection callbacks
    // through the model. A retained line must still reproduce its complete
    // outcome from the newly captured live root.
    internal static class SelectionAdviceReuse
    {
        // UI-owned ordered history since the last accepted publication. Roots
        // are detached immutable checkpoints; a worker receives its own array.
        internal sealed class Observation
        {
            internal readonly int Room;
            internal readonly BattleTurnState? Root;
            internal Observation(int room) { Room = room; }
            internal Observation(BattleTurnState root) { Room = -1; Root = root; }
        }

        // One cache belongs to one verified suffix and is only accessed by its
        // session worker. Every root field, including the allocator, is keyed:
        // a different allocator may change the strict replayed final snapshot.
        internal sealed class Result
        {
            internal readonly BattlePlanResult Plan;
            internal readonly VerifiedAdviceContinuation? Proof;
            internal Result(BattlePlanResult plan, VerifiedAdviceContinuation? proof)
            { Plan = plan; Proof = proof; }
        }

        internal static bool SameBattle(BattleTurnState left, BattleTurnState right)
        {
            using var keys = new BattleStateKeys(true, ignoreRoomSelection: true);
            if (keys.Key(left) == keys.Key(right)) return true;
            if (left.SelectedRoom < 0 || right.SelectedRoom < 0 || left.SelectedRoom == right.SelectedRoom)
                return false;
            // Native selection callbacks update Conduit/room spell upgrades and
            // their private trackers. Prove that complete transition instead of
            // ignoring these gameplay fields or treating them as a new input.
            BattleTurnResult selected = BattleTurnModel.SelectRoom(left, right.SelectedRoom);
            if (!selected.Supported) return false;
            using var selectedKeys = new BattleStateKeys(ignorePreviewCopyAllocator: true);
            return selectedKeys.Key(selected.State!) == selectedKeys.Key(right);
        }

        internal static bool TryRebase(BattleAdvice advice, BattleTurnState root, bool retainPlans,
            CancellationToken token, out BattlePlanResult plan, out VerifiedAdviceContinuation? proof)
        {
            plan = advice.Plan; proof = null;
            using var selectionKeys = new BattleStateKeys();
            string key = selectionKeys.Key(root);
            if (advice.SelectionCache.TryGetValue(key, out Result? cached))
            { plan = Reused(cached.Plan, cached.Plan.State); proof = cached.Proof; return true; }
            string originalKey = selectionKeys.Key(advice.Root);
            advice.SelectionCache[originalKey] = new Result(advice.Plan, advice.Continuation);
            if (key == originalKey)
            { plan = Reused(advice.Plan, advice.Plan.State); proof = advice.Continuation; return true; }
            if (!SameBattle(advice.Root, root)) return false;
            return Replay(advice.Plan, root, retainPlans, token, out plan, out proof, advice.SelectionCache, key);
        }

        // Called after an actual input, never by a floor-view event. Reproduce
        // every recorded native selection callback before matching the next
        // complete decision/RNG against the original line's prefixes.
        internal static VerifiedAdviceContinuation? MatchAfterObservations(BattleAdvice advice, IReadOnlyList<Observation> observations,
            BattleTurnState actual, CancellationToken token)
        {
            BattleTurnState projected = advice.Root;
            int index = 0;
            using var keys = new BattleStateKeys(ignorePreviewCopyAllocator: true);
            foreach (Observation observation in observations)
            {
                token.ThrowIfCancellationRequested();
                if (observation.Root != null)
                {
                    if (!AdvanceTo(observation.Root)) return null;
                    // Continue from the exact observed allocator, rather than
                    // from the normalized model prefix used for matching.
                    projected = observation.Root;
                }
                else
                {
                    BattleTurnResult selected = BattleTurnModel.SelectRoom(projected, observation.Room);
                    if (!selected.Supported) return null;
                    projected = selected.State!;
                }
            }
            if (!AdvanceTo(actual)) return null;
            BattlePlanResult remaining = VerifiedAdviceContinuation.Remaining(advice.Plan, index);
            // Reproduce the complete suffix from the latest actual root. Never
            // publish an old final snapshot with a different preview allocator.
            return Replay(remaining, actual, true, token, out _, out VerifiedAdviceContinuation? proof) ? proof : null;

            bool AdvanceTo(BattleTurnState checkpoint)
            {
                string actualKey = keys.Key(checkpoint);
                while (index < advice.Plan.Steps.Count)
                {
                    token.ThrowIfCancellationRequested();
                    if (keys.Key(projected) == actualKey) return true;
                    BattleSearchStep step = advice.Plan.Steps[index++];
                    if (step.IsEndTurn)
                    {
                        BattleTurnResult next = BattleTurnModel.EndTurn(projected);
                        if (!next.Supported) return false;
                        projected = next.State!;
                    }
                    else
                    {
                        if (step.Action == null) return false;
                        BattleActionResult next = BattleActionModel.PlayCard(projected, step.Action);
                        if (!next.Supported) return false;
                        projected = next.State!;
                    }
                }
                return false;
            }
        }

        private static bool Replay(BattlePlanResult original, BattleTurnState root, bool retainPlans,
            CancellationToken token, out BattlePlanResult plan, out VerifiedAdviceContinuation? proof,
            Dictionary<string, Result>? cache = null, string? key = null)
        {
            plan = original; proof = null;
            BattleTurnState state = root;
            RoomOutcome outcome = RoomOutcome.Exchanged;
            using var continuationKeys = new BattleStateKeys(ignorePreviewCopyAllocator: true);
            var roots = retainPlans && VerifiedAdviceContinuation.CanRetain(plan) ? new List<string>() : null;
            using var fullKeys = new BattleStateKeys();
            var fullRoots = roots == null ? null : new List<string>();
            foreach (BattleSearchStep step in plan.Steps)
            {
                token.ThrowIfCancellationRequested();
                roots?.Add(continuationKeys.Key(state));
                fullRoots?.Add(fullKeys.Key(state));
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
            plan = Reused(plan, state);
            if (roots != null) proof = new VerifiedAdviceContinuation(plan, roots.ToArray(), fullRoots!.ToArray());
            // Preview allocators can advance on every browse. Bound retained
            // calibration graphs even if none of those exact roots repeat.
            if (cache != null && key != null)
            {
                if (cache.Count >= 8) cache.Clear();
                cache[key] = new Result(plan, proof);
            }
            return true;
        }

        private static BattlePlanResult Reused(BattlePlanResult plan, BattleTurnState state) =>
            new BattlePlanResult(state, plan.Outcome, plan.Steps, plan.Score, plan.TurnsCompleted,
                0, 0, 0, TimeSpan.Zero, plan.TimedOut, false, plan.ReachedNodeLimit, plan.ReachedActionLimit,
                plan.CompletedHorizon, plan.UnsupportedAttempts, null, plan.RequestedTurns, plan.UsedShorterHorizon,
                plan.SearchedToBattleEnd, plan.ReachedStepLimit, plan.MaxSteps, plan.WorkerThreadIds,
                stoppedEarly: plan.StoppedEarly, pyreHealthUpperBound: plan.PyreHealthUpperBound);
    }
}

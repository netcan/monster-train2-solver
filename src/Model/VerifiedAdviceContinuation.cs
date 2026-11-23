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
        private readonly (string? Actor, string? Target)[] unitKeys;
        private readonly int offset;

        internal bool IsWinning => original.Outcome == RoomOutcome.BattleWon;
        internal int ReusedVerifiedSteps { get; }
        internal bool SamePosition(VerifiedAdviceContinuation other) =>
            ReferenceEquals(original, other.original) && offset == other.offset;
        internal static bool CanRetain(BattlePlanResult plan) => plan.Steps.Count > 0 &&
            plan.Outcome is RoomOutcome.Exchanged or RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated or RoomOutcome.Stalemate;

        internal VerifiedAdviceContinuation(BattlePlanResult original, string[] roots, string[] fullRoots,
            (string? Actor, string? Target)[] unitKeys)
            : this(original, roots, fullRoots, unitKeys, 0) { }

        private VerifiedAdviceContinuation(BattlePlanResult original, string[] roots, string[] fullRoots,
            (string? Actor, string? Target)[] unitKeys, int offset,
            int reusedVerifiedSteps = 0)
        {
            this.original = original; this.roots = roots; this.fullRoots = fullRoots; this.unitKeys = unitKeys; this.offset = offset;
            ReusedVerifiedSteps = reusedVerifiedSteps;
        }

        internal (string? Actor, string? Target) UnitKeys(int stepIndex) => unitKeys[offset + stepIndex];

        internal static (string? Actor, string? Target) CaptureUnitKeys(BattleTurnState state, BattleSearchStep step)
        {
            PlayCardAction? action = step.Action;
            if (action == null) return default;
            return (Key(action.ActivatorUnitId), Key(action.TargetUnitId));

            string? Key(int id) => id <= 0 ? null : state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .FirstOrDefault(unit => unit.Id == id)?.AssetKey;
        }

        internal VerifiedAdviceContinuation? ForReplay(BattlePlanResult plan)
        {
            int start = original.Steps.Count - plan.Steps.Count;
            if (start < offset || !ReferenceEquals(original.State, plan.State) || original.Outcome != plan.Outcome ||
                original.Score != plan.Score ||
                original.CompletedHorizon != plan.CompletedHorizon || original.UsedShorterHorizon != plan.UsedShorterHorizon ||
                original.SearchedToBattleEnd != plan.SearchedToBattleEnd || original.MaxSteps != plan.MaxSteps ||
                Math.Max(0, original.RequestedTurns - original.Steps.Take(start).Count(step => step.IsEndTurn)) != plan.RequestedTurns)
                return null;
            for (int i = 0; i < plan.Steps.Count; i++)
                if (!ReferenceEquals(original.Steps[start + i], plan.Steps[i])) return null;
            return new VerifiedAdviceContinuation(original, roots, fullRoots, unitKeys, start);
        }

        // The prefix has already been replayed from the actual root. Joining
        // the verified suffix requires the complete state at the same action.
        internal VerifiedAdviceContinuation? JoinAt(BattlePlanResult plan, string fullKey,
            List<string> prefix, List<string> fullPrefix, List<(string? Actor, string? Target)> unitPrefix)
        {
            int index = offset + prefix.Count;
            if (fullRoots[index] != fullKey) return null;
            int reused = fullRoots.Length - index;
            string[] joined = new string[plan.Steps.Count], fullJoined = new string[plan.Steps.Count];
            var unitJoined = new (string? Actor, string? Target)[plan.Steps.Count];
            prefix.CopyTo(joined);
            fullPrefix.CopyTo(fullJoined);
            unitPrefix.CopyTo(unitJoined);
            Array.Copy(roots, index, joined, prefix.Count, reused);
            Array.Copy(fullRoots, index, fullJoined, fullPrefix.Count, reused);
            Array.Copy(unitKeys, index, unitJoined, unitPrefix.Count, reused);
            return new VerifiedAdviceContinuation(plan, joined, fullJoined, unitJoined, 0, reused);
        }

        internal VerifiedAdviceContinuation? Match(BattleTurnState state)
        {
            using var keys = new BattleStateKeys(ignorePreviewCopyAllocator: true);
            return MatchKey(keys.Key(state));
        }

        // Exact settled checkpoints already prove the forward gameplay path.
        // Do not let recorded view callbacks replace that stronger proof, but
        // reject any observed intermediate state outside the verified suffix.
        internal VerifiedAdviceContinuation? MatchObserved(BattleTurnState state,
            IReadOnlyList<SelectionAdviceReuse.Observation> observations, string? requiredFullKey)
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
            if (matched == null || requiredFullKey == null) return matched;
            // Otherwise use selection replay to update the full final snapshot
            // when even the unused preview allocator differs after browsing.
            return matched.fullRoots[matched.offset] == requiredFullKey ? matched : null;
        }

        private VerifiedAdviceContinuation? MatchKey(string key)
        {
            for (int index = offset; index < roots.Length; index++)
                if (roots[index] == key) return new VerifiedAdviceContinuation(original, roots, fullRoots, unitKeys, index);
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

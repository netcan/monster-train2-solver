using System.Collections.Generic;

namespace MonsterTrain2Poju.Model
{
    // This only controls a visibly unverified UI hint, never the retained proof.
    internal static class TentativeAdviceProgress
    {
        internal static bool Matches(IReadOnlyList<BattleSearchStep> steps, int offset, int cardId,
            int room, int dropPosition, int targetId, bool directHandPlay)
        {
            if (offset + 1 >= steps.Count || !directHandPlay) return false;
            PlayCardAction? action = MatchedCard(steps, offset, cardId, room, dropPosition, targetId);
            return action != null && action.RecursionSelections.Count == 0 &&
                action.MixSelections.Count == 0 && action.SummonSelections.Count == 0;
        }

        internal static PlayCardAction? MatchedCard(IReadOnlyList<BattleSearchStep> steps, int offset, int cardId,
            int room, int dropPosition, int targetId)
        {
            if (offset < 0 || offset >= steps.Count) return null;
            PlayCardAction? action = steps[offset].Action;
            if (steps[offset].IsEndTurn || action == null || action.CardInstanceId != cardId || action.RoomIndex != room ||
                action.ActivatorUnitId != 0 || action.ActivatorRoomIndex.HasValue ||
                action.TargetSpawnPointIndex >= 0) return null;
            if (action.PlayerPosition >= 0)
                return action.TargetUnitId == 0 && action.PlayerPosition == dropPosition ? action : null;
            return action.TargetUnitId == targetId ? action : null;
        }

        internal static SummonChoiceSelection? MatchedSummonAbility(IReadOnlyList<BattleSearchStep> steps, int offset, int cardId,
            int actorId, int room, int dropPosition)
        {
            if (offset < 0 || offset >= steps.Count || cardId <= 0 || actorId <= 0 || room < 0 || dropPosition < 0) return null;
            PlayCardAction? action = steps[offset].Action;
            return !steps[offset].IsEndTurn && action is
                { TargetUnitId: 0, ActivatorRoomIndex: null, RecursionSelections.Count: 0, MixSelections.Count: 0,
                    SummonSelections: [var summon] } && action.CardInstanceId == cardId && action.ActivatorUnitId == actorId &&
                action.RoomIndex == room && action.TargetSpawnPointIndex == dropPosition ? summon : null;
        }

        internal static PlayCardAction? MatchedMixAbility(IReadOnlyList<BattleSearchStep> steps, int offset, int cardId,
            int actorId, int room, int dropPosition, int targetId)
        {
            if (offset < 0 || offset >= steps.Count || cardId <= 0 || actorId <= 0 || room < 0 || targetId < 0) return null;
            PlayCardAction? action = steps[offset].Action;
            return !steps[offset].IsEndTurn && action is
                { ActivatorRoomIndex: null, PlayerPosition: -1, RecursionSelections.Count: 0, MixSelections.Count: > 0,
                    SummonSelections.Count: 0 } && action.CardInstanceId == cardId && action.ActivatorUnitId == actorId &&
                action.RoomIndex == room && action.TargetSpawnPointIndex == dropPosition && action.TargetUnitId == targetId
                ? action : null;
        }
    }
}

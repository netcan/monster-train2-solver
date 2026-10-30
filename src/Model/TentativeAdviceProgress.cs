using System.Collections.Generic;

namespace MonsterTrain2Poju.Model
{
    // This only controls a visibly unverified UI hint, never the retained proof.
    internal static class TentativeAdviceProgress
    {
        internal static bool Matches(IReadOnlyList<BattleSearchStep> steps, int offset, int cardId,
            int room, int dropPosition, int targetId, bool directHandPlay)
        {
            if (!directHandPlay || offset < 0 || offset + 1 >= steps.Count) return false;
            BattleSearchStep step = steps[offset];
            PlayCardAction? action = step.Action;
            if (step.IsEndTurn || action == null || action.CardInstanceId != cardId || action.RoomIndex != room ||
                action.ActivatorUnitId != 0 || action.ActivatorRoomIndex.HasValue ||
                action.RecursionSelections.Count != 0 || action.MixSelections.Count != 0 ||
                action.TargetSpawnPointIndex >= 0) return false;
            if (action.PlayerPosition >= 0)
                return action.TargetUnitId == 0 && action.PlayerPosition == dropPosition;
            return action.TargetUnitId == targetId;
        }
    }
}

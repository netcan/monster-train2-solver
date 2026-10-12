using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class UnitTeleportModel
    {
        internal static bool CanPlay(TrainCombatState source, int actorId, int roomIndex, int targetSpawnPointIndex)
        {
            CombatContext? context = source.Context;
            if (actorId <= 0 || targetSpawnPointIndex < 0 || context?.SpawnPoints == null) return false;
            RoomCombatState? actorRoom = source.Rooms.FirstOrDefault(room => room.Units.Any(unit => unit.Id == actorId));
            CombatUnit? actor = actorRoom?.Units.FirstOrDefault(unit => unit.Id == actorId);
            SpawnPointReference? current = context.SpawnPoints.Units.SingleOrDefault(unit => unit.UnitId == actorId)?.Current;
            SpawnPointGroupState? destination = context.SpawnPoints.Group(roomIndex, CombatTeam.Player);
            if (actor?.Team != CombatTeam.Player || actorRoom == null || current == null || destination == null ||
                targetSpawnPointIndex >= destination.GroupCount || destination.Outside[targetSpawnPointIndex]) return false;
            if (actorRoom.RoomIndex != roomIndex) return true;
            return TryGoalIndex(context.SpawnPoints, actorId, roomIndex, targetSpawnPointIndex, out int goalIndex) &&
                goalIndex != current.Index;
        }

        internal static bool TryGoalIndex(BattleSpawnPoints points, int actorId, int roomIndex,
            int targetSpawnPointIndex, out int goalIndex)
        {
            goalIndex = -1;
            SpawnPointReference? current = points.Units.SingleOrDefault(unit => unit.UnitId == actorId)?.Current;
            SpawnPointGroupState? destination = points.Group(roomIndex, CombatTeam.Player);
            if (current == null || current.RoomIndex != roomIndex || destination == null || targetSpawnPointIndex < 0 ||
                targetSpawnPointIndex >= destination.GroupCount || destination.Outside[targetSpawnPointIndex]) return false;
            goalIndex = targetSpawnPointIndex;
            int occupied = destination.Occupants.Count(id => id != 0 &&
                points.Units.Single(unit => unit.UnitId == id).OuterBoss != true);
            if (destination.GroupCount - occupied > 0 && goalIndex > current.Index) goalIndex--;
            return true;
        }
    }
}

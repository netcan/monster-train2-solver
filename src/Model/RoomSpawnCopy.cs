using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RoomSpawnCopyModel
    {
        internal static bool Active(CombatContext? context, int roomIndex) => context?.RoomCapacities?
            .FirstOrDefault(room => room.RoomIndex == roomIndex)?.Attachments?.Any(attachment => attachment.SpawnCopySubtype != null) == true;

        internal static UnitCloneResult PaidBirth(TrainCombatState source, int unitId, UnitCopyCatalog? catalog)
        {
            RoomCombatState? room = source.Rooms.SingleOrDefault(room => room.Units.Any(unit => unit.Id == unitId));
            CombatUnit? born = room?.Units.Single(unit => unit.Id == unitId);
            if (room == null || born == null || !Active(source.Context, room.RoomIndex) || born.Modifiers?.IsClone == true || born.Health <= 0)
                return new UnitCloneResult(source, 0, RoomOutcome.Exchanged, null, Array.Empty<CombatEvent>(),
                    Array.Empty<UnitCloneBoundary>(), Array.Empty<RoomCombatModel.QueuedCharacterTrigger>());
            SpawnPointReference? point = source.Context!.SpawnPoints?.Units.Single(unit => unit.UnitId == unitId).Current;
            UnitCloneResult cloned = UnitCloneModel.ApplyWithPending(source, unitId, room.RoomIndex, point,
                catalog?.Resolve(source.Context, born), Array.Empty<RoomCombatModel.QueuedCharacterTrigger>());
            if (!cloned.Supported || cloned.UnitId == 0) return cloned;
            var pending = cloned.PendingCallbacks.Concat(room.Units.Where(unit => unit.Id != unitId).Select(unit =>
                new RoomCombatModel.QueuedCharacterTrigger(room.RoomIndex, unit, "CardMonsterPlayed"))).ToArray();
            return UnitCloneModel.Drain(new UnitCloneResult(cloned.State, cloned.UnitId, cloned.Outcome, null,
                cloned.Events, cloned.Boundaries, pending, cloned.Dispatched));
        }
    }
}

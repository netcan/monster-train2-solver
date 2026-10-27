using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RoomSpawnCopyModel
    {
        internal static bool Active(CombatContext? context, int roomIndex) => context?.RoomCapacities?
            .FirstOrDefault(room => room.RoomIndex == roomIndex)?.Attachments?.Any(attachment => attachment.SpawnCopySubtype != null) == true;
        internal static bool Active(RoomCombatState room) => Active(room.Context, room.RoomIndex) || room.Units.Any(unit =>
            unit.Team == CombatTeam.Player && unit.Health > 0 && unit.DeathState?.IsDestroyed != true && unit.Modifiers?.SpawnCopySubtype != null);

        internal static bool OriginalMirrorsOnly(RoomCombatState room) =>
            room.Units.All(unit => unit.Modifiers?.SpawnCopySubtype == null) &&
            room.Context?.RoomCapacities?.FirstOrDefault(capacity => capacity.RoomIndex == room.RoomIndex)?.Attachments?
                .Where(attachment => attachment.SpawnCopySubtype != null).All(attachment =>
                    attachment is { DataId: "83734ffd-cbaa-47f1-9809-cc4c4dac568d", AssetKey: "HallOfMirrors", SpawnCopySubtype: "" }) == true;

        internal static UnitCloneResult ExistingHorde(TrainCombatState source, int unitId)
        {
            RoomCombatState room = source.Rooms.Single(room => room.Units.Any(unit => unit.Id == unitId));
            CombatUnit target = room.Units.Single(unit => unit.Id == unitId);
            if (Active(room) && target.Modifiers?.IsClone != true &&
                (target is not { AssetKey: "MonsterFunguy", HordeDefinition.Attack: 3, HordeDefinition.Health: 4 } ||
                    target.Status("horde") == null || !OriginalMirrorsOnly(room)))
                return new UnitCloneResult(null, 0, RoomOutcome.Unsupported,
                    "Spawn-or-status room copying requires its calibrated original Funguy Horde and Hall of Mirrors.",
                    Array.Empty<CombatEvent>(), Array.Empty<UnitCloneBoundary>(), Array.Empty<RoomCombatModel.QueuedCharacterTrigger>());
            return PaidBirth(source, unitId, null);
        }

        internal static UnitCloneResult PaidBirth(TrainCombatState source, int unitId, UnitCopyCatalog? catalog)
        {
            RoomCombatState? room = source.Rooms.SingleOrDefault(room => room.Units.Any(unit => unit.Id == unitId));
            CombatUnit? born = room?.Units.Single(unit => unit.Id == unitId);
            if (room == null || born == null || !Active(room) || born.Modifiers?.IsClone == true || born.Health <= 0)
                return new UnitCloneResult(source, 0, RoomOutcome.Exchanged, null, Array.Empty<CombatEvent>(),
                    Array.Empty<UnitCloneBoundary>(), Array.Empty<RoomCombatModel.QueuedCharacterTrigger>());
            // MonsterManager snapshots active monsters in creation order before
            // their OnOtherCharacterSpawned callbacks, then visits room attachments.
            var modifiers = room.Units.Where(unit => unit.Id != unitId && unit.Team == CombatTeam.Player &&
                unit.Health > 0 && unit.DeathState?.IsDestroyed != true && unit.Modifiers?.SpawnCopySubtype != null)
                .OrderBy(unit => unit.Id).Select(unit => (OwnerId: unit.Id, Subtype: unit.Modifiers!.SpawnCopySubtype!))
                .Concat(source.Context!.RoomCapacities?.FirstOrDefault(capacity => capacity.RoomIndex == room.RoomIndex)?.Attachments?
                    .Where(attachment => attachment.SpawnCopySubtype != null).Select(attachment => (OwnerId: 0, Subtype: attachment.SpawnCopySubtype!)) ??
                    Array.Empty<(int OwnerId, string Subtype)>()).ToArray();
            var result = new UnitCloneResult(source, 0, RoomOutcome.Exchanged, null, Array.Empty<CombatEvent>(),
                Array.Empty<UnitCloneBoundary>(), Array.Empty<RoomCombatModel.QueuedCharacterTrigger>());
            foreach (var modifier in modifiers)
            {
                string subtype = modifier.Subtype;
                if (modifier.OwnerId > 0 && !result.State!.Rooms.SelectMany(room => room.Units).Any(unit =>
                    unit.Id == modifier.OwnerId && unit.Health > 0 && unit.DeathState?.IsDestroyed != true)) continue;
                if (subtype.Length > 0 && !born.Subtypes.Contains(subtype) && source.Context.Relics?.Any(relic =>
                    relic.EffectTypes.Contains("RelicEffectMonstersAreAllSubtypes")) != true) continue;
                UnitCloneResult copied = CopyOnce(result.State!, unitId, catalog);
                if (!copied.Supported) return copied;
                result = new UnitCloneResult(copied.State, copied.UnitId, copied.Outcome == RoomOutcome.Exchanged ? result.Outcome : copied.Outcome,
                    null, result.Events.Concat(copied.Events).ToArray(), result.Boundaries.Concat(copied.Boundaries).ToArray(),
                    copied.PendingCallbacks, result.Dispatched.Concat(copied.Dispatched).ToArray());
            }
            return result;
        }
        private static UnitCloneResult CopyOnce(TrainCombatState source, int unitId, UnitCopyCatalog? catalog)
        {
            RoomCombatState room = source.Rooms.Single(room => room.Units.Any(unit => unit.Id == unitId));
            CombatUnit born = room.Units.Single(unit => unit.Id == unitId);
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

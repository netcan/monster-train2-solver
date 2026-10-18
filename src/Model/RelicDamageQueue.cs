using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicDamageQueueModel
    {
        internal static bool Settle(ref TrainCombatState state,
            List<RoomCombatModel.QueuedCharacterTrigger> pending, List<CombatEvent> events,
            List<CharacterTriggerDispatch> dispatches, List<UnitCloneCallback> dispatched,
            ref RoomOutcome outcome, out string? error, bool deferPhysicalRemoval = false)
        {
            error = null;
            if (pending.Count > 0)
            {
                UnitCloneResult drained = UnitCloneModel.Drain(new UnitCloneResult(state, 0, outcome, null,
                    Array.Empty<CombatEvent>(), Array.Empty<UnitCloneBoundary>(), pending), deferPhysicalRemoval);
                if (!drained.Supported) { error = drained.UnsupportedReason; return false; }
                state = drained.State!; pending.Clear(); events.AddRange(drained.Events);
                dispatched.AddRange(drained.Dispatched);
                dispatches.AddRange(drained.Dispatched.Select(ToDispatch));
                if (drained.Outcome != RoomOutcome.Exchanged) outcome = drained.Outcome;
            }

            foreach (RoomCombatState room in state.Rooms.ToArray())
            foreach (CombatUnit dead in room.Units.Where(unit => unit.Health <= 0 || unit.DeathState?.IsBeingRemoved == true).ToArray())
            {
                UnitSpawnPointState? point = state.Context?.SpawnPoints?.Units.FirstOrDefault(item => item.UnitId == dead.Id);
                if (point?.Current == null) continue;
                RoomCombatState current = state.Rooms.Single(item => item.RoomIndex == room.RoomIndex);
                SpawnPointGroupState? group = state.Context!.SpawnPoints!.SelectedGroup(room.RoomIndex, dead.Team);
                bool vacancyBefore = point.Current.RoomIndex == room.RoomIndex && point.Current.Team == dead.Team &&
                    point.Current.Index > 0 && group?.Occupants.Take(point.Current.Index).Any(id => id == 0) == true;
                if (vacancyBefore)
                {
                    BattleSpawnPointResult compacted = BattleSpawnPointModel.Apply(state.Context.SpawnPoints, current,
                        "Compact", dead.Team);
                    if (!compacted.Supported) { error = compacted.Error; return false; }
                    state = CardSpellModel.WithContext(state, state.Context.WithSpawnPoints(compacted.State!));
                    current = state.Rooms.Single(item => item.RoomIndex == room.RoomIndex);
                }
                RoomCombatResult removed = RoomCombatModel.SettleQueuedSpawner(current, dead, preservePhysicalPoint: deferPhysicalRemoval);
                if (!removed.Supported) { error = removed.UnsupportedReason; return false; }
                Import(ref state, removed.State!); events.AddRange(removed.Events);
            }

            var livingEnemies = state.Rooms.SelectMany(room => room.Units)
                .Where(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0 && unit.DeathState?.IsDestroyed != true)
                .Select(unit => unit.Id).ToHashSet();
            if (state.Movement.Any(rule => !livingEnemies.Contains(rule.UnitId)))
                state = new TrainCombatState(state.Rooms, state.Movement.Where(rule => livingEnemies.Contains(rule.UnitId)).ToArray(),
                    state.EnemySlotsPerRoom, state.Context);
            return true;
        }

        private static CharacterTriggerDispatch ToDispatch(UnitCloneCallback callback) => new CharacterTriggerDispatch(
            callback.ActorId, callback.Kind, callback.DyingId, callback.TriggerCount, callback.ParamInt,
            callback.ParamInt2, callback.ParamString, callback.OverrideTargetId, callback.LastSpawnedOverrideUnitId,
            callback.ExclusiveTriggerStateId);

        internal static void Import(ref TrainCombatState state, RoomCombatState changed)
        {
            CombatContext context = changed.Context!;
            RoomCombatState[] rooms = state.Rooms.Select(room => room.RoomIndex == changed.RoomIndex ? changed :
                new RoomCombatState(room.RoomIndex, room.Deployment, room.Units, room.ExternalInteractions,
                    context, room.Preview, room.ActiveCompanionBossIds)).ToArray();
            state = new TrainCombatState(rooms, state.Movement, state.EnemySlotsPerRoom, context);
        }
    }
}

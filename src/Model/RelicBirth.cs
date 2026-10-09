using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicBirthResult
    {
        public TrainCombatState? State { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => State != null;
        public IReadOnlyList<UnitCloneCallback> Queued { get; }
        public IReadOnlyList<UnitCloneCallback> Dispatched { get; }
        public IReadOnlyList<CombatEvent> Events { get; }
        internal RelicBirthResult(TrainCombatState? state, string? error, IReadOnlyList<UnitCloneCallback> queued,
            IReadOnlyList<UnitCloneCallback> dispatched, IReadOnlyList<CombatEvent> events)
        { State = state; UnsupportedReason = error; Queued = Array.AsReadOnly(queued.ToArray());
            Dispatched = Array.AsReadOnly(dispatched.ToArray()); Events = Array.AsReadOnly(events.ToArray()); }
    }

    public static class RelicBirthModel
    {
        public static RelicBirthResult CharacterAdded(TrainCombatState source, int unitId, int fromCardId, bool onlyCovenants,
            bool deferCallbacks, IReadOnlyList<UnitCloneCallback> queued)
        {
            TrainCombatState state = source;
            var events = new List<CombatEvent>(); var dispatched = new List<UnitCloneCallback>();
            RoomCombatState? room = source.Rooms.FirstOrDefault(item => item.Units.Any(unit => unit.Id == unitId));
            if (room == null) return Fail("Missing relic birth room.");
            var pending = new List<RoomCombatModel.QueuedCharacterTrigger>();
            foreach (UnitCloneCallback item in queued)
            {
                RoomCombatState? actorRoom = source.Rooms.FirstOrDefault(frame => frame.Units.Any(unit => unit.Id == item.ActorId));
                CombatUnit? dying = Find(item.DyingId), target = Find(item.OverrideTargetId);
                if (actorRoom == null || item.DyingId != 0 && dying == null || item.OverrideTargetId != 0 && target == null)
                    return Fail("Missing retained native relic birth queue actor.");
                pending.Add(new RoomCombatModel.QueuedCharacterTrigger(actorRoom.RoomIndex,
                    actorRoom.Units.First(unit => unit.Id == item.ActorId), item.Kind, paramInt: item.ParamInt,
                    paramInt2: item.ParamInt2, paramString: item.ParamString, dyingCharacter: dying, overrideTarget: target,
                    triggerCount: item.TriggerCount, lastSpawnedOverrideUnitId: item.LastSpawnedOverrideUnitId));
            }
            RoomCombatResult result = RelicSpawnStatusModel.CharacterAddedWithPending(room, unitId, fromCardId, onlyCovenants,
                deferCallbacks ? null : Drain, pending);
            if (!result.Supported) return Fail(result.UnsupportedReason!);
            Import(result.State!); events.AddRange(result.Events);
            return new RelicBirthResult(state, null, result.PendingCallbacks.Select(UnitCloneCallback.From).ToArray(), dispatched, events);

            CombatUnit? Find(int id) => id == 0 ? null : source.Rooms.SelectMany(frame => frame.Units).FirstOrDefault(unit => unit.Id == id);
            void Import(RoomCombatState frame) => state = CardSpellModel.WithContext(new TrainCombatState(
                state.Rooms.Select(item => item.RoomIndex == frame.RoomIndex ? frame : item).ToArray(),
                state.Movement, state.EnemySlotsPerRoom, frame.Context), frame.Context!);
            RoomCombatResult Drain(RoomCombatState frame, IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger> children)
            {
                Import(frame);
                UnitCloneResult drained = UnitCloneModel.Drain(new UnitCloneResult(state, unitId, RoomOutcome.Exchanged, null,
                    Array.Empty<CombatEvent>(), Array.Empty<UnitCloneBoundary>(), children));
                if (!drained.Supported) return new RoomCombatResult(null, RoomOutcome.Unsupported, 0, drained.Events.ToList(), drained.UnsupportedReason);
                state = drained.State!; dispatched.AddRange(drained.Dispatched);
                return new RoomCombatResult(state.Rooms.First(item => item.RoomIndex == frame.RoomIndex), drained.Outcome, 0, drained.Events.ToList());
            }
            RelicBirthResult Fail(string error) => new RelicBirthResult(null, error, Array.Empty<UnitCloneCallback>(), dispatched, events);
        }
    }
}

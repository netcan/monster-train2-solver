using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    // Room snapshots intentionally have no context: the shared context owns this world,
    // and a world may not refer back to its owner through another room context.
    public sealed class EnchantmentWorld
    {
        public IReadOnlyList<RoomCombatState> Rooms { get; }
        public IReadOnlyList<EnemyMovement> Movement { get; }
        public int EnemySlotsPerRoom { get; }
        public IReadOnlyList<EnchantmentRetainedUnit> RetainedUnits { get; }
        public IReadOnlyList<int> EnchanterIds { get; }
        public bool AllowUpdates { get; }
        public bool Updating { get; }
        public bool Preview { get; }
        public UnityRng TestRng { get; }
        public bool AutomaticLifecycle { get; }
        public EnchantmentWorld(IReadOnlyList<RoomCombatState> rooms, IReadOnlyList<EnemyMovement> movement, int enemySlotsPerRoom,
            IReadOnlyList<EnchantmentRetainedUnit> retainedUnits, IReadOnlyList<int> enchanterIds,
            bool allowUpdates, bool updating, bool preview, UnityRng testRng, bool automaticLifecycle = false)
        {
            Rooms = Array.AsReadOnly(rooms.OrderBy(room => room.RoomIndex).Select(Strip).ToArray());
            Movement = Array.AsReadOnly(movement.ToArray()); EnemySlotsPerRoom = enemySlotsPerRoom;
            RetainedUnits = Array.AsReadOnly((automaticLifecycle ? retainedUnits.OrderBy(actor => actor.Unit.Id) : retainedUnits.AsEnumerable()).ToArray()); EnchanterIds = Array.AsReadOnly(enchanterIds.ToArray());
            AllowUpdates = allowUpdates; Updating = updating; Preview = preview; TestRng = testRng; AutomaticLifecycle = automaticLifecycle;
        }
        private static RoomCombatState Strip(RoomCombatState room) => new RoomCombatState(room.RoomIndex, room.Deployment,
            room.Units, room.ExternalInteractions, null, room.Preview);
        internal EnchantmentCombatState Frame(CombatContext context) => new EnchantmentCombatState(new TrainCombatState(
            Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment, room.Units, room.ExternalInteractions,
                context.WithEnchantments(null), room.Preview)).ToArray(), Movement, EnemySlotsPerRoom, context.WithEnchantments(null)),
            RetainedUnits, EnchanterIds, AllowUpdates, Updating, Preview, TestRng);
        internal static EnchantmentWorld From(EnchantmentCombatState frame, bool automaticLifecycle = false) => new EnchantmentWorld(frame.Train.Rooms,
            frame.Train.Movement, frame.Train.EnemySlotsPerRoom, frame.RetainedUnits, frame.EnchanterIds,
            frame.AllowUpdates, frame.Updating, frame.Preview, frame.TestRng, automaticLifecycle);
    }

    internal static class EnchantmentWorldModel
    {
        internal static RoomCombatState Sync(RoomCombatState source, IReadOnlyList<CombatUnit>? retainedUnits = null)
        {
            EnchantmentWorld? world = source.Context?.Enchantments;
            if (world == null) return source;
            var currentIds = source.Units.Select(unit => unit.Id).ToHashSet();
            var rooms = world.Rooms.Select(room => room.RoomIndex == source.RoomIndex ? source :
                new RoomCombatState(room.RoomIndex, room.Deployment, room.Units.Where(unit => !currentIds.Contains(unit.Id)).ToArray(),
                    room.ExternalInteractions, null, room.Preview)).ToArray();
            var liveIds = rooms.SelectMany(room => room.Units).Select(unit => unit.Id).ToHashSet();
            var retained = world.RetainedUnits.Where(actor => !liveIds.Contains(actor.Unit.Id)).ToList();
            RoomCombatState? previous = world.Rooms.FirstOrDefault(room => room.RoomIndex == source.RoomIndex);
            foreach (CombatUnit unit in previous?.Units ?? Array.Empty<CombatUnit>())
                if (!liveIds.Contains(unit.Id) && retained.All(actor => actor.Unit.Id != unit.Id))
                    retained.Add(new EnchantmentRetainedUnit(unit, source.RoomIndex, source.Preview));
            foreach (CombatUnit unit in retainedUnits ?? Array.Empty<CombatUnit>())
            {
                if (liveIds.Contains(unit.Id)) continue;
                retained.RemoveAll(actor => actor.Unit.Id == unit.Id);
                retained.Add(new EnchantmentRetainedUnit(unit, source.RoomIndex, source.Preview));
            }
            if (world.AutomaticLifecycle)
            {
                var referenced = rooms.SelectMany(room => room.Units).Concat(retained.Select(actor => actor.Unit))
                    .SelectMany(unit => unit.Triggers).SelectMany(trigger => trigger.Effects).Where(effect => effect.Enchantment != null)
                    .SelectMany(effect => effect.Enchantment!.State.PrimaryTargets.Concat(effect.Enchantment.State.PreviewTargets))
                    .Select(target => target.UnitId).Concat(world.EnchanterIds).ToHashSet();
                retained = retained.Where(actor => referenced.Contains(actor.Unit.Id)).Select(actor =>
                    new EnchantmentRetainedUnit(actor.Unit, actor.Unit.DeathState?.IsDestroyed == true ? -1 : actor.RoomIndex, actor.Preview)).ToList();
            }
            world = new EnchantmentWorld(rooms,
                world.Movement.Where(rule => liveIds.Contains(rule.UnitId)).ToArray(), world.EnemySlotsPerRoom, retained, world.EnchanterIds, world.AllowUpdates, world.Updating,
                world.Preview, world.TestRng, world.AutomaticLifecycle);
            return Room(source, source.Context!.WithEnchantments(world));
        }
        internal static RoomCombatState Room(RoomCombatState source, CombatContext? context) => new RoomCombatState(source.RoomIndex,
            source.Deployment, source.Units, source.ExternalInteractions, context, source.Preview);

        internal static TrainCombatState Rebase(TrainCombatState source, bool? allowUpdates = null, bool? preview = null, UnityRng? testRng = null)
        {
            EnchantmentWorld? world = source.Context?.Enchantments;
            if (world == null) return source;
            var live = source.Rooms.SelectMany(room => room.Units).Select(unit => unit.Id).ToHashSet();
            var retained = world.RetainedUnits.Where(actor => !live.Contains(actor.Unit.Id)).ToList();
            foreach (RoomCombatState room in world.Rooms)
                foreach (CombatUnit unit in room.Units)
                    if (!live.Contains(unit.Id) && retained.All(actor => actor.Unit.Id != unit.Id))
                        retained.Add(new EnchantmentRetainedUnit(unit, room.RoomIndex, room.Preview));
            var changed = new EnchantmentWorld(source.Rooms, source.Movement, source.EnemySlotsPerRoom, retained,
                world.EnchanterIds, allowUpdates ?? world.AllowUpdates, world.Updating, preview ?? world.Preview,
                testRng ?? world.TestRng, world.AutomaticLifecycle);
            CombatContext context = source.Context!.WithEnchantments(changed);
            return new TrainCombatState(source.Rooms.Select(room => Room(room, context)).ToArray(), source.Movement,
                source.EnemySlotsPerRoom, context);
        }
        internal static TrainCombatResult SetAllowUpdates(TrainCombatState source, bool allow)
        {
            if (source.Context?.Enchantments?.AutomaticLifecycle != true)
                return new TrainCombatResult(source, RoomOutcome.Exchanged, Array.Empty<RoomCombatResult>());
            source = Rebase(source, allowUpdates: allow);
            return allow ? UpdateAll(source) : new TrainCombatResult(source, RoomOutcome.Exchanged, Array.Empty<RoomCombatResult>());
        }
        internal static TrainCombatResult UpdateAll(TrainCombatState source)
        {
            if (source.Context?.Enchantments?.AutomaticLifecycle != true)
                return new TrainCombatResult(source, RoomOutcome.Exchanged, Array.Empty<RoomCombatResult>());
            source = Rebase(source);
            RoomCombatResult updated = Update(source.Rooms.First());
            if (!updated.Supported) return new TrainCombatResult(null, RoomOutcome.Unsupported, Array.Empty<RoomCombatResult>(), updated.UnsupportedReason);
            CombatContext context = updated.State!.Context!;
            var train = new TrainCombatState(context.Enchantments!.Rooms.Select(room => Room(room, context)).ToArray(), source.Movement,
                source.EnemySlotsPerRoom, context);
            return TrainCombatModel.ApplyCharacterQueue(train, updated.PendingCallbacks.ToList());
        }

        internal static RoomCombatResult Update(RoomCombatState source, int? onlySourceId = null)
        {
            source = Sync(source);
            EnchantmentWorld? world = source.Context?.Enchantments;
            if (world == null) return new RoomCombatResult(source, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
            EnchantmentCombatState frame = world.Frame(source.Context!);
            EnchantmentCombatResult result;
            if (onlySourceId.HasValue)
            {
                CombatUnit? owner = frame.Train.Rooms.SelectMany(room => room.Units).Concat(frame.RetainedUnits.Select(actor => actor.Unit))
                    .FirstOrDefault(unit => unit.Id == onlySourceId.Value);
                result = new EnchantmentCombatResult(frame, Array.Empty<EnchantmentRequest>(), Array.Empty<RoomCombatModel.QueuedCharacterTrigger>());
                if (owner != null && world.EnchanterIds.Contains(owner.Id))
                    for (int trigger = 0; trigger < owner.Triggers.Count; trigger++)
                        for (int effect = 0; effect < owner.Triggers[trigger].Effects.Count; effect++)
                        {
                            if (owner.Triggers[trigger].Effects[effect].Enchantment == null) continue;
                            EnchantmentCombatResult next = EnchantmentCombatModel.UpdateEffect(result.State!, owner.Id, trigger, effect);
                            result = new EnchantmentCombatResult(next.State, result.Requests.Concat(next.Requests).ToArray(),
                                result.PendingCallbacks.Concat(next.PendingCallbacks).ToArray(), next.UnsupportedReason);
                            if (!result.Supported) return Unsupported(result.UnsupportedReason!);
                        }
            }
            else result = EnchantmentCombatModel.UpdateAll(frame);
            return Complete(source, result);
        }
        internal static RoomCombatResult UpdateEffect(RoomCombatState source, int sourceId, int triggerIndex, int effectIndex)
        {
            source = Sync(source);
            EnchantmentWorld? world = source.Context?.Enchantments;
            if (world == null) return Unsupported("A direct enchantment effect update requires the captured shared world.");
            CombatUnit? owner = world.Rooms.SelectMany(room => room.Units).Concat(world.RetainedUnits.Select(actor => actor.Unit))
                .FirstOrDefault(unit => unit.Id == sourceId);
            if (owner == null || triggerIndex < 0 || triggerIndex >= owner.Triggers.Count || effectIndex < 0 ||
                effectIndex >= owner.Triggers[triggerIndex].Effects.Count || owner.Triggers[triggerIndex].Effects[effectIndex].Enchantment == null)
                return Unsupported("A direct enchantment effect update requires a captured actor and valid effect identity.");
            // CharacterState.ApplyEffects invokes this one effect directly, even while
            // the manager's global update gate is closed. Bound remains an effect gate.
            return Complete(source, EnchantmentCombatModel.UpdateEffect(world.Frame(source.Context!), sourceId, triggerIndex, effectIndex));
        }
        private static RoomCombatResult Complete(RoomCombatState source, EnchantmentCombatResult result)
        {
            if (!result.Supported) return Unsupported(result.UnsupportedReason!);
            EnchantmentWorld updated = EnchantmentWorld.From(result.State!, source.Context!.Enchantments!.AutomaticLifecycle);
            CombatContext context = result.State!.Train.Context!.WithEnchantments(updated);
            RoomCombatState roomState = updated.Rooms.Single(room => room.RoomIndex == source.RoomIndex);
            return new RoomCombatResult(Room(roomState, context), RoomOutcome.Exchanged, 0, new List<CombatEvent>(), pendingCallbacks: result.PendingCallbacks);
        }
        internal static RoomCombatState Bind(RoomCombatState source, int unitId)
        {
            if (source.Context?.Enchantments == null) return source;
            CombatUnit unit = source.Units.Single(unit => unit.Id == unitId);
            bool enchanter = unit.Triggers.Any(trigger => trigger.Effects.Any(effect => effect.Enchantment != null));
            if (!enchanter) return Sync(source);
            var bound = unit.WithTriggers(unit.Triggers.Select(trigger => trigger.WithEffects(trigger.Effects.Select(effect =>
                effect.Enchantment == null ? effect : effect.WithEnchantment(new EnchantmentRule(effect.Enchantment.Targeting,
                    effect.Enchantment.StatusPool, effect.Enchantment.State, true, effect.Enchantment.HasParentCard))).ToArray())).ToArray());
            source = new RoomCombatState(source.RoomIndex, source.Deployment, source.Units.Select(old => old.Id == unitId ? bound : old).ToArray(),
                source.ExternalInteractions, source.Context, source.Preview);
            CombatContext context = source.Context!;
            EnchantmentWorld world = context.Enchantments!;
            var changed = new EnchantmentWorld(world.Rooms, world.Movement, world.EnemySlotsPerRoom, world.RetainedUnits,
                world.EnchanterIds.Concat(new[] { unitId }).Distinct().ToArray(), world.AllowUpdates, world.Updating, world.Preview, world.TestRng, world.AutomaticLifecycle);
            return Sync(Room(source, context.WithEnchantments(changed)));
        }
        internal static RoomCombatState Refresh(RoomCombatState source)
        {
            RoomCombatState? room = source.Context?.Enchantments?.Rooms.FirstOrDefault(item => item.RoomIndex == source.RoomIndex);
            return room == null ? source : Room(room, source.Context);
        }
        internal static CombatUnit RestorePreviewEffects(CombatUnit original, EnchantmentWorld observed, bool prepare)
        {
            CombatUnit? tested = observed.Rooms.SelectMany(room => room.Units).Concat(observed.RetainedUnits.Select(actor => actor.Unit))
                .FirstOrDefault(unit => unit.Id == original.Id);
            if (tested == null) return original;
            return original.WithTriggers(original.Triggers.Select((trigger, index) => trigger.WithEffects(trigger.Effects.Select((effect, effectIndex) =>
            {
                EnchantmentRule? after = index < tested.Triggers.Count && effectIndex < tested.Triggers[index].Effects.Count
                    ? tested.Triggers[index].Effects[effectIndex].Enchantment : null;
                return effect.Enchantment == null || after == null ? effect : effect.WithEnchantment(effect.Enchantment.WithState(
                    prepare ? EnchantmentLifecycleModel.PrepareForPreview(after.State) : after.State));
            }).ToArray())).ToArray());
        }
        internal static TrainCombatState Attach(EnchantmentCombatState frame)
        {
            CombatContext context = frame.Train.Context!.WithEnchantments(EnchantmentWorld.From(frame));
            return new TrainCombatState(frame.Train.Rooms.Select(room => Room(room, context)).ToArray(), frame.Train.Movement,
                frame.Train.EnemySlotsPerRoom, context);
        }
        internal static EnchantmentCombatState Snapshot(TrainCombatState train) => train.Context!.Enchantments!.Frame(train.Context);

        internal static EnchantmentCombatResult ChangeStatus(EnchantmentCombatState source, int unitId, CombatStatus status, bool remove)
        {
            TrainCombatState train = Attach(source);
            RoomCombatState room = train.Rooms.Single(item => item.Units.Any(unit => unit.Id == unitId));
            RoomCombatResult changed = remove ? StatusRemovalModel.Remove(room, unitId, status.Id, status.Stacks) :
                StatusApplicationModel.ApplyRetained(room, unitId, status, 0);
            return new EnchantmentCombatResult(changed.Supported ? changed.State!.Context!.Enchantments!.Frame(changed.State.Context) : null,
                Array.Empty<EnchantmentRequest>(), changed.PendingCallbacks, changed.UnsupportedReason);
        }

        internal static EnchantmentCombatResult Drain(EnchantmentCombatResult result)
        {
            if (!result.Supported) return result;
            foreach (var callback in result.PendingCallbacks)
            {
                CombatUnit unit = result.State!.Train.Rooms.SelectMany(room => room.Units)
                    .FirstOrDefault(actor => actor.Id == callback.Unit.Id) ?? callback.Unit;
                if (unit.Triggers.Where(trigger => trigger.Kind == callback.Kind).SelectMany(trigger => trigger.Effects)
                    .Any(effect => effect.Type != "CardEffectRewardGold"))
                    return new EnchantmentCombatResult(null, result.Requests, result.PendingCallbacks,
                        "Aura callbacks that change actors/cards require automatic combat lifecycle integration.");
            }
            TrainCombatResult drained = TrainCombatModel.ApplyCharacterQueue(Attach(result.State!), result.PendingCallbacks.ToList());
            return new EnchantmentCombatResult(drained.Supported ? Snapshot(drained.State!) : null, result.Requests,
                Array.Empty<RoomCombatModel.QueuedCharacterTrigger>(), drained.UnsupportedReason);
        }

        // Only native control-status APIs request an immediate global aura update.
        // Other status writes still synchronize the shared actor snapshot.
        internal static RoomCombatResult StatusChanged(RoomCombatResult result, string statusId, bool dormant, bool update)
        {
            if (!result.Supported || result.State!.Context?.Enchantments == null) return result;
            RoomCombatState state = Sync(result.State!);
            if (!update || statusId != "silenced" && statusId != "muted" && (statusId != "spark" || !dormant))
                return new RoomCombatResult(state, result.Outcome, result.Rounds, result.Events.ToList(),
                    pendingCallbacks: result.PendingCallbacks, retainedUnits: result.RetainedUnits, dispatches: result.Dispatches);
            RoomCombatResult refreshed = Update(state);
            return !refreshed.Supported ? refreshed : new RoomCombatResult(refreshed.State, result.Outcome, result.Rounds,
                result.Events.ToList(), pendingCallbacks: result.PendingCallbacks.Concat(refreshed.PendingCallbacks).ToArray(),
                retainedUnits: result.RetainedUnits, dispatches: result.Dispatches);
        }
        private static RoomCombatResult Unsupported(string error) => new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error);
    }
}

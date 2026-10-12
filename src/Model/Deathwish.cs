using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class DeathwishModel
    {
        internal static readonly string[] CallbackKinds = { "OnDeathwish", "OnDeathwishLost" };

        internal static RoomCombatResult Update(RoomCombatState source)
        {
            EnchantmentWorld? world = source.Context?.Enchantments;
            EnchantmentCombatState frame;
            if (world == null)
            {
                var train = new TrainCombatState(new[] { source }, Array.Empty<EnemyMovement>(), 0, source.Context);
                frame = new EnchantmentCombatState(train, Array.Empty<EnchantmentRetainedUnit>(), Array.Empty<int>(),
                    true, false, source.Preview, default);
            }
            else
            {
                source = EnchantmentWorldModel.Sync(source);
                world = source.Context!.Enchantments!;
                frame = world.Frame(source.Context);
            }

            EnchantmentCombatResult updated = EnchantmentCombatModel.UpdateDeathwish(frame);
            if (!updated.Supported) return Unsupported(updated.UnsupportedReason!);
            RoomCombatState room;
            if (world == null) room = updated.State!.Train.Rooms.Single(item => item.RoomIndex == source.RoomIndex);
            else
            {
                EnchantmentWorld changed = EnchantmentWorld.From(updated.State!, world.AutomaticLifecycle);
                CombatContext context = source.Context!.WithEnchantments(changed);
                room = changed.Rooms.Single(item => item.RoomIndex == source.RoomIndex);
                room = EnchantmentWorldModel.Room(room, context);
            }
            return new RoomCombatResult(room, RoomOutcome.Exchanged, 0, new List<CombatEvent>(),
                pendingCallbacks: updated.PendingCallbacks);
        }

        internal static TrainCombatResult UpdateAll(TrainCombatState source,
            Action<RoomCombatModel.QueuedCharacterTrigger>? enqueueCallback = null)
        {
            EnchantmentWorld? world = source.Context?.Enchantments;
            EnchantmentCombatState frame;
            TrainCombatState updatedTrain;
            if (world == null)
                frame = new EnchantmentCombatState(source, Array.Empty<EnchantmentRetainedUnit>(), Array.Empty<int>(),
                    true, false, source.Rooms.Any(room => room.Preview), default);
            else
            {
                source = EnchantmentWorldModel.Rebase(source);
                world = source.Context!.Enchantments!;
                frame = world.Frame(source.Context);
            }

            EnchantmentCombatResult updated = EnchantmentCombatModel.UpdateDeathwish(frame);
            if (!updated.Supported) return new TrainCombatResult(null, RoomOutcome.Unsupported,
                Array.Empty<RoomCombatResult>(), updated.UnsupportedReason);
            EnchantmentCombatState updatedState = updated.State!;
            updatedTrain = updatedState.Train;

            TrainCombatState train;
            if (world == null) train = updatedTrain;
            else
            {
                EnchantmentWorld changed = EnchantmentWorld.From(updatedState, world.AutomaticLifecycle);
                CombatContext context = source.Context!.WithEnchantments(changed);
                train = new TrainCombatState(changed.Rooms.Select(room => EnchantmentWorldModel.Room(room, context)).ToArray(),
                    updatedTrain.Movement, updatedTrain.EnemySlotsPerRoom, context);
            }

            if (enqueueCallback != null)
            {
                foreach (RoomCombatModel.QueuedCharacterTrigger callback in updated.PendingCallbacks) enqueueCallback(callback);
                return new TrainCombatResult(train, RoomOutcome.Exchanged, Array.Empty<RoomCombatResult>());
            }
            return updated.PendingCallbacks.Count == 0 ? new TrainCombatResult(train, RoomOutcome.Exchanged,
                Array.Empty<RoomCombatResult>()) : TrainCombatModel.ApplyCharacterQueue(train, updated.PendingCallbacks.ToList());
        }

        private static RoomCombatResult Unsupported(string reason) =>
            new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), reason);
    }
}

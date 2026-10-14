using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RoomCorruptionResult
    {
        public CombatContext? Context { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => UnsupportedReason == null;
        internal RoomCorruptionResult(CombatContext? context, string? error = null)
        { Context = context; UnsupportedReason = error; }
    }

    public static class RoomCorruptionModel
    {
        private static readonly string[] RelicCallbacks =
        {
            "RelicEffectDamageOnCorruptionAdded", "RelicEffectDrawOnCorruptionAdded",
            "RelicEffectAddStatusEffectOnCorruptionAdded"
        };

        public static string? Validate(CombatContext? context, CardActionEffect effect)
        {
            if (context?.RoomCapacities == null) return "Corruption requires captured room state.";
            if (effect.Type != "AdjustMaxCorruption" && effect.Type != "AdjustCorruption" &&
                effect.Type != "AddPermanentCorruption")
                return "Unknown corruption effect.";
            if (effect.Type == "AdjustCorruption" && !effect.CorruptionAdd.HasValue)
                return "Missing corruption add/remove mode.";
            if (effect.Target != "Room" && effect.Target != "DropTargetCharacter" && effect.Target != "Tower")
                return "Unsupported corruption target mode.";
            if (effect.Type == "AdjustMaxCorruption" && effect.Target == "Tower")
                return "Maximum-corruption adjustment requires a selected room.";
            if (effect.Type == "AddPermanentCorruption" && effect.Target != "Room")
                return "Permanent corruption requires a selected room.";
            if (effect.Range != null) return "Corruption effects use fixed integer parameters.";
            if (context.RoomCapacities.Any(room => !room.Corruption.HasValue || !room.MaxCorruption.HasValue ||
                !room.PermanentCorruption.HasValue || room.Corruption < 0 || room.MaxCorruption < 1 || room.PermanentCorruption < 0))
                return "Incomplete or invalid captured corruption state.";
            return null;
        }

        public static bool Test(RoomCombatState? room, CardActionEffect effect)
        {
            if (effect.Target == "Tower") return true;
            int? current = room?.Context?.RoomCapacities?.FirstOrDefault(item => item.RoomIndex == room.RoomIndex)?.Corruption;
            return current.HasValue && (effect.Type != "AdjustCorruption" || effect.CorruptionAdd == true ||
                current.Value >= effect.Value);
        }

        public static RoomCorruptionResult Apply(TrainCombatState source, int selectedRoomIndex, CardActionEffect effect)
        {
            string? error = Validate(source.Context, effect);
            if (error != null) return new RoomCorruptionResult(null, error);
            CombatContext context = source.Context!;
            int[] indices = effect.Target == "Tower" ? source.Rooms.Select(room => room.RoomIndex).ToArray() : new[] { selectedRoomIndex };
            foreach (int index in indices)
            {
                RoomCapacityState? room = context.RoomCapacities!.FirstOrDefault(item => item.RoomIndex == index);
                RoomCombatState? combatRoom = source.Rooms.FirstOrDefault(item => item.RoomIndex == index);
                if (room == null || combatRoom == null) return new RoomCorruptionResult(null, "Missing corruption target room.");
                int current = room.Corruption!.Value, maximum = room.MaxCorruption!.Value;
                int permanent = room.PermanentCorruption!.Value;
                int nextCurrent = current, nextMaximum = maximum;
                if (effect.Type == "AdjustMaxCorruption") nextMaximum = Math.Max(1, unchecked(maximum + effect.Value));
                else
                {
                    int requested;
                    if (effect.Type == "AddPermanentCorruption")
                    {
                        permanent = Math.Min(unchecked(permanent + effect.Value), maximum);
                        requested = unchecked(current + 1);
                    }
                    else
                    {
                        int amount = effect.Value < 0 ? effect.CorruptionAdd == true ? Math.Max(0, maximum - current) : current : effect.Value;
                        requested = effect.CorruptionAdd == true ? unchecked(current + amount) : unchecked(current - amount);
                    }
                    if (requested > current)
                    {
                        if (combatRoom.Units.Any(unit => unit.Triggers.Any(trigger => trigger.Kind == "CorruptionAdded")))
                            return new RoomCorruptionResult(null, "CorruptionAdded unit callbacks are not modeled.");
                        if (context.Relics?.Any(relic => relic.EffectTypes.Any(RelicCallbacks.Contains)) == true)
                            return new RoomCorruptionResult(null, "CorruptionAdded relic callbacks are not modeled.");
                    }
                    nextCurrent = Math.Max(requested, permanent);
                }
                var changed = new RoomCapacityState(index, room.PlayerMaximum, room.EnemyMaximum, room.IsPyre,
                    nextCurrent, room.Attachments, nextMaximum, permanent);
                context = context.WithRoomCapacities(context.RoomCapacities.Select(item => item.RoomIndex == index ? changed : item).ToArray());
            }
            return new RoomCorruptionResult(context);
        }
    }
}

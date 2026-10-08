using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class StandbyPileProbe
    {
        internal static CardPileState Capture(Dictionary<CardState, RemoveFromStandByCondition> native, UnitPlayModelProbe projection)
        {
            // Read Mono's physical entries and free-list without changing dictionary state.
            Type type = native.GetType();
            int count = (int)AccessTools.Field(type, "_count").GetValue(native);
            var entries = (Array?)AccessTools.Field(type, "_entries").GetValue(native);
            int free = (int)AccessTools.Field(type, "_freeList").GetValue(native);
            int freeCount = (int)AccessTools.Field(type, "_freeCount").GetValue(native);
            if (freeCount == 0) free = -1; // A never-initialized dictionary has a default head of zero.
            var slots = new int[count];
            var holes = new List<int>();
            for (int index = 0; index < count; index++)
            {
                object entry = entries!.GetValue(index)!;
                if ((int)AccessTools.Field(entry.GetType(), "hashCode").GetValue(entry) < 0) continue;
                var card = (CardState)AccessTools.Field(entry.GetType(), "key").GetValue(entry);
                slots[index] = projection.CaptureCards(new List<CardState> { card })[0].InstanceId;
            }
            while (free >= 0)
            {
                if (free >= count || holes.Contains(free)) throw new InvalidOperationException("Invalid native standby free list.");
                holes.Add(free);
                object entry = entries!.GetValue(free)!;
                free = (int)AccessTools.Field(entry.GetType(), "next").GetValue(entry);
            }
            if (holes.Count != freeCount) throw new InvalidOperationException("Native standby free count differs from its chain.");
            var conditions = new List<EquipmentStandbyCondition>();
            foreach (var pair in native.Where(item => item.Key.GetCardType() == CardType.Equipment))
            {
                if (pair.Value == null) throw new InvalidOperationException("Equipment has no native standby condition.");
                var callback = (Delegate)AccessTools.Field(typeof(RemoveFromStandByCondition), "conditionFunction").GetValue(pair.Value);
                object closure = callback.Target;
                CharacterState? host = Host(closure);
                if (ReferenceEquals(host, null) || FullBattleTrace.Active == null || pair.Key.HasTrait<CardTraitGraftedEquipment>())
                    throw new InvalidOperationException("Unmodeled equipment standby closure or grafted equipment.");
                conditions.Add(new EquipmentStandbyCondition(FullBattleTrace.Active.CardId(pair.Key), FullBattleTrace.Active.UnitId(host),
                    pair.Key.HasTrait<CardTraitReturnToHandEquipment>()));
            }
            List<UnitStandbyCondition>? unitConditions = MultiSummonScenario.FreshSources ? new List<UnitStandbyCondition>() : null;
            if (unitConditions != null)
                foreach (var pair in native.Where(item => item.Key.GetCardType() == CardType.Monster))
                {
                    var callback = (Delegate)AccessTools.Field(typeof(RemoveFromStandByCondition), "conditionFunction").GetValue(pair.Value);
                    CharacterState? host = Host(callback.Target, unitCard: true);
                    CardPile location = pair.Value.GetReturnLocation();
                    if (ReferenceEquals(host, null) || location != CardPile.KeepInStandBy && location != CardPile.ExhaustedPile)
                        throw new InvalidOperationException("Unmodeled unit standby closure or return location.");
                    unitConditions.Add(new UnitStandbyCondition(FullBattleTrace.Active!.CardId(pair.Key), FullBattleTrace.Active.UnitId(host),
                        location == CardPile.ExhaustedPile));
                }
            var result = new CardPileState("Standby", projection.CaptureCards(native.Keys.ToList()), slots, holes, conditions, unitConditions);
            string? error = CardPileModel.Validate(result);
            if (error != null) throw new InvalidOperationException(error);
            return result;
        }
        private static CharacterState? Host(object closure, int depth = 0, bool unitCard = false)
        {
            if (depth > 4) return null;
            foreach (var field in closure.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public))
            {
                object? value = field.GetValue(closure);
                if (value is CharacterState character) return character;
                if (value == null) continue;
                if (value.GetType().Name == "DiscardCardParams")
                    return (CharacterState?)AccessTools.Field(value.GetType(), unitCard ? "characterSummoned" : "firstTarget").GetValue(value);
                if (value.GetType().Name.Contains("DisplayClass"))
                {
                    CharacterState? nested = Host(value, depth + 1, unitCard);
                    if (!ReferenceEquals(nested, null)) return nested;
                }
            }
            return null;
        }
    }
}

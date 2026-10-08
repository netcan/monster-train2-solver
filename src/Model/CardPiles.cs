using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class CardPileModel
    {
        public static string? ValidateMembership(CardCycleState cards, IReadOnlyList<CardPileState> piles, int nextCardId)
        {
            CardToken[] primary = cards.Hand.Concat(cards.Draw).Concat(cards.Discard)
                .Concat(piles.Where(pile => pile.Name != "DiscardBuffer").SelectMany(pile => pile.Cards)).ToArray();
            if (primary.Select(card => card.InstanceId).Distinct().Count() != primary.Length ||
                primary.Any(card => card.InstanceId <= 0 || card.InstanceId >= nextCardId))
                return "Invalid card identity allocation or duplicate pile membership.";
            CardToken[] buffer = piles.Where(pile => pile.Name == "DiscardBuffer").SelectMany(pile => pile.Cards).ToArray();
            if (buffer.Select(card => card.InstanceId).Distinct().Count() != buffer.Length ||
                buffer.Any(card => card.InstanceId <= 0 || card.InstanceId >= nextCardId ||
                    primary.Any(other => other.InstanceId == card.InstanceId && other.DataId != card.DataId)))
                return "Invalid discard buffer identity or duplicate buffer reference.";
            return null;
        }

        public static string? Validate(CardPileState source)
        {
            if (source.UnitConditions != null && (source.Name != "Standby" ||
                source.UnitConditions.Select(item => item.CardId).Distinct().Count() != source.UnitConditions.Count ||
                source.UnitConditions.Any(item => item.HostUnitId <= 0 || !source.Cards.Any(card => card.InstanceId == item.CardId))))
                return "Invalid unit standby conditions.";
            if (source.EquipmentConditions != null && (source.Name != "Standby" ||
                source.EquipmentConditions.Select(item => item.CardId).Distinct().Count() != source.EquipmentConditions.Count ||
                source.EquipmentConditions.Any(item => item.HostUnitId <= 0 || !source.Cards.Any(card => card.InstanceId == item.CardId))))
                return "Invalid equipment standby conditions.";
            if (source.EntrySlots == null && source.FreeSlots == null) return null; // Legacy captures.
            if (source.Name != "Standby" || source.EntrySlots == null || source.FreeSlots == null)
                return "Incomplete or misplaced standby dictionary layout.";
            if (source.EntrySlots.Any(id => id < 0) || !source.EntrySlots.Where(id => id != 0)
                .SequenceEqual(source.Cards.Select(card => card.InstanceId))) return "Standby slots differ from card order.";
            int[] holes = source.EntrySlots.Select((id, index) => new { id, index }).Where(item => item.id == 0)
                .Select(item => item.index).ToArray();
            return holes.OrderBy(index => index).SequenceEqual(source.FreeSlots.OrderBy(index => index))
                ? null : "Invalid standby free-slot chain.";
        }
        public static CardPileState Add(CardPileState source, CardToken card)
        {
            if (source.EntrySlots == null) return new CardPileState(source.Name, source.Cards.Concat(new[] { card }).ToArray(), equipmentConditions: source.EquipmentConditions, unitConditions: source.UnitConditions);
            int[] slots = source.EntrySlots.ToArray();
            int[] free = source.FreeSlots!.ToArray();
            if (free.Length == 0) slots = slots.Concat(new[] { card.InstanceId }).ToArray();
            else { slots[free[0]] = card.InstanceId; free = free.Skip(1).ToArray(); }
            var cards = source.Cards.Concat(new[] { card }).ToDictionary(item => item.InstanceId);
            return new CardPileState(source.Name, slots.Where(id => id != 0).Select(id => cards[id]).ToArray(), slots, free, source.EquipmentConditions, source.UnitConditions);
        }
        public static CardPileState Remove(CardPileState source, int cardId)
        {
            var cards = source.Cards.Where(card => card.InstanceId != cardId).ToArray();
            var units = source.UnitConditions?.Where(item => item.CardId != cardId).ToArray();
            var conditions = source.EquipmentConditions?.Where(item => item.CardId != cardId).ToArray();
            if (source.EntrySlots == null) return new CardPileState(source.Name, cards, equipmentConditions: conditions, unitConditions: units);
            int[] slots = source.EntrySlots.ToArray();
            int index = Array.IndexOf(slots, cardId);
            if (index < 0) return source;
            slots[index] = 0;
            return new CardPileState(source.Name, cards, slots, new[] { index }.Concat(source.FreeSlots!).ToArray(), conditions, units);
        }
        public static CardPileState Clear(CardPileState source) => new CardPileState(source.Name, Array.Empty<CardToken>(),
            source.EntrySlots == null ? null : Array.Empty<int>(), source.FreeSlots == null ? null : Array.Empty<int>(),
            source.EquipmentConditions == null ? null : Array.Empty<EquipmentStandbyCondition>(),
            source.UnitConditions == null ? null : Array.Empty<UnitStandbyCondition>());
        internal static CardPileState BindEquipment(CardPileState source, EquipmentStandbyCondition condition) =>
            new CardPileState(source.Name, source.Cards, source.EntrySlots, source.FreeSlots,
                (source.EquipmentConditions ?? Array.Empty<EquipmentStandbyCondition>()).Where(item => item.CardId != condition.CardId).Concat(new[] { condition }).ToArray(), source.UnitConditions);
        internal static CardPileState BindUnit(CardPileState source, UnitStandbyCondition condition) =>
            new CardPileState(source.Name, source.Cards, source.EntrySlots, source.FreeSlots, source.EquipmentConditions,
                (source.UnitConditions ?? Array.Empty<UnitStandbyCondition>()).Where(item => item.CardId != condition.CardId).Concat(new[] { condition }).ToArray());
    }
}

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
            if (primary.GroupBy(card => card.InstanceId).Any(group => group.Count() > 1 &&
                // Ephemeral gives two purge references; SelfPurge plus Ephemeral gives three.
                !(group.Count() is 2 or 3 && group.Select(card => card.DataId).Distinct().Count() == 1 &&
                    piles.Any(pile => pile.Name == "Purged" && pile.Cards.Count(card => card.InstanceId == group.Key) == group.Count())) &&
                !(group.Count() == 2 && group.Select(card => card.DataId).Distinct().Count() == 1 &&
                    piles.Any(pile => pile.Name == "Exhausted" && pile.Cards.Any(card => card.InstanceId == group.Key)) &&
                    piles.Any(pile => pile.Name == "Standby" && pile.Cards.Any(card => card.InstanceId == group.Key) &&
                        (pile.UnitConditions?.Any(item => item.CardId == group.Key) == true ||
                         pile.EquipmentConditions?.Any(item => item.CardId == group.Key) == true ||
                         pile.ExhaustConditions?.Any(item => item.CardId == group.Key) == true)))) ||
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
            if (source.ExhaustConditions != null && (source.Name != "Standby" ||
                source.ExhaustConditions.Select(item => item.CardId).Distinct().Count() != source.ExhaustConditions.Count ||
                source.ExhaustConditions.Any(item => (item.KeepInStandBy ? item.ExhaustedCardId.HasValue :
                        item.ExhaustedCardId.GetValueOrDefault() <= 0) ||
                    !source.Cards.Any(card => card.InstanceId == item.CardId) ||
                    source.UnitConditions?.Any(binding => binding.CardId == item.CardId) == true ||
                    source.EquipmentConditions?.Any(binding => binding.CardId == item.CardId) == true))) return "Invalid Consume standby conditions.";
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
            if (source.EntrySlots == null) return new CardPileState(source.Name, source.Cards.Concat(new[] { card }).ToArray(), equipmentConditions: source.EquipmentConditions, unitConditions: source.UnitConditions, exhaustConditions: source.ExhaustConditions);
            int[] slots = source.EntrySlots.ToArray();
            int[] free = source.FreeSlots!.ToArray();
            if (free.Length == 0) slots = slots.Concat(new[] { card.InstanceId }).ToArray();
            else { slots[free[0]] = card.InstanceId; free = free.Skip(1).ToArray(); }
            var cards = source.Cards.Concat(new[] { card }).ToDictionary(item => item.InstanceId);
            return new CardPileState(source.Name, slots.Where(id => id != 0).Select(id => cards[id]).ToArray(), slots, free, source.EquipmentConditions, source.UnitConditions, exhaustConditions: source.ExhaustConditions);
        }
        public static CardPileState Remove(CardPileState source, int cardId)
        {
            var cards = source.Cards.Where(card => card.InstanceId != cardId).ToArray();
            var units = source.UnitConditions?.Where(item => item.CardId != cardId).ToArray();
            var conditions = source.EquipmentConditions?.Where(item => item.CardId != cardId).ToArray();
            if (source.EntrySlots == null) return new CardPileState(source.Name, cards, equipmentConditions: conditions, unitConditions: units, exhaustConditions: source.ExhaustConditions?.Where(item => item.CardId != cardId).ToArray());
            int[] slots = source.EntrySlots.ToArray();
            int index = Array.IndexOf(slots, cardId);
            if (index < 0) return source;
            slots[index] = 0;
            return new CardPileState(source.Name, cards, slots, new[] { index }.Concat(source.FreeSlots!).ToArray(), conditions, units, source.ExhaustConditions?.Where(item => item.CardId != cardId).ToArray());
        }
        public static CardPileState Clear(CardPileState source) => new CardPileState(source.Name, Array.Empty<CardToken>(),
            source.EntrySlots == null ? null : Array.Empty<int>(), source.FreeSlots == null ? null : Array.Empty<int>(),
            source.EquipmentConditions == null ? null : Array.Empty<EquipmentStandbyCondition>(),
            source.UnitConditions == null ? null : Array.Empty<UnitStandbyCondition>(),
            source.ExhaustConditions == null ? null : Array.Empty<CardExhaustStandbyCondition>());
        internal static CardPileState BindEquipment(CardPileState source, EquipmentStandbyCondition condition) =>
            new CardPileState(source.Name, source.Cards, source.EntrySlots, source.FreeSlots,
                (source.EquipmentConditions ?? Array.Empty<EquipmentStandbyCondition>()).Where(item => item.CardId != condition.CardId).Concat(new[] { condition }).ToArray(), source.UnitConditions, exhaustConditions: source.ExhaustConditions);
        internal static CardPileState BindUnit(CardPileState source, UnitStandbyCondition condition) =>
            new CardPileState(source.Name, source.Cards, source.EntrySlots, source.FreeSlots, source.EquipmentConditions,
                (source.UnitConditions ?? Array.Empty<UnitStandbyCondition>()).Where(item => item.CardId != condition.CardId).Concat(new[] { condition }).ToArray(), source.ExhaustConditions);
        internal static CardPileState UnbindUnit(CardPileState source, int cardId) =>
            new CardPileState(source.Name, source.Cards, source.EntrySlots, source.FreeSlots, source.EquipmentConditions,
                source.UnitConditions?.Where(item => item.CardId != cardId).ToArray(), source.ExhaustConditions);
        internal static CardPileState BindExhaust(CardPileState source, CardExhaustStandbyCondition condition) =>
            new CardPileState(source.Name, source.Cards, source.EntrySlots, source.FreeSlots, source.EquipmentConditions, source.UnitConditions,
                (source.ExhaustConditions ?? Array.Empty<CardExhaustStandbyCondition>()).Where(item => item.CardId != condition.CardId).Concat(new[] { condition }).ToArray());
    }
}

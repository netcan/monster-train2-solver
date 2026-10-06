using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class CardPileModel
    {
        public static string? Validate(CardPileState source)
        {
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
            if (source.EntrySlots == null) return new CardPileState(source.Name, source.Cards.Concat(new[] { card }).ToArray());
            int[] slots = source.EntrySlots.ToArray();
            int[] free = source.FreeSlots!.ToArray();
            if (free.Length == 0) slots = slots.Concat(new[] { card.InstanceId }).ToArray();
            else { slots[free[0]] = card.InstanceId; free = free.Skip(1).ToArray(); }
            var cards = source.Cards.Concat(new[] { card }).ToDictionary(item => item.InstanceId);
            return new CardPileState(source.Name, slots.Where(id => id != 0).Select(id => cards[id]).ToArray(), slots, free);
        }
        public static CardPileState Remove(CardPileState source, int cardId)
        {
            var cards = source.Cards.Where(card => card.InstanceId != cardId).ToArray();
            if (source.EntrySlots == null) return new CardPileState(source.Name, cards);
            int[] slots = source.EntrySlots.ToArray();
            int index = Array.IndexOf(slots, cardId);
            if (index < 0) return source;
            slots[index] = 0;
            return new CardPileState(source.Name, cards, slots, new[] { index }.Concat(source.FreeSlots!).ToArray());
        }
        public static CardPileState Clear(CardPileState source) => new CardPileState(source.Name, Array.Empty<CardToken>(),
            source.EntrySlots == null ? null : Array.Empty<int>(), source.FreeSlots == null ? null : Array.Empty<int>());
    }
}

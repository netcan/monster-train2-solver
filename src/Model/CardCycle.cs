using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class CardCycleState
    {
        public IReadOnlyList<CardToken> Hand { get; }
        public IReadOnlyList<CardToken> Draw { get; }
        public IReadOnlyList<CardToken> Discard { get; }
        public UnityRng Rng { get; }
        public int DrawModifier { get; }
        public IReadOnlyList<string> ExternalInteractions { get; }

        public CardCycleState(IReadOnlyList<CardToken> hand, IReadOnlyList<CardToken> draw,
            IReadOnlyList<CardToken> discard, UnityRng rng, int drawModifier,
            IReadOnlyList<string> externalInteractions)
        {
            Hand = Array.AsReadOnly(hand.ToArray()); Draw = Array.AsReadOnly(draw.ToArray());
            Discard = Array.AsReadOnly(discard.ToArray()); Rng = rng; DrawModifier = drawModifier;
            ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray());
        }
    }

    public sealed class CardCycleResult
    {
        public CardCycleState? State { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => State != null;
        internal CardCycleResult(CardCycleState? state, string? reason = null)
        { State = state; UnsupportedReason = reason; }
    }

    public static class CardCycleModel
    {
        // Spell draws neither add nor reset the pending start-of-turn modifier.
        // Native selection ignores the resolving card, and only reshuffles an empty deck.
        public static CardCycleResult DrawCards(CardCycleState source, int count, int maxHandSize, int playedCardId = 0)
        {
            if (source.ExternalInteractions.Count > 0)
                return new CardCycleResult(null, string.Join("; ", source.ExternalInteractions));
            if (maxHandSize < 0 || source.Hand.Count > maxHandSize)
                return new CardCycleResult(null, "Invalid hand size.");
            var hand = source.Hand.ToList(); var draw = source.Draw.ToList(); var discard = source.Discard.ToList();
            UnityRng rng = source.Rng;
            int capacity = maxHandSize - hand.Count + (hand.Any(card => card.InstanceId == playedCardId) ? 1 : 0);
            for (int index = 0; index < Math.Min(count, capacity); index++)
            {
                if (draw.Count == 0)
                {
                    ShuffleResult<CardToken> shuffled = rng.Shuffle(discard);
                    rng = shuffled.State; draw = shuffled.Items.ToList(); discard.Clear();
                }
                int selected = draw.FindLastIndex(card => card.InstanceId != playedCardId);
                if (selected < 0) break;
                // DrawSpecificCard refuses a full hand, even when DrawCards reserved the played card's slot.
                if (hand.Count == maxHandSize) continue;
                CardToken card = draw[selected]; draw.RemoveAt(selected); hand.Insert(0, card);
            }
            return new CardCycleResult(new CardCycleState(hand, draw, discard, rng, source.DrawModifier, source.ExternalInteractions));
        }

        public static CardCycleResult DrawHand(CardCycleState source, int handSize, int maxHandSize)
        {
            if (source.ExternalInteractions.Count > 0)
                return new CardCycleResult(null, string.Join("; ", source.ExternalInteractions));
            if (handSize < 0 || maxHandSize < 0 || source.Hand.Count > maxHandSize)
                return new CardCycleResult(null, "Invalid hand size.");
            var hand = source.Hand.ToList(); var draw = source.Draw.ToList(); var discard = source.Discard.ToList();
            UnityRng rng = source.Rng;
            // Native DrawHand exits before resetting the modifier if no draw can begin.
            if (hand.Count == maxHandSize || hand.Count + draw.Count + discard.Count == 0)
                return new CardCycleResult(source);
            int count = Math.Min(Math.Max(0, handSize + source.DrawModifier), maxHandSize - hand.Count);
            for (int index = 0; index < count; index++)
            {
                if (draw.Count == 0)
                {
                    ShuffleResult<CardToken> shuffled = rng.Shuffle(discard);
                    rng = shuffled.State; draw = shuffled.Items.ToList(); discard.Clear();
                }
                if (draw.Count == 0) break;
                CardToken card = draw[draw.Count - 1]; draw.RemoveAt(draw.Count - 1); hand.Insert(0, card);
            }
            return new CardCycleResult(new CardCycleState(hand, draw, discard, rng, 0, source.ExternalInteractions));
        }

        public static CardCycleResult DiscardHand(CardCycleState source)
        {
            if (source.ExternalInteractions.Count > 0)
                return new CardCycleResult(null, string.Join("; ", source.ExternalInteractions));
            return new CardCycleResult(new CardCycleState(Array.Empty<CardToken>(), source.Draw,
                source.Discard.Concat(source.Hand.Reverse()).ToArray(), source.Rng, source.DrawModifier,
                source.ExternalInteractions));
        }
    }
}

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
        public BonusDrawState? BonusDraw { get; }

        public CardCycleState(IReadOnlyList<CardToken> hand, IReadOnlyList<CardToken> draw,
            IReadOnlyList<CardToken> discard, UnityRng rng, int drawModifier,
            IReadOnlyList<string> externalInteractions, BonusDrawState? bonusDraw = null)
        {
            Hand = Array.AsReadOnly(hand.ToArray()); Draw = Array.AsReadOnly(draw.ToArray());
            Discard = Array.AsReadOnly(discard.ToArray()); Rng = rng; DrawModifier = drawModifier;
            ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray());
            BonusDraw = bonusDraw;
        }
    }

    public sealed class CardCycleResult
    {
        public CardCycleState? State { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => State != null;
        public IReadOnlyList<BonusUpgradeApplication> UpgradeApplications { get; }
        internal CardCycleResult(CardCycleState? state, string? reason = null, IReadOnlyList<BonusUpgradeApplication>? upgradeApplications = null)
        { State = state; UnsupportedReason = reason; UpgradeApplications = Array.AsReadOnly((upgradeApplications ?? Array.Empty<BonusUpgradeApplication>()).ToArray()); }
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
            string? bonusError = BonusDrawModel.Validate(source);
            if (bonusError != null) return new CardCycleResult(null, bonusError);
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
            var applications = new List<BonusUpgradeApplication>();
            BonusDrawState? bonus = BonusDrawModel.CompleteDraw(source.BonusDraw, Array.Empty<CardToken>(), source.Hand.Count, -1, applications);
            return new CardCycleResult(new CardCycleState(hand, draw, discard, rng, source.DrawModifier, source.ExternalInteractions, bonus), upgradeApplications: applications);
        }

        public static CardCycleResult DrawHand(CardCycleState source, int handSize, int maxHandSize)
        {
            if (source.ExternalInteractions.Count > 0)
                return new CardCycleResult(null, string.Join("; ", source.ExternalInteractions));
            if (handSize < 0 || maxHandSize < 0 || source.Hand.Count > maxHandSize)
                return new CardCycleResult(null, "Invalid hand size.");
            string? bonusError = BonusDrawModel.Validate(source);
            if (bonusError != null) return new CardCycleResult(null, bonusError);
            var hand = source.Hand.ToList(); var draw = source.Draw.ToList(); var discard = source.Discard.ToList();
            UnityRng rng = source.Rng;
            // Native DrawHand exits before resetting the modifier if no draw can begin.
            if (hand.Count == maxHandSize || hand.Count + draw.Count + discard.Count == 0)
                return new CardCycleResult(source);
            var drawn = new List<CardToken>();
            int count = Math.Min(Math.Max(0, unchecked(handSize + source.DrawModifier)), maxHandSize - hand.Count);
            for (int index = 0; index < count; index++)
            {
                if (draw.Count == 0)
                {
                    ShuffleResult<CardToken> shuffled = rng.Shuffle(discard);
                    rng = shuffled.State; draw = shuffled.Items.ToList(); discard.Clear();
                }
                if (draw.Count == 0) break;
                CardToken card = draw[draw.Count - 1]; draw.RemoveAt(draw.Count - 1); hand.Insert(0, card);
                drawn.Add(card);
            }
            var applications = new List<BonusUpgradeApplication>();
            BonusDrawState? bonus = BonusDrawModel.CompleteDraw(source.BonusDraw, drawn, source.Hand.Count, handSize, applications);
            return new CardCycleResult(new CardCycleState(hand, draw, discard, rng, 0, source.ExternalInteractions, bonus), upgradeApplications: applications);
        }

        public static CardCycleResult DiscardHand(CardCycleState source)
        {
            if (source.ExternalInteractions.Count > 0)
                return new CardCycleResult(null, string.Join("; ", source.ExternalInteractions));
            return new CardCycleResult(new CardCycleState(Array.Empty<CardToken>(), source.Draw,
                source.Discard.Concat(source.Hand.Reverse()).ToArray(), source.Rng, source.DrawModifier,
                source.ExternalInteractions, source.BonusDraw));
        }
    }
}

using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class UnitCardPurgeModel
    {
        internal static CombatContext? Apply(CombatContext source, int cardId, out string? error)
        {
            error = null;
            if (cardId <= 0) return source;
            if (source.OtherPiles == null || source.FindCard(cardId) == null ||
                !source.OtherPiles.Any(pile => pile.Name == "Purged"))
            { error = "Unit-card purge requires captured card metadata and native piles."; return null; }
            if (source.Relics?.Any(relic => relic.EffectTypes.Count > 0) == true)
            { error = "Unit-card purge with relic callbacks requires native calibration."; return null; }
            if (source.Cards.Hand.Concat(source.Cards.Draw).Any(card => card.InstanceId == cardId))
            { error = "Unit-card purge from Hand or Draw requires native alias calibration."; return null; }
            CardInstanceState card = source.FindCard(cardId)!;
            CardToken token = new CardToken(cardId, card.DataId);
            CardCycleState cards = source.Cards;
            CombatContext context = source.WithCards(new CardCycleState(cards.Hand, cards.Draw,
                cards.Discard.Where(item => item.InstanceId != cardId).ToArray(), cards.Rng, cards.DrawModifier,
                cards.ExternalInteractions, cards.BonusDraw, cards.NextDrawUpgrades, cards.NextUpgradeReferenceId,
                cards.DeckDrawDisabledThisTurn));
            context = context.WithOtherPiles(source.OtherPiles.Select(pile => pile.Name == "Purged" ? CardPileModel.Add(pile, token) :
                pile.Name is "DiscardBuffer" or "Exhausted" or "Standby" ? CardPileModel.Remove(pile, cardId) : pile).ToArray());
            if (context.Statistics?.DeckCards?.Contains(cardId) == true && context.Resources != null)
                context = context.WithResources(context.Resources.WithVisibleDeckCount(context.Resources.VisibleDeckCount - 1));
            return context.WithStatistics(context.Statistics?.RemoveDeckCard(cardId));
        }
    }
}

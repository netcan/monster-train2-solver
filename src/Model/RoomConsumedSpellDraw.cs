using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class RoomConsumedSpellDrawModel
    {
        public static TypedDrawResult Apply(CombatContext context)
        {
            CardPileState? exhausted = context.OtherPiles?.SingleOrDefault(pile => pile.Name == "Exhausted");
            if (exhausted == null || context.CardRegistry == null)
                return new TypedDrawResult(null, Array.Empty<int>(), "Consumed-spell room draws require captured piles and card metadata.");
            if (exhausted.Cards.Any(token => context.FindCard(token.InstanceId)?.MaskDescriptor == null))
                return new TypedDrawResult(null, Array.Empty<int>(), "Consumed-spell room draw is missing a candidate's native type.");
            CardToken[] candidates = exhausted.Cards.Where(token => context.FindCard(token.InstanceId)!.MaskDescriptor!.Definition.CardType == "Spell").ToArray();
            if (candidates.Length == 0) return new TypedDrawResult(context, Array.Empty<int>());
            RngDraw selected = context.BattleRng.Range(0, candidates.Length);
            CardToken token = candidates[selected.Value];
            context = context.WithBattleRng(selected.State).WithOtherPiles(context.OtherPiles!.Select(pile =>
                pile.Name is "Exhausted" or "Eaten" or "Standby" ? CardPileModel.Remove(pile, token.InstanceId) : pile).ToArray());
            CardInstanceState card = context.FindCard(token.InstanceId)!;
            if (card.ExhaustState != null || CardRetainModel.Present(card)) context = context.WithCard(CardExhaustModel.ResetTraits(card));
            CardCycleState cards = context.Cards;
            context = context.WithCards(new CardCycleState(cards.Hand, cards.Draw.Concat(new[] { token }).ToArray(), cards.Discard,
                cards.Rng, cards.DrawModifier, cards.ExternalInteractions, cards.BonusDraw, cards.NextDrawUpgrades,
                cards.NextUpgradeReferenceId, cards.DeckDrawDisabledThisTurn));
            return NextDrawUpgradeModel.DrawSpecific(context, token.InstanceId, 0);
        }
    }
}

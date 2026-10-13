using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class CardExhaustProbe
    {
        internal static CardExhaustState? Capture(CardState card, Func<CardState, int> cardId)
        {
            if (!CardBranchMaskProbe.Enabled) return null;
            var bases = (List<CardTraitState>)AccessTools.Field(typeof(CardState), "traits").GetValue(card);
            // GetTraitStates ensures a dirty temporary composition is materialized before reading private fields.
            List<CardTraitState> combined = card.GetTraitStates();
            CardExhaustTraitState Read(CardTraitExhaustState trait, int index)
            {
                var exhaustedCard = (CardState?)AccessTools.Field(typeof(CardTraitExhaustState), "exhaustedCard").GetValue(trait);
                return new CardExhaustTraitState(index, trait.IsExhausted(), exhaustedCard == null ? null : (int?)cardId(exhaustedCard));
            }
            CardTraitExhaustState? upgraded = combined.OfType<CardTraitExhaustState>()
                .SingleOrDefault(trait => !bases.Any(item => ReferenceEquals(item, trait)));
            return new CardExhaustState(AllGameManagers.Instance!.GetRelicManager().GetPlayCountForExhaustModifier(),
                bases.Select((trait, index) => new { trait, index }).Where(item => item.trait is CardTraitExhaustState)
                    .Select(item => Read((CardTraitExhaustState)item.trait, item.index)).ToArray(),
                upgraded == null ? null : Read(upgraded, -1));
        }
        internal static CardExhaustStandbyCondition? Condition(CardState card, RemoveFromStandByCondition condition,
            Func<CardState, int> cardId)
        {
            if (condition == null) return null;
            var callback = (Delegate)AccessTools.Field(typeof(RemoveFromStandByCondition), "conditionFunction").GetValue(condition);
            if (!(callback.Target is CardTraitExhaustState trait))
            {
                if (!AllGameManagers.Instance!.GetCardManager()!.IsCardInExhaustPile(card)) return null;
                CardPile location = condition.GetReturnLocation();
                return location == CardPile.ExhaustedPile
                    ? new CardExhaustStandbyCondition(cardId(card), cardId(card))
                    : location == CardPile.KeepInStandBy
                        ? new CardExhaustStandbyCondition(cardId(card), null, keepInStandBy: true) : null;
            }
            var exhaustedCard = (CardState?)AccessTools.Field(typeof(CardTraitExhaustState), "exhaustedCard").GetValue(trait);
            if (callback.Method.Name != "StandByCondition") throw new InvalidOperationException("Unmodeled Consume standby callback.");
            return new CardExhaustStandbyCondition(cardId(card), exhaustedCard == null ? null : (int?)cardId(exhaustedCard));
        }
    }
}

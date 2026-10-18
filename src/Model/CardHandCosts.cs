using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class CardHandCostModel
    {
        public const string Trait = "CardTraitIncreaseCostOfCardsInHand";
        public static bool HasModifiers(CombatContext context) => context.Cards.Hand.Any(owner =>
            context.FindCard(owner.InstanceId) is CardInstanceState card &&
            MayHaveModifier(card) &&
            CardExhaustModel.RefreshTraits(card).CombinedTraits!.Any(trait => trait.RuntimeType == Trait));
        private static bool MayHaveModifier(CardInstanceState card) => card.MaskDescriptor is CardMaskDescriptor mask &&
            (mask.BaseTraits.Concat(mask.TemporaryTraits).Any(trait => trait.RuntimeType == Trait) ||
             mask.PermanentReplacements.Any(replacement => replacement.NewRuntimeType == Trait) ||
             card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true));

        // Native visits hand owners and their combined traits in order, including
        // the modified card itself. Cards outside the hand receive no hand cost.
        public static int Resolve(CombatContext context, CardInstanceState card, int rawCost, string cardType)
        {
            int cost = card.RawCostOverride ?? rawCost;
            if (context.Cards.Hand.Any(item => item.InstanceId == card.InstanceId))
                foreach (var owner in context.Cards.Hand)
                {
                    CardInstanceState? instance = context.FindCard(owner.InstanceId);
                    if (instance == null || !MayHaveModifier(instance)) continue;
                    cost = Apply(cost, cardType, CardExhaustModel.RefreshTraits(instance).CombinedTraits!);
                }
            // Hand traits precede numeric upgrades, so a reduction can cancel
            // the surcharge even when the unmodified card costs zero.
            return CardModifierModel.UpgradedStat(cost, "Cost", true, card.Permanent, card.Temporary);
        }

        public static int Apply(int cost, string cardType, IEnumerable<CardTraitValue> traits)
        {
            foreach (CardTraitValue trait in traits.Where(item => item.RuntimeType == Trait))
            {
                string? target = trait.HandCostTargetType;
                if (target is not ("Any" or "Spell" or "Monster" or "Blight" or "Junk" or "Equipment" or "TrainRoomAttachment"))
                    throw new InvalidOperationException("Hand cost traits require a captured native card type filter.");
                if (target == "Any" || target == cardType)
                    cost = Math.Clamp(unchecked(cost + trait.ParamInt), 0, 99);
            }
            return cost;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicCardTypeCost
    {
        public int EffectIndex { get; }
        public string CardType { get; }
        public int Amount { get; }
        public bool IgnoreRestrictions { get; }
        public string? RequiredSubtype { get; }
        public int ConditionCount { get; }
        public string? PileType { get; }
        public CardUpgradeModifier? FirstCardUpgrade { get; }
        public bool? WaitingForFirstCard { get; }

        public RelicCardTypeCost(int effectIndex, string cardType, int amount, bool ignoreRestrictions,
            string? requiredSubtype, int conditionCount, string? pileType = null,
            CardUpgradeModifier? firstCardUpgrade = null, bool? waitingForFirstCard = null)
        { EffectIndex = effectIndex; CardType = cardType; Amount = amount;
            IgnoreRestrictions = ignoreRestrictions; RequiredSubtype = requiredSubtype; ConditionCount = conditionCount;
            PileType = pileType; FirstCardUpgrade = firstCardUpgrade; WaitingForFirstCard = waitingForFirstCard; }

        internal RelicCardTypeCost WithFirstCardState(CardUpgradeModifier upgrade, bool waiting) =>
            new RelicCardTypeCost(EffectIndex, CardType, Amount, IgnoreRestrictions, RequiredSubtype,
                ConditionCount, PileType, upgrade, waiting);
    }

    public static class RelicCardTypeCostModel
    {
        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type is RelicModel.ModifyCardTypeCost or RelicFirstCardCostModel.Effect)
                .Select(item => item.index).ToArray();
            if (indices.Length == 0)
                return relic.CardTypeCosts?.Count > 0 ? "Unexpected relic card-type cost definitions." : null;
            if (relic.CardTypeCosts == null || !indices.SequenceEqual(relic.CardTypeCosts.Select(item => item.EffectIndex)))
                return "Missing ordered relic card-type cost definitions.";
            foreach (RelicCardTypeCost rule in relic.CardTypeCosts)
            {
                if (relic.EffectTypes[rule.EffectIndex] == RelicFirstCardCostModel.Effect)
                {
                    string? error = RelicFirstCardCostModel.Validate(relic, rule);
                    if (error != null) return error;
                    continue;
                }
                if (rule.PileType != null || rule.FirstCardUpgrade != null || rule.WaitingForFirstCard.HasValue ||
                    rule.CardType is not ("Blight" or "Junk") || rule.RequiredSubtype != null || rule.ConditionCount != 0)
                    return "Unmodeled relic card-type cost restrictions or conditions.";
            }
            return null;
        }

        internal static CardInstanceState Apply(CardInstanceState card, RelicCardTypeCost rule, out bool matched)
        {
            if (rule.FirstCardUpgrade != null)
            { matched = false; return card; }
            matched = card.MaskDescriptor!.Definition.CardType == rule.CardType;
            if (!matched) return card;
            CardModifiers original = card.Temporary;
            CardStatModifier offsets = original.Offsets;
            var adjusted = new CardStatModifier(offsets.Damage, offsets.Health,
                unchecked(offsets.Cost + rule.Amount), offsets.Heal, offsets.Size,
                unchecked(offsets.XCost - rule.Amount), offsets.EquipmentLimit, offsets.UpgradeSlotCount);
            return card.WithModifiers(card.Permanent,
                new CardModifiers(adjusted, original.Upgrades, original.PersistentHealth, original.ExternalInteractions),
                refreshTraits: false);
        }
    }
}

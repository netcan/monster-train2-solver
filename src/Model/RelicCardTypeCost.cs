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

        public RelicCardTypeCost(int effectIndex, string cardType, int amount, bool ignoreRestrictions,
            string? requiredSubtype, int conditionCount)
        { EffectIndex = effectIndex; CardType = cardType; Amount = amount;
            IgnoreRestrictions = ignoreRestrictions; RequiredSubtype = requiredSubtype; ConditionCount = conditionCount; }
    }

    public static class RelicCardTypeCostModel
    {
        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type == RelicModel.ModifyCardTypeCost).Select(item => item.index).ToArray();
            if (indices.Length == 0)
                return relic.CardTypeCosts?.Count > 0 ? "Unexpected relic card-type cost definitions." : null;
            if (relic.CardTypeCosts == null || !indices.SequenceEqual(relic.CardTypeCosts.Select(item => item.EffectIndex)))
                return "Missing ordered relic card-type cost definitions.";
            foreach (RelicCardTypeCost rule in relic.CardTypeCosts)
                if (rule.CardType is not ("Blight" or "Junk") || rule.RequiredSubtype != null || rule.ConditionCount != 0)
                    return "Unmodeled relic card-type cost restrictions or conditions.";
            return null;
        }

        internal static CardInstanceState Apply(CardInstanceState card, RelicCardTypeCost rule, out bool matched)
        {
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

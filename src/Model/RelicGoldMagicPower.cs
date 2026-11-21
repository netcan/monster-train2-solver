using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicGoldMagicPowerModel
    {
        internal const string Effect = "RelicEffectModifyMagicPowerPerGold";
        internal const string MutatorId = "74291ecc-cd0d-4958-9311-98fcf0a90b3f";
        internal const string UpgradeId = "ModifyMagicPowerPerGold";

        internal static string? Validate(CombatRelicState owner, RelicResourceEffect rule) =>
            owner.DataId == MutatorId && owner.DisallowedInPlacementPhase == false && owner.EffectTypes.Count == 1 &&
            rule.EffectIndex == 0 && rule.ParamInt == 1 && rule.Amount == rule.ParamInt && rule.ParamInt2 == 100 &&
            rule.CanApplyInPreviewMode && rule.Conditions.Count == 0 && rule.RequiredMutatorId == null &&
            rule.GoldMagicPowerCards != null && rule.GoldMagicPowerCards.All(id => id > 0) &&
            rule.GoldMagicPowerCards.Distinct().Count() == rule.GoldMagicPowerCards.Count
                ? null : "Missing or changed original gold magic-power definition or card references.";

        internal static RelicFirstCardCostResult Apply(CombatContext source, int ownerIndex, int effectIndex)
        {
            var owner = source.Relics![ownerIndex];
            var rules = owner.ResourceEffects?.ToArray();
            int index = rules == null ? -1 : Array.FindIndex(rules, rule => rule.EffectIndex == effectIndex);
            if (index < 0 || rules == null) return new(source, "Missing ordered gold magic-power effect.");
            RelicResourceEffect rule = rules[index];
            string? error = Validate(owner, rule);
            if (error != null) return new(source, error);
            if (source.Statistics?.DeckCards == null || source.CardInstances == null ||
                source.Resources?.MutatorIds.Contains(MutatorId) != true)
                return new(source, "Gold magic power requires the native permanent deck and mutator input.");

            var tracked = rule.GoldMagicPowerCards!.ToHashSet();
            var context = source;
            int amount = unchecked(rule.Amount * (source.Gold / rule.ParamInt2));
            foreach (int id in source.Statistics.DeckCards)
            {
                CardInstanceState? card = context.FindCard(id);
                bool? eligible = card?.MaskDescriptor?.Definition.CanScaleWithMagicPower;
                if (card == null || !eligible.HasValue)
                    return new(source, "Missing native eligibility for permanent card " + id + ".");
                if (!eligible.Value) continue;

                var temporary = card.Temporary;
                var upgrades = temporary.Upgrades.ToList();
                int position = upgrades.FindIndex(upgrade => upgrade.DataId == UpgradeId);
                var stats = new CardStatModifier(damage: amount, heal: amount);
                CardUpgradeModifier? updated = null;
                if (position >= 0 && tracked.Contains(id))
                {
                    var existing = upgrades[position];
                    var values = existing.Stats;
                    updated = existing.WithStats(new CardStatModifier(amount, values.Health, values.Cost, amount,
                        values.Size, values.XCost, values.EquipmentLimit, values.UpgradeSlotCount));
                    upgrades[position] = updated;
                }
                else if (position < 0)
                    upgrades.Add(Create(stats));
                tracked.Add(id);
                var changed = card.WithModifiers(card.Permanent, new CardModifiers(temporary.Offsets, upgrades,
                    temporary.PersistentHealth, temporary.ExternalInteractions, temporary.BoxedCards, temporary.MergedEquipment));
                context = context.WithCard(changed);
                if (updated?.SharedReferenceId is int reference)
                    context = CardMagicPowerModel.ReplaceShared(context, new Dictionary<int, CardUpgradeModifier> { [reference] = updated });
            }
            rules[index] = rule.WithGoldMagicPowerCards(tracked);
            var owners = context.Relics!.ToArray();
            owners[ownerIndex] = owner.WithResourceEffects(rules);
            return new(context.WithRelics(owners));
        }

        private static CardUpgradeModifier Create(CardStatModifier stats)
        {
            var statuses = Array.Empty<UpgradeMaskStatus>();
            var value = new CardMaskUpgrade(UpgradeId, stats, statuses, false, false, false, false);
            return new CardUpgradeModifier(UpgradeId, "", stats, Array.Empty<CombatStatus>(), false, true, false, 0, 0,
                Array.Empty<string>(), maskMetadata: new CardUpgradeMaskMetadata(statuses, false, false, false, false),
                lifecycle: new CardLifecycleUpgrade(value, "", true, false, Array.Empty<CardTraitValue>(), Array.Empty<string>(),
                    false, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<CardLifecycleTrigger>()));
        }
    }
}

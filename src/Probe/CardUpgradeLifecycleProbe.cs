using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using TypeNameCache = ShinyShoe.TypeNameCache;

namespace MonsterTrain2Poju.Probe
{
    internal sealed class CardUpgradeLifecycleProbe
    {
        private readonly Dictionary<CardUpgradeState, int> upgradeIds = new Dictionary<CardUpgradeState, int>();
        internal int NextInstanceId => upgradeIds.Count + 1;
        internal CardLifecycleUpgrade Upgrade(CardUpgradeState upgrade)
        {
            if (!upgradeIds.TryGetValue(upgrade, out int id)) { id = upgradeIds.Count + 1; upgradeIds.Add(upgrade, id); }
            return Value(upgrade, id);
        }
        internal static CardLifecycleUpgrade Definition(CardUpgradeState upgrade) => Value(upgrade, 0);
        private static CardLifecycleUpgrade Value(CardUpgradeState upgrade, int id)
        {
            var source = upgrade.GetSourceCardUpgradeData();
            bool rescale = source != null && !source.GetUpgradeWillBeScaledByNonMagicPowerTrait();
            var values = new CardMaskUpgrade(upgrade.GetCardUpgradeDataId(),
                new CardStatModifier(upgrade.GetAttackDamage(), upgrade.GetAdditionalHP(), unchecked(-upgrade.GetCostReduction()), upgrade.GetAdditionalHeal(),
                    upgrade.GetAdditionalSize(), upgrade.GetXCostReduction(), upgrade.GetAdditionalEquipmentLimit(), upgrade.GetAdditionalUpgradeSlotCount()),
                upgrade.GetStatusEffectUpgrades().Select(status => new UpgradeMaskStatus(status.statusId, status.count, status.fromPermanentUpgrade)).ToArray(),
                upgrade.GetUpgradeIcon() != null, upgrade.GetHideUpgradeIconOnCard(), upgrade.GetIsRegionRunUpgrade(), upgrade.GetUnitAbilityUpgrade() != null);
            return new CardLifecycleUpgrade(values, upgrade.GetAssetName(), upgrade.IsUnique(), upgrade.GetRemoveOnDiscard(),
                upgrade.GetTraitDataUpgrades().Select(CardTraitCompositionProbe.Definition).ToArray(),
                upgrade.GetRemoveTraitUpgrades().Select(name => TypeNameCache.GetType(name).FullName).ToArray(), upgrade.GetAvoidClobberingExistingTraits(),
                ((List<string>)AccessTools.Field(typeof(CardUpgradeState), "traitsModified").GetValue(upgrade)).ToArray(),
                upgrade.GetUpgradesToRemove().Select(data => data.GetAssetKey()).ToArray(), upgrade.GetCardTriggerUpgrades().Select(Trigger).ToArray(),
                rescale && source!.GetBonusDamage() > 0 ? (int?)source.GetBonusDamage() : null,
                rescale && source!.GetBonusHeal() > 0 ? (int?)source.GetBonusHeal() : null, id);
        }
        private static CardLifecycleTrigger Trigger(CardTriggerEffectData data) => new CardLifecycleTrigger(data.GetTrigger().ToString(), data.GetDescriptionKey(),
            data.GetCardEffects().Select(effect => effect.GetEffectStateName()).ToArray());
        private static CardLifecycleTrigger Trigger(CardTriggerEffectState state)
        {
            var data = state.GetCardTriggerEffectData();
            return new CardLifecycleTrigger(state.GetTrigger().ToString(), state.GetDescriptionKey(), data.GetCardEffects().Select(effect => effect.GetEffectStateName()).ToArray(), state.GetTriggerId());
        }
        internal CardUpgradeLifecycleState Capture(CardState card)
        {
            CardLifecycleTrigger[] Triggers(string name) => ((List<CardTriggerEffectState>)AccessTools.Field(typeof(CardState), name).GetValue(card)).Select(Trigger).ToArray();
            bool standby = card.GetRemoveFromStandByPileOverride(out var pile);
            return new CardUpgradeLifecycleState(CardOwnedMaskProbe.Capture(card), card.GetCardStateModifiers().GetCardUpgrades().Select(Upgrade).ToArray(),
                card.GetTemporaryCardStateModifiers().GetCardUpgrades().Select(Upgrade).ToArray(), Triggers("triggers"), Triggers("upgradeTriggers"), standby, pile.ToString());
        }
        internal static CardState Copy(CardState original)
        {
            object Clone(object value) => AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(value, null);
            var card = (CardState)Clone(original);
            var copies = new Dictionary<CardUpgradeState, CardUpgradeState>();
            CardStateModifiers Modifiers(CardStateModifiers source)
            {
                var result = (CardStateModifiers)Clone(source);
                // Preserve aliases within the copy without retaining mutable upgrades.
                CardUpgradeState Upgrade(CardUpgradeState value)
                {
                    if (!copies.TryGetValue(value, out var copy)) { copy = new CardUpgradeState(); copy.Setup(value); copies.Add(value, copy); }
                    return copy;
                }
                AccessTools.Field(typeof(CardStateModifiers), "cardUpgrades").SetValue(result, source.GetCardUpgrades().Select(Upgrade).ToList());
                AccessTools.Field(typeof(CardStateModifiers), "cardTraitReplacements").SetValue(result, source.GetTraitReplacements().ToList());
                AccessTools.Field(typeof(CardStateModifiers), "temporaryTraits").SetValue(result, source.GetTemporaryTraits().Select(trait => (CardTraitState)Clone(trait)).ToList());
                return result;
            }
            AccessTools.Field(typeof(CardState), "cardModifiers").SetValue(card, Modifiers(original.GetCardStateModifiers()));
            AccessTools.Field(typeof(CardState), "temporaryCardModifiers").SetValue(card, Modifiers(original.GetTemporaryCardStateModifiers()));
            AccessTools.Field(typeof(CardState), "traits").SetValue(card, CardTraitCompositionProbe.Bases(original).Select(trait => (CardTraitState)Clone(trait)).ToList());
            AccessTools.Field(typeof(CardState), "combinedTraits").SetValue(card, CardTraitCompositionProbe.Combined(original).Select(trait => (CardTraitState)Clone(trait)).ToList());
            foreach (string name in new[] { "effects", "triggers", "upgradeTriggers" })
            {
                if (name == "effects") AccessTools.Field(typeof(CardState), name).SetValue(card,
                    ((List<CardEffectState>)AccessTools.Field(typeof(CardState), name).GetValue(original)).Select(effect => (CardEffectState)Clone(effect)).ToList());
                else AccessTools.Field(typeof(CardState), name).SetValue(card,
                    ((List<CardTriggerEffectState>)AccessTools.Field(typeof(CardState), name).GetValue(original)).Select(trigger => (CardTriggerEffectState)Clone(trigger)).ToList());
            }
            return card;
        }
    }
}

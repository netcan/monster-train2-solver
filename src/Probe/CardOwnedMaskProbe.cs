using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class CardOwnedMaskProbe
    {
        internal static CardOwnedMaskState Capture(CardState card)
        {
            var spawn = card.GetSpawnCharacterData(); var effects = card.GetEffects(); var initial = new List<StatusEffectStackData>();
            spawn?.GetStartingStatusEffectsCopy(initial);
            UpgradeMaskStatus Status(StatusEffectStackData item) => new UpgradeMaskStatus(item.statusId, item.count, item.fromPermanentUpgrade);
            bool equipmentAbility = card.GetCardType() == CardType.Equipment && card.TryGetPrimaryUpgradeForEquipment(out var equipment) && equipment.GetUnitAbilityUpgrade() != null;
            var definition = new CardMaskDefinition(card.GetID(), card.GetCardType().ToString(), card.GetRarity().ToString(), card.IsSpawnerCard(), spawn != null,
                spawn != null && spawn.GetCanAttack(), spawn?.GetSubtypes().Select(subtype => subtype.Key).ToArray() ?? Array.Empty<string>(), initial.Select(Status).ToArray(),
                effects.Select(effect => (IReadOnlyList<UpgradeMaskStatus>)effect.GetParamStatusEffectStackData().Select(Status).ToArray()).ToArray(),
                effects.Select(effect => effect.GetEffectStateName()).ToArray(), card.GetLinkedClassID(), card.IsConsumeRemainingEnergyCostType(), spawn?.GetSize() ?? 0,
                (int)card.GetCardTargetMode(), spawn?.GetUnitAbilityCardData() != null, equipmentAbility, spawn?.GetGraftedEquipment() != null);
            CardMaskModifiers Modifiers(CardStateModifiers modifiers)
            {
                int Field(string name) => (int)AccessTools.Field(typeof(CardStateModifiers), name).GetValue(modifiers);
                return new CardMaskModifiers(new CardStatModifier(Field("additionalDamage"), Field("additionalMaxHP"), Field("additionalCost"), Field("additionalHeal"),
                    Field("additionalSize"), Field("additionalXCost"), Field("additionalEquipmentLimit"), Field("additionalUpgradeSlotCount")),
                    modifiers.GetCardUpgrades().Select(upgrade => new CardMaskUpgrade(upgrade.GetCardUpgradeDataId(),
                        new CardStatModifier(upgrade.GetAttackDamage(), upgrade.GetAdditionalHP(), unchecked(-upgrade.GetCostReduction()), upgrade.GetAdditionalHeal(),
                            upgrade.GetAdditionalSize(), upgrade.GetXCostReduction(), upgrade.GetAdditionalEquipmentLimit(), upgrade.GetAdditionalUpgradeSlotCount()),
                        upgrade.GetStatusEffectUpgrades().Select(Status).ToArray(), upgrade.GetUpgradeIcon() != null, upgrade.GetHideUpgradeIconOnCard(),
                        upgrade.GetIsRegionRunUpgrade(), upgrade.GetUnitAbilityUpgrade() != null)).ToArray());
            }
            object Field(string name) => AccessTools.Field(typeof(CardState), name).GetValue(card);
            var permanent = card.GetCardStateModifiers(); var temporary = card.GetTemporaryCardStateModifiers();
            var traits = new CardTraitRefreshState(CardTraitCompositionProbe.Capture(card, true), (bool)Field("refreshCombinedTraitsDirty"),
                permanent.CombinedTraitsDirtyCount, temporary.CombinedTraitsDirtyCount, (int)Field("refreshCombinedTraitsCardModifiersDirtyCount"),
                (int)Field("refreshCombinedTraitsTempCardModifiersDirtyCount"));
            return new CardOwnedMaskState(definition, (int)Field("cost"), Modifiers(permanent), Modifiers(temporary), traits,
                card.GetCardTriggers().Where(trigger => trigger.GetTrigger() == CardTriggerType.OnCast).SelectMany(trigger => trigger.GetCardEffects())
                    .Select(effect => effect.GetEffectStateName()).ToArray(), card.IsPurified, permanent.GraftedEquipmentCardState != null);
        }
    }
}

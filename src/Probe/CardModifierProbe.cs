using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class CardModifierProbe
    {
        internal static CardInstanceState[] Capture(CardManager cards, Func<CardState, int> cardId)
            => Capture(cards.GetAllCards(new List<CardState>()), cardId);

        internal static CardInstanceState[] Capture(IEnumerable<CardState> cards, Func<CardState, int> cardId)
        {
            AllGameManagers managers = AllGameManagers.Instance!;
            return cards.Select(card =>
            {
                var interactions = new List<string>();
                if (card.IsPurified) interactions.Add("Purified card");
                if (card.IsConsumeRemainingEnergyCostType()) interactions.Add("Variable energy cost");
                if (card.IsNonPlayableEnergyCostType()) interactions.Add("Nonplayable card cost type");
                if (card.GetRemoveFromStandByPileOverride(out _)) interactions.Add("Overridden standby return pile");
                CardData data = managers.GetSaveManager().GetAllGameData().FindCardData(card.GetCardDataID())!;
                if ((int)AccessTools.Field(typeof(CardState), "cost").GetValue(card) != data.GetCost()) interactions.Add("Changed base card cost");
                var state = new CardInstanceState(cardId(card), card.GetCardDataID(),
                    Modifiers(card.GetCardStateModifiers()), Modifiers(card.GetTemporaryCardStateModifiers()),
                    card.GetLastPlayedCost(), card.GetLastForgedAmount(), card.GetCurrentScenarioPlayCount(), interactions);
                CardPlayRule rule = CardModifierModel.Resolve(BattleActionProbe.Definition(data), state);
                for (int index = 0; index < managers.GetRoomManager()!.GetNumRooms(); index++)
                    if (card.GetCost(managers.GetCardStatistics(), managers.GetMonsterManager(), managers.GetRelicManager(),
                        managers.GetRoomManager()!.GetRoom(index)) != rule.Cost) interactions.Add("Unmodeled room/trait/hand card cost");
                if (rule.SpawnUnit != null && (card.GetSize() != rule.SpawnUnit.Size ||
                    card.GetTotalAttackDamage() != rule.SpawnUnit.BaseAttack || (int)Math.Round(card.GetHealth()) != rule.SpawnUnit.MaxHealth))
                    interactions.Add("Unmodeled unit card statistics");
                if (rule.Effect == "Spell")
                {
                    var effects = card.GetEffectStates();
                    if (effects.Count != rule.Effects.Count) interactions.Add("Changed card effect sequence");
                    else for (int index = 0; index < effects.Count; index++)
                        if ((rule.Effects[index].Type == "Damage" || rule.Effects[index].Type == "Heal") &&
                            effects[index].GetParamInt() != rule.Effects[index].Value)
                            interactions.Add("Unmodeled spell damage/heal modifier");
                }
                return new CardInstanceState(state.InstanceId, state.DataId, state.Permanent, state.Temporary,
                    state.LastPlayedCost, state.LastForgedAmount, state.PlayCount,
                    interactions.Distinct().OrderBy(value => value, StringComparer.Ordinal).ToArray());
            }).OrderBy(card => card.InstanceId).ToArray();
        }

        private static CardModifiers Modifiers(CardStateModifiers modifiers)
        {
            int Field(string name) => (int)AccessTools.Field(typeof(CardStateModifiers), name).GetValue(modifiers);
            var interactions = new List<string>();
            if (modifiers.GetMergedCardStateIDs().Count > 0) interactions.Add("Merged cards");
            if (modifiers.GraftedEquipmentCardState != null || Field("graftedEquipmentCardStateId") != -1) interactions.Add("Grafted equipment");
            if (modifiers.BoxedCardStates.Count > 0 || ((ICollection)AccessTools.Field(typeof(CardStateModifiers), "boxedCardStateIds")
                .GetValue(modifiers)).Count > 0) interactions.Add("Boxed cards");
            if (modifiers.GetTemporaryTraits().Count > 0 || modifiers.GetTraitReplacements().Count > 0) interactions.Add("Temporary/replaced card traits");
            return new CardModifiers(new CardStatModifier(Field("additionalDamage"), Field("additionalMaxHP"), Field("additionalCost"),
                Field("additionalHeal"), Field("additionalSize"), Field("additionalXCost"), Field("additionalEquipmentLimit"),
                Field("additionalUpgradeSlotCount")), modifiers.GetCardUpgrades().Select(Upgrade).ToArray(),
                modifiers.GetPersistentHP(), interactions);
        }

        internal static CardUpgradeModifier Upgrade(CardUpgradeState upgrade)
        {
            var interactions = new List<string>();
            if (upgrade.GetUnitAbilityUpgrade() != null || upgrade.GetRoomAbilityUpgrade() != null) interactions.Add("Upgrade ability");
            if (upgrade.GetTraitDataUpgrades().Count > 0 || upgrade.GetRemoveTraitUpgrades().Count > 0) interactions.Add("Upgrade traits");
            if (upgrade.GetTriggerUpgrades().Count > 0 || upgrade.GetCardTriggerUpgrades().Count > 0) interactions.Add("Upgrade triggers");
            if (upgrade.GetRoomModifierUpgrades().Count > 0) interactions.Add("Upgrade room modifiers");
            if (upgrade.GetFilters().Count > 0) interactions.Add("Upgrade card filters");
            if (upgrade.GetUpgradesToRemove().Count > 0) interactions.Add("Upgrade replacements");
            return new CardUpgradeModifier(upgrade.GetCardUpgradeDataId(), upgrade.GetAssetName(),
                new CardStatModifier(upgrade.GetAttackDamage(), upgrade.GetAdditionalHP(), -upgrade.GetCostReduction(),
                    upgrade.GetAdditionalHeal(), upgrade.GetAdditionalSize(), upgrade.GetXCostReduction(),
                    upgrade.GetAdditionalEquipmentLimit(), upgrade.GetAdditionalUpgradeSlotCount()),
                upgrade.GetStatusEffectUpgrades().Select(status => BattleActionProbe.Status(status.statusId, status.count)).ToArray(),
                upgrade.GetRemoveOnDiscard(), upgrade.IsUnique(), upgrade.GetExcludeFromClones(), upgrade.GetAdditionalUnhealedHP(),
                upgrade.GetAttackDamageBuff(), interactions, upgrade.GetRestrictSizeToRoomCapacity());
        }
    }
}

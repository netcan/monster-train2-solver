using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicProbe
    {
        private static EquipmentTriggerDefinition[]? equipmentDefinitions;
        internal static bool HasTriggerCounts(AllGameManagers managers) =>
            Current(managers).Any(relic => relic.GetEffects().Any(effect => effect is RelicEffectModifyTriggerCount));
        private static List<RelicState> Current(AllGameManagers managers)
        {
            var relics = new List<RelicState>();
            AccessTools.Method(typeof(RelicManager), "GetCurrentRelics").Invoke(managers.GetRelicManager(), new object[] { relics });
            return relics;
        }
        internal static CombatRelicState[] Capture(AllGameManagers managers)
        {
            // Includes hero blessings, covenants, mutators, Pyre artifacts and souls,
            // in the exact order searched by GetRelicEffect<T>.
            var relics = Current(managers);
            return relics.Select(relic => new CombatRelicState(relic.GetRelicDataID(), relic.GetAssetName(),
                relic.GetEffects().Select(effect => effect.GetType().Name).ToArray())).ToArray();
        }

        internal static TriggerCountState? TriggerCounts(AllGameManagers managers)
        {
            if (!HasTriggerCounts(managers)) return null;
            RelicManager manager = managers.GetRelicManager();
            var modifiers = (Dictionary<CharacterTriggerData.Trigger, int>)AccessTools.Field(typeof(RelicManager), "triggerFireCountModifiers").GetValue(manager);
            var excluded = (List<CardTriggerType>)AccessTools.Field(typeof(RelicManager), "excludedCardTriggersFromCountModifiers").GetValue(manager);
            equipmentDefinitions ??= managers.GetSaveManager().GetAllGameData().GetAllCardData().Where(card => card != null)
                .GroupBy(card => card.GetID()).Select(group => group.First()).Select(card => new EquipmentTriggerDefinition(card.GetID(),
                    card.GetEffects().Where(effect => effect.GetEffectStateName() == "CardEffectAttachEquipment" && effect.GetParamCardUpgradeData() != null)
                        .SelectMany(effect => effect.GetParamCardUpgradeData().GetCharacterTriggerUpgrades()).Select(trigger => trigger.GetTrigger().ToString())
                        .Distinct().ToArray())).OrderBy(card => card.DataId, StringComparer.Ordinal).ToArray();
            return new TriggerCountState(modifiers.Select(item => new TriggerCountModifier(item.Key.ToString(), item.Value)).ToArray(),
                Enum.GetValues(typeof(CharacterTriggerData.Trigger)).Cast<CharacterTriggerData.Trigger>()
                    .Where(kind => manager.HasTriggerCountModifierRelicEffect(kind, Team.Type.Heroes)).Select(kind => kind.ToString()).ToArray(),
                excluded.Select(kind => kind.ToString()).ToArray(), equipmentDefinitions);
        }
    }
}

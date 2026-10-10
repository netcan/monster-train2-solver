using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicCardModifierScenario
    {
        internal static bool Prepared { get; private set; }
        internal static bool CaptureReady { get; private set; }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (!RelicCardModifierProbe.Enabled || Prepared) return;
            if (Environment.GetEnvironmentVariable("MT2_PROBE_RELIC_CARD_STATUS_UPGRADE") == "1" &&
                Environment.GetEnvironmentVariable("MT2_PROBE_RELIC_CARD_PIERCING_UPGRADE") == "1")
                throw new InvalidOperationException("Relic card status and piercing scenarios are mutually exclusive.");
            var save = managers.GetSaveManager();
            var original = save.GetAllGameData().GetAllCollectableRelicData().Single(relic => relic.name == "ReduceStarterCost");
            var cards = managers.GetCardManager()!;
            var owned = cards.GetAllCards(new List<CardState>());
            // The generation fixture creates copies of its casting spell. Keep
            // that authored test spell paid after the unchanged original relic.
            var generators = owned.Where(card => card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectAddBattleCard"))
                .Select(card => card.GetCardDataID()).Distinct().ToArray();
            foreach (string id in generators)
            {
                var definition = save.GetAllGameData().FindCardData(id)!;
                AccessTools.Field(typeof(CardData), "cost").SetValue(definition, 2);
                foreach (var card in owned.Where(card => card.GetCardDataID() == id)) AccessTools.Field(typeof(CardState), "cost").SetValue(card, 2);
            }
            save.AddRelic(original);
            RelicEffectAddTempUpgrade relicEffect = CurrentRelic(managers).GetEffects().OfType<RelicEffectAddTempUpgrade>().Single();
            if (Environment.GetEnvironmentVariable("MT2_PROBE_RELIC_CARD_STATUS_UPGRADE") == "1")
                InstallArmorUpgrade(save.GetAllGameData(), relicEffect);
            if (Environment.GetEnvironmentVariable("MT2_PROBE_RELIC_CARD_PIERCING_UPGRADE") == "1")
                InstallPiercingUpgrade(save.GetAllGameData(), relicEffect);
            var filters = UpgradeData(relicEffect).GetFilters();
            var eligible = owned.Where(card => filters.All(filter => filter.FilterCard(card, managers.GetRelicManager()))).Take(2).ToArray();
            if (eligible.Length != 2) throw new InvalidOperationException("Relic scenario lacks eligible repeated/reset owned cards.");
            CaptureReady = true;
            foreach (var card in eligible)
            {
                managers.GetRelicManager().ApplyCardStateModifiers(card, resetTempCardModifiers: false);
                managers.GetRelicManager().ApplyCardStateModifiers(card);
            }
            if (Environment.GetEnvironmentVariable("MT2_PROBE_CONDITIONAL_BATTLE_RELIC_UPGRADES") == "1")
                InstallOncePerTurnCondition(managers, eligible[0]);
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("RELIC-CARD-MODIFIERS-PREPARED unchanged original ReduceStarterCost, native acquire/repeat/reset and battle-generated cards; conditional=" +
                (Environment.GetEnvironmentVariable("MT2_PROBE_CONDITIONAL_BATTLE_RELIC_UPGRADES") == "1") + ", status=" +
                (Environment.GetEnvironmentVariable("MT2_PROBE_RELIC_CARD_STATUS_UPGRADE") == "1") + ".");

            static RelicState CurrentRelic(AllGameManagers managers)
            {
                var current = new List<RelicState>();
                AccessTools.Method(typeof(RelicManager), "GetCurrentRelics").Invoke(managers.GetRelicManager(), new object[] { current });
                return current.Single(relic => relic.GetAssetName() == "ReduceStarterCost");
            }

            static CardUpgradeData UpgradeData(RelicEffectAddTempUpgrade effect) =>
                (CardUpgradeData)AccessTools.Field(typeof(RelicEffectAddTempUpgrade), "_cardUpgradeData").GetValue(effect);

            static void InstallArmorUpgrade(AllGameData gameData, RelicEffectAddTempUpgrade effect)
            {
                CardUpgradeData original = UpgradeData(effect);
                CardUpgradeData? source = gameData.GetAllCardUpgradeData().FirstOrDefault(upgrade =>
                    upgrade.GetStatusEffectUpgrades().Count > 0 &&
                    upgrade.GetStatusEffectUpgrades().All(status => status.statusId == "armor" && status.count > 0) &&
                    !upgrade.IsUnique() && upgrade.GetTraitDataUpgrades().Count == 0 && upgrade.GetRemoveTraitUpgrades().Count == 0 &&
                    upgrade.GetCharacterTriggerUpgrades().Count == 0 && upgrade.GetCardTriggerUpgrades().Count == 0 &&
                    upgrade.GetRoomModifierUpgrades().Count == 0 && upgrade.GetUpgradesToRemove().Count == 0 &&
                    upgrade.GetUnitAbilityUpgrade() == null && upgrade.GetRoomAbilityUpgrade() == null && upgrade.GetBonusSize() == 0);
                if (source == null) throw new InvalidOperationException("Game data has no isolated armor-only card upgrade.");
                var clone = (CardUpgradeData)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(source, null)!;
                var monsterMask = UnityEngine.ScriptableObject.CreateInstance<CardUpgradeMaskData>();
                monsterMask.name = "PojuRelicCardStatusMonsterMask";
                AccessTools.Field(typeof(CardUpgradeMaskData), "cardType").SetValue(monsterMask, CardType.Monster);
                AccessTools.Field(typeof(CardUpgradeData), "filters").SetValue(clone, new List<CardUpgradeMaskData> { monsterMask });
                AccessTools.Field(typeof(CardUpgradeData), "isUnique").SetValue(clone, original.IsUnique());
                AccessTools.Field(typeof(RelicEffectAddTempUpgrade), "_cardUpgradeData").SetValue(effect, clone);
            }

            static void InstallPiercingUpgrade(AllGameData gameData, RelicEffectAddTempUpgrade effect)
            {
                CardUpgradeData source = gameData.GetAllCardUpgradeData().Single(upgrade => upgrade.GetAssetKey() == "StingBuffPiercing");
                if (!source.GetTraitDataUpgrades().Select(trait => trait.GetTraitStateName()).SequenceEqual(new[] { "CardTraitIgnoreArmor" }) ||
                    source.GetRemoveTraitUpgrades().Count != 0 || source.GetCardTriggerUpgrades().Count != 0 ||
                    source.GetCharacterTriggerUpgrades().Count != 0 || source.GetRoomModifierUpgrades().Count != 0 ||
                    source.GetUnitAbilityUpgrade() != null || source.GetRoomAbilityUpgrade() != null)
                    throw new InvalidOperationException("Original StingBuffPiercing upgrade changed.");
                var clone = (CardUpgradeData)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(source, null)!;
                var monsterMask = UnityEngine.ScriptableObject.CreateInstance<CardUpgradeMaskData>();
                monsterMask.name = "PojuRelicPiercingMonsterMask";
                AccessTools.Field(typeof(CardUpgradeMaskData), "cardType").SetValue(monsterMask, CardType.Monster);
                AccessTools.Field(typeof(CardUpgradeData), "filters").SetValue(clone, new List<CardUpgradeMaskData> { monsterMask });
                AccessTools.Field(typeof(RelicEffectAddTempUpgrade), "_cardUpgradeData").SetValue(effect, clone);
            }

            static void InstallOncePerTurnCondition(AllGameManagers managers, CardState sample)
            {
                RelicEffectAddTempUpgrade effect = CurrentRelic(managers).GetEffects().OfType<RelicEffectAddTempUpgrade>().Single();
                var condition = new RelicEffectCondition();
                void Set(string name, object value) => AccessTools.Field(typeof(RelicEffectCondition), name).SetValue(condition, value);
                Set("paramTrackedValue", CardStatistics.TrackedValueType.TimesPlayed);
                Set("paramCardType", CardStatistics.CardTypeTarget.Any);
                Set("paramTrackTriggerCount", true);
                Set("paramEntryDuration", CardStatistics.EntryDuration.ThisTurn);
                Set("paramComparator", RelicEffectCondition.Comparator.LessThan);
                Set("paramInt", 1);
                Set("allowMultipleTriggersPerDuration", true);
                ((List<RelicEffectCondition>)AccessTools.Field(typeof(RelicEffectBase), "_effectConditions").GetValue(effect)).Add(condition);
                managers.GetRelicManager().ApplyCardStateModifiers(sample, resetTempCardModifiers: false);
                managers.GetRelicManager().ApplyCardStateModifiers(sample, resetTempCardModifiers: false);
            }
        }
    }
}

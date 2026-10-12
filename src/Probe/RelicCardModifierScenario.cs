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
        internal static bool AllUnitsMultistrikeMutator =>
            Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "all-units-multistrike-mutator";
        internal static bool SupportedMutatorContext => Prepared && AllUnitsMultistrikeMutator;
        internal static bool ExhaustUpgrade => Environment.GetEnvironmentVariable("MT2_PROBE_RELIC_CARD_EXHAUST_UPGRADE") == "1";
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (!RelicCardModifierProbe.Enabled || Prepared) return;
            int selectedScenario = new[] { "MT2_PROBE_RELIC_CARD_STATUS_UPGRADE", "MT2_PROBE_RELIC_CARD_PIERCING_UPGRADE",
                "MT2_PROBE_RELIC_CARD_SELF_PURGE_UPGRADE", "MT2_PROBE_RELIC_CARD_EXHAUST_UPGRADE", "MT2_PROBE_RELIC_CARD_ABILITY_UPGRADE",
                "MT2_PROBE_RELIC_CARD_TRIGGER_UPGRADE", "MT2_PROBE_RELIC_CARD_REPLACEMENT_UPGRADE" }
                .Count(name => Environment.GetEnvironmentVariable(name) == "1");
            if (AllUnitsMultistrikeMutator) selectedScenario++;
            if (selectedScenario > 1) throw new InvalidOperationException("Relic card status, piercing, self-purge, exhaust, ability and trigger scenarios are mutually exclusive.");
            var save = managers.GetSaveManager();
            var data = save.GetAllGameData();
            CollectableRelicData? originalRelic = null;
            MutatorData? originalMutator = null;
            if (AllUnitsMultistrikeMutator)
            {
                originalMutator = data.FindMutatorData("626d64ba-8548-4a10-9240-b40d40b26485");
                if (originalMutator == null || originalMutator.name != "AllUnitsMultistrike")
                    throw new InvalidOperationException("Original AllUnitsMultistrike mutator identity changed.");
                string[] expectedEffects = { "RelicEffectAddTempUpgrade", "RelicEffectAddStatusEffectOnSpawn" };
                if (!originalMutator.GetEffects().Select(effect => effect.GetEffectClassName()).SequenceEqual(expectedEffects))
                    throw new InvalidOperationException("Original AllUnitsMultistrike effect order changed.");
                RelicEffectData effect = originalMutator.GetEffects().Single(item => item.GetEffectClassName() == "RelicEffectAddTempUpgrade");
                CardUpgradeData upgrade = effect.GetParamCardUpgradeData();
                Team.Type sourceTeam = effect.GetParamSourceTeam();
                if (sourceTeam != Team.Type.Monsters || upgrade == null || upgrade.name != "Multistrike1" ||
                    !upgrade.IsUnique() || !upgrade.GetStatusEffectUpgrades().Any(status => status.statusId == "multistrike" && status.count == 1) ||
                    !upgrade.GetFilters().Any(filter => filter.name == "OnlyUnit"))
                    throw new InvalidOperationException("Original AllUnitsMultistrike card upgrade changed.");
            }
            else
            {
                originalRelic = data.GetAllCollectableRelicData().Single(relic => relic.name == "ReduceStarterCost");
            }
            var cards = managers.GetCardManager()!;
            var owned = cards.GetAllCards(new List<CardState>());
            // The generation fixture creates copies of its casting spell. Keep
            // that authored test spell paid after the unchanged original relic.
            var generators = AllUnitsMultistrikeMutator ? Array.Empty<string>() :
                owned.Where(card => card.GetEffects().Any(effect => effect.GetEffectStateName() == "CardEffectAddBattleCard"))
                    .Select(card => card.GetCardDataID()).Distinct().ToArray();
            foreach (string id in generators)
            {
                var definition = data.FindCardData(id)!;
                AccessTools.Field(typeof(CardData), "cost").SetValue(definition, 2);
                foreach (var card in owned.Where(card => card.GetCardDataID() == id)) AccessTools.Field(typeof(CardState), "cost").SetValue(card, 2);
            }
            if (originalMutator != null)
            {
                save.AddMutator(originalMutator);
                var activeMutators = save.GetMutators();
                if (activeMutators.Count != 1 || activeMutators[0].GetAssetName() != "AllUnitsMultistrike")
                    throw new InvalidOperationException("The native AllUnitsMultistrike mutator did not install as the only run mutator.");
            }
            else save.AddRelic(originalRelic!);
            RelicEffectAddTempUpgrade relicEffect = CurrentRelic(managers).GetEffects().OfType<RelicEffectAddTempUpgrade>().Single();
            if (!AllUnitsMultistrikeMutator && Environment.GetEnvironmentVariable("MT2_PROBE_RELIC_CARD_STATUS_UPGRADE") == "1")
                InstallArmorUpgrade(save.GetAllGameData(), relicEffect);
            if (!AllUnitsMultistrikeMutator && Environment.GetEnvironmentVariable("MT2_PROBE_RELIC_CARD_PIERCING_UPGRADE") == "1")
                InstallPiercingUpgrade(save.GetAllGameData(), relicEffect);
            if (!AllUnitsMultistrikeMutator && Environment.GetEnvironmentVariable("MT2_PROBE_RELIC_CARD_SELF_PURGE_UPGRADE") == "1")
                InstallSelfPurgeUpgrade(relicEffect);
            if (!AllUnitsMultistrikeMutator && ExhaustUpgrade) InstallExhaustUpgrade(relicEffect);
            bool abilityScenario = !AllUnitsMultistrikeMutator && Environment.GetEnvironmentVariable("MT2_PROBE_RELIC_CARD_ABILITY_UPGRADE") == "1";
            if (abilityScenario) InstallStewardAbilityUpgrade(save.GetAllGameData(), relicEffect);
            bool triggerScenario = !AllUnitsMultistrikeMutator && Environment.GetEnvironmentVariable("MT2_PROBE_RELIC_CARD_TRIGGER_UPGRADE") == "1";
            if (triggerScenario) InstallCharacterTriggerUpgrade(save.GetAllGameData(), relicEffect);
            bool replacementScenario = !AllUnitsMultistrikeMutator && Environment.GetEnvironmentVariable("MT2_PROBE_RELIC_CARD_REPLACEMENT_UPGRADE") == "1";
            CardUpgradeData? replacementTarget = replacementScenario ? InstallReplacementUpgrade(save.GetAllGameData(), relicEffect) : null;
            var filters = UpgradeData(relicEffect).GetFilters();
            var eligible = owned.Where(card => filters.All(filter => filter.FilterCard(card, managers.GetRelicManager())))
                .Where(card => !ExhaustUpgrade || card.GetTraitStates().All(trait =>
                    trait is not CardTraitSelfPurge && trait is not CardTraitExhaustState))
                .Take(AllUnitsMultistrikeMutator || abilityScenario || triggerScenario || replacementScenario ? 1 : 2).ToArray();
            int requiredEligibleCards = AllUnitsMultistrikeMutator || abilityScenario || triggerScenario || replacementScenario ? 1 : 2;
            if (eligible.Length != requiredEligibleCards)
                throw new InvalidOperationException("Relic scenario has " + eligible.Length + " eligible owned cards; required " + requiredEligibleCards + ".");
            if (replacementTarget != null)
            {
                var prior = new CardUpgradeState(); prior.Setup(replacementTarget);
                if (!eligible[0].ApplyTemporaryUpgrade(prior, save))
                    throw new InvalidOperationException("Could not install the native temporary upgrade selected for replacement.");
            }
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
            log.LogInfo((AllUnitsMultistrikeMutator ? "ALL-UNITS-MULTISTRIKE-MUTATOR-PREPARED original AllUnitsMultistrike; " :
                "RELIC-CARD-MODIFIERS-PREPARED unchanged original ReduceStarterCost, native acquire/repeat/reset and battle-generated cards; ") +
                "conditional=" +
                (Environment.GetEnvironmentVariable("MT2_PROBE_CONDITIONAL_BATTLE_RELIC_UPGRADES") == "1") + ", status=" +
                (Environment.GetEnvironmentVariable("MT2_PROBE_RELIC_CARD_STATUS_UPGRADE") == "1") + ", ability=" + abilityScenario +
                ", trigger=" + triggerScenario + ", exhaust=" + ExhaustUpgrade + ".");

            static RelicState CurrentRelic(AllGameManagers managers)
            {
                var current = new List<RelicState>();
                AccessTools.Method(typeof(RelicManager), "GetCurrentRelics").Invoke(managers.GetRelicManager(), new object[] { current });
                return current.Single(relic => relic.GetAssetName() == (AllUnitsMultistrikeMutator ? "AllUnitsMultistrike" : "ReduceStarterCost"));
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

            static void InstallSelfPurgeUpgrade(RelicEffectAddTempUpgrade effect)
            {
                CardUpgradeData original = UpgradeData(effect);
                var clone = (CardUpgradeData)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(original, null)!;
                var selfPurge = new CardTraitData();
                selfPurge.Setup("CardTraitSelfPurge");
                AccessTools.Field(typeof(CardUpgradeData), "traitDataUpgrades").SetValue(clone, new List<CardTraitData> { selfPurge });
                var spellMask = UnityEngine.ScriptableObject.CreateInstance<CardUpgradeMaskData>();
                spellMask.name = "PojuRelicSelfPurgeSpellMask";
                AccessTools.Field(typeof(CardUpgradeMaskData), "cardType").SetValue(spellMask, CardType.Spell);
                AccessTools.Field(typeof(CardUpgradeData), "filters").SetValue(clone, new List<CardUpgradeMaskData> { spellMask });
                AccessTools.Field(typeof(CardUpgradeData), "isUnique").SetValue(clone, original.IsUnique());
                AccessTools.Field(typeof(RelicEffectAddTempUpgrade), "_cardUpgradeData").SetValue(effect, clone);
            }

            static void InstallExhaustUpgrade(RelicEffectAddTempUpgrade effect)
            {
                CardUpgradeData original = UpgradeData(effect);
                var clone = (CardUpgradeData)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(original, null)!;
                var exhaust = new CardTraitData();
                exhaust.Setup("CardTraitExhaustState");
                AccessTools.Field(typeof(CardUpgradeData), "traitDataUpgrades").SetValue(clone, new List<CardTraitData> { exhaust });
                var spellMask = UnityEngine.ScriptableObject.CreateInstance<CardUpgradeMaskData>();
                spellMask.name = "PojuRelicExhaustSpellMask";
                AccessTools.Field(typeof(CardUpgradeMaskData), "cardType").SetValue(spellMask, CardType.Spell);
                AccessTools.Field(typeof(CardUpgradeData), "filters").SetValue(clone, new List<CardUpgradeMaskData> { spellMask });
                AccessTools.Field(typeof(CardUpgradeData), "isUnique").SetValue(clone, original.IsUnique());
                AccessTools.Field(typeof(RelicEffectAddTempUpgrade), "_cardUpgradeData").SetValue(effect, clone);
            }

            static void InstallStewardAbilityUpgrade(AllGameData gameData, RelicEffectAddTempUpgrade effect)
            {
                CardUpgradeData source = gameData.GetAllCardUpgradeData().Single(upgrade =>
                    upgrade.GetAssetKey() == "StewardSacrificeDamage_Upgrade");
                if (source.GetUnitAbilityUpgrade() == null || source.GetFilters().Count == 0 || !source.IsUnique())
                    throw new InvalidOperationException("Original StewardSacrificeDamage upgrade definition changed.");
                AccessTools.Field(typeof(RelicEffectAddTempUpgrade), "_cardUpgradeData").SetValue(effect, source);
            }

            static void InstallCharacterTriggerUpgrade(AllGameData gameData, RelicEffectAddTempUpgrade effect)
            {
                CardUpgradeData source = gameData.GetAllCardUpgradeData().Single(upgrade =>
                    upgrade.GetAssetKey() == "PyreHeartSavagery_UpgradeData");
                if (source.GetCharacterTriggerUpgrades().Select(trigger => trigger.GetTrigger().ToString())
                        .SequenceEqual(new[] { "OnKill" }) != true || source.GetCardTriggerUpgrades().Count != 0 ||
                    source.GetTraitDataUpgrades().Count != 0 || source.GetStatusEffectUpgrades().Count != 0 ||
                    source.GetUnitAbilityUpgrade() != null || source.GetRoomAbilityUpgrade() != null ||
                    source.GetRoomModifierUpgrades().Count != 0 || source.GetFilters().Count != 1 || !source.IsUnique())
                    throw new InvalidOperationException("Original PyreHeartSavagery upgrade definition changed.");
                AccessTools.Field(typeof(RelicEffectAddTempUpgrade), "_cardUpgradeData").SetValue(effect, source);
            }

            static CardUpgradeData InstallReplacementUpgrade(AllGameData gameData, RelicEffectAddTempUpgrade effect)
            {
                CardUpgradeData original = UpgradeData(effect);
                CardUpgradeData? target = gameData.GetAllCardUpgradeData().FirstOrDefault(upgrade =>
                    upgrade.GetStatusEffectUpgrades().Count > 0 &&
                    upgrade.GetStatusEffectUpgrades().All(status => status.statusId == "armor" && status.count > 0) &&
                    upgrade.GetTraitDataUpgrades().Count == 0 && upgrade.GetRemoveTraitUpgrades().Count == 0 &&
                    upgrade.GetCharacterTriggerUpgrades().Count == 0 && upgrade.GetCardTriggerUpgrades().Count == 0 &&
                    upgrade.GetRoomModifierUpgrades().Count == 0 && upgrade.GetUpgradesToRemove().Count == 0 &&
                    upgrade.GetUnitAbilityUpgrade() == null && upgrade.GetRoomAbilityUpgrade() == null && upgrade.GetBonusSize() == 0);
                if (target == null || target.GetAssetKey() == original.GetAssetKey())
                    throw new InvalidOperationException("Game data has no isolated armor upgrade to replace.");
                var clone = (CardUpgradeData)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(original, null)!;
                AccessTools.Field(typeof(CardUpgradeData), "upgradesToRemove").SetValue(clone, new List<CardUpgradeData> { target });
                AccessTools.Field(typeof(CardUpgradeData), "isUnique").SetValue(clone, false);
                AccessTools.Field(typeof(RelicEffectAddTempUpgrade), "_cardUpgradeData").SetValue(effect, clone);
                return target;
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

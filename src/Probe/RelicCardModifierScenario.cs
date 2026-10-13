using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

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
        internal static bool EphemeralUpgrade => Environment.GetEnvironmentVariable("MT2_PROBE_RELIC_CARD_EPHEMERAL_UPGRADE") == "1";
        internal static bool ExtraToughTombs => Environment.GetEnvironmentVariable("MT2_PROBE_RELIC_CARD_HEALTH_UPGRADE") == "1";
        internal static bool UnhealedHealthUpgrade => Environment.GetEnvironmentVariable("MT2_PROBE_RELIC_CARD_UNHEALED_UPGRADE") == "1";
        internal const string TombSubtypeKey = "SubtypesData_Tomb";
        internal static string? TombCarrierCardId { get; private set; }
        internal static bool TombCarrierPrepared { get; private set; }
        internal static PlayCardAction? ChooseTombCarrier(BattleTurnState state)
        {
            CardToken? tomb = state.Spawn.Train.Context?.Cards.Hand.FirstOrDefault(card => card.DataId == TombCarrierCardId);
            return tomb == null ? null : BattleActionModel.EnumerateSupportedPlays(state)
                .FirstOrDefault(action => action.CardInstanceId == tomb.InstanceId);
        }
        private static void AuthorTombCarrier(CardData card)
        {
            Set(card, "cardType", CardType.Monster); Set(card, "cost", 0);
            Set(card, "costType", CardData.CostType.Default); Set(card, "targetless", false);
            Set(card, "targetsRoom", true); Set(card, "isUnitAbility", false); Set(card, "isRoomAbility", false);
            Set(card, "traits", new List<CardTraitData>()); Set(card, "effectTriggers", new List<CharacterTriggerData>());
            Set(card, "triggers", new List<CardTriggerEffectData>()); Set(card, "startingUpgrades", new List<CardUpgradeData>());
        }
        private static void Set(object target, string field, object? value) =>
            AccessTools.Field(target.GetType(), field).SetValue(target, value);

        internal static void AcquireTombCarrierBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (!ExtraToughTombs || TombCarrierPrepared) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("ExtraToughTombs carrier must be added before native battle setup.");
            AllGameData data = save.GetAllGameData();
            CardData template = save.GetDeckState().Select(card => data.FindCardData(card.GetCardDataID())!)
                .First(card => card.GetSpawnCharacterData()?.name.StartsWith("TrainSteward", StringComparison.Ordinal) == true);
            CharacterData steward = template.GetSpawnCharacterData()!;
            CharacterData tomb = UnityEngine.Object.Instantiate(steward);
            tomb.name = "TrainStewardPojuTomb";
            Set(tomb, "id", "fc119000-0000-4000-8000-000000000098");
            var subtypes = steward.GetSubtypes().Select(subtype => subtype.Key).ToList();
            if (!subtypes.Contains(TombSubtypeKey, StringComparer.Ordinal)) subtypes.Add(TombSubtypeKey);
            Set(tomb, "subtypeKeys", subtypes);
            Set(tomb, "startingStatusEffects", Array.Empty<StatusEffectStackData>());
            Set(tomb, "triggers", new List<CharacterTriggerData>());
            Set(tomb, "unitAbility", null);
            ((List<CharacterData>)AccessTools.Field(typeof(AllGameData), "characterDatas").GetValue(data)).Add(tomb);
            CardData carrier = UnityEngine.Object.Instantiate(template);
            carrier.name = "PojuExtraToughTombsCarrier";
            TombCarrierCardId = "fc119000-0000-4000-8000-000000000099";
            Set(carrier, "id", TombCarrierCardId);
            AuthorTombCarrier(carrier);
            var spawn = new CardEffectData("CardEffectSpawnMonster", null!, Team.Type.Monsters);
            spawn.Cheat_SetTargetMode(TargetMode.Room);
            Set(spawn, "paramInt", 1);
            Set(spawn, "paramCharacterData", tomb);
            Set(carrier, "effects", new List<CardEffectData> { spawn });
            ((List<CardData>)AccessTools.Field(typeof(AllGameData), "cardDatas").GetValue(data)).Add(carrier);
            save.AddCardToDeck(carrier, applyExistingRelicModifiers: false, extraCopies: 0,
                applyExtraCopiesMutator: false, showAnimation: false, setupStartingUpgrades: true, notifyDeckCount: false);
            TombCarrierPrepared = true;
            log.LogInfo("EXTRA-TOUGH-TOMBS-CARRIER-ACQUIRED testCharacter=" + tomb.GetAssetKey() +
                " subtype=" + TombSubtypeKey + " card=" + TombCarrierCardId + "; cloned from original TrainSteward and added before native deck initialization.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (!RelicCardModifierProbe.Enabled || Prepared) return;
            int selectedScenario = new[] { "MT2_PROBE_RELIC_CARD_STATUS_UPGRADE", "MT2_PROBE_RELIC_CARD_PIERCING_UPGRADE",
                "MT2_PROBE_RELIC_CARD_SELF_PURGE_UPGRADE", "MT2_PROBE_RELIC_CARD_EXHAUST_UPGRADE", "MT2_PROBE_RELIC_CARD_ABILITY_UPGRADE",
                "MT2_PROBE_RELIC_CARD_TRIGGER_UPGRADE", "MT2_PROBE_RELIC_CARD_REPLACEMENT_UPGRADE",
                "MT2_PROBE_RELIC_CARD_EPHEMERAL_UPGRADE", "MT2_PROBE_RELIC_CARD_HEALTH_UPGRADE",
                "MT2_PROBE_RELIC_CARD_UNHEALED_UPGRADE" }
                .Count(name => Environment.GetEnvironmentVariable(name) == "1");
            if (AllUnitsMultistrikeMutator) selectedScenario++;
            if (selectedScenario > 1) throw new InvalidOperationException("Relic card status, piercing, self-purge, exhaust, Ephemeral, health, unhealed-health, ability, trigger and replacement scenarios are mutually exclusive.");
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
                originalRelic = data.GetAllCollectableRelicData().Single(relic =>
                    relic.name == (ExtraToughTombs ? "ExtraToughTombs" : "ReduceStarterCost"));
            }
            var cards = managers.GetCardManager()!;
            if (ExtraToughTombs)
            {
                if (!TombCarrierPrepared || string.IsNullOrEmpty(TombCarrierCardId))
                    throw new InvalidOperationException("ExtraToughTombs carrier was not added before native battle setup.");
                StatusEffectData ephemeral = StatusEffectManager.Instance?.GetStatusEffectDataById("ephemeral") ??
                    throw new InvalidOperationException("Native Ephemeral status metadata is unavailable.");
                log.LogInfo("EXTRA-TOUGH-TOMBS-EPHEMERAL-DATA stage=" + ephemeral.GetPrimaryTriggerStage() +
                    " additionalStages=" + string.Join(",", ephemeral.GetAdditionalTriggerStages()) +
                    " removeWhenTriggered=" + ephemeral.GetRemoveWhenTriggered() +
                    " removeStackAtEnd=" + ephemeral.GetRemoveStackAtEndOfTurn() +
                    " removeAtEnd=" + ephemeral.GetRemoveAtEndOfTurn() +
                    " removeAfterPostCombat=" + ephemeral.GetRemoveAtEndOfTurnAfterPostCombat());
            }
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
            if (!AllUnitsMultistrikeMutator && EphemeralUpgrade) InstallEphemeralUpgrade(relicEffect);
            if (!AllUnitsMultistrikeMutator && UnhealedHealthUpgrade) InstallUnhealedHealthUpgrade(relicEffect);
            bool abilityScenario = !AllUnitsMultistrikeMutator && Environment.GetEnvironmentVariable("MT2_PROBE_RELIC_CARD_ABILITY_UPGRADE") == "1";
            if (abilityScenario) InstallStewardAbilityUpgrade(save.GetAllGameData(), relicEffect);
            bool triggerScenario = !AllUnitsMultistrikeMutator && Environment.GetEnvironmentVariable("MT2_PROBE_RELIC_CARD_TRIGGER_UPGRADE") == "1";
            if (triggerScenario) InstallCharacterTriggerUpgrade(save.GetAllGameData(), relicEffect);
            bool replacementScenario = !AllUnitsMultistrikeMutator && Environment.GetEnvironmentVariable("MT2_PROBE_RELIC_CARD_REPLACEMENT_UPGRADE") == "1";
            CardUpgradeData? replacementTarget = replacementScenario ? InstallReplacementUpgrade(save.GetAllGameData(), relicEffect) : null;
            var filters = UpgradeData(relicEffect).GetFilters();
            if (ExtraToughTombs)
            {
                CardState? carrier = owned.SingleOrDefault(card => card.GetCardDataID() == TombCarrierCardId);
                CharacterData? spawn = carrier?.GetSpawnCharacterData();
                log.LogInfo("EXTRA-TOUGH-TOMBS-FILTER cardFound=" + (carrier != null) + " type=" + carrier?.GetCardType() +
                    " rarity=" + carrier?.GetRarity() + " cost=" + (carrier == null ? "<missing>" : AccessTools.Field(typeof(CardState), "cost").GetValue(carrier)) +
                    " isSpawner=" + carrier?.IsSpawnerCard() +
                    " spawn=" + spawn?.GetAssetKey() + " subtypes=" + string.Join(",", spawn?.GetSubtypes().Select(subtype => subtype.Key) ?? Array.Empty<string>()) +
                    " filters=" + string.Join(",", filters.Select(filter => filter.name + ":" +
                        (carrier == null ? false : filter.FilterCard(carrier, managers.GetRelicManager())))));
            }
            var eligibleCandidates = owned.Where(card => filters.All(filter => filter.FilterCard(card, managers.GetRelicManager())))
                .Where(card => !ExtraToughTombs || card.GetCardDataID() == TombCarrierCardId)
                .Where(card => !UnhealedHealthUpgrade || card.IsSpawnerCard())
                .Where(card => !ExhaustUpgrade || card.GetTraitStates().All(trait =>
                    trait is not CardTraitSelfPurge && trait is not CardTraitExhaustState))
                .Where(card => !EphemeralUpgrade || card.GetTraitStates().All(trait =>
                    trait is not CardTraitEphemeral && trait is not CardTraitSelfPurge && trait is not CardTraitExhaustState)).ToArray();
            if (EphemeralUpgrade)
            {
                CardState[] distinctCandidates = eligibleCandidates.GroupBy(card => card.GetCardDataID())
                    .Select(group => group.First()).ToArray();
                if (distinctCandidates.Length < 2) throw new InvalidOperationException("Ephemeral relic scenario needs two distinct eligible spells.");
                var costField = AccessTools.Field(typeof(CardState), "cost");
                CardState heldCard = distinctCandidates.OrderByDescending(card => (int)costField.GetValue(card)).First();
                CardState playableCard = distinctCandidates.Where(card => card.GetCardDataID() != heldCard.GetCardDataID())
                    .OrderBy(card => (int)costField.GetValue(card)).First();
                costField.SetValue(heldCard, 99);
                eligibleCandidates = new[] { heldCard, playableCard };
            }
            var eligible = eligibleCandidates
                .Take(AllUnitsMultistrikeMutator || abilityScenario || triggerScenario || replacementScenario || ExtraToughTombs || UnhealedHealthUpgrade ? 1 : 2).ToArray();
            int requiredEligibleCards = AllUnitsMultistrikeMutator || abilityScenario || triggerScenario || replacementScenario || ExtraToughTombs || UnhealedHealthUpgrade ? 1 : 2;
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
            if ((ExtraToughTombs || UnhealedHealthUpgrade) && !cards.GetHand().Contains(eligible[0]) && !cards.DrawSpecificCard(eligible[0]))
                throw new InvalidOperationException("Could not draw the relic-upgraded carrier into the battle hand.");
            if (Environment.GetEnvironmentVariable("MT2_PROBE_CONDITIONAL_BATTLE_RELIC_UPGRADES") == "1")
                InstallOncePerTurnCondition(managers, eligible[0]);
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo((AllUnitsMultistrikeMutator ? "ALL-UNITS-MULTISTRIKE-MUTATOR-PREPARED original AllUnitsMultistrike; " :
                ExtraToughTombs ? "EXTRA-TOUGH-TOMBS-PREPARED unchanged original relic and pre-battle Tomb carrier; " :
                "RELIC-CARD-MODIFIERS-PREPARED unchanged original ReduceStarterCost, native acquire/repeat/reset and battle-generated cards; ") +
                "conditional=" +
                (Environment.GetEnvironmentVariable("MT2_PROBE_CONDITIONAL_BATTLE_RELIC_UPGRADES") == "1") + ", status=" +
                (Environment.GetEnvironmentVariable("MT2_PROBE_RELIC_CARD_STATUS_UPGRADE") == "1") + ", ability=" + abilityScenario +
                ", trigger=" + triggerScenario + ", exhaust=" + ExhaustUpgrade + ".");

            static RelicState CurrentRelic(AllGameManagers managers)
            {
                var current = new List<RelicState>();
                AccessTools.Method(typeof(RelicManager), "GetCurrentRelics").Invoke(managers.GetRelicManager(), new object[] { current });
                return current.Single(relic => relic.GetAssetName() == (AllUnitsMultistrikeMutator ? "AllUnitsMultistrike" :
                    ExtraToughTombs ? "ExtraToughTombs" : "ReduceStarterCost"));
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

            static void InstallUnhealedHealthUpgrade(RelicEffectAddTempUpgrade effect)
            {
                CardUpgradeData upgrade = DynamicUpgradeScenario.Upgrade("PojuRelicCardUnhealedMinusOne",
                    "7f9d616e-6ca2-4c8a-a470-0c4ca1000001", 0, 0, 0, -1, "armor", 0);
                var monsterMask = UnityEngine.ScriptableObject.CreateInstance<CardUpgradeMaskData>();
                monsterMask.name = "PojuRelicCardUnhealedMonsterMask";
                AccessTools.Field(typeof(CardUpgradeMaskData), "cardType").SetValue(monsterMask, CardType.Monster);
                AccessTools.Field(typeof(CardUpgradeData), "filters").SetValue(upgrade, new List<CardUpgradeMaskData> { monsterMask });
                AccessTools.Field(typeof(RelicEffectAddTempUpgrade), "_cardUpgradeData").SetValue(effect, upgrade);
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
                var ignoredArmor = new CardTraitData();
                ignoredArmor.Setup("CardTraitIgnoreArmor");
                var exhaust = new CardTraitData();
                exhaust.Setup("CardTraitExhaustState");
                AccessTools.Field(typeof(CardUpgradeData), "traitDataUpgrades").SetValue(clone,
                    new List<CardTraitData> { selfPurge, ignoredArmor, exhaust });
                AccessTools.Field(typeof(CardUpgradeData), "removeTraitUpgrades").SetValue(clone,
                    new List<string> { "CardTraitIgnoreArmor", "CardTraitExhaustState" });
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

            static void InstallEphemeralUpgrade(RelicEffectAddTempUpgrade effect)
            {
                CardUpgradeData original = UpgradeData(effect);
                var clone = (CardUpgradeData)AccessTools.Method(typeof(object), "MemberwiseClone").Invoke(original, null)!;
                var ephemeral = new CardTraitData();
                ephemeral.Setup("CardTraitEphemeral");
                AccessTools.Field(typeof(CardUpgradeData), "traitDataUpgrades").SetValue(clone, new List<CardTraitData> { ephemeral });
                var spellMask = UnityEngine.ScriptableObject.CreateInstance<CardUpgradeMaskData>();
                spellMask.name = "PojuRelicEphemeralSpellMask";
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

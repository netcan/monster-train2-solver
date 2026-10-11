using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalDeployableRelicScenario
    {
        internal const string Name = "original-deployable-relic";
        private const string RelicId = "fb6332bb-3e1c-44ba-951b-39fbc8d43601";
        private const string RelicKey = "NonBannerUnitsHaveDeployable";
        private const string UpgradeKey = "NonBannerUnitsHaveDeployable_Upgrade";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static bool CaptureReady { get; private set; }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Deployable relic must be acquired before native battle setup.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == RelicId);
            CheckOriginal(relic);
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == RelicId))
                throw new InvalidOperationException("Deployable relic is already present in the test run.");
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-DEPLOYABLE-RELIC-ACQUIRED unchanged NonBannerUnitsHaveDeployable before first battle draw.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            if (!Acquired) throw new InvalidOperationException("Deployable relic missed pre-battle acquisition.");
            SaveManager save = managers.GetSaveManager();
            CardManager cards = managers.GetCardManager()!;
            AllGameData data = save.GetAllGameData();
            CollectableRelicData relic = data.GetAllCollectableRelicData().Single(item => item.GetID() == RelicId);
            CheckOriginal(relic);
            CardUpgradeData upgrade = relic.GetEffects().Single().GetParamCardUpgradeData();
            CardUpgradeMaskData[] filters = upgrade.GetFilters().ToArray();
            CardState[] owned = cards.GetAllCards(new List<CardState>()).ToArray();
            CardState[] eligibleUnits = owned.Where(card => card.GetSpawnCharacterData() != null &&
                filters.All(filter => filter.FilterCard(card, managers.GetRelicManager()))).ToArray();
            CardState[] initialHand = cards.GetHand().ToArray();
            CardState[] naturallyDeployed = initialHand.Where(card => eligibleUnits.Contains(card) && cards.IsDeployableCard(card)).ToArray();
            if (eligibleUnits.Length == 0 || naturallyDeployed.Length == 0)
                throw new InvalidOperationException("Original deployable relic did not place an eligible unit in the native deployment-phase hand.");

            CaptureReady = true;
            RelicManager relicManager = managers.GetRelicManager();
            foreach (CardState card in owned)
            {
                relicManager.ApplyCardStateModifiers(card, resetTempCardModifiers: false);
                relicManager.ApplyCardStateModifiers(card, resetTempCardModifiers: false);
                relicManager.ApplyCardStateModifiers(card);
            }
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("ORIGINAL-DEPLOYABLE-RELIC-PREPARED native first hand contains " + naturallyDeployed.Length +
                " eligible non-Banner units; retained original relic payload and captured manager reset/filter/repeat behavior.");
        }

        internal static PlayCardAction? Choose(BattleTurnState source)
        {
            if (source.PlayRules == null || source.Spawn.Train.Context == null) return null;
            CombatContext context = source.Spawn.Train.Context;
            foreach (PlayCardAction play in BattleActionModel.EnumerateSupportedPlays(source))
            {
                CardInstanceState? card = context.FindCard(play.CardInstanceId);
                CardPlayRule? rule = source.PlayRules.Cards.FirstOrDefault(item => item.DataId == card?.DataId);
                if (card?.Temporary.Upgrades.Any(item => item.AssetKey == UpgradeKey) == true &&
                    rule?.Effect == "SpawnMonster") return play;
            }
            return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
        }

        private static void CheckOriginal(CollectableRelicData relic)
        {
            RelicEffectData effect = relic.GetEffects().Single();
            CardUpgradeData upgrade = effect.GetParamCardUpgradeData();
            if (relic.name != RelicKey || effect.GetEffectClassName() != "RelicEffectAddTempUpgrade" ||
                effect.GetParamSourceTeam() != Team.Type.Monsters || effect.GetEffectConditions().Count != 0 ||
                upgrade == null || upgrade.GetAssetKey() != UpgradeKey || upgrade.GetTraitDataUpgrades().Count != 1 ||
                upgrade.GetTraitDataUpgrades()[0].GetTraitStateName() != "CardTraitDrawInDeploymentPhase" ||
                upgrade.GetFilters().Count != 1 || upgrade.GetFilters()[0].name != RelicKey + "_CardMask" ||
                upgrade.GetRemoveTraitUpgrades().Count != 0 || upgrade.GetCardTriggerUpgrades().Count != 0 ||
                upgrade.GetCharacterTriggerUpgrades().Count != 0 || upgrade.GetRoomModifierUpgrades().Count != 0 ||
                upgrade.GetUnitAbilityUpgrade() != null || upgrade.GetRoomAbilityUpgrade() != null)
                throw new InvalidOperationException("Original deployable relic definition or payload changed.");
        }
    }
}

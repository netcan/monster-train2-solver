using System;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalMoonPhaseEnergyRelicScenario
    {
        internal const string Name = "original-moon-phase-energy-relic";
        internal const string AssetKey = "MoonPhaseModifyEmber";
        internal const string DataId = "70096a91-220a-4ab3-b8d0-b8fe8f3a819c";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static bool IsolatedTrackingEnabled => Prepared;

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Moon-phase energy relic must be acquired before battle setup.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == DataId);
            CheckOriginal(relic);
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == DataId))
                throw new InvalidOperationException("Moon-phase energy relic is already present.");
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-MOON-PHASE-ENERGY-RELIC-ACQUIRED unchanged MoonPhaseModifyEmber; +1 energy on Full; " +
                "native moon-phase mask, once-per-turn condition and starting deck retained.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            if (!Acquired || managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("Moon-phase energy relic missed the native deployment boundary.");
            if (managers.GetSaveManager().GetCollectedRelics().Count(item => item.GetRelicDataID() == DataId) != 1)
                throw new InvalidOperationException("Moon-phase energy relic is missing or duplicated at battle setup.");
            SaveManager save = managers.GetSaveManager();
            if (save.GetDeckHasMoonPhaseCardEffect())
                throw new InvalidOperationException("Moon-phase energy isolation requires a deck without other moon-phase card effects.");
            CheckOriginal(managers.GetSaveManager().GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == DataId));
            AccessTools.Field(typeof(PlayerManager), "isMoonPhaseTrackingActive").SetValue(managers.GetPlayerManager(), true);
            Prepared = true;
            log.LogInfo("ORIGINAL-MOON-PHASE-ENERGY-RELIC-PREPARED isolated native tracking enabled without adding moon-phase card effects; current phase " +
                managers.GetPlayerManager().CurrentMoonPhase + "; original deck retained.");
        }

        private static void CheckOriginal(CollectableRelicData relic)
        {
            RelicEffectData effect = relic.GetEffects().Single();
            RelicEffectCondition condition = effect.GetEffectConditions().Single();
            object Field(string name) => AccessTools.Field(typeof(RelicEffectCondition), name).GetValue(condition);
            if (relic.name != AssetKey || relic.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != "RelicEffectModifyEnergyOnMoonPhase" ||
                effect.GetParamInt() != 1 || effect.GetParamInt2() != 2 ||
                effect.GetParamSourceTeam() != Team.Type.None || effect.GetParamBool() || effect.GetParamBool2() ||
                Field("paramTrackedValue").ToString() != "MoonPhase" ||
                Field("paramEntryDuration").ToString() != "ThisTurn" ||
                !((bool)Field("paramTrackTriggerCount")) || Convert.ToInt32(Field("paramComparator")) != 6 ||
                (int)Field("paramInt") != 1 || (bool)Field("allowMultipleTriggersPerDuration"))
                throw new InvalidOperationException("Original MoonPhaseModifyEmber payload or once-per-turn condition changed.");
        }
    }
}

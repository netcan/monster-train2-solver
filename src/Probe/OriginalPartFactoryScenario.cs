using System;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalPartFactoryScenario
    {
        internal const string Name = "original-part-factory";
        internal const string DataId = "623ce968-aae5-4b07-b0c8-071f315031b7";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("PartFactory must be acquired before battle.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == DataId);
            RelicEffectData effect = relic.GetEffects().Single();
            if (relic.name != "PartFactory" || relic.DisallowInDeploymentPhase || effect.GetEffectClassName() != "RelicEffectAddCardsStartOfBattle" ||
                effect.GetParamBool() || effect.GetParamBool2() || !effect.GetParamBool3() || effect.GetParamCardUpgradeData() != null ||
                effect.GetParamCardPool() == null || effect.GetEffectConditions().Count != 0 ||
                save.GetCollectedRelics().Any(item => item.GetRelicDataID() == DataId))
                throw new InvalidOperationException("Original PartFactory definition or owner changed.");
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-PART-FACTORY-ACQUIRED unchanged start-of-battle owner; native SetupRun pool selection remains external input.");
        }
    }
}

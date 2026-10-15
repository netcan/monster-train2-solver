using System;
using System.Linq;
using BepInEx.Logging;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalStartCombatEnergyScenario
    {
        internal const string Name = "original-start-combat-energy-relic";
        internal const string DataId = "c5a68f6d-671d-4db3-999f-120b5febf41b";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Start-combat energy relic must be acquired before battle.");
            var relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == DataId);
            CheckOriginal(relic);
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == DataId))
                throw new InvalidOperationException("Start-combat energy relic already acquired.");
            save.AddRelic(relic); Acquired = true;
            log.LogInfo("ORIGINAL-START-COMBAT-ENERGY-ACQUIRED unchanged OneEmberFirstTurn_DH, actual native amount +2.");
        }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            if (!Acquired || managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("Start-combat energy relic missed deployment.");
            CheckOriginal(managers.GetSaveManager().GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == DataId));
            Prepared = true;
            log.LogInfo("ORIGINAL-START-COMBAT-ENERGY-PREPARED original deck; deployment control and natural first-combat callback.");
        }
        private static void CheckOriginal(CollectableRelicData relic)
        {
            var effect = relic.GetEffects().Single();
            if (relic.name != "OneEmberFirstTurn_DH" || !relic.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != "RelicEffectModifyEnergyStartOfCombat" || effect.GetParamInt() != 2 ||
                effect.GetParamInt2() != 0 || effect.GetParamFloat() != 0 || effect.GetParamBool() || effect.GetParamBool2() ||
                effect.GetParamBool3() || effect.GetParamBool4() || effect.GetParamCardFilter() != null ||
                effect.GetParamCharacterSubtype()?.IsNone != true || effect.GetParamCardType() != CardType.Spell ||
                effect.GetEffectConditions().Count != 0)
                throw new InvalidOperationException("Original OneEmberFirstTurn_DH definition changed.");
        }
    }
}

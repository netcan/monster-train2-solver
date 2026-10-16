using System;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalEarlyDiscardEnergyScenario
    {
        internal const string Name = "original-ember-on-early-discard";
        internal const string DataId = "44149a09-df55-4864-b5d9-f30c4e659999";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_ORIGINAL_EARLY_DISCARD_ENERGY") == "1";
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Early-discard energy relic must be acquired before battle setup.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == DataId);
            RelicEffectData effect = relic.GetEffects().Single();
            if (relic.name != "EmberOnEarlyDiscard" || effect.GetEffectClassName() != "RelicEffectEnergyOnCardEarlyDiscard" ||
                save.GetCollectedRelics().Any(item => item.GetRelicDataID() == DataId))
                throw new InvalidOperationException("Original EmberOnEarlyDiscard definition changed or is already acquired.");
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-EMBER-EARLY-DISCARD-ACQUIRED unchanged relic=" + relic.name + " energy=" + effect.GetParamInt() +
                " conditions=" + effect.GetEffectConditions().Count + " disallowedInPlacement=" + relic.DisallowInDeploymentPhase + ".");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (Prepared) return;
            if (!Acquired || managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("Early-discard relic fixture missed native deployment setup.");
            HandRemovalScenario.Prepare(managers, log, lethal: false, startAtZeroEnergy: true);
            Prepared = true;
            log.LogInfo("ORIGINAL-EMBER-EARLY-DISCARD-PREPARED card-triggered multi-card discard starts at zero energy for native per-card and cap evidence.");
        }
    }
}

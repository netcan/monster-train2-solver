#if DEVELOPMENT_PROBE
using System;
using System.Linq;
using BepInEx.Logging;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalHallowedHallsDraftRelicScenario
    {
        private const string SpellId = "c6484604-b077-43ce-84a4-0179d2f36352";
        private const string RelicId = "656861c9-a96d-4c99-988c-390b75e5d8d4";
        private static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_HALLOWED_HALLS_DRAFT_RELIC") == "1";
        private static bool acquired;

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (!Enabled || acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (typeof(RelicState).Module.ModuleVersionId != new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d") ||
                save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                save.GetCurrentDistance() != 0 || OriginalSupportSpellsScenario.IsolatedIds?.SequenceEqual(new[] { SpellId }) != true)
                throw new InvalidOperationException("Hallowed Halls draft relic requires the pinned original opening spell setup.");

            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == RelicId);
            RelicEffectData effect = relic.GetEffects().Single();
            if (relic.name != "AlliedClassRelic" || relic.DisallowInDeploymentPhase ||
                !save.IsDlcAvailableWhenStartingRun(relic.GetRequiredDLC()) ||
                effect.GetEffectClassName() != nameof(RelicEffectOverrideCardDraftClassType) ||
                effect.GetParamInt() != 1 || effect.GetParamInt2() != 2 || effect.GetEffectConditions().Count != 0 ||
                save.GetCollectedRelics().Any(item => item.GetRelicDataID() == RelicId))
                throw new InvalidOperationException("Original AlliedClassRelic definition or ownership changed.");

            save.AddRelic(relic);
            if (save.GetCollectedRelics().Count(item => item.GetRelicDataID() == RelicId) != 1)
                throw new InvalidOperationException("Original AlliedClassRelic was not added to the controlled run.");
            acquired = true;
            log.LogInfo("ORIGINAL-HALLOWED-HALLS-DRAFT-RELIC-ACQUIRED " + RelicId);
        }
    }
}
#endif

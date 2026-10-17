using System;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalFinalEndingArtifactScenario
    {
        internal const string Name = "original-final-ending-artifact";
        internal const string OwnerId = "6bb39014-9b03-4620-b088-f618a7e680b7";
        internal const string OwnerKey = "FinalEndingArtifact";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Prepared { get; private set; }
        internal static bool PolicyActionPlayed { get; private set; }
        internal static int GeneratedCardId { get; set; } = -1;
        internal static void MarkPolicyActionPlayed() => PolicyActionPlayed = true;
        internal static bool IsGeneratedCard(int instanceId) => instanceId == GeneratedCardId;

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CombatManager combat = managers.GetCombatManager()!;
            if (Prepared || save.PreviewMode || save.GetGameSequence() != SaveData.GameSequence.InBattle ||
                combat.GetTurnCount() != 0 || save.GetCollectedRelics().Any(item => item.GetRelicDataID() == OwnerId))
                throw new InvalidOperationException("FinalEndingArtifact requires an isolated deployment state.");

            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == OwnerId);
            RelicEffectData effect = relic.GetEffects().Single();
            RandomChampionPool pool = effect.GetParamRandomChampionPool() ?? throw new InvalidOperationException("FinalEndingArtifact champion pool is missing.");
            var champions = pool.GetAllChampions();
            if (relic.name != OwnerKey || effect.GetEffectClassName() != "RelicEffectAddChampionCardToHand" ||
                effect.GetEffectConditions().Count != 0 || champions.Count == 0 ||
                champions.Any(item => item.championCard?.GetSpawnCharacterData()?.IsChampion() != true || item.upgrades == null))
                throw new InvalidOperationException("The unchanged FinalEndingArtifact owner or champion pool changed.");

            save.AddRelic(relic);
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(combat, true);
            log.LogInfo($"ORIGINAL-FINAL-ENDING-ARTIFACT-PREPARED unchanged owner; {champions.Count} ordered champion entries and natural post-draw gates.");
        }
    }
}

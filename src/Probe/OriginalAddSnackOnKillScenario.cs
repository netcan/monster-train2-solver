using System;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalAddSnackOnKillScenario
    {
        internal const string Name = "original-add-snack-on-kill";
        internal const string OwnerId = "b6548209-3ccf-4deb-9896-f07d5df3dab9";
        private static readonly string[] PoolIds =
        {
            "0347419a-2e3a-492d-a054-f6e340be3db2", "0347419a-2e3a-492d-a054-f6e340be3db2",
            "0347419a-2e3a-492d-a054-f6e340be3db2", "0347419a-2e3a-492d-a054-f6e340be3db2",
            "0347419a-2e3a-492d-a054-f6e340be3db2", "e16040b1-eac0-4769-bc89-90bc2eb31944",
            "e16040b1-eac0-4769-bc89-90bc2eb31944", "e16040b1-eac0-4769-bc89-90bc2eb31944",
            "e16040b1-eac0-4769-bc89-90bc2eb31944", "e16040b1-eac0-4769-bc89-90bc2eb31944",
            "9c8d3e46-1758-4ade-a4b4-cd2bf649a4d6", "9c8d3e46-1758-4ade-a4b4-cd2bf649a4d6",
            "9c8d3e46-1758-4ade-a4b4-cd2bf649a4d6", "9c8d3e46-1758-4ade-a4b4-cd2bf649a4d6",
            "b24a8e4f-294f-4d24-9bc2-9c14474d83d1", "b24a8e4f-294f-4d24-9bc2-9c14474d83d1",
            "b24a8e4f-294f-4d24-9bc2-9c14474d83d1", "b24a8e4f-294f-4d24-9bc2-9c14474d83d1",
            "536e6de0-7ccf-412a-9a4e-ec55ab3988e5"
        };
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static bool PolicyActionPlayed { get; private set; }
        internal static void MarkPolicyActionPlayed() => PolicyActionPlayed = true;

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle ||
                save.GetCollectedRelics().Any(item => item.GetRelicDataID() == OwnerId))
                throw new InvalidOperationException("AddSnackOnKill must be acquired once before battle.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == OwnerId);
            RelicEffectData effect = relic.GetEffects().Single();
            CardPool pool = effect.GetParamCardPool() ?? throw new InvalidOperationException("AddSnackOnKill filtered pool is missing.");
            string[] actualPool = pool.GetFilteredChoices().Select(card => card.GetID()).ToArray();
            if (relic.name != "AddSnackOnKill" || relic.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != "RelicEffectAddBattleCardToHandOnUnitTrigger" ||
                effect.GetParamTrigger() != CharacterTriggerData.Trigger.OnDeath ||
                effect.GetParamSourceTeam() != Team.Type.Heroes || effect.GetParamInt() != 2 ||
                effect.GetParamCharacterSubtype() == null || !effect.GetParamCharacterSubtype().IsNone ||
                effect.GetParamCardUpgradeData() != null || !actualPool.SequenceEqual(PoolIds) ||
                effect.GetEffectConditions().Count != 1)
                throw new InvalidOperationException("The unchanged original AddSnackOnKill definition or filtered pool changed.");
            object Field(string name) => AccessTools.Field(typeof(RelicEffectCondition), name).GetValue(effect.GetEffectConditions()[0]);
            if (!(bool)Field("paramTrackTriggerCount") || Field("paramTrackedValue").ToString() != "SubtypeInDeck" ||
                Field("paramEntryDuration").ToString() != "ThisTurn" ||
                Convert.ToInt32(Field("paramComparator")) != 6 || (int)Field("paramInt") != 1 ||
                (bool)Field("allowMultipleTriggersPerDuration"))
                throw new InvalidOperationException("The original AddSnackOnKill same-turn trigger condition changed.");
            save.AddRelic(relic);
            Acquired = true;
            log.LogInfo("ORIGINAL-ADD-SNACK-ACQUIRED unchanged owner; 19 ordered filtered choices preserve pool weights; threshold-one ThisTurn condition.");
        }

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            CombatManager combat = managers.GetCombatManager()!;
            if (Prepared || !Acquired || save.PreviewMode || save.GetGameSequence() != SaveData.GameSequence.InBattle ||
                combat.GetTurnCount() != 0 || save.GetCollectedRelics().Count(item => item.GetRelicDataID() == OwnerId) != 1)
                throw new InvalidOperationException("AddSnackOnKill requires its isolated deployment state.");
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(combat, true);
            log.LogInfo("ORIGINAL-ADD-SNACK-ON-KILL-PREPARED unchanged relic; natural enemy-hero deaths, full callback model comparison.");
        }
    }
}

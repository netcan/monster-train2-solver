using System;
using System.Linq;
using BepInEx.Logging;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalRoomEntryStatusRelicScenario
    {
        internal const string Name = "original-room-entry-status-relics";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static readonly (string Key, string Id, string Status, int Stacks)[] Artifacts =
        {
            ("RootBeforePyre", "4778bb46-27e4-49ae-aa5d-750813ceb6d0", "rooted", 1),
            ("SpellWeaknessBelowPyre", "5a96d89f-d7c0-49fa-88d1-8ec75a8f3140", "spell weakness", 2),
            ("PyregelOnEnterTopFloor", "e4be072c-e389-4ae7-acc6-0c8aa4cf0078", "pyregel", 10)
        };

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Room-entry status relics must be acquired before battle setup.");

            CollectableRelicData[] relics = Artifacts.Select(artifact =>
            {
                CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData()
                    .Single(item => item.GetID() == artifact.Id);
                CheckOriginal(relic, artifact);
                if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == artifact.Id))
                    throw new InvalidOperationException("Room-entry status relic is already present: " + artifact.Key);
                return relic;
            }).ToArray();

            foreach (CollectableRelicData relic in relics) save.AddRelic(relic);
            Acquired = true;
            Prepared = true;
            StatusEffectManager statuses = managers.GetStatusEffectManager()!;
            string statusRules = string.Join(";", Artifacts.Select(artifact =>
            {
                StatusEffectData rule = statuses.GetStatusEffectDataById(artifact.Status)!;
                return artifact.Status + ":removeAfterCard=" + rule.GetRemoveWhenTriggeredAfterCardPlayed() +
                    ",removeAtEndIfTriggered=" + rule.GetRemoveAtEndOfTurnIfTriggered();
            }));
            log.LogInfo("ORIGINAL-ROOM-ENTRY-STATUS-RELICS-ACQUIRED unchanged RootBeforePyre, SpellWeaknessBelowPyre, " +
                "PyregelOnEnterTopFloor; target Heroes entering room 2; original effects and conditions retained; status rules=" + statusRules + ".");
        }

        private static void CheckOriginal(CollectableRelicData relic, (string Key, string Id, string Status, int Stacks) artifact)
        {
            RelicEffectData effect = relic.GetEffects().Single();
            StatusEffectStackData[] statuses = effect.GetParamStatusEffects();
            if (relic.name != artifact.Key || relic.GetID() != artifact.Id ||
                effect.GetEffectClassName() != "RelicEffectAddStatusEffectOnEnterRoomIndex" ||
                effect.GetParamInt() != 2 || effect.GetParamSourceTeam() != Team.Type.Heroes ||
                effect.GetParamBool() || effect.GetParamBool2() || effect.GetEffectConditions().Count != 0 ||
                statuses.Length != 1 || statuses[0].statusId != artifact.Status || statuses[0].count != artifact.Stacks)
                throw new InvalidOperationException("Original room-entry status relic changed: " + artifact.Key +
                    "; id=" + relic.GetID() + ", effect=" + effect.GetEffectClassName() + ", room=" + effect.GetParamInt() +
                    ", team=" + effect.GetParamSourceTeam() + ", flags=" + effect.GetParamBool() + "/" + effect.GetParamBool2() +
                    ", statuses=" + string.Join(",", statuses.Select(status => status.statusId + ":" + status.count)) + ".");
        }
    }
}

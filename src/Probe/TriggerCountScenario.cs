using System;
using System.Linq;
using BepInEx.Logging;

namespace MonsterTrain2Poju.Probe
{
    internal static class TriggerCountScenario
    {
        internal static bool Prepared { get; private set; }
        internal static bool Combined { get; private set; }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            Combined = Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "incant-relic-combined";
            IncantScenario.Prepare(managers, log, spawnAndDeath: Combined);
            SaveManager save = managers.GetSaveManager();
            Acquire("410ba540-7c4f-4dc5-a84f-b1d8af508891", "ExtraSpellCastTrigger", CharacterTriggerData.Trigger.CardSpellPlayed);
            if (Combined)
            {
                Acquire("9e0deb69-6196-44a6-8220-85bd0df25f77", "ExtraSpawnTrigger", CharacterTriggerData.Trigger.OnSpawn);
                Acquire("a5d67620-a9ec-4257-91b5-305336e11987", "ExtraDeathTrigger", CharacterTriggerData.Trigger.OnDeath);
            }
            Prepared = true;
            log.LogInfo("TRIGGER-COUNT-PREPARED original trigger-count relics acquired through SaveManager; no relic definition changes.");

            void Acquire(string id, string key, CharacterTriggerData.Trigger kind)
            {
                CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData()
                    .Single(data => data.GetID() == id);
                if (relic.name != key || relic.GetEffects().Count != 1 ||
                    relic.GetEffects()[0].GetParamTrigger() != kind ||
                    relic.GetEffects()[0].GetParamInt() != 1 || relic.GetEffects()[0].GetParamBool() ||
                    relic.GetEffects()[0].GetParamBool2() || relic.GetEffects()[0].GetEffectConditions().Count != 0)
                    throw new InvalidOperationException("Original " + key + " definition changed.");
                save.AddRelic(relic);
            }
        }
    }
}

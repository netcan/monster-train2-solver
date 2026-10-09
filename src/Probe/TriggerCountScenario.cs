using System;
using System.Linq;
using BepInEx.Logging;

namespace MonsterTrain2Poju.Probe
{
    internal static class TriggerCountScenario
    {
        internal static bool Prepared { get; private set; }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            IncantScenario.Prepare(managers, log);
            SaveManager save = managers.GetSaveManager();
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(data => data.GetID() == "410ba540-7c4f-4dc5-a84f-b1d8af508891");
            if (relic.name != "ExtraSpellCastTrigger" || relic.GetEffects().Count != 1 ||
                relic.GetEffects()[0].GetParamTrigger() != CharacterTriggerData.Trigger.CardSpellPlayed ||
                relic.GetEffects()[0].GetParamInt() != 1 || relic.GetEffects()[0].GetParamBool() ||
                relic.GetEffects()[0].GetParamBool2() || relic.GetEffects()[0].GetEffectConditions().Count != 0)
                throw new InvalidOperationException("Original Incant relic definition changed.");
            save.AddRelic(relic);
            Prepared = true;
            log.LogInfo("TRIGGER-COUNT-PREPARED original ExtraSpellCastTrigger acquired through SaveManager; no relic definition changes.");
        }
    }
}

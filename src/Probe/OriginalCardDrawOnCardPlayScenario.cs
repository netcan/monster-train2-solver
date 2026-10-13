using System;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalCardDrawOnCardPlayScenario
    {
        internal const string Name = "original-card-draw-on-card-play";
        internal const string RelicId = "f3f07b9b-1349-41d0-abab-bd5ec04d81e4";
        internal const string RelicKey = "CardDrawOnCardPlay";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }

        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            if (Acquired) return;
            SaveManager save = managers.GetSaveManager();
            if (save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Card-draw-on-play relic must be acquired before battle setup.");
            CollectableRelicData relic = save.GetAllGameData().GetAllCollectableRelicData()
                .Single(item => item.GetID() == RelicId);
            CheckOriginal(relic);
            if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == RelicId))
                throw new InvalidOperationException("Card-draw-on-play relic is already present in the test run.");
            save.AddRelic(relic);
            Acquired = true;
            RelicEffectData effect = relic.GetEffects().Single();
            log.LogInfo("ORIGINAL-CARD-DRAW-ON-CARD-PLAY-ACQUIRED unchanged relic=" + relic.name + " id=" + relic.GetID() +
                " count=" + effect.GetParamInt() + " cardType=" + effect.GetParamCardType() +
                " conditions=" + effect.GetEffectConditions().Count + ".");
        }

        internal static void CheckOriginal(CollectableRelicData relic)
        {
            RelicEffectData effect = relic.GetEffects().Single();
            RelicEffectCondition condition = effect.GetEffectConditions().Single();
            object Field(string name) => AccessTools.Field(typeof(RelicEffectCondition), name).GetValue(condition);
            if (relic.GetID() != RelicId || effect.GetEffectClassName() != "RelicEffectCardDrawOnCardPlay" ||
                effect.GetParamInt() != 1 || effect.GetParamCardType() != CardType.Invalid ||
                Field("paramTrackedValue").ToString() != "AnyCardPlayed" ||
                Field("paramCardType").ToString() != "Any" || (bool)Field("paramTrackTriggerCount") ||
                Field("paramEntryDuration").ToString() != "ThisTurn" || Convert.ToInt32(Field("paramComparator")) != 6 ||
                (int)Field("paramInt") != 3 || (bool)Field("allowMultipleTriggersPerDuration"))
                throw new InvalidOperationException("Original CardDrawOnCardPlay definition changed.");
        }
    }
}

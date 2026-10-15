using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class CampaignPurgeGoldCalibration
    {
        internal const string Id = "d8d1e069-938e-4141-b6ac-e20f4578163a";
        private static List<string>? notifications;
        [HarmonyPatch(typeof(RelicManager), "NotifyRelicTriggered")]
        private static class NotificationPatch
        {
            private static void Postfix(RelicState triggeredRelic) { if (notifications != null) notifications.Add(triggeredRelic.GetRelicDataID()); }
        }
        internal static void Capture(AllGameManagers managers)
        {
            if (Environment.GetEnvironmentVariable("MT2_PROBE_CAMPAIGN_PURGE_GOLD") != "1") return;
            var save = managers.GetSaveManager(); var data = save.GetAllGameData(); var relics = managers.GetRelicManager();
            var owner = data.GetAllCollectableRelicData().Single(item => item.GetID() == Id);
            if (save.PreviewMode || save.GetAllRelics().Any(item => item.GetEffects().Any(effect => effect is IOnPurgeRelicEffect))) throw new InvalidOperationException("Purge calibration needs isolated owners.");
            var cards = data.GetAllCardData().Where(card => card.GetCardType() != CardType.Invalid).GroupBy(card => card.GetCardType()).OrderBy(group => (int)group.Key)
                .Select(group => { var card = new CardState(); card.Setup(group.First(), save, setupStartingUpgrades: false); return card; }).ToArray();
            object Rng() => Enum.GetValues(typeof(RngId)).Cast<RngId>().Where(id => id != RngId.NonDeterministic && id != RngId.Chatter)
                .Select(id => new { Id = id.ToString(), State = RngCalibration.Words(RandomManager.GetState(id)) }).ToArray();
            JObject Snapshot() => JObject.FromObject(new { Owners = save.GetCollectedRelics().Select(item => item.GetRelicDataID()).ToArray(), Deck = save.GetDeckState().Select(card => card.GetCardDataID()).ToArray(),
                Gold = save.GetGold(), Health = save.GetTowerHP(), Maximum = save.GetMaxTowerHP(), Frame = UnityEngine.Time.frameCount, Rng = Rng() });
            var before = Snapshot(); int originalGold = save.GetGold(); var rows = new List<object>();
            void Remove() => save.RemoveRelic(save.GetCollectedRelics().Single(item => item.GetRelicDataID() == Id));
            void Samples(string phase)
            {
                foreach (var card in cards)
                foreach (bool combat in new[] { false, true })
                foreach (int gold in new[] { 0, 123 })
                foreach (int repetition in new[] { 0, 1 })
                {
                    save.SetGold(gold, isTotalReset: true); var rngBefore = Rng(); notifications = new List<string>(); int completed = 0;
                    try
                    {
                        if (combat) Drain(relics.ApplyOnPurgeRelicEffectsInCombat(card, managers.GetCoreManagers(), () => completed++));
                        else Drain(relics.ApplyOnPurgeRelicEffectsNotInCombat(card, relics, () => completed++));
                        rows.Add(new { Phase = phase, CardId = card.GetCardDataID(), CardType = card.GetCardType().ToString(), InCombat = combat, Repetition = repetition,
                            Owned = save.GetCollectedRelics().Any(item => item.GetRelicDataID() == Id), Placement = managers.GetCombatManager().IsPlacementPhase,
                            CanApplyInPreview = save.GetCollectedRelics().FirstOrDefault(item => item.GetRelicDataID() == Id)?.GetEffects().Single().CanApplyInPreviewMode ?? false,
                            BeforeGold = gold, Gold = save.GetGold(), Completed = completed, Notifications = notifications.ToArray(),
                            Multiplier = ((MutableRules)AccessTools.Field(typeof(SaveManager), "mutableRules").GetValue(save)).GetRule(MutableRules.Condition.GoldRewardMultiplier, 1f), RngBefore = rngBefore, RngAfter = Rng() });
                    }
                    finally { notifications = null; }
                }
            }
            try { Samples("baseline"); save.AddRelic(owner); Samples("owned"); Remove(); Samples("removed"); }
            finally { if (save.GetCollectedRelics().Any(item => item.GetRelicDataID() == Id)) Remove(); save.SetGold(originalGold, isTotalReset: true); }
            var after = Snapshot(); if (!JToken.DeepEquals(before, after)) throw new InvalidOperationException("Purge calibration changed recorded state/frame/RNG.");
            var definition = owner.GetEffects().Single();
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(RelicState).Assembly.ManifestModule.ModuleVersionId, Before = before, After = after,
                Owner = new { Id = owner.GetID(), Key = owner.name, DisallowedInPlacement = owner.DisallowInDeploymentPhase,
                    Type = definition.GetEffectClassName(), Amount = definition.GetParamInt(), CardType = definition.GetParamCardType().ToString(), Conditions = definition.GetEffectConditions().Count }, Rows = rows }))
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "campaign-purge-gold-calibration.mt2f"))) document.Write(stream);
        }
        private static void Drain(IEnumerator routine)
        { try { while (routine.MoveNext()) { if (routine.Current is IEnumerator nested) Drain(nested); else if (routine.Current != null) throw new InvalidOperationException("Purge callback requires asynchronous native consumer."); } } finally { (routine as IDisposable)?.Dispose(); } }
    }
}

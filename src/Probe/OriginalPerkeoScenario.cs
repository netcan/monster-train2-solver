using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalPerkeoScenario
    {
        internal const string Name = "original-perkeo";
        internal const string DataId = "94f7f915-2a2d-4b56-afe6-76ce875ba54d";
        internal const string UpgradeId = "95278115-d14a-45f4-aec7-6cb437f441c1";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Name;
        internal static bool Acquired { get; private set; }
        internal static bool Prepared { get; private set; }
        internal static readonly List<ConversionRecord> Conversions = new();
        internal static readonly List<UnplayedRecord> Unplayed = new();
        internal sealed class UnplayedRecord
        {
            public int[] UnupgradedControls { get; set; } = Array.Empty<int>();
            public CombatContext Before { get; set; } = null!;
            public CombatContext? Actual { get; set; }
            public CombatContext? Predicted { get; set; }
            public bool Completed { get; set; }
            public string? Difference { get; set; }
            public string? UnsupportedReason { get; set; }
            public string? Error { get; set; }
        }
        internal sealed class ConversionRecord
        {
            public int CardId { get; set; }
            public int Count { get; set; }
            public string Destination { get; set; } = "";
            public CardCreationRule Creation { get; set; } = null!;
            public CombatContext Before { get; set; } = null!;
            public CombatContext? Actual { get; set; }
            public CombatContext? Predicted { get; set; }
            public string? Difference { get; set; }
            public string? UnsupportedReason { get; set; }
            public bool Completed { get; set; }
            public string? Error { get; set; }
        }
        internal static void AcquireBeforeBattle(AllGameManagers managers, ManualLogSource log)
        {
            SaveManager save = managers.GetSaveManager();
            if (Acquired || save.PreviewMode || save.GetGameSequence() == SaveData.GameSequence.InBattle)
                throw new InvalidOperationException("Perkeo requires pre-battle acquisition.");
            var relic = save.GetAllGameData().GetAllCollectableRelicData().Single(item => item.GetID() == DataId);
            var effect = relic.GetEffects().Single();
            if (relic.name != "BalatroEventPerkeo" || relic.DisallowInDeploymentPhase ||
                effect.GetEffectClassName() != "RelicEffectAddCardsStartOfBattle" || effect.GetParamBool() ||
                !effect.GetParamBool2() || effect.GetParamBool3() || effect.GetParamCardUpgradeData()?.GetID() != UpgradeId ||
                effect.GetEffectConditions().Count != 0 || effect.GetParamCardPool() == null)
                throw new InvalidOperationException("Original Perkeo definition changed.");
            log.LogInfo("ORIGINAL-PERKEO-POOL " + string.Join(",", effect.GetParamCardPool().GetFilteredChoices().Select(card => card.name + ":" + card.GetID())));
            log.LogInfo("ORIGINAL-PERKEO-UPGRADE " + string.Join(",", effect.GetParamCardUpgradeData().GetCardTriggerUpgrades()
                .SelectMany(trigger => trigger.GetCardEffects().Select(card => trigger.GetTrigger() + ":" + card.GetEffectStateName() + ":" + card.GetParamInt() + ":" + card.GetParamCardData()?.name))));
            save.AddCardToDeck(save.GetAllGameData().FindCardData(RelicPerkeoModel.SourceCardId),
                applyExistingRelicModifiers: false, showAnimation: false, notifyDeckCount: false);
            save.AddRelic(relic); Acquired = true;
        }
        internal static void PrepareDeployment(AllGameManagers managers)
        {
            if (!Acquired || Prepared || managers.GetCombatManager()!.GetTurnCount() != 0)
                throw new InvalidOperationException("Expected Perkeo deployment.");
            Prepared = true;
        }
        [HarmonyPatch(typeof(CardManager), nameof(CardManager.TriggerOnUnplayedCardsInHand))]
        private static class UnplayedPatch
        {
            private static void Postfix(bool preview, ref IEnumerator __result)
            {
                if (Enabled && Acquired && !preview && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    __result = ObserveUnplayed(__result);
            }
        }
        private static IEnumerator ObserveUnplayed(IEnumerator native)
        {
            var row = new UnplayedRecord(); Unplayed.Add(row);
            try
            {
                row.Before = FullBattleTrace.Active!.CaptureContext();
                row.UnupgradedControls = row.Before.Cards.Hand.Where(card => card.DataId == RelicPerkeoModel.SourceCardId &&
                    !RelicPerkeoModel.HasConversion(row.Before.FindCard(card.InstanceId))).Select(card => card.InstanceId).ToArray();
                CardGenerationResult result = RelicPerkeoModel.ApplyUnplayed(row.Before);
                row.Predicted = result.Context; row.UnsupportedReason = result.UnsupportedReason;
            }
            catch (Exception error) { row.Error = error.ToString(); }
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally
            {
                (native as IDisposable)?.Dispose();
                try
                {
                    row.Actual = FullBattleTrace.Active!.CaptureContext();
                    if (row.Predicted != null && !JToken.DeepEquals(JToken.FromObject(row.Predicted), JToken.FromObject(row.Actual)))
                        row.Difference = "Native Perkeo unplayed phase/control context differs.";
                }
                catch (Exception error) { row.Error = error.ToString(); }
            }
        }
        [HarmonyPatch(typeof(CardEffectBalatroConvertCard), nameof(CardEffectBalatroConvertCard.ApplyEffect))]
        private static class ConvertPatch
        {
            private static void Postfix(CardEffectState cardEffectState, ref IEnumerator __result)
            {
                if (Enabled && Acquired && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    __result = Observe(__result, cardEffectState);
            }
        }
        private static IEnumerator Observe(IEnumerator native, CardEffectState effect)
        {
            var row = new ConversionRecord(); Conversions.Add(row);
            try
            {
                var source = effect.GetParentCardState(); row.CardId = FullBattleTrace.Active!.CardId(source);
                row.Count = effect.GetParamInt(); row.Destination = AllGameManagers.Instance!.GetCardManager()!.GetCardPileLocation(source).ToString();
                row.Creation = CardGenerationProbe.Creation(effect.GetParamCardData()); row.Before = FullBattleTrace.Active.CaptureContext();
                CardGenerationResult result = RelicPerkeoModel.Convert(row.Before, row.CardId, row.Creation);
                row.Predicted = result.Context; row.UnsupportedReason = result.UnsupportedReason;
            }
            catch (Exception error) { row.Error = error.ToString(); }
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally
            {
                (native as IDisposable)?.Dispose();
                try
                {
                    row.Actual = FullBattleTrace.Active!.CaptureContext();
                    if (row.Predicted != null && !JToken.DeepEquals(JToken.FromObject(row.Predicted), JToken.FromObject(row.Actual)))
                        row.Difference = "Native Perkeo conversion context differs.";
                }
                catch (Exception error) { row.Error = error.ToString(); }
            }
        }
    }
}

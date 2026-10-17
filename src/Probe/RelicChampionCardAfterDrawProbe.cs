using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicChampionCardAfterDrawProbe
    {
        internal static readonly List<Record> Records = new List<Record>();
        private static readonly Dictionary<RelicEffectAddChampionCardToHand, Record> active =
            new Dictionary<RelicEffectAddChampionCardToHand, Record>();

        internal sealed class Record
        {
            public string OwnerId { get; set; } = "";
            public int EffectIndex { get; set; }
            public int Turn { get; set; }
            public bool TestResult { get; set; }
            public int SelectedPoolIndex { get; set; } = -1;
            public string? SelectedDataId { get; set; }
            public string[] SelectedUpgradeIds { get; set; } = Array.Empty<string>();
            public string[] DeckChampionDataIds { get; set; } = Array.Empty<string>();
            public UnityRng TestRng { get; set; }
            public CombatContext Before { get; set; } = null!;
            public CombatContext? Predicted { get; set; }
            public CombatContext? Actual { get; set; }
            public string? UnsupportedReason { get; set; }
            public string? Difference { get; set; }
            public string? CaptureError { get; set; }
            public bool Completed { get; set; }
        }

        [HarmonyPatch(typeof(RelicEffectAddChampionCardToHand), nameof(RelicEffectAddChampionCardToHand.TestEffectTurnPhaseTiming))]
        private static class TestPatch
        {
            private static void Prefix(RelicEffectAddChampionCardToHand __instance)
            {
                if (!OriginalFinalEndingArtifactScenario.Prepared || FullBattleTrace.Active == null ||
                    AllGameManagers.Instance!.GetSaveManager().PreviewMode) return;
                try
                {
                    RelicState owner = __instance.SourceRelicState;
                    var record = new Record
                    {
                        OwnerId = owner.GetRelicDataID(),
                        EffectIndex = owner.GetEffects().IndexOf(__instance),
                        Turn = AllGameManagers.Instance!.GetCombatManager()!.GetTurnCount(),
                        Before = FullBattleTrace.Active.CaptureContext(),
                        DeckChampionDataIds = AllGameManagers.Instance!.GetSaveManager().GetDeckState()
                            .Where(card => card.IsChampionCard()).Select(card => card.GetCardDataID()).ToArray()
                    };
                    if (active.ContainsKey(__instance)) throw new InvalidOperationException("Overlapping FinalEndingArtifact timing tests.");
                    active.Add(__instance, record);
                    Records.Add(record);
                }
                catch (Exception error) { FullBattleTrace.Active.CaptureFailure(error); }
            }

            private static void Postfix(RelicEffectAddChampionCardToHand __instance, bool __result)
            {
                if (!active.TryGetValue(__instance, out Record? record)) return;
                try
                {
                    record.TestResult = __result;
                    RandomChampionPool.GrantedChampionInfo? selected = AccessTools.Field(
                        typeof(RelicEffectAddChampionCardToHand), "championToAdd").GetValue(__instance) as RandomChampionPool.GrantedChampionInfo;
                    record.SelectedDataId = selected?.championCard.GetID();
                    record.SelectedUpgradeIds = selected?.upgrades.Select(upgrade => upgrade.GetID()).ToArray() ?? Array.Empty<string>();
                    if (selected != null)
                    {
                        List<RandomChampionPool.GrantedChampionInfo> pool =
                            __instance.SourceRelicEffectData.GetParamRandomChampionPool()!.GetAllChampions();
                        record.SelectedPoolIndex = pool.FindIndex(item => ReferenceEquals(item, selected));
                    }
                    record.TestRng = FullBattleTrace.Active!.CaptureContext().BattleRng;
                    if (!__result)
                    {
                        record.Predicted = record.Before;
                        record.Actual = FullBattleTrace.Active!.CaptureContext();
                        record.Difference = JToken.DeepEquals(JToken.FromObject(record.Before), JToken.FromObject(record.Actual))
                            ? null : "Gated native context changed.";
                        record.Completed = true;
                        active.Remove(__instance);
                    }
                }
                catch (Exception error)
                {
                    record.CaptureError = error.ToString();
                    FullBattleTrace.Active!.CaptureFailure(error);
                    active.Remove(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(RelicEffectAddChampionCardToHand), nameof(RelicEffectAddChampionCardToHand.ApplyEffectTurnPhaseTiming))]
        private static class ApplyPatch
        {
            private static void Postfix(RelicEffectAddChampionCardToHand __instance, ref IEnumerator __result)
            {
                if (active.TryGetValue(__instance, out Record? record)) __result = Wrap(__result, __instance, record);
            }
        }

        private static IEnumerator Wrap(IEnumerator native, RelicEffectAddChampionCardToHand effect, Record record)
        {
            try
            {
                CardGenerationResult predicted = RelicCardGenerationModel.ApplyChampionCardAfterDrawOne(
                    record.Before, record.OwnerId, record.EffectIndex);
                record.Predicted = predicted.Context;
                record.UnsupportedReason = predicted.UnsupportedReason;
                if (predicted.AddedCards.Count == 1) OriginalFinalEndingArtifactScenario.GeneratedCardId = predicted.AddedCards[0].InstanceId;
            }
            catch (Exception error)
            {
                record.CaptureError = error.ToString();
                FullBattleTrace.Active!.CaptureFailure(error);
            }
            try
            {
                while (native.MoveNext()) yield return native.Current;
                record.Completed = true;
            }
            finally
            {
                (native as IDisposable)?.Dispose();
                active.Remove(effect);
                try
                {
                    record.Actual = FullBattleTrace.Active!.CaptureContext();
                    if (record.Predicted != null && record.Actual != null)
                    {
                        record.Difference = JToken.DeepEquals(JToken.FromObject(record.Predicted), JToken.FromObject(record.Actual))
                            ? null : "Native generated champion context differs from model.";
                    }
                }
                catch (Exception error)
                {
                    record.CaptureError = error.ToString();
                    FullBattleTrace.Active!.CaptureFailure(error);
                }
            }
        }
    }
}

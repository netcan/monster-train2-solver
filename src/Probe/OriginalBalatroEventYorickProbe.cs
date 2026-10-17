using System;
using System.Collections;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalBalatroEventYorickProbe
    {
        internal sealed class CallbackRecord
        {
            public int CardInstanceId { get; set; }
            public string CardDataId { get; set; } = "";
            public int EffectIndex { get; set; }
            public int Amount { get; set; }
            public TrainCombatState? Before { get; set; }
            public TrainCombatState? Actual { get; set; }
            public bool Completed { get; set; }
            public string? Difference { get; set; }
            public string? UnsupportedReason { get; set; }
            public string? CaptureError { get; set; }
        }

        private sealed class Observation
        {
            internal CallbackRecord Record { get; }
            internal TrainCombatState? Predicted { get; }
            internal Observation(CallbackRecord record, TrainCombatState? predicted)
            { Record = record; Predicted = predicted; }
        }

        internal static readonly System.Collections.Generic.List<CallbackRecord> Records =
            new System.Collections.Generic.List<CallbackRecord>();

        private static Observation Begin(RelicEffectAddGoldOnExhaust effect, CardPlayedRelicEffectParams parameters)
        {
            var record = new CallbackRecord();
            TrainCombatState? predicted = null;
            try
            {
                CardState card = parameters.cardState ?? throw new InvalidOperationException("Gold-on-exhaust callback has no card.");
                FullBattleTrace trace = FullBattleTrace.Active ?? throw new InvalidOperationException("Gold-on-exhaust callback has no battle trace.");
                record.CardInstanceId = trace.CardId(card);
                record.CardDataId = card.GetCardDataID();
                record.Amount = (int)AccessTools.Field(typeof(RelicEffectAddGoldOnExhaust), "goldAmount")!.GetValue(effect);
                record.Before = trace.CaptureTrain();
                CombatContext context = record.Before.Context ?? throw new InvalidOperationException("Gold-on-exhaust capture has no context.");
                int relicIndex = -1;
                for (int index = 0; index < (context.Relics?.Count ?? 0); index++)
                    if (context.Relics![index].DataId == OriginalBalatroEventYorickScenario.OwnerId) relicIndex = index;
                CombatRelicState owner = relicIndex < 0 ? throw new InvalidOperationException("Gold-on-exhaust owner was not captured.") : context.Relics![relicIndex];
                record.EffectIndex = owner.EffectTypes.Select((type, index) => (type, index))
                    .Single(item => item.type == RelicCardPlayGoldOnExhaustModel.Effect).index;
                TrainCombatResult result = RelicCardPlayGoldOnExhaustModel.ApplyOne(record.Before, record.CardInstanceId,
                    relicIndex, record.EffectIndex, AllGameManagers.Instance!.GetCombatManager()!.IsPlacementPhase,
                    AllGameManagers.Instance.GetSaveManager().PreviewMode);
                predicted = result.State;
                record.UnsupportedReason = result.UnsupportedReason;
            }
            catch (Exception error)
            {
                record.CaptureError = error.ToString();
                FullBattleTrace.Active?.CaptureFailure(error);
            }
            Records.Add(record);
            return new Observation(record, predicted);
        }

        private static IEnumerator Wrap(IEnumerator native, Observation observation)
        {
            while (native.MoveNext()) yield return native.Current;
            try
            {
                CallbackRecord record = observation.Record;
                record.Actual = FullBattleTrace.Active!.CaptureTrain();
                if (observation.Predicted != null && !JToken.DeepEquals(
                    JToken.Parse(JsonConvert.SerializeObject(observation.Predicted)),
                    JToken.Parse(JsonConvert.SerializeObject(record.Actual))))
                    record.Difference = "Predicted gold-on-exhaust train state differs from native callback.";
                record.Completed = true;
            }
            catch (Exception error)
            {
                observation.Record.CaptureError = error.ToString();
                FullBattleTrace.Active?.CaptureFailure(error);
            }
        }

        [HarmonyPatch(typeof(RelicEffectAddGoldOnExhaust), nameof(RelicEffectAddGoldOnExhaust.TestEffectOnCardPlayed))]
        private static class TestPatch
        {
            private static void Postfix(CardPlayedRelicEffectParams relicEffectParams, bool __result)
            {
                if (!OriginalBalatroEventYorickScenario.Enabled || !OriginalBalatroEventYorickScenario.Prepared) return;
                OriginalBalatroEventYorickScenario.RecordTest(relicEffectParams?.cardState, __result);
            }
        }

        [HarmonyPatch(typeof(RelicEffectAddGoldOnExhaust), nameof(RelicEffectAddGoldOnExhaust.ApplyEffectOnCardPlayed))]
        private static class ApplyPatch
        {
            private static void Postfix(RelicEffectAddGoldOnExhaust __instance,
                CardPlayedRelicEffectParams relicEffectParams, ref IEnumerator __result)
            {
                if (!OriginalBalatroEventYorickScenario.Enabled || !OriginalBalatroEventYorickScenario.Prepared) return;
                try { __result = Wrap(__result, Begin(__instance, relicEffectParams)); }
                catch (Exception error) { FullBattleTrace.Active?.CaptureFailure(error); }
            }
        }
    }
}

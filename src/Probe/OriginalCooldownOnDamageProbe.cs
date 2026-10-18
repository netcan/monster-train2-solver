using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalCooldownOnDamageProbe
    {
        internal sealed class TestRecord
        {
            public int CardInstanceId { get; set; }
            public string CardDataId { get; set; } = "";
            public bool Result { get; set; }
        }

        internal sealed class CallbackRecord
        {
            public int CardInstanceId { get; set; }
            public int TargetUnitId { get; set; }
            public int CooldownBefore { get; set; }
            public int CooldownAfter { get; set; }
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

        internal static readonly List<TestRecord> Tests = new List<TestRecord>();
        internal static readonly List<CallbackRecord> Applications = new List<CallbackRecord>();

        private static Observation Begin(RelicEffectAddStatusEffectOnDamage effect, CardPlayedRelicEffectParams parameters)
        {
            var record = new CallbackRecord();
            TrainCombatState? predicted = null;
            try
            {
                CardState card = parameters.cardState ?? throw new System.InvalidOperationException("CooldownOnDamage callback has no card.");
                FullBattleTrace trace = FullBattleTrace.Active ?? throw new System.InvalidOperationException("CooldownOnDamage callback has no battle trace.");
                record.CardInstanceId = trace.CardId(card);
                record.TargetUnitId = OriginalCooldownOnDamageScenario.CarrierUnitId;
                TrainCombatState before = trace.CaptureTrain();
                CombatUnit target = before.Rooms.SelectMany(room => room.Units)
                    .Single(unit => unit.Id == record.TargetUnitId);
                record.CooldownBefore = target.Status("cooldown")?.Stacks ?? 0;
                CombatContext context = before.Context ?? throw new System.InvalidOperationException("CooldownOnDamage capture has no context.");
                IReadOnlyList<CombatRelicState> relics = context.Relics ??
                    throw new System.InvalidOperationException("CooldownOnDamage capture has no relic definitions.");
                int relicIndex = relics.Select((owner, index) => (owner, index))
                    .Where(item => item.owner.DataId == OriginalCooldownOnDamageScenario.OwnerId)
                    .Select(item => item.index).Single();
                int effectIndex = relics[relicIndex].EffectTypes
                    .Select((type, index) => (type, index)).Single(item => item.type == RelicCardDamageStatusModel.Effect).index;
                TrainCombatResult result = RelicCardDamageStatusModel.ApplyOne(before,
                    parameters.roomIndex, new[] { record.TargetUnitId }, relicIndex, effectIndex);
                predicted = result.State;
                record.UnsupportedReason = result.UnsupportedReason;
                if (predicted != null)
                    record.CooldownAfter = predicted.Rooms.SelectMany(room => room.Units)
                        .Single(unit => unit.Id == record.TargetUnitId).Status("cooldown")?.Stacks ?? 0;
            }
            catch (System.Exception error)
            {
                record.CaptureError = error.ToString();
                FullBattleTrace.Active?.CaptureFailure(error);
            }
            Applications.Add(record);
            return new Observation(record, predicted);
        }

        private static IEnumerator Wrap(IEnumerator native, Observation observation)
        {
            while (native.MoveNext()) yield return native.Current;
            try
            {
                CallbackRecord record = observation.Record;
                record.Actual = FullBattleTrace.Active!.CaptureTrain();
                int actualCooldown = record.Actual.Rooms.SelectMany(room => room.Units)
                    .Single(unit => unit.Id == record.TargetUnitId).Status("cooldown")?.Stacks ?? 0;
                if (observation.Predicted != null && !JToken.DeepEquals(
                    JToken.Parse(JsonConvert.SerializeObject(observation.Predicted)),
                    JToken.Parse(JsonConvert.SerializeObject(record.Actual))))
                    record.Difference = "Predicted CooldownOnDamage train state differs from native callback.";
                if (actualCooldown != record.CooldownAfter)
                    record.Difference = "Native CooldownOnDamage target cooldown differs from the prediction.";
                record.Completed = true;
            }
            catch (System.Exception error)
            {
                observation.Record.CaptureError = error.ToString();
                FullBattleTrace.Active?.CaptureFailure(error);
            }
        }

        [HarmonyPatch(typeof(RelicEffectAddStatusEffectOnDamage), nameof(RelicEffectAddStatusEffectOnDamage.TestEffectOnCardPlayed))]
        private static class TestPatch
        {
            private static void Postfix(CardPlayedRelicEffectParams relicEffectParams, bool __result)
            {
                if (!OriginalCooldownOnDamageScenario.Enabled || !OriginalCooldownOnDamageScenario.Prepared ||
                    relicEffectParams?.cardState == null) return;
                FullBattleTrace trace = FullBattleTrace.Active!;
                int cardId = trace.CardId(relicEffectParams.cardState);
                if (!OriginalCooldownOnDamageScenario.CardIds.Contains(cardId)) return;
                Tests.Add(new TestRecord { CardInstanceId = cardId,
                    CardDataId = relicEffectParams.cardState.GetCardDataID(), Result = __result });
            }
        }

        [HarmonyPatch(typeof(RelicEffectAddStatusEffectOnDamage), nameof(RelicEffectAddStatusEffectOnDamage.ApplyEffectOnCardPlayed))]
        private static class ApplyPatch
        {
            private static void Postfix(RelicEffectAddStatusEffectOnDamage __instance,
                CardPlayedRelicEffectParams relicEffectParams, ref IEnumerator __result)
            {
                if (!OriginalCooldownOnDamageScenario.Enabled || !OriginalCooldownOnDamageScenario.Prepared) return;
                try { __result = Wrap(__result, Begin(__instance, relicEffectParams)); }
                catch (System.Exception error) { FullBattleTrace.Active?.CaptureFailure(error); }
            }
        }
    }
}

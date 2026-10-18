using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicConsumeDamageProbe
    {
        internal sealed class TestRecord
        {
            public int CardId { get; set; }
            public int RoomIndex { get; set; }
            public CardPlayRule CardRule { get; set; } = null!;
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Actual { get; set; }
            public TrainCombatState? Predicted { get; set; }
            public int[] Targets { get; set; } = Array.Empty<int>();
            public int[] CachedTargets { get; set; } = Array.Empty<int>();
            public bool Result { get; set; }
            public string? Difference { get; set; }
            public string? UnsupportedReason { get; set; }
            public string? Error { get; set; }
        }

        internal sealed class ApplicationRecord
        {
            public int CardId { get; set; }
            public int[] Targets { get; set; } = Array.Empty<int>();
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Actual { get; set; }
            public TrainCombatState? Predicted { get; set; }
            public string? Difference { get; set; }
            public string? UnsupportedReason { get; set; }
            public string? Error { get; set; }
            public bool Completed { get; set; }
        }

        internal static readonly List<TestRecord> Tests = new List<TestRecord>();
        internal static readonly List<ApplicationRecord> Applications = new List<ApplicationRecord>();
        private static bool Active => OriginalConsumeDamageScenario.Prepared && FullBattleTrace.Active != null &&
            !AllGameManagers.Instance!.GetSaveManager().PreviewMode;
        private static int[] Targets(RelicEffectDamageOnExhaust effect) =>
            ((List<CharacterState>)AccessTools.Field(typeof(RelicEffectDamageOnExhaust), "targets").GetValue(effect))
                .Select(FullBattleTrace.Active!.UnitId).ToArray();

        [HarmonyPatch(typeof(RelicEffectDamageOnExhaust), nameof(RelicEffectDamageOnExhaust.TestEffectOnCardPlayed))]
        private static class TestPatch
        {
            private static void Prefix(CardPlayedRelicEffectParams relicEffectParams, out TestRecord? __state)
            {
                __state = null; if (!Active) return;
                var row = new TestRecord(); __state = row; Tests.Add(row);
                try
                {
                    FullBattleTrace trace = FullBattleTrace.Active!;
                    CardState card = relicEffectParams.cardState;
                    row.CardId = trace.CardId(card); row.RoomIndex = relicEffectParams.roomIndex;
                    row.CardRule = new CardPlayRule(card.GetCardDataID(), card.GetAssetName(), 0, "Null", "Discard", null,
                        Array.Empty<string>(), cardType: card.GetCardType().ToString(), isAnyAbility: card.IsAnyAbility());
                    row.Before = trace.CaptureTrain();
                    int owner = row.Before.Context!.Relics!.Select((relic, index) => (relic, index))
                        .Single(item => item.relic.DataId == RelicConsumeDamageModel.DataId).index;
                    RelicCardPlayDamageSelection selected = RelicConsumeDamageModel.Select(row.Before, row.CardId,
                        row.RoomIndex, owner, 0);
                    row.Predicted = selected.State; row.Targets = selected.Targets.ToArray();
                    row.UnsupportedReason = selected.UnsupportedReason;
                }
                catch (Exception error) { row.Error = error.ToString(); FullBattleTrace.Active!.CaptureFailure(error); }
            }

            private static void Postfix(RelicEffectDamageOnExhaust __instance, bool __result, TestRecord? __state)
            {
                if (__state == null) return;
                try
                {
                    __state.Result = __result; __state.CachedTargets = Targets(__instance);
                    __state.Actual = FullBattleTrace.Active!.CaptureTrain();
                    if (__state.Predicted != null && (!JToken.DeepEquals(JToken.FromObject(__state.Predicted), JToken.FromObject(__state.Actual)) ||
                        __result != (__state.Targets.Length > 0) || __result && !__state.Targets.SequenceEqual(__state.CachedTargets)))
                        __state.Difference = "Native Consume damage Test target/Battle RNG differs from model.";
                }
                catch (Exception error) { __state.Error = error.ToString(); FullBattleTrace.Active!.CaptureFailure(error); }
            }
        }

        [HarmonyPatch(typeof(RelicEffectDamageOnExhaust), nameof(RelicEffectDamageOnExhaust.ApplyEffectOnCardPlayed))]
        private static class ApplyPatch
        {
            private static void Postfix(RelicEffectDamageOnExhaust __instance, CardPlayedRelicEffectParams relicEffectParams, ref IEnumerator __result)
            { if (Active) __result = Observe(__result, __instance, relicEffectParams); }
        }

        private static IEnumerator Observe(IEnumerator native, RelicEffectDamageOnExhaust effect, CardPlayedRelicEffectParams parameters)
        {
            var row = new ApplicationRecord(); Applications.Add(row);
            try
            {
                row.CardId = FullBattleTrace.Active!.CardId(parameters.cardState); row.Targets = Targets(effect);
                row.Before = FullBattleTrace.Active.CaptureTrain();
                TrainCombatResult predicted = RelicConsumeDamageModel.ApplySelected(row.Before, row.Targets, deferPhysicalRemoval: true);
                row.Predicted = predicted.State; row.UnsupportedReason = predicted.UnsupportedReason;
            }
            catch (Exception error) { row.Error = error.ToString(); FullBattleTrace.Active!.CaptureFailure(error); }
            try
            {
                while (native.MoveNext()) yield return native.Current;
                row.Completed = true;
            }
            finally
            {
                (native as IDisposable)?.Dispose();
                try
                {
                    row.Actual = FullBattleTrace.Active!.CaptureTrain();
                    if (row.Predicted != null && !JToken.DeepEquals(JToken.FromObject(row.Predicted), JToken.FromObject(row.Actual)))
                        row.Difference = "Native relic damage/death queue differs from model.";
                }
                catch (Exception error) { row.Error = error.ToString(); FullBattleTrace.Active!.CaptureFailure(error); }
            }
        }
    }
}

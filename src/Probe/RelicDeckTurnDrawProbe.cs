using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicDeckTurnDrawProbe
    {
        internal sealed class TestRecord
        {
            public string Phase { get; set; } = "";
            public int Turn { get; set; }
            public bool Placement { get; set; }
            public int NativeCount { get; set; }
            public bool Result { get; set; }
            public CombatContext Before { get; set; } = null!;
            public CombatContext? After { get; set; }
        }

        internal sealed class ApplicationRecord
        {
            public int Turn { get; set; }
            public int NativeCount { get; set; }
            public bool Placement { get; set; }
            public CombatContext Before { get; set; } = null!;
            public CombatContext? Actual { get; set; }
            public string? Difference { get; set; }
            public string? UnsupportedReason { get; set; }
            public string? CaptureError { get; set; }
            public bool Completed { get; set; }
        }

        internal static readonly List<TestRecord> Tests = new List<TestRecord>();
        internal static readonly List<ApplicationRecord> Applications = new List<ApplicationRecord>();
        private static bool Active => OriginalDeckTurnDrawScenario.Acquired && FullBattleTrace.Active != null &&
            !AllGameManagers.Instance!.GetSaveManager().PreviewMode;
        private static int NativeCount(RelicEffectCardDrawPerPileStartOfTurn effect) =>
            (int)AccessTools.Method(typeof(RelicEffectCardDrawPerPileStartOfTurn), "GetCardDrawCount")!
                .Invoke(effect, new object[] { AllGameManagers.Instance!.GetCoreManagers() });

        [HarmonyPatch(typeof(RelicEffectCardDrawPerPileStartOfTurn), nameof(RelicEffectCardDrawPerPileStartOfTurn.TestEffectTurnPhaseTiming))]
        private static class TestPatch
        {
            private static void Prefix(RelicEffectCardDrawPerPileStartOfTurn __instance, out TestRecord? __state)
            {
                __state = null;
                if (!Active) return;
                try
                {
                    __state = new TestRecord { Phase = OriginalDeckTurnDrawScenario.Phase,
                        Turn = AllGameManagers.Instance!.GetCombatManager()!.GetTurnCount(),
                        Placement = AllGameManagers.Instance.GetCombatManager()!.IsPlacementPhase,
                        NativeCount = NativeCount(__instance), Before = FullBattleTrace.Active!.CaptureContext() };
                    Tests.Add(__state);
                }
                catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
            }

            private static void Postfix(bool __result, TestRecord? __state)
            {
                if (__state == null) return;
                try { __state.Result = __result; __state.After = FullBattleTrace.Active!.CaptureContext(); }
                catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
            }
        }

        [HarmonyPatch(typeof(RelicEffectCardDrawPerPileStartOfTurn), nameof(RelicEffectCardDrawPerPileStartOfTurn.ApplyEffectTurnPhaseTiming))]
        private static class ApplyPatch
        {
            private static void Postfix(RelicEffectCardDrawPerPileStartOfTurn __instance, ref IEnumerator __result)
            { if (Active) __result = Observe(__result, __instance); }
        }

        private static IEnumerator Observe(IEnumerator native, RelicEffectCardDrawPerPileStartOfTurn effect)
        {
            ApplicationRecord record = new ApplicationRecord();
            CombatContext? predicted = null;
            Applications.Add(record);
            try
            {
                record.Turn = AllGameManagers.Instance!.GetCombatManager()!.GetTurnCount();
                record.Placement = AllGameManagers.Instance.GetCombatManager()!.IsPlacementPhase;
                record.NativeCount = NativeCount(effect);
                record.Before = FullBattleTrace.Active!.CaptureContext();
                RelicResourceResult model = RelicDeckTurnDrawModel.Apply(record.Before, placement: record.Placement);
                predicted = model.Context;
                record.UnsupportedReason = model.UnsupportedReason;
            }
            catch (Exception error) { record.CaptureError = error.ToString(); FullBattleTrace.Active!.CaptureFailure(error); }
            try
            {
                while (native.MoveNext()) yield return native.Current;
                record.Completed = true;
            }
            finally
            {
                (native as IDisposable)?.Dispose();
                try
                {
                    record.Actual = FullBattleTrace.Active!.CaptureContext();
                    if (predicted != null && !JToken.DeepEquals(JToken.FromObject(predicted), JToken.FromObject(record.Actual)))
                        record.Difference = "Deck-scaled after-draw prediction differs from native context.";
                }
                catch (Exception error) { record.CaptureError = error.ToString(); FullBattleTrace.Active!.CaptureFailure(error); }
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicPrecachedPlayerClanGenerationProbe
    {
        internal static readonly List<Record> Records = new List<Record>();
        private static readonly Dictionary<RelicEffectAddPrecachedBattleCardToHand, Record> active =
            new Dictionary<RelicEffectAddPrecachedBattleCardToHand, Record>();

        internal sealed class Record
        {
            public string OwnerId { get; set; } = "";
            public int EffectIndex { get; set; }
            public int Turn { get; set; }
            public bool TestResult { get; set; }
            public UnityRng TestRng { get; set; }
            public string? Selected { get; set; }
            public UnityRng SelectionRng { get; set; }
            public CombatContext Before { get; set; } = null!;
            public CombatContext? Actual { get; set; }
            public bool Completed { get; set; }
        }

        [HarmonyPatch(typeof(RelicEffectAddPrecachedBattleCardToHand), nameof(RelicEffectAddPrecachedBattleCardToHand.TestEffectTurnPhaseTiming))]
        private static class TestPatch
        {
            private static void Prefix(RelicEffectAddPrecachedBattleCardToHand __instance)
            {
                if (!OriginalGarfieldBoxRandomCardScenario.Prepared || FullBattleTrace.Active == null ||
                    AllGameManagers.Instance!.GetSaveManager().PreviewMode) return;
                try
                {
                    var owner = __instance.SourceRelicState;
                    var record = new Record
                    {
                        OwnerId = owner.GetRelicDataID(),
                        EffectIndex = owner.GetEffects().IndexOf(__instance),
                        Turn = AllGameManagers.Instance!.GetCombatManager()!.GetTurnCount(),
                        Before = FullBattleTrace.Active.CaptureContext()
                    };
                    if (active.ContainsKey(__instance)) throw new InvalidOperationException("Overlapping GarfieldBox timing tests.");
                    active.Add(__instance, record);
                    Records.Add(record);
                }
                catch (Exception error) { FullBattleTrace.Active.CaptureFailure(error); }
            }

            private static void Postfix(RelicEffectAddPrecachedBattleCardToHand __instance, bool __result)
            {
                if (!active.TryGetValue(__instance, out Record? record)) return;
                record.TestResult = __result;
                record.TestRng = FullBattleTrace.Active!.CaptureContext().BattleRng;
            }
        }

        [HarmonyPatch]
        private static class SelectionPatch
        {
            private static System.Reflection.MethodBase TargetMethod() =>
                AccessTools.Method(typeof(RelicEffectAddPrecachedBattleCardToHand), "GetRandomCardFromPlayerClans");

            private static void Postfix(RelicEffectAddPrecachedBattleCardToHand __instance, CardData? __result)
            {
                if (!active.TryGetValue(__instance, out Record? record)) return;
                record.Selected = __result?.GetID();
                record.SelectionRng = FullBattleTrace.Active!.CaptureContext().BattleRng;
            }
        }

        [HarmonyPatch(typeof(RelicEffectAddPrecachedBattleCardToHand), nameof(RelicEffectAddPrecachedBattleCardToHand.ApplyEffectTurnPhaseTiming))]
        private static class ApplyPatch
        {
            private static void Postfix(RelicEffectAddPrecachedBattleCardToHand __instance, ref IEnumerator __result)
            {
                if (active.TryGetValue(__instance, out Record? record)) __result = Wrap(__result, __instance, record);
            }
        }

        private static IEnumerator Wrap(IEnumerator native, RelicEffectAddPrecachedBattleCardToHand effect, Record record)
        {
            try
            {
                while (native.MoveNext()) yield return native.Current;
                record.Completed = true;
            }
            finally
            {
                (native as IDisposable)?.Dispose();
                active.Remove(effect);
                try { record.Actual = FullBattleTrace.Active!.CaptureContext(); }
                catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
            }
        }
    }
}

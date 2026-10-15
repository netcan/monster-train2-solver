using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicAfterDrawGenerationProbe
    {
        internal static readonly List<Record> Records = new List<Record>();
        private static readonly Dictionary<RelicEffectAddBattleCardToHand, Record> active = new Dictionary<RelicEffectAddBattleCardToHand, Record>();
        internal sealed class Record
        {
            public string OwnerId { get; set; } = "";
            public int EffectIndex { get; set; }
            public int Turn { get; set; }
            public string? Selected { get; set; }
            public bool TestResult { get; set; }
            public UnityRng TestRng { get; set; }
            public CombatContext Before { get; set; } = null!;
            public CombatContext? Actual { get; set; }
            public bool Completed { get; set; }
        }
        [HarmonyPatch(typeof(RelicEffectAddBattleCardToHand), nameof(RelicEffectAddBattleCardToHand.TestEffectTurnPhaseTiming))]
        private static class TestPatch
        {
            private static void Prefix(RelicEffectAddBattleCardToHand __instance)
            {
                if (!OriginalAfterDrawGenerationScenario.Prepared || FullBattleTrace.Active == null || AllGameManagers.Instance!.GetSaveManager().PreviewMode) return;
                try
                {
                    var owner = __instance.SourceRelicState;
                    var record = new Record { OwnerId = owner.GetRelicDataID(), EffectIndex = owner.GetEffects().ToList().IndexOf(__instance),
                        Turn = AllGameManagers.Instance!.GetCombatManager().GetTurnCount(), Before = FullBattleTrace.Active.CaptureContext() };
                    if (active.ContainsKey(__instance)) throw new InvalidOperationException("Overlapping native generation tests.");
                    active.Add(__instance, record); Records.Add(record);
                }
                catch (Exception error) { FullBattleTrace.Active.CaptureFailure(error); }
            }
            private static void Postfix(RelicEffectAddBattleCardToHand __instance, bool __result)
            {
                if (!active.TryGetValue(__instance, out var record)) return;
                record.TestResult = __result;
                record.Selected = (AccessTools.Field(typeof(RelicEffectAddBattleCardToHand), "cardToAdd").GetValue(__instance) as CardData)?.GetID();
                record.TestRng = FullBattleTrace.Active!.CaptureContext().BattleRng;
            }
        }
        [HarmonyPatch(typeof(RelicEffectAddBattleCardToHand), nameof(RelicEffectAddBattleCardToHand.ApplyEffectTurnPhaseTiming))]
        private static class ApplyPatch
        {
            private static void Postfix(RelicEffectAddBattleCardToHand __instance, ref IEnumerator __result)
            {
                if (active.TryGetValue(__instance, out var record)) __result = Wrap(__result, __instance, record);
            }
        }
        private static IEnumerator Wrap(IEnumerator native, RelicEffectAddBattleCardToHand effect, Record record)
        {
            try { while (native.MoveNext()) yield return native.Current; record.Completed = true; }
            finally
            {
                (native as IDisposable)?.Dispose(); active.Remove(effect);
                try { record.Actual = FullBattleTrace.Active!.CaptureContext(); }
                catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
            }
        }
    }
}

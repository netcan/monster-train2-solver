using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicTurnDrawProbe
    {
        internal sealed class Record
        {
            public string Kind { get; set; } = "";
            public string Phase { get; set; } = "";
            public bool Placement { get; set; }
            public int Health { get; set; }
            public bool Returned { get; set; }
            public bool Completed { get; set; }
            public CombatContext Before { get; set; } = null!;
            public CombatContext? Actual { get; set; }
        }
        internal static readonly List<Record> Records = new List<Record>();
        private static bool Active => OriginalTurnDrawScenario.Acquired && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode;
        private static Record Begin(string kind)
        {
            var managers = AllGameManagers.Instance!;
            var record = new Record { Kind = kind, Phase = OriginalTurnDrawScenario.Phase, Placement = managers.GetCombatManager().IsPlacementPhase,
                Health = managers.GetSaveManager().GetTowerHP(), Before = FullBattleTrace.Active!.CaptureContext() };
            Records.Add(record); return record;
        }
        [HarmonyPatch]
        private static class HealthPatch
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                yield return AccessTools.Method(typeof(RelicEffectAdditionalDrawOnTowerFullHealth), "TestEffectStartOfBattle");
                yield return AccessTools.Method(typeof(RelicEffectAdditionalDrawOnTowerFullHealth), "TestEffectTurnPhaseTiming");
            }
            private static void Prefix(MethodBase __originalMethod, out Record? __state)
            {
                __state = null; if (!Active) return;
                try { __state = Begin(__originalMethod.Name); } catch (Exception e) { FullBattleTrace.Active!.CaptureFailure(e); }
            }
            private static void Postfix(bool __result, Record? __state)
            {
                if (__state == null) return;
                try { __state.Returned = __result; __state.Completed = true; __state.Actual = FullBattleTrace.Active!.CaptureContext(); }
                catch (Exception e) { FullBattleTrace.Active!.CaptureFailure(e); }
            }
        }
        [HarmonyPatch(typeof(RelicEffectDrawToMaxStartOfCombat), "ApplyEffectOnPostOpeningDraw")]
        private static class OpeningPatch
        {
            private static void Postfix(ref IEnumerator __result) { if (Active) __result = Wrap(__result); }
        }
        private static IEnumerator Wrap(IEnumerator native)
        {
            Record? record = null;
            try
            {
                try { record = Begin("PostOpening"); } catch (Exception e) { FullBattleTrace.Active!.CaptureFailure(e); }
                while (native.MoveNext()) yield return native.Current;
                if (record != null) record.Completed = true;
            }
            finally
            {
                (native as IDisposable)?.Dispose();
                if (record != null) try { record.Actual = FullBattleTrace.Active!.CaptureContext(); } catch (Exception e) { FullBattleTrace.Active!.CaptureFailure(e); }
            }
        }
    }
}

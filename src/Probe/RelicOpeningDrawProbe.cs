using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicOpeningDrawProbe
    {
        internal static readonly List<Record> Records = new List<Record>();
        internal sealed class Record
        {
            public string OwnerId { get; set; } = "";
            public string Phase { get; set; } = "";
            public bool Completed { get; set; }
            public CombatContext Before { get; set; } = null!;
            public CombatContext? Actual { get; set; }
        }
        [HarmonyPatch]
        private static class ApplyPatch
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                yield return AccessTools.Method(typeof(RelicEffectDrawOnPreOpeningDraw), nameof(RelicEffectDrawOnPreOpeningDraw.ApplyEffectOnPreOpeningDraw));
                yield return AccessTools.Method(typeof(RelicEffectDrawOnPreOpeningDrawCascade), nameof(RelicEffectDrawOnPreOpeningDrawCascade.ApplyEffectOnPreOpeningDraw));
            }
            private static void Postfix(RelicEffectDrawOnPreOpeningDraw __instance, ref IEnumerator __result)
            {
                if (OriginalOpeningDrawScenario.Acquired && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    __result = Wrap(__result, __instance.SourceRelicState.GetRelicDataID());
            }
        }
        private static IEnumerator Wrap(IEnumerator native, string ownerId)
        {
            Record? record = null;
            try
            {
                try { record = new Record { OwnerId = ownerId, Phase = OriginalOpeningDrawScenario.Phase, Before = FullBattleTrace.Active!.CaptureContext() }; Records.Add(record); }
                catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
                while (native.MoveNext()) yield return native.Current;
                if (record != null) record.Completed = true;
            }
            finally
            {
                (native as IDisposable)?.Dispose();
                if (record != null) try { record.Actual = FullBattleTrace.Active!.CaptureContext(); }
                    catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
            }
        }
    }
}

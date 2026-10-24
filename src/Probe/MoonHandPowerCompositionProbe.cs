using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    // Observe the original effect without injecting callbacks or changing its order.
    internal static class MoonHandPowerCompositionProbe
    {
        internal sealed class Sample
        {
            public CombatContext Before { get; set; } = null!;
            public CombatContext? After { get; set; }
            public bool Completed { get; set; }
        }
        internal static readonly List<Sample> Rows = new List<Sample>();
        [HarmonyPatch(typeof(RelicEffectApplyCardUpgradeToHandEndOfTurnOnMoon), nameof(RelicEffectApplyCardUpgradeToHandEndOfTurnOnMoon.ApplyEffect))]
        private static class EffectPatch
        {
            private static void Postfix(ref IEnumerator __result)
            {
                if ((RoomMoonCallbackCalibration.SpellHandPowerRelic ||
                    OriginalMoonCardUpgradesScenario.Prepared && OriginalMoonCardUpgradesScenario.HandPowerRelic) &&
                    !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    __result = Observe(__result);
            }
        }
        private static IEnumerator Observe(IEnumerator native)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            var row = new Sample { Before = trace.CaptureContext() }; Rows.Add(row);
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally { (native as IDisposable)?.Dispose(); row.After = trace.CaptureContext(); }
        }
    }
}

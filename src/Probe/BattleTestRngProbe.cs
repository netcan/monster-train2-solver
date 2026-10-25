#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    // Passive component diagnostics: preserve the exact caller and both RNG ends.
    internal static class BattleTestRngProbe
    {
        internal sealed class Record
        {
            public int Min, Max, Result, Frame;
            public bool Preview;
            public string Stack = "";
            public UnityRng Before, After;
        }
        internal static readonly List<Record> Records = new List<Record>();
        private static UnityRng State()
        {
            uint[] words = RngCalibration.Words(RandomManager.GetState(RngId.BattleTest));
            return new UnityRng(words[0], words[1], words[2], words[3]);
        }
        [HarmonyPatch(typeof(RandomManager), nameof(RandomManager.Range), new[] { typeof(int), typeof(int), typeof(RngId) })]
        private static class RangePatch
        {
            private static void Prefix(int min, int max, RngId rngId, out Record? __state)
            {
                __state = null;
                if (!UnsupportedBranchObservation.Enabled || rngId != RngId.BattleTest) return;
                __state = new Record { Min = min, Max = max, Before = State(), Frame = UnityEngine.Time.frameCount,
                    Preview = AllGameManagers.Instance?.GetSaveManager()?.PreviewMode == true,
                    Stack = new StackTrace(1, false).ToString() };
                Records.Add(__state);
            }
            private static void Postfix(int __result, Record? __state)
            {
                if (__state == null) return;
                __state.Result = __result; __state.After = State();
            }
        }
    }
}
#endif

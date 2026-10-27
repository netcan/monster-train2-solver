#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class StandbyOrderingProbe
    {
        internal static readonly List<object> Records = new List<object>();
        private sealed class Record
        {
            public int Character { get; set; }
            public int OnlyCard { get; set; }
            public CombatContext Before { get; set; } = null!;
            public CombatContext? After { get; set; }
            public int[] ProcessingBefore { get; set; } = null!;
            public int[]? ProcessingAfter { get; set; }
        }
        private static int[] Processing(CardManager manager) =>
            ((IEnumerable<CardState>)AccessTools.Field(typeof(CardManager), "standbyCardsBeingProcessed").GetValue(manager))
                .Select(FullBattleTrace.Active!.CardId).ToArray();
        private static IEnumerator Observe(CardManager manager, CharacterState character, CardState onlyCard, IEnumerator native)
        {
            var trace = FullBattleTrace.Active!;
            var record = new Record { Character = character == null ? 0 : trace.UnitId(character),
                OnlyCard = onlyCard == null ? 0 : trace.CardId(onlyCard), Before = trace.CaptureContext(), ProcessingBefore = Processing(manager) };
            Records.Add(record);
            try { while (native.MoveNext()) yield return native.Current; }
            finally
            {
                (native as IDisposable)?.Dispose();
                record.After = trace.CaptureContext(); record.ProcessingAfter = Processing(manager);
            }
        }
        [HarmonyPatch(typeof(CardManager), "CheckStandByConditions")]
        private static class CheckPatch
        {
            private static void Postfix(CardManager __instance, CharacterState character, CardState onlyCardToCheck, ref IEnumerator __result)
            {
                if (UnsupportedBranchObservation.ObserveStandbyOrdering)
                    __result = Observe(__instance, character, onlyCardToCheck, __result);
            }
        }
    }
}
#endif

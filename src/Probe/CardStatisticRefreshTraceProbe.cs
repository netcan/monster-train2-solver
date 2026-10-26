#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    // Opt-in, read-only diagnostic for lazy zero-valued native statistic entries.
    [HarmonyPatch(typeof(CardStatistics), "UpdateDeckStats")]
    internal static class CardStatisticRefreshTraceProbe
    {
        private static readonly bool Enabled = Environment.GetEnvironmentVariable("MT2_PROBE_TRACE_CARD_STATS") == "1";
        private static void Prefix(CardStatistics __instance)
        {
            if (!Enabled || !UnsupportedBranchObservation.Enabled) return;
            var stored = (Dictionary<CardState, CardStatsEntry>)AccessTools.Field(typeof(CardStatistics), "deckStats").GetValue(__instance);
            var owned = AllGameManagers.Instance?.GetCardManager()?.GetAllCards(new List<CardState>());
            var missing = owned?.Where(card => !stored.ContainsKey(card)).ToArray();
            var current = owned?.Count > 0 ? owned : AllGameManagers.Instance?.GetSaveManager()?.GetDeckState();
            var removed = current == null ? Array.Empty<CardState>() : stored.Keys.Except(current).ToArray();
            if (missing == null || missing.Length == 0 && removed.Length == 0) return;
            string caller = string.Join(">", new StackTrace(1, false).GetFrames().Take(12)
                .Select(frame => frame.GetMethod()?.DeclaringType?.FullName + "." + frame.GetMethod()?.Name));
            UnityEngine.Debug.Log("CARD-STAT-REFRESH missing=" + string.Join(",", missing.Select(card => card.GetCardDataID())) +
                " removed=" + string.Join(",", removed.Select(card => card.GetCardDataID())) + " caller=" + caller);
        }
    }
}
#endif

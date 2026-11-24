#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
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
        private static readonly List<object> regionRanges = new List<object>();
        internal static bool RegionPreviewEnabled =>
            Environment.GetEnvironmentVariable("MT2_PROBE_REGION_RUN_INPUT") == "1" &&
            Environment.GetEnvironmentVariable("MT2_PROBE_REGION_PREVIEW_SCHEDULING") == "1";

        internal static void WriteRegionPreview()
        {
            if (!RegionPreviewEnabled) return;
            using var archive = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "RegionPreviewScheduling", GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CardState).Module.ModuleVersionId,
                Scheduling = PreviewSchedulingProbe.Records, Ranges = regionRanges,
                HivemindFeeds = OriginalHivemindProbe.RegionFeedCallbacks
            });
            using var file = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                "region-preview-scheduling.mt2f"));
            archive.Write(file);
        }

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
                bool preview = AllGameManagers.Instance?.GetSaveManager()?.PreviewMode == true;
                bool unsupportedBranch = UnsupportedBranchObservation.Enabled &&
                    (rngId == RngId.BattleTest || preview && rngId == RngId.Battle);
                bool region = RegionPreviewEnabled && rngId == RngId.BattleTest && FullBattleTrace.Active != null;
                if (!unsupportedBranch && !region) return;
                __state = new Record { Min = min, Max = max, Before = State(), Frame = UnityEngine.Time.frameCount,
                    Preview = preview,
                    Stack = new StackTrace(1, false).ToString() };
                if (unsupportedBranch) Records.Add(__state);
                if (region)
                    regionRanges.Add(new { Event = __state, Turn = AllGameManagers.Instance!.GetCombatManager()!.GetTurnCount(),
                        ActionCount = FullBattleTrace.Active!.ObservedActionCount });
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

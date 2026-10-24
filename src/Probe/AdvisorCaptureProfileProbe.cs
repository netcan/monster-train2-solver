#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    // Optional diagnostic hooks into the separately loaded player assembly.
    // This type and its patches are absent from the player build.
    internal static class AdvisorCaptureProfileProbe
    {
        [ThreadStatic] private static Sample? current;
        private static ManualLogSource log = null!;
        private static FieldInfo history = null!;
        private static readonly Dictionary<MethodBase, string> names = new Dictionary<MethodBase, string>();
        internal static void Install(Assembly assembly, ManualLogSource logger)
        {
            if (Environment.GetEnvironmentVariable("MT2_PROBE_CAPTURE_PROFILE") != "1") return;
            log = logger;
            const string ns = "MonsterTrain2Poju.Probe.";
            history = AccessTools.Field(assembly.GetType(ns + "CardBranchMaskProbe", true), "Records");
            var harmony = new Harmony("netcan.monstertrain2.capture-profile");
            foreach (string target in new[] {
                "BattleTurnProbe.CaptureState", "EnemySpawningProbe.CaptureState", "TrainCombatProbe.Capture",
                "FullBattleTrace.Capture", "FullBattleTrace.CaptureUnit", "FullBattleTrace.CaptureContextCore",
                "UnitPlayModelProbe.Capture", "CardModifierProbe.Capture", "BattleActionProbe.CaptureRules",
                "BattleStatisticsProbe.Capture", "RelicProbe.Capture", "TriggeredSummonProbe.Catalog",
                "EnchantmentBattleScenario.CaptureWorld", "BattleSpawnPointProbe.Capture", "AbilityCardProbe.Capture",
                "BonusDrawProbe.Capture", "CardBranchMaskProbe.Observe", "CardBranchMaskProbe.Capture",
                "CardOwnedMaskProbe.Capture", "CardUpgradeMaskCalibration.Card" })
            {
                int dot = target.LastIndexOf('.');
                Type type = assembly.GetType(ns + target.Substring(0, dot), true)!;
                MethodInfo[] methods = AccessTools.GetDeclaredMethods(type).Where(method => method.Name == target.Substring(dot + 1)).ToArray();
                if (methods.Length == 0) throw new InvalidOperationException("Missing profiling target " + target);
                foreach (MethodInfo method in methods)
                {
                    names.Add(method, target);
                    harmony.Patch(method, prefix: new HarmonyMethod(typeof(AdvisorCaptureProfileProbe), nameof(Begin)),
                        finalizer: new HarmonyMethod(typeof(AdvisorCaptureProfileProbe), nameof(End)));
                }
            }
            harmony.Patch(AccessTools.Method(assembly.GetType(ns + "BattleAdvisorOverlay", true), "CaptureReadOnly"),
                prefix: new HarmonyMethod(typeof(AdvisorCaptureProfileProbe), nameof(BeginCapture)),
                finalizer: new HarmonyMethod(typeof(AdvisorCaptureProfileProbe), nameof(EndCapture)));
            log.LogInfo("CAPTURE-PROFILE-READY targets=" + names.Count);
        }
        private static void BeginCapture(out Sample __state)
        { __state = current = new Sample(); }
        private static Exception? EndCapture(Exception? __exception, Sample __state)
        {
            long elapsed = Stopwatch.GetTimestamp() - __state.Start;
            long allocated = GC.GetAllocatedBytesForCurrentThread() - __state.Allocated;
            current = null;
            log.LogInfo("CAPTURE-PROFILE " + JsonConvert.SerializeObject(new {
                __state.Turn, __state.Frame, Thread = Thread.CurrentThread.ManagedThreadId,
                // Some Unity Mono builds stub this API with zero. The profiler
                // itself allocates, so zero cannot mean a measured zero here.
                TotalMs = Milliseconds(elapsed), AllocatedBytes = allocated > 0 ? (long?)allocated : null,
                GC0 = GC.CollectionCount(0) - __state.GC0, GC1 = GC.CollectionCount(1) - __state.GC1,
                GC2 = GC.CollectionCount(2) - __state.GC2,
                HistoryAdded = ((ICollection)history.GetValue(null)).Count - __state.History,
                HistoryCount = ((ICollection)history.GetValue(null)).Count,
                Error = __exception?.GetType().Name,
                Phases = __state.Rows.OrderByDescending(pair => pair.Value.Exclusive).Select(pair => new {
                    Method = pair.Key, pair.Value.Calls, InclusiveMs = Milliseconds(pair.Value.Inclusive),
                    ExclusiveMs = Milliseconds(pair.Value.Exclusive) }).ToArray()
            }));
            return __exception;
        }
        private static void Begin(MethodBase __originalMethod, out Segment? __state)
        {
            __state = current == null ? null : new Segment(names[__originalMethod], current.Top);
            if (current != null) current.Top = __state;
        }
        private static Exception? End(Exception? __exception, Segment? __state)
        {
            if (current != null && __state != null)
            {
                long elapsed = Stopwatch.GetTimestamp() - __state.Start;
                if (!current.Rows.TryGetValue(__state.Name, out Row row)) current.Rows.Add(__state.Name, row = new Row());
                row.Calls++; row.Inclusive += elapsed; row.Exclusive += elapsed - __state.Children;
                if (__state.Parent != null) __state.Parent.Children += elapsed;
                current.Top = __state.Parent;
            }
            return __exception;
        }
        private static double Milliseconds(long ticks) => ticks * 1000d / Stopwatch.Frequency;
        private sealed class Sample
        {
            internal readonly long Start = Stopwatch.GetTimestamp(), Allocated = GC.GetAllocatedBytesForCurrentThread();
            internal readonly int GC0 = GC.CollectionCount(0), GC1 = GC.CollectionCount(1), GC2 = GC.CollectionCount(2);
            internal readonly int Turn = AllGameManagers.Instance.GetCombatManager().GetTurnCount(), Frame = Time.frameCount;
            internal readonly int History = ((ICollection)history.GetValue(null)).Count;
            internal readonly Dictionary<string, Row> Rows = new Dictionary<string, Row>();
            internal Segment? Top;
        }
        private sealed class Segment
        {
            internal readonly string Name;
            internal readonly Segment? Parent;
            internal readonly long Start = Stopwatch.GetTimestamp();
            internal long Children;
            internal Segment(string name, Segment? parent) { Name = name; Parent = parent; }
        }
        private sealed class Row { internal int Calls; internal long Inclusive, Exclusive; }
    }
}
#endif

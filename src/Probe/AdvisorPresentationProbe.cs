using System;
using HarmonyLib;
using BepInEx.Logging;
using MonsterTrain2Poju.Model;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterTrain2Poju.Probe
{
    // Isolated acceptance only. Observe the actual player UI across native
    // card/EndTurn hooks, animation settlement and verified publications.
    internal static class AdvisorPresentationProbe
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_ADVISOR_PRESENTATION") == "1";
        private static Type type = null!;
        private static object overlay = null!;
        private static ManualLogSource log = null!;
        private static bool expanded;
        private static int retainedInputs, pendingFrames, reusedPublications, scrolledPublications, resets;
        internal static object Evidence => new { RetainedInputs = retainedInputs, PendingFrames = pendingFrames,
            ReusedPublications = reusedPublications, ScrolledPublications = scrolledPublications, Resets = resets };

        private static T Field<T>(string name) => (T)AccessTools.Field(type, name).GetValue(overlay);
        private static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException("Player presentation acceptance: " + message); }

        internal static void Install(Type playerType, object advisor, ManualLogSource logger)
        {
            type = playerType; overlay = advisor; log = logger;
            var harmony = new Harmony("netcan.poju.isolated-presentation-acceptance");
            harmony.Patch(AccessTools.Method(type, "InvalidateState"),
                prefix: new HarmonyMethod(typeof(AdvisorPresentationProbe), nameof(BeforeInvalidation)),
                postfix: new HarmonyMethod(typeof(AdvisorPresentationProbe), nameof(AfterInvalidation)));
            harmony.Patch(AccessTools.Method(type, "Tick"),
                prefix: new HarmonyMethod(typeof(AdvisorPresentationProbe), nameof(BeforeTick)),
                postfix: new HarmonyMethod(typeof(AdvisorPresentationProbe), nameof(AfterTick)));
        }

        private sealed class Snapshot
        {
            internal readonly BattleAdvice? Advice = Field<BattleAdvice?>("displayed");
            internal readonly string Text = Field<Text>("body")?.text ?? "";
            internal readonly float Scroll = Field<ScrollRect>("planScroll")?.verticalNormalizedPosition ?? 1;
            internal readonly long Searches = Field<BattleAdviceSession>("session").SearchCount;
        }

        internal static void ExpandAndScroll()
        {
            AccessTools.Method(type, "SetFullPlan").Invoke(overlay, new object[] { true });
            expanded = true;
            ScrollForNextInput();
        }

        private static void ScrollForNextInput()
        {
            ScrollRect scroll = Field<ScrollRect>("planScroll");
            LayoutRebuilder.ForceRebuildLayoutImmediate(Field<Text>("body").rectTransform);
            if (scroll.content.rect.height > scroll.viewport.rect.height + 10)
                scroll.verticalNormalizedPosition = .37f;
        }

        private static void BeforeInvalidation(out Snapshot __state) => __state = new Snapshot();
        private static void AfterInvalidation(bool preserveContinuation, Snapshot __state)
        {
            if (__state.Advice == null) return;
            if (!preserveContinuation)
            {
                Require(Field<BattleAdvice?>("displayed") == null, "explicit reset kept old advice");
                Require(!Field<Text>("body").text.Contains("下一步") && !Field<Text>("body").text.Contains("完整方案"),
                    "explicit reset kept the old forecast text");
                resets++; return;
            }
            Require(ReferenceEquals(Field<BattleAdvice?>("displayed"), __state.Advice), "input cleared the readable forecast");
            Require(Field<Text>("body").text == __state.Text, "input replaced forecast text with a waiting message");
            Require(Math.Abs(Field<ScrollRect>("planScroll").verticalNormalizedPosition - __state.Scroll) < .005,
                "input moved the reading position");
            Require(Field<Text>("statusLabel").text.Contains("待确认"), "retained forecast was not marked pending");
            Require(Field<BattleAdviceSession>("session").Current == null, "pending forecast remained a valid publication");
            retainedInputs++;
        }

        private static void BeforeTick(out Snapshot __state) => __state = new Snapshot();
        private static void AfterTick(Snapshot __state)
        {
            BattleAdvice? displayed = Field<BattleAdvice?>("displayed");
            if (__state.Advice != null && displayed == null && Field<BattleAdviceSession>("session").SearchCount == __state.Searches &&
                !Field<bool>("paused") && Field<bool>("panelVisible") && Field<string?>("captureError") == null &&
                Field<BattleAdviceSession>("session").Status != BattleAdviceStatus.Unavailable &&
                AllGameManagers.Instance?.GetSaveManager()?.GetGameSequence() == SaveData.GameSequence.InBattle)
                Require(false, "native loading/animation frame cleared the readable forecast without a new search");
            if (displayed == null) return;
            if (ReferenceEquals(displayed, __state.Advice))
            {
                Require(Field<Text>("body").text == __state.Text, "waiting frame rewrote retained text");
                if (Field<Text>("statusLabel").text.Contains("待确认")) pendingFrames++;
                return;
            }
            if (__state.Advice == null || Field<BattleAdviceSession>("session").SearchCount != __state.Searches ||
                !(displayed.ReusedContinuation || displayed.ReusedSelection)) return;
            ScrollRect scroll = Field<ScrollRect>("planScroll");
            if (scroll.content.rect.height > scroll.viewport.rect.height + 10)
            {
                Require(Math.Abs(scroll.verticalNormalizedPosition - __state.Scroll) < .005,
                    "reused publication reset the reading position");
                if (__state.Scroll < .95f) scrolledPublications++;
            }
            Require(!Field<Text>("statusLabel").text.Contains("待确认"), "verified publication still marked pending");
            reusedPublications++;
            log.LogInfo("PLAYER-ADVISOR-PRESENTATION-PASS reuse=" + reusedPublications +
                " retainedInputs=" + retainedInputs + " pendingFrames=" + pendingFrames + " scroll=" + scroll.verticalNormalizedPosition);
            if (expanded) ScrollForNextInput();
        }
    }
}

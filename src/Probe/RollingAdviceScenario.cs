using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    // Test driver only. The interactive advisor never executes these inputs.
    // Each native action consumes just the first step of the newest publication.
    internal static class RollingAdviceScenario
    {
        internal sealed class Decision
        {
            public string Mode { get; set; } = "rolling";
            public BattleTurnState Root { get; set; } = null!;
            public PlayCardAction? Action { get; set; }
            public bool IsEndTurn { get; set; }
            public BattleTurnState Expected { get; set; } = null!;
            public BattleTurnState? Actual { get; set; }
            public RoomOutcome Outcome { get; set; }
            public List<PlannerReplayScenario.Step> PlanSteps { get; set; } = new List<PlannerReplayScenario.Step>();
            public BattleTurnState Planned { get; set; } = null!;
            public RoomOutcome PlannedOutcome { get; set; }
            public double Score { get; set; }
            public double? PolicyBaselineScore { get; set; }
            public int Nodes { get; set; }
            public bool ReusedWinningContinuation { get; set; }
            public long Generation { get; set; }
            public BattleAdvisorOverlay.Publication? Timing { get; set; }
            public string? Difference { get; set; }
        }
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_ROLLING_ADVICE") == "1";
        private static bool Replay => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MT2_PROBE_ROLLING_INPUT"));
        internal static bool WaitingForWorker => Enabled && !Replay &&
#if DEVELOPMENT_PROBE
            !PlayerAdvisorProbe.Enabled &&
#endif
            BattleAdvisorOverlay.Active?.IsPlanning == true;
        internal static List<Decision> Records { get; } = new List<Decision>();
        private static FixtureValue[]? frozen;
        private static bool waiting;
        private static bool terminalObserved;

        internal static bool AwaitingAdvice(BattleTurnState root)
        {
            if (!Enabled) return false;
#if DEVELOPMENT_PROBE
            if (PlayerAdvisorProbe.Enabled)
            {
                if (!PlayerAdvisorProbe.CaptureInitialized) return true;
                if (waiting) Observe(PlayerAdvisorProbe.Capture(), false);
                return !Replay && !PlayerAdvisorProbe.ReadyFor();
            }
#endif
            Observe(root, false);
            return !Replay && !BattleAdvisorOverlay.Active!.ReadyFor(root);
        }

        internal static bool TryChoose(BattleTurnState root, ManualLogSource log, out PlayCardAction? action)
        {
            action = null;
            if (!Enabled) return false;
#if DEVELOPMENT_PROBE
            if (PlayerAdvisorProbe.Enabled) root = PlayerAdvisorProbe.Capture();
#endif
            if (Records.Count >= 256) throw new InvalidOperationException("Rolling advice exceeded the battle decision limit.");
            Decision decision;
            if (Replay)
            {
                if (frozen == null)
                {
                    using var document = FixtureDocument.Read(Environment.GetEnvironmentVariable("MT2_PROBE_ROLLING_INPUT")!);
                    frozen = document.RootElement.GetProperty("RollingDecisions").EnumerateArray().ToArray();
                }
                if (Records.Count >= frozen.Length) throw new InvalidOperationException("Frozen rolling sequence ended before native combat.");
                // Decode only the next input. Prior native observations are
                // replaced below; expanding them eagerly duplicates whole states.
                decision = PlannerReplayScenario.ReadWithoutNativeObservation<Decision>(frozen[Records.Count]);
                if (!Equal(decision.Root, root, false)) throw new InvalidOperationException("Fresh rolling replay root differs at decision " + Records.Count + ": " +
                    UnitPlayModelProbe.FirstDifference(BattleTurnProbe.Comparable(decision.Root), BattleTurnProbe.Comparable(root), "$"));
                decision.Mode = "replay"; decision.Nodes = 0; decision.Actual = null; decision.Timing = null; decision.Difference = null;
            }
            else
            {
                BattleAdvice advice;
                BattleAdvisorOverlay.Publication timing;
#if DEVELOPMENT_PROBE
                if (PlayerAdvisorProbe.Enabled)
                {
                    if (!PlayerAdvisorProbe.ReadyFor()) throw new InvalidOperationException("Rolling driver consumed stale player advice.");
                    advice = PlayerAdvisorProbe.Advice; timing = PlayerAdvisorProbe.LatestTiming;
                }
                else
#endif
                {
                    BattleAdvisorOverlay advisor = BattleAdvisorOverlay.Active!;
                    if (!advisor.ReadyFor(root)) throw new InvalidOperationException("Rolling driver consumed stale advice.");
                    advice = advisor.CurrentAdvice; timing = advisor.Publications.Last();
                }
                BattlePlanResult plan = advice.Plan;
                PlannerReplayScenario.Step[] steps = plan.Steps.Select(step => new PlannerReplayScenario.Step
                    { Action = step.Action, IsEndTurn = step.IsEndTurn }).ToArray();
                if (Records.Count > 0 && timing.Generation <= Records.Last().Generation)
                    throw new InvalidOperationException("Rolling action did not consume a new publication.");
                decision = new Decision { Root = root, Action = steps[0].Action, IsEndTurn = steps[0].IsEndTurn,
                    PlanSteps = steps.ToList(), Planned = plan.State, PlannedOutcome = plan.Outcome, Score = plan.Score,
                    PolicyBaselineScore = plan.PolicyBaselineScore, Nodes = plan.ExpandedNodes,
                    ReusedWinningContinuation = advice.ReusedWinningContinuation,
                    Generation = timing.Generation, Timing = timing };
                if (decision.IsEndTurn)
                {
                    BattleTurnResult next = BattleTurnModel.EndTurn(root);
                    if (!next.Supported) throw new InvalidOperationException(next.UnsupportedReason);
                    decision.Expected = next.State!; decision.Outcome = next.Outcome;
                }
                else
                {
                    BattleActionResult next = BattleActionModel.PlayCard(root, decision.Action!);
                    if (!next.Supported) throw new InvalidOperationException(next.Reason);
                    decision.Expected = next.State!; decision.Outcome = next.Outcome;
                }
            }
            Records.Add(decision); waiting = true; action = decision.Action;
#if DEVELOPMENT_PROBE
            if (PlayerAdvisorProbe.Enabled) action = PlayerAdvisorProbe.NativeAction(action);
#endif
            log.LogInfo("ROLLING-CHOOSE mode=" + decision.Mode + " decision=" + Records.Count + " turn=" + root.Spawn.Turn +
                " endTurn=" + decision.IsEndTurn + " generation=" + decision.Generation + " nodes=" + decision.Nodes +
                " retainedWin=" + decision.ReusedWinningContinuation +
                " searchMs=" + decision.Timing?.SearchMs + " responseMs=" + decision.Timing?.EndToEndMs);
            return true;
        }

        private static bool Equal(BattleTurnState expected, BattleTurnState actual, bool terminal) =>
            JToken.DeepEquals(BattleTurnProbe.Comparable(expected, terminal), BattleTurnProbe.Comparable(actual, terminal));

        private static void Observe(BattleTurnState actual, bool terminal)
        {
            if (!waiting) return;
            Decision decision = Records.Last();
            decision.Actual = actual; waiting = false;
            if (!Equal(decision.Expected, actual, terminal))
            {
                decision.Difference = "Rolling native state differs after decision " + Records.Count + ": " +
                    UnitPlayModelProbe.FirstDifference(BattleTurnProbe.Comparable(decision.Expected, terminal),
                        BattleTurnProbe.Comparable(actual, terminal), "$");
                throw new InvalidOperationException(decision.Difference);
            }
        }

        internal static void ObserveTerminal(ManualLogSource log)
        {
            if (!Enabled || terminalObserved) return;
            if (Records.Count == 0 || !waiting) throw new InvalidOperationException("Native battle ended without a rolling action.");
            Observe(
#if DEVELOPMENT_PROBE
                PlayerAdvisorProbe.Enabled ? PlayerAdvisorProbe.Terminal :
#endif
                // Export can run after native UI advances to EndOfCombat.
                // Reuse the settled StopCombatLoop boundary already captured.
                FullBattleTrace.Active!.TerminalDecision ?? throw new InvalidOperationException("Native terminal checkpoint is missing."), true);
            if (Records.Last().Outcome is not (RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated))
                throw new InvalidOperationException("Rolling model did not predict native combat completion.");
            if (Replay && frozen!.Length != Records.Count) throw new InvalidOperationException("Native battle ended before the frozen rolling sequence.");
            terminalObserved = true;
            log.LogInfo("ROLLING-REPLAY-PASS decisions=" + Records.Count + " mode=" + Records.Last().Mode);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Capture;
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
            public IReadOnlyList<RelentlessDecision>? RelentlessDecisions { get; set; }
            public bool ShouldSerializeRelentlessDecisions() => RelentlessDecisions?.Count > 0;
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
            public bool ReusedContinuation { get; set; }
            public int RequestedHorizon { get; set; }
            public int TurnsPlanned { get; set; }
            public bool UsedShorterHorizon { get; set; }
            public long Generation { get; set; }
            public BattleAdvisorOverlay.Publication? Timing { get; set; }
            public string? Difference { get; set; }
        }
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_ROLLING_ADVICE") == "1";
        private static bool Replay => (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MT2_PROBE_ROLLING_INPUT"))
#if DEVELOPMENT_PROBE
            && !resumed) || UnsupportedBranchObservation.Enabled
#else
            )
#endif
            ;
        internal static bool WaitingForWorker => Enabled && !Replay &&
#if DEVELOPMENT_PROBE
            !PlayerAdvisorProbe.Enabled &&
#endif
            BattleAdvisorOverlay.Active?.IsPlanning == true;
        internal static List<Decision> Records { get; } = new List<Decision>();
        private static FixtureValue[]? frozen;
        private static bool waiting;
        private static bool terminalObserved;
#if DEVELOPMENT_PROBE
        private static bool deploymentExportAttempted;
        private static bool ResumeRequested => Environment.GetEnvironmentVariable("MT2_PROBE_RESUME_ROLLING_ADVICE") == "1";
        private static bool resumed;
        private static string? prefixSourceHash;
        private static int stalePredictions;
        private static long resumeGeneration, firstFreshGeneration;
        internal static object? PrefixResumeEvidence => !ResumeRequested ? null : new
        {
            SourceSHA256 = prefixSourceHash, PrefixInputs = frozen?.Length ?? 0, Resumed = resumed,
            StalePredictions = stalePredictions, ResumeGeneration = resumeGeneration,
            FirstFreshGeneration = firstFreshGeneration
        };

        private static int VerifyResumePrefix(FixtureValue archive, FixtureValue[] rows, Guid nativeModule)
        {
            if (NativeBranchIdentity.SourceModule(archive) != nativeModule ||
                archive.GetProperty("PlayerAdvisor").GetProperty("Assembly").GetString() != "MonsterTrain2Poju.Advisor" ||
                rows.Length is < 1 or >= 256)
                throw new InvalidOperationException("Resume requires a bounded native player prefix from the installed game module.");
            int staleCount = 0;
            BattleTurnState? previous = null;
            for (int index = 0; index < rows.Length; index++)
            {
                FixtureValue row = rows[index];
                FixtureValue plannedStep = row.GetProperty("PlanSteps")[0];
                if (!row.GetProperty("Action").ContentEquals(plannedStep.GetProperty("Action")) ||
                    !row.GetProperty("IsEndTurn").ContentEquals(plannedStep.GetProperty("IsEndTurn")) ||
                    !PlannerReplayScenario.SameChoices(PlannerReplayScenario.ReadChoices(row), PlannerReplayScenario.ReadChoices(plannedStep)))
                    throw new InvalidOperationException("Archived resume input differs from its recorded entry at decision " + (index + 1));
                Decision decision = PlannerReplayScenario.ReadWithoutNativeObservation<Decision>(row);
                BattleTurnState actual = PlannerReplayScenario.Read<BattleTurnState>(row.GetProperty("Actual"));
                if (actual == null || decision.Outcome != RoomOutcome.Exchanged ||
                    (previous == null ? decision.Root.Spawn.Turn != 0 : !Equal(previous, decision.Root, false)) ||
                    decision.IsEndTurn != (decision.Action == null))
                    throw new InvalidOperationException("Incomplete or discontinuous native resume prefix at decision " + (index + 1));

                BattleTurnState? latest;
                RoomOutcome outcome;
                if (decision.IsEndTurn)
                {
                    BattleTurnResult turn = BattleTurnModel.EndTurn(decision.Root, decision.RelentlessDecisions);
                    latest = turn.State; outcome = turn.Outcome;
                }
                else
                {
                    BattleActionResult played = BattleActionModel.PlayCard(decision.Root, decision.Action!);
                    latest = played.State; outcome = played.Outcome;
                }
                if (latest == null || outcome != RoomOutcome.Exchanged || !Equal(latest, actual, false))
                    throw new InvalidOperationException("Current model does not reproduce archived native resume decision " + (index + 1));
                bool stale = !Equal(decision.Expected, actual, false);
                if (stale != (decision.Difference != null))
                    throw new InvalidOperationException("Archived resume prediction disagrees with recorded mismatch at decision " + (index + 1));
                if (stale) staleCount++;
                previous = actual;
            }
            return staleCount;
        }

        private static void ExportPlayerDeployment(bool frozenObservation = false)
        {
            if (Replay && !frozenObservation || deploymentExportAttempted || Records.Count != 0) return;
            deploymentExportAttempted = true;
            BattleTurnState root = PlayerAdvisorProbe.Capture();
            if (root.Spawn.Turn != 0) return;
            // This is an observation, not a completed or accepted native battle.
            // Keep the actual player's identity space for later frozen inputs.
            using (var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, EvidenceKind = "NativeDeploymentObservation", Root = root,
                GameModuleMvid = typeof(CardState).Module.ModuleVersionId,
                PlayerAdvisor = PlayerAdvisorProbe.Evidence
            }))
            using (var stream = new FileStream(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                "player-deployment.mt2f"), FileMode.CreateNew, FileAccess.Write)) document.Write(stream);
        }
#endif

        internal static bool AwaitingAdvice(BattleTurnState root)
        {
            if (!Enabled) return false;
#if DEVELOPMENT_PROBE
            if (PlayerAdvisorProbe.Enabled)
            {
                if (!PlayerAdvisorProbe.CaptureInitialized) return true;
                if (UnsupportedBranchObservation.Enabled)
                {
                    PlayerAdvisorProbe.PauseForFrozenPlan();
                    ExportPlayerDeployment(frozenObservation: true);
                    if (UnsupportedBranchObservation.AwaitingSettlement()) return true;
                    UnsupportedBranchObservation.Observe(PlayerAdvisorProbe.Capture());
                    return false;
                }
                ExportPlayerDeployment();
                if (Replay && Records.Count == 0 && PlayerAdvisorProbe.ReplayFloorBrowsing()) return true;
                if (waiting) Observe(PlayerAdvisorProbe.Capture(), false);
                if (ResumeRequested && !resumed && frozen != null && Records.Count == frozen.Length)
                {
                    resumeGeneration = PlayerAdvisorProbe.ResumeAfterFrozenPrefix();
                    resumed = true;
                    return true;
                }
                if (resumed) return !PlayerAdvisorProbe.ReadyAfterFrozenPrefix(resumeGeneration);
                return !Replay && !PlayerAdvisorProbe.ReadyFor();
            }
#endif
            Observe(root, false);
            return !Replay && !BattleAdvisorOverlay.Active!.ReadyFor(root);
        }

        internal static bool TryChoose(BattleTurnState root, ManualLogSource log, out PlayCardAction? action,
            out IReadOnlyList<RelentlessDecision>? choices)
        {
            action = null;
            choices = null;
            if (!Enabled) return false;
#if DEVELOPMENT_PROBE
            if (PlayerAdvisorProbe.Enabled) root = PlayerAdvisorProbe.Capture();
            if (UnsupportedBranchObservation.Enabled)
            {
                action = UnsupportedBranchObservation.Choose(root, log, out choices);
                return true;
            }
#endif
            if (Records.Count >= 256) throw new InvalidOperationException("Rolling advice exceeded the battle decision limit.");
            Decision decision;
            if (Replay)
            {
                if (frozen == null)
                {
                    string source = Environment.GetEnvironmentVariable("MT2_PROBE_ROLLING_INPUT")!;
                    using var input = System.IO.File.OpenRead(source);
                    using var document = FixtureDocument.Read(input);
                    var rows = document.RootElement.GetProperty("RollingDecisions").EnumerateArray().ToArray();
                    foreach (FixtureValue row in rows)
                    {
                        if (!PlannerReplayScenario.SameChoices(PlannerReplayScenario.ReadChoices(row),
                            PlannerReplayScenario.ReadChoices(row.GetProperty("PlanSteps")[0])))
                            throw new InvalidOperationException("Frozen Relentless choices differ from the first forecast step.");
                    }
#if DEVELOPMENT_PROBE
                    if (ResumeRequested)
                    {
                        if (!PlayerAdvisorProbe.Enabled) throw new InvalidOperationException("Native prefix resume requires the player advisor.");
                        stalePredictions = VerifyResumePrefix(document.RootElement, rows, typeof(CardState).Module.ModuleVersionId);
                        input.Position = 0;
                        using var sha = System.Security.Cryptography.SHA256.Create();
                        prefixSourceHash = BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").ToLowerInvariant();
                    }
#endif
                    frozen = rows;
                }
                if (Records.Count >= frozen.Length) throw new InvalidOperationException("Frozen rolling sequence ended before native combat.");
                // Decode only the next input. Prior native observations are
                // replaced below; expanding them eagerly duplicates whole states.
                decision = PlannerReplayScenario.ReadWithoutNativeObservation<Decision>(frozen[Records.Count]);
                if (!Equal(decision.Root, root, false)) throw new InvalidOperationException("Fresh rolling replay root differs at decision " + Records.Count + ": " +
                    UnitPlayModelProbe.FirstDifference(BattleTurnProbe.Comparable(decision.Root), BattleTurnProbe.Comparable(root), "$"));
                decision.Mode = "replay"; decision.Nodes = 0; decision.Actual = null; decision.Timing = null; decision.Difference = null;
#if DEVELOPMENT_PROBE
                if (ResumeRequested)
                {
                    decision.Mode = "prefix-replay";
                    decision.Expected = PlannerReplayScenario.Read<BattleTurnState>(frozen[Records.Count].GetProperty("Actual"));
                    decision.PlanSteps = new List<PlannerReplayScenario.Step> { new PlannerReplayScenario.Step
                    {
                        Action = decision.Action, IsEndTurn = decision.IsEndTurn, RelentlessDecisions = decision.RelentlessDecisions
                    } };
                    decision.Planned = decision.Expected;
                    decision.PlannedOutcome = decision.Outcome;
                    decision.Score = BattlePlanner.Evaluate(decision.Expected, decision.Outcome);
                    decision.PolicyBaselineScore = null;
                    decision.Generation = 0;
                    decision.ReusedContinuation = false;
                    decision.ReusedWinningContinuation = false;
                }
#endif
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
                PlannerReplayScenario.Step[] steps = plan.Steps.Select(PlannerReplayScenario.Step.From).ToArray();
#if DEVELOPMENT_PROBE
                if (resumed && firstFreshGeneration == 0)
                    firstFreshGeneration = timing.Generation;
#endif
                if (Records.Count > 0 && timing.Generation <= Records.Last().Generation)
                    throw new InvalidOperationException("Rolling action did not consume a new publication.");
                decision = new Decision { Root = root, Action = steps[0].Action, IsEndTurn = steps[0].IsEndTurn,
                    RelentlessDecisions = steps[0].RelentlessDecisions,
                    PlanSteps = steps.ToList(), Planned = plan.State, PlannedOutcome = plan.Outcome, Score = plan.Score,
                    PolicyBaselineScore = plan.PolicyBaselineScore, Nodes = plan.ExpandedNodes,
                    ReusedWinningContinuation = advice.ReusedWinningContinuation,
                    ReusedContinuation = advice.ReusedContinuation,
                    RequestedHorizon = plan.RequestedTurns, TurnsPlanned = plan.TurnsCompleted, UsedShorterHorizon = plan.UsedShorterHorizon,
                    Generation = timing.Generation, Timing = timing };
                if (decision.IsEndTurn)
                {
                    BattleTurnResult next = BattleTurnModel.EndTurn(root, decision.RelentlessDecisions);
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
            if (!decision.IsEndTurn && decision.RelentlessDecisions?.Count > 0)
                throw new InvalidOperationException("Frozen Relentless choices require EndTurn.");
            choices = decision.RelentlessDecisions;
            Records.Add(decision); waiting = true; action = decision.Action;
#if DEVELOPMENT_PROBE
            if (PlayerAdvisorProbe.Enabled) action = PlayerAdvisorProbe.NativeAction(action);
#endif
            log.LogInfo("ROLLING-CHOOSE mode=" + decision.Mode + " decision=" + Records.Count + " turn=" + root.Spawn.Turn +
                " endTurn=" + decision.IsEndTurn + " generation=" + decision.Generation + " nodes=" + decision.Nodes +
                " retainedWin=" + decision.ReusedWinningContinuation +
                " retainedPlan=" + decision.ReusedContinuation +
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

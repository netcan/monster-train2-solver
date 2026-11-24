using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        private const int DecisionLimit = 256;
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
#if DEVELOPMENT_PROBE
        internal static bool DeploymentOnly => Environment.GetEnvironmentVariable("MT2_PROBE_PLAYER_DEPLOYMENT_ONLY") == "1";
        internal static bool OffRouteEnabled => Environment.GetEnvironmentVariable("MT2_PROBE_ADVISOR_OFF_ROUTE") == "1";
        private static long offRouteSearches;
        private static long rapidSearches;
        private static int rapidPublications;
#endif
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
        private static readonly string? timeoutSignalPath = Environment.GetEnvironmentVariable("MT2_PROBE_ROLLING_TIMEOUT_SIGNAL");
        internal static Decision[]? SettledTimeoutPrefix() =>
            string.IsNullOrEmpty(timeoutSignalPath) || !File.Exists(timeoutSignalPath) ? null :
            Records.Take(DecisionLimit - 1)
                .TakeWhile(row => row.Actual != null && row.Difference == null && row.Outcome == RoomOutcome.Exchanged).ToArray();
        private static FixtureValue[]? frozen;
        private static bool waiting;
        private static bool terminalObserved;
        private static int FrozenCount =>
#if DEVELOPMENT_PROBE
            preparedPrefix?.Inputs.Length ??
#endif
            frozen?.Length ?? 0;
#if DEVELOPMENT_PROBE
        private static bool deploymentExportAttempted;
        private static bool ResumeRequested => Environment.GetEnvironmentVariable("MT2_PROBE_RESUME_ROLLING_ADVICE") == "1";
        private static bool resumed;
        private static Task<PreparedPrefix>? prefixVerification;
        private static PreparedPrefix? preparedPrefix;
        private static long resumeGeneration, firstFreshGeneration;
        internal static object? PrefixResumeEvidence => !ResumeRequested ? null : new
        {
            SourceSHA256 = preparedPrefix?.SourceSHA256, PrefixInputs = FrozenCount, Resumed = resumed,
            StalePredictions = preparedPrefix?.StalePredictions ?? 0, ResumeGeneration = resumeGeneration,
            VerificationMilliseconds = preparedPrefix?.VerificationMilliseconds,
            FirstFreshGeneration = firstFreshGeneration
        };

        private static bool PlayerAdviceReady() => resumed
            ? PlayerAdvisorProbe.ReadyAfterFrozenPrefix(resumeGeneration)
            : PlayerAdvisorProbe.ReadyFor();

        private sealed class PreparedPrefix
        {
            internal FixtureValue Archive { get; }
            internal Decision[] Inputs { get; }
            internal string SourceSHA256 { get; }
            internal int StalePredictions { get; }
            internal long VerificationMilliseconds { get; }

            internal PreparedPrefix(FixtureValue archive, Decision[] inputs, string sourceSHA256,
                int stalePredictions, long verificationMilliseconds)
            {
                Archive = archive;
                Inputs = inputs;
                SourceSHA256 = sourceSHA256;
                StalePredictions = stalePredictions;
                VerificationMilliseconds = verificationMilliseconds;
            }
        }

        internal static void StartPrefixVerification()
        {
            if (!Enabled || !ResumeRequested || prefixVerification != null) return;
            string? source = Environment.GetEnvironmentVariable("MT2_PROBE_ROLLING_INPUT");
            Guid nativeModule = typeof(CardState).Module.ModuleVersionId;
            prefixVerification = Task.Run(() => LoadResumePrefix(source, nativeModule));
        }

        private static PreparedPrefix LoadResumePrefix(string? source, Guid nativeModule)
        {
            if (string.IsNullOrEmpty(source)) throw new InvalidOperationException("Native prefix resume requires a replay archive.");
            var timer = System.Diagnostics.Stopwatch.StartNew();
            using var input = File.OpenRead(source);
            using var document = FixtureDocument.Read(input);
            FixtureValue archive = document.RootElement;
            FixtureValue[] rows = archive.GetProperty("RollingDecisions").EnumerateArray().ToArray();
            Decision[] inputs = VerifyResumePrefix(archive, rows, nativeModule, out int staleCount);
            input.Position = 0;
            using var sha = System.Security.Cryptography.SHA256.Create();
            string hash = BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").ToLowerInvariant();
            return new PreparedPrefix(archive, inputs, hash, staleCount, timer.ElapsedMilliseconds);
        }

        private static Decision[] VerifyResumePrefix(FixtureValue archive, FixtureValue[] rows, Guid nativeModule, out int staleCount)
        {
            if (NativeBranchIdentity.SourceModule(archive) != nativeModule ||
                archive.GetProperty("PlayerAdvisor").GetProperty("Assembly").GetString() != "MonsterTrain2Poju.Advisor" ||
                rows.Length is < 1 or >= DecisionLimit)
                throw new InvalidOperationException("Resume requires a bounded native player prefix from the installed game module.");
            staleCount = 0;
            var inputs = new Decision[rows.Length];
            BattleTurnState? previous = null;
            for (int index = 0; index < rows.Length; index++)
            {
                FixtureValue row = rows[index];
                FixtureValue plannedStep = row.GetProperty("PlanSteps")[0];
                if (!row.GetProperty("Action").ContentEquals(plannedStep.GetProperty("Action")) ||
                    !row.GetProperty("IsEndTurn").ContentEquals(plannedStep.GetProperty("IsEndTurn")) ||
                    !PlannerReplayScenario.SameChoices(PlannerReplayScenario.ReadChoices(row), PlannerReplayScenario.ReadChoices(plannedStep)))
                    throw new InvalidOperationException("Archived resume input differs from its recorded entry at decision " + (index + 1));
                BattleTurnState root = PlannerReplayScenario.Read<BattleTurnState>(row.GetProperty("Root"));
                FixtureValue observed = row.GetProperty("Actual");
                BattleTurnState actual = PlannerReplayScenario.Read<BattleTurnState>(observed);
                PlannerReplayScenario.Step step = PlannerReplayScenario.Read<PlannerReplayScenario.Step>(plannedStep);
                if (actual == null || row.GetProperty("Outcome").GetInt32() != (int)RoomOutcome.Exchanged ||
                    (previous == null ? root.Spawn.Turn != 0 : !Equal(previous, root, false)) ||
                    step.IsEndTurn != (step.Action == null) || !step.IsEndTurn && step.RelentlessDecisions?.Count > 0)
                    throw new InvalidOperationException("Incomplete or discontinuous native resume prefix at decision " + (index + 1));

                BattleTurnState? latest;
                RoomOutcome outcome;
                if (step.IsEndTurn)
                {
                    BattleTurnResult turn = BattleTurnModel.EndTurn(root, step.RelentlessDecisions);
                    latest = turn.State; outcome = turn.Outcome;
                }
                else
                {
                    BattleActionResult played = BattleActionModel.PlayCard(root, step.Action!);
                    latest = played.State; outcome = played.Outcome;
                }
                if (latest == null || outcome != RoomOutcome.Exchanged || !Equal(latest, actual, false))
                    throw new InvalidOperationException("Current model does not reproduce archived native resume decision " + (index + 1));
                FixtureValue expected = row.GetProperty("Expected");
                bool stale = !expected.ContentEquals(observed) &&
                    !Equal(PlannerReplayScenario.Read<BattleTurnState>(expected), actual, false);
                if (row.GetProperty("Difference").ValueKind != (stale ? FixtureKind.String : FixtureKind.Null))
                    throw new InvalidOperationException("Archived resume prediction disagrees with recorded mismatch at decision " + (index + 1));
                if (stale) staleCount++;
                inputs[index] = new Decision { Mode = "prefix-replay", Root = root, Action = step.Action,
                    IsEndTurn = step.IsEndTurn, RelentlessDecisions = step.RelentlessDecisions,
                    Expected = actual, Planned = actual, Outcome = outcome, PlannedOutcome = outcome,
                    PlanSteps = new List<PlannerReplayScenario.Step> { step }, Score = BattlePlanner.Evaluate(actual, outcome) };
                previous = actual;
            }
            return inputs;
        }

        internal static void ExportPlayerDeployment(bool frozenObservation = false)
        {
            if (Replay && !frozenObservation || deploymentExportAttempted || Records.Count != 0) return;
            deploymentExportAttempted = true;
            BattleTurnState root = DeploymentOnly ? PlayerAdvisorProbe.CaptureDeploymentOnly() : PlayerAdvisorProbe.Capture();
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
            if (ResumeRequested && !PlayerAdvisorProbe.Enabled)
                throw new InvalidOperationException("Native prefix resume requires the player advisor.");
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
                if (ResumeRequested && preparedPrefix == null)
                {
                    PlayerAdvisorProbe.PauseForFrozenPlan();
                    if (prefixVerification == null) throw new InvalidOperationException("Native prefix verification did not start.");
                    if (!prefixVerification.IsCompleted) return true;
                    preparedPrefix = prefixVerification.GetAwaiter().GetResult();
                }
                if (Replay && Records.Count == 0 && PlayerAdvisorProbe.ReplayFloorBrowsing(preparedPrefix?.Archive)) return true;
                if (waiting) Observe(PlayerAdvisorProbe.Capture(), false);
                if (AdvisorRapidContinuationProbe.Enabled && Records.Count == 1)
                    return AdvisorRapidContinuationProbe.AwaitingBarrier();
                if (AdvisorRapidContinuationProbe.Enabled && Records.Count == 2)
                    AdvisorRapidContinuationProbe.Release();
                if (ResumeRequested && !resumed && Records.Count == FrozenCount)
                {
                    resumeGeneration = PlayerAdvisorProbe.ResumeAfterFrozenPrefix();
                    resumed = true;
                    return true;
                }
                return !Replay && !PlayerAdviceReady();
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
            if (Records.Count >= DecisionLimit) throw new InvalidOperationException("Rolling advice exceeded the battle decision limit.");
            Decision decision;
#if DEVELOPMENT_PROBE
            if (AdvisorRapidContinuationProbe.Enabled && Records.Count == 1)
            {
                Decision original = Records[0];
                if (!AdvisorRapidContinuationProbe.Blocked || original.Actual == null)
                    throw new InvalidOperationException("Native continuation verification did not reach the test barrier.");
                var second = original.PlanSteps[1].Action!;
                BattleActionResult predicted = BattleActionModel.PlayCard(root, second);
                if (!predicted.Supported) throw new InvalidOperationException(predicted.Reason);
                decision = new Decision { Mode = "rapid-continuation", Root = root, Action = second,
                    Expected = predicted.State!, Outcome = predicted.Outcome, Generation = original.Generation,
                    PlanSteps = original.PlanSteps, Planned = original.Planned, PlannedOutcome = original.PlannedOutcome,
                    Score = original.Score, RequestedHorizon = original.RequestedHorizon, TurnsPlanned = original.TurnsPlanned,
                    UsedShorterHorizon = original.UsedShorterHorizon, PolicyBaselineScore = original.PolicyBaselineScore };
            }
            else
#endif
            if (Replay)
            {
                decision = ReplayDecision();
                if (!Equal(decision.Root, root, false)) throw new InvalidOperationException("Fresh rolling replay root differs at decision " + Records.Count + ": " +
                    UnitPlayModelProbe.FirstDifference(BattleTurnProbe.Comparable(decision.Root), BattleTurnProbe.Comparable(root), "$"));
            }
            else
            {
                BattleAdvice advice;
                BattleAdvisorOverlay.Publication timing;
#if DEVELOPMENT_PROBE
                if (PlayerAdvisorProbe.Enabled)
                {
                    if (!PlayerAdviceReady()) throw new InvalidOperationException("Rolling driver consumed stale player advice.");
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
#if DEVELOPMENT_PROBE
                if (AdvisorRapidContinuationProbe.Enabled)
                {
                    if (Records.Count != 0 || root.Spawn.Turn != 0 || steps.Length <= 2 ||
                        steps.Take(2).Any(step => step.IsEndTurn || step.Action is not { ActivatorUnitId: 0 } action ||
                            action.RecursionSelections.Count != 0 || action.MixSelections.Count != 0 || action.SummonSelections.Count != 0))
                        throw new InvalidOperationException("The rapid-continuation test needs two original advised deployment card plays.");
                    rapidSearches = PlayerAdvisorProbe.SearchCount;
                    rapidPublications = PlayerAdvisorProbe.PublicationCount;
                    AdvisorRapidContinuationProbe.Arm();
                }
                if (OffRouteEnabled)
                {
                    if (Records.Count != 0 || root.Spawn.Turn != 0 || steps[0].Action is not { } advised ||
                        root.Spawn.Train.Context == null || root.PlayRules == null)
                        throw new InvalidOperationException("The off-route test requires a published original deployment.");
                    var hand = root.Spawn.Train.Context.Cards.Hand;
                    decision.Action = hand.Where(card => card.InstanceId != advised.CardInstanceId &&
                            root.PlayRules.Cards.Any(rule => rule.DataId == card.DataId && rule.CardType == "Monster" &&
                                rule.Effect == "SpawnMonster" && rule.Summon == null))
                        .Select(card => new PlayCardAction(card.InstanceId, root.SelectedRoom))
                        .FirstOrDefault(action => BattleActionModel.PlayCard(root, action).Supported)
                        ?? throw new InvalidOperationException("The selected deployment floor has no legal alternate native unit card.");
                    decision.Mode = "off-route-observation";
                    offRouteSearches = PlayerAdvisorProbe.SearchCount;
                }
#endif
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

#if DEVELOPMENT_PROBE
        internal static bool WriteMixPromptObservation()
        {
            if (!PlayerAdvisorProbe.MixPromptWitness || PlayerAdvisorProbe.MixPromptRows.Count == 0) return false;
            Decision input = Records.Last();
            PlayCardAction action = input.Action!;
            BattleAdvice final = PlayerAdvisorProbe.Advice;
            var timing = PlayerAdvisorProbe.LatestTiming;
            BattleTurnState native = PlayerAdvisorProbe.Capture();
            using var keys = new BattleStateKeys();
            using var boundaryKeys = new BattleStateKeys(ignorePreviewCopyAllocator: true);
            string nativeRootKey = keys.Key(native);
            if (waiting || input.Actual == null || input.Difference != null || input.Outcome != RoomOutcome.Exchanged ||
                action.MixSelections.Count != PlayerAdvisorProbe.MixPromptRows.Count ||
                !PlayerAdvisorProbe.MixPromptCleared || timing.Generation <= input.Generation ||
                nativeRootKey != final.RootKey || boundaryKeys.Key(input.Actual) != boundaryKeys.Key(final.Root) ||
                FullBattleTrace.Active!.CaptureFailures != 0 || FullBattleTrace.Active.Pending != 0)
                throw new InvalidOperationException("Native Mix prompt did not settle into a fresh verified advisor publication.");
            using var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, EvidenceKind = "NativePlayerAdvisorMixPrompt", GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CardState).Module.ModuleVersionId, PlayerAdvisor = PlayerAdvisorProbe.Evidence,
                Input = input, Prompts = PlayerAdvisorProbe.MixPromptRows, NormalPromptRestored = PlayerAdvisorProbe.MixPromptCleared,
                FinalGeneration = timing.Generation, FinalRoot = final.Root, FinalRootKey = final.RootKey,
                FinalNativeRoot = native, FinalNativeRootKey = nativeRootKey, FinalPlan = final.Plan
            });
            using var stream = new FileStream(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                "player-advisor-mix-prompt.mt2f"), FileMode.CreateNew, FileAccess.Write);
            document.Write(stream);
            return true;
        }

        internal static bool WriteRapidContinuationObservation()
        {
            if (!AdvisorRapidContinuationProbe.Enabled || Records.Count != 2) return false;
            BattleAdvice final = PlayerAdvisorProbe.Advice;
            BattleAdvisorOverlay.Publication timing = PlayerAdvisorProbe.LatestTiming;
            BattleTurnState native = PlayerAdvisorProbe.Capture();
            using var keys = new BattleStateKeys();
            using var boundaryKeys = new BattleStateKeys(ignorePreviewCopyAllocator: true);
            string nativeRootKey = keys.Key(native);
            if (waiting || Records.Any(row => row.Actual == null || row.Difference != null || row.Outcome != RoomOutcome.Exchanged) ||
                !PlayerAdvisorProbe.RapidSecondAcceptedWhileBlocked || timing.Generation <= Records[0].Generation ||
                PlayerAdvisorProbe.SearchCount != rapidSearches || PlayerAdvisorProbe.PublicationCount != rapidPublications + 1 ||
                !final.ReusedContinuation || final.Plan.Steps.Count != Records[0].PlanSteps.Count - 2 ||
                nativeRootKey != final.RootKey || boundaryKeys.Key(Records[1].Actual!) != boundaryKeys.Key(final.Root) ||
                FullBattleTrace.Active!.CaptureFailures != 0 || FullBattleTrace.Active.Pending != 0)
                throw new InvalidOperationException("Rapid native inputs did not publish exactly one verified continuation of the original forecast.");
            using var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, EvidenceKind = "NativePlayerAdvisorRapidContinuation", GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CardState).Module.ModuleVersionId, Inputs = Records,
                OriginalGeneration = Records[0].Generation, FinalGeneration = timing.Generation,
                OriginalSearches = rapidSearches, FinalSearches = PlayerAdvisorProbe.SearchCount,
                OriginalPublications = rapidPublications, FinalPublications = PlayerAdvisorProbe.PublicationCount,
                AdvisorRapidContinuationProbe.VerificationThread, AdvisorRapidContinuationProbe.MainThread,
                SecondAcceptedWhileBlocked = PlayerAdvisorProbe.RapidSecondAcceptedWhileBlocked,
                FinalRoot = final.Root, FinalRootKey = final.RootKey, FinalNativeRoot = native,
                FinalNativeRootKey = nativeRootKey, FinalPlan = final.Plan
            });
            using var stream = new FileStream(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                "player-advisor-rapid-continuation.mt2f"), FileMode.CreateNew, FileAccess.Write);
            document.Write(stream);
            return true;
        }

        internal static bool WriteOffRouteObservation()
        {
            if (!OffRouteEnabled || Records.Count != 1) return false;
            Decision input = Records[0];
            BattleAdvice fresh = PlayerAdvisorProbe.Advice;
            BattleAdvisorOverlay.Publication timing = PlayerAdvisorProbe.LatestTiming;
            long searches = PlayerAdvisorProbe.SearchCount;
            using var keys = new BattleStateKeys();
            using var boundaryKeys = new BattleStateKeys(ignorePreviewCopyAllocator: true);
            BattleTurnState actual = PlayerAdvisorProbe.Capture();
            // The native preview can allocate unused IDs between the recorded
            // action boundary and the later exact publication capture.
            bool settledMatch = input.Actual != null && boundaryKeys.Key(input.Actual) == boundaryKeys.Key(fresh.Root);
            bool currentMatch = keys.Key(actual) == fresh.RootKey;
            if (waiting || input.Actual == null || input.Difference != null || input.Root.Spawn.Turn != 0 ||
                input.Outcome != RoomOutcome.Exchanged || !PlayerAdvisorProbe.OffRouteInputInvalidated ||
                timing.Generation <= input.Generation || searches != offRouteSearches + 1 ||
                fresh.ReusedContinuation || fresh.ReusedWinningContinuation || fresh.ReusedSelection ||
                !settledMatch || !currentMatch ||
                FullBattleTrace.Active!.CaptureFailures != 0 || FullBattleTrace.Active.Pending != 0)
                throw new InvalidOperationException("Native off-route publication failed: waiting=" + waiting +
                    " difference=" + input.Difference + " outcome=" + input.Outcome + " invalidated=" + PlayerAdvisorProbe.OffRouteInputInvalidated +
                    " generation=" + input.Generation + "->" + timing.Generation + " searches=" + offRouteSearches + "->" + searches +
                    " reused=" + (fresh.ReusedContinuation || fresh.ReusedWinningContinuation || fresh.ReusedSelection) +
                    " settledMatch=" + settledMatch + " currentMatch=" + currentMatch + " settledDifference=" +
                    (input.Actual == null ? "missing" : UnitPlayModelProbe.FirstDifference(JToken.FromObject(input.Actual),
                        JToken.FromObject(fresh.Root), "$")) + " captures=" + FullBattleTrace.Active?.CaptureFailures +
                    " pending=" + FullBattleTrace.Active?.Pending);
            using var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, EvidenceKind = "NativePlayerAdvisorOffRoute", GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CardState).Module.ModuleVersionId, PlayerAdvisor = PlayerAdvisorProbe.Evidence,
                Input = input, StaleInputInvalidated = PlayerAdvisorProbe.OffRouteInputInvalidated,
                PreviousSearches = offRouteSearches, FreshSearches = searches, FreshGeneration = timing.Generation,
                FreshRoot = fresh.Root, FreshRootKey = fresh.RootKey, FreshNativeRoot = actual, FreshPlan = fresh.Plan
            });
            using var stream = new FileStream(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                "player-advisor-off-route.mt2f"), FileMode.CreateNew, FileAccess.Write);
            document.Write(stream);
            return true;
        }
#endif

        private static Decision ReplayDecision()
        {
#if DEVELOPMENT_PROBE
            if (ResumeRequested)
            {
                if (preparedPrefix == null || Records.Count >= preparedPrefix.Inputs.Length)
                    throw new InvalidOperationException("The verified native prefix is not available for this input.");
                return preparedPrefix.Inputs[Records.Count];
            }
#endif
            if (frozen == null)
            {
                using var document = FixtureDocument.Read(Environment.GetEnvironmentVariable("MT2_PROBE_ROLLING_INPUT")!);
                var rows = document.RootElement.GetProperty("RollingDecisions").EnumerateArray().ToArray();
                foreach (FixtureValue row in rows)
                    if (!PlannerReplayScenario.SameChoices(PlannerReplayScenario.ReadChoices(row),
                        PlannerReplayScenario.ReadChoices(row.GetProperty("PlanSteps")[0])))
                        throw new InvalidOperationException("Frozen Relentless choices differ from the first forecast step.");
                frozen = rows;
            }
            if (Records.Count >= frozen.Length) throw new InvalidOperationException("Frozen rolling sequence ended before native combat.");
            // Decode only the next input. Prior native observations are
            // replaced below; expanding them eagerly duplicates whole states.
            Decision decision = PlannerReplayScenario.ReadWithoutNativeObservation<Decision>(frozen[Records.Count]);
#if DEVELOPMENT_PROBE
            if (decision.Mode == "prefix-replay")
            {
                decision.RequestedHorizon = 0;
                decision.TurnsPlanned = 0;
                decision.UsedShorterHorizon = false;
            }
#endif
            decision.Mode = "replay"; decision.Nodes = 0; decision.Actual = null; decision.Timing = null; decision.Difference = null;
            return decision;
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
            if (Replay && FrozenCount != Records.Count) throw new InvalidOperationException("Native battle ended before the frozen rolling sequence.");
            terminalObserved = true;
            log.LogInfo("ROLLING-REPLAY-PASS decisions=" + Records.Count + " mode=" + Records.Last().Mode);
        }
    }
}

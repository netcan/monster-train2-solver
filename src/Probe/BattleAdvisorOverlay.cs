using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using ShinyShoe.Loading;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterTrain2Poju.Probe
{
    internal sealed class BattleAdvisorOverlay : IDisposable
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_SCENARIO") == "advisor" ||
            Environment.GetEnvironmentVariable("MT2_PROBE_ADVISOR") == "1";
        internal static BattleAdvisorOverlay? Active { get; private set; }
        private readonly ManualLogSource log;
        private readonly BattleAdviceSession session;
        private readonly int horizon;
        private readonly int timeLimitMs;
        private readonly bool wholeBattle;
        private readonly int maxSteps, searchWorkers;
        private readonly bool playerSession;
        private readonly bool pauseWhenHidden;
        private readonly KeyCode toggleKey;
        private bool ManualSession => playerSession || Environment.GetEnvironmentVariable("MT2_PROBE_SCENARIO") == "advisor";
        private GameObject canvas = null!;
        private GameObject panel = null!;
        private Text body = null!, pauseLabel = null!, toggleLabel = null!, planLabel = null!, statusLabel = null!;
        private ScrollRect planScroll = null!;
        private bool showFullPlan;
        private Font? uiFont;
        private bool panelVisible = true;
        private bool HiddenPause => pauseWhenHidden && !panelVisible;
        private BattleAdvice? displayed;
        private int displayFrame;
        private long displayedSearches;
        private bool captureNeeded = true, observed;
        private bool capturing;
        private AdvisorObservation observation;
        private Task<PreparedBattleObservation>? preparation;
        private long observationRevision, preparationRevision;
        private AdvisorObservation preparationObservation;
        private TimeSpan preparationCaptureElapsed;
        private int manualBattleDistance = -1;
        private static readonly FieldInfo combatChanged = AccessTools.Field(typeof(CombatManager), "combatStateChanged");
        private bool paused, wasInBattle, screenshotTaken;
        private bool selectionPending;
        private float selectionReadyAt;
        private string? captureError;
        private string? manualBattleKey;
        private BattleTurnState? lastCapture;
        internal int Captures { get; private set; }
        internal int PurityFailures { get; private set; }
        internal int Published { get; private set; }
        internal int PublicationRootChecks { get; private set; }
        internal int Invalidations { get; private set; }
        internal int SelectionReuses { get; private set; }
        internal long Searches => session.SearchCount;
        internal int PlanningFrames { get; private set; }
        internal int IdleFrames { get; private set; }
        internal int PanelVertices { get; private set; }
        internal int PanelPixels { get; private set; }
        internal int Failures { get; private set; }
        internal double LastCaptureMs { get; private set; }
        internal double TotalCaptureMs { get; private set; }
        internal double LastProjectionMs { get; private set; }
        internal double LastPurityMs { get; private set; }
        internal double LastPreparationMs { get; private set; }
        internal int PreparedObservations { get; private set; }
        internal int DiscardedPreparations { get; private set; }
        internal int MainThreadId { get; } = Thread.CurrentThread.ManagedThreadId;
        internal int PreparationThreadId { get; private set; }
        internal sealed class Publication
        {
            public long Generation { get; set; }
            public int Turn { get; set; }
            public double CaptureMs { get; set; }
            public double PreparationMs { get; set; }
            public double PublicationCaptureMs { get; set; }
            public double PublicationPreparationMs { get; set; }
            public double QueueMs { get; set; }
            public double SearchMs { get; set; }
            public double VerificationMs { get; set; }
            public double EndToEndMs { get; set; }
            public int SearchWorkers { get; set; }
            public int[] WorkerThreadIds { get; set; } = Array.Empty<int>();
            public bool SearchedToBattleEnd { get; set; }
            public bool ReachedStepLimit { get; set; }
            public bool ReusedWinningContinuation { get; set; }
            public bool ReusedContinuation { get; set; }
            public bool ReusedSelection { get; set; }
        }
        internal List<Publication> Publications { get; } = new List<Publication>();
        internal bool IsPlanning => session.Status == BattleAdviceStatus.Planning;
        internal object Evidence => new { CrossTurnContinuation = true, Captures, PurityFailures, Published, PublicationRootChecks, Invalidations, SelectionReuses, Searches, PlanningFrames, IdleFrames, PanelVertices, PanelPixels, Failures, Publications, LastCaptureMs, TotalCaptureMs,
            LastProjectionMs, LastPurityMs, LastPreparationMs, PreparedObservations, DiscardedPreparations,
            MainThreadId, PreparationThreadId,
            Error = session.Error, UnavailableRoot = session.Status == BattleAdviceStatus.Unavailable ? lastCapture : null, Horizon = horizon,
            PreferredDeploymentRoom = int.Parse(Environment.GetEnvironmentVariable("MT2_PROBE_PLANNER_DEPLOYMENT_ROOM") ?? "2"),
            TacticalSeed = Environment.GetEnvironmentVariable("MT2_PROBE_PLANNER_TACTICAL_SEED") == "1",
            PlannerBackend = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MT2_PROBE_DOTNET_PLANNER")) ? "in-process" : "dotnet",
            TimeLimitMs = timeLimitMs,
            WholeBattle = wholeBattle, MaxSteps = maxSteps, SearchWorkers = searchWorkers,
            Scenario = AllGameManagers.Instance?.GetSaveManager()?.GetCurrentScenarioData()?.name,
            InTestScenario = AllGameManagers.Instance?.GetSaveManager()?.InTestScenario,
            Seed = Environment.GetEnvironmentVariable("MT2_PROBE_SEED"),
            Bosses = FullBattleTrace.Active?.KnownUnits.Where(unit => unit.IsMiniboss() || unit.IsOuterTrainBoss())
                .Select(unit => new { Name = unit.GetSourceCharacterData()?.name, Miniboss = unit.IsMiniboss(), OuterTrainBoss = unit.IsOuterTrainBoss() }).ToArray() };

        internal BattleAdvisorOverlay(ManualLogSource log, PlayerAdvisorSettings? settings = null)
        {
            this.log = log;
            playerSession = settings != null;
            pauseWhenHidden = settings?.PauseWhenHidden ?? true;
            toggleKey = settings?.ToggleKey ?? KeyCode.F8;
            horizon = settings?.Horizon ?? int.Parse(Environment.GetEnvironmentVariable("MT2_PROBE_PLANNER_HORIZON") ?? "3");
            timeLimitMs = settings?.TimeLimitMs ?? int.Parse(Environment.GetEnvironmentVariable("MT2_PROBE_PLANNER_TIME_LIMIT_MS") ?? "3000");
            wholeBattle = settings?.WholeBattle ?? Environment.GetEnvironmentVariable("MT2_PROBE_PLANNER_WHOLE_BATTLE") == "1";
            maxSteps = settings?.MaxSteps ?? int.Parse(Environment.GetEnvironmentVariable("MT2_PROBE_PLANNER_MAX_STEPS") ?? "256");
            searchWorkers = settings?.SearchWorkers ?? int.Parse(Environment.GetEnvironmentVariable("MT2_PROBE_PLANNER_SEARCH_WORKERS") ?? "1");
            if (timeLimitMs < 100 || timeLimitMs > 30000) throw new ArgumentOutOfRangeException(nameof(timeLimitMs));
            int floor = settings?.DeploymentRoom ?? int.Parse(Environment.GetEnvironmentVariable("MT2_PROBE_PLANNER_DEPLOYMENT_ROOM") ?? "2");
            bool tactical = settings?.TacticalSeed ?? Environment.GetEnvironmentVariable("MT2_PROBE_PLANNER_TACTICAL_SEED") == "1";
            string? externalTool = playerSession ? null : Environment.GetEnvironmentVariable("MT2_PROBE_DOTNET_PLANNER");
            if (string.IsNullOrEmpty(externalTool))
                session = new BattleAdviceSession(new BattlePlannerOptions(256, horizon, 8, timeLimit: TimeSpan.FromMilliseconds(timeLimitMs),
                    preferredDeploymentRoom: floor, useTacticalSeed: tactical, allowShorterHorizon: playerSession || wholeBattle,
                    searchToBattleEnd: wholeBattle, maxSteps: maxSteps), searchWorkers);
            else
            {
                if (wholeBattle || searchWorkers != 1) throw new InvalidOperationException("Whole-battle/parallel search requires the in-process planner.");
                var external = new ExternalBattlePlanner(externalTool!, horizon, timeLimitMs, floor, tactical);
                session = new BattleAdviceSession(external.Search, external.Verify);
            }
            Active = this;
            if (playerSession) new FullBattleTrace(log);
            log.LogInfo("ADVISOR-READY background model worker; " + toggleKey + " toggles overlay; no automatic plays.");
        }

        private void EnsureUi()
        {
            // Recover if scene changes clear the persistent canvas.
            if (canvas != null && body != null) return;
            if (canvas != null) UnityEngine.Object.Destroy(canvas);
            displayed = null;
            canvas = new GameObject("MT2 Battle Advisor", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            UnityEngine.Object.DontDestroyOnLoad(canvas);
            Canvas layer = canvas.GetComponent<Canvas>(); layer.renderMode = RenderMode.ScreenSpaceOverlay; layer.sortingOrder = 32000;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            panel = new GameObject("Advice panel", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(canvas.transform, false);
            RectTransform rect = panel.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(20, -20); rect.sizeDelta = new Vector2(460, 365);
            panel.GetComponent<Image>().color = new Color(.045f, .06f, .09f, .97f);
            Font font = uiFont ??= Font.CreateDynamicFontFromOSFont(new[] { "PingFang SC", "Hiragino Sans GB", "Heiti SC", "Microsoft YaHei", "Noto Sans CJK SC", "Arial" }, 18);
            Text Label(string name, string text, float y, float height, int size)
            {
                var child = new GameObject(name, typeof(RectTransform), typeof(Text)); child.transform.SetParent(panel.transform, false);
                RectTransform place = child.GetComponent<RectTransform>(); place.anchorMin = place.anchorMax = place.pivot = new Vector2(0, 1);
                place.anchoredPosition = new Vector2(18, -y); place.sizeDelta = new Vector2(424, height);
                Text label = child.GetComponent<Text>(); label.font = font; label.fontSize = size;
                label.color = new Color(.91f, .94f, .98f); label.text = text; label.raycastTarget = false;
                label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
                return label;
            }
            Text titleLabel = Label("Title", "怪物火车 · 战局建议", 14, 34, 23);
            titleLabel.raycastTarget = true;
            titleLabel.gameObject.AddComponent<AdvisorPanelDrag>().Target = rect;
            statusLabel = Label("Scope", "已知牌序 · 未来 " + horizon + " 回合 · " + toggleKey + " 显示/隐藏", 52, 28, 15);
            statusLabel.color = new Color(.55f, .72f, .84f);
            body = Label("Recommendation", "进入战斗后自动计算建议。", 92, 208, 18);
            var viewport = new GameObject("Plan viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            viewport.transform.SetParent(panel.transform, false);
            RectTransform viewRect = viewport.GetComponent<RectTransform>();
            viewRect.anchorMin = viewRect.anchorMax = viewRect.pivot = new Vector2(0, 1);
            viewRect.anchoredPosition = new Vector2(18, -92); viewRect.sizeDelta = new Vector2(424, 208);
            viewport.GetComponent<Image>().color = Color.clear;
            body.transform.SetParent(viewport.transform, false);
            RectTransform content = body.rectTransform; content.anchoredPosition = Vector2.zero;
            body.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            planScroll = viewport.GetComponent<ScrollRect>(); planScroll.viewport = viewRect; planScroll.content = content;
            planScroll.horizontal = false; planScroll.vertical = true; planScroll.scrollSensitivity = 35;
            planScroll.movementType = ScrollRect.MovementType.Clamped;
            Text Button(string title, float x, Action click, bool launcher = false)
            {
                var button = new GameObject(title, typeof(RectTransform), typeof(Image), typeof(Button));
                button.transform.SetParent(launcher ? canvas.transform : panel.transform, false);
                RectTransform place = button.GetComponent<RectTransform>();
                place.anchorMin = place.anchorMax = place.pivot = new Vector2(launcher ? 1 : 0, 1);
                place.anchoredPosition = launcher ? new Vector2(-20, -20) : new Vector2(x, -315);
                place.sizeDelta = launcher ? new Vector2(160, 34) : new Vector2(132, 32);
                button.GetComponent<Image>().color = new Color(.12f, .22f, .29f);
                button.GetComponent<Button>().onClick.AddListener(() => click());
                var child = new GameObject("Label", typeof(RectTransform), typeof(Text)); child.transform.SetParent(button.transform, false);
                RectTransform textRect = child.GetComponent<RectTransform>(); textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
                textRect.offsetMin = textRect.offsetMax = Vector2.zero;
                Text label = child.GetComponent<Text>(); label.font = font; label.fontSize = 16; label.color = Color.white;
                label.alignment = TextAnchor.MiddleCenter; label.text = title; label.raycastTarget = false;
                return label;
            }
            Button("重新计算", 18, () => { paused = false; pauseLabel.text = "暂停建议"; Invalidate(); });
            pauseLabel = Button(paused ? "恢复建议" : "暂停建议", 164, () => { paused = !paused; Invalidate(); pauseLabel.text = paused ? "恢复建议" : "暂停建议"; });
            planLabel = Button("完整方案", 310, () => SetFullPlan(!showFullPlan));
            toggleLabel = Button(panelVisible ? "隐藏破局建议" : "显示破局建议", 0, () => SetPanelVisible(!panelVisible), launcher: true);
            SetFullPlan(showFullPlan);
            panel.SetActive(panelVisible);
            log.LogInfo("ADVISOR-UI ready resolution=" + Screen.width + "x" + Screen.height +
                " visible=" + panelVisible + " toggle=" + toggleKey);
        }

        internal void Keyboard()
        { if (canvas != null && Input.GetKeyDown(toggleKey)) SetPanelVisible(!panelVisible); }

        private void SetPanelVisible(bool visible)
        {
            if (panelVisible == visible) return;
            panelVisible = visible;
            if (panel != null) panel.SetActive(visible);
            if (toggleLabel != null) toggleLabel.text = visible ? "隐藏破局建议" : "显示破局建议";
            if (pauseWhenHidden) Invalidate();
        }

        private void SetFullPlan(bool expanded)
        {
            showFullPlan = expanded;
            panel.GetComponent<RectTransform>().sizeDelta = new Vector2(460, expanded ? 620 : 365);
            planScroll.viewport.sizeDelta = new Vector2(424, expanded ? 450 : 208);
            foreach (Text label in new[] { pauseLabel, planLabel })
            {
                RectTransform button = (RectTransform)label.transform.parent;
                button.anchoredPosition = new Vector2(button.anchoredPosition.x, expanded ? -570 : -315);
            }
            RectTransform refresh = (RectTransform)panel.transform.Find("重新计算");
            refresh.anchoredPosition = new Vector2(18, expanded ? -570 : -315);
            planLabel.text = expanded ? "收起方案" : "完整方案";
            if (displayed != null) body.text = Describe(displayed);
            planScroll.verticalNormalizedPosition = 1;
        }

        private void SetPending(bool pending)
        {
            if (statusLabel == null) return;
            statusLabel.text = pending ? "正在核对后续步骤 · 原方案待确认" :
                "已知牌序 · 未来 " + horizon + " 回合 · " + toggleKey + " 显示/隐藏";
            body.color = pending ? new Color(.65f, .72f, .78f) : new Color(.91f, .94f, .98f);
        }

        private void ShowMessage(string message)
        {
            displayed = null;
            SetPending(false);
            if (body != null) body.text = message;
        }

        private static bool Ready(AllGameManagers managers)
        {
            SaveManager save = managers.GetSaveManager(); CombatManager combat = managers.GetCombatManager();
            CardManager cards = managers.GetCardManager();
            return save != null && combat != null && cards != null && !LoadingScreen.IsWorking() && !save.PreviewMode &&
                !PreviewRngIsolation.Active && save.GetGameSequence() == SaveData.GameSequence.InBattle &&
                !managers.GetScreenManager().GetScreenActive(ScreenName.Dialog) && CardAnimationSettlement.Ready() &&
                NativeEffectSettlement.Ready(combat) && combat.ShouldShowEndTurnButton() && NativeDecisionReadiness.CardsDrawn(combat) && !combat.IsRunningTriggerQueue &&
                (!save.GetBattlePreviewEnabled() || !(bool)combatChanged.GetValue(combat)) &&
                !managers.GetReplayManager().IsCardPlaying() && !(managers.GetHandUI()?.AreAnyCardsAnimating() ?? false);
        }

        internal void Tick()
        {
            try { TickCore(); }
            catch (Exception error)
            {
                Failures++; paused = true; session.Invalidate(); displayed = null;
                log.LogError("ADVISOR-FAIL " + error);
                if (pauseLabel != null) pauseLabel.text = "恢复建议";
                ShowMessage("建议暂时停止，可继续手动操作。\n点击重新计算后恢复建议。");
            }
        }

        private void TickCore()
        {
            // The launcher and waiting state are useful before battle managers
            // exist and while the game loads. Only state capture must wait.
            EnsureUi();
            Keyboard();
            AllGameManagers? managers = AllGameManagers.Instance;
            if (managers == null || LoadingScreen.IsWorking())
            {
                session.InvalidateForInput(); session.Poll();
                observationRevision++;
                captureNeeded = true; observed = false;
                // Native card/turn animations also use LoadingScreen's work
                // counter. That does not mean the battle scene was replaced.
                if (displayed != null && !paused && !HiddenPause &&
                    managers?.GetSaveManager()?.GetGameSequence() == SaveData.GameSequence.InBattle) SetPending(true);
                else ShowMessage("等待场景加载完成…");
                return;
            }
            bool inBattle = managers?.GetSaveManager()?.GetGameSequence() == SaveData.GameSequence.InBattle;
            if (!inBattle && wasInBattle && ManualSession)
            { new FullBattleTrace(log); manualBattleKey = null; Invalidate(); }
            wasInBattle = inBattle;
            // A settled publication needs no model traversal or animation scan.
            // Input/mutation hooks, cheap resource/pile changes and native busy
            // boundaries wake it; cosmetic previews alone leave it untouched.
            if (inBattle && !paused && !HiddenPause && observed && preparation == null && !captureNeeded && !selectionPending &&
                ReferenceEquals(displayed, session.Current) &&
                session.Status is BattleAdviceStatus.Ready or BattleAdviceStatus.Unavailable &&
                (managers!.GetSaveManager().PreviewMode || observation.Equals(new AdvisorObservation(managers))) &&
                NativeEffectSettlement.Ready(managers.GetCombatManager()) && !managers.GetCombatManager().IsRunningTriggerQueue &&
                !managers.GetReplayManager().IsCardPlaying())
            { IdleFrames++; return; }
            if (paused || HiddenPause || managers == null || !Ready(managers))
            {
                captureNeeded = true;
                if (preparation != null && preparationRevision == observationRevision)
                    observationRevision++;
                // A native preview/animation is not proof of a gameplay input.
                // Keep its verified candidate and in-flight worker; the next
                // settled full snapshot must still match before publication.
                // Actual plays/mutations invalidate through their input hooks.
                if ((paused || HiddenPause || !inBattle) && session.Status != BattleAdviceStatus.Idle)
                    InvalidateState(false, "paused-or-outside-battle");
                session.Poll();
                // Keep the last forecast readable through native input and
                // animation settlement, without presenting it as current advice.
                if (inBattle && !paused && !HiddenPause && displayed != null) SetPending(true);
                else ShowMessage(paused || HiddenPause ? "建议已暂停。" : inBattle ? "等待当前操作完成…" : "进入战斗后自动计算建议。");
                return;
            }
            if (ManualSession)
            {
                SaveManager save = managers.GetSaveManager();
                int distance = save.GetCurrentDistance();
                if (manualBattleKey == null || manualBattleDistance != distance || FullBattleTrace.Active?.NativeWon != null)
                {
                    new FullBattleTrace(log); manualBattleDistance = distance;
                    manualBattleKey = save.GetShareCode() + ":" + distance; Invalidate();
                }
            }
            var currentObservation = new AdvisorObservation(managers);
            if (observed && !observation.Equals(currentObservation) && !selectionPending)
            {
                if (observation.SameGameplay(currentObservation)) MarkSelection();
                else InvalidateState(true, "observed-gameplay-change");
            }
            observation = currentObservation; observed = true;
            session.Poll();
            if (session.Status == BattleAdviceStatus.Planning) PlanningFrames++;
            if (!playerSession && Environment.GetEnvironmentVariable("MT2_PROBE_ADVISOR") == "1" &&
                managers.GetCombatManager().GetTurnCount() < int.Parse(Environment.GetEnvironmentVariable("MT2_PROBE_PLANNER_TURN") ?? "0")) return;
            // Gameplay inputs invalidate immediately; browsing keeps the worker
            // and observes its new UI selection once the preview has settled.
            // Hash detached model values on a worker. A publication still needs
            // an exact fresh root match, with no input/selection/busy boundary or
            // cheap observation change between capture and accepting that hash.
            bool publicationChecked = false;
            if (preparation?.IsCompleted == true)
            {
                Task<PreparedBattleObservation> completed = preparation; preparation = null;
                try
                {
                    PreparedBattleObservation prepared = completed.GetAwaiter().GetResult();
                    if (preparationRevision != observationRevision || !preparationObservation.Equals(currentObservation))
                    { DiscardedPreparations++; captureNeeded = true; }
                    else
                    {
                        PreparationThreadId = prepared.ThreadId;
                        if (PreparationThreadId == MainThreadId)
                            throw new InvalidOperationException("Snapshot preparation ran on the native main thread.");
                        LastPreparationMs = prepared.Elapsed.TotalMilliseconds; PreparedObservations++;
                        session.Observe(prepared, preparationCaptureElapsed);
                        selectionPending = false; captureNeeded = false;
                        publicationChecked = session.Current?.RootKey == prepared.RootKey;
                    }
                }
                catch (Exception error)
                { session.Invalidate(); captureError = error.Message; log.LogError("ADVISOR-CAPTURE-FAIL " + error); }
            }
            bool unpublished = session.Current != null && !ReferenceEquals(displayed, session.Current);
            if (captureError == null && preparation == null && !publicationChecked &&
                (!selectionPending || Time.realtimeSinceStartup >= selectionReadyAt) && (unpublished || selectionPending ||
                captureNeeded && session.Status != BattleAdviceStatus.Planning))
            {
                try
                {
                    var capture = Stopwatch.StartNew();
                    BattleTurnState root = CaptureReadOnly(managers);
                    lastCapture = root;
                    capture.Stop();
                    LastCaptureMs = capture.Elapsed.TotalMilliseconds; TotalCaptureMs += LastCaptureMs;
                    preparationRevision = observationRevision; preparationObservation = currentObservation;
                    preparationCaptureElapsed = capture.Elapsed;
                    preparation = session.PrepareObservationAsync(root);
                    captureNeeded = false;
                }
                catch (Exception error)
                { session.Invalidate(); captureError = error.Message; log.LogError("ADVISOR-CAPTURE-FAIL " + error); }
            }
            // A selection-sensitive effect can make the retained line diverge.
            // As soon as replay falls back to search, hide its old forecast.
            if (session.Current == null && session.SearchCount != displayedSearches) displayed = null;
            if (publicationChecked && session.Current != null && !ReferenceEquals(displayed, session.Current))
            {
                PublicationRootChecks++;
                bool preserveScroll = displayed != null && session.SearchCount == displayedSearches &&
                    (session.Current.ReusedContinuation || session.Current.ReusedSelection);
                float scroll = preserveScroll ? planScroll.verticalNormalizedPosition : 1;
                displayed = session.Current; Published++;
                if (displayed.ReusedSelection) SelectionReuses++;
                displayFrame = Time.frameCount;
                displayedSearches = session.SearchCount;
                string description = Describe(displayed);
                if (body.text != description)
                {
                    body.text = description;
                    // Rebuild only this text's layout before restoring the
                    // reading position after the suffix changes content height.
                    LayoutRebuilder.ForceRebuildLayoutImmediate(body.rectTransform);
                    planScroll.verticalNormalizedPosition = scroll;
                }
                SetPending(false);
                Publications.Add(new Publication { Generation = session.Generation, Turn = displayed.Root.Spawn.Turn,
                    CaptureMs = displayed.CaptureElapsed.TotalMilliseconds, QueueMs = displayed.QueueElapsed.TotalMilliseconds,
                    PreparationMs = displayed.PreparationElapsed.TotalMilliseconds,
                    PublicationCaptureMs = LastCaptureMs, PublicationPreparationMs = LastPreparationMs,
                    SearchMs = displayed.Plan.Elapsed.TotalMilliseconds, VerificationMs = displayed.VerificationElapsed.TotalMilliseconds,
                    EndToEndMs = displayed.ResponseElapsed.TotalMilliseconds,
                    SearchWorkers = displayed.Plan.SearchWorkers, WorkerThreadIds = displayed.Plan.WorkerThreadIds.ToArray(),
                    SearchedToBattleEnd = displayed.Plan.SearchedToBattleEnd, ReachedStepLimit = displayed.Plan.ReachedStepLimit,
                    ReusedWinningContinuation = displayed.ReusedWinningContinuation, ReusedSelection = displayed.ReusedSelection,
                    ReusedContinuation = displayed.ReusedContinuation });
                if (playerSession && Publications.Count > 128) Publications.RemoveAt(0);
                log.LogInfo((playerSession ? "POJU-ADVICE" : "ADVISOR-PUBLISHED") + " generation=" + session.Generation + " steps=" + displayed.Plan.Steps.Count +
                    " elapsedMs=" + displayed.Plan.Elapsed.TotalMilliseconds.ToString("F1") + " capturePurityFailures=" + PurityFailures +
                    " captureMs=" + displayed.CaptureElapsed.TotalMilliseconds.ToString("F1") +
                    " prepareMs=" + displayed.PreparationElapsed.TotalMilliseconds.ToString("F1") +
                    " publishCaptureMs=" + LastCaptureMs.ToString("F1") + " publishPrepareMs=" + LastPreparationMs.ToString("F1") +
                    " projectionMs=" + LastProjectionMs.ToString("F1") + " purityMs=" + LastPurityMs.ToString("F1") +
                    " verifyMs=" + displayed.VerificationElapsed.TotalMilliseconds.ToString("F1") +
                    " wholeBattle=" + displayed.Plan.SearchedToBattleEnd + " workers=" + displayed.Plan.SearchWorkers +
                    " workerThreads=" + string.Join(",", displayed.Plan.WorkerThreadIds) +
                    " source=" + (displayed.ReusedSelection ? "floor-browse" : displayed.ReusedWinningContinuation ? "verified-continuation" :
                        displayed.ReusedContinuation ? "verified-plan" : "search"));
                if (!playerSession && !screenshotTaken && Environment.GetEnvironmentVariable("MT2_ADVISOR_SCREENSHOT") == "1")
                {
                    CapturePanel(); screenshotTaken = true;
                }
            }
            else if (captureError != null || session.Status == BattleAdviceStatus.Unavailable || displayed == null)
                ShowMessage(captureError != null ? "局面采集失败，请重新计算。" : session.Status == BattleAdviceStatus.Unavailable ?
                    "本局面暂时无法给出完整建议。\n可重新计算或继续手动操作。" :
                    session.Status == BattleAdviceStatus.Planning && session.SearchCount != displayedSearches ?
                    "正在计算新方案…\n你可以继续操作，建议会随局面更新。" : "正在核对局面…");
            else SetPending(!ReferenceEquals(displayed, session.Current) || captureNeeded || selectionPending || preparation != null);
        }

        private BattleTurnState CaptureReadOnly(AllGameManagers managers)
        {
            string Stamp() => TwoTurnScenario.CaptureSnapshot("advisor", managers, managers.GetSaveManager(),
                managers.GetCombatManager(), managers.GetCardManager(), log, writeLog: false, includePresentationRng: true) +
                " room=" + managers.GetRoomManager().GetSelectedRoom();
            capturing = true;
            try
            {
                var timer = Stopwatch.StartNew();
                string before = Stamp(); UnityEngine.Random.State unity = UnityEngine.Random.state; int frame = Time.frameCount;
                double beforeMs = timer.Elapsed.TotalMilliseconds;
                timer.Restart();
                BattleTurnState root = FullBattleTrace.Active!.CaptureDecision();
                LastProjectionMs = timer.Elapsed.TotalMilliseconds; timer.Restart();
                if (before != Stamp() || !unity.Equals(UnityEngine.Random.state) || frame != Time.frameCount)
                { PurityFailures++; throw new InvalidOperationException("Decision capture changed native state, RNG or frame."); }
                LastPurityMs = beforeMs + timer.Elapsed.TotalMilliseconds;
                Captures++; return root;
            }
            finally { capturing = false; }
        }

        internal bool ReadyFor(BattleTurnState root)
        {
            if (paused || HiddenPause || Failures > 0) throw new InvalidOperationException("Advisor is paused or failed.");
            using var keys = new BattleStateKeys();
            string key = keys.Key(root);
            if (session.Status == BattleAdviceStatus.Unavailable) throw new InvalidOperationException(session.Error);
            return !captureNeeded && !selectionPending && preparation == null && observed &&
                session.Current?.RootKey == key && ReferenceEquals(displayed, session.Current) && Time.frameCount > displayFrame + 1;
        }
        internal BattlePlanResult CurrentPlan => session.Current?.Plan ?? throw new InvalidOperationException("Advice is not ready.");
        internal BattleAdvice CurrentAdvice => session.Current ?? throw new InvalidOperationException("Advice is not ready.");

        private void CapturePanel()
        {
            // Desktop screenshots can be black in an occluded/locked Windows
            // session. Render the actual Unity UI into its own target instead.
            var target = new RenderTexture(1280, 800, 24);
            var cameraObject = new GameObject("Advisor capture camera", typeof(Camera));
            var pixels = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            Camera camera = cameraObject.GetComponent<Camera>(); camera.enabled = false;
            Canvas layer = canvas.GetComponent<Canvas>();
            RenderMode previousMode = layer.renderMode; Camera previousCamera = layer.worldCamera;
            RenderTexture previousTarget = RenderTexture.active;
            Transform[] children = canvas.GetComponentsInChildren<Transform>(true);
            int[] layers = children.Select(child => child.gameObject.layer).ToArray();
            try
            {
                target.Create(); camera.targetTexture = target; camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.015f, .02f, .03f); camera.cullingMask = 1 << 31;
                camera.orthographic = true;
                foreach (Transform child in children) child.gameObject.layer = 31;
                layer.renderMode = RenderMode.ScreenSpaceCamera; layer.worldCamera = camera;
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1280, 800), 0, 0); pixels.Apply();
                PanelVertices = canvas.GetComponentsInChildren<Text>().Sum(label => label.cachedTextGenerator.vertexCount);
                Color32 reference = pixels.GetPixel(1279, 0);
                PanelPixels = pixels.GetPixels32().Count(pixel => pixel.r != reference.r || pixel.g != reference.g || pixel.b != reference.b);
                if (PanelVertices == 0 || PanelPixels < 1000) throw new InvalidOperationException("Advisor panel render is empty.");
                File.WriteAllBytes(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "advisor-ready.png"), pixels.EncodeToPNG());
                log.LogInfo("ADVISOR-PANEL-CAPTURE-PASS vertices=" + PanelVertices + " pixels=" + PanelPixels);
            }
            finally
            {
                layer.renderMode = previousMode; layer.worldCamera = previousCamera; RenderTexture.active = previousTarget;
                for (int index = 0; index < children.Length; index++) children[index].gameObject.layer = layers[index];
                camera.targetTexture = null; target.Release();
                UnityEngine.Object.Destroy(cameraObject); UnityEngine.Object.Destroy(target); UnityEngine.Object.Destroy(pixels);
            }
        }

        private string Describe(BattleAdvice advice)
        {
            BattlePlanResult plan = advice.Plan;
            int hp = plan.State.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => unit.IsPyre).Select(unit => unit.Health).FirstOrDefault();
            int initialHp = advice.Root.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => unit.IsPyre).Select(unit => unit.Health).FirstOrDefault();
            var text = new StringBuilder();
            text.Append(plan.Outcome == RoomOutcome.BattleWon ? "预计获胜" : plan.Outcome == RoomOutcome.PlayerDefeated ? "预计战败" :
                plan.Outcome == RoomOutcome.Stalemate ? "预计僵持" : "规划窗口结束")
                .Append(" · 薪火 ").Append(hp).Append(" 血")
                .Append(" · 预计损失 ").Append(Math.Max(0, initialHp - hp));
            if (plan.UsedShorterHorizon)
            {
                if (plan.SearchedToBattleEnd) text.Append("\n整场目标 · 部分方案 · 已预测 ").Append(plan.TurnsCompleted).Append(" 回合");
                else text.Append(" · 已完成 ").Append(plan.TurnsCompleted).Append('/').Append(plan.RequestedTurns).Append(" 回合（预算内缩短）");
            }
            FullBattleTrace trace = FullBattleTrace.Active!;
            var cards = trace.KnownCards.ToDictionary(card => trace.CardId(card));
            var units = trace.KnownUnits.Where(unit => unit != null).ToDictionary(unit => trace.UnitId(unit));
            string CardName(int id)
            {
                if (cards.TryGetValue(id, out CardState known)) return known.GetTitle();
                string? dataId = advice.Root.Spawn.Train.Context?.FindCard(id)?.DataId ?? plan.State.Spawn.Train.Context?.FindCard(id)?.DataId;
                return dataId == null ? "生成的卡牌 #" + id :
                    AllGameManagers.Instance.GetSaveManager().GetAllGameData().FindCardData(dataId)?.GetName() ?? "卡牌 #" + id;
            }
            string UnitName(int id) => units.TryGetValue(id, out CharacterState known) ? known.GetName() : "单位 #" + id;
            void AppendStep(BattleSearchStep step)
            {
                if (step.IsEndTurn) { text.Append("结束回合"); return; }
                PlayCardAction action = step.Action!;
                if (action.ActivatorUnitId > 0 && units.TryGetValue(action.ActivatorUnitId, out CharacterState actor))
                    text.Append(actor.GetName()).Append("：").Append(actor.GetUnitAbilityCardState()?.GetTitle() ?? "单位技能");
                else if (action.ActivatorUnitId > 0) text.Append(UnitName(action.ActivatorUnitId)).Append("：单位技能");
                else text.Append(action.CardInstanceId > 0 ? CardName(action.CardInstanceId) : "房间技能");
                text.Append(showFullPlan ? " → 第 " : "\n第 ").Append(action.RoomIndex + 1).Append(" 层");
                if (action.PlayerPosition >= 0) text.Append(" · 部署位 ").Append(action.PlayerPosition + 1);
                if (action.TargetSpawnPointIndex >= 0) text.Append(" · 目标部署位 ").Append(action.TargetSpawnPointIndex + 1);
                if (action.ActivatorRoomIndex.HasValue) text.Append(" · 来源：第 ").Append(action.ActivatorRoomIndex.Value + 1).Append(" 层房间技能");
                if (action.TargetUnitId > 0)
                {
                    text.Append(" · 目标：").Append(UnitName(action.TargetUnitId));
                    if (!showFullPlan && units.TryGetValue(action.TargetUnitId, out CharacterState target))
                        text.Append("（").Append(target.GetHP()).Append(" 血）");
                }
            }
            if (showFullPlan)
            {
                text.Append("\n完整方案 · ").Append(plan.Steps.Count).Append(" 步\n按当前局面预测，操作后更新。\n\n当前回合\n");
                int turns = 0;
                for (int index = 0; index < plan.Steps.Count; index++)
                {
                    text.Append(index + 1).Append(". "); AppendStep(plan.Steps[index]); text.Append('\n');
                    if (plan.Steps[index].IsEndTurn && index + 1 < plan.Steps.Count)
                        text.Append("\n后续第 ").Append(++turns).Append(" 回合\n");
                }
            }
            else { text.Append("\n\n下一步：\n"); AppendStep(plan.Steps[0]); }
            if (advice.ReusedSelection) text.Append("\n\n沿用当前建议");
            else if (advice.ReusedWinningContinuation) text.Append("\n\n沿用已验证胜局");
            else if (advice.ReusedContinuation) text.Append("\n\n沿用已验证步骤");
            else text.Append("\n\n搜索 ").Append((plan.Elapsed.TotalMilliseconds / 1000).ToString("F2")).Append(" 秒");
            if (plan.TimedOut) text.Append(" · 使用预算内已验证方案");
            if (plan.ReachedStepLimit) text.Append("\n部分路线已达 ").Append(plan.MaxSteps).Append(" 步上限");
            if (plan.UnsupportedAttempts.Count > 0) text.Append("\n部分操作尚未纳入比较");
            return text.ToString();
        }

        private void Invalidate() => InvalidateState(false, "manual-reset");

        private void InvalidateState(bool preserveContinuation, string reason = "native-state-change")
        {
            observationRevision++;
            if (session.Status != BattleAdviceStatus.Idle) Invalidations++;
            if (playerSession && session.Status != BattleAdviceStatus.Idle)
                log.LogInfo("POJU-INVALIDATE reason=" + reason + " generation=" + session.Generation);
            if (preserveContinuation) session.InvalidateForInput(); else session.Invalidate();
            captureError = null; captureNeeded = true; observed = false; selectionPending = false;
            if (preserveContinuation && displayed != null) SetPending(true);
            else ShowMessage("正在核对局面…");
        }
        private static void NativeAction(bool preserveContinuation = true, string reason = "native-mutation")
        {
            if (Active != null && !Active.capturing && AllGameManagers.Instance?.GetSaveManager()?.PreviewMode == false && !PreviewRngIsolation.Active)
                Active.InvalidateState(preserveContinuation, reason);
        }
        internal static void InstallAdvisorPatches(Harmony harmony)
        {
            foreach (Type patch in new[] { typeof(CardPlayedPatch), typeof(EndTurnPatch), typeof(UndoPatch), typeof(RestartPatch), typeof(SelectionPatch), typeof(MutationPatch) })
                harmony.CreateClassProcessor(patch).Patch();
        }
        [HarmonyPatch(typeof(CardManager), nameof(CardManager.PlayAnyCard))]
        private static class CardPlayedPatch { private static void Prefix() => NativeAction(reason: "card-play"); }
        [HarmonyPatch(typeof(BattleHud), "EndTurn")]
        private static class EndTurnPatch { private static void Prefix() => NativeAction(reason: "end-turn"); }
        [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.UndoTurn))]
        private static class UndoPatch { private static void Prefix() { NativeAction(false, "undo"); if (Active != null) Active.manualBattleKey = null; } }
        [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.RestartBattle))]
        private static class RestartPatch { private static void Prefix() { NativeAction(false, "restart"); if (Active != null) Active.manualBattleKey = null; } }
        [HarmonyPatch]
        private static class MutationPatch
        {
            // Real changes outside PlayAnyCard (including other mods) must wake
            // the observer too. Native preview/capture copies are excluded.
            private static IEnumerable<MethodBase> TargetMethods()
            {
                foreach (MethodInfo method in AccessTools.GetDeclaredMethods(typeof(CardState)))
                    if (method.Name is "ApplyPermanentUpgrade" or "ApplyTemporaryUpgrade" or "RemoveUpgrade") yield return method;
                foreach (MethodInfo method in AccessTools.GetDeclaredMethods(typeof(CharacterState)))
                    if (method.Name is "SetHealth" or "SetAttackDamage" or "AddStatusEffect" or "RemoveStatusEffect" or
                        "ApplyCardUpgrade" or "RemoveCardUpgrade" or "ApplyHeal" or "ApplyDamage") yield return method;
            }
            private static void Postfix(object __instance)
            {
                if (FullBattleTrace.Active?.KnowsNativeObject(__instance) == true) NativeAction();
            }
        }
        [HarmonyPatch(typeof(RoomManager), nameof(RoomManager.RoomSelectionChanged))]
        private static class SelectionPatch
        {
            private static void Prefix()
            {
                if (Active != null && AllGameManagers.Instance?.GetSaveManager()?.PreviewMode == false && !PreviewRngIsolation.Active)
                    Active.MarkSelection();
            }
        }

        private void MarkSelection()
        {
            observationRevision++; selectionPending = true; captureNeeded = true;
            // Coalesce rapid wheel events before taking a native snapshot.
            selectionReadyAt = Time.realtimeSinceStartup + .12f;
        }

        public void Dispose()
        {
            if (preparation != null) _ = preparation.ContinueWith(task => { _ = task.Exception; }, TaskScheduler.Default);
            session.Dispose();
            if (canvas != null) UnityEngine.Object.Destroy(canvas);
            if (uiFont != null) UnityEngine.Object.Destroy(uiFont);
            Active = null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using MonsterTrain2Poju.Fixtures;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MonsterTrain2Poju.Probe
{
    // Isolated driver only: observe the separately loaded, unmodified player
    // assembly. Never substitute the development overlay or its search result.
    internal static class PlayerAdvisorProbe
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_PLAYER_ADVISOR") == "1";
        private static object? advisor;
        private static Type? type;
        private static int phase, boundaryFrame, captureCount, publicationCount;
        private static long initialGeneration;
        private static ManualLogSource? log;
        private static BattleTurnState? terminal;
        private static bool frozenObservation;
        private static long frozenSearches;
        private static bool BrowseEnabled => Environment.GetEnvironmentVariable("MT2_PROBE_ADVISOR_FLOOR_BROWSE") == "1";
        private static int browseSwitches;
        private static long browseGeneration, browseSearches;
        private static float browseStarted;
        private static int browseCaptures, browsePublications;
        private static BattleAdvice? browseAdvice;
        private static string? browseText, browseStatus;
        private static Color browseColor;
        private static float browseScroll;
        private static int captureFreeSwitches;
        private static int conduitSwitches;
        private static bool conduitBrowsePassed;
        private static bool browsingDuringSearch;
        private static int browseDirection = 1;
        private static bool replayWheelInput;
        private static readonly List<double> browseMilliseconds = new List<double>();
        private static int replayBrowseCount = -1, replayBrowseSwitches;
        internal static bool FloorBrowsePassed { get; private set; }
        private static float idleUntil;
        private static int idleCaptures, idlePublications;
        private static long idleSearches;
        internal static bool IdlePassed { get; private set; }
        internal static bool PlanPreviewPassed { get; private set; }
        internal static bool InteractionsPassed { get; private set; }
        internal static bool CaptureInitialized
        {
            get
            {
                Resolve();
                // Let the real overlay establish its per-battle identity space
                // before a fast frozen driver can issue the first native input.
                SaveManager save = AllGameManagers.Instance.GetSaveManager();
                return Field<bool>("wasInBattle") && Field<string?>("manualBattleKey") ==
                    save.GetShareCode() + ":" + save.GetCurrentDistance();
            }
        }
        internal static bool IsPlanning => Get<bool>("IsPlanning");
        internal static string WaitState
        {
            get
            {
                Resolve();
                Type traceType = type!.Assembly.GetType("MonsterTrain2Poju.Probe.FullBattleTrace", true)!;
                object trace = AccessTools.Property(traceType, "Active").GetValue(null)!;
                var units = (IReadOnlyList<CharacterState>)AccessTools.Property(traceType, "KnownUnits").GetValue(trace)!;
                return " playerStatus=" + Field<BattleAdviceSession>("session").Status + " paused=" + Field<bool>("paused") +
                    " captureError=" + Field<string?>("captureError") + " retainedDestroyed=" +
                    string.Join(",", units.Where(unit => unit != null && unit.IsDestroyed).Select(unit =>
                        unit.name + ":preview=" + unit.SpawnedInPreviewMode));
            }
        }
        internal static BattleAdvice Advice => Get<BattleAdvice>("CurrentAdvice");
        internal static BattleAdvisorOverlay.Publication LatestTiming =>
            JArray.FromObject(Get<object>("Publications")).Last!.ToObject<BattleAdvisorOverlay.Publication>()!;
        internal static object? Evidence => !Enabled ? null : new
        {
            Assembly = type?.Assembly.GetName().Name,
            Version = type?.Assembly.GetName().Version?.ToString(),
            InteractionsPassed,
            IdlePassed, PlanPreviewPassed,
            Presentation = AdvisorPresentationProbe.Enabled ? AdvisorPresentationProbe.Evidence : null,
            FloorBrowse = !BrowseEnabled ? null : new { Passed = FloorBrowsePassed, Switches = browseSwitches,
                DuringSearch = browsingDuringSearch, WheelInput = true, CaptureFreeSwitches = captureFreeSwitches,
                DeployedConduitPassed = conduitBrowsePassed, DeployedConduitSwitches = conduitSwitches,
                SwitchMilliseconds = browseMilliseconds },
            ReplayedFloorSwitches = replayBrowseSwitches,
            FrozenObservation = !frozenObservation ? null : new { Paused = Field<bool>("paused"),
                SearchesAtPause = frozenSearches, SearchesAfterPause = Get<long>("Searches") - frozenSearches,
                FinalAdviceAbsent = CurrentIsNull() },
            EnchantmentBindings = advisor == null ? null : Bindings(),
            Overlay = advisor == null ? null : Property("Evidence")
        };

        private static object[] Bindings()
        {
            Type traceType = type!.Assembly.GetType("MonsterTrain2Poju.Probe.FullBattleTrace", true)!;
            object trace = AccessTools.Property(traceType, "Active").GetValue(null)!;
            var units = (IReadOnlyList<CharacterState>)AccessTools.Property(traceType, "KnownUnits").GetValue(trace)!;
            int Id(CharacterState value) => (int)AccessTools.Method(traceType, "UnitId").Invoke(trace, new object[] { value });
            var result = new List<object>();
            foreach (CharacterState owner in units)
                foreach (CharacterTriggerState trigger in (IEnumerable<CharacterTriggerState>)AccessTools.Field(typeof(CharacterState), "triggers").GetValue(owner))
                    foreach (CardEffectState state in trigger.GetEffects())
                    {
                        if (state.GetCardEffect() is not CardEffectEnchant effect) continue;
                        var source = (CharacterState?)AccessTools.Field(typeof(CardEffectEnchant), "enchanterCharacter").GetValue(effect);
                        result.Add(new { Owner = Id(owner), OwnerUnityAlive = owner != null, OwnerDestroyed = owner.IsDestroyed,
                            OwnerPreviewBorn = owner.SpawnedInPreviewMode,
                            Source = ReferenceEquals(source, null) ? 0 : Id(source!), SourceUnityAlive = source != null,
                            SourceDestroyed = ReferenceEquals(source, null) ? (bool?)null : source!.IsDestroyed,
                            SameOwner = ReferenceEquals(owner, source) });
                    }
            return result.ToArray();
        }

        private static object? Property(string name) => AccessTools.Property(type!, name).GetValue(advisor);
        private static T Get<T>(string name) { Resolve(); return (T)Property(name)!; }
        private static T Field<T>(string name) => (T)AccessTools.Field(type!, name).GetValue(advisor);
        private static object? Call(string name, params object[] args) => AccessTools.Method(type!, name).Invoke(advisor, args);
        private static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException("Player advisor acceptance: " + message); }
        private static void Resolve()
        {
            if (advisor != null) return;
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().Single(item => item.GetName().Name == "MonsterTrain2Poju.Advisor");
            type = assembly.GetType("MonsterTrain2Poju.Probe.BattleAdvisorOverlay", throwOnError: true)!;
            advisor = AccessTools.Property(type, "Active").GetValue(null);
            Require(advisor != null, "the real player entry point has not created an overlay");
            Require(BattleAdvisorOverlay.Active == null, "a development overlay is also active");
            log = BepInEx.Logging.Logger.CreateLogSource("PlayerAdvisorAcceptance");
            AdvisorCaptureProfileProbe.Install(assembly, log);
            if (AdvisorPresentationProbe.Enabled) AdvisorPresentationProbe.Install(type!, advisor!, log);
        }

        internal static BattleTurnState Capture()
        {
            Resolve();
            return (BattleTurnState)Call("CaptureReadOnly", AllGameManagers.Instance)!;
        }

        internal static void CaptureTerminal()
        { if (Enabled) terminal = Capture(); }
        internal static BattleTurnState Terminal => terminal ?? throw new InvalidOperationException("Player terminal capture is missing.");

        internal static void PauseForFrozenPlan()
        {
            if (frozenObservation) return;
            Resolve();
            if (!Field<bool>("paused")) Click("暂停建议");
            Require(Field<bool>("paused") && CurrentIsNull(), "frozen plan retained background advice");
            frozenObservation = true;
            frozenSearches = Get<long>("Searches");
            log!.LogInfo("PLAYER-ADVISOR-FROZEN-OBSERVATION pausedBackground=true; native observation remains active");
        }

        internal static PlayCardAction? NativeAction(PlayCardAction? action)
        {
            if (action == null) return null;
            Resolve();
            Type traceType = type!.Assembly.GetType("MonsterTrain2Poju.Probe.FullBattleTrace", true)!;
            object trace = AccessTools.Property(traceType, "Active").GetValue(null)!;
            int Card(int id)
            {
                if (id == 0) return 0;
                var cards = (IReadOnlyList<CardState>)AccessTools.Property(traceType, "KnownCards").GetValue(trace)!;
                CardState card = cards.Single(item => (int)AccessTools.Method(traceType, "CardId").Invoke(trace, new object[] { item }) == id);
                return FullBattleTrace.Active!.CardId(card);
            }
            int Unit(int id)
            {
                if (id == 0) return 0;
                var units = (IReadOnlyList<CharacterState>)AccessTools.Property(traceType, "KnownUnits").GetValue(trace)!;
                CharacterState unit = units.Single(item => (int)AccessTools.Method(traceType, "UnitId").Invoke(trace, new object[] { item }) == id);
                return FullBattleTrace.Active!.UnitId(unit);
            }
            Require(action.RecursionSelections.Count == 0 && action.MixSelections.Count == 0,
                "this focused driver does not translate choice-card identities");
            return new PlayCardAction(Card(action.CardInstanceId), action.RoomIndex, action.PlayerPosition,
                Unit(action.TargetUnitId), Unit(action.ActivatorUnitId), action.TargetSpawnPointIndex,
                activatorRoomIndex: action.ActivatorRoomIndex);
        }

        internal static bool ReadyFor()
        {
            Resolve();
            Require(Field<string?>("captureError") == null, "player capture failed: " + Field<string?>("captureError"));
            Require(Get<int>("Failures") == 0 && Get<int>("PurityFailures") == 0, "player capture or overlay failed");
            if (phase == 0)
            {
                if (!Ready()) return false;
                initialGeneration = GetGeneration();
                Call("CapturePanel");
                PreviewPlan();
                Drag();
                Click("暂停建议");
                Require(Field<bool>("paused") && CurrentIsNull(), "pause retained a recommendation");
                Hold(1); return false;
            }
            if (Time.frameCount <= boundaryFrame && phase is 1 or 3) return false;
            if (phase == 1)
            {
                AssertHeld();
                Click("隐藏破局建议"); Click("隐藏破局建议");
                Require(Field<bool>("paused") && Field<bool>("panelVisible"), "showing cleared manual pause");
                Click("暂停建议");
                Require(!Field<bool>("paused") && CurrentIsNull(), "resume reused a recommendation");
                phase = 2; return false;
            }
            if (phase == 2)
            {
                if (!IsPlanning) return false;
                Click("隐藏破局建议");
                Require(!Field<GameObject>("panel").activeSelf && CurrentIsNull(), "hide retained visible advice");
                Hold(3); return false;
            }
            if (phase == 3)
            {
                AssertHeld(); Click("隐藏破局建议");
                Require(Field<GameObject>("panel").activeSelf && CurrentIsNull(), "show reused hidden advice");
                phase = 4; return false;
            }
            if (phase == 4)
            {
                if (!Ready()) return false;
                Require(GetGeneration() > initialGeneration, "cancelled generation was republished");
                idleUntil = Time.realtimeSinceStartup + 2;
                idleCaptures = Get<int>("Captures"); idlePublications = Get<int>("Published"); idleSearches = Get<long>("Searches");
                phase = 40; return false;
            }
            if (phase == 40)
            {
                Require(Get<int>("Captures") == idleCaptures && Get<int>("Published") == idlePublications &&
                    Get<long>("Searches") == idleSearches && !CurrentIsNull(), "idle advice repeated capture, publication or search");
                if (Time.realtimeSinceStartup < idleUntil) return false;
                Require(Ready(), "idle advice became stale");
                IdlePassed = true;
                log!.LogInfo("PLAYER-ADVISOR-IDLE-PASS seconds=2 captures=0 publications=0 searches=0");
                browseSearches = Get<long>("Searches");
                initialGeneration = GetGeneration(); Click("重新计算");
                Require(CurrentIsNull(), "refresh retained a recommendation");
                phase = 5; return false;
            }
            if (BrowseEnabled && phase == 5 && !browsingDuringSearch)
            {
                if (!IsPlanning || Get<long>("Searches") <= browseSearches) return false;
                browseGeneration = GetGeneration(); browseSearches = Get<long>("Searches");
                BrowseNext(); browsingDuringSearch = true;
                phase = 6; return false;
            }
            if (BrowseEnabled && phase is 6 or 7)
            {
                CheckBrowseView();
                if (Time.frameCount <= boundaryFrame || Field<bool>("selectionPending") || !SettledBrowse()) return false;
                captureFreeSwitches++;
                Require(GetGeneration() == browseGeneration && Get<long>("Searches") == browseSearches,
                    "browsing restarted or cancelled a search");
                browseMilliseconds.Add((Time.realtimeSinceStartup - browseStarted) * 1000d);
                if (browseSwitches < 7) { BrowseNext(); phase = 7; return false; }
                FloorBrowsePassed = true;
                log!.LogInfo("PLAYER-ADVISOR-FLOOR-BROWSE-PASS switches=" + browseSwitches +
                    " duringSearch=true newSearches=0 generation=" + browseGeneration +
                    " captureFreeSwitches=" + captureFreeSwitches +
                    " readySwitchMs=" + string.Join(",", browseMilliseconds.Skip(1).Select(value => value.ToString("F1"))));
                phase = 8;
            }
            if (BrowseEnabled && phase == 70)
            {
                CheckBrowseView();
                if (Time.frameCount <= boundaryFrame || Field<bool>("selectionPending") || !SettledBrowse()) return false;
                Require(GetGeneration() == browseGeneration && Get<long>("Searches") == browseSearches,
                    "deployed Conduit browsing restarted or cancelled a search");
                if (conduitSwitches < 4) { BrowseNext(false); return false; }
                conduitBrowsePassed = true; phase = 8;
                log!.LogInfo("PLAYER-ADVISOR-CONDUIT-BROWSE-PASS switches=4 captures=0 publications=0 searches=0 stableView=true fullNativeRng=true");
            }
            if (!Ready()) return false;
            if (BrowseEnabled && phase == 8 && !conduitBrowsePassed && RollingAdviceScenario.Records.Count > 0)
            {
                Type traceType = type!.Assembly.GetType("MonsterTrain2Poju.Probe.FullBattleTrace", true)!;
                object trace = AccessTools.Property(traceType, "Active").GetValue(null)!;
                var units = (IReadOnlyList<CharacterState>)AccessTools.Property(traceType, "KnownUnits").GetValue(trace)!;
                if (units.Any(unit => unit != null && !unit.IsDestroyed && unit.IsAlive && unit.GetStatusEffectStacks("conduit") > 0))
                {
                    browseGeneration = GetGeneration(); browseSearches = Get<long>("Searches");
                    BrowseNext(false); phase = 70; return false;
                }
            }
            if (!InteractionsPassed)
            {
                Require(GetGeneration() > initialGeneration, "refresh reused the preceding generation");
                InteractionsPassed = true;
                log!.LogInfo("PLAYER-ADVISOR-UI-PASS pause/resume hide/cancel/show refresh pointer-raycast drag/clamp generation=" + GetGeneration());
                if (AdvisorPresentationProbe.Enabled) AdvisorPresentationProbe.ExpandAndScroll();
            }
            return true;
        }

        private static void CheckBrowseView()
        {
            Require(Get<int>("Captures") == browseCaptures, "floor browsing triggered native state capture");
            if (browseAdvice != null)
                Require(ReferenceEquals(Advice, browseAdvice) && Get<int>("Published") == browsePublications &&
                    Field<Text>("body").text == browseText && Field<Text>("statusLabel").text == browseStatus &&
                    Field<Text>("body").color == browseColor &&
                    Math.Abs(Field<ScrollRect>("planScroll").verticalNormalizedPosition - browseScroll) < .005,
                    "floor browsing changed the publication, text, status, color or scroll position");
        }

        private static void BrowseNext(bool initial = true)
        {
            browseStarted = Time.realtimeSinceStartup; boundaryFrame = Time.frameCount + 2;
            browseCaptures = Get<int>("Captures"); browsePublications = Get<int>("Published");
            browseAdvice = CurrentIsNull() ? null : Advice;
            browseText = Field<Text>("body").text; browseStatus = Field<Text>("statusLabel").text;
            browseColor = Field<Text>("body").color;
            browseScroll = Field<ScrollRect>("planScroll").verticalNormalizedPosition;
            ScrollNext();
            if (initial) browseSwitches++; else conduitSwitches++;
        }

        private static void ScrollNext()
        {
            RoomManager rooms = AllGameManagers.Instance.GetRoomManager();
            int before = rooms.GetSelectedRoom();
            if (before == 0) browseDirection = 1;
            else if (before == 2) browseDirection = -1;
            AccessTools.Method(typeof(RoomUI), "HandleScrollWheel").Invoke(rooms.GetRoomUI(),
                new object[] { browseDirection * 100f });
            Require(rooms.GetSelectedRoom() == before + browseDirection, "native wheel input did not select the adjacent floor");
        }

        private static bool SettledBrowse()
        {
            if (CurrentIsNull() || Time.frameCount <= Field<int>("displayFrame") + 1 ||
                Field<bool>("captureNeeded") || Field<object?>("preparation") != null) return false;
            BattleTurnState root = Capture();
            Require((bool)Call("ReadyFor", root)!, "deferred floor view differs from a fresh complete native capture/RNG");
            return true;
        }

        internal static bool ReplayFloorBrowsing()
        {
            if (replayBrowseCount < 0)
            {
                using var fixture = FixtureDocument.Read(Environment.GetEnvironmentVariable("MT2_PROBE_ROLLING_INPUT")!);
                replayBrowseCount = 0;
                if (fixture.RootElement.GetProperty("PlayerAdvisor").TryGetProperty("FloorBrowse", out FixtureValue browse) &&
                    browse.ValueKind != FixtureKind.Null && browse.GetProperty("Passed").GetBoolean())
                {
                    replayBrowseCount = browse.GetProperty("Switches").GetInt32();
                    replayWheelInput = browse.TryGetProperty("WheelInput", out FixtureValue wheel) && wheel.GetBoolean();
                }
                Require(replayBrowseCount >= 0 && replayBrowseCount <= 30, "invalid frozen browsing count");
            }
            PauseForFrozenPlan();
            if (replayBrowseCount == 0) return false;
            if (Time.frameCount <= boundaryFrame) return true;
            if (replayBrowseSwitches < replayBrowseCount)
            {
                if (replayWheelInput) ScrollNext();
                else
                {
                    RoomManager rooms = AllGameManagers.Instance.GetRoomManager();
                    AccessTools.Method(typeof(RoomUI), "InnerSetSelectedRoom").Invoke(rooms.GetRoomUI(),
                        new object[] { (rooms.GetSelectedRoom() + 1) % 3, true });
                }
                replayBrowseSwitches++; boundaryFrame = Time.frameCount + 2;
                return true;
            }
            log!.LogInfo("PLAYER-ADVISOR-FLOOR-BROWSE-REPLAY-PASS switches=" + replayBrowseSwitches);
            replayBrowseCount = 0;
            return false;
        }

        private static bool Ready()
        {
            BattleAdviceSession session = Field<BattleAdviceSession>("session");
            Require(session.Status != BattleAdviceStatus.Unavailable, "player advice unavailable: " + session.Error);
            if (CurrentIsNull()) return false;
            // Both assemblies allocate their own capture identities. Compare
            // the player publication with a fresh native capture in its own
            // identity space; translate chosen inputs by native object identity.
            BattleTurnState root = Capture();
            using var keys = new BattleStateKeys();
            Require(session.MatchesCurrentRoot(root), "player advice is stale: " +
                UnitPlayModelProbe.FirstDifference(BattleTurnProbe.Comparable(Advice.Root), BattleTurnProbe.Comparable(root), "$"));
            return (bool)Call("ReadyFor", root)!;
        }
        private static bool CurrentIsNull() => Field<BattleAdviceSession>("session").Current == null;
        private static long GetGeneration() => Field<BattleAdviceSession>("session").Generation;
        private static void Hold(int nextPhase)
        {
            phase = nextPhase; boundaryFrame = Time.frameCount + 8;
            captureCount = Get<int>("Captures"); publicationCount = Get<int>("Published");
        }
        private static void AssertHeld()
        {
            Require(captureCount == Get<int>("Captures") && publicationCount == Get<int>("Published") && CurrentIsNull(),
                "paused/hidden overlay captured or published");
        }
        private static void Click(string objectName)
        {
            Canvas.ForceUpdateCanvases();
            GameObject canvas = Field<GameObject>("canvas");
            Button button = canvas.GetComponentsInChildren<Button>(true).Single(item => item.name == objectName);
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, button.transform.TransformPoint(((RectTransform)button.transform).rect.center)) };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Require(hits.Count > 0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) == button.gameObject,
                "button is blocked from pointer input: " + objectName);
            Require(ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler), "button has no click handler");
        }
        private static void PreviewPlan()
        {
            int captures = Get<int>("Captures"); long searches = Get<long>("Searches");
            BattleAdvice advice = Advice;
            Click("完整方案"); Canvas.ForceUpdateCanvases();
            Text text = Field<Text>("body");
            Require(Field<bool>("showFullPlan") && text.text.Contains(AdvisorForecastPresentation.IsTerminal(advice.Plan.Outcome) ? "完整方案" : "部分方案") &&
                text.text.Contains(Advice.Plan.Steps.Count + ". "), "complete plan omitted its final step");
            ScrollRect scroll = Field<ScrollRect>("planScroll");
            if (scroll.content.rect.height > scroll.viewport.rect.height)
            {
                scroll.OnScroll(new PointerEventData(EventSystem.current) { scrollDelta = new Vector2(0, -10) });
                Require(scroll.verticalNormalizedPosition < 1, "long plan did not scroll");
            }
            Call("CapturePanel");
            Click("完整方案");
            int currentTurnSteps = advice.Plan.Steps.TakeWhile(step => !step.IsEndTurn).Count();
            if (currentTurnSteps < advice.Plan.Steps.Count) currentTurnSteps++;
            Require(!Field<bool>("showFullPlan") && text.text.Contains("本回合步骤") &&
                text.text.Contains("\n" + currentTurnSteps + ". ") &&
                !text.text.Contains("\n" + (currentTurnSteps + 1) + ". ") && !text.text.Contains("后续第") &&
                ReferenceEquals(Advice, advice) &&
                Get<int>("Captures") == captures && Get<long>("Searches") == searches,
                "collapsed plan omitted current-turn steps, included a later turn, or changed planning");
            PlanPreviewPassed = true;
            log!.LogInfo("PLAYER-ADVISOR-PLAN-PASS steps=" + Advice.Plan.Steps.Count +
                " currentTurnSteps=" + currentTurnSteps + " captures=0 searches=0");
        }
        private static void Drag()
        {
            GameObject panel = Field<GameObject>("panel"); RectTransform rect = panel.GetComponent<RectTransform>();
            Vector2 before = rect.anchoredPosition;
            GameObject title = panel.transform.Find("Title").gameObject;
            var pointer = new PointerEventData(EventSystem.current) { delta = new Vector2(100000, -100000) };
            Require(ExecuteEvents.Execute(title, pointer, ExecuteEvents.dragHandler), "title has no drag handler");
            Rect bounds = ((RectTransform)rect.parent).rect;
            Require(rect.anchoredPosition != before && Mathf.Approximately(rect.anchoredPosition.x, Mathf.Max(0, bounds.width - rect.rect.width)) &&
                Mathf.Approximately(rect.anchoredPosition.y, -Mathf.Max(0, bounds.height - rect.rect.height)), "drag escaped canvas bounds");
            pointer.delta = new Vector2(-100000, 100000); ExecuteEvents.Execute(title, pointer, ExecuteEvents.dragHandler);
            Require(rect.anchoredPosition == Vector2.zero, "reverse drag escaped canvas bounds");
            rect.anchoredPosition = before;
        }
    }
}

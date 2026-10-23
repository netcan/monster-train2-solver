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
        private static bool BrowseEnabled => Environment.GetEnvironmentVariable("MT2_PROBE_ADVISOR_FLOOR_BROWSE") == "1";
        private static int browseSwitches;
        private static long browseGeneration, browseSearches;
        private static float browseStarted;
        private static bool browsingDuringSearch;
        private static readonly List<double> browseMilliseconds = new List<double>();
        private static int replayBrowseCount = -1, replayBrowseSwitches;
        internal static bool FloorBrowsePassed { get; private set; }
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
            FloorBrowse = !BrowseEnabled ? null : new { Passed = FloorBrowsePassed, Switches = browseSwitches,
                DuringSearch = browsingDuringSearch, SwitchMilliseconds = browseMilliseconds },
            ReplayedFloorSwitches = replayBrowseSwitches,
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
                if (Time.frameCount <= boundaryFrame || Field<bool>("selectionPending") || !SettledBrowse()) return false;
                Require(GetGeneration() == browseGeneration && Get<long>("Searches") == browseSearches,
                    "browsing restarted or cancelled a search");
                browseMilliseconds.Add((Time.realtimeSinceStartup - browseStarted) * 1000d);
                if (browseSwitches < 7) { BrowseNext(); phase = 7; return false; }
                FloorBrowsePassed = true;
                log!.LogInfo("PLAYER-ADVISOR-FLOOR-BROWSE-PASS switches=" + browseSwitches +
                    " duringSearch=true newSearches=0 generation=" + browseGeneration +
                    " readySwitchMs=" + string.Join(",", browseMilliseconds.Skip(1).Select(value => value.ToString("F1"))));
                phase = 8;
            }
            if (!Ready()) return false;
            if (!InteractionsPassed)
            {
                Require(GetGeneration() > initialGeneration, "refresh reused the preceding generation");
                InteractionsPassed = true;
                log!.LogInfo("PLAYER-ADVISOR-UI-PASS pause/resume hide/cancel/show refresh pointer-raycast drag/clamp generation=" + GetGeneration());
            }
            return true;
        }

        private static void BrowseNext()
        {
            RoomManager rooms = AllGameManagers.Instance.GetRoomManager();
            int next = (rooms.GetSelectedRoom() + 1) % 3;
            browseStarted = Time.realtimeSinceStartup; boundaryFrame = Time.frameCount + 2;
            AccessTools.Method(typeof(RoomUI), "InnerSetSelectedRoom").Invoke(rooms.GetRoomUI(), new object[] { next, true });
            browseSwitches++;
        }

        private static bool SettledBrowse()
        {
            if (CurrentIsNull()) return false;
            BattleTurnState root = Capture();
            using var keys = new BattleStateKeys();
            if (Advice.RootKey != keys.Key(root)) return false;
            return (bool)Call("ReadyFor", root)!;
        }

        internal static bool ReplayFloorBrowsing()
        {
            if (replayBrowseCount < 0)
            {
                using var fixture = FixtureDocument.Read(Environment.GetEnvironmentVariable("MT2_PROBE_ROLLING_INPUT")!);
                replayBrowseCount = 0;
                if (fixture.RootElement.GetProperty("PlayerAdvisor").TryGetProperty("FloorBrowse", out FixtureValue browse) &&
                    browse.ValueKind != FixtureKind.Null && browse.GetProperty("Passed").GetBoolean())
                    replayBrowseCount = browse.GetProperty("Switches").GetInt32();
                Require(replayBrowseCount >= 0 && replayBrowseCount <= 30, "invalid frozen browsing count");
            }
            if (replayBrowseCount == 0) return false;
            PauseForFrozenPlan();
            if (Time.frameCount <= boundaryFrame) return true;
            if (replayBrowseSwitches < replayBrowseCount)
            {
                RoomManager rooms = AllGameManagers.Instance.GetRoomManager();
                AccessTools.Method(typeof(RoomUI), "InnerSetSelectedRoom").Invoke(rooms.GetRoomUI(),
                    new object[] { (rooms.GetSelectedRoom() + 1) % 3, true });
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
            Require(Advice.RootKey == keys.Key(root), "player advice is stale: " +
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

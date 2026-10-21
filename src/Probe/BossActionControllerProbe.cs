using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class BossActionControllerProbe
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_BOSS_ACTION_CONTROLLERS") == "1";
        private static readonly Dictionary<BossActionController, string> Owners = new Dictionary<BossActionController, string>();
        private static readonly List<Record> Records = new List<Record>();
        private static readonly List<string> Errors = new List<string>();
        internal sealed class Record
        {
            public string Boss { get; set; } = "";
            public string Operation { get; set; } = "";
            public bool DestroyRound { get; set; }
            public BossActionControllerProgress Before { get; set; } = null!;
            public BossActionControllerProgress? Actual { get; set; }
            public BossActionReference? NativeNext { get; set; }
            public string? Difference { get; set; }
        }
        private static T Field<T>(object owner, string name) => (T)AccessTools.Field(owner.GetType(), name).GetValue(owner);
        private static void Set(object owner, string name, object? value) => AccessTools.Field(owner.GetType(), name).SetValue(owner, value);
        private static BossActionControllerProgress Snapshot(BossActionController controller, RoomManager rooms)
        {
            List<ActionGroupState> groups = Field<List<ActionGroupState>>(controller, "_actionGroups");
            ActionGroupState destroy = Field<ActionGroupState>(controller, "_roomDestroyActionGroup");
            ActionGroupState? current = Field<ActionGroupState?>(controller, "_currentActionGroup");
            BossActionState? initial = Field<BossActionState?>(controller, "_initialAction");
            BossActionReference? Reference(BossActionState? action)
            {
                if (action == null) return null;
                for (int index = 0; index <= groups.Count; index++)
                {
                    int found = (index == groups.Count ? destroy : groups[index]).GetActions().IndexOf(action);
                    if (found >= 0) return new BossActionReference(index, found);
                }
                throw new InvalidOperationException("Native boss action identity is outside its controller.");
            }
            BossActionGroupProgress Group(ActionGroupState group) => new BossActionGroupProgress(group.GetNumRepeats(),
                Field<int>(group, "_currentActionIndex"), group.GetNextAction() == null ? null : (int?)group.GetActions().IndexOf(group.GetNextAction()),
                group.GetActions().Select(action => action.CanExecute(rooms)).ToArray(), group is RoomDestroyActionGroup);
            return new BossActionControllerProgress(groups.Select(Group).ToArray(), Group(destroy),
                Field<int>(controller, "_currentGroupIndex"), current == null ? null : current == destroy ? groups.Count : groups.IndexOf(current), Reference(initial));
        }
        private static BossActionReference? NativeNext(BossActionController controller)
        {
            List<ActionGroupState> groups = Field<List<ActionGroupState>>(controller, "_actionGroups");
            ActionGroupState destroy = Field<ActionGroupState>(controller, "_roomDestroyActionGroup");
            if (groups.Count > 0 && Field<BossActionState?>(controller, "_initialAction") == null &&
                Field<ActionGroupState?>(controller, "_currentActionGroup") == null) return null;
            BossActionState? action = controller.GetNextBossAction();
            if (action == null) return null;
            for (int index = 0; index <= groups.Count; index++)
            {
                int found = (index == groups.Count ? destroy : groups[index]).GetActions().IndexOf(action);
                if (found >= 0) return new BossActionReference(index, found);
            }
            throw new InvalidOperationException("Unknown native next boss action reference.");
        }
        private static Record? Begin(BossActionController controller, string operation, bool destroyRound)
        {
            if (!Enabled || !Owners.TryGetValue(controller, out string boss) || AllGameManagers.Instance.GetSaveManager().PreviewMode) return null;
            try
            {
                var row = new Record { Boss = boss, Operation = operation, DestroyRound = destroyRound,
                    Before = Snapshot(controller, AllGameManagers.Instance.GetRoomManager()) };
                Records.Add(row); return row;
            }
            catch (Exception error) { Errors.Add(error.ToString()); return null; }
        }
        private static void Complete(Record? row, BossActionController controller, RoomManager rooms)
        {
            if (row == null) return;
            try
            {
                row.Actual = Snapshot(controller, rooms); row.NativeNext = NativeNext(controller);
                BossActionControllerResult predicted = BossActionControllerModel.Apply(row.Before, row.Operation, row.DestroyRound);
                row.Difference = predicted.Supported && JToken.DeepEquals(JToken.FromObject(predicted.State!), JToken.FromObject(row.Actual)) &&
                    JToken.DeepEquals(JToken.FromObject(new { Next = predicted.State!.NextAction }), JToken.FromObject(new { Next = row.NativeNext }))
                    ? null : predicted.UnsupportedReason ?? "Native boss controller cursor or next-action reference differs.";
            }
            catch (Exception error) { Errors.Add(error.ToString()); }
        }
        private sealed class ControlBehavior : IBossActionBehavior
        {
            private readonly bool executable;
            internal ControlBehavior(bool executable) { this.executable = executable; }
            public void Setup(BossActionData data) { }
            public bool CanExecute(RoomManager rooms) => executable;
            public int SelectRoom(RoomManager rooms, List<int> visits, bool firstSelection, int currentRoomIndex) => 0;
        }
        internal static void Write(FullBattleTrace trace, ManualLogSource log)
        {
            if (!Enabled) return;
            RoomManager rooms = AllGameManagers.Instance.GetRoomManager();
            JObject before = JObject.FromObject(trace.CaptureDecision()); JObject rngBefore = Rng();
            int frame = UnityEngine.Time.frameCount;
            var controls = new List<Record>();
            for (int count = 0; count <= 3; count++)
            for (int shape = 0; shape < 12; shape++)
            {
                var controller = new BossActionController(new List<ActionGroupData>());
                var groups = new List<ActionGroupState>();
                for (int index = 0; index < count; index++)
                {
                    int size = (shape + index) % 3 + 1, mask = (shape * 5 + index * 3) % (1 << size);
                    var group = new ActionGroupState(); var actions = new List<BossActionState>();
                    for (int action = 0; action < size; action++)
                    {
                        var native = new BossActionState(); Set(native, "_actionBehavior", new ControlBehavior((mask & (1 << action)) != 0));
                        actions.Add(native);
                    }
                    Set(group, "_numRepeats", (shape + index) % 3 - 1); Set(group, "_actions", actions);
                    Set(group, "_currentActionIndex", shape % size); groups.Add(group);
                }
                Set(controller, "_actionGroups", groups); Set(controller, "_currentGroupIndex", shape % Math.Max(1, count));
                for (int step = 0; step < 24; step++)
                {
                    BossActionControllerProgress input = Snapshot(controller, rooms);
                    bool canPerform = input.CurrentGroup.HasValue && (input.CurrentGroup == count ? input.DestroyGroup : input.Groups[input.CurrentGroup.Value]).CurrentAction.HasValue;
                    string operation = step == 0 ? "DecideInitial" : step % 3 == 2 && canPerform ? "Perform" : "DecideNext";
                    bool destroyRound = step % 7 == 0 && step > 0;
                    var row = new Record { Boss = "detached-controller", Before = input, Operation = operation, DestroyRound = destroyRound };
                    if (operation == "DecideInitial") controller.DecideInitialAction(rooms);
                    else if (operation == "Perform") controller.PerformCurrentAction();
                    else controller.DecideNextAction(rooms, null!, destroyRound);
                    Complete(row, controller, rooms); controls.Add(row);
                }
            }
            JObject after = JObject.FromObject(trace.CaptureDecision()); JObject rngAfter = Rng();
            bool unchanged = JToken.DeepEquals(before, after) && JToken.DeepEquals(rngBefore, rngAfter) && frame == UnityEngine.Time.frameCount;
            CharacterData boss = AllGameManagers.Instance.GetHeroManager().GetOuterTrainBossCharacter().GetSourceCharacterData();
            object definition = new { Boss = boss.name, Groups = boss.GetBossActionData().Select(group => new { Repeats = group.GetNumRepeats(),
                Actions = group.GetActions().Select(action => new { Behavior = action.GetActionBehaviorName(), Team = action.GetParamTeamType().ToString(),
                    Effects = action.GetActionEffectData().Select(RuleCatalogProbe.Effect).ToArray() }).ToArray() }).ToArray() };
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                DecisionUnchanged = unchanged, FrameBefore = frame, FrameAfter = UnityEngine.Time.frameCount,
                RngBefore = rngBefore, RngAfter = rngAfter, Definition = definition, Actions = Records, Controls = controls, Errors }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "boss-action-controller-calibration.mt2f"))) document.Write(output);
            int differences = Records.Concat(controls).Count(row => row.Actual == null || row.Difference != null);
            log.LogInfo("BOSS-CONTROLLER-CALIBRATION actions=" + Records.Count + " controls=" + controls.Count + " differences=" + differences + " unchanged=" + unchanged);
            if (!unchanged || differences != 0 || Errors.Count != 0 || Records.Count == 0) throw new InvalidOperationException("Native boss controller calibration failed.");
        }
        private static JObject Rng()
        {
            var result = new JObject();
            foreach (RngId id in Enum.GetValues(typeof(RngId)))
                if (id != RngId.NonDeterministic && id != RngId.Chatter) result[id.ToString()] = JToken.FromObject(RngCalibration.Words(RandomManager.GetState(id)));
            result["UnityGlobal"] = JToken.FromObject(RngCalibration.Words(UnityEngine.Random.state)); return result;
        }
        [HarmonyPatch(typeof(BossState), nameof(BossState.DecideInitialAction))]
        private static class RegisterPatch
        {
            private static void Prefix(BossState __instance)
            {
                if (!Enabled || AllGameManagers.Instance.GetSaveManager().PreviewMode) return;
                CharacterState boss = Field<CharacterState>(__instance, "_characterState");
                if (boss.IsOuterTrainBoss()) Owners[Field<BossActionController>(__instance, "_bossActionController")] = boss.GetSourceCharacterData().name;
            }
        }
        [HarmonyPatch(typeof(BossActionController), nameof(BossActionController.DecideInitialAction))]
        private static class InitialPatch
        {
            private static void Prefix(BossActionController __instance, out Record? __state) { __state = Begin(__instance, "DecideInitial", false); }
            private static void Postfix(BossActionController __instance, RoomManager roomManager, Record? __state) { Complete(__state, __instance, roomManager); }
        }
        [HarmonyPatch(typeof(BossActionController), nameof(BossActionController.DecideNextAction))]
        private static class DecidePatch
        {
            private static void Prefix(BossActionController __instance, bool destroyRound, out Record? __state) { __state = Begin(__instance, "DecideNext", destroyRound); }
            private static void Postfix(BossActionController __instance, RoomManager roomManager, Record? __state) { Complete(__state, __instance, roomManager); }
        }
        [HarmonyPatch(typeof(BossActionController), nameof(BossActionController.PerformCurrentAction))]
        private static class PerformPatch
        {
            private static void Prefix(BossActionController __instance, out Record? __state) { __state = Begin(__instance, "Perform", false); }
            private static void Postfix(BossActionController __instance, Record? __state) { Complete(__state, __instance, AllGameManagers.Instance.GetRoomManager()); }
        }
    }
}

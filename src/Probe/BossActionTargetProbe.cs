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
    // Observe real setup calls. Synchronous controls use detached action/visit
    // state and RNG; the borrowed boss pointer is restored before the frame ends.
    internal static class BossActionTargetProbe
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_BOSS_ACTION_TARGETS") == "1";
        internal sealed class Record
        {
            public string Boss { get; set; } = "";
            public BossActionTargetState Before { get; set; } = null!;
            public BossActionTargetResult? Actual { get; set; }
            public BossActionTargetResult? Predicted { get; set; }
            public string? Difference { get; set; }
        }
        private static readonly List<Record> Records = new List<Record>();
        private static readonly List<string> Errors = new List<string>();
        private static bool calibrating;
        private static T Field<T>(object owner, string name) => (T)AccessTools.Field(owner.GetType(), name).GetValue(owner);
        private static UnityRng Rng()
        {
            uint[] words = RngCalibration.Words(RandomManager.GetState(RngId.BossActions));
            return new UnityRng(words[0], words[1], words[2], words[3]);
        }
        private static JObject AllRng()
        {
            var result = new JObject();
            foreach (RngId id in Enum.GetValues(typeof(RngId)))
                if (id != RngId.NonDeterministic && id != RngId.Chatter)
                    result[id.ToString()] = JToken.FromObject(RngCalibration.Words(RandomManager.GetState(id)));
            result["UnityGlobal"] = JToken.FromObject(RngCalibration.Words(UnityEngine.Random.state));
            return result;
        }
        private static Record Snapshot(BossActionState action, CharacterState boss, RoomManager rooms, bool initial, int currentRoom)
        {
            object behavior = Field<object>(action, "_actionBehavior");
            Team.Type targetTeam = behavior is RandomOccupiedBossActionBehavior ? Field<Team.Type>(behavior, "_teamType") : Team.Type.Monsters;
            RoomState[] floors = Enumerable.Range(0, rooms.GetNumRooms() - 1).Select(rooms.GetRoom).ToArray();
            return new Record { Boss = boss.GetSourceCharacterData().name,
                Before = new BossActionTargetState(behavior.GetType().Name, floors.Select(room => room.IsRoomEnabled()).ToArray(),
                    floors.Select(room => room.HasCharacters(Team.Type.Monsters, includeOuterTrainBoss: false)).ToArray(),
                    floors.Select(room => room.HasCharacters(targetTeam, includeOuterTrainBoss: false)).ToArray(),
                    boss.GetBossState().GetVisitedRoomRecord(), initial, currentRoom, boss.GetMovementSpeed(),
                    action.GetTargetedRoomIndex(), action.GetPostActionRoomIndex(), Field<bool>(action, "_setupAsInitialAction"), Rng()) };
        }
        private static Record? Begin(BossActionState action, CharacterState boss, RoomManager rooms, bool initial, int currentRoom)
        {
            if (!Enabled || calibrating || !boss.IsOuterTrainBoss() || AllGameManagers.Instance.GetSaveManager().PreviewMode) return null;
            try
            {
                Record row = Snapshot(action, boss, rooms, initial, currentRoom);
                Records.Add(row); return row;
            }
            catch (Exception error) { Errors.Add(error.ToString()); return null; }
        }
        private static void Complete(Record? row, BossActionState action, CharacterState boss)
        {
            if (row == null) return;
            try
            {
                row.Actual = new BossActionTargetResult(action.GetTargetedRoomIndex(), action.GetPostActionRoomIndex(),
                    boss.GetBossState().GetVisitedRoomRecord(), Field<bool>(action, "_setupAsInitialAction"), Rng());
                row.Predicted = BossActionTargetModel.Setup(row.Before);
                row.Difference = row.Predicted.Supported && JToken.DeepEquals(JToken.FromObject(row.Predicted), JToken.FromObject(row.Actual))
                    ? null : row.Predicted.UnsupportedReason ?? UnitPlayModelProbe.FirstDifference(
                        JToken.FromObject(row.Predicted), JToken.FromObject(row.Actual), "$");
            }
            catch (Exception error) { Errors.Add(error.ToString()); }
        }
        internal static void Write(FullBattleTrace trace, ManualLogSource log)
        {
            if (!Enabled) return;
            JObject before = JObject.FromObject(trace.CaptureDecision()); JObject rngBefore = AllRng();
            int frame = UnityEngine.Time.frameCount;
            RandomState random = (RandomState)AccessTools.Field(typeof(RandomManager), "_randomState").GetValue(null);
            RandomState.State stream = random.FindState(RngId.BossActions); HadesRNG original = stream.rng;
            var samples = new List<object>();
            var controls = new List<Record>();
            CharacterState boss = AllGameManagers.Instance.GetHeroManager().GetOuterTrainBossCharacter();
            RoomManager rooms = AllGameManagers.Instance.GetRoomManager();
            BossState originalBoss = boss.GetBossState(); int[] originalVisits = originalBoss.GetVisitedRoomRecord().ToArray();
            var bossField = AccessTools.Field(typeof(CharacterState), "bossState");
            try
            {
                foreach (int seed in new[] { 0, 1, -1, 424242 })
                for (int mask = 1; mask <= 8; mask++)
                for (int history = -1; history < 27; history++)
                foreach (bool initial in new[] { false, true })
                {
                    int[] possible = mask == 8 ? new[] { 2, 0, 1 } : Enumerable.Range(0, 3).Where(index => (mask & (1 << index)) != 0).ToArray();
                    List<int>? visited = history < 0 ? null : new List<int> { history % 3, history / 3 % 3, history / 9 % 3 };
                    stream.rng = new HadesRNG(seed); UnityRng input = Rng();
                    int room = RandomBossActionBehavior.SelectRandomRoom(possible.ToList(), visited, initial, -1);
                    samples.Add(new { Seed = seed, Rooms = possible, VisitedRooms = visited, Initial = initial,
                        Before = input, Room = room, After = Rng() });
                }
                calibrating = true;
                var detached = new BossState(boss, new List<ActionGroupData>());
                bossField.SetValue(boss, detached);
                foreach (int seed in new[] { 0, 1, -1, 424242 })
                for (int behavior = 0; behavior < 4; behavior++)
                for (int history = 0; history < 27; history++)
                for (int mode = 0; mode < 7; mode++)
                {
                    detached.GetVisitedRoomRecord().Clear();
                    detached.GetVisitedRoomRecord().AddRange(new[] { history % 3, history / 3 % 3, history / 9 % 3 });
                    IBossActionBehavior native;
                    if (behavior == 0) native = new RandomBossActionBehavior();
                    else if (behavior == 3) native = new RoomDestroyActionBehavior();
                    else
                    {
                        var occupied = new RandomOccupiedBossActionBehavior();
                        AccessTools.Field(typeof(RandomOccupiedBossActionBehavior), "_teamType")
                            .SetValue(occupied, behavior == 1 ? Team.Type.Monsters : Team.Type.Heroes);
                        native = occupied;
                    }
                    var action = new BossActionState();
                    AccessTools.Field(typeof(BossActionState), "_actionBehavior").SetValue(action, native);
                    AccessTools.Field(typeof(BossActionState), "_setupAsInitialAction").SetValue(action, mode >= 4);
                    stream.rng = new HadesRNG(seed);
                    bool initial = mode == 0; int current = initial ? -1 : (mode - 1) % 3;
                    Record row = Snapshot(action, boss, rooms, initial, current);
                    if (initial) action.InitialSetup(boss, rooms);
                    else action.TriggerSetup(boss, rooms, current);
                    Complete(row, action, boss); controls.Add(row);
                }
            }
            finally { bossField.SetValue(boss, originalBoss); stream.rng = original; calibrating = false; }
            JObject after = JObject.FromObject(trace.CaptureDecision()); JObject rngAfter = AllRng();
            bool unchanged = JToken.DeepEquals(before, after) && JToken.DeepEquals(rngBefore, rngAfter) && frame == UnityEngine.Time.frameCount &&
                ReferenceEquals(boss.GetBossState(), originalBoss) && originalVisits.SequenceEqual(originalBoss.GetVisitedRoomRecord());
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                Scenario = AllGameManagers.Instance.GetSaveManager().GetCurrentScenarioData().name,
                DecisionUnchanged = unchanged, FrameBefore = frame, FrameAfter = UnityEngine.Time.frameCount,
                RngBefore = rngBefore, RngAfter = rngAfter, Samples = samples, Actions = Records, ControlActions = controls, Errors }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "boss-action-target-calibration.mt2f"))) document.Write(output);
            int differences = Records.Concat(controls).Count(row => row.Actual == null || row.Difference != null);
            log.LogInfo("BOSS-TARGET-CALIBRATION samples=" + samples.Count + " actions=" + Records.Count + " controls=" + controls.Count +
                " differences=" + differences + " errors=" + Errors.Count + " unchanged=" + unchanged);
            if (!unchanged || differences != 0 || Errors.Count != 0 || Records.Count == 0)
                throw new InvalidOperationException("Native boss targeting calibration failed; inspect component capture.");
        }
        [HarmonyPatch(typeof(BossActionState), nameof(BossActionState.InitialSetup))]
        private static class InitialPatch
        {
            private static void Prefix(BossActionState __instance, CharacterState bossCharacter, RoomManager roomManager, out Record? __state)
            { __state = Begin(__instance, bossCharacter, roomManager, true, -1); }
            private static void Postfix(BossActionState __instance, CharacterState bossCharacter, Record? __state)
            { Complete(__state, __instance, bossCharacter); }
        }
        [HarmonyPatch(typeof(BossActionState), nameof(BossActionState.TriggerSetup))]
        private static class TriggerPatch
        {
            private static void Prefix(BossActionState __instance, CharacterState bossCharacter, RoomManager roomManager, int currentRoomIndex, out Record? __state)
            { __state = Begin(__instance, bossCharacter, roomManager, false, currentRoomIndex); }
            private static void Postfix(BossActionState __instance, CharacterState bossCharacter, Record? __state)
            { Complete(__state, __instance, bossCharacter); }
        }
    }
}

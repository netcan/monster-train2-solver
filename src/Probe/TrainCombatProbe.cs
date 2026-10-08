using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal sealed class TrainCombatProbe
    {
        private readonly ManualLogSource log;
        private readonly FullBattleTrace trace;
        private readonly List<Record> records = new List<Record>();
        private static TrainCombatProbe? active;
        internal IReadOnlyList<Record> Records => records;
        internal int Mismatches => records.Count(record => record.Difference != null);
        internal int Unsupported => records.Count(record => !record.Predicted.Supported);

        internal TrainCombatProbe(ManualLogSource log, FullBattleTrace trace)
        { this.log = log; this.trace = trace; active = this; }

        internal TrainCombatState Capture(bool captureContext = true)
        {
            AllGameManagers managers = AllGameManagers.Instance!;
            RoomManager rooms = managers.GetRoomManager()!;
            var states = new List<RoomCombatState>();
            var movement = new List<EnemyMovement>();
            for (int index = 0; index < rooms.GetNumRooms(); index++)
            {
                RoomState room = rooms.GetRoom(index);
                RoomCombatState state = trace.Capture(room, captureContext);
                states.Add(state);
                var enemies = new List<CharacterState>();
                room.AddCharactersToList(enemies, Team.Type.Heroes);
                CombatUnit[] captured = state.Units.Where(unit => unit.Team == CombatTeam.Enemy).ToArray();
                enemies = enemies.Where(unit => unit.IsAlive && !unit.IsDestroyed).ToList();
                for (int unitIndex = 0; unitIndex < enemies.Count; unitIndex++)
                {
                    CharacterState enemy = enemies[unitIndex];
                    movement.Add(new EnemyMovement(captured[unitIndex].Id, 1,
                        enemy.GetAscendsTrainAutomatically(), enemy.GetLoopsBetweenTrainFloors(), enemy.IsCompanionBoss()));
                }
            }
            return new TrainCombatState(states, movement,
                managers.GetCombatManager()!.NumSpawnPointsPerFloor(Team.Type.Heroes), captureContext ? trace.CaptureContext() : null);
        }

        private IEnumerator Wrap(IEnumerator native, string kind)
        {
            Record? record = null;
            try
            {
                AllGameManagers? managers = AllGameManagers.Instance;
                if (managers != null && !managers.GetSaveManager().PreviewMode && trace.NativeWon == null)
                {
                    TrainCombatState before = Capture();
                    record = new Record
                    {
                        Index = records.Count, Turn = managers.GetCombatManager()!.GetTurnCount(),
                        Kind = kind, Before = before,
                        Predicted = kind == "Combat" ? TrainCombatModel.ResolveCombat(before) : TrainCombatModel.Ascend(before)
                    };
                    records.Add(record);
                }
            }
            catch (Exception error) { trace.CaptureFailure(error); }
            try { while (native.MoveNext()) yield return native.Current; }
            finally
            {
                (native as IDisposable)?.Dispose();
                if (record != null) Complete(record);
            }
        }

        internal void CompletePending()
        { foreach (Record record in records.Where(record => record.Actual == null).ToArray()) Complete(record); }

        private void Complete(Record record)
        {
            if (record.Actual != null) return;
            try
            {
                record.Actual = Capture();
                if (record.Predicted.Supported)
                {
                    JToken expected = Comparable(record.Predicted.State!);
                    JToken actual = Comparable(record.Actual);
                    record.Difference = JToken.DeepEquals(expected, actual) ? null : "Train room unit states differ";
                    log.LogInfo("TRAIN-MODEL-" + (record.Difference == null ? "MATCH" : "MISMATCH") +
                        " index=" + record.Index + " kind=" + record.Kind + " turn=" + record.Turn);
                }
                else log.LogInfo("TRAIN-MODEL-UNSUPPORTED index=" + record.Index + " reason=" +
                    record.Predicted.UnsupportedReason);
            }
            catch (Exception error) { trace.CaptureFailure(error); }
        }

        private static JToken Comparable(TrainCombatState state) => JToken.FromObject(new
            { Rooms = state.Rooms.Select(room => new { room.RoomIndex, room.Units }).ToArray(), state.Context });

        internal sealed class Record
        {
            public int Index { get; set; }
            public int Turn { get; set; }
            public string Kind { get; set; } = "";
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatResult Predicted { get; set; } = null!;
            public TrainCombatState? Actual { get; set; }
            public string? Difference { get; set; }
        }

        [HarmonyPatch(typeof(CombatManager), "ResolveCombat")]
        private static class CombatPatch
        {
            private static void Postfix(ref IEnumerator __result)
            { if (active != null) __result = active.Wrap(__result, "Combat"); }
        }

        [HarmonyPatch(typeof(HeroManager), nameof(HeroManager.DoTurn))]
        private static class AscendPatch
        {
            private static void Postfix(ref IEnumerator __result)
            { if (active != null) __result = active.Wrap(__result, "Ascend"); }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class BattleSpawnPointProbe
    {
        internal sealed class CompactRecord
        {
            public int RoomIndex { get; set; }
            public CombatTeam Team { get; set; }
            public string Phase { get; set; } = "";
            public int Pivot { get; set; }
            public int PivotMoves { get; set; }
            public BattleSpawnPoints Before { get; set; } = null!;
            public BattleSpawnPoints? After { get; set; }
            public string? Error { get; set; }
        }
        internal static readonly List<CompactRecord> Compactions = new List<CompactRecord>();
        internal sealed class DecisionRecord
        {
            public BattleSpawnPoints Raw { get; set; } = null!;
            public BattleSpawnPoints Canonical { get; set; } = null!;
            public int[] LivingUnitIds { get; set; } = Array.Empty<int>();
        }
        internal static readonly List<DecisionRecord> Decisions = new List<DecisionRecord>();
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_PHYSICAL_SPAWNPOINTS") == "1";
        internal static BattleSpawnPoints Capture(FullBattleTrace trace)
        {
            AllGameManagers managers = AllGameManagers.Instance!;
            SaveManager save = managers.GetSaveManager();
            var groups = new List<SpawnPointGroupState>();
            var pointsByReference = new Dictionary<SpawnPoint, SpawnPointReference>();
            for (int index = 0; index < managers.GetRoomManager()!.GetNumRooms(); index++)
            foreach (Team.Type team in new[] { Team.Type.Heroes, Team.Type.Monsters })
            {
                RoomState room = managers.GetRoomManager()!.GetRoom(index);
                var group = (SpawnPointGroup)AccessTools.Field(typeof(RoomState),
                    team == Team.Type.Heroes ? "heroSpawnPointGroup" : "monsterSpawnPointGroup").GetValue(room);
                var points = (List<SpawnPoint>)AccessTools.Field(typeof(SpawnPointGroup), "_primarySpawnPoints").GetValue(group);
                CombatTeam modelTeam = team == Team.Type.Heroes ? CombatTeam.Enemy : CombatTeam.Player;
                for (int point = 0; point < points.Count; point++) pointsByReference.Add(points[point], new SpawnPointReference(index, modelTeam, point));
                groups.Add(new SpawnPointGroupState(index, modelTeam, save.GetNumSpawnPointsPerFloor(team), group.NumSpawnPoints,
                    points.Select(point => point.GetCharacterState() is CharacterState unit ? trace.UnitId(unit) : 0).ToArray(),
                    points.Select(point => point.IsOutsideTrain).ToArray()));
            }
            var references = new List<UnitSpawnPointState>();
            foreach (CharacterState unit in trace.KnownUnits)
            using (new CharacterState.SetAllowDestroyedAccessHelper(unit, onlyIfDestroyed: true))
            {
                object primary = AccessTools.Field(typeof(CharacterState), "_primaryStateInformation").GetValue(unit);
                references.Add(new UnitSpawnPointState(trace.UnitId(unit),
                    Reference((SpawnPoint?)AccessTools.Field(primary.GetType(), "spawnPoint").GetValue(primary)),
                    Reference((SpawnPoint?)AccessTools.Field(primary.GetType(), "lastKnownSpawnPoint").GetValue(primary)),
                    unit.IsOuterTrainBoss(), unit.SpawnedInPreviewMode));
            }
            var raw = new BattleSpawnPoints(groups, references);
            if (!trace.CanonicalDecisionCapture) return raw;
            // Coroutine effects retain raw points above. Stable decisions use logical
            // removal, independent of when the corpse dissolve destroys its Unity object.
            int[] living = trace.KnownUnits.Where(unit => unit != null && unit.IsAlive && !unit.IsDestroyed)
                .Select(trace.UnitId).OrderBy(id => id).ToArray();
            var active = new HashSet<int>(living);
            var canonical = new BattleSpawnPoints(groups, references.Select(point => active.Contains(point.UnitId) ? point :
                new UnitSpawnPointState(point.UnitId, null, null, point.OuterBoss, point.SpawnedInPreview)).ToArray());
            Decisions.Add(new DecisionRecord { Raw = raw, Canonical = canonical, LivingUnitIds = living });
            return canonical;
            SpawnPointReference? Reference(SpawnPoint? point) => point == null ? null :
                pointsByReference.TryGetValue(point, out SpawnPointReference? reference) ? reference :
                throw new InvalidOperationException("Retained physical point is outside the captured primary groups.");
        }
        [HarmonyPatch(typeof(RoomState), nameof(RoomState.ShiftSpawnPoints))]
        private static class CompactPatch
        {
            private static void Prefix(RoomState __instance, Team.Type team, int pivotIndex, out CompactRecord? __state)
            {
                __state = null;
                if (!Enabled || FullBattleTrace.Active == null || AllGameManagers.Instance!.GetSaveManager().PreviewMode ||
                    FullBattleTrace.Active.KnownUnits.Any(unit => unit.TemporaryPreviewsEnabled)) return;
                var record = new CompactRecord { RoomIndex = __instance.GetRoomIndex(),
                    Team = team == Team.Type.Heroes ? CombatTeam.Enemy : CombatTeam.Player, Pivot = pivotIndex,
                    Phase = AllGameManagers.Instance.GetCombatManager()!.GetCombatPhase().ToString() };
                __state = record; Compactions.Add(record);
                try { record.Before = Capture(FullBattleTrace.Active); }
                catch (Exception error) { record.Error = error.ToString(); }
            }
            private static void Postfix(int __result, CompactRecord? __state)
            {
                if (__state == null || __state.Error != null) return;
                try { __state.After = Capture(FullBattleTrace.Active!); __state.PivotMoves = __result; }
                catch (Exception error) { __state.Error = error.ToString(); }
            }
        }
    }
}

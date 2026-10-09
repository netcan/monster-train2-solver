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
            public PlaneSnapshot Planes { get; set; } = null!;
        }
        internal sealed class UnitPlanes
        {
            public int UnitId { get; set; }
            public UnitSpawnPointState Primary { get; set; } = null!;
            public UnitSpawnPointState? Preview { get; set; }
            public UnitSpawnPointState? Temporary { get; set; }
        }
        internal sealed class PlaneSnapshot
        {
            public SpawnPointGroupState[] Groups { get; set; } = Array.Empty<SpawnPointGroupState>();
            public UnitPlanes[] Units { get; set; } = Array.Empty<UnitPlanes>();
            public int[] CurrentCopyIds { get; set; } = Array.Empty<int>();
        }
        private sealed class Copy
        {
            internal int Id, RoomIndex, InnerCount;
            internal CombatTeam Team;
            internal SpawnPointGroup Owner = null!;
            internal SpawnPoint[] Points = Array.Empty<SpawnPoint>();
        }
        private static readonly Dictionary<SpawnPoint, SpawnPointReference> pointReferences = new Dictionary<SpawnPoint, SpawnPointReference>();
        private static readonly Dictionary<int, Copy> copies = new Dictionary<int, Copy>();
        internal static readonly List<DecisionRecord> Decisions = new List<DecisionRecord>();
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_PHYSICAL_SPAWNPOINTS") == "1";
        private static int TrackCopy(SpawnPointGroup group)
        {
            var points = (List<SpawnPoint>)AccessTools.Field(typeof(SpawnPointGroup), "_temporarySpawnPoints").GetValue(group);
            if (points.Count == 0) throw new InvalidOperationException("A copied point list is empty.");
            if (pointReferences.TryGetValue(points[0], out SpawnPointReference? known)) return known.PreviewCopyId!.Value;
            RoomState room = (RoomState)AccessTools.Field(typeof(SpawnPointGroup), "room").GetValue(group);
            Team.Type team = points[0].GetTeam();
            var copy = new Copy { Id = copies.Count + 1, RoomIndex = room.GetRoomIndex(),
                Team = team == Team.Type.Heroes ? CombatTeam.Enemy : CombatTeam.Player,
                InnerCount = AllGameManagers.Instance!.GetSaveManager().GetNumSpawnPointsPerFloor(team),
                Owner = group, Points = points.ToArray() };
            copies.Add(copy.Id, copy);
            for (int index = 0; index < points.Count; index++)
                pointReferences.Add(points[index], new SpawnPointReference(copy.RoomIndex, copy.Team, index, copy.Id));
            return copy.Id;
        }
        internal static SpawnPointReference? Reference(SpawnPoint? point) => point == null ? null :
            pointReferences.TryGetValue(point, out SpawnPointReference? reference) ? reference :
            throw new InvalidOperationException("A retained physical point is outside all observed primary/copied lists.");
        private static SpawnPointGroupState CopyGroup(FullBattleTrace trace, int id)
        {
            Copy copy = copies[id];
            return new SpawnPointGroupState(copy.RoomIndex, copy.Team, copy.InnerCount, copy.Owner.NumSpawnPoints,
                copy.Points.Select(point => point.GetCharacterState() is CharacterState actor ? trace.UnitId(actor) : 0).ToArray(),
                copy.Points.Select(point => point.IsOutsideTrain).ToArray(), id);
        }
        internal static SpawnPointWorld ActiveWorld(FullBattleTrace trace, SpawnPoint? target,
            IReadOnlyList<SpawnPointGroupState>? layout = null)
        {
            BattleSpawnPoints raw = Capture(trace);
            // Copied slots may still own actors absent from every primary room.
            // Observe this closure before freezing the unit inventory, including
            // the selected list when no actor currently points back to it.
            int observed;
            do
            {
                observed = trace.KnownUnits.Count();
                var selectedCopies = new HashSet<int>();
                if (Reference(target)?.PreviewCopyId is int targetCopy) selectedCopies.Add(targetCopy);
                foreach (CharacterState actor in trace.KnownUnits.ToArray())
                    using (new CharacterState.SetAllowDestroyedAccessHelper(actor, onlyIfDestroyed: true))
                    {
                        object selected = AccessTools.Property(typeof(CharacterState), "PrimaryStateInformation").GetValue(actor);
                        foreach (string field in new[] { "spawnPoint", "lastKnownSpawnPoint" })
                            if (Reference((SpawnPoint?)AccessTools.Field(selected.GetType(), field).GetValue(selected))?.PreviewCopyId is int id)
                                selectedCopies.Add(id);
                    }
                foreach (int id in selectedCopies) CopyGroup(trace, id);
            } while (observed != trace.KnownUnits.Count());
            var units = new List<SpawnPointOccupant>();
            foreach (CharacterState actor in trace.KnownUnits)
                using (new CharacterState.SetAllowDestroyedAccessHelper(actor, onlyIfDestroyed: true))
                {
                    object selected = AccessTools.Property(typeof(CharacterState), "PrimaryStateInformation").GetValue(actor);
                    units.Add(new SpawnPointOccupant(trace.UnitId(actor), actor.GetHP(), actor.IsAlive, actor.IsDead, actor.IsDestroyed,
                        actor.GetStatusEffectStacks("undying"), actor.SpawnedInPreviewMode, actor.IsOuterTrainBoss(),
                        Reference(actor.GetSpawnPoint()), Reference((SpawnPoint?)AccessTools.Field(selected.GetType(), "lastKnownSpawnPoint").GetValue(selected))));
                }
            int[] needed = units.SelectMany(unit => new[] { unit.Current, unit.LastKnown }).Append(Reference(target))
                .Where(point => point?.PreviewCopyId != null).Select(point => point!.PreviewCopyId!.Value).Distinct().ToArray();
            var groups = layout == null ? raw.Groups.Where(group => group.PreviewCopyId == null)
                .Concat(needed.Select(id => CopyGroup(trace, id))).ToArray() : layout.Select(group => group.PreviewCopyId is int id
                    ? CopyGroup(trace, id) : raw.Group(group.RoomIndex, group.Team)!).ToArray();
            return new SpawnPointWorld(AllGameManagers.Instance!.GetSaveManager().PreviewMode, groups, units);
        }
        internal static BattleSpawnPoints Capture(FullBattleTrace trace)
        {
            AllGameManagers managers = AllGameManagers.Instance!;
            SaveManager save = managers.GetSaveManager();
            var groups = new List<SpawnPointGroupState>();
            var currentCopies = new List<int>();
            for (int index = 0; index < managers.GetRoomManager()!.GetNumRooms(); index++)
            foreach (Team.Type team in new[] { Team.Type.Heroes, Team.Type.Monsters })
            {
                RoomState room = managers.GetRoomManager()!.GetRoom(index);
                var group = (SpawnPointGroup)AccessTools.Field(typeof(RoomState),
                    team == Team.Type.Heroes ? "heroSpawnPointGroup" : "monsterSpawnPointGroup").GetValue(room);
                var points = (List<SpawnPoint>)AccessTools.Field(typeof(SpawnPointGroup), "_primarySpawnPoints").GetValue(group);
                CombatTeam modelTeam = team == Team.Type.Heroes ? CombatTeam.Enemy : CombatTeam.Player;
                for (int point = 0; point < points.Count; point++) pointReferences[points[point]] = new SpawnPointReference(index, modelTeam, point);
                groups.Add(new SpawnPointGroupState(index, modelTeam, save.GetNumSpawnPointsPerFloor(team), group.NumSpawnPoints,
                    points.Select(point => point.GetCharacterState() is CharacterState unit ? trace.UnitId(unit) : 0).ToArray(),
                    points.Select(point => point.IsOutsideTrain).ToArray()));
                currentCopies.Add(TrackCopy(group));
            }
            var references = new List<UnitSpawnPointState>();
            var planes = new List<UnitPlanes>();
            foreach (CharacterState unit in trace.KnownUnits)
            using (new CharacterState.SetAllowDestroyedAccessHelper(unit, onlyIfDestroyed: true))
            {
                UnitSpawnPointState Read(object state) => new UnitSpawnPointState(trace.UnitId(unit),
                    Reference((SpawnPoint?)AccessTools.Field(state.GetType(), "spawnPoint").GetValue(state)),
                    Reference((SpawnPoint?)AccessTools.Field(state.GetType(), "lastKnownSpawnPoint").GetValue(state)),
                    unit.IsOuterTrainBoss(), unit.SpawnedInPreviewMode);
                UnitSpawnPointState? Optional(string field) => AccessTools.Field(typeof(CharacterState), field).GetValue(unit) is object state ? Read(state) : null;
                var mapping = new UnitPlanes { UnitId = trace.UnitId(unit), Primary = Optional("_primaryStateInformation")!,
                    Preview = Optional("_previewStateInformation"), Temporary = Optional("_temporaryStateInformation") };
                references.Add(mapping.Primary); planes.Add(mapping);
            }
            int[] CopyIds(IEnumerable<UnitSpawnPointState> states) => states.SelectMany(state => new[] { state.Current, state.LastKnown })
                .Where(point => point?.PreviewCopyId != null).Select(point => point!.PreviewCopyId!.Value).Distinct().OrderBy(id => id).ToArray();
            var raw = new BattleSpawnPoints(groups.Concat(CopyIds(references).Select(id => CopyGroup(trace, id))).ToArray(), references);
            if (!trace.CanonicalDecisionCapture) return raw;
            // Coroutine effects retain raw points above. Stable decisions use logical
            // removal, independent of when the corpse dissolve destroys its Unity object.
            int[] living = trace.KnownUnits.Where(unit => unit != null && unit.IsAlive && !unit.IsDestroyed)
                .Select(trace.UnitId).OrderBy(id => id).ToArray();
            var active = new HashSet<int>(living);
            var canonical = new BattleSpawnPoints(groups, references.Select(point => active.Contains(point.UnitId) ? point :
                new UnitSpawnPointState(point.UnitId, null, null, point.OuterBoss, point.SpawnedInPreview)).ToArray());
            int[] allCopies = CopyIds(planes.SelectMany(unit => new[] { unit.Primary, unit.Preview, unit.Temporary })
                .Where(state => state != null).Cast<UnitSpawnPointState>()).Concat(currentCopies).Distinct().OrderBy(id => id).ToArray();
            Decisions.Add(new DecisionRecord { Raw = raw, Canonical = canonical, LivingUnitIds = living,
                Planes = new PlaneSnapshot { Groups = groups.Concat(allCopies.Select(id => CopyGroup(trace, id))).ToArray(),
                    Units = planes.ToArray(), CurrentCopyIds = currentCopies.ToArray() } });
            return canonical;
        }
        [HarmonyPatch(typeof(SpawnPointGroup), "DeepCopyTemporarySpawnPointsList")]
        private static class CopyPatch
        {
            private static void Postfix(SpawnPointGroup __instance)
            { if (Enabled && FullBattleTrace.Active != null) TrackCopy(__instance); }
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

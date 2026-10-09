using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class UnitSpawnPointState
    {
        public int UnitId { get; }
        public SpawnPointReference? Current { get; }
        public SpawnPointReference? LastKnown { get; }
        public bool OuterBoss { get; }
        public bool SpawnedInPreview { get; }
        public UnitSpawnPointState(int unitId, SpawnPointReference? current, SpawnPointReference? lastKnown,
            bool outerBoss = false, bool spawnedInPreview = false)
        { UnitId = unitId; Current = current; LastKnown = lastKnown; OuterBoss = outerBoss; SpawnedInPreview = spawnedInPreview; }
    }
    // Position ownership survives unit removal. Health/status/lifecycle inputs are
    // supplied by the actual operation's combat actors rather than duplicated here.
    public sealed class BattleSpawnPoints
    {
        public IReadOnlyList<SpawnPointGroupState> Groups { get; }
        public IReadOnlyList<UnitSpawnPointState> Units { get; }
        public int? NextPreviewCopyId { get; }
        // These are simulation-owned selected planes. Native captures expose the
        // primary pointers; preview operations temporarily select a different list.
        internal IReadOnlyList<int> ActiveCopies { get; private set; } = Array.Empty<int>();
        internal IReadOnlyList<UnitSpawnPointState> BirthPrimaries { get; private set; } = Array.Empty<UnitSpawnPointState>();
        public BattleSpawnPoints(IReadOnlyList<SpawnPointGroupState> groups, IReadOnlyList<UnitSpawnPointState> units,
            int? nextPreviewCopyId = null)
        { Groups = Array.AsReadOnly(groups.OrderBy(group => group.RoomIndex).ThenBy(group => group.Team).ThenBy(group => group.PreviewCopyId).ToArray());
            Units = Array.AsReadOnly(units.OrderBy(unit => unit.UnitId).ToArray()); NextPreviewCopyId = nextPreviewCopyId; }
        public SpawnPointGroupState? Group(int roomIndex, CombatTeam team, int? previewCopyId = null) =>
            Groups.SingleOrDefault(group => group.RoomIndex == roomIndex && group.Team == team && group.PreviewCopyId == previewCopyId);
        public int FirstEmpty(int roomIndex, CombatTeam team)
        {
            SpawnPointGroupState? group = SelectedGroup(roomIndex, team);
            if (group == null) return -1;
            for (int index = 0; index < group.GroupCount; index++) if (group.Occupants[index] == 0) return index;
            return -1;
        }
        internal SpawnPointGroupState? SelectedGroup(int roomIndex, CombatTeam team) => ActiveCopies.Count == 0 ?
            Group(roomIndex, team) : Groups.SingleOrDefault(group => group.RoomIndex == roomIndex &&
                group.Team == team && group.PreviewCopyId.HasValue && ActiveCopies.Contains(group.PreviewCopyId.Value));
        internal BattleSpawnPoints WithState(IReadOnlyList<SpawnPointGroupState> groups, IReadOnlyList<UnitSpawnPointState> units)
            => new BattleSpawnPoints(groups, units, NextPreviewCopyId) { ActiveCopies = ActiveCopies, BirthPrimaries = BirthPrimaries };
        internal BattleSpawnPoints WithBirthPrimary(int id)
            => new BattleSpawnPoints(Groups, Units, NextPreviewCopyId) { ActiveCopies = ActiveCopies,
                BirthPrimaries = Array.AsReadOnly(BirthPrimaries.Append(Units.Single(unit => unit.UnitId == id)).ToArray()) };
        internal static BattleSpawnPoints FromWorld(BattleSpawnPoints source, SpawnPointWorld world) => source.WithState(world.Groups,
            world.Units.Select(unit => new UnitSpawnPointState(unit.UnitId, unit.Current, unit.LastKnown,
                unit.OuterBoss, unit.SpawnedInPreview)).ToArray());

        internal BattleSpawnPoints BeginPreview()
        {
            if (!NextPreviewCopyId.HasValue || ActiveCopies.Count > 0) return this;
            int next = NextPreviewCopyId.Value;
            var copies = Groups.Where(group => !group.PreviewCopyId.HasValue).Select(group => new SpawnPointGroupState(
                group.RoomIndex, group.Team, group.InnerCount, group.GroupCount, group.Occupants, group.Outside, next++)).ToArray();
            return new BattleSpawnPoints(Groups.Concat(copies).ToArray(), Units, next)
            { ActiveCopies = Array.AsReadOnly(copies.Select(group => group.PreviewCopyId!.Value).ToArray()), BirthPrimaries = BirthPrimaries };
        }

        internal static BattleSpawnPoints EndPreview(BattleSpawnPoints primary, BattleSpawnPoints selected, bool clearBirths)
        {
            if (!primary.NextPreviewCopyId.HasValue) return primary;
            var existing = primary.Units.Select(unit => unit.UnitId).ToHashSet();
            var births = selected.Units.Where(unit => !existing.Contains(unit.UnitId)).Select(unit => clearBirths ?
                new UnitSpawnPointState(unit.UnitId, null, null, unit.OuterBoss, unit.SpawnedInPreview) :
                selected.BirthPrimaries.FirstOrDefault(birth => birth.UnitId == unit.UnitId) ?? unit).ToArray();
            UnitSpawnPointState[] units = primary.Units.Concat(births).ToArray();
            var retained = units.SelectMany(unit => new[] { unit.Current, unit.LastKnown }).Where(point => point?.PreviewCopyId != null)
                .Select(point => point!.PreviewCopyId!.Value).ToHashSet();
            var groups = primary.Groups.Where(group => !group.PreviewCopyId.HasValue).Concat(
                selected.Groups.Where(group => group.PreviewCopyId.HasValue && retained.Contains(group.PreviewCopyId.Value))).ToArray();
            return new BattleSpawnPoints(groups, units, selected.NextPreviewCopyId);
        }
    }
    internal sealed class BattleSpawnPointResult
    {
        internal BattleSpawnPoints? State { get; }
        internal bool Supported => State != null;
        internal string? Error { get; }
        internal int PivotMoves { get; }
        internal BattleSpawnPointResult(BattleSpawnPoints? state, string? error = null, int pivotMoves = 0)
        { State = state; Error = error; PivotMoves = pivotMoves; }
    }
    internal static class BattleSpawnPointModel
    {
        internal static string? Validate(BattleSpawnPoints source, int? nextUnitId)
        {
            if (!nextUnitId.HasValue || source.Units.Any(unit => unit.UnitId <= 0 || unit.UnitId >= nextUnitId.Value) ||
                source.Units.Select(unit => unit.UnitId).Distinct().Count() != source.Units.Count ||
                source.Groups.Select(group => (group.RoomIndex, group.Team, group.PreviewCopyId)).Distinct().Count() != source.Groups.Count)
                return "Invalid retained physical identities or missing shared counter.";
            if (source.NextPreviewCopyId <= 0 || source.Groups.Any(group => group.PreviewCopyId >= source.NextPreviewCopyId))
                return "Invalid copied point identity counter.";
            var ids = source.Units.Select(unit => unit.UnitId).ToHashSet();
            if (source.Groups.Any(group => group.PreviewCopyId <= 0 || group.InnerCount < 1 || group.GroupCount < 0 ||
                Math.Max(group.InnerCount, group.GroupCount) > group.Occupants.Count || group.Outside.Count != group.Occupants.Count ||
                group.Occupants.Any(id => id < 0 || id > 0 && !ids.Contains(id)))) return "Invalid physical point layout.";
            foreach (var reference in source.Units.SelectMany(unit => new[] { unit.Current, unit.LastKnown }).Where(point => point != null))
            {
                var group = source.Group(reference!.RoomIndex, reference.Team, reference.PreviewCopyId);
                if (group == null || reference.Index < 0 || reference.Index >= group.Occupants.Count)
                    return "Uncaptured retained physical reference.";
            }
            return null;
        }
        internal static BattleSpawnPointResult Apply(BattleSpawnPoints source, RoomCombatState room, string operation,
            CombatTeam team, int unitId = 0, int index = -1, int targetIndex = -1, SpawnPointReference? target = null,
            CombatUnit? newUnit = null)
        {
            string? invalid = Validate(source, room.Context?.NextUnitId);
            if (invalid != null) return Reject(invalid);
            UnitSpawnPointState[] references = source.Units.ToArray();
            if (newUnit != null)
            {
                if (references.Any(unit => unit.UnitId == newUnit.Id)) return Reject("A birth reused a retained physical identity.");
                references = references.Append(new UnitSpawnPointState(newUnit.Id, null, null, spawnedInPreview: room.Preview)).ToArray();
            }
            var known = room.Units.Concat(newUnit == null ? Array.Empty<CombatUnit>() : new[] { newUnit })
                .GroupBy(unit => unit.Id).ToDictionary(group => group.Key, group => group.Last());
            SpawnPointGroupState? group = source.SelectedGroup(room.RoomIndex, team);
            if (group == null) return Reject("Missing physical spawn point group.");
            if (operation == "Compact" || operation == "ShiftOccupants")
                foreach (int id in group.Occupants.Where(id => id != 0))
                    if (!known.ContainsKey(id)) return Reject("Physical shift requires its retained occupant's combat state.");
            var world = new SpawnPointWorld(room.Preview, source.Groups, references.Select(reference =>
            {
                known.TryGetValue(reference.UnitId, out CombatUnit? unit);
                // Unaffected groups retain their references. Their numerical fields
                // are never used by this operation; the selected group's actors are required above.
                int hp = unit?.Health ?? 1;
                bool alive = hp > 0 && unit?.DeathState?.IsDespawned != true;
                return new SpawnPointOccupant(reference.UnitId, hp, alive, !alive, unit?.DeathState?.IsDestroyed == true,
                    unit?.Status("undying")?.Stacks ?? 0, reference.SpawnedInPreview, reference.OuterBoss,
                    reference.Current, reference.LastKnown);
            }).ToArray());
            var result = SpawnPointModel.Apply(world, operation, room.RoomIndex, team, unitId, index, targetIndex, target,
                previewCopyId: group.PreviewCopyId);
            return result.Supported ? new BattleSpawnPointResult(BattleSpawnPoints.FromWorld(source, result.State!), pivotMoves: result.PivotMoves) : Reject(result.UnsupportedReason!);
        }
        internal static BattleSpawnPointResult Birth(BattleSpawnPoints source, RoomCombatState room, CombatUnit unit,
            int position, bool shift)
        {
            SpawnPointGroupState? group = source.SelectedGroup(room.RoomIndex, unit.Team);
            if (group == null || position < 0 || position >= group.GroupCount) return Reject("Invalid physical birth point.");
            BattleSpawnPoints state = source;
            if (shift)
            {
                var moved = Apply(state, room, "ShiftOccupants", unit.Team, index: position);
                if (!moved.Supported) return moved;
                state = moved.State!;
            }
            if (state.SelectedGroup(room.RoomIndex, unit.Team)!.Occupants[position] != 0)
                return Reject("Physical birth point is still occupied after insertion shifting.");
            var placed = Apply(state, room, "Set", unit.Team, unit.Id,
                target: new SpawnPointReference(room.RoomIndex, unit.Team, position, group.PreviewCopyId), newUnit: unit);
            if (placed.Supported && source.ActiveCopies.Count > 0)
                placed = new BattleSpawnPointResult(placed.State!.WithBirthPrimary(unit.Id), pivotMoves: placed.PivotMoves);
            if (!placed.Supported || !shift && unit.Team != CombatTeam.Player || room.Preview) return placed;
            return Apply(placed.State!, new RoomCombatState(room.RoomIndex, room.Deployment, room.Units.Append(unit).ToArray(),
                room.ExternalInteractions, room.Context, room.Preview), "Compact", unit.Team);
        }
        internal static CombatUnit[] Order(BattleSpawnPoints? source, int roomIndex, IReadOnlyList<CombatUnit> units)
        {
            if (source == null) return units.ToArray();
            var result = new List<CombatUnit>();
            foreach (CombatTeam team in new[] { CombatTeam.Enemy, CombatTeam.Player })
            {
                var members = units.Where(unit => unit.Team == team).ToDictionary(unit => unit.Id);
                foreach (int id in source.SelectedGroup(roomIndex, team)?.Occupants ?? Array.Empty<int>())
                    if (members.TryGetValue(id, out CombatUnit? unit)) { result.Add(unit); members.Remove(id); }
                result.AddRange(units.Where(unit => unit.Team == team && members.ContainsKey(unit.Id)));
            }
            return result.ToArray();
        }
        private static BattleSpawnPointResult Reject(string error) => new BattleSpawnPointResult(null, error);
    }
}

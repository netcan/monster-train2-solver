using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class SpawnPointReference
    {
        public int RoomIndex { get; }
        public CombatTeam Team { get; }
        public int Index { get; }
        public SpawnPointReference(int roomIndex, CombatTeam team, int index)
        { RoomIndex = roomIndex; Team = team; Index = index; }
        internal bool Same(SpawnPointReference? other) => other != null && RoomIndex == other.RoomIndex && Team == other.Team && Index == other.Index;
    }
    public sealed class SpawnPointOccupant
    {
        public int UnitId { get; }
        public int Health { get; }
        public bool Alive { get; }
        public bool Dead { get; }
        public bool Destroyed { get; }
        public int Undying { get; }
        public bool SpawnedInPreview { get; }
        public bool OuterBoss { get; }
        public SpawnPointReference? Current { get; }
        public SpawnPointReference? LastKnown { get; }
        public SpawnPointOccupant(int unitId, int health, bool alive, bool dead, bool destroyed, int undying,
            bool spawnedInPreview, bool outerBoss, SpawnPointReference? current, SpawnPointReference? lastKnown)
        { UnitId = unitId; Health = health; Alive = alive; Dead = dead; Destroyed = destroyed; Undying = undying;
            SpawnedInPreview = spawnedInPreview; OuterBoss = outerBoss; Current = current; LastKnown = lastKnown; }
        internal SpawnPointOccupant WithPoints(SpawnPointReference? current, SpawnPointReference? lastKnown) =>
            new SpawnPointOccupant(UnitId, Health, Alive, Dead, Destroyed, Undying, SpawnedInPreview, OuterBoss, current, lastKnown);
    }
    public sealed class SpawnPointGroupState
    {
        public int RoomIndex { get; }
        public CombatTeam Team { get; }
        public int InnerCount { get; }
        public int GroupCount { get; }
        public IReadOnlyList<int> Occupants { get; }
        public IReadOnlyList<bool> Outside { get; }
        public SpawnPointGroupState(int roomIndex, CombatTeam team, int innerCount, int groupCount,
            IReadOnlyList<int> occupants, IReadOnlyList<bool> outside)
        { RoomIndex = roomIndex; Team = team; InnerCount = innerCount; GroupCount = groupCount;
            Occupants = Array.AsReadOnly(occupants.ToArray()); Outside = Array.AsReadOnly(outside.ToArray()); }
    }
    public sealed class SpawnPointWorld
    {
        public bool Preview { get; }
        public IReadOnlyList<SpawnPointGroupState> Groups { get; }
        public IReadOnlyList<SpawnPointOccupant> Units { get; }
        public SpawnPointWorld(bool preview, IReadOnlyList<SpawnPointGroupState> groups, IReadOnlyList<SpawnPointOccupant> units)
        { Preview = preview; Groups = Array.AsReadOnly(groups.OrderBy(group => group.RoomIndex).ThenBy(group => group.Team).ToArray());
            Units = Array.AsReadOnly(units.OrderBy(unit => unit.UnitId).ToArray()); }
        public SpawnPointReference? Point(int unitId, bool allowLastKnown = false)
        {
            SpawnPointOccupant unit = Units.Single(item => item.UnitId == unitId);
            return allowLastKnown && unit.Health <= 0 ? unit.LastKnown ?? unit.Current : unit.Current;
        }
        public int Remaining(int roomIndex, CombatTeam team)
        {
            SpawnPointGroupState group = Groups.Single(item => item.RoomIndex == roomIndex && item.Team == team);
            return Math.Max(group.GroupCount - group.Occupants.Count(id => id != 0 && !Units.Single(unit => unit.UnitId == id).OuterBoss), 0);
        }
    }
    public sealed class SpawnPointResult
    {
        public SpawnPointWorld? State { get; }
        public int PivotMoves { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => State != null;
        internal SpawnPointResult(SpawnPointWorld? state, int pivotMoves = 0, string? unsupportedReason = null)
        { State = state; PivotMoves = pivotMoves; UnsupportedReason = unsupportedReason; }
    }
    public static class SpawnPointModel
    {
        public static SpawnPointResult Apply(SpawnPointWorld source, string operation, int roomIndex,
            CombatTeam team, int unitId = 0, int index = -1, int targetIndex = -1, SpawnPointReference? target = null)
        {
            string? error = Validate(source);
            if (error != null) return new SpawnPointResult(null, unsupportedReason: error);
            var groups = source.Groups.ToDictionary(group => (group.RoomIndex, group.Team), group => group.Occupants.ToArray());
            var units = source.Units.ToDictionary(unit => unit.UnitId);
            if (!groups.TryGetValue((roomIndex, team), out int[]? points)) return Reject("Unknown spawn point group.");
            SpawnPointGroupState group = source.Groups.Single(item => item.RoomIndex == roomIndex && item.Team == team);
            int pivotMoves = 0;
            try
            {
                if (operation == "Set") Set(unitId, target);
                else if (operation == "Remember") Remember(unitId);
                else if (operation == "Remove") Set(unitId, null);
                else if (operation == "ShiftOccupants")
                {
                    if (index >= 0)
                        for (int i = group.GroupCount - 2; i >= index; i--)
                        {
                            int id = points[i], next = points[i + 1];
                            if (id != 0 && units[id].Alive && !units[id].Destroyed &&
                                (next == 0 || !units[next].Alive || units[next].Destroyed))
                            { Set(id, Reference(i + 1)); points[i] = 0; }
                        }
                }
                else if (operation == "Rearrange")
                {
                    if (index >= 0 && index < group.GroupCount && targetIndex >= 0 && targetIndex < group.GroupCount)
                        for (int i = index; i != targetIndex;)
                        {
                            int next = i + (i < targetIndex ? 1 : -1), first = points[i], second = points[next];
                            if (first != 0) Set(first, Reference(next));
                            if (second != 0) Set(second, Reference(i));
                            i = next;
                        }
                }
                else if (operation == "Compact")
                {
                    int pivot = index, passes = 0; bool changed;
                    do
                    {
                        changed = false;
                        for (int i = 0; i < group.InnerCount - 1; i++)
                        {
                            int id = points[i];
                            bool dead = id != 0 && (units[id].Destroyed || units[id].Dead || units[id].Health <= 0) &&
                                (units[id].Destroyed || units[id].Undying <= 0);
                            if (id != 0 && !dead && (source.Preview || !units[id].SpawnedInPreview)) continue;
                            if (id != 0) { Remember(id); Set(id, null); changed = true; }
                            int next = points[i + 1];
                            if (next != 0)
                            {
                                Set(next, Reference(i)); points[i + 1] = 0; changed = true;
                                if (pivot > i) { pivotMoves++; pivot--; }
                            }
                        }
                        passes++;
                    } while (changed && passes < 12);
                    for (int i = 0; i < points.Length; i++)
                        if (!group.Outside[i] && points[i] != 0) Set(points[i], Reference(i));
                }
                else return Reject("Unknown spawn point operation.");
            }
            catch (InvalidOperationException ex) { return Reject(ex.Message); }
            return new SpawnPointResult(new SpawnPointWorld(source.Preview, source.Groups.Select(item =>
                new SpawnPointGroupState(item.RoomIndex, item.Team, item.InnerCount, item.GroupCount,
                    groups[(item.RoomIndex, item.Team)], item.Outside)).ToArray(), units.Values.ToArray()), pivotMoves);
            SpawnPointReference Reference(int at) => new SpawnPointReference(roomIndex, team, at);
            void Remember(int id)
            {
                if (!units.TryGetValue(id, out SpawnPointOccupant? unit)) throw new InvalidOperationException("Unknown spawn point unit.");
                units[id] = unit.WithPoints(unit.Current, unit.Current);
            }
            void Set(int id, SpawnPointReference? to)
            {
                if (!units.TryGetValue(id, out SpawnPointOccupant? unit)) throw new InvalidOperationException("Unknown spawn point unit.");
                if (unit.Destroyed && unit.Current == null && to != null) return;
                if (to != null && !Exists(to)) throw new InvalidOperationException("Unknown target spawn point.");
                if (unit.Current != null && groups[(unit.Current.RoomIndex, unit.Current.Team)][unit.Current.Index] == id)
                    groups[(unit.Current.RoomIndex, unit.Current.Team)][unit.Current.Index] = 0;
                units[id] = unit.WithPoints(to, unit.LastKnown);
                if (to != null) groups[(to.RoomIndex, to.Team)][to.Index] = id;
            }
            bool Exists(SpawnPointReference point) => groups.TryGetValue((point.RoomIndex, point.Team), out int[]? values) &&
                point.Index >= 0 && point.Index < values.Length;
            SpawnPointResult Reject(string reason) => new SpawnPointResult(null, unsupportedReason: reason);
        }
        private static string? Validate(SpawnPointWorld source)
        {
            if (source.Groups.Select(group => (group.RoomIndex, group.Team)).Distinct().Count() != source.Groups.Count ||
                source.Units.Select(unit => unit.UnitId).Distinct().Count() != source.Units.Count || source.Units.Any(unit => unit.UnitId <= 0))
                return "Invalid spawn point identities.";
            var ids = source.Units.Select(unit => unit.UnitId).ToHashSet();
            if (source.Groups.Any(group => group.InnerCount < 1 || group.GroupCount < 0 ||
                Math.Max(group.InnerCount, group.GroupCount) > group.Occupants.Count || group.Outside.Count != group.Occupants.Count ||
                group.Occupants.Any(id => id < 0 || id != 0 && !ids.Contains(id)))) return "Invalid spawn point group layout.";
            foreach (SpawnPointReference point in source.Units.SelectMany(unit => new[] { unit.Current, unit.LastKnown }).Where(point => point != null).Cast<SpawnPointReference>())
            {
                SpawnPointGroupState? group = source.Groups.SingleOrDefault(item => item.RoomIndex == point.RoomIndex && item.Team == point.Team);
                if (group == null || point.Index < 0 || point.Index >= group.Occupants.Count) return "Invalid current or last-known spawn point.";
            }
            return null;
        }
    }
}

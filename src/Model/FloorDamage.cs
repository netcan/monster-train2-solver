using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class FloorDamageModel
    {
        internal const string Effect = "DamageFloorByFloor";
        // The native effect snapshots its targets, then sorts descending by
        // floor and ascending by physical point. Never recollect after a kill.
        internal static int[]? Order(TrainCombatState source, IReadOnlyList<int> targets, out string? error)
        {
            error = null;
            if (targets.Count == 0) return Array.Empty<int>();
            var points = source.Context?.SpawnPoints;
            if (points == null) { error = "Floor damage requires captured physical spawn points."; return null; }
            var ordered = targets.Select(id => (Id: id, Point: points.Units.FirstOrDefault(unit => unit.UnitId == id)?.Current)).ToArray();
            if (ordered.Any(item => item.Point == null))
            { error = "Floor damage target lost its physical room/index."; return null; }
            if (ordered.GroupBy(item => (item.Point!.RoomIndex, item.Point.Index)).Any(group => group.Count() > 1))
            { error = "Mixed-team floor damage with tied physical indices requires native ordering calibration."; return null; }
            return ordered.OrderByDescending(item => item.Point!.RoomIndex).ThenBy(item => item.Point!.Index).Select(item => item.Id).ToArray();
        }
    }
}

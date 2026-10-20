using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class UnitRoomTriggerCountModel
    {
        internal const string Type = "RoomStateModifyTriggerCount";
        internal static int Modifier(IEnumerable<CombatUnit> units, CombatTeam team, string kind) => units
            .Where(unit => unit.Team == team && unit.Health > 0 && unit.DeathState?.IsDestroyed != true && unit.DeathState?.IsDespawned != true)
            .Sum(unit => unit.Modifiers?.RoomTriggerCounts?.Where(rule => rule.Kind == kind).Sum(rule => rule.Value) ?? 0);
        internal static string? Validate(CombatUnit unit) => unit.Modifiers?.RoomTriggerCounts?.Any(rule =>
            rule.Kind != "OnMoonPhaseShift" || rule.Value != 1) == true ? "Uncalibrated unit room trigger count modifier." : null;
    }
}

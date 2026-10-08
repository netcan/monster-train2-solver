using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class UnitIdentityAllocation
    {
        public int UnitId { get; }
        public int NextUnitId { get; }
        public CombatContext? Context { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => UnsupportedReason == null;
        internal UnitIdentityAllocation(int id, int next, CombatContext? context, string? error = null)
        { UnitId = id; NextUnitId = next; Context = context; UnsupportedReason = error; }
    }

    public static class UnitIdentityModel
    {
        public static string? Validate(CombatContext? context, IEnumerable<CombatUnit> units, int? outerNextUnitId = null)
        {
            if (context?.NextUnitId is not int next) return null;
            if (next <= 0 || outerNextUnitId.HasValue && next != outerNextUnitId.Value)
                return "Invalid or inconsistent shared unit identity counter.";
            return units.Any(unit => unit.Id >= next) ? "Shared unit identity counter overlaps an existing unit." : null;
        }

        // Context-only allocation is required for effects that have no outer spawn state.
        public static UnitIdentityAllocation Allocate(CombatContext context) => context.NextUnitId.HasValue
            ? Allocate(context, context.NextUnitId.Value)
            : new UnitIdentityAllocation(0, 0, null, "Unit creation requires a captured shared identity counter.");

        // Legacy unit plays/waves retain their authoritative outer counter and null context field.
        public static UnitIdentityAllocation Allocate(CombatContext? context, int nextUnitId)
        {
            if (nextUnitId <= 0 || nextUnitId == int.MaxValue ||
                context?.NextUnitId is int shared && shared != nextUnitId)
                return new UnitIdentityAllocation(0, 0, null, "Invalid, exhausted or inconsistent unit identity allocation.");
            int next = nextUnitId + 1;
            return new UnitIdentityAllocation(nextUnitId, next,
                context?.NextUnitId.HasValue == true ? context.WithNextUnitId(next) : context);
        }
    }
}

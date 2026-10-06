using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class CardTargetFilters
    {
        public string Health { get; }
        public IReadOnlyList<string> RequiredStatuses { get; }
        public IReadOnlyList<string> ExcludedStatuses { get; }
        public bool IgnoreBosses { get; }
        public string Subtype { get; }
        public IReadOnlyList<string> ExcludedSubtypes { get; }
        public CardTargetFilters(string health, IReadOnlyList<string> requiredStatuses, IReadOnlyList<string> excludedStatuses,
            bool ignoreBosses, string subtype, IReadOnlyList<string> excludedSubtypes)
        { Health = health; RequiredStatuses = Array.AsReadOnly(requiredStatuses.ToArray()); ExcludedStatuses = Array.AsReadOnly(excludedStatuses.ToArray());
            IgnoreBosses = ignoreBosses; Subtype = subtype; ExcludedSubtypes = Array.AsReadOnly(excludedSubtypes.ToArray()); }

        internal string? Validate() => new[] { "Both", "Damaged", "Undamaged" }.Contains(Health) ? null : "Unknown target health filter.";
        internal bool Matches(CombatUnit unit, bool dropOverride, out string? error)
        {
            error = null;
            if (dropOverride)
            {
                // CheckTargetsOverride bypasses health/status filters and tests BOTH subtype lists.
                if (!SubtypeMatches(unit, Subtype) || ExcludedSubtypes.Any(subtype => subtype.Length > 0 && SubtypeMatches(unit, subtype))) return false;
            }
            else
            {
                if (RequiredStatuses.Any(id => unit.Statuses.All(status => status.Id != id)) ||
                    ExcludedStatuses.Any(id => unit.Statuses.Any(status => status.Id == id))) return false;
                if (Health == "Damaged" && unit.Health == unit.MaxHealth || Health == "Undamaged" && unit.Health < unit.MaxHealth) return false;
            }
            if (IgnoreBosses)
            {
                if (!unit.IsBoss.HasValue) { error = "Boss filtering requires captured IsAnyBoss state."; return false; }
                if (unit.IsBoss.Value) return false;
            }
            if (!dropOverride)
            {
                // Native returns here for a required subtype, skipping excluded subtype checks.
                if (Subtype.Length > 0) return SubtypeMatches(unit, Subtype);
                if (ExcludedSubtypes.Any(subtype => SubtypeMatches(unit, subtype))) return false;
            }
            return true;
        }
        private static bool SubtypeMatches(CombatUnit unit, string subtype) => subtype.Length == 0 ? !unit.IsPyre : unit.Subtypes.Contains(subtype);
    }
}

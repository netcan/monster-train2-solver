using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RoomPostCombatRule
    {
        public IReadOnlyList<CardActionEffect> Effects { get; }
        public bool DisallowedInDeployment { get; }
        public RoomPostCombatRule(IReadOnlyList<CardActionEffect> effects, bool disallowedInDeployment)
        { Effects = Array.AsReadOnly(effects.ToArray()); DisallowedInDeployment = disallowedInDeployment; }
    }
    public static class RoomPostCombatModel
    {
        public static string? Validate(RoomPostCombatRule rule) => !rule.DisallowedInDeployment || rule.Effects.Count != 1 || rule.Effects.Any(effect =>
            effect.Type != "AdjustAllStatusEffects" || effect.Target != "Room" || !effect.AllowPlayer || !effect.AllowEnemy ||
            effect.Value != 1 || effect.StatusAdjustmentFilter != 1 || effect.Statuses.Count > 0 ||
            effect.Range != null || effect.Upgrade != null || effect.Filters != null &&
                (effect.Filters.Health != "Both" || effect.Filters.RequiredStatuses.Count > 0 || effect.Filters.ExcludedStatuses.Count > 0 ||
                 effect.Filters.IgnoreBosses || effect.Filters.Subtype.Length > 0 || effect.Filters.ExcludedSubtypes.Count > 0))
            ? "Uncalibrated room post-combat effect composition." : null;
    }
}

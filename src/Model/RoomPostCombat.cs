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
        public static string? Validate(RoomPostCombatRule rule) => rule.Effects.Count != 1 || rule.Effects.Any(effect =>
            !SupportedPayload(effect, rule.DisallowedInDeployment) || effect.Target != "Room" ||
            effect.Range != null || effect.Upgrade != null || effect.Filters != null &&
                (effect.Filters.Health != "Both" || effect.Filters.RequiredStatuses.Count > 0 || effect.Filters.ExcludedStatuses.Count > 0 ||
                 effect.Filters.IgnoreBosses || effect.Filters.Subtype.Length > 0 || effect.Filters.ExcludedSubtypes.Count > 0))
            ? "Uncalibrated room post-combat effect composition." : null;
        private static bool SupportedPayload(CardActionEffect effect, bool disallowedInDeployment) =>
            disallowedInDeployment && effect.Type == "AdjustAllStatusEffects" && effect.AllowPlayer && effect.AllowEnemy && effect.Value == 1 &&
                effect.StatusAdjustmentFilter == 1 && effect.Statuses.Count == 0 ||
            !disallowedInDeployment && effect.Type == "AddStatus" && !effect.AllowPlayer && effect.AllowEnemy && effect.Value == 0 &&
                effect.Statuses.Count == 1 && effect.Statuses[0].Id == "debuff" && effect.Statuses[0].Stacks == 3;
    }
}

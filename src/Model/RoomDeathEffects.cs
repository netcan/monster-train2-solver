using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RoomDeathEffectRule
    {
        public IReadOnlyList<CardActionEffect> Effects { get; }
        public IReadOnlyList<CardUpgradeMaskRule> Filters { get; }
        public bool DisallowedInDeployment { get; }
        public RoomDeathEffectRule(IReadOnlyList<CardActionEffect> effects, IReadOnlyList<CardUpgradeMaskRule> filters,
            bool disallowedInDeployment)
        { Effects = Array.AsReadOnly(effects.ToArray()); Filters = Array.AsReadOnly(filters.ToArray()); DisallowedInDeployment = disallowedInDeployment; }
    }
    public static class RoomDeathEffectModel
    {
        internal static bool RequiresBirthWorld(CombatContext? context) => context?.CardRegistry?.Any(card =>
            card.DataId == "e1a39d88-4a77-4399-98b2-0b81f44fe6a8") == true;
        public static string? Validate(RoomDeathEffectRule rule) => rule.DisallowedInDeployment ||
            RoomCapacityModel.ValidateCharacterFilters(rule.Filters) != null || rule.Effects.Count != 1 ||
            rule.Effects.Any(effect => effect.Type != "SpawnOrStatus" || effect.Target != "Room" ||
                !effect.AllowEnemy || effect.AllowPlayer || effect.Value != 1 || effect.Statuses.Count != 1 ||
                effect.Statuses[0].Id != "horde" || effect.Statuses[0].Stacks != 1 ||
                effect.SpawnFallbackUnit?.AssetKey != "MonsterFunguy")
            ? "Uncalibrated room unit-death effect." : null;
        internal static bool Matches(RoomDeathEffectRule rule, CombatUnit unit, CombatContext? context) =>
            !unit.IsPyre && rule.Filters.All(filter => CardUpgradeMaskModel.FilterCharacter(filter,
                new CardUpgradeMaskCharacter(unit.Subtypes, (unit.StatusRegistry ?? unit.Statuses).Select(status => status.Id).ToArray(), unit.Size),
                context?.Relics?.Any(relic => relic.EffectTypes.Contains("RelicEffectMonstersAreAllSubtypes")) == true));
    }
}

using System.Linq;

namespace MonsterTrain2Poju.Model
{
    // Original Ring 4 miniboss: lower the enemy tower, then retreat behind it.
    // Admission stays narrow until other triggered movement has native evidence.
    internal static class HarvesterBirthModel
    {
        internal static bool IsTrigger(CombatUnit unit, CombatTrigger trigger) =>
            unit.AssetKey == "Boss_Level4_Harvester" && unit.Team == CombatTeam.Enemy &&
            trigger.Kind == "OnSpawnNotFromCard" && !trigger.Once && trigger.FireCount == 1 &&
            trigger.Conditions == null && (trigger.Origin == null || trigger.Origin is { UpgradeId: "", EquipmentCardId: 0,
                IsFromEquipment: false, OnlyIfEquipped: false }) && trigger.TriggerAtThreshold.GetValueOrDefault() == 0 &&
            trigger.Effects.Count == 2 && trigger.Effects[0].Type == "CardEffectBump" &&
            trigger.Effects[0].Action is { Type: "Bump", Target: "Tower", Value: -2, Range: null,
                AllowEnemy: true, AllowPlayer: false, UseCardTraits: false } bump && Plain(bump) &&
            trigger.Effects[1].Type == "CardEffectFloorRearrange" &&
            trigger.Effects[1].Action is { Type: "FloorRearrange", Target: "Self", Value: 1, Range: null,
                AllowEnemy: true, AllowPlayer: false, UseCardTraits: false } rearrange && Plain(rearrange);

        private static bool Plain(CardActionEffect effect) => effect.Statuses.Count == 0 && effect.Upgrade == null &&
            (effect.Filters == null || effect.Filters.Health == "Both") &&
            effect.Filters?.RequiredStatuses.Count is null or 0 && effect.Filters?.ExcludedStatuses.Count is null or 0 &&
            effect.Filters?.ExcludedSubtypes.Count is null or 0 && effect.Filters?.IgnoreBosses != true &&
            string.IsNullOrEmpty(effect.Filters?.Subtype) && effect.RequiredGold == 0;
    }
}

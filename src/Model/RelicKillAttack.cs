using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicKillAttackModel
    {
        internal const string DataId = "4190af4b-a32f-42ab-acda-8f0d4f50c3a5";
        internal const string AssetKey = "AddAttackOnKill";
        internal const string UpgradeId = "a8bf7a64-1a75-405e-9e43-8afa7ee31106";
        internal static bool IsOwner(CombatRelicState owner) => owner.DataId == DataId;

        internal static string? Validate(CombatRelicState owner)
        {
            RelicUnitUpgradeTrigger? rule = owner.UnitUpgradeTriggers?.FirstOrDefault();
            CardUpgradeModifier? upgrade = rule?.Upgrade;
            CardStatModifier? stats = upgrade?.Stats;
            return owner.DataId == DataId && owner.AssetKey == AssetKey && owner.IsCovenant == false &&
                owner.DisallowedInPlacementPhase == false && owner.EffectTypes.SequenceEqual(new[] { RelicModel.ModifyCharacterAttackDamage }) &&
                owner.UnitUpgradeTriggers?.Count == 1 && rule != null && rule.EffectIndex == 0 && rule.Trigger == "OnKill" && rule.SourcePlayer && !rule.SourceEnemy &&
                rule.SubtypeIsNone && !rule.ShouldRemove && rule.UpgradeLifetime == RelicUnitUpgradeTriggerModel.UntilEndOfBattle &&
                upgrade != null && upgrade.DataId == UpgradeId && upgrade.AssetKey == "AddAttackOnKillTriggerUpgrade" &&
                !upgrade.Unique && !upgrade.RemoveOnDiscard && !upgrade.ExcludeFromClones &&
                upgrade.UnhealedHealth == 0 && upgrade.DamageBuff == 0 && upgrade.Statuses.Count == 0 &&
                upgrade.ExternalInteractions.Count == 0 && stats != null && stats.Damage == 3 && stats.Health == 0 &&
                stats.Cost == 0 && stats.Heal == 0 && stats.Size == 0 && stats.XCost == 0 &&
                stats.EquipmentLimit == 0 && stats.UpgradeSlotCount == 0
                ? null : "Missing or changed original AddAttackOnKill definition.";
        }

        internal static bool Eligible(CombatUnit actor, string trigger) => trigger == "OnKill" &&
            actor.Team == CombatTeam.Player && actor.Health > 0 && actor.DeathState?.IsDestroyed != true &&
            actor.Modifiers != null && !actor.Modifiers.IsClone;
    }
}

using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class ArkionAttackTrial
    {
        internal const string DataId = "7998304e-4d5b-437b-8652-3d3d7f125163";
        internal const string AssetKey = "Enemy_Boss_Level3Rage_TitanTrial";
        internal static bool IsOwner(CombatRelicState owner) => owner.DataId == DataId;

        internal static string? Validate(CombatRelicState owner)
        {
            RelicUnitUpgradeTrigger? rule = owner.UnitUpgradeTriggers?.FirstOrDefault();
            CardUpgradeModifier? upgrade = rule?.Upgrade;
            CardStatModifier? stats = upgrade?.Stats;
            return IsOwner(owner) && owner.AssetKey == AssetKey && owner.IsCovenant == false &&
                owner.DisallowedInPlacementPhase == false && owner.EffectTypes.SequenceEqual(new[] { RelicModel.ModifyCharacterAttackDamage }) &&
                owner.UnitUpgradeTriggers?.Count == 1 && rule != null && rule.EffectIndex == 0 && rule.Trigger == "OnSpawn" &&
                !rule.SourcePlayer && rule.SourceEnemy && !rule.SubtypeIsNone &&
                rule.RequiredSubtype == "SubtypesData_BigBossT1_50952308-e0f3-4acb-b0cc-e0d662b868b8" &&
                !rule.ShouldRemove && rule.UpgradeLifetime == RelicUnitUpgradeTriggerModel.UntilEndOfBattle &&
                upgrade != null && upgrade.DataId == "7a2eaa0b-56b1-436b-92a2-aefb01833369" &&
                upgrade.AssetKey == "Boss_Level3_Rage_AtkUpgrade" && !upgrade.Unique && !upgrade.RemoveOnDiscard &&
                !upgrade.ExcludeFromClones && upgrade.UnhealedHealth == 0 && upgrade.DamageBuff == 0 &&
                upgrade.Statuses.Count == 0 && upgrade.ExternalInteractions.Count == 0 && stats != null &&
                stats.Damage == 5 && stats.Health == 0 && stats.Cost == 0 && stats.Heal == 0 && stats.Size == 0 &&
                stats.XCost == 0 && stats.EquipmentLimit == 0 && stats.UpgradeSlotCount == 0
                ? null : "Missing or changed original Arkion attack trial definition.";
        }

        internal static bool Eligible(CombatUnit actor, string trigger) => trigger == "OnSpawn" &&
            actor.Team == CombatTeam.Enemy && actor.Health > 0 && actor.DeathState?.IsDestroyed != true &&
            actor.Modifiers != null && !actor.Modifiers.IsClone;
    }
}

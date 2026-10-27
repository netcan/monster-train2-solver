using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class CardUnderlegionTroopUpgradeModel
    {
        internal const string DataId = "6ab69569-15d2-4902-a934-539858021760";
        internal const string AssetKey = "Underlegion1RegenOnHarvest1";
        internal const string AuraUpgradeId = "9cb768b3-efb9-480c-819b-33882cf0908d";
        internal const string AuraUpgradeKey = "Underlegion1RegenOnHarvest1_Upgrade";
        internal const string SecondDataId = "6bf3f96e-21b0-4afa-b8e4-b4f0725e0731";
        internal const string SecondAssetKey = "Underlegion1RegenOnHarvest2";
        internal const string SecondAuraUpgradeId = "4eeaeacc-03be-4c25-ae9e-e4f23cf50d3d";
        internal const string SecondAuraUpgradeKey = "Underlegion1RegenOnHarvest2_Upgrade";
        internal const string ThirdDataId = "0e5c13f8-0b55-4239-bc16-9961e2900f12";
        internal const string ThirdAssetKey = "Underlegion1RegenOnHarvest3";
        internal const string ThirdAuraUpgradeId = "e4217d7c-8301-4fcf-ab8b-a96b8c921a0c";
        internal const string ThirdAuraUpgradeKey = "Underlegion1RegenOnHarvest3_Upgrade";
        internal const string NoneSubtype = "SubtypesData_None";

        internal static bool Known(CardUpgradeModifier upgrade) =>
            (upgrade.DataId == DataId && upgrade.AssetKey == AssetKey && upgrade.Stats.Damage == 20 && upgrade.Stats.Health == 5 ||
             upgrade.DataId == SecondDataId && upgrade.AssetKey == SecondAssetKey && upgrade.Stats.Damage == 40 && upgrade.Stats.Health == 35 ||
             upgrade.DataId == ThirdDataId && upgrade.AssetKey == ThirdAssetKey && upgrade.Stats.Damage == 70 && upgrade.Stats.Health == 85) &&
            upgrade.ExternalInteractions.SequenceEqual(new[] { CardCorruptionAttackUpgradeModel.Guard }) &&
            !upgrade.Unique && !upgrade.RemoveOnDiscard && !upgrade.ExcludeFromClones && upgrade.UnhealedHealth == 0 &&
            upgrade.DamageBuff == 0 && !upgrade.RestrictSizeToRoomCapacity && !upgrade.MagicPowerTraitScalingOnly &&
            upgrade.Statuses.Count == 0 && upgrade.AbilityUpgrade == null && !upgrade.DoNotReplaceExistingAbility &&
            upgrade.CloneDamageBase == upgrade.Stats.Damage && upgrade.CloneHealBase == null &&
            new[] { "Cost", "Heal", "Size", "XCost", "EquipmentLimit", "UpgradeSlotCount" }
                .All(stat => upgrade.Stats.Value(stat) == 0) &&
            upgrade.TriggerUpgrades is { Count: 1 } && upgrade.TriggerUpgrades[0].Kind == "OnAnyHeroDeathOnFloor" &&
            (upgrade.DataId != ThirdDataId || upgrade.TriggerUpgrades[0] is
                { Once: false, IgnoreSilence: false, FireCount: 1, SkipDuringDeployment: false,
                  Effects: [{ Type: "CardEffectSpawnMonsterOrAddStatusEffect", Value: 3, Action:
                    { Type: "SpawnOrStatus", Target: "Room", Value: 3, AllowPlayer: true, AllowEnemy: false, Range: null,
                      Statuses: [{ Id: "horde", Stacks: 3 }], Filters.Subtype: "SubtypesData_Horde",
                      SpawnFallbackUnit: { AssetKey: "MonsterFunguy", BaseAttack: 3, Health: 4 } } }] }) &&
            upgrade.Lifecycle is { AddedTraits.Count: 0, RemovedRuntimeTypes.Count: 0, Triggers.Count: 0,
                ReplacedAssets.Count: 0, TraitsModified.Count: 0 };

        internal static (string Id, string AssetKey, int Health) AuraDefinition(string upgradeId) => upgradeId switch
        {
            DataId => (AuraUpgradeId, AuraUpgradeKey, 1),
            SecondDataId => (SecondAuraUpgradeId, SecondAuraUpgradeKey, 2),
            ThirdDataId => (ThirdAuraUpgradeId, ThirdAuraUpgradeKey, 3),
            _ => throw new ArgumentException("Uncalibrated Underlegion champion upgrade.", nameof(upgradeId))
        };

        // This exact native champion upgrade inherits one room modifier at birth.
        // Live-unit upgrade/removal remains guarded by its external interaction.
        internal static TroopAuraState? BirthRule(TroopAuraState? original, CardUpgradeModifier[] upgrades)
        {
            if (original != null || upgrades.Count(Known) != 1) return original;
            var aura = AuraDefinition(upgrades.Single(Known).DataId);
            string auraId = aura.Id;
            string auraKey = aura.AssetKey;
            var stats = new CardStatModifier(health: aura.Health);
            var metadata = new CardUpgradeMaskMetadata(Array.Empty<UpgradeMaskStatus>(), false, false, false, false);
            var lifecycle = new CardLifecycleUpgrade(new CardMaskUpgrade(auraId, stats,
                Array.Empty<UpgradeMaskStatus>(), false, false, false, false), auraKey, false, false,
                Array.Empty<CardTraitValue>(), Array.Empty<string>(), false, Array.Empty<string>(),
                Array.Empty<string>(), Array.Empty<CardLifecycleTrigger>());
            var upgrade = new CardUpgradeModifier(auraId, auraKey, stats, Array.Empty<CombatStatus>(),
                false, false, false, 0, 0, Array.Empty<string>(), maskMetadata: metadata,
                copyRemoveOnDiscard: false, copyUnique: false, copyExcludeFromClones: false, lifecycle: lifecycle);
            return new TroopAuraState(NoneSubtype, upgrade, Array.Empty<CombatStatus>(), Array.Empty<int>());
        }
    }
}

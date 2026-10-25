using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class CardCorruptionAttackUpgradeModel
    {
        internal const string DataId = "1b2642b2-3ae9-46e6-b7cf-f0fd3c4a53f8";
        internal const string AssetKey = "AddWurmScaleDamage";
        internal const string Type = "RoomStateSelfDamagePerCorruptionModifier";
        internal const string Guard = "Upgrade room modifiers";
        internal static bool Known(CardUpgradeModifier upgrade) => upgrade.DataId == DataId && upgrade.AssetKey == AssetKey &&
            upgrade.ExternalInteractions.SequenceEqual(new[] { Guard }) && !upgrade.Unique && !upgrade.RemoveOnDiscard &&
            !upgrade.ExcludeFromClones && upgrade.UnhealedHealth == 0 && upgrade.DamageBuff == 0 &&
            !upgrade.RestrictSizeToRoomCapacity && !upgrade.MagicPowerTraitScalingOnly &&
            upgrade.Statuses.Count == 0 && upgrade.TriggerUpgrades?.Count is not > 0 && upgrade.AbilityUpgrade == null &&
            new[] { "Damage", "Health", "Cost", "Heal", "Size", "XCost", "EquipmentLimit", "UpgradeSlotCount" }
                .All(stat => upgrade.Stats.Value(stat) == 0) && upgrade.Lifecycle != null &&
            upgrade.Lifecycle.AddedTraits.Count == 0 && upgrade.Lifecycle.RemovedRuntimeTypes.Count == 0 &&
            upgrade.Lifecycle.Triggers.Count == 0 && upgrade.Lifecycle.ReplacedAssets.Count == 0;

        // Only unit-card birth consumes this known upgrade. Live-unit upgrade
        // APIs retain the captured external-interaction guard until calibrated.
        internal static IReadOnlyList<UnitSelfAttackRule>? BirthRules(IReadOnlyList<UnitSelfAttackRule>? original,
            IEnumerable<CardUpgradeModifier> upgrades)
        {
            var added = upgrades.Where(Known).Select(_ => new UnitSelfAttackRule(Type, 5)).ToArray();
            return added.Length == 0 ? original : (original ?? Array.Empty<UnitSelfAttackRule>()).Concat(added).ToArray();
        }
    }
}

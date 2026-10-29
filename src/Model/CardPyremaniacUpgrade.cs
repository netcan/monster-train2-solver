using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class CardPyremaniacUpgradeModel
    {
        internal const string DataId = "4f13682e-4612-4754-9c36-e1539daec4f8";
        internal const string AssetKey = "Pyremaniac1";
        internal static bool Known(CardUpgradeModifier upgrade) => upgrade.DataId == DataId && upgrade.AssetKey == AssetKey &&
            upgrade.ExternalInteractions.SequenceEqual(new[] { CardCorruptionAttackUpgradeModel.Guard }) &&
            !upgrade.Unique && !upgrade.RemoveOnDiscard && !upgrade.ExcludeFromClones && upgrade.UnhealedHealth == 0 &&
            upgrade.DamageBuff == 0 && !upgrade.RestrictSizeToRoomCapacity && !upgrade.MagicPowerTraitScalingOnly &&
            upgrade.Statuses.Count == 0 && upgrade.TriggerUpgrades?.Count is not > 0 && upgrade.AbilityUpgrade == null &&
            upgrade.Stats.Damage == 5 && upgrade.Stats.Health == 10 &&
            new[] { "Cost", "Heal", "Size", "XCost", "EquipmentLimit", "UpgradeSlotCount" }.All(stat => upgrade.Stats.Value(stat) == 0) &&
            upgrade.Lifecycle != null && upgrade.Lifecycle.AddedTraits.Count == 0 && upgrade.Lifecycle.RemovedRuntimeTypes.Count == 0 &&
            upgrade.Lifecycle.Triggers.Count == 0 && upgrade.Lifecycle.ReplacedAssets.Count == 0;

        internal static IReadOnlyList<UnitRoomStatusRule>? BirthRules(IReadOnlyList<UnitRoomStatusRule>? original,
            IEnumerable<CardUpgradeModifier> upgrades)
        {
            var added = upgrades.Where(Known).Select(_ => new UnitRoomStatusRule(UnitRoomStatusModel.StackModifier, "pyregel", 1)).ToArray();
            return added.Length == 0 ? original : (original ?? Array.Empty<UnitRoomStatusRule>()).Concat(added).ToArray();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class CardSentientDrawUpgradeModel
    {
        internal const string DataId = "f8032f84-d804-4b2e-ad51-679f949f5eb2";
        internal const string AssetKey = "SentientDrawOnRevenge";
        internal const string SecondDataId = "09621571-ddd8-4ee9-bf99-55c846d565bd";
        internal const string SecondAssetKey = "SentientDrawOnRevenge2";
        internal static bool KnownIdentity(string id, string asset) => id == DataId && asset == AssetKey ||
            id == SecondDataId && asset == SecondAssetKey;
        internal static bool Known(CardUpgradeModifier upgrade) => KnownIdentity(upgrade.DataId, upgrade.AssetKey) &&
            upgrade.ExternalInteractions.SequenceEqual(new[] { CardCorruptionAttackUpgradeModel.Guard }) &&
            !upgrade.Unique && !upgrade.RemoveOnDiscard && !upgrade.ExcludeFromClones && upgrade.UnhealedHealth == 0 &&
            upgrade.DamageBuff == 0 && !upgrade.RestrictSizeToRoomCapacity && !upgrade.MagicPowerTraitScalingOnly &&
            upgrade.Statuses.Count == 0 && upgrade.AbilityUpgrade == null &&
            upgrade.Stats.Health == (upgrade.DataId == SecondDataId ? 70 : 30) &&
            new[] { "Damage", "Cost", "Heal", "Size", "XCost", "EquipmentLimit", "UpgradeSlotCount" }
                .All(stat => upgrade.Stats.Value(stat) == 0) &&
            upgrade.TriggerUpgrades is { Count: 1 } && upgrade.TriggerUpgrades[0].Kind == "OnHit" &&
            upgrade.Lifecycle != null && upgrade.Lifecycle.AddedTraits.Count == 0 &&
            upgrade.Lifecycle.RemovedRuntimeTypes.Count == 0 && upgrade.Lifecycle.Triggers.Count == 0 &&
            upgrade.Lifecycle.ReplacedAssets.Count == 0;

        internal static IReadOnlyList<UnitRoomResourceRule>? BirthRules(IReadOnlyList<UnitRoomResourceRule>? original,
            IEnumerable<CardUpgradeModifier> upgrades)
        {
            var added = upgrades.Where(Known).Select(_ => new UnitRoomResourceRule(UnitRoomResourceModel.Draw, 1, false)).ToArray();
            return added.Length == 0 ? original : (original ?? Array.Empty<UnitRoomResourceRule>()).Concat(added).ToArray();
        }
    }
}

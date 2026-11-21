using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class CardShardtailImpCostModel
    {
        internal const string CardId = "7cff7749-0803-402c-8a9a-965222c24b4b";
        internal const string UnitKey = "MonsterChampionHellhornedShardtail";
        internal const string UpgradeId = "2c5a6dd2-3320-4d90-8fd8-af32a4f913e7";
        internal const string UpgradeKey = "ShardtailGainImps";
        internal const string Subtype = "SubtypesData_Imp_0f9b989f-15b5-4b16-8378-5d8ed8691e7c";
        private const string GeneratedImp = "d76be2d0-f851-4d43-bdc0-7c7a7779724f";

        private static bool Known(CardUpgradeModifier upgrade) => upgrade.DataId == UpgradeId && upgrade.AssetKey == UpgradeKey &&
            upgrade.ExternalInteractions.SequenceEqual(new[] { CardCorruptionAttackUpgradeModel.Guard }) &&
            !upgrade.Unique && !upgrade.RemoveOnDiscard && !upgrade.ExcludeFromClones && upgrade.UnhealedHealth == 0 &&
            upgrade.DamageBuff == 0 && !upgrade.RestrictSizeToRoomCapacity && !upgrade.MagicPowerTraitScalingOnly &&
            upgrade.Statuses.Count == 0 && upgrade.AbilityUpgrade == null &&
            upgrade.Stats.Damage == 20 && upgrade.Stats.Health == 5 &&
            new[] { "Cost", "Heal", "Size", "XCost", "EquipmentLimit", "UpgradeSlotCount" }.All(stat => upgrade.Stats.Value(stat) == 0) &&
            upgrade.Lifecycle is { } lifecycle && lifecycle.AddedTraits.Count == 0 && lifecycle.RemovedRuntimeTypes.Count == 0 &&
            lifecycle.Triggers.Count == 0 && lifecycle.ReplacedAssets.Count == 0 &&
            upgrade.TriggerUpgrades is { Count: 1 } triggers && triggers[0].Kind == "PostCombat" && triggers[0].SkipDuringDeployment == true &&
            triggers[0].Effects.Count == 1 && triggers[0].Effects[0] is { Type: "CardEffectAddBattleCard", Generation: { } generation } &&
            generation.Destination == "HandPile" && generation.Count == 1 &&
            generation.Pool.Count == 1 && generation.Pool[0].DataId == GeneratedImp;

        internal static CardUpgradeModifier? PermanentSource(CardInstanceState instance)
        {
            if (instance.DataId != CardId || instance.MaskDescriptor?.Purified == true ||
                instance.Temporary.Upgrades.Any(upgrade => upgrade.DataId == UpgradeId)) return null;
            var upgrades = instance.Permanent.Upgrades.Where(upgrade => upgrade.DataId == UpgradeId).ToArray();
            return upgrades.Length == 1 && Known(upgrades[0]) ? upgrades[0] : null;
        }

        internal static IReadOnlyList<UnitRoomResourceRule>? BirthRules(IReadOnlyList<UnitRoomResourceRule>? original,
            CardUpgradeModifier? source) => source == null ? original : (original ?? Array.Empty<UnitRoomResourceRule>())
                .Concat(new[] { new UnitRoomResourceRule(UnitRoomResourceModel.SubtypeCost, -1, false, Subtype) }).ToArray();
    }
}

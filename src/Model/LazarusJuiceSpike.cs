using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class LazarusJuiceSpikeModel
    {
        internal const string DataId = "7168ed36-de03-41db-a36f-79b54b121341";
        internal static bool Known(CardInstanceState card)
        {
            string[] stats = { "Damage", "Health", "Cost", "Heal", "Size", "XCost", "EquipmentLimit", "UpgradeSlotCount" };
            if (card.DataId != DataId || card.MaskDescriptor == null || card.ExternalInteractions.Count != 0 ||
                card.Permanent.Upgrades.Count != 1 || card.Temporary.Upgrades.Count > 2 ||
                card.Temporary.Upgrades.Any(upgrade => !KnownConduit(upgrade)) ||
                card.Temporary.Upgrades.Select(upgrade => upgrade.DataId).Distinct(StringComparer.Ordinal).Count() != card.Temporary.Upgrades.Count ||
                stats.Any(stat => card.Permanent.Offsets.Value(stat) != 0 || card.Temporary.Offsets.Value(stat) != 0) ||
                card.DamageScalingTraits?.Count is > 0 || card.UnitUpgradeScalingTraits?.Count is > 0 ||
                card.CapacityScalingTraits?.Count is > 0 || card.ForgeCountScalingTraits?.Count is > 0 ||
                card.TrainHealScalingTraits?.Count is > 0 || card.StatusScalingTraits?.Count != 1) return false;
            CardUpgradeModifier upgrade = card.Permanent.Upgrades[0];
            if (upgrade.DataId != CardJuiceModel.Upgrade || upgrade.AssetKey != "TraitJuice" ||
                stats.Any(stat => upgrade.Stats.Value(stat) != 0) || upgrade.Statuses.Count != 0 ||
                upgrade.ExternalInteractions.Count != 0 || upgrade.UnhealedHealth != 0 || upgrade.DamageBuff != 0 ||
                upgrade.AbilityUpgrade != null || upgrade.TriggerUpgrades?.Count is > 0 ||
                upgrade.Lifecycle is not { AddedTraits.Count: 1, RemovedRuntimeTypes.Count: 0, Triggers.Count: 0 } lifecycle ||
                lifecycle.AddedTraits[0].RuntimeType != CardJuiceModel.Trait || !Ordinary(lifecycle.AddedTraits[0], 2)) return false;
            var traits = CardExhaustModel.RefreshTraits(card).CombinedTraits!;
            var scaling = card.StatusScalingTraits[0];
            return traits.Count == 3 && traits[0].RuntimeType == "CardTraitExhaustState" && Ordinary(traits[0], 1) &&
                traits[1].RuntimeType == "CardTraitScalingAddStatusEffect" && Ordinary(traits[1], 1) &&
                traits[2].RuntimeType == CardJuiceModel.Trait && Ordinary(traits[2], 2) &&
                scaling.Query is { Type: "PlayedCost", Duration: "ThisTurn", VariableCost: true, SourceRawCost: 0,
                    TargetTeam: "None", CurrentCostSourceIsSpell: true, CurrentCostCardModifiersOnly: false,
                    SubtypeIsNone: true, SpecificCardDataId: null, TypeMatchDataIds: null, StatusIds: null } &&
                scaling.Query.SubtypeMatchDataIds?.Count is not > 0 && scaling.StacksPerStat == 1 &&
                !scaling.OnlyWhenSourceZero && scaling.Filter == 0 && scaling.StatusIds.SequenceEqual(new[] { "undying" });
        }
        internal static bool KnownConduit(CardUpgradeModifier upgrade) =>
            upgrade.DataId.StartsWith("conduit:", StringComparison.Ordinal) &&
            int.TryParse(upgrade.DataId.Substring(8), out int owner) && owner > 0 &&
            upgrade.DataId == "conduit:" + owner && upgrade.AssetKey == "ConduitUpgrade" &&
            upgrade.Stats.Damage >= 0 && upgrade.Stats.Heal == upgrade.Stats.Damage &&
            new[] { "Health", "Cost", "Size", "XCost", "EquipmentLimit", "UpgradeSlotCount" }.All(stat => upgrade.Stats.Value(stat) == 0) &&
            upgrade.Statuses.Count == 0 && upgrade.ExternalInteractions.Count == 0 &&
            !upgrade.RemoveOnDiscard && !upgrade.Unique && !upgrade.ExcludeFromClones &&
            upgrade.UnhealedHealth == 0 && upgrade.DamageBuff == 0 && !upgrade.RestrictSizeToRoomCapacity &&
            !upgrade.MagicPowerTraitScalingOnly && upgrade.CloneDamageBase == null && upgrade.CloneHealBase == null &&
            upgrade.EquipmentSourceCardId == null && upgrade.EquipmentSourceUpgradeIndex == null &&
            upgrade.TriggerUpgrades?.Count is not > 0 && upgrade.AbilityUpgrade == null && !upgrade.DoNotReplaceExistingAbility &&
            upgrade.CopyRemoveOnDiscard != true && upgrade.CopyUnique != true && upgrade.CopyExcludeFromClones != true &&
            upgrade.SharedReferenceId == null &&
            upgrade.MaskMetadata is { Statuses.Count: 0, HasIcon: true, HideIcon: true, RegionRun: false, UnitAbility: false } &&
            upgrade.Lifecycle is { AssetKey: "ConduitUpgrade", AddedTraits.Count: 0, RemovedRuntimeTypes.Count: 0,
                Triggers.Count: 0, TraitsModified.Count: 0, ReplacedAssets.Count: 0, Unique: false, RemoveOnDiscard: false,
                AvoidClobbering: false, OriginalDamage: null, OriginalHeal: null } lifecycle &&
            lifecycle.Values.DataId == upgrade.DataId && lifecycle.Values.Statuses.Count == 0 &&
            new[] { "Damage", "Health", "Cost", "Heal", "Size", "XCost", "EquipmentLimit", "UpgradeSlotCount" }
                .All(stat => lifecycle.Values.Stats.Value(stat) == upgrade.Stats.Value(stat));
        private static bool Ordinary(CardTraitValue trait, int parameter) => trait.DeclaredName == trait.RuntimeType &&
            trait.DefinitionParamInt == parameter && trait.ParamInt == parameter && trait.Removable &&
            trait.DefinitionRemovable == true && trait.StackMode == 0 && !trait.TemporaryReplacement;
    }
}

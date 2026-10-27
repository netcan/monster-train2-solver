using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class EchoRemnantPactModel
    {
        internal const string DataId = "6534ff5b-d036-4b2f-ac21-70ee4d68700e";
        private static readonly string[] Stats = { "Damage", "Health", "Cost", "Heal", "Size", "XCost", "EquipmentLimit", "UpgradeSlotCount" };
        internal static bool Known(CardInstanceState card, IReadOnlyList<CardActionEffect> effects)
        {
            if (card.DataId != DataId || card.MaskDescriptor == null || card.ExternalInteractions.Count != 0 ||
                card.Permanent.Upgrades.Count != 0 || card.Temporary.Upgrades.Count != 1 ||
                !CardJuiceModel.KnownTitanCurse(card.Temporary.Upgrades[0]) ||
                Stats.Any(stat => card.Permanent.Offsets.Value(stat) != 0 || card.Temporary.Offsets.Value(stat) != 0) ||
                card.DamageScalingTraits?.Count is > 0 || card.StatusScalingTraits?.Count is > 0 ||
                card.UnitUpgradeScalingTraits?.Count is > 0 || card.CapacityScalingTraits?.Count is > 0 ||
                card.ForgeCountScalingTraits?.Count is > 0 || card.TrainHealScalingTraits?.Count is > 0 ||
                !CardBranchMaskModel.Resolve(card).Traits.SequenceEqual(new[] { "CardTraitExhaustState" }) || effects.Count != 1)
                return false;
            var traits = CardExhaustModel.RefreshTraits(card).CombinedTraits;
            if (traits?.Count != 1 || traits[0] is not { RuntimeType: "CardTraitExhaustState", DeclaredName: "CardTraitExhaustState",
                ParamInt: 0, DefinitionParamInt: 0, Removable: true, DefinitionRemovable: true, StackMode: 0,
                TemporaryReplacement: false, ParameterUpgradeCastEffects.Count: 0 }) return false;
            if (effects[0] is not { Type: "UnitUpgrade", Target: "DropTargetCharacter", Value: 0,
                AllowEnemy: false, AllowPlayer: true, Statuses.Count: 0, ScaleUpgrade: false,
                Lifetime: "TemporaryUntilEndOfBattle", Upgrade: not null, Generation: null, Range: null } effect) return false;
            var upgrade = effect.Upgrade;
            return upgrade.DataId == "e90daf43-a1c5-4fcb-a012-b3a2c0064fcb" && upgrade.AssetKey == "RemnantPact" &&
                Stats.All(stat => upgrade.Stats.Value(stat) == 0) && upgrade.Statuses.Count == 1 &&
                upgrade.Statuses[0].Id == "endless" && upgrade.Statuses[0].Stacks == 1 &&
                upgrade.ExternalInteractions.Count == 0 && upgrade.TriggerUpgrades?.Count is not > 0 &&
                upgrade.AbilityUpgrade == null && upgrade.UnhealedHealth == 0 && upgrade.DamageBuff == 0 &&
                upgrade.Lifecycle is { AddedTraits.Count: 0, RemovedRuntimeTypes.Count: 0, Triggers.Count: 0 };
        }
    }
}

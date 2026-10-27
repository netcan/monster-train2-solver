using System.Linq;
using System.Collections.Generic;

namespace MonsterTrain2Poju.Model
{
    internal static class EchoJustCauseModel
    {
        internal static bool Known(CardInstanceState card, IReadOnlyList<CardActionEffect> effects)
        {
            string[] stats = { "Damage", "Health", "Cost", "Heal", "Size", "XCost", "EquipmentLimit", "UpgradeSlotCount" };
            return card.DataId == "c8b39244-83c1-4eb3-b474-8898226d4280" && card.MaskDescriptor != null && card.ExternalInteractions.Count == 0 &&
                card.Permanent.Upgrades.Count == 0 && card.Temporary.Upgrades.Count == 1 &&
                CardJuiceModel.KnownTitanCurse(card.Temporary.Upgrades[0]) &&
                stats.All(stat => card.Permanent.Offsets.Value(stat) == 0 && card.Temporary.Offsets.Value(stat) == 0) &&
                CardBranchMaskModel.Resolve(card).Traits.Count == 0 &&
                card.DamageScalingTraits?.Count is not > 0 && card.StatusScalingTraits?.Count is not > 0 &&
                card.UnitUpgradeScalingTraits?.Count is not > 0 && card.CapacityScalingTraits?.Count is not > 0 &&
                card.ForgeCountScalingTraits?.Count is not > 0 && card.TrainHealScalingTraits?.Count is not > 0 &&
                effects.Count == 2 && effects[0] is { Type: "FloorRearrange", Target: "DropTargetCharacter", Value: 0,
                    AllowEnemy: true, AllowPlayer: true, Statuses.Count: 0, Upgrade: null, Generation: null, Range: null } &&
                effects[1] is { Type: "AddStatus", Target: "LastTargetedCharacters", Value: 0, AllowEnemy: true, AllowPlayer: true,
                    Statuses.Count: 1, Upgrade: null, Generation: null, Range: null } &&
                effects[1].Statuses[0].Id == "valor" && effects[1].Statuses[0].Stacks == 2;
        }
    }
}

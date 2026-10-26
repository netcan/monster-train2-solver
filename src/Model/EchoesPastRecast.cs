using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class EchoesPastRecastModel
    {
        internal const string DataId = "78996030-e8d1-4731-897e-ee4b809fa26c";
        private static readonly string[] Stats = { "Damage", "Health", "Cost", "Heal", "Size", "XCost", "EquipmentLimit", "UpgradeSlotCount" };
        internal static bool Known(CardPlayRule rule) => rule.DataId == DataId && rule.AssetKey == "EchoesOfThePast" &&
            rule.OnCastEffects.Count == 0 && rule.Effects.Count == 2 &&
            rule.Effects[0] is { Type: "UnitUpgrade", Target: "DropTargetCharacter", Value: 0, AllowEnemy: true, AllowPlayer: true,
                Range: null, ScaleUpgrade: false, Lifetime: "TemporaryUntilEndOfBattle", Upgrade: not null } effect &&
            effect.Upgrade.DataId == "2f55fb6e-402c-4a49-a4c8-e9e0dae98329" && effect.Upgrade.AssetKey == "WurmEchoesOfThePastAttack" &&
            Stats.All(stat => effect.Upgrade.Stats.Value(stat) == 0) &&
            effect.Upgrade is { Statuses.Count: 0, UnhealedHealth: 0, DamageBuff: 0, AbilityUpgrade: null, ExternalInteractions.Count: 0,
                Unique: false, RemoveOnDiscard: false, ExcludeFromClones: false, MagicPowerTraitScalingOnly: false } &&
            effect.Upgrade.TriggerUpgrades?.Count is not > 0 &&
            rule.Effects[1] is { Type: "AdjustCorruption", Target: "Room", Value: -1, CorruptionAdd: false,
                AllowEnemy: false, AllowPlayer: false, Range: null, Statuses.Count: 0 };

        internal static bool Known(CardInstanceState card)
        {
            if (card.DataId != DataId || card.MaskDescriptor == null || card.Permanent.Upgrades.Count != 0 ||
                card.Temporary.Upgrades.Count != 0 || card.UnitUpgradeScalingTraits?.Count != 1 ||
                Stats.Any(stat => card.Permanent.Offsets.Value(stat) != 0 || card.Temporary.Offsets.Value(stat) != 0)) return false;
            var traits = CardExhaustModel.RefreshTraits(card).CombinedTraits;
            var scaling = card.UnitUpgradeScalingTraits[0];
            return traits?.Count == 1 && traits[0] is { RuntimeType: "CardTraitScalingUpgradeUnitAttack",
                DeclaredName: "CardTraitScalingUpgradeUnitAttack", ParamInt: 8, DefinitionParamInt: 8,
                Removable: true, DefinitionRemovable: true, StackMode: 0, TemporaryReplacement: false } &&
                scaling.Stat == "Damage" && scaling.AmountPerStat == 8 && scaling.Restriction == 0 &&
                scaling.Query.Type == "CorruptionInTargetRoom" && scaling.Query.Duration == "ThisTurn";
        }

        internal static TrainCombatState RestoreCorruption(TrainCombatState source, int roomIndex, int corruption)
        {
            var context = source.Context!;
            return CardSpellModel.WithContext(source, context.WithRoomCapacities(context.RoomCapacities!.Select(room =>
                room.RoomIndex == roomIndex ? new RoomCapacityState(room.RoomIndex, room.PlayerMaximum, room.EnemyMaximum,
                    room.IsPyre, corruption, room.Attachments, room.MaxCorruption, room.PermanentCorruption,
                    room.AttachmentsAddedFromCards) : room).ToArray()));
        }
    }
}

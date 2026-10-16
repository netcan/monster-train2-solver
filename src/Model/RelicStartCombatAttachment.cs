using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicStartCombatAttachmentModel
    {
        internal const string Effect = "RelicEffectAddTrainRoomAttachmentOnStartOfCombat";
        internal const string RelicId = "2c97e1b3-d4bb-4133-9572-e582805c106f";
        internal const string RelicKey = "TrainRoomInstallFloor1AtStartOfCombat";
        internal const string UpgradeId = "05e215c6-600c-4ecb-a75e-c806dd596bcf";
        internal const string UpgradeKey = "FuelCar_Upgrade";

        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type == Effect).Select(item => item.index).ToArray();
            if (indices.Length == 0) return null;
            return relic.DataId == RelicId && relic.AssetKey == RelicKey && relic.EffectTypes.Count == 1 &&
                indices.SequenceEqual([0]) ? null : "Unmodeled start-of-combat room attachment relic identity or effect order.";
        }

        internal static string? Validate(CombatContext? context)
        {
            if (context?.Relics?.Any(relic => relic.EffectTypes.Contains(Effect)) != true) return null;
            if (context.RoomCapacities == null) return "Missing captured start-of-combat room attachment state.";
            RoomCapacityState? room = context.RoomCapacities.SingleOrDefault(item => item.RoomIndex == 0);
            RoomCapacityAttachmentState? attachment = room?.Attachments?.SingleOrDefault(item =>
                item.DataId == UpgradeId && item.AssetKey == UpgradeKey);
            if (room?.IsPyre != false || attachment == null || attachment.AttachmentType != 0 ||
                attachment.CapacityDelta != 0 || !attachment.AffectsHeroes || !attachment.AffectsMonsters ||
                !Guid.TryParse(attachment.AttachmentId, out Guid id) || id == Guid.Empty ||
                attachment.CharacterUpgrades?.Count != 1)
                return "Incomplete original floor-one FuelCar room attachment state.";
            CardUpgradeModifier upgrade = attachment.CharacterUpgrades[0];
            return upgrade.DataId == UpgradeId && upgrade.AssetKey == UpgradeKey && upgrade.ExternalInteractions.Count == 0 &&
                upgrade.TriggerUpgrades?.Count == 1 && upgrade.TriggerUpgrades[0].Kind == "OnDeath"
                    ? null : "Unmodeled original FuelCar room attachment upgrade payload.";
        }
    }
}

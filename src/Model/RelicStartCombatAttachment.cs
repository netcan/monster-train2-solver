using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicStartCombatAttachmentModel
    {
        internal const string Effect = "RelicEffectAddTrainRoomAttachmentOnStartOfCombat";
        internal const string RelicId = "2c97e1b3-d4bb-4133-9572-e582805c106f";
        internal const string RelicKey = "TrainRoomInstallFloor1AtStartOfCombat";
        internal const string Floor2RelicId = "b037ec04-6164-4cd8-8b9d-3bc420c29351";
        internal const string Floor2RelicKey = "TrainRoomInstallFloor2AtStartOfCombat";
        internal const string UpgradeId = "05e215c6-600c-4ecb-a75e-c806dd596bcf";
        internal const string UpgradeKey = "FuelCar_Upgrade";
        internal const string Floor2UpgradeId = "35178e4a-0880-440b-a929-de29de6e88f8";
        internal const string Floor2UpgradeKey = "SpellSanctum_Upgrade";

        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type == Effect).Select(item => item.index).ToArray();
            if (indices.Length == 0) return null;
            bool knownOwner = relic.DataId == RelicId && relic.AssetKey == RelicKey ||
                relic.DataId == Floor2RelicId && relic.AssetKey == Floor2RelicKey;
            return knownOwner && relic.EffectTypes.Count == 1 &&
                indices.SequenceEqual([0]) ? null : "Unmodeled start-of-combat room attachment relic identity or effect order.";
        }

        internal static string? Validate(CombatContext? context)
        {
            CombatRelicState[] owners = context?.Relics?.Where(relic => relic.EffectTypes.Contains(Effect)).ToArray() ??
                Array.Empty<CombatRelicState>();
            if (owners.Length == 0) return null;
            if (context == null || context.RoomCapacities == null) return "Missing captured start-of-combat room attachment state.";
            foreach (CombatRelicState owner in owners)
            {
                bool floor2 = owner.DataId == Floor2RelicId;
                int roomIndex = floor2 ? 1 : 0;
                string upgradeId = floor2 ? Floor2UpgradeId : UpgradeId;
                string upgradeKey = floor2 ? Floor2UpgradeKey : UpgradeKey;
                RoomCapacityState? room = context.RoomCapacities.SingleOrDefault(item => item.RoomIndex == roomIndex);
                RoomCapacityAttachmentState[] attachments = room?.Attachments?.Where(item =>
                    item.DataId == upgradeId && item.AssetKey == upgradeKey).ToArray() ?? Array.Empty<RoomCapacityAttachmentState>();
                if (room?.IsPyre != false || attachments.Length != 1)
                    return "Incomplete original start-of-combat room attachment state.";
                RoomCapacityAttachmentState attachment = attachments[0];
                if (attachment.AttachmentType != 0 || attachment.CapacityDelta != 0 ||
                    attachment.SpellCostDelta != (floor2 ? -1 : 0) || !attachment.AffectsHeroes ||
                    !attachment.AffectsMonsters || !Guid.TryParse(attachment.AttachmentId, out Guid id) || id == Guid.Empty ||
                    attachment.CharacterUpgrades?.Count != 1)
                    return "Incomplete original start-of-combat room attachment state.";
                CardUpgradeModifier upgrade = attachment.CharacterUpgrades[0];
                if (upgrade.DataId != upgradeId || upgrade.AssetKey != upgradeKey || upgrade.ExternalInteractions.Count != 0)
                    return "Unmodeled original start-of-combat room attachment upgrade payload.";
                if (floor2)
                {
                    if (upgrade.Stats.Damage != 0 || upgrade.Stats.Health != 0 || upgrade.Stats.Size != 0 ||
                        upgrade.Statuses.Count != 0 || upgrade.TriggerUpgrades?.Count > 0 || upgrade.AbilityUpgrade != null)
                        return "Unmodeled original SpellSanctum character-upgrade payload.";
                }
                else if (upgrade.TriggerUpgrades?.Count != 1 || upgrade.TriggerUpgrades[0].Kind != "OnDeath")
                    return "Unmodeled original FuelCar room attachment upgrade payload.";
            }
            return null;
        }
    }
}

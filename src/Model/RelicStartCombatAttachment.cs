using System;
using System.Collections.Generic;
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
        internal const string Floor3RelicId = "c474dd00-3725-46d6-b074-0be7f7687cdd";
        internal const string Floor3RelicKey = "TrainRoomInstallFloor3AtStartOfCombat";
        internal const string UpgradeId = "05e215c6-600c-4ecb-a75e-c806dd596bcf";
        internal const string UpgradeKey = "FuelCar_Upgrade";
        internal const string Floor2UpgradeId = "35178e4a-0880-440b-a929-de29de6e88f8";
        internal const string Floor2UpgradeKey = "SpellSanctum_Upgrade";
        internal const string Floor3UpgradeId = "f93ac9f7-45d2-47a4-a159-a1b326bd047f";
        internal const string Floor3UpgradeKey = "FightClub";
        internal const string InfernoMutatorId = "a5b3592e-e0ee-457d-8cd4-9fa102dfdc32";
        internal const string InfernoMutatorKey = "Echoes_InfernoRoomInstalled";
        internal const string InfernoUpgradeId = "96547940-8feb-4f73-b147-c763a64161c2";
        internal const string InfernoUpgradeKey = "InfernoRoom";

        private static bool IsInfernoMutator(CombatRelicState relic) =>
            relic.DataId == InfernoMutatorId && relic.AssetKey == InfernoMutatorKey;

        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type == Effect).Select(item => item.index).ToArray();
            if (indices.Length == 0) return null;
            if (IsInfernoMutator(relic))
                return relic.IsCovenant == false && relic.DisallowedInPlacementPhase == false &&
                    relic.EffectTypes.Count == 3 && indices.Length == 3
                    ? null : "Unmodeled opening Inferno mutator effect order.";
            bool knownOwner = relic.DataId == RelicId && relic.AssetKey == RelicKey ||
                relic.DataId == Floor2RelicId && relic.AssetKey == Floor2RelicKey ||
                relic.DataId == Floor3RelicId && relic.AssetKey == Floor3RelicKey;
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
                if (IsInfernoMutator(owner))
                {
                    string? error = ValidateInfernoRooms(context);
                    if (error != null) return error;
                    continue;
                }
                bool floor2 = owner.DataId == Floor2RelicId;
                bool floor3 = owner.DataId == Floor3RelicId;
                int roomIndex = floor3 ? 2 : floor2 ? 1 : 0;
                string upgradeId = floor3 ? Floor3UpgradeId : floor2 ? Floor2UpgradeId : UpgradeId;
                string upgradeKey = floor3 ? Floor3UpgradeKey : floor2 ? Floor2UpgradeKey : UpgradeKey;
                RoomCapacityState? room = context.RoomCapacities.SingleOrDefault(item => item.RoomIndex == roomIndex);
                RoomCapacityAttachmentState[] attachments = room?.Attachments?.Where(item =>
                    item.DataId == upgradeId && item.AssetKey == upgradeKey).ToArray() ?? Array.Empty<RoomCapacityAttachmentState>();
                // This relic installs once. A later card can replace that
                // ordinary slot; require the captured creation ordinal and a
                // current card-owned replacement instead of the retired slot.
                if (attachments.Length == 0 && HasCardReplacement(context, room)) continue;
                if (room?.IsPyre != false || attachments.Length != 1)
                    return "Incomplete original start-of-combat room attachment state.";
                RoomCapacityAttachmentState attachment = attachments[0];
                if (attachment.AttachmentType != 0 || attachment.CapacityDelta != 0 ||
                    attachment.SpellCostDelta != (floor2 ? -1 : 0) ||
                    (floor3 ? attachment.AffectsHeroes || !attachment.AffectsMonsters : !attachment.AffectsHeroes || !attachment.AffectsMonsters) ||
                    !Guid.TryParse(attachment.AttachmentId, out Guid id) || id == Guid.Empty ||
                    attachment.CharacterUpgrades?.Count != 1)
                    return "Incomplete original start-of-combat room attachment state.";
                CardUpgradeModifier upgrade = attachment.CharacterUpgrades[0];
                if (upgrade.DataId != upgradeId || upgrade.AssetKey != upgradeKey || upgrade.ExternalInteractions.Count != 0)
                    return "Unmodeled original start-of-combat room attachment upgrade payload.";
                if (floor3)
                {
                    if (upgrade.Stats.Damage != 15 || upgrade.Stats.Health != 0 || upgrade.Stats.Size != 0 ||
                        upgrade.Statuses.Count != 0 || upgrade.TriggerUpgrades?.Count > 0 || upgrade.AbilityUpgrade != null)
                        return "Unmodeled original FightClub room attachment upgrade payload.";
                }
                else if (floor2)
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

        private static bool HasCardReplacement(CombatContext context, RoomCapacityState? room) =>
            room is { IsPyre: false, AttachmentsAddedFromCards: > 0, Attachments: [{ AttachmentType: 0 } attachment] } &&
            context.CardInstances?.Any(card => card.DataId == attachment.DataId) == true;

        private static string? ValidateInfernoRooms(CombatContext context)
        {
            if (context.Relics!.Count(IsInfernoMutator) != 1)
                return "Expected one original opening Inferno mutator.";
            var identities = new HashSet<Guid>();
            for (int floor = 0; floor < 3; floor++)
            {
                RoomCapacityState? room = context.RoomCapacities!.FirstOrDefault(item => item.RoomIndex == floor);
                if (HasCardReplacement(context, room)) continue;
                if (room?.IsPyre != false || room.Attachments?.Where(attachment => attachment.AttachmentType == 0).ToArray() is not
                    [{ DataId: InfernoUpgradeId, AssetKey: InfernoUpgradeKey, CapacityDelta: 0, SpellCostDelta: 0,
                       EnergyPerTurnDelta: 0, AffectsHeroes: true, AffectsMonsters: true, IgnoreBosses: false,
                       MoonPhase: null, PostCombat: null, DeathEffect: null, SpellUpgrade: null, Ability: null,
                       Echo: null, CorruptionModifier: null, SpawnCopySubtype: null } attachment] ||
                    attachment.CharacterFilters?.Count > 0 || attachment.StatusStackBonuses?.Count > 0 ||
                    attachment.EnemyEntry?.Count > 0 || !Guid.TryParse(attachment.AttachmentId, out Guid identity) ||
                    identity == Guid.Empty || !identities.Add(identity) ||
                    !RoomAttachmentUnitModel.InfernoPayload(attachment))
                    return "Incomplete original opening Inferno room attachment state.";
            }
            return null;
        }
    }
}

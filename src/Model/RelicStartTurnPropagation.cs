using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicStartTurnPropagationModel
    {
        internal const string DataId = "a42d2f85-7b53-4d2b-9979-5e6269b4e01c";
        internal const string AssetKey = "StartOfTurnPropagate";
        internal const string Effect = RelicStartTurnStatusModel.Effect;

        internal static string? Validate(CombatRelicState owner, RelicResourceEffect resource)
        {
            RelicStartTurnRoomEffectRule? rule = resource.StartTurnRoomEffect; CardActionEffect? action = rule?.Action;
            return owner.DataId == DataId && owner.AssetKey == AssetKey && owner.IsCovenant == false && owner.DisallowedInPlacementPhase == false &&
                owner.EffectTypes.SequenceEqual(new[] { Effect }) && resource.Type == Effect && resource.EffectIndex == 0 &&
                resource.ParamInt == 1 && resource.ParamInt2 == 0 && resource.ParamMinInt == 0 && resource.Amount == 1 && resource.ParamFloat == 0 &&
                resource.CanApplyInPreviewMode && resource.RequiredMutatorId == null && resource.Conditions.Count == 0 &&
                rule?.EffectIndex == 0 && rule.Mode == 1 && rule.ParamMaxInt == 0 && rule.ParamFloat == 0 && rule.ParamBool &&
                !rule.ParamBool2 && !rule.ParamBool3 && !rule.ParamBool4 && !rule.UseStatusStackMultiplier && !rule.UseHealthMissingStackMultiplier &&
                !rule.UseMagicPowerMultiplier && !rule.IgnoreDualism && rule.TriggerMetadata.Count == 0 && rule.ExternalInteractions.Count == 0 &&
                action?.Type == "AdjustAllStatusEffects" && action.Target == "Room" && action.Value == 1 && !action.AllowEnemy && action.AllowPlayer &&
                action.StatusAdjustmentFilter == 1 && action.Statuses.Count == 0 && action.Range == null && action.Upgrade == null &&
                action.Tests?.ShouldTest == true && !action.Tests.StrictTargets && !action.Tests.FailToCast && !action.Tests.CancelSubsequent &&
                action.Tests.CanPlayAfterBossDead == true && !action.Tests.MustHaveValidTargets && action.Filters?.Health == "Both" &&
                !action.Filters.IgnoreBosses && action.Filters.Subtype.Length == 0 && action.Filters.RequiredStatuses.Count == 0 &&
                action.Filters.ExcludedStatuses.Count == 0 && action.Filters.ExcludedSubtypes.Count == 0
                ? null : "Missing or changed original StartOfTurnPropagate definition.";
        }

        internal static RelicStartTurnStatusResult Apply(TrainCombatState source, IReadOnlyList<RoomPlayRule> rooms,
            RelicResourceEffect resource)
        {
            CombatRelicState? owner = source.Context?.Relics?.SingleOrDefault(item => item.DataId == DataId);
            if (owner == null) return new(source);
            string? error = Validate(owner, resource); if (error != null) return new(null, error: error);
            RoomPlayRule? pyre = rooms.SingleOrDefault(room => room.IsPyre);
            if (pyre == null) return new(null, error: "Start-turn propagation requires the native Pyre room boundary.");
            int[] affectedRooms = rooms.Where(room => room.RoomIndex < pyre.RoomIndex).OrderBy(room => room.RoomIndex).Select(room => room.RoomIndex).ToArray();
            TrainCombatState state = source;
            foreach (int room in affectedRooms)
            {
                // The original all-room mode calls every floor in ascending order, including empty floors.
                TrainSpellResult result = CardSpellModel.Apply(state, room, new[] { resource.StartTurnRoomEffect!.Action }, 0,
                    definitions: new BattlePlayRules(rooms, Array.Empty<CardPlayRule>()), otherPiles: state.Context?.OtherPiles);
                if (!result.Supported) return new(null, error: result.UnsupportedReason);
                if (result.Outcome != RoomOutcome.Exchanged) return new(null, error: "Unmodeled terminal propagation callback.");
                state = result.State!;
            }
            return new(state, affectedRooms);
        }
    }
}

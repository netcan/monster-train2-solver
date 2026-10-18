using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal sealed class RelicStartTurnStatusResult
    {
        internal TrainCombatState? State { get; }
        internal IReadOnlyList<int> CandidateRooms { get; }
        internal int? SelectedRoom { get; }
        internal string? UnsupportedReason { get; }
        internal bool Supported => State != null;
        internal RelicStartTurnStatusResult(TrainCombatState? state, IReadOnlyList<int>? candidates = null,
            int? selectedRoom = null, string? error = null)
        {
            State = state; CandidateRooms = Array.AsReadOnly((candidates ?? Array.Empty<int>()).ToArray());
            SelectedRoom = selectedRoom; UnsupportedReason = error;
        }
    }

    internal static class RelicStartTurnStatusModel
    {
        internal const string Effect = "RelicEffectAddEffectStartOfTurn";
        internal const string DataId = "727e69a6-3a8c-4029-895c-99e9dac0bc0a";
        internal const string AssetKey = "Inkbound_NimsOath";

        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type == Effect).Select(item => item.index).ToArray();
            if (indices.Length == 0)
                return relic.ResourceEffects?.Any(rule => rule.StartTurnRoomEffect != null) == true
                    ? "Unexpected start-turn room-effect definition." : null;
            RelicResourceEffect[] resources = (relic.ResourceEffects ?? Array.Empty<RelicResourceEffect>())
                .Where(rule => rule.Type == Effect).ToArray();
            if (indices.Length != 1 || !indices.SequenceEqual(new[] { 0 }) || resources.Length != 1 ||
                relic.EffectTypes.Count != 1 ||
                !(relic.DataId == DataId && relic.AssetKey == AssetKey || relic.DataId == RelicStartTurnPropagationModel.DataId && relic.AssetKey == RelicStartTurnPropagationModel.AssetKey) ||
                relic.IsCovenant != false || relic.DisallowedInPlacementPhase != false)
                return "Unmodeled native start-turn room-effect owner.";
            return Validate(relic, resources[0]);
        }

        internal static string? Validate(CombatRelicState relic, RelicResourceEffect resource)
        {
            if (relic.DataId == RelicStartTurnPropagationModel.DataId) return RelicStartTurnPropagationModel.Validate(relic, resource);
            RelicStartTurnRoomEffectRule? rule = resource.StartTurnRoomEffect;
            CardActionEffect? action = rule?.Action;
            if (resource.Type != Effect || resource.EffectIndex != 0 || rule == null || action == null ||
                resource.ParamInt != 2 || resource.ParamInt2 != 0 || resource.ParamMinInt != 0 || resource.Amount != 2 ||
                resource.ParamFloat != 0 ||
                resource.CanApplyInPreviewMode || resource.RequiredMutatorId != null || resource.Conditions.Count != 0 ||
                rule.EffectIndex != 0 || rule.Mode != 2 || rule.ParamMaxInt != 0 || rule.ParamFloat != 1 ||
                !rule.ParamBool || rule.ParamBool2 || rule.ParamBool3 || rule.ParamBool4 ||
                rule.UseStatusStackMultiplier || rule.UseHealthMissingStackMultiplier || rule.UseMagicPowerMultiplier ||
                rule.IgnoreDualism || rule.ExternalInteractions.Count != 0 ||
                action.Type != "AddStatus" || action.Target != "FrontInRoom" || action.Value != 0 ||
                !action.AllowEnemy || action.AllowPlayer || action.Statuses.Count != 1 ||
                action.Statuses[0].Id != "dazed" || action.Statuses[0].Stacks != 1 || action.Statuses[0].ParamInt != 0 ||
                action.Statuses[0].Stackable != true || action.Range != null ||
                action.Tests?.ShouldTest != true || action.Tests.StrictTargets != true ||
                action.Tests.FailToCast || action.Tests.CancelSubsequent || action.Tests.CanPlayAfterBossDead != true ||
                action.Tests.MustHaveValidTargets ||
                action.Filters == null || action.Filters.Health != "Both" || action.Filters.IgnoreBosses ||
                action.Filters.Subtype.Length != 0 || action.Filters.RequiredStatuses.Count != 0 ||
                action.Filters.ExcludedStatuses.Count != 0 || action.Filters.ExcludedSubtypes.Count != 0)
                return "Missing or changed native Nims Oath start-turn Dazed payload.";
            if (rule.TriggerMetadata.Count != 1)
                return "Nims Oath trigger metadata changed; its non-timing callbacks are not modeled.";
            RelicStartTurnEffectTriggerMetadata trigger = rule.TriggerMetadata[0];
            if (trigger.Effects.Count != 1)
                return "Nims Oath non-timing trigger metadata changed.";
            CardActionEffect triggerEffect = trigger.Effects[0];
            if (trigger.Kind != "OnKill" || trigger.Once || trigger.IgnoreSilence || trigger.OnlyIfEquipped ||
                triggerEffect.Type != "AddStatus" || triggerEffect.Target != "Self" ||
                triggerEffect.Value != 0 || triggerEffect.AllowEnemy || !triggerEffect.AllowPlayer ||
                triggerEffect.Statuses.Count != 1 || triggerEffect.Statuses[0].Id != "conduit" ||
                triggerEffect.Statuses[0].Stacks != 1 || triggerEffect.Statuses[0].ParamInt != 1 ||
                triggerEffect.Statuses[0].Stackable != true || triggerEffect.Range != null ||
                triggerEffect.Tests?.ShouldTest != true || triggerEffect.Tests.StrictTargets ||
                triggerEffect.Tests.FailToCast || triggerEffect.Tests.CancelSubsequent ||
                triggerEffect.Tests.CanPlayAfterBossDead != true || triggerEffect.Tests.MustHaveValidTargets ||
                triggerEffect.Filters == null || triggerEffect.Filters.Health != "Both" ||
                triggerEffect.Filters.IgnoreBosses || triggerEffect.Filters.Subtype.Length != 0 ||
                triggerEffect.Filters.RequiredStatuses.Count != 0 || triggerEffect.Filters.ExcludedStatuses.Count != 0 ||
                triggerEffect.Filters.ExcludedSubtypes.Count != 0)
                return "Nims Oath non-timing trigger metadata changed.";
            return null;
        }

        internal static RelicStartTurnStatusResult Apply(TrainCombatState source, IReadOnlyList<RoomPlayRule>? roomRules,
            int turn, bool placement = false, bool preview = false)
        {
            CombatContext? context = source.Context;
            if (context?.Relics?.Any(relic => relic.EffectTypes.Contains(Effect)) != true)
                return new RelicStartTurnStatusResult(source);
            string? error = RelicModel.Validate(context);
            if (error != null) return Fail(error);
            if (roomRules == null) return Fail("Nims Oath requires native enabled-room definitions.");
            RoomPlayRule[] rooms = roomRules.OrderBy(room => room.RoomIndex).ToArray();
            RoomPlayRule? pyreRule = rooms.SingleOrDefault(room => room.IsPyre);
            if (pyreRule == null || rooms.Length == 0 || rooms[^1].RoomIndex != pyreRule.RoomIndex)
                return Fail("Nims Oath requires the native final Pyre room boundary.");
            TrainCombatState state = source;
            var allCandidates = new List<int>();
            int? selectedRoom = null;
            foreach (CombatRelicState relic in context.Relics!)
            {
                foreach (RelicResourceEffect resource in relic.ResourceEffects ?? Array.Empty<RelicResourceEffect>())
                {
                    if (resource.Type != Effect) continue;
                    if (placement && relic.DisallowedInPlacementPhase == true || preview && !resource.CanApplyInPreviewMode)
                        continue;
                    error = Validate(relic, resource);
                    if (error != null) return Fail(error);
                    RelicStartTurnRoomEffectRule rule = resource.StartTurnRoomEffect!;
                    if (relic.DataId == RelicStartTurnPropagationModel.DataId)
                    {
                        RelicStartTurnStatusResult propagated = RelicStartTurnPropagationModel.Apply(state, rooms, resource);
                        if (!propagated.Supported) return propagated;
                        state = propagated.State!; allCandidates.AddRange(propagated.CandidateRooms);
                        continue;
                    }
                    var targetsByRoom = new Dictionary<int, int[]>();
                    var candidates = new List<int>();
                    foreach (RoomPlayRule roomRule in rooms.Where(room => room.RoomIndex < pyreRule.RoomIndex && room.Enabled))
                    {
                        RoomCombatState? room = state.Rooms.FirstOrDefault(item => item.RoomIndex == roomRule.RoomIndex);
                        if (room == null) return Fail("Nims Oath requires complete native floor states.");
                        CardTargets targets = CardTargetModel.Collect(state, room.RoomIndex, rule.Action,
                            Array.Empty<int>(), isTesting: true);
                        if (!targets.Supported) return Fail(targets.UnsupportedReason!);
                        targetsByRoom.Add(room.RoomIndex, targets.UnitIds.ToArray());
                        if (Eligible(targets.UnitIds, state, rule)) candidates.Add(room.RoomIndex);
                    }
                    allCandidates.AddRange(candidates);
                    if (candidates.Count == 0) continue;

                    RngDraw draw = state.Context!.BattleRng.Range(0, candidates.Count);
                    selectedRoom = candidates[draw.Value];
                    state = WithContext(state, state.Context.WithBattleRng(draw.State));
                    foreach (int targetId in targetsByRoom[selectedRoom.Value])
                    {
                        RoomCombatState room = state.Rooms.Single(item => item.RoomIndex == selectedRoom.Value);
                        RoomCombatResult applied = StatusApplicationModel.ApplyRetained(room, targetId,
                            rule.Action.Statuses[0], 0, allowModification: true,
                            fromEffectType: "CardEffectAddStatusEffect", allowStackModifiers: true);
                        if (!applied.Supported) return Fail(applied.UnsupportedReason!);
                        state = WithRoom(state, selectedRoom.Value, applied.State!);
                        if (applied.PendingCallbacks.Count > 0)
                        {
                            TrainCombatResult callbacks = TrainCombatModel.ApplyCharacterQueue(state,
                                applied.PendingCallbacks.ToList());
                            if (!callbacks.Supported) return Fail(callbacks.UnsupportedReason!);
                            if (callbacks.Outcome != RoomOutcome.Exchanged)
                                return Fail("Nims Oath status callbacks caused an unmodeled terminal outcome.");
                            state = callbacks.State!;
                        }
                    }
                    break;
                }
            }
            return new RelicStartTurnStatusResult(state, allCandidates, selectedRoom);
        }

        private static bool Eligible(IReadOnlyList<int> targetIds, TrainCombatState state,
            RelicStartTurnRoomEffectRule rule)
        {
            if (targetIds.Count == 0) return false;
            if (rule.Action.Tests?.StrictTargets != true) return true;
            CombatStatus status = rule.Action.Statuses.Single();
            if (status.Stackable == true) return true;
            return targetIds.Any(id => state.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == id &&
                unit.Statuses.All(existing => existing.Id != status.Id) &&
                (rule.Action.Filters!.Subtype.Length == 0 || unit.Subtypes.Contains(rule.Action.Filters.Subtype))));
        }

        private static TrainCombatState WithRoom(TrainCombatState source, int roomIndex, RoomCombatState updated)
        {
            CombatContext context = updated.Context ?? source.Context!;
            RoomCombatState[] rooms = source.Rooms.Select(room => room.RoomIndex == roomIndex
                ? updated.WithContext(context) : room.WithContext(context)).ToArray();
            return new TrainCombatState(rooms, source.Movement, source.EnemySlotsPerRoom, context);
        }

        private static TrainCombatState WithContext(TrainCombatState source, CombatContext context) =>
            new TrainCombatState(source.Rooms.Select(room => room.WithContext(context)).ToArray(),
                source.Movement, source.EnemySlotsPerRoom, context);

        private static RelicStartTurnStatusResult Fail(string error) => new(null, error: error);
    }
}

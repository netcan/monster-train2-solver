using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class TriggeredStatusTransferModel
    {
        internal const string EffectType = "CardEffectTransferAllStatusEffectsFromUnit";
        private const string FromEffectType = EffectType;
        private const int EvenlyDistributedAcrossHeroTeam = 12;

        internal static string? Validate(CardActionEffect? action)
        {
            if (action?.Type != "TransferAllStatusesFromUnit" || action.Target != "Self" || action.Value != EvenlyDistributedAcrossHeroTeam ||
                action.StatusTransfer == null || action.StatusTransfer.TargetMode != "EvenlyDistributedAcrossHeroTeam" ||
                action.StatusTransfer.AllowedStatusTypes != "BuffsAndDebuffs" || action.StatusTransfer.RoomScope != "SourceRoomOnly" ||
                action.StatusTransfer.AllowStackModification || action.StatusTransfer.SkipRemove || action.StatusTransfer.StackPercentage != 0f ||
                action.Range != null || action.AllowEnemy != true || action.AllowPlayer != true || action.Statuses.Count != 1 ||
                action.Statuses[0].Id != "pyregel" || action.Filters?.Subtype.Length > 0 || action.Filters?.IgnoreBosses == true ||
                action.Filters?.RequiredStatuses.Count > 0 || action.Filters?.ExcludedStatuses.Count > 0 || action.Filters?.ExcludedSubtypes.Count > 0)
                return "Unmodeled triggered status-transfer definition.";
            return null;
        }

        internal static RoomCombatResult Apply(RoomCombatState source, int sourceId, CardActionEffect action, int sourceCardId)
        {
            string? error = Validate(action);
            if (error != null) return Unsupported(error);
            CombatUnit? sourceUnit = source.Units.FirstOrDefault(unit => unit.Id == sourceId);
            if (sourceUnit == null) return Unsupported("Status transfer lost its source unit.");

            int stacks = sourceUnit.Status(action.Statuses[0].Id)?.Stacks ?? 0;
            int[] targetIds = source.Units.Where(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0 &&
                unit.DeathState?.IsDestroyed != true && unit.DeathState?.IsDespawned != true && !unit.IsPyre &&
                unit.Status("untouchable")?.Stacks is not > 0 &&
                (action.Filters == null || string.IsNullOrEmpty(action.Filters.Subtype) ||
                    unit.Subtypes.Contains(action.Filters.Subtype)))
                .Select(unit => unit.Id).ToArray();

            RoomCombatState state = source;
            var events = new List<CombatEvent>();
            var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
            RoomCombatResult removed = StatusRemovalModel.Remove(state, sourceId, action.Statuses[0].Id, stacks, sourceCardId, FromEffectType);
            if (!removed.Supported) return removed;
            state = removed.State!; events.AddRange(removed.Events); callbacks.AddRange(removed.PendingCallbacks);

            if (stacks > 0 && targetIds.Length > 0)
            {
                int perTarget = (int)Math.Ceiling((double)stacks / targetIds.Length);
                foreach (int targetId in targetIds.Reverse())
                {
                    CombatUnit? target = state.Units.FirstOrDefault(unit => unit.Id == targetId);
                    if (target == null || target.Status(action.Statuses[0].Id)?.Stackable == false &&
                        target.Status(action.Statuses[0].Id)?.Stacks > 0) continue;
                    RoomCombatResult applied = StatusApplicationModel.ApplyRetained(state, targetId,
                        action.Statuses[0].WithStacks(perTarget), sourceCardId, allowModification: false,
                        fromEffectType: FromEffectType, allowStackModifiers: false);
                    if (!applied.Supported) return applied;
                    state = applied.State!; events.AddRange(applied.Events); callbacks.AddRange(applied.PendingCallbacks);
                }
            }
            return new RoomCombatResult(state, RoomOutcome.Exchanged, 0, events, pendingCallbacks: callbacks);
        }

        private static RoomCombatResult Unsupported(string reason) => new(null, RoomOutcome.Unsupported, 0,
            new List<CombatEvent>(), reason);
    }
}

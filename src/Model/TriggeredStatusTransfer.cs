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
            bool pyregel = action?.Value == EvenlyDistributedAcrossHeroTeam && action.StatusTransfer?.TargetMode == "EvenlyDistributedAcrossHeroTeam" &&
                action.StatusTransfer.AllowedStatusTypes == "BuffsAndDebuffs" && !action.StatusTransfer.SkipRemove && action.Statuses.Count == 1 && action.Statuses[0].Id == "pyregel";
            bool decay = action?.Value == 11 && action.StatusTransfer?.TargetMode == "EvenlyDistributedAcrossFriendlyTeam" &&
                action.StatusTransfer.AllowedStatusTypes == "DebuffsOnly" && action.StatusTransfer.SkipRemove && action.Statuses.Count == 1 && action.Statuses[0].Id == "decay";
            bool copyBuffs = action?.Value == 8 && action.StatusTransfer?.TargetMode == "CopyToEachUnitOnFriendlyTeam" &&
                action.StatusTransfer.AllowedStatusTypes == "BuffsOnly" && !action.StatusTransfer.SkipRemove && action.Statuses.Count == 0 &&
                action.AllowEnemy == false && action.AllowPlayer == true;
            if (action?.Type != "TransferAllStatusesFromUnit" || action.Target != "Self" || !(pyregel || decay || copyBuffs) ||
                action.StatusTransfer == null || action.StatusTransfer.RoomScope != "SourceRoomOnly" ||
                action.StatusTransfer.AllowStackModification || action.StatusTransfer.StackPercentage != 0f ||
                action.Range != null || !copyBuffs && (action.AllowEnemy != true || action.AllowPlayer != true || action.Statuses.Count != 1) ||
                action.Filters?.Subtype.Length > 0 || action.Filters?.IgnoreBosses == true ||
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
            if (action.Value == 8) return CopyBuffs(source, sourceUnit, sourceCardId);

            int stacks = sourceUnit.Status(action.Statuses[0].Id)?.Stacks ?? 0;
            bool friendly = action.StatusTransfer!.TargetMode == "EvenlyDistributedAcrossFriendlyTeam";
            int[] targetIds = source.Units.Where(unit => unit.Team == (friendly ? sourceUnit.Team : CombatTeam.Enemy) && unit.Health > 0 &&
                unit.DeathState?.IsDestroyed != true && unit.DeathState?.IsDespawned != true && (friendly || !unit.IsPyre) &&
                unit.Status("untouchable")?.Stacks is not > 0 &&
                (action.Filters == null || string.IsNullOrEmpty(action.Filters.Subtype) ||
                    unit.Subtypes.Contains(action.Filters.Subtype)))
                .Select(unit => unit.Id).ToArray();

            RoomCombatState state = source;
            var events = new List<CombatEvent>();
            var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
            if (!action.StatusTransfer.SkipRemove)
            {
                RoomCombatResult removed = StatusRemovalModel.Remove(state, sourceId, action.Statuses[0].Id, stacks, sourceCardId, FromEffectType);
                if (!removed.Supported) return removed;
                state = removed.State!; events.AddRange(removed.Events); callbacks.AddRange(removed.PendingCallbacks);
            }

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

        private static RoomCombatResult CopyBuffs(RoomCombatState source, CombatUnit actor, int sourceCardId)
        {
            if (actor.StatusRegistry == null) return Unsupported("Copying buffs requires captured status definitions and visibility.");
            CombatStatus[] buffs = (actor.StatusDictionary?.Order(actor.StatusRegistry) ?? actor.StatusRegistry)
                .Where(status => status.Stacks > 0 && status.Id != "horde" && status.Hidden != true && status.DisplayCategory == "Positive").ToArray();
            int[] targets = source.Units.Where(unit => unit.Team == actor.Team && unit.Health > 0 &&
                unit.DeathState?.IsDestroyed != true && unit.DeathState?.IsDespawned != true && unit.Status("untouchable")?.Stacks is not > 0)
                .Select(unit => unit.Id).ToArray();
            RoomCombatState state = source;
            var events = new List<CombatEvent>();
            var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
            if (targets.Length != 1 || targets[0] != actor.Id)
            {
                foreach (CombatStatus buff in buffs)
                {
                    RoomCombatResult removed = StatusRemovalModel.Remove(state, actor.Id, buff.Id, buff.Stacks, sourceCardId, FromEffectType);
                    if (!removed.Supported) return removed;
                    state = removed.State!; events.AddRange(removed.Events); callbacks.AddRange(removed.PendingCallbacks);
                }
                foreach (CombatStatus buff in buffs)
                foreach (int targetId in targets.Reverse())
                {
                    CombatUnit target = state.Units.Single(unit => unit.Id == targetId);
                    if (buff.Stackable == false && target.Status(buff.Id)?.Stacks > 0) continue;
                    RoomCombatResult applied = StatusApplicationModel.ApplyRetained(state, targetId, buff, sourceCardId,
                        allowModification: false, fromEffectType: FromEffectType, allowStackModifiers: false);
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

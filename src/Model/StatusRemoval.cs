using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class StatusRemovalModel
    {
        internal static string? Validate(CardActionEffect effect, RoomCombatState room)
        {
            if (effect.Statuses.Count != 1) return "Status removal requires one status definition.";
            return RoomCombatModel.KnowsStatus(effect.Statuses[0].Id) ? null : "Unmodeled removed status definition.";
        }
        internal static bool Test(RoomCombatState room, CardActionEffect effect, IReadOnlyList<int> targets) =>
            string.IsNullOrEmpty(effect.Filters?.Subtype) || room.Units.Any(unit => targets.Contains(unit.Id) && unit.Subtypes.Contains(effect.Filters!.Subtype));
        internal static bool RequiresSacrifice(RoomCombatResult result) => result.PendingCallbacks.Any(item => item.Kind == "OnDeath" &&
            item.HarvestAfterDeath && !item.DeferUntilRemoval);
        // This is the raw CharacterState API. Setting HP to zero does not dispatch a
        // death signal, exhaust the spawner or increment another physical death.
        internal static RoomCombatResult Remove(RoomCombatState source, int targetId, string id, int amount, int sourceCardId = 0)
        {
            id = id.ToLowerInvariant();
            CombatUnit? target = source.Units.FirstOrDefault(unit => unit.Id == targetId);
            if (target == null) return Unsupported("Missing status removal target.");
            CombatStatus? status = target.RegisteredStatus(id);
            if (status == null) return Match(source);
            int count = amount == -1 ? 0 : Math.Max(0, Math.Min(status.Stackable == false ? 1 : 9999, unchecked(status.Stacks - amount)));
            CombatUnit changed = CardSpellModel.Copy(target, target.Health, target.Statuses.Where(item => item.Id != id)
                .Concat(count > 0 ? new[] { status.WithStacks(count) } : Array.Empty<CombatStatus>()).ToArray());
            CombatContext? context = source.Context;
            if (!source.Preview && sourceCardId > 0 && count < status.Stacks && context?.Statistics != null)
                context = context.WithStatistics(context.LiveStatistics!.Increment(sourceCardId, "AnyStatusEffectStacksRemoved", status.Stacks - count,
                    requireTrackedCard: context.CardInstances?.Count == 0));
            var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
            if (id == "horde" && count < status.Stacks)
            {
                var staged = new RoomCombatState(source.RoomIndex, source.Deployment, source.Units, source.ExternalInteractions, context, source.Preview);
                RoomCombatResult horde = HordeStatusModel.Change(staged, target, changed, count - status.Stacks);
                if (!horde.Supported) return horde;
                changed = horde.State!.Units.First(unit => unit.Id == targetId);
                context = horde.State.Context; callbacks.AddRange(horde.PendingCallbacks);
            }
            StatusCallbackModel.Removed(source.RoomIndex, target, changed, id, callbacks);
            return new RoomCombatResult(new RoomCombatState(source.RoomIndex, source.Deployment,
                source.Units.Select(unit => unit.Id == targetId ? changed : unit)
                    .Where(unit => unit.Id != targetId || target.Health <= 0 || changed.Health > 0).ToArray(), source.ExternalInteractions, context, source.Preview),
                RoomOutcome.Exchanged, 0, new List<CombatEvent>(), pendingCallbacks: callbacks);
        }
        // The card effect clamps its request to the current count, skips a zero
        // status and explicitly sacrifices a Horde whose final count becomes zero.
        internal static RoomCombatResult Effect(RoomCombatState source, int targetId, CardActionEffect effect, int sourceCardId = 0, bool whileRunningQueue = false)
        {
            CombatUnit? target = source.Units.FirstOrDefault(unit => unit.Id == targetId);
            if (target == null) return Match(source);
            if (!string.IsNullOrEmpty(effect.Filters?.Subtype) && !target.Subtypes.Contains(effect.Filters!.Subtype)) return Match(source);
            string id = effect.Statuses[0].Id;
            int count = target.RegisteredStatus(id)?.Stacks ?? 0;
            if (count == 0) return Match(source);
            RoomCombatResult raw = Remove(source, targetId, id, Math.Min(effect.Statuses[0].Stacks, count), sourceCardId);
            if (!raw.Supported || id != "horde") return raw;
            CombatUnit changed = raw.State!.Units.FirstOrDefault(unit => unit.Id == targetId) ??
                raw.PendingCallbacks.LastOrDefault(item => item.Unit.Id == targetId)?.Unit ?? target;
            if ((changed.RegisteredStatus(id)?.Stacks ?? 0) != 0) return raw;
            var callbacks = raw.PendingCallbacks.ToList();
            if (changed.DeathState?.PendingStatisticsCardId.HasValue == true)
            {
                raw = RoomCombatModel.SettlePendingDeathStatistics(raw.State!, changed);
                changed = raw.State!.Units.Single(unit => unit.Id == targetId);
            }
            RoomCombatResult sacrificed = RoomCombatModel.QueueSacrifice(raw.State!, changed, sourceCardId, callbacks.Add, whileRunningQueue);
            return new RoomCombatResult(sacrificed.State, sacrificed.Outcome, sacrificed.Rounds, sacrificed.Events.ToList(),
                sacrificed.UnsupportedReason, callbacks);
        }
        internal static RoomCombatResult Drain(RoomCombatResult result)
        {
            if (!result.Supported) return result;
            var callbacks = result.PendingCallbacks.ToList(); var events = result.Events.ToList(); RoomOutcome outcome = result.Outcome;
            bool drained = RoomCombatModel.DrainCharacterQueue(callbacks, queued =>
            {
                result = RoomCombatModel.ApplyQueuedCharacterTrigger(result.State!, queued, callbacks.Add);
                if (result.Supported) { events.AddRange(result.Events); if (result.Outcome != RoomOutcome.Exchanged) outcome = result.Outcome; }
                return result.Supported;
            }, queued => { result = RoomCombatModel.SettleQueuedSpawner(result.State!, queued.Unit); return result.Supported; });
            return drained ? new RoomCombatResult(result.State, outcome, result.Rounds, events) : result;
        }
        private static RoomCombatResult Match(RoomCombatState state) => new RoomCombatResult(state, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
        private static RoomCombatResult Unsupported(string reason) => new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), reason);
    }
}

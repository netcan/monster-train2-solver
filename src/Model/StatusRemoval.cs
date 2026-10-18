using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class StatusRemovalModel
    {
        private static readonly string[] DisplayCategories = { "Positive", "Negative", "Persistent", "Ability" };

        internal static string? Validate(CardActionEffect effect, RoomCombatState room)
        {
            if (effect.Statuses.Count != 1) return "Status removal requires one status definition.";
            return RoomCombatModel.KnowsStatus(effect.Statuses[0].Id) ? null : "Unmodeled removed status definition.";
        }
        internal static string? ValidateAll(CardActionEffect effect)
        {
            if (effect.RemoveHalfStatusEffects == null) return "Missing native remove-half status parameter.";
            if (effect.Value < 0 || effect.Value >= DisplayCategories.Length) return "Unknown status display category.";
            if (effect.Statuses.Count != 0) return "Remove-all status effect unexpectedly contains a status list.";
            return null;
        }
        internal static bool Test(RoomCombatState room, CardActionEffect effect, IReadOnlyList<int> targets) =>
            string.IsNullOrEmpty(effect.Filters?.Subtype) || room.Units.Any(unit => targets.Contains(unit.Id) && unit.Subtypes.Contains(effect.Filters!.Subtype));
        internal static bool RequiresSacrifice(RoomCombatResult result) => result.PendingCallbacks.Any(item => item.Kind == "OnDeath" &&
            item.HarvestAfterDeath && !item.DeferUntilRemoval);
        // This is the raw CharacterState API. Setting HP to zero does not dispatch a
        // death signal, exhaust the spawner or increment another physical death.
        internal static RoomCombatResult Remove(RoomCombatState source, int targetId, string id, int amount, int sourceCardId = 0,
            string? fromEffectType = null, bool allowStackModifiers = true)
        {
            id = id.ToLowerInvariant();
            CombatUnit? target = source.Units.FirstOrDefault(unit => unit.Id == targetId);
            if (target == null) return Unsupported("Missing status removal target.");
            RoomCombatResult relicAdded = RelicStatusStackModel.OnStatusRemoved(source, targetId, id, amount, fromEffectType);
            if (!relicAdded.Supported) return relicAdded;
            source = relicAdded.State!;
            target = source.Units.FirstOrDefault(unit => unit.Id == targetId);
            if (target == null) return Unsupported("Status removal relic lost its target.");
            if (allowStackModifiers)
            {
                StatusScalingResult scaled = CardStatusRemovalScalingModel.Apply(source.Context, sourceCardId, id, amount);
                if (!scaled.Supported) return Unsupported(scaled.UnsupportedReason!);
                source = source.WithContext(scaled.Context);
                amount = scaled.Stacks;
            }
            CombatStatus? status = target.RegisteredStatus(id);
            if (status == null) return new RoomCombatResult(source, RoomOutcome.Exchanged, 0, new List<CombatEvent>(),
                pendingCallbacks: relicAdded.PendingCallbacks);
            int count = amount == -1 ? 0 : Math.Max(0, Math.Min(status.Stackable == false ? 1 : 9999, unchecked(status.Stacks - amount)));
            CombatUnit changed = CardSpellModel.Copy(target, target.Health, target.Statuses.Where(item => item.Id != id)
                .Concat(count > 0 ? new[] { status.WithStacks(count) } : Array.Empty<CombatStatus>()).ToArray());
            if (count == 0 && status.MarkedForRemovalAfterCardPlayed && changed.StatusRegistry != null)
                changed = changed.WithStatusRegistry(changed.StatusRegistry.Select(item => item.Id == id
                    ? item.WithStacks(0).WithMarkedForRemovalAfterCardPlayed(false) : item).ToArray());
            CombatContext? context = source.Context;
            StatusAttackDamageResult attack = StatusAttackDamageModel.Change(context, changed, status, status.Stacks - count, adding: false);
            if (!attack.Supported) return Unsupported(attack.UnsupportedReason!);
            changed = attack.Unit!; context = attack.Context;
            if (id == "conduit")
            {
                if (!ConduitModel.Captured(context)) return Unsupported("Conduit removal requires captured native room/card tracker state.");
                context = ConduitModel.Change(context!, source.RoomIndex, changed, source.Preview, refreshSelection: false,
                    order: source.Units.Select(unit => unit.Id).ToArray());
            }
            if (!source.Preview && sourceCardId > 0 && count < status.Stacks && context?.Statistics != null)
                context = context.WithStatistics(context.LiveStatistics!.Increment(sourceCardId, "AnyStatusEffectStacksRemoved", status.Stacks - count,
                    requireTrackedCard: context.CardInstances?.Count == 0));
            var callbacks = relicAdded.PendingCallbacks.ToList();
            if (id == "horde" && count < status.Stacks)
            {
                var staged = new RoomCombatState(source.RoomIndex, source.Deployment, source.Units,
                    source.ExternalInteractions, context, source.Preview, source.ActiveCompanionBossIds);
                RoomCombatResult horde = HordeStatusModel.Change(staged, target, changed, count - status.Stacks);
                if (!horde.Supported) return horde;
                changed = horde.State!.Units.First(unit => unit.Id == targetId);
                context = horde.State.Context; callbacks.AddRange(horde.PendingCallbacks);
            }
            StatusCallbackModel.Removed(source.RoomIndex, target, changed, id, callbacks);
            string? queueError = StatusCallbackModel.FilterPurifyQueue(context, callbacks);
            if (queueError != null) return Unsupported(queueError);
            return EnchantmentWorldModel.StatusChanged(new RoomCombatResult(new RoomCombatState(source.RoomIndex, source.Deployment,
                source.Units.Select(unit => unit.Id == targetId ? changed : unit)
                    .Where(unit => unit.Id != targetId || target.Health <= 0 || changed.Health > 0).ToArray(),
                source.ExternalInteractions, context, source.Preview, source.ActiveCompanionBossIds),
                RoomOutcome.Exchanged, 0, new List<CombatEvent>(), pendingCallbacks: callbacks), id,
                target.Status("dormant") != null, update: count < status.Stacks);
        }
        // The card effect clamps its request to the current count, skips a zero
        // status and explicitly sacrifices a Horde whose final count becomes zero.
        internal static RoomCombatResult Effect(RoomCombatState source, int targetId, CardActionEffect effect, int sourceCardId = 0, bool whileRunningQueue = false)
        {
            if (effect.Type == "RemoveAllStatusEffects") return RemoveAll(source, targetId, effect, sourceCardId);
            CombatUnit? target = source.Units.FirstOrDefault(unit => unit.Id == targetId);
            if (target == null) return Match(source);
            if (!string.IsNullOrEmpty(effect.Filters?.Subtype) && !target.Subtypes.Contains(effect.Filters!.Subtype)) return Match(source);
            string id = effect.Statuses[0].Id;
            int count = target.RegisteredStatus(id)?.Stacks ?? 0;
            if (count == 0) return Match(source);
            RoomCombatResult raw = Remove(source, targetId, id, Math.Min(effect.Statuses[0].Stacks, count), sourceCardId,
                "CardEffectRemoveStatusEffect", allowStackModifiers: !effect.DisallowStatusStackModifiers);
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
        private static RoomCombatResult RemoveAll(RoomCombatState source, int targetId, CardActionEffect effect, int sourceCardId)
        {
            string? error = ValidateAll(effect);
            if (error != null) return Unsupported(error);
            RoomCombatState state = source;
            var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
            var events = new List<CombatEvent>();
            RoomOutcome outcome = RoomOutcome.Exchanged;
            CombatUnit? initial = state.Units.FirstOrDefault(unit => unit.Id == targetId);
            if (initial == null) return Match(source);
            string category = DisplayCategories[effect.Value];
            foreach (CombatStatus status in initial.Statuses.Where(status => status.Stacks > 0).ToArray())
            {
                if (!status.Hidden.HasValue || status.DisplayCategory == null)
                    return Unsupported("Remove-all status effect requires captured visibility and display-category metadata.");
                if (status.Hidden.Value || status.DisplayCategory != category) continue;
                CombatUnit? current = state.Units.FirstOrDefault(unit => unit.Id == targetId);
                int stacks = current?.RegisteredStatus(status.Id)?.Stacks ?? 0;
                if (stacks <= 0) continue;
                int amount = effect.RemoveHalfStatusEffects == true ? (stacks / 2) + (stacks % 2) : stacks;
                RoomCombatResult removed = Remove(state, targetId, status.Id, amount, sourceCardId,
                    "CardEffectRemoveAllStatusEffects");
                if (!removed.Supported) return removed;
                state = removed.State!;
                callbacks.AddRange(removed.PendingCallbacks);
                events.AddRange(removed.Events);
                if (removed.Outcome != RoomOutcome.Exchanged) outcome = removed.Outcome;
            }
            return new RoomCombatResult(state, outcome, 0, events, pendingCallbacks: callbacks);
        }
        internal static RoomCombatResult Drain(RoomCombatResult result, bool settleSpawner = true)
        {
            if (!result.Supported) return result;
            var callbacks = result.PendingCallbacks.ToList(); var events = result.Events.ToList(); RoomOutcome outcome = result.Outcome;
            bool drained = RoomCombatModel.DrainCharacterQueue(callbacks, queued =>
            {
                result = RoomCombatModel.ApplyQueuedCharacterTrigger(result.State!, queued, callbacks.Add);
                if (result.Supported) { events.AddRange(result.Events); if (result.Outcome != RoomOutcome.Exchanged) outcome = result.Outcome; }
                return result.Supported;
            }, queued =>
            {
                if (!settleSpawner) return true;
                result = RoomCombatModel.SettleQueuedSpawner(result.State!, queued.Unit); return result.Supported;
            },
                () => { result = EnchantmentWorldModel.CompleteQueuedRemovals(result.State!, callbacks.Add); return result.Supported; }, () => result.State?.Context, message => result = new RoomCombatResult(null, RoomOutcome.Unsupported, 0, events, message));
            return drained ? new RoomCombatResult(result.State, outcome, result.Rounds, events) : result;
        }
        private static RoomCombatResult Match(RoomCombatState state) => new RoomCombatResult(state, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
        private static RoomCombatResult Unsupported(string reason) => new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), reason);
    }
}

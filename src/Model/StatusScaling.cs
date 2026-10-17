using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class ScalingStatusTrait
    {
        public CardStatisticQuery Query { get; }
        public int StacksPerStat { get; }
        public bool OnlyWhenSourceZero { get; }
        public int Filter { get; }
        public IReadOnlyList<string> StatusIds { get; }
        public IReadOnlyList<string>? EnemyPropagatableStatuses { get; }
        public IReadOnlyList<string>? PlayerPropagatableStatuses { get; }
        public ScalingStatusTrait(CardStatisticQuery query, int stacksPerStat, bool onlyWhenSourceZero, int filter,
            IReadOnlyList<string> statusIds, IReadOnlyList<string>? enemyPropagatableStatuses = null,
            IReadOnlyList<string>? playerPropagatableStatuses = null)
        {
            Query = query; StacksPerStat = stacksPerStat; OnlyWhenSourceZero = onlyWhenSourceZero; Filter = filter;
            StatusIds = Freeze(statusIds)!; EnemyPropagatableStatuses = Freeze(enemyPropagatableStatuses);
            PlayerPropagatableStatuses = Freeze(playerPropagatableStatuses);
        }
        private static IReadOnlyList<string>? Freeze(IReadOnlyList<string>? source) => source == null ? null :
            Array.AsReadOnly(source.Distinct().OrderBy(id => id, StringComparer.Ordinal).ToArray());
    }

    public sealed class StatusScalingResult
    {
        public CombatContext? Context { get; }
        public int Stacks { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => UnsupportedReason == null;
        internal StatusScalingResult(CombatContext? context, int stacks, string? error = null)
        { Context = context; Stacks = stacks; UnsupportedReason = error; }
    }

    public static class StatusScalingModel
    {
        // Each trait receives the running incoming amount, not the target's existing stack count.
        public static StatusScalingResult Apply(CombatContext? context, int sourceCardId, CombatTeam targetTeam,
            string statusId, int stacks)
        {
            if (sourceCardId <= 0) return new StatusScalingResult(context, stacks);
            CardInstanceState? owner = context?.FindCard(sourceCardId);
            if (owner == null)
            {
                if (context?.CardInstances == null && context?.CardRegistry == null) return new StatusScalingResult(context, stacks);
                return Unsupported("Missing source status card.");
            }
            foreach (ScalingStatusTrait trait in owner.StatusScalingTraits ?? Array.Empty<ScalingStatusTrait>())
            {
                StatusScalingResult additional = ApplyTrait(context, trait, sourceCardId, targetTeam, statusId, stacks);
                if (!additional.Supported) return additional;
                context = additional.Context; stacks = unchecked(stacks + additional.Stacks);
            }
            return new StatusScalingResult(context, stacks);
        }

        // Returns only this trait's bonus, as the native callback does.
        public static StatusScalingResult ApplyTrait(CombatContext? context, ScalingStatusTrait trait, int ownerCardId,
            CombatTeam targetTeam, string statusId, int sourceStacks)
        {
            if (trait.OnlyWhenSourceZero && sourceStacks != 0) return new StatusScalingResult(context, 0);
            bool matches;
            switch (trait.Filter)
            {
                case 0: matches = trait.StatusIds.Contains(statusId); break;
                case 1:
                    IReadOnlyList<string>? mask = targetTeam == CombatTeam.Enemy ? trait.EnemyPropagatableStatuses : trait.PlayerPropagatableStatuses;
                    if (mask == null) return Unsupported("Missing propagatable status definitions for the target team.");
                    matches = mask.Contains(statusId); break;
                case 2: matches = statusId == "horde"; break;
                default: matches = false; break; // Native unknown filter values are no-ops.
            }
            if (!matches) return new StatusScalingResult(context, 0);
            if (context == null) return Unsupported("Status scaling requires shared combat context.");
            if (trait.Query.Type == "AnyStatusEffectStacksRemoved")
                return Unsupported("Removed-status scaling requires the native status-removal attribution paths.");
            StatisticQueryResult query = StatisticQueryModel.Evaluate(context, trait.Query, ownerCardId);
            return query.Supported ? new StatusScalingResult(query.Context, unchecked(trait.StacksPerStat * query.Value)) :
                Unsupported(query.UnsupportedReason!);
        }
        private static StatusScalingResult Unsupported(string reason) => new StatusScalingResult(null, 0, reason);
    }

    public static class StatusApplicationModel
    {
        public static RoomCombatResult Apply(RoomCombatState source, int targetId, CombatStatus added, int sourceCardId = 0,
            bool overrideImmunity = false, bool allowModification = true, bool fromPermanentUpgrade = false,
            bool spawnEffect = false, string? fromEffectType = null, bool allowStackModifiers = true,
            bool doubleForDuality = false)
        {
            string? error = RoomCombatModel.Validate(source);
            if (error != null || !RoomCombatModel.KnowsStatus(added.Id)) return Unsupported(error ?? "Unmodeled added status " + added.Id);
            return ApplyRetained(source, targetId, added, sourceCardId, overrideImmunity, allowModification,
                fromPermanentUpgrade: fromPermanentUpgrade, spawnEffect: spawnEffect, fromEffectType: fromEffectType,
                allowStackModifiers: allowStackModifiers, doubleForDuality: doubleForDuality);
        }
        // Trigger effects retain their native actor/target objects, including HP-zero victims.
        internal static RoomCombatResult ApplyRetained(RoomCombatState source, int targetId, CombatStatus added, int sourceCardId,
            bool overrideImmunity = false, bool allowModification = true, bool suppressHordeSpawnCallbacks = false,
            bool fromPermanentUpgrade = false, bool spawnEffect = false, string? fromEffectType = null,
            bool allowStackModifiers = true, bool doubleForDuality = false)
        {
            CombatUnit? target = source.Units.FirstOrDefault(unit => unit.Id == targetId);
            if (target == null) return Unsupported("Missing status target.");
            if (!overrideImmunity && (target.StatusImmunities.Contains(added.Id) || target.Status("immune") != null)) return Match(source);
            if (target.Status("purify")?.Stacks > 0) return Match(source);
            CombatContext? context = source.Context;
            if (added.Id == "conduit" && !ConduitModel.Captured(context))
                return Unsupported("Conduit application requires captured native room/card tracker state.");
            int stacks = added.Stacks;
            if (allowModification)
            {
                StatusScalingResult scaled = StatusScalingModel.Apply(context, sourceCardId, target.Team, added.Id, stacks);
                if (!scaled.Supported) return Unsupported(scaled.UnsupportedReason!);
                context = scaled.Context; stacks = scaled.Stacks;
                if (doubleForDuality) stacks = unchecked(stacks * 2);
                if (allowStackModifiers)
                    stacks = RelicStatusStackModel.GetStacks(context?.Relics, added.Id, stacks, target.Team,
                        target.Status(added.Id)?.Stacks > 0, fromPermanentUpgrade, fromEffectType: fromEffectType);
            }
            StatusScalingResult notified = RelicStatusStackModel.Notify(context, source.Preview, fromEffectType);
            if (!notified.Supported) return Unsupported(notified.UnsupportedReason!);
            context = notified.Context;
            CombatStatus? existing = target.RegisteredStatus(added.Id);
            int old = existing?.Stacks ?? 0;
            int count = Math.Max(0, Math.Min((existing?.Stackable ?? added.Stackable) == false ? 1 : 9999, unchecked(old + stacks)));
            CombatUnit changed = CardSpellModel.Copy(target, target.Health, target.Statuses.Where(status => status.Id != added.Id)
                .Concat(new[] { (existing ?? added).WithStacks(count) }).ToArray());
            StatusAttackDamageResult attack = StatusAttackDamageModel.Change(context, changed, existing ?? added, count - old, adding: true);
            if (!attack.Supported) return Unsupported(attack.UnsupportedReason!);
            changed = attack.Unit!; context = attack.Context;
            if (added.Id == "conduit") context = ConduitModel.Change(context!, source.RoomIndex, changed, source.Preview,
                order: source.Units.Select(unit => unit.Id).ToArray());
            if (!source.Preview && sourceCardId > 0 && count > old && context?.Statistics != null)
                context = context.WithStatistics(context.LiveStatistics!.Increment(sourceCardId, "AnyStatusEffectStacksAdded", count - old,
                    requireTrackedCard: context.CardInstances?.Count == 0));
            var queue = new List<RoomCombatModel.QueuedCharacterTrigger>();
            if (added.Id == "horde" && count > old)
            {
                var staged = new RoomCombatState(source.RoomIndex, source.Deployment, source.Units,
                    source.ExternalInteractions, context, source.Preview, source.ActiveCompanionBossIds);
                RoomCombatResult horde = HordeStatusModel.Change(staged, target, changed, count - old, suppressHordeSpawnCallbacks);
                if (!horde.Supported) return horde;
                changed = horde.State!.Units.First(unit => unit.Id == targetId); context = horde.State.Context;
                queue.AddRange(horde.PendingCallbacks);
            }
            string? callbackError = StatusCallbackModel.Added(source.RoomIndex, target, changed, added.Id, queue, existing ?? added);
            if (callbackError != null) return Unsupported(callbackError);
            callbackError = StatusCallbackModel.FilterPurifyQueue(context, queue);
            if (callbackError != null) return Unsupported(callbackError);
            RoomCombatResult applied = EnchantmentWorldModel.StatusChanged(new RoomCombatResult(new RoomCombatState(source.RoomIndex, source.Deployment,
                source.Units.Select(unit => unit.Id == targetId ? changed : unit).ToArray(),
                source.ExternalInteractions, context, source.Preview, source.ActiveCompanionBossIds),
                RoomOutcome.Exchanged, 0, new List<CombatEvent>(), pendingCallbacks: queue), added.Id, target.Status("dormant") != null, update: true);
            if (applied.Supported && applied.State!.Context != null)
            {
                RelicConditionEvaluation conditions = RelicConditionModel.EvaluateFirstImpStatusEffectApplied(applied.State.Context);
                if (!conditions.Supported) return Unsupported(conditions.UnsupportedReason!);
                applied = new RoomCombatResult(applied.State.WithContext(conditions.Context), applied.Outcome, applied.Rounds,
                    applied.Events.ToList(), pendingCallbacks: applied.PendingCallbacks, retainedUnits: applied.RetainedUnits);
            }
            if (applied.Supported && count > old && fromPermanentUpgrade && !spawnEffect)
            {
                int deferred = RelicStatusStackModel.GetStacks(applied.State!.Context?.Relics, added.Id, 0,
                    target.Team, count > 0, fromPermanentUpgrade: true, deferred: true, fromEffectType: fromEffectType);
                if (deferred > 0)
                {
                    RoomCombatResult extra = ApplyRetained(applied.State, targetId, added.WithStacks(deferred), sourceCardId,
                        overrideImmunity: overrideImmunity, allowModification: false, fromEffectType: fromEffectType);
                    if (!extra.Supported) return extra;
                    applied = new RoomCombatResult(extra.State, extra.Outcome, extra.Rounds,
                        applied.Events.Concat(extra.Events).ToList(), pendingCallbacks: applied.PendingCallbacks.Concat(extra.PendingCallbacks).ToArray(),
                        retainedUnits: applied.RetainedUnits.Concat(extra.RetainedUnits).ToArray());
                }
            }
            return applied.Supported && added.Id == "purify" ? ClearForPurify(applied, targetId) : applied;
        }
        private static RoomCombatResult ClearForPurify(RoomCombatResult applied, int targetId)
        {
            RoomCombatState state = applied.State!;
            CombatUnit actor = state.Units.Single(unit => unit.Id == targetId);
            // Native copies every dictionary key after adding purify, including zero
            // definitions, then calls the raw removal API with no responsible card.
            string[] keys = (actor.StatusRegistry ?? actor.Statuses).Select(status => status.Id).Where(id => id != "purify").ToArray();
            var callbacks = applied.PendingCallbacks.ToList();
            var events = applied.Events.ToList();
            var retained = applied.RetainedUnits.ToDictionary(unit => unit.Id);
            bool inRoom = true;
            foreach (string id in keys)
            {
                RoomCombatState scope = inRoom ? state : new RoomCombatState(state.RoomIndex, state.Deployment,
                    state.Units.Concat(new[] { actor }).ToArray(), state.ExternalInteractions, state.Context,
                    state.Preview, state.ActiveCompanionBossIds);
                RoomCombatResult removed = StatusRemovalModel.Remove(scope, targetId, id, -1);
                if (!removed.Supported) return removed;
                actor = removed.State!.Units.FirstOrDefault(unit => unit.Id == targetId) ??
                    removed.RetainedUnits.FirstOrDefault(unit => unit.Id == targetId) ??
                    removed.PendingCallbacks.LastOrDefault(item => item.Unit.Id == targetId)?.Unit ?? actor;
                inRoom &= removed.State.Units.Any(unit => unit.Id == targetId);
                foreach (CombatUnit unit in removed.RetainedUnits) retained[unit.Id] = unit;
                if (!inRoom) retained[targetId] = actor;
                state = new RoomCombatState(state.RoomIndex, state.Deployment,
                    removed.State.Units.Where(unit => inRoom || unit.Id != targetId).ToArray(),
                    removed.State.ExternalInteractions, removed.State.Context, state.Preview, state.ActiveCompanionBossIds);
                if (!inRoom) state = EnchantmentWorldModel.Sync(state, retained.Values.ToArray());
                callbacks.AddRange(removed.PendingCallbacks); events.AddRange(removed.Events);
            }
            return new RoomCombatResult(state, applied.Outcome, applied.Rounds, events, pendingCallbacks: callbacks,
                retainedUnits: retained.Values.ToArray());
        }
        private static RoomCombatResult Match(RoomCombatState state) => new RoomCombatResult(state, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
        private static RoomCombatResult Unsupported(string reason) => new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), reason);
    }
}

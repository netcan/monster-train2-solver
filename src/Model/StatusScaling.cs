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
            bool overrideImmunity = false, bool allowModification = true)
        {
            string? error = RoomCombatModel.Validate(source);
            if (error != null || !RoomCombatModel.KnowsStatus(added.Id)) return Unsupported(error ?? "Unmodeled added status " + added.Id);
            return ApplyRetained(source, targetId, added, sourceCardId, overrideImmunity, allowModification);
        }
        // Trigger effects retain their native actor/target objects, including HP-zero victims.
        internal static RoomCombatResult ApplyRetained(RoomCombatState source, int targetId, CombatStatus added, int sourceCardId,
            bool overrideImmunity = false, bool allowModification = true)
        {
            CombatUnit? target = source.Units.FirstOrDefault(unit => unit.Id == targetId);
            if (target == null) return Unsupported("Missing status target.");
            if (!overrideImmunity && (target.StatusImmunities.Contains(added.Id) || target.Status("immune") != null)) return Match(source);
            CombatContext? context = source.Context;
            int stacks = added.Stacks;
            if (allowModification)
            {
                StatusScalingResult scaled = StatusScalingModel.Apply(context, sourceCardId, target.Team, added.Id, stacks);
                if (!scaled.Supported) return Unsupported(scaled.UnsupportedReason!);
                context = scaled.Context; stacks = scaled.Stacks;
            }
            CombatStatus? existing = target.RegisteredStatus(added.Id);
            int old = existing?.Stacks ?? 0;
            int count = Math.Max(0, Math.Min((existing?.Stackable ?? added.Stackable) == false ? 1 : 9999, unchecked(old + stacks)));
            if (!source.Preview && sourceCardId > 0 && count > old && context?.Statistics != null)
                context = context.WithStatistics(context.LiveStatistics!.Increment(sourceCardId, "AnyStatusEffectStacksAdded", count - old,
                    requireTrackedCard: context.CardInstances?.Count == 0));
            CombatUnit changed = CardSpellModel.Copy(target, target.Health, target.Statuses.Where(status => status.Id != added.Id)
                .Concat(new[] { (existing ?? added).WithStacks(count) }).ToArray());
            var queue = new List<RoomCombatModel.QueuedCharacterTrigger>();
            string? callbackError = StatusCallbackModel.Added(source.RoomIndex, target, changed, added.Id, queue, existing ?? added);
            if (callbackError != null) return Unsupported(callbackError);
            return new RoomCombatResult(new RoomCombatState(source.RoomIndex, source.Deployment, source.Units.Select(unit => unit.Id == targetId ? changed : unit).ToArray(),
                source.ExternalInteractions, context, source.Preview), RoomOutcome.Exchanged, 0, new List<CombatEvent>(), pendingCallbacks: queue);
        }
        private static RoomCombatResult Match(RoomCombatState state) => new RoomCombatResult(state, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
        private static RoomCombatResult Unsupported(string reason) => new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), reason);
    }
}

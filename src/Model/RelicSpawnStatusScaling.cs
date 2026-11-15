using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicSpawnStatusScalingRule
    {
        public float Multiplier { get; }
        public IReadOnlyList<string> ExcludedSubtypes { get; }
        public bool ScaleStarting { get; }
        public bool ScaleApplied { get; }
        public bool OnlyListed { get; }
        public IReadOnlyList<string> StatusIds { get; }
        public IReadOnlyList<string> ScalableStatusIds { get; }
        public string? RequiredMutatorId { get; }
        public RelicSpawnStatusScalingRule(float multiplier, IReadOnlyList<string> excludedSubtypes,
            bool scaleStarting, bool scaleApplied, bool onlyListed, IReadOnlyList<string> statusIds,
            IReadOnlyList<string> scalableStatusIds, string? requiredMutatorId = null)
        {
            Multiplier = multiplier; ExcludedSubtypes = Array.AsReadOnly(excludedSubtypes.ToArray());
            ScaleStarting = scaleStarting; ScaleApplied = scaleApplied; OnlyListed = onlyListed;
            StatusIds = Array.AsReadOnly(statusIds.ToArray()); ScalableStatusIds = Array.AsReadOnly(scalableStatusIds.ToArray());
            RequiredMutatorId = requiredMutatorId;
        }
        internal bool Scales(string id) => ScalableStatusIds.Contains(id) && (!OnlyListed || StatusIds.Contains(id));
        internal int Count(string id, int count) => Scales(id) ? unchecked((int)Math.Ceiling((double)((float)count * Multiplier))) : count;
        internal int StartingDelta(string id, int count) => Scales(id) ? Math.Max(0, unchecked(Count(id, count) - count)) : 0;
    }

    internal static class RelicSpawnStatusScalingModel
    {
        internal const string Effect = "RelicEffectScaleStatusApplicationsOnSpawn";
        internal static string? Validate(CombatRelicState owner, RelicResourceEffect effect) =>
            owner.IsCovenant.HasValue && owner.DisallowedInPlacementPhase.HasValue && effect.SpawnStatusScaling != null &&
            !float.IsNaN(effect.SpawnStatusScaling.Multiplier) && !float.IsInfinity(effect.SpawnStatusScaling.Multiplier) &&
            effect.RequiredMutatorId == null && !effect.CanApplyInPreviewMode ? null : "Missing initialized birth status scaling rule.";

        internal static RoomCombatResult Apply(RoomCombatState source, int unitId, RelicResourceEffect effect)
        {
            var rule = effect.SpawnStatusScaling!;
            var actor = source.Units.SingleOrDefault(unit => unit.Id == unitId);
            if (actor == null || source.Context == null) return Fail("Missing status scaling birth references.");
            var evaluated = RelicConditionModel.Evaluate(source.Context, effect.Conditions);
            if (!evaluated.Supported) return Fail(evaluated.UnsupportedReason!);
            var state = source.WithContext(evaluated.Context);
            if (!evaluated.Passed || actor.Team != CombatTeam.Enemy || actor.Subtypes.Any(rule.ExcludedSubtypes.Contains)) return Match(state);
            // The actual callback skips when the required mutator IS present;
            // the separate tooltip query deliberately uses the opposite gate.
            if (rule.RequiredMutatorId != null)
            {
                if (state.Context!.Resources == null) return Fail("Missing birth scaling mutator roster.");
                if (state.Context.Resources.MutatorIds.Contains(rule.RequiredMutatorId)) return Match(state);
            }
            var events = new List<CombatEvent>(); var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
            var retained = new Dictionary<int, CombatUnit>(); var outcome = RoomOutcome.Exchanged;
            if (rule.ScaleStarting)
            {
                if (actor.AbilityRules == null) return Fail("Missing authored starting statuses for birth scaling.");
                foreach (var original in actor.AbilityRules.AuthoredStartingStatuses)
                {
                    int delta = rule.StartingDelta(original.Id, original.Stacks);
                    if (delta <= 0) continue;
                    var added = StatusApplicationModel.ApplyRetained(state, unitId, original.WithStacks(delta), 0, spawnEffect: true);
                    if (!added.Supported) return added;
                    state = added.State!; events.AddRange(added.Events); callbacks.AddRange(added.PendingCallbacks);
                    foreach (var unit in added.RetainedUnits) retained[unit.Id] = unit;
                    if (added.Outcome != RoomOutcome.Exchanged) outcome = added.Outcome;
                }
            }
            if (rule.ScaleApplied)
            {
                actor = state.Units.SingleOrDefault(unit => unit.Id == unitId);
                if (actor == null) return Fail("Birth scaling actor vanished before trigger parameter rewrite.");
                var triggers = actor.Triggers.ToArray();
                // Exact native runtime types, in their original two passes.
                foreach (string type in new[] { "CardEffectAddStatusEffect", "CardEffectEnchant" })
                    for (int t = 0; t < triggers.Length; t++)
                    {
                        var effects = triggers[t].Effects.ToArray(); bool changed = false;
                        for (int e = 0; e < effects.Length; e++)
                        {
                            var current = effects[e];
                            if (current.Type != type) continue;
                            if (type == "CardEffectAddStatusEffect")
                            {
                                if (current.Action == null) return Fail("Missing runtime status trigger parameters for birth scaling.");
                                effects[e] = current.WithAction(current.Action.WithStatuses(current.Action.Statuses.Select(status => status.WithStacks(rule.Count(status.Id, status.Stacks))).ToArray()));
                            }
                            else
                            {
                                if (current.Enchantment == null) return Fail("Missing runtime enchantment parameters for birth scaling.");
                                var enchantment = current.Enchantment;
                                effects[e] = current.WithEnchantment(new EnchantmentRule(enchantment.Targeting,
                                    enchantment.StatusPool.Select(status => status.WithStacks(rule.Count(status.Id, status.Stacks))).ToArray(),
                                    enchantment.State, enchantment.Bound, enchantment.HasParentCard, enchantment.Upgrade));
                            }
                            changed = true;
                        }
                        if (changed) triggers[t] = triggers[t].WithEffects(effects);
                    }
                var updated = actor.WithTriggers(triggers);
                state = state.WithUnits(state.Units.Select(unit => unit.Id == unitId ? updated : unit).ToArray());
            }
            // OnCharacterAdded itself yields nothing. Leave shared status callbacks
            // queued until the existing enclosing birth pipeline drains them.
            return new RoomCombatResult(state, outcome, 0, events, pendingCallbacks: callbacks, retainedUnits: retained.Values.ToArray());
        }
        private static RoomCombatResult Match(RoomCombatState state) => new(state, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
        private static RoomCombatResult Fail(string message) => new(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), message);
    }
}

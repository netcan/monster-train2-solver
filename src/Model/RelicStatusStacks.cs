using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicStatusStacks
    {
        public int EffectIndex { get; }
        public string SourceTeam { get; }
        public string StatusId { get; }
        public int AdditionalStacks { get; }
        public int AddCondition { get; }
        public string? TriggerStatusId { get; }
        public CombatStatus? AddedStatusDefinition { get; }
        public IReadOnlyList<RelicConditionState> Conditions { get; }
        public RelicStatusStacks(int effectIndex, string sourceTeam, string statusId, int additionalStacks,
            int addCondition, IReadOnlyList<RelicConditionState>? conditions = null,
            string? triggerStatusId = null, CombatStatus? addedStatusDefinition = null)
        {
            EffectIndex = effectIndex; SourceTeam = sourceTeam; StatusId = statusId;
            AdditionalStacks = additionalStacks; AddCondition = addCondition;
            TriggerStatusId = triggerStatusId; AddedStatusDefinition = addedStatusDefinition;
            Conditions = Array.AsReadOnly((conditions ?? Array.Empty<RelicConditionState>()).ToArray());
        }
        internal RelicStatusStacks WithConditions(IReadOnlyList<RelicConditionState> conditions) =>
            new RelicStatusStacks(EffectIndex, SourceTeam, StatusId, AdditionalStacks, AddCondition, conditions,
                TriggerStatusId, AddedStatusDefinition);
    }

    public static class RelicStatusStackModel
    {
        private static readonly string[] RemovedStatusExclusions =
        { "CardEffectTransferAllStatusEffects", "CardEffectMultiplyStatusEffect", "CardEffectTransferAllStatusEffectsFromUnit" };

        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => new { type, index })
                .Where(item => item.type == RelicModel.AddStatusOnApplied || item.type == RelicModel.AddStatusOnOtherStatusRemoved)
                .Select(item => item.index).ToArray();
            if (!indices.SequenceEqual((relic.StatusStackAdditions ?? Array.Empty<RelicStatusStacks>()).Select(rule => rule.EffectIndex)))
                return "Missing or unordered relic status-stack additions on " + relic.AssetKey + ".";
            foreach (RelicStatusStacks rule in relic.StatusStackAdditions ?? Array.Empty<RelicStatusStacks>())
                if (string.IsNullOrEmpty(rule.SourceTeam) || !RoomCombatModel.KnowsStatus(rule.StatusId) ||
                    (rule.TriggerStatusId == null) != (relic.EffectTypes[rule.EffectIndex] == RelicModel.AddStatusOnApplied) ||
                    rule.TriggerStatusId != null && (!RoomCombatModel.KnowsStatus(rule.TriggerStatusId) ||
                        rule.AddedStatusDefinition?.Id != rule.StatusId || rule.Conditions.Count != 0))
                    return "Unmodeled relic status-stack definition on " + relic.AssetKey + ".";
            return null;
        }

        private static bool ExcludesAdders(string? fromEffectType) => fromEffectType == "CardEffectTransferAllStatusEffects" ||
            fromEffectType == "CardEffectMultiplyStatusEffect" || fromEffectType == "CardEffectTransferAllStatusEffectsFromUnit";

        // Native value queries ignore outer conditions. Adders see the running
        // amount, while first-application tests see the unchanged target dictionary.
        public static int GetStacks(IReadOnlyList<CombatRelicState>? relics, string statusId, int incoming,
            CombatTeam? targetTeam, bool hasStatus, bool fromPermanentUpgrade = false, bool deferred = false,
            string? fromEffectType = null)
        {
            if (ExcludesAdders(fromEffectType)) return incoming;
            int total = incoming;
            foreach (CombatRelicState relic in relics ?? Array.Empty<CombatRelicState>())
            foreach (RelicStatusStacks rule in relic.StatusStackAdditions ?? Array.Empty<RelicStatusStacks>())
            {
                if (rule.TriggerStatusId != null) continue;
                if (rule.StatusId != statusId || targetTeam.HasValue && rule.SourceTeam != "None" &&
                    rule.SourceTeam != (targetTeam == CombatTeam.Player ? "Monsters" : "Heroes")) continue;
                bool add = deferred ? fromPermanentUpgrade : !fromPermanentUpgrade &&
                    (rule.AddCondition == 1 ? total > 0 : rule.AddCondition == 2 ? targetTeam.HasValue && !hasStatus : true);
                if (add) total = unchecked(total + rule.AdditionalStacks);
            }
            return total;
        }

        internal static RoomCombatResult OnStatusRemoved(RoomCombatState source, int targetId, string statusId,
            int requestedStacks, string? fromEffectType)
        {
            if (source.Context?.Relics == null) return new RoomCombatResult(source, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
            RoomCombatState state = source;
            var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
            foreach (CombatRelicState relic in source.Context.Relics)
            {
                string? error = Validate(relic);
                if (error != null) return Unsupported(error);
                foreach (RelicStatusStacks rule in relic.StatusStackAdditions ?? Array.Empty<RelicStatusStacks>())
                {
                    if (rule.TriggerStatusId != statusId || RemovedStatusExclusions.Contains(fromEffectType ?? "", StringComparer.Ordinal)) continue;
                    CombatUnit? target = state.Units.FirstOrDefault(unit => unit.Id == targetId);
                    if (target == null) return Unsupported("Missing status removal target.");
                    if (rule.SourceTeam != "None" && rule.SourceTeam != (target.Team == CombatTeam.Player ? "Monsters" : "Heroes")) continue;
                    int amount = unchecked(rule.AdditionalStacks * requestedStacks);
                    RoomCombatResult added = StatusApplicationModel.ApplyRetained(state, targetId,
                        rule.AddedStatusDefinition!.WithStacks(amount), 0);
                    if (!added.Supported) return added;
                    state = added.State!;
                    callbacks.AddRange(added.PendingCallbacks);
                }
            }
            return new RoomCombatResult(state, RoomOutcome.Exchanged, 0, new List<CombatEvent>(), pendingCallbacks: callbacks);
        }

        internal static StatusScalingResult Notify(CombatContext? source, bool preview, string? fromEffectType)
        {
            if (source?.Relics == null) return new StatusScalingResult(source, 0);
            CombatContext context = source;
            // Both callbacks evaluate conditions before the status dictionary and
            // responsible-card statistics change. Only the adder callback records.
            for (int phase = 0; phase < 2; phase++)
            for (int relicIndex = 0; relicIndex < context.Relics!.Count; relicIndex++)
            {
                if (phase == 0)
                {
                    var passive = RelicConditionModel.EvaluatePassiveCardPlayConditions(context, context.Relics[relicIndex]);
                    if (!passive.Supported) return new StatusScalingResult(null, 0, passive.UnsupportedReason);
                    context = passive.Context;
                }
                var rules = context.Relics![relicIndex].StatusStackAdditions;
                if (rules == null) continue;
                for (int ruleIndex = 0; ruleIndex < rules.Count; ruleIndex++)
                {
                    RelicStatusStacks rule = context.Relics![relicIndex].StatusStackAdditions![ruleIndex];
                    if (rule.TriggerStatusId != null) continue;
                    RelicConditionEvaluation checkedCondition = RelicConditionModel.Evaluate(context, rule.Conditions);
                    if (!checkedCondition.Supported) return new StatusScalingResult(null, 0, checkedCondition.UnsupportedReason);
                    context = checkedCondition.Context;
                    if (phase == 0 || !checkedCondition.Passed || preview || ExcludesAdders(fromEffectType)) continue;
                    RelicConditionRecord record = RelicConditionModel.Record(context, rule.Conditions);
                    if (!record.Supported) return new StatusScalingResult(null, 0, record.UnsupportedReason);
                    context = record.Context;
                    CombatRelicState relic = context.Relics![relicIndex];
                    var changedRules = relic.StatusStackAdditions!.ToArray();
                    changedRules[ruleIndex] = rule.WithConditions(record.Conditions);
                    var changedRelics = context.Relics.ToArray();
                    changedRelics[relicIndex] = relic.WithStatusStackAdditions(changedRules);
                    context = context.WithRelics(changedRelics);
                }
            }
            return new StatusScalingResult(context, 0);
        }

        private static RoomCombatResult Unsupported(string reason) => new(null, RoomOutcome.Unsupported, 0,
            new List<CombatEvent>(), reason);
    }
}

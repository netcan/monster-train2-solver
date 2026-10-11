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
        public IReadOnlyList<RelicConditionState> Conditions { get; }
        public RelicStatusStacks(int effectIndex, string sourceTeam, string statusId, int additionalStacks,
            int addCondition, IReadOnlyList<RelicConditionState>? conditions = null)
        {
            EffectIndex = effectIndex; SourceTeam = sourceTeam; StatusId = statusId;
            AdditionalStacks = additionalStacks; AddCondition = addCondition;
            Conditions = Array.AsReadOnly((conditions ?? Array.Empty<RelicConditionState>()).ToArray());
        }
        internal RelicStatusStacks WithConditions(IReadOnlyList<RelicConditionState> conditions) =>
            new RelicStatusStacks(EffectIndex, SourceTeam, StatusId, AdditionalStacks, AddCondition, conditions);
    }

    public static class RelicStatusStackModel
    {
        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => new { type, index })
                .Where(item => item.type == RelicModel.AddStatusOnApplied).Select(item => item.index).ToArray();
            if (!indices.SequenceEqual((relic.StatusStackAdditions ?? Array.Empty<RelicStatusStacks>()).Select(rule => rule.EffectIndex)))
                return "Missing or unordered relic status-stack additions on " + relic.AssetKey + ".";
            foreach (RelicStatusStacks rule in relic.StatusStackAdditions ?? Array.Empty<RelicStatusStacks>())
                if (string.IsNullOrEmpty(rule.SourceTeam) || !RoomCombatModel.KnowsStatus(rule.StatusId))
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
                if (rule.StatusId != statusId || targetTeam.HasValue && rule.SourceTeam != "None" &&
                    rule.SourceTeam != (targetTeam == CombatTeam.Player ? "Monsters" : "Heroes")) continue;
                bool add = deferred ? fromPermanentUpgrade : !fromPermanentUpgrade &&
                    (rule.AddCondition == 1 ? total > 0 : rule.AddCondition == 2 ? targetTeam.HasValue && !hasStatus : true);
                if (add) total = unchecked(total + rule.AdditionalStacks);
            }
            return total;
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
                var rules = context.Relics[relicIndex].StatusStackAdditions;
                if (rules == null) continue;
                for (int ruleIndex = 0; ruleIndex < rules.Count; ruleIndex++)
                {
                    RelicStatusStacks rule = context.Relics![relicIndex].StatusStackAdditions![ruleIndex];
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
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicEarlyDiscardEnergyEvent
    {
        public string RelicDataId { get; }
        public int EffectIndex { get; }
        public int EnergyBefore { get; }
        public int EnergyAfter { get; }
        internal RelicEarlyDiscardEnergyEvent(string relicDataId, int effectIndex, int before, int after)
        { RelicDataId = relicDataId; EffectIndex = effectIndex; EnergyBefore = before; EnergyAfter = after; }
    }

    public sealed class RelicEarlyDiscardEnergyResult
    {
        public CombatContext? Context { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => Context != null;
        public IReadOnlyList<RelicEarlyDiscardEnergyEvent> Events { get; }
        internal RelicEarlyDiscardEnergyResult(CombatContext? context, string? error = null,
            IReadOnlyList<RelicEarlyDiscardEnergyEvent>? events = null)
        { Context = context; UnsupportedReason = error;
            Events = Array.AsReadOnly((events ?? Array.Empty<RelicEarlyDiscardEnergyEvent>()).ToArray()); }
    }

    public static class RelicEarlyDiscardEnergyModel
    {
        public static RelicEarlyDiscardEnergyResult CardDiscarded(CombatContext source, bool triggeredByCard,
            bool placement = false, bool preview = false)
        {
            if (source.Relics == null) return new RelicEarlyDiscardEnergyResult(source);
            CombatContext context = source;
            var events = new List<RelicEarlyDiscardEnergyEvent>();
            for (int relicIndex = 0; relicIndex < context.Relics!.Count; relicIndex++)
            {
                CombatRelicState owner = context.Relics[relicIndex];
                if (!owner.EffectTypes.Contains(RelicResourceModel.EnergyOnCardEarlyDiscard)) continue;
                string? validation = RelicResourceModel.Validate(owner);
                if (validation != null) return Fail(validation, events);
                if (placement && owner.DisallowedInPlacementPhase == true) continue;
                IReadOnlyList<RelicResourceEffect> rules = owner.ResourceEffects ?? Array.Empty<RelicResourceEffect>();
                for (int ruleIndex = 0; ruleIndex < rules.Count; ruleIndex++)
                {
                    RelicResourceEffect rule = rules[ruleIndex];
                    if (rule.Type != RelicResourceModel.EnergyOnCardEarlyDiscard || preview && !rule.CanApplyInPreviewMode) continue;
                    RelicConditionEvaluation evaluation = RelicConditionModel.Evaluate(context, rule.Conditions);
                    if (!evaluation.Supported) return Fail(evaluation.UnsupportedReason!, events);
                    context = evaluation.Context;
                    if (!triggeredByCard || !evaluation.Passed) continue;

                    string? energyError = EnergyModel.Validate(context);
                    if (energyError != null) return Fail(energyError, events);
                    RelicConditionRecord recorded = RelicConditionModel.Record(context, rule.Conditions);
                    if (!recorded.Supported) return Fail(recorded.UnsupportedReason!, events);
                    context = ReplaceConditions(recorded.Context, relicIndex, ruleIndex, recorded.Conditions);

                    int before = context.QueryFrame!.Energy!.Value;
                    int after = Math.Min(context.EnergyState!.Maximum, unchecked(before + rule.Amount));
                    context = context.WithQueryFrame(context.QueryFrame.With(energy: after));
                    events.Add(new RelicEarlyDiscardEnergyEvent(owner.DataId, rule.EffectIndex, before, after));
                }
            }
            return new RelicEarlyDiscardEnergyResult(context, events: events);
        }

        internal static string? Validate(CombatRelicState relic)
        {
            IReadOnlyList<RelicResourceEffect> resourceEffects = relic.ResourceEffects ?? Array.Empty<RelicResourceEffect>();
            int[] indices = relic.EffectTypes.Select((type, index) => new { type, index })
                .Where(item => item.type == RelicResourceModel.EnergyOnCardEarlyDiscard)
                .Select(item => item.index).ToArray();
            int[] rules = resourceEffects
                .Where(rule => rule.Type == RelicResourceModel.EnergyOnCardEarlyDiscard).Select(rule => rule.EffectIndex).ToArray();
            if (!indices.SequenceEqual(rules)) return "Missing ordered early-discard energy definition on " + relic.AssetKey + ".";
            if (indices.Length > 0 && !relic.DisallowedInPlacementPhase.HasValue)
                return "Missing native early-discard relic placement restriction.";
            foreach (RelicResourceEffect rule in resourceEffects.Where(item => item.Type == RelicResourceModel.EnergyOnCardEarlyDiscard))
                if (rule.Conditions.Any(condition => condition.Comparator < 0 || condition.Comparator > 7))
                    return "Malformed native early-discard energy condition.";
            return null;
        }

        private static CombatContext ReplaceConditions(CombatContext context, int relicIndex, int ruleIndex,
            IReadOnlyList<RelicConditionState> conditions)
        {
            CombatRelicState owner = context.Relics![relicIndex];
            var rules = owner.ResourceEffects!.ToArray();
            rules[ruleIndex] = rules[ruleIndex].WithConditions(conditions);
            var relics = context.Relics.ToArray(); relics[relicIndex] = owner.WithResourceEffects(rules);
            return context.WithRelics(relics);
        }

        private static RelicEarlyDiscardEnergyResult Fail(string error, IReadOnlyList<RelicEarlyDiscardEnergyEvent> events) =>
            new RelicEarlyDiscardEnergyResult(null, error, events);
    }
}

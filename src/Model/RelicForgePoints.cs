using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicForgePointModifier
    {
        public int EffectIndex { get; }
        public int AdditionalPoints { get; }
        public bool CanApplyInPreviewMode { get; }
        public int ConditionCount { get; }
        public IReadOnlyList<RelicConditionState> Conditions { get; }

        public RelicForgePointModifier(int effectIndex, int additionalPoints, bool canApplyInPreviewMode,
            IReadOnlyList<RelicConditionState>? conditions = null, int conditionCount = 0)
        {
            EffectIndex = effectIndex;
            AdditionalPoints = additionalPoints;
            CanApplyInPreviewMode = canApplyInPreviewMode;
            Conditions = Array.AsReadOnly((conditions ?? Array.Empty<RelicConditionState>()).ToArray());
            ConditionCount = conditions?.Count ?? conditionCount;
        }

        internal RelicForgePointModifier WithConditions(IReadOnlyList<RelicConditionState> conditions) =>
            new RelicForgePointModifier(EffectIndex, AdditionalPoints, CanApplyInPreviewMode, conditions);
    }

    internal sealed class RelicForgePointResult
    {
        public CombatContext? Context { get; }
        public int AdditionalPoints { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => Context != null;
        internal RelicForgePointResult(CombatContext? context, int additionalPoints, string? error = null)
        { Context = context; AdditionalPoints = additionalPoints; UnsupportedReason = error; }
    }

    internal static class RelicForgePointModel
    {
        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => new { type, index })
                .Where(item => item.type == RelicModel.AdditionalForgePoints).Select(item => item.index).ToArray();
            IReadOnlyList<RelicForgePointModifier> rules = relic.ForgePointModifiers ?? Array.Empty<RelicForgePointModifier>();
            if (!indices.SequenceEqual(rules.Select(rule => rule.EffectIndex)))
                return "Missing or unordered relic Forge Points modifiers on " + relic.AssetKey + ".";
            if (indices.Length > 0 && !relic.DisallowedInPlacementPhase.HasValue)
                return "Missing native Forge Points relic placement restriction on " + relic.AssetKey + ".";
            foreach (RelicForgePointModifier rule in rules)
                if (rule.ConditionCount != rule.Conditions.Count || rule.Conditions.Any(condition => condition.Comparator < 0 || condition.Comparator > 7))
                    return "Malformed relic Forge Points conditions on " + relic.AssetKey + ".";
            return null;
        }

        internal static RelicForgePointResult Apply(CombatContext source, int baseAmount, bool placement, bool preview)
        {
            if (baseAmount <= 0 || source.Relics == null) return new RelicForgePointResult(source, 0);
            CombatContext context = source;
            int total = 0;
            for (int relicIndex = 0; relicIndex < context.Relics!.Count; relicIndex++)
            {
                CombatRelicState relic = context.Relics[relicIndex];
                if (placement && relic.DisallowedInPlacementPhase == true) continue;
                IReadOnlyList<RelicForgePointModifier> rules = relic.ForgePointModifiers ?? Array.Empty<RelicForgePointModifier>();
                for (int ruleIndex = 0; ruleIndex < rules.Count; ruleIndex++)
                {
                    RelicForgePointModifier rule = rules[ruleIndex];
                    if (preview && !rule.CanApplyInPreviewMode) continue;
                    RelicConditionEvaluation checkedCondition = RelicConditionModel.Evaluate(context, rule.Conditions);
                    if (!checkedCondition.Supported) return new RelicForgePointResult(null, 0, checkedCondition.UnsupportedReason);
                    context = checkedCondition.Context;
                    if (!checkedCondition.Passed || rule.AdditionalPoints == 0) continue;
                    total = unchecked(total + rule.AdditionalPoints);
                    if (preview) continue;
                    RelicConditionRecord record = RelicConditionModel.Record(context, rule.Conditions);
                    if (!record.Supported) return new RelicForgePointResult(null, 0, record.UnsupportedReason);
                    context = record.Context;
                    relic = context.Relics![relicIndex];
                    rules = relic.ForgePointModifiers!.ToArray();
                    rules = rules.Select((item, index) => index == ruleIndex ? rule.WithConditions(record.Conditions) : item).ToArray();
                    CombatRelicState[] changedRelics = context.Relics.ToArray();
                    changedRelics[relicIndex] = relic.WithForgePointModifiers(rules);
                    context = context.WithRelics(changedRelics);
                }
            }
            return new RelicForgePointResult(context, total);
        }
    }
}

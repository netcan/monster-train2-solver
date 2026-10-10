using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal sealed class RelicConditionEvaluation
    {
        public CombatContext Context { get; }
        public bool Passed { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => UnsupportedReason == null;
        internal RelicConditionEvaluation(CombatContext context, bool passed, string? unsupportedReason = null)
        { Context = context; Passed = passed; UnsupportedReason = unsupportedReason; }
    }

    internal sealed class RelicConditionRecord
    {
        public CombatContext Context { get; }
        public IReadOnlyList<RelicConditionState> Conditions { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => UnsupportedReason == null;
        internal RelicConditionRecord(CombatContext context, IReadOnlyList<RelicConditionState> conditions,
            string? unsupportedReason = null)
        { Context = context; Conditions = Array.AsReadOnly(conditions.ToArray()); UnsupportedReason = unsupportedReason; }
    }

    internal static class RelicConditionModel
    {
        internal static RelicConditionEvaluation Evaluate(CombatContext source, IReadOnlyList<RelicConditionState> conditions)
        {
            CombatContext context = source;
            foreach (RelicConditionState condition in conditions)
            {
                if (!condition.AllowMultiple && condition.Triggered)
                    return new RelicConditionEvaluation(context, false);

                int value;
                if (condition.TrackTriggerCount) value = condition.DurationTriggerCount;
                else
                {
                    StatisticQueryResult query = StatisticQueryModel.Evaluate(context, condition.Query);
                    if (!query.Supported)
                        return new RelicConditionEvaluation(context, false, query.UnsupportedReason);
                    context = query.Context!;
                    value = query.Value;
                }
                int delta = unchecked(value - condition.ValueAtLastTrigger);
                bool passed = condition.TrackTriggerCount ? delta < condition.Value :
                    (condition.Comparator & 2) != 0 && delta == condition.Value ||
                    (condition.Comparator & 4) != 0 && delta > condition.Value ||
                    (condition.Comparator & 1) != 0 && delta < condition.Value;
                if (!passed) return new RelicConditionEvaluation(context, false);
            }
            return new RelicConditionEvaluation(context, true);
        }

        internal static RelicConditionRecord Record(CombatContext source, IReadOnlyList<RelicConditionState> conditions)
        {
            CombatContext context = source;
            var recorded = new List<RelicConditionState>(conditions.Count);
            foreach (RelicConditionState condition in conditions)
            {
                int value;
                if (condition.TrackTriggerCount) value = condition.DurationTriggerCount;
                else
                {
                    StatisticQueryResult query = StatisticQueryModel.Evaluate(context, condition.Query);
                    if (!query.Supported) return new RelicConditionRecord(context, recorded, query.UnsupportedReason);
                    context = query.Context!;
                    value = query.Value;
                }
                recorded.Add(condition.Recorded(value));
            }
            return new RelicConditionRecord(context, recorded);
        }

        internal static CombatContext EndDuration(CombatContext context, string duration)
        {
            if (context.Relics == null) return context;
            var relics = context.Relics.Select(relic =>
            {
                if (relic.SpawnStatuses != null)
                    relic = relic.WithSpawnStatuses(relic.SpawnStatuses.Select(rule => rule.WithConditions(
                        rule.Conditions.Select(condition => condition.Reset(duration)).ToArray())).ToArray());
                if (relic.CardModifiers != null)
                    relic = relic.WithCardModifiers(relic.CardModifiers.Select(effect => effect.WithConditions(
                        effect.Conditions.Select(condition => condition.Reset(duration)).ToArray())).ToArray());
                return relic;
            }).ToArray();
            return context.WithRelics(relics);
        }
    }
}

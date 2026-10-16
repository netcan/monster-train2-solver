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
        // RelicState invokes these base callbacks on every effect, even when
        // that effect has no specialized status/birth behavior. Failed global
        // conditions can still refresh native deckStats membership.
        internal static RelicConditionEvaluation EvaluatePassiveCardPlayConditions(CombatContext source, CombatRelicState relic)
        {
            CombatContext context = source;
            for (int index = 0; index < relic.EffectTypes.Count; index++)
            {
                IReadOnlyList<RelicConditionState>? conditions = null;
                if (relic.EffectTypes[index] == RelicModel.ModifyCardMagicPowerOnCardPlayed)
                    conditions = relic.CardPlayMagicPowerModifiers?.SingleOrDefault(effect => effect.EffectIndex == index)?.Conditions;
                if (relic.EffectTypes[index] == RelicModel.CardDrawOnCardPlay)
                    conditions = relic.CardDrawOnCardPlayModifiers?.SingleOrDefault(effect => effect.EffectIndex == index)?.Conditions;
                if (relic.EffectTypes[index] == RelicModel.CopyCardOnCardPlay)
                    conditions = relic.CopyCardOnCardPlayModifiers?.SingleOrDefault(effect => effect.EffectIndex == index)?.Conditions;
                if (relic.EffectTypes[index] == RelicModel.FirstRoomCardOnAllFloors)
                    conditions = relic.RoomAttachmentReplications?.SingleOrDefault(effect => effect.EffectIndex == index)?.Conditions;
                if (conditions == null) continue;
                var evaluation = Evaluate(context, conditions);
                if (!evaluation.Supported) return evaluation;
                context = evaluation.Context;
            }
            return new RelicConditionEvaluation(context, true);
        }
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
                if (relic.TriggerAdditions != null)
                    relic = relic.WithTriggerAdditions(relic.TriggerAdditions.Select(rule => rule.WithConditions(
                        rule.Conditions.Select(condition => condition.Reset(duration)).ToArray())).ToArray());
                if (relic.StatusStackAdditions != null)
                    relic = relic.WithStatusStackAdditions(relic.StatusStackAdditions.Select(rule => rule.WithConditions(
                        rule.Conditions.Select(condition => condition.Reset(duration)).ToArray())).ToArray());
                if (relic.StatusMagnitudes != null)
                    relic = relic.WithStatusMagnitudes(relic.StatusMagnitudes.Select(rule => rule.WithConditions(
                        rule.Conditions.Select(condition => condition.Reset(duration)).ToArray())).ToArray());
                if (relic.ResourceEffects != null)
                    relic = relic.WithResourceEffects(relic.ResourceEffects.Select(rule => rule.WithConditions(
                        rule.Conditions.Select(condition => condition.Reset(duration)).ToArray())).ToArray());
                if (relic.CardPlayMagicPowerModifiers != null)
                    relic = relic.WithCardPlayMagicPowerModifiers(relic.CardPlayMagicPowerModifiers.Select(rule => rule.WithConditions(
                        rule.Conditions.Select(condition => condition.Reset(duration)).ToArray())).ToArray());
                if (relic.CardDrawOnCardPlayModifiers != null)
                    relic = relic.WithCardDrawOnCardPlayModifiers(relic.CardDrawOnCardPlayModifiers.Select(rule => rule.WithConditions(
                        rule.Conditions.Select(condition => condition.Reset(duration)).ToArray())).ToArray());
                if (relic.CopyCardOnCardPlayModifiers != null)
                    relic = relic.WithCopyCardOnCardPlayModifiers(relic.CopyCardOnCardPlayModifiers.Select(rule => rule.WithConditions(
                        rule.Conditions.Select(condition => condition.Reset(duration)).ToArray())).ToArray());
                if (relic.RoomAttachmentReplications != null)
                    relic = relic.WithRoomAttachmentReplications(relic.RoomAttachmentReplications.Select(rule => rule.WithConditions(
                        rule.Conditions.Select(condition => condition.Reset(duration)).ToArray())).ToArray());
                if (relic.ForgePointModifiers != null)
                    relic = relic.WithForgePointModifiers(relic.ForgePointModifiers.Select(rule => rule.WithConditions(
                        rule.Conditions.Select(condition => condition.Reset(duration)).ToArray())).ToArray());
                return relic;
            }).ToArray();
            return context.WithRelics(relics);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicStatusMagnitude
    {
        public int EffectIndex { get; }
        public int SourceTeam { get; }
        public string StatusId { get; }
        public int Modifier { get; }
        public IReadOnlyList<RelicConditionState> Conditions { get; }

        public RelicStatusMagnitude(int effectIndex, int sourceTeam, string statusId, int modifier,
            IReadOnlyList<RelicConditionState>? conditions = null)
        {
            EffectIndex = effectIndex; SourceTeam = sourceTeam; StatusId = statusId; Modifier = modifier;
            Conditions = Array.AsReadOnly((conditions ?? Array.Empty<RelicConditionState>()).ToArray());
        }

        internal RelicStatusMagnitude WithConditions(IReadOnlyList<RelicConditionState> conditions) =>
            new RelicStatusMagnitude(EffectIndex, SourceTeam, StatusId, Modifier, conditions);
    }

    public static class RelicStatusMagnitudeModel
    {
        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => new { type, index })
                .Where(item => item.type == RelicModel.ModifyStatusMagnitude).Select(item => item.index).ToArray();
            if (!indices.SequenceEqual((relic.StatusMagnitudes ?? Array.Empty<RelicStatusMagnitude>()).Select(rule => rule.EffectIndex)))
                return "Missing or unordered relic status-magnitude definitions on " + relic.AssetKey + ".";
            foreach (RelicStatusMagnitude rule in relic.StatusMagnitudes ?? Array.Empty<RelicStatusMagnitude>())
                if (string.IsNullOrEmpty(rule.StatusId) || !RoomCombatModel.KnowsStatus(rule.StatusId) ||
                    rule.StatusId != "buff" && rule.StatusId != "debuff" && rule.StatusId != "spikes" &&
                    rule.StatusId != "regen" && rule.StatusId != "mageblade" && rule.StatusId != "titanskin")
                    return "Unmodeled relic status-magnitude consumer " + rule.StatusId + " on " + relic.AssetKey + ".";
            return null;
        }

        // Native magnitude queries evaluate outer conditions before filtering the
        // status or team. They do not notify the relic or record its conditions.
        public static StatusScalingResult GetModifier(CombatContext? source, string statusId, int characterTeam)
        {
            if (source?.Relics == null) return new StatusScalingResult(source, 0);
            CombatContext context = source;
            int total = 0;
            foreach (CombatRelicState relic in source.Relics)
            {
                string? error = Validate(relic);
                if (error != null) return new StatusScalingResult(null, 0, error);
                foreach (RelicStatusMagnitude rule in relic.StatusMagnitudes ?? Array.Empty<RelicStatusMagnitude>())
                {
                    RelicConditionEvaluation checkedCondition = RelicConditionModel.Evaluate(context, rule.Conditions);
                    if (!checkedCondition.Supported)
                        return new StatusScalingResult(null, 0, checkedCondition.UnsupportedReason);
                    context = checkedCondition.Context;
                    if (checkedCondition.Passed && rule.StatusId == statusId &&
                        (characterTeam == 0 || (characterTeam & rule.SourceTeam) == rule.SourceTeam))
                        total = unchecked(total + rule.Modifier);
                }
            }
            return new StatusScalingResult(context, total);
        }
    }
}

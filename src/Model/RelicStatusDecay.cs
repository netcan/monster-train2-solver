using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicStatusDecay
    {
        public int EffectIndex { get; }
        public int SourceTeam { get; }
        public IReadOnlyList<string> StatusIds { get; }
        // Native GetShouldRemove never evaluates or records these conditions.
        public IReadOnlyList<RelicConditionState> Conditions { get; }
        public RelicStatusDecay(int effectIndex, int sourceTeam, IReadOnlyList<string> statusIds,
            IReadOnlyList<RelicConditionState>? conditions = null)
        {
            EffectIndex = effectIndex; SourceTeam = sourceTeam;
            StatusIds = Array.AsReadOnly(statusIds.ToArray());
            Conditions = Array.AsReadOnly((conditions ?? Array.Empty<RelicConditionState>()).ToArray());
        }
    }

    public sealed class RelicStatusDecayResult
    {
        public CombatContext? Context { get; }
        public bool ShouldRemove { get; }
        public string? SuppressingRelicId { get; }
        public int? SuppressingEffectIndex { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => UnsupportedReason == null;
        internal RelicStatusDecayResult(CombatContext? context, bool shouldRemove, string? relicId = null,
            int? effectIndex = null, string? error = null)
        { Context = context; ShouldRemove = shouldRemove; SuppressingRelicId = relicId;
            SuppressingEffectIndex = effectIndex; UnsupportedReason = error; }
    }

    public static class RelicStatusDecayModel
    {
        private static string? ValidateDefinitions(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => new { type, index })
                .Where(item => item.type == RelicModel.PreventStatusDecay).Select(item => item.index).ToArray();
            if (!indices.SequenceEqual((relic.StatusDecays ?? Array.Empty<RelicStatusDecay>()).Select(rule => rule.EffectIndex)))
                return "Missing or unordered relic status-decay definitions on " + relic.AssetKey + ".";
            foreach (RelicStatusDecay rule in relic.StatusDecays ?? Array.Empty<RelicStatusDecay>())
                if (rule.StatusIds.Any(string.IsNullOrEmpty))
                    return "Malformed relic status-decay status filter on " + relic.AssetKey + ".";
            return null;
        }

        internal static string? Validate(CombatRelicState relic)
        {
            string? error = ValidateDefinitions(relic);
            if (error != null) return error;
            // Other status families also consume or mark stacks on trigger/card
            // boundaries. Keep them guarded until their complete consumers have
            // native evidence, rather than supporting their natural decay alone.
            foreach (RelicStatusDecay rule in relic.StatusDecays ?? Array.Empty<RelicStatusDecay>())
                foreach (string id in rule.StatusIds)
                    if (id != "buff" && id != "poison")
                        return "Unmodeled relic status-decay consumer " + id + " on " + relic.AssetKey + ".";
            return null;
        }

        public static RelicStatusDecayResult GetShouldRemove(CombatContext? source, bool removeAtContext,
            string statusId, int characterTeam, bool preview = false)
        {
            // Native returns before consulting relics for a non-removing context.
            if (!removeAtContext) return new RelicStatusDecayResult(source, false);
            foreach (CombatRelicState relic in source?.Relics ?? Array.Empty<CombatRelicState>())
            {
                string? error = ValidateDefinitions(relic);
                if (error != null) return new RelicStatusDecayResult(source, false, error: error);
                foreach (RelicStatusDecay rule in relic.StatusDecays ?? Array.Empty<RelicStatusDecay>())
                {
                    // This native effect always permits preview. Team None is a
                    // wildcard; all other values require equality, not flag overlap.
                    if ((rule.SourceTeam == 0 || rule.SourceTeam == characterTeam) && rule.StatusIds.Contains(statusId))
                        return new RelicStatusDecayResult(source, false, relic.DataId, rule.EffectIndex);
                }
            }
            return new RelicStatusDecayResult(source, true);
        }
    }
}

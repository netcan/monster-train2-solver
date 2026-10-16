using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicUnstableRebalanceModifier
    {
        public int EffectIndex { get; }
        public int SourceTeam { get; }
        public int AdditionalProcDamage { get; }
        public int AdditionalExplosionDamage { get; }
        public int AdditionalGrowthRate { get; }
        public bool RequireOnlyOneUnitWithUnstable { get; }
        public string StatusId { get; }
        public int StatusStacks { get; }

        public RelicUnstableRebalanceModifier(int effectIndex, int sourceTeam, int additionalProcDamage,
            int additionalExplosionDamage, int additionalGrowthRate, bool requireOnlyOneUnitWithUnstable,
            string statusId, int statusStacks)
        {
            EffectIndex = effectIndex;
            SourceTeam = sourceTeam;
            AdditionalProcDamage = additionalProcDamage;
            AdditionalExplosionDamage = additionalExplosionDamage;
            AdditionalGrowthRate = additionalGrowthRate;
            RequireOnlyOneUnitWithUnstable = requireOnlyOneUnitWithUnstable;
            StatusId = statusId;
            StatusStacks = statusStacks;
        }
    }

    public static class RelicUnstableRebalanceModel
    {
        public const string Effect = "RelicEffectRebalanceUnstable";

        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => new { type, index })
                .Where(item => item.type == Effect).Select(item => item.index).ToArray();
            RelicUnstableRebalanceModifier[] rules = (relic.UnstableRebalances ??
                Array.Empty<RelicUnstableRebalanceModifier>()).ToArray();
            if (!indices.SequenceEqual(rules.Select(rule => rule.EffectIndex)))
                return "Missing or unordered unstable-rebalance definitions on " + relic.AssetKey + ".";
            foreach (RelicUnstableRebalanceModifier rule in rules)
            {
                if (rule.SourceTeam < 0 || rule.SourceTeam > byte.MaxValue || string.IsNullOrEmpty(rule.StatusId) ||
                    rule.StatusStacks != 1 || rule.StatusId != "unstable")
                    return "Malformed unstable-rebalance definition on " + relic.AssetKey + ".";
                if (rule.AdditionalProcDamage != 0 || rule.AdditionalGrowthRate != 0)
                    return "Unmodeled unstable proc-damage or growth hook on " + relic.AssetKey + ".";
            }
            return null;
        }

        public static int GetAdditionalExplosionDamagePerStack(CombatContext? context, CombatUnit unstableUnit,
            IReadOnlyList<CombatUnit> roomUnits)
        {
            if (context?.Relics == null) return 0;
            int total = 0;
            int nativeTeam = unstableUnit.Team == CombatTeam.Player ? 2 : 1;
            foreach (CombatRelicState relic in context.Relics)
            {
                string? error = Validate(relic);
                if (error != null) throw new InvalidOperationException(error);
                foreach (RelicUnstableRebalanceModifier rule in relic.UnstableRebalances ??
                    Array.Empty<RelicUnstableRebalanceModifier>())
                {
                    if (rule.SourceTeam != 0 && (rule.SourceTeam & nativeTeam) == 0) continue;
                    if (rule.RequireOnlyOneUnitWithUnstable && roomUnits.Any(candidate =>
                        candidate.Id != unstableUnit.Id && candidate.Team == unstableUnit.Team &&
                        (candidate.Status("unstable")?.Stacks ?? 0) > 0 && candidate.DeathState?.IsDestroyed != true &&
                        (candidate.DeathState?.HasFinishedDying != true && candidate.Health > 0 ||
                            (candidate.Status("undying")?.Stacks ?? 0) > 0)))
                        continue;
                    total = unchecked(total + rule.AdditionalExplosionDamage);
                }
            }
            return total;
        }
    }
}

using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicLateAttackRule
    {
        public float Multiplier { get; }
        public bool IgnoreChampion { get; }
        public bool SkipPyre { get; }
        public bool HasConditions { get; }
        public RelicLateAttackRule(float multiplier, bool ignoreChampion, bool skipPyre, bool hasConditions = false)
        { Multiplier = multiplier; IgnoreChampion = ignoreChampion; SkipPyre = skipPyre; HasConditions = hasConditions; }
    }

    internal static class RelicLateAttackModel
    {
        internal const string Effect = "RelicEffectMultiplyAttackLate";
        internal static bool Contains(string source, string target) => target == "None" || source == "Both" || source == target;
        internal static float Query(string source, RelicLateAttackRule rule, string target, bool champion) =>
            Contains(source, target) && (!rule.IgnoreChampion || !champion) ? rule.Multiplier : 1f;
        internal static float Multiplier(CombatUnit unit, bool applyPyreSkip = true)
        {
            var rules = unit.Modifiers?.StatusAttackModifiers;
            if (rules == null || applyPyreSkip && unit.IsPyre && rules.Any(rule => rule.LateAttack?.SkipPyre == true)) return 1f;
            float result = 1f;
            string team = unit.Team == CombatTeam.Player ? "Monsters" : "Heroes";
            foreach (var rule in rules)
                if (rule.LateAttack != null) result *= Query(rule.Team, rule.LateAttack, team, unit.Modifiers!.IsChampion == true);
            return result;
        }
        internal static int Apply(CombatUnit unit, int amount)
        {
            var rules = unit.Modifiers?.StatusAttackModifiers;
            if (rules?.Any(rule => rule.LateAttack != null) != true || unit.IsPyre && rules.Any(rule => rule.LateAttack?.SkipPyre == true))
                return Math.Max(0, amount);
            return Math.Max(0, Scale(amount, Multiplier(unit)));
        }
        internal static int Scale(int amount, float multiplier) => unchecked((int)Math.Ceiling((float)amount * multiplier));
        internal static string? Validate(CombatUnit unit)
        {
            var rules = unit.Modifiers?.StatusAttackModifiers;
            if (rules?.Any(rule => rule.LateAttack != null) != true) return null;
            if (!unit.Modifiers!.IsChampion.HasValue) return "Late attack requires captured champion metadata.";
            return rules.Any(rule => rule.LateAttack != null && (rule.StatusId != "" || rule.Multiplier != 0 ||
                rule.Team is not ("None" or "Monsters" or "Heroes" or "Both") || rule.LateAttack.HasConditions ||
                float.IsNaN(rule.LateAttack.Multiplier) || float.IsInfinity(rule.LateAttack.Multiplier)))
                ? "Unmodeled unit late attack filter." : null;
        }
    }
}

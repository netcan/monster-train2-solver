using System;
using System.Collections.Generic;

namespace MonsterTrain2Poju.Model
{
    public sealed class ScalingDamageTrait
    {
        public CardStatisticQuery Query { get; }
        public int DamagePerStat { get; }
        public float Multiplier { get; }
        public bool AddToDamage { get; }
        public ScalingDamageTrait(CardStatisticQuery query, int damagePerStat, float multiplier, bool addToDamage)
        { Query = query; DamagePerStat = damagePerStat; Multiplier = multiplier; AddToDamage = addToDamage; }
    }

    public sealed class DamageScalingResult
    {
        public CombatContext? Context { get; }
        public int Damage { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => UnsupportedReason == null;
        internal DamageScalingResult(CombatContext? context, int damage, string? error = null)
        { Context = context; Damage = damage; UnsupportedReason = error; }
    }

    public static class DamageScalingModel
    {
        // The trait owner is the responsible spawner/sacrifice card. Only an explicit damage
        // source contributes numeric damage upgrades (ordinary unit attacks pass no card).
        public static DamageScalingResult Apply(CombatContext? context, int responsibleCardId, int damageSourceCardId,
            int damage, StatisticQueryFrame? frame = null)
        {
            if (responsibleCardId <= 0) return new DamageScalingResult(context, damage);
            CardInstanceState? responsible = context?.FindCard(responsibleCardId);
            if (responsible == null)
            {
                // Legacy captures have neither instance metadata nor a registry; their native
                // trait interactions were rejected by the capture boundary.
                if (context?.CardInstances == null && context?.CardRegistry == null) return new DamageScalingResult(context, damage);
                return Unsupported("Missing responsible damage card.");
            }
            foreach (ScalingDamageTrait trait in responsible.DamageScalingTraits ?? Array.Empty<ScalingDamageTrait>())
            {
                DamageScalingResult applied = ApplyTrait(context, trait, responsibleCardId, damageSourceCardId, damage, frame);
                if (!applied.Supported) return applied;
                context = applied.Context; damage = applied.Damage;
            }
            return new DamageScalingResult(context, damage);
        }

        public static DamageScalingResult ApplyTrait(CombatContext? context, ScalingDamageTrait trait, int ownerCardId,
            int damageSourceCardId, int damage, StatisticQueryFrame? frame = null)
        {
            if (context == null) return Unsupported("Scaling damage requires shared combat context.");
            int scaled = 0;
            // This native special case returns zero without querying or refreshing membership.
            if (trait.Query.Type != "MagicPowerInTargetRoom")
            {
                if (trait.Query.Type == "AnyStatusEffectStacksAdded" || trait.Query.Type == "AnyStatusEffectStacksRemoved")
                    return Unsupported("Scaling from status counters requires the native status-counter update paths.");
                StatisticQueryResult statistic = StatisticQueryModel.Evaluate(context, trait.Query, ownerCardId, frame);
                if (!statistic.Supported) return Unsupported(statistic.UnsupportedReason!);
                context = statistic.Context!;
                float product = (float)unchecked(trait.DamagePerStat * statistic.Value) * trait.Multiplier;
                double floor = Math.Floor(product);
                if (double.IsNaN(floor) || floor < int.MinValue || floor > int.MaxValue)
                    return Unsupported("Scaling damage exceeds the modeled float-to-integer domain.");
                scaled = (int)floor;
            }
            int extra = 0;
            if (damageSourceCardId > 0)
            {
                CardInstanceState? source = context.FindCard(damageSourceCardId);
                if (source == null) return Unsupported("Missing explicit damage-source card modifiers.");
                try
                {
                    extra = CardModifierModel.UpgradedStat(CardModifierModel.UpgradedStat(0, "Damage", true, source.Permanent),
                        "Damage", true, source.Temporary);
                }
                catch (OverflowException) { return Unsupported("Scaling damage modifiers exceed the modeled numeric domain."); }
            }
            return new DamageScalingResult(context, unchecked(scaled + (trait.AddToDamage ? damage : 0) + extra));
        }
        private static DamageScalingResult Unsupported(string reason) => new DamageScalingResult(null, 0, reason);
    }
}

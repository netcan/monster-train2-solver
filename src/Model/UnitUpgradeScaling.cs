using System;

namespace MonsterTrain2Poju.Model
{
    public sealed class ScalingUnitUpgradeTrait
    {
        public CardStatisticQuery Query { get; }
        public string Stat { get; }
        public int AmountPerStat { get; }
        public int Restriction { get; }
        public ScalingUnitUpgradeTrait(CardStatisticQuery query, string stat, int amountPerStat, int restriction = 0)
        { Query = query; Stat = stat; AmountPerStat = amountPerStat; Restriction = restriction; }
    }

    public sealed class UnitUpgradeScalingResult
    {
        public CombatContext? Context { get; }
        public CardUpgradeModifier? Upgrade { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => Upgrade != null;
        internal UnitUpgradeScalingResult(CombatContext? context, CardUpgradeModifier? upgrade, string? error = null)
        { Context = context; Upgrade = upgrade; UnsupportedReason = error; }
    }

    public static class UnitUpgradeScalingModel
    {
        public static UnitUpgradeScalingResult Apply(CombatContext? context, int sourceCardId, CardUpgradeModifier upgrade, string? triggerKind = null)
        {
            if (sourceCardId <= 0) return new UnitUpgradeScalingResult(context, upgrade);
            CardInstanceState? owner = context?.FindCard(sourceCardId);
            if (owner == null)
            {
                if (context?.CardInstances == null && context?.CardRegistry == null) return new UnitUpgradeScalingResult(context, upgrade);
                return Unsupported("Missing unit-upgrade source card.");
            }
            foreach (ScalingUnitUpgradeTrait trait in owner.UnitUpgradeScalingTraits ?? Array.Empty<ScalingUnitUpgradeTrait>())
            {
                UnitUpgradeScalingResult scaled = ApplyTrait(context, trait, sourceCardId, upgrade, triggerKind);
                if (!scaled.Supported) return scaled;
                context = scaled.Context; upgrade = scaled.Upgrade!;
            }
            return new UnitUpgradeScalingResult(context, upgrade);
        }
        public static UnitUpgradeScalingResult ApplyTrait(CombatContext? context, ScalingUnitUpgradeTrait trait, int ownerCardId,
            CardUpgradeModifier upgrade, string? triggerKind = null)
        {
            // Native restriction 1 allows an ordinary cast (no trigger), as well as OnSpawn.
            if (trait.Restriction == 1 && triggerKind != null && triggerKind != "OnSpawn" || upgrade.MagicPowerTraitScalingOnly)
                return new UnitUpgradeScalingResult(context, upgrade);
            if (context == null) return Unsupported("Unit-upgrade scaling requires shared combat context.");
            if (trait.Stat != "Damage" && trait.Stat != "Health") return Unsupported("Unknown unit-upgrade scaling stat.");
            if (trait.Query.Type == "AnyStatusEffectStacksRemoved") return Unsupported("Removed-status scaling attribution is not modeled.");
            StatisticQueryResult query = StatisticQueryModel.Evaluate(context, trait.Query, ownerCardId);
            if (!query.Supported) return Unsupported(query.UnsupportedReason!);
            int bonus = unchecked(trait.AmountPerStat * query.Value);
            return new UnitUpgradeScalingResult(query.Context, upgrade.WithScaledStats(
                trait.Stat == "Damage" ? unchecked(upgrade.Stats.Damage + bonus) : upgrade.Stats.Damage,
                trait.Stat == "Health" ? unchecked(upgrade.Stats.Health + bonus) : upgrade.Stats.Health));
        }
        private static UnitUpgradeScalingResult Unsupported(string reason) => new UnitUpgradeScalingResult(null, null, reason);
    }
}

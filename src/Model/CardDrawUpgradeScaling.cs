using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class CardDrawUpgradeScalingModel
    {
        public const string Trait = "CardTraitScalingTempCardUpgradeToCard";
        public static UnitUpgradeScalingResult Apply(CombatContext context, int sourceCardId, CardUpgradeModifier upgrade)
        {
            UnitUpgradeScalingResult Reject(string reason) => new UnitUpgradeScalingResult(null, null, reason);
            CardInstanceState? card = context.FindCard(sourceCardId);
            CardMaskDescriptor? descriptor = card?.MaskDescriptor;
            if (card == null || descriptor == null || !descriptor.BaseTraits.Concat(descriptor.TemporaryTraits).Any(trait => trait.RuntimeType == Trait) &&
                !descriptor.PermanentReplacements.Any(rule => rule.NewRuntimeType == Trait) &&
                !card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(item => item.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true))
                return new UnitUpgradeScalingResult(context, upgrade);
            CardTraitValue[] active = (CardExhaustModel.RefreshTraits(card).CombinedTraits ?? Array.Empty<CardTraitValue>()).Where(trait => trait.RuntimeType == Trait).ToArray();
            if (active.Length == 0) return new UnitUpgradeScalingResult(context, upgrade);
            int index = descriptor.BaseTraits.ToList().FindIndex(trait => trait.RuntimeType == Trait);
            if (active.Length != 1 || index < 0 || descriptor.SourceTraits == null || descriptor.SourceTraits.Count != descriptor.BaseTraits.Count ||
                descriptor.TemporaryTraits.Any(trait => trait.RuntimeType == Trait) ||
                descriptor.PermanentReplacements.Any(rule => rule.OldName == Trait || rule.NewRuntimeType == Trait) ||
                card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(item => item.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true))
                return Reject("Next-draw scaling requires one unchanged base trait and captured source parameters.");
            CardSourceTrait source = descriptor.SourceTraits[index];
            if (active[0].DeclaredName != Trait || active[0].ParamInt != 1 || active[0].DefinitionParamInt != 1 || source.Type != Trait || source.Int != 1 ||
                source.Int2 != 0 || source.Int3 != 0 || source.Bool || source.Str != "" || source.Float != 0 || !source.Scaling ||
                source.TrackedValue != "PlayedCost" || source.EntryDuration != "ThisTurn" || descriptor.BaseCost != 0 ||
                !descriptor.Definition.XCost || descriptor.Definition.CardType != "Spell")
                return Reject("Only captured Prism Retrieval played-cost next-draw scaling is calibrated.");
            CardStatModifier stats = upgrade.Stats;
            if (stats.Damage != 5 || stats.Cost != -1 || stats.Health != 0 || stats.Heal != 0 || stats.Size != 0 || stats.XCost != 0 ||
                stats.EquipmentLimit != 0 || stats.UpgradeSlotCount != 0 || upgrade.Statuses.Count != 0 || upgrade.ExternalInteractions.Count != 0)
                return Reject("Only Prism Retrieval's original damage/cost upgrade payload is calibrated.");
            StatisticQueryResult query = StatisticQueryModel.Evaluate(context, new CardStatisticQuery("PlayedCost", "ThisTurn",
                sourceRawCost: 0, variableCost: true, currentCostSourceIsSpell: true), sourceCardId);
            return query.Supported ? new UnitUpgradeScalingResult(query.Context, upgrade.WithStats(new CardStatModifier(
                damage: unchecked(stats.Damage * query.Value), cost: unchecked(stats.Cost * query.Value)))) : Reject(query.UnsupportedReason!);
        }
        internal static bool Present(CardInstanceState? card) => card?.MaskDescriptor != null &&
            (card.MaskDescriptor.BaseTraits.Concat(card.MaskDescriptor.TemporaryTraits).Any(trait => trait.RuntimeType == Trait) ||
             card.MaskDescriptor.PermanentReplacements.Any(rule => rule.NewRuntimeType == Trait) ||
             card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true));
    }
}

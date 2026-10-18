using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class CardBuffDamageModel
    {
        public const string Trait = "CardTraitScalingBuffDamage";
        public static DamageScalingResult Apply(CombatContext? context, int sourceCardId, int incoming, bool targetIsPyre)
        {
            DamageScalingResult Reject(string reason) => new DamageScalingResult(null, 0, reason);
            CardInstanceState? card = context?.FindCard(sourceCardId);
            CardMaskDescriptor? descriptor = card?.MaskDescriptor;
            if (card == null || descriptor == null || !descriptor.BaseTraits.Concat(descriptor.TemporaryTraits).Any(trait => trait.RuntimeType == Trait) &&
                !descriptor.PermanentReplacements.Any(rule => rule.NewRuntimeType == Trait) &&
                !card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true))
                return new DamageScalingResult(context, incoming);
            CardTraitValue[] active = (CardExhaustModel.RefreshTraits(card).CombinedTraits ?? Array.Empty<CardTraitValue>()).Where(trait => trait.RuntimeType == Trait).ToArray();
            if (active.Length == 0) return new DamageScalingResult(context, incoming);
            int index = descriptor.BaseTraits.ToList().FindIndex(trait => trait.RuntimeType == Trait);
            if (active.Length != 1 || index < 0 || descriptor.SourceTraits == null || descriptor.SourceTraits.Count != descriptor.BaseTraits.Count ||
                descriptor.TemporaryTraits.Any(trait => trait.RuntimeType == Trait) ||
                descriptor.PermanentReplacements.Any(rule => rule.OldName == Trait || rule.NewRuntimeType == Trait) ||
                card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true))
                return Reject("Scaled attack buff requires one unchanged base trait and captured source parameters.");
            CardSourceTrait source = descriptor.SourceTraits[index];
            if (active[0].DeclaredName != Trait || active[0].ParamInt != 4 || active[0].DefinitionParamInt != 4 || source.Type != Trait || source.Int != 4 ||
                source.Int2 != 0 || source.Int3 != 0 || source.Bool || source.Str != "" || source.Float != 0 || source.Scaling ||
                source.TrackedValue != "PlayedCost" || source.EntryDuration != "ThisTurn" || !targetIsPyre ||
                descriptor.BaseCost != 0 || !descriptor.Definition.XCost || descriptor.Definition.CardType != "Spell")
                return Reject("Only captured Pyre Plating played-cost attack buff parameters and Pyre target are calibrated.");
            StatisticQueryResult query = StatisticQueryModel.Evaluate(context!, new CardStatisticQuery("PlayedCost", "ThisTurn",
                sourceRawCost: descriptor.BaseCost, variableCost: true, currentCostSourceIsSpell: true), sourceCardId);
            // This callback replaces its incoming amount; it does not add to it.
            return query.Supported ? new DamageScalingResult(query.Context, unchecked(4 * query.Value)) : Reject(query.UnsupportedReason!);
        }
    }
}

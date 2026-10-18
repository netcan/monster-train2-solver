using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class CardMoonAdvanceScalingModel
    {
        public const string Trait = "CardTraitScalingAdvanceMoonPhase";
        public static StatisticQueryResult Adjust(CombatContext? context, int sourceCardId, int amount)
        {
            StatisticQueryResult Reject(string reason) => new StatisticQueryResult(null, 0, reason);
            CardInstanceState? card = context?.FindCard(sourceCardId);
            CardMaskDescriptor? descriptor = card?.MaskDescriptor;
            if (card == null || descriptor == null || !descriptor.BaseTraits.Concat(descriptor.TemporaryTraits).Any(trait => trait.RuntimeType == Trait) &&
                !descriptor.PermanentReplacements.Any(rule => rule.NewRuntimeType == Trait) &&
                !card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true))
                return new StatisticQueryResult(context, amount);
            CardTraitValue[] active = (CardExhaustModel.RefreshTraits(card).CombinedTraits ?? Array.Empty<CardTraitValue>()).Where(trait => trait.RuntimeType == Trait).ToArray();
            if (active.Length == 0) return new StatisticQueryResult(context, amount);
            int index = descriptor.BaseTraits.ToList().FindIndex(trait => trait.RuntimeType == Trait);
            if (active.Length != 1 || index < 0 || descriptor.SourceTraits == null || descriptor.SourceTraits.Count != descriptor.BaseTraits.Count ||
                descriptor.TemporaryTraits.Any(trait => trait.RuntimeType == Trait) ||
                descriptor.PermanentReplacements.Any(rule => rule.OldName == Trait || rule.NewRuntimeType == Trait) ||
                card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true))
                return Reject("Moon advance scaling requires one unchanged base trait and captured source parameters.");
            CardSourceTrait source = descriptor.SourceTraits[index];
            if (active[0].DeclaredName != Trait || active[0].ParamInt != 0 || active[0].DefinitionParamInt != 0 || source.Type != Trait || source.Int != 0 ||
                source.Int2 != 0 || source.Int3 != 0 || source.Bool || source.Str != "" || source.Float != 1 || source.Scaling ||
                source.TrackedValue != "PlayedCost" || source.EntryDuration != "ThisTurn" || descriptor.BaseCost != 0 ||
                !descriptor.Definition.XCost || descriptor.Definition.CardType != "Spell")
                return Reject("Only captured Lunacy played-cost moon-advance parameters are calibrated.");
            StatisticQueryResult query = StatisticQueryModel.Evaluate(context!, new CardStatisticQuery("PlayedCost", "ThisTurn",
                sourceRawCost: 0, variableCost: true, currentCostSourceIsSpell: true), sourceCardId);
            return query.Supported ? new StatisticQueryResult(query.Context, unchecked(amount + query.Value)) : Reject(query.UnsupportedReason!);
        }
    }
}

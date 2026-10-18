using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class CardPileDrawModel
    {
        public const string Trait = "CardTraitScalingCardDrawCountPerPile";
        public static StatisticQueryResult Adjust(CombatContext context, int sourceCardId, int amount, bool forRecursion = false)
        {
            StatisticQueryResult Reject(string reason) => new StatisticQueryResult(null, 0, reason);
            CardInstanceState? card = context.FindCard(sourceCardId);
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
                return Reject("Pile-scaled draw requires one unchanged base trait and captured source parameters.");
            CardSourceTrait source = descriptor.SourceTraits[index];
            if (active[0].DeclaredName != Trait || active[0].ParamInt != 1 || active[0].DefinitionParamInt != 1 || source.Type != Trait || source.Int != 1 ||
                source.Str != "" || source.Float != 0 || source.Scaling ||
                source.TrackedValue != "SubtypeInDeck" || source.EntryDuration != "ThisTurn" ||
                descriptor.Definition.XCost || descriptor.Definition.CardType != "Spell")
                return Reject("Pile-scaled draw requires calibrated original query parameters.");
            if (forRecursion)
            {
                if (source.Int2 != 2 || source.Int3 != 5 || source.Bool || descriptor.BaseCost != 1)
                    return Reject("Only captured Unearth Treasure exhaust-pile threshold recursion is calibrated.");
                CardPileState[] exhausted = context.OtherPiles?.Where(pile => pile.Name == "Exhausted").ToArray() ?? Array.Empty<CardPileState>();
                if (exhausted.Length != 1) return Reject("Exhaust-scaled recursion requires one captured exhausted pile.");
                return new StatisticQueryResult(context, unchecked(amount + (exhausted[0].Cards.Count >= source.Int3 ? source.Int : 0)));
            }
            if (source.Int2 != 0 || source.Int3 != 15 || !source.Bool || descriptor.BaseCost != 0)
                return Reject("Only captured Mulch visible-deck future draw per fifteen cards is calibrated.");
            if (context.Resources == null || context.Resources.VisibleDeckCount < 0)
                return Reject("Pile-scaled draw requires the native visible permanent-deck count.");
            // Native's repeated-threshold branch uses integer division directly, not ParamInt * count.
            return new StatisticQueryResult(context, unchecked(amount + context.Resources.VisibleDeckCount / source.Int3));
        }
    }
}

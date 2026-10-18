using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class CardDiscardDrawModel
    {
        public const string Trait = "CardTraitScalingAddCards";
        public static StatisticQueryResult Count(CombatContext context, CardInstanceState card, bool wasPlayed)
        {
            if (!wasPlayed || !Present(card)) return new StatisticQueryResult(context, 0);
            StatisticQueryResult Reject(string reason) => new StatisticQueryResult(null, 0, reason);
            CardMaskDescriptor descriptor = card.MaskDescriptor!;
            if (descriptor.AuthoredCastEffects.Count > 0 || context.Relics?.Any(relic => relic.EffectTypes.Count > 0) == true)
                return Reject("Scaled discard draw with OnCast effects or relic owners requires native calibration.");
            CardTraitValue[] active = (CardExhaustModel.RefreshTraits(card).CombinedTraits ?? Array.Empty<CardTraitValue>()).Where(trait => trait.RuntimeType == Trait).ToArray();
            int index = descriptor.BaseTraits.ToList().FindIndex(trait => trait.RuntimeType == Trait);
            if (active.Length != 1 || index < 0 || descriptor.SourceTraits == null || descriptor.SourceTraits.Count != descriptor.BaseTraits.Count ||
                descriptor.TemporaryTraits.Any(trait => trait.RuntimeType == Trait) || descriptor.PermanentReplacements.Any(rule => rule.OldName == Trait || rule.NewRuntimeType == Trait) ||
                card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true))
                return Reject("Scaled discard draw requires one unchanged base trait and captured source parameters.");
            CardSourceTrait source = descriptor.SourceTraits[index];
            if (active[0].DeclaredName != Trait || active[0].ParamInt != 1 || active[0].DefinitionParamInt != 1 || source.Type != Trait ||
                source.Int != 1 || source.Int2 != 0 || source.Int3 != 0 || !source.Bool || source.Str != "" || source.Float != 0 || !source.Scaling ||
                source.TrackedValue != "PlayedCost" || source.EntryDuration != "ThisBattle" || descriptor.BaseCost != 0 || !descriptor.Definition.XCost || descriptor.Definition.CardType != "Spell")
                return Reject("Only captured Awoken's Rail Spike played-cost draw and blocking parameters are calibrated.");
            if (!context.Cards.DeckDrawDisabledThisTurn.HasValue) return Reject("Scaled discard draw requires captured deck-draw blocking state.");
            if (context.Statistics?.PlayedCosts.Any(cost => cost.CardId == card.InstanceId) != true) return Reject("Scaled discard draw requires the live paid-cost statistic.");
            return StatisticQueryModel.Evaluate(context, new CardStatisticQuery("PlayedCost", "ThisBattle", sourceRawCost: 0, variableCost: true,
                currentCostSourceIsSpell: true), card.InstanceId);
        }
        public static TypedDrawResult Discarded(CombatContext context, CardInstanceState card, bool wasPlayed, BattlePlayRules? definitions = null)
        {
            if (!wasPlayed || !Present(card)) return new TypedDrawResult(context, Array.Empty<int>());
            StatisticQueryResult count = Count(context, card, wasPlayed);
            if (!count.Supported) return new TypedDrawResult(null, Array.Empty<int>(), count.UnsupportedReason);
            TypedDrawResult drawn = NextDrawUpgradeModel.DrawCards(count.Context!, count.Value, card.InstanceId, definitions);
            return drawn.Supported ? new TypedDrawResult(drawn.Context!.WithCards(drawn.Context.Cards.WithDrawDisabled(true)), drawn.DrawnCardIds) : drawn;
        }
        internal static bool Present(CardInstanceState? card) => card?.MaskDescriptor != null &&
            (card.MaskDescriptor.BaseTraits.Concat(card.MaskDescriptor.TemporaryTraits).Any(trait => trait.RuntimeType == Trait) ||
             card.MaskDescriptor.PermanentReplacements.Any(rule => rule.NewRuntimeType == Trait) ||
             card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true));
    }
}

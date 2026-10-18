using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class ForcedFeedingModel
    {
        public static StatisticQueryResult TriggerCount(CombatContext context, CardInstanceState? card)
        {
            CardTraitValue? scaling = card == null ? null : CardExhaustModel.RefreshTraits(card).CombinedTraits?
                .FirstOrDefault(trait => trait.RuntimeType == "CardTraitScalingFeederRules");
            if (scaling == null) return new StatisticQueryResult(context, 1);
            CardStatisticQuery? captured = scaling.FeedingQuery;
            CardMaskDescriptor? descriptor = card!.MaskDescriptor;
            if (captured?.Type != "PlayedCost" || descriptor == null)
                return new StatisticQueryResult(null, 0, "Forced feeding scaling requires a calibrated native paid-cost query and card descriptor.");
            var query = new CardStatisticQuery(captured.Type, captured.Duration, captured.TypeMatchDataIds,
                captured.SubtypeMatchDataIds, captured.SubtypeIsNone, captured.Subtype, captured.SpecificCardDataId,
                card.RawCostOverride ?? descriptor.BaseCost, descriptor.Definition.XCost, captured.TargetTeam,
                currentCostSourceIsSpell: descriptor.Definition.CardType == "Spell");
            StatisticQueryResult value = StatisticQueryModel.Evaluate(context, query, card.InstanceId);
            if (!value.Supported) return value;
            int repeats = unchecked(scaling.ParamInt * value.Value);
            return repeats < 0 || repeats > 4096
                ? new StatisticQueryResult(null, 0, "Forced feeding exceeds the supported trigger-count limit.")
                : new StatisticQueryResult(value.Context, repeats);
        }
    }
}

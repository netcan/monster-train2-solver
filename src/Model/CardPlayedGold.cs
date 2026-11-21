using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class CardPlayedGoldModel
    {
        internal const string Trait = "CardTraitScalingRewardGold";

        private static IEnumerable<CardTraitValue> Active(CardInstanceState? card) => card?.MaskDescriptor == null
            ? Array.Empty<CardTraitValue>()
            : CardExhaustModel.RefreshTraits(card).CombinedTraits!.Where(trait => trait.RuntimeType == Trait);

        internal static bool Present(CardInstanceState card) => Active(card).Any();

        private static StatisticQueryResult Evaluate(CombatContext context, CardInstanceState card, CardTraitValue trait)
        {
            if (trait.PlayedGoldQuery == null) return new(null, 0, "Missing native played-discard gold statistic.");
            var result = StatisticQueryModel.Evaluate(context, trait.PlayedGoldQuery, card.InstanceId);
            return result.Supported ? new(result.Context, unchecked(result.Value * trait.ParamInt)) : result;
        }

        internal static StatisticQueryResult Amount(CombatContext context, CardInstanceState? card)
        {
            int total = 0;
            foreach (var trait in Active(card))
            {
                var result = Evaluate(context, card!, trait);
                if (!result.Supported) return result;
                context = result.Context!;
                total = unchecked(total + result.Value);
            }
            return new(context, total);
        }

        internal static TrainCombatResult Discarded(TrainCombatState source, CardInstanceState card)
        {
            var state = source;
            foreach (var trait in Active(card))
            {
                var result = Evaluate(state.Context!, card, trait);
                if (!result.Supported) return GoldModel.Fail(result.UnsupportedReason!);
                var adjusted = GoldModel.Adjust(CardSpellModel.WithContext(state, result.Context!), result.Value,
                    isReward: result.Value > 0, preview: state.Rooms.Any(room => room.Preview));
                if (!adjusted.Supported) return adjusted;
                state = adjusted.State!;
            }
            return GoldModel.Success(state);
        }
    }
}

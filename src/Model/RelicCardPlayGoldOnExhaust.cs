using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicCardPlayGoldOnExhaustModel
    {
        internal const string Effect = "RelicEffectAddGoldOnExhaust";

        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type == Effect).Select(item => item.index).ToArray();
            if (indices.Length == 0) return null;

            RelicResourceEffect[] rules = (relic.ResourceEffects ?? Array.Empty<RelicResourceEffect>())
                .Where(rule => rule.Type == Effect).ToArray();
            if (!indices.SequenceEqual(rules.Select(rule => rule.EffectIndex)))
                return "Missing ordered native card-play gold-on-exhaust definitions.";
            if (rules.Any(rule => rule.Amount != rule.ParamInt || rule.Conditions.Count != 0 ||
                rule.RequiredMutatorId != null || rule.CanApplyInPreviewMode))
                return "Unsupported card-play gold-on-exhaust parameters or conditions.";
            return null;
        }

        internal static TrainCombatResult ApplyOne(TrainCombatState source, int playedCardId,
            int relicIndex, int effectIndex, bool placement = false, bool preview = false)
        {
            if (source.Context == null) return GoldModel.Fail("Gold-on-exhaust requires shared battle context.");
            CombatContext context = source.Context;
            string? error = RelicModel.Validate(context);
            if (error != null) return GoldModel.Fail(error);
            if (relicIndex < 0 || relicIndex >= context.Relics!.Count)
                return GoldModel.Fail("Missing gold-on-exhaust relic owner.");

            CombatRelicState owner = context.Relics[relicIndex];
            if (effectIndex < 0 || effectIndex >= owner.EffectTypes.Count || owner.EffectTypes[effectIndex] != Effect)
                return GoldModel.Fail("Gold-on-exhaust effect order changed.");
            RelicResourceEffect? rule = owner.ResourceEffects?.SingleOrDefault(item => item.EffectIndex == effectIndex);
            if (rule == null) return GoldModel.Fail("Missing ordered native gold-on-exhaust definition.");
            if (placement && owner.DisallowedInPlacementPhase == true || preview && !rule.CanApplyInPreviewMode)
                return GoldModel.Success(source);

            CardInstanceState? card = context.FindCard(playedCardId);
            if (card == null) return GoldModel.Fail("Gold-on-exhaust requires the played owned-card state.");
            if (!CardExhaustModel.HasConsume(card)) return GoldModel.Success(source);
            error = CardExhaustModel.Validate(card);
            if (error != null) return GoldModel.Fail(error);
            if (!CardExhaustModel.ShouldExhaustOnPlayedDiscard(card)) return GoldModel.Success(source);

            return GoldModel.Adjust(source, rule.Amount, isReward: true, preview: preview, roundIncrement: 1);
        }
    }
}

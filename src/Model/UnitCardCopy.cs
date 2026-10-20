using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class UnitCardCopyModel
    {
        internal const string Effect = "CardEffectAddUnitCard";
        internal static string? Validate(CombatEffect effect)
        {
            CardGenerationRule? rule = effect.Generation;
            if (rule == null || rule.Destination != "DiscardPile" || rule.Count != 1 || rule.Permanent != true ||
                !rule.DeterministicPoolSelection || rule.Pool.Count != 0 || !effect.SkipDuplicateInHand ||
                rule.ExternalInteractions.Count > 0 || rule.Upgrade?.ExternalInteractions.Count > 0)
                return "Uncalibrated unit-source card generation parameters.";
            return null;
        }
        internal static CardGenerationResult Apply(CombatContext context, int sourceCardId, CombatEffect effect)
        {
            string? error = Validate(effect);
            if (error != null) return new CardGenerationResult(null, Array.Empty<CardToken>(), error);
            CardInstanceState? source = context.FindCard(sourceCardId);
            if (source == null) return new CardGenerationResult(context, Array.Empty<CardToken>());
            CardCreationRule? creation = context.SummonCatalog?.Cards.SingleOrDefault(card => card.Creation.DataId == source.DataId)?.Creation;
            if (creation == null) return new CardGenerationResult(null, Array.Empty<CardToken>(), "Unit-source card generation requires captured source creation metadata.");
            CardGenerationRule rule = effect.Generation!;
            return CardGenerationModel.Apply(context, new CardGenerationRule(rule.Destination, rule.Count, new[] { creation },
                copyModifiers: rule.CopyModifiers, ignoreTemporaryModifiers: rule.IgnoreTemporaryModifiers, upgrade: rule.Upgrade,
                discardCopyUpgrades: rule.DiscardCopyUpgrades, deterministicPoolSelection: true, permanent: rule.Permanent), sourceCardId);
        }
    }
}

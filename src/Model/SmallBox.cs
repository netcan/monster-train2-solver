using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    // Boxed cards are saved templates, not battle-owned card identities.
    public sealed class BoxedCardState
    {
        public CardCreationRule Creation { get; }
        public CardModifiers Permanent { get; }
        public CardModifiers Temporary { get; }
        public BoxedCardState(CardCreationRule creation, CardModifiers permanent, CardModifiers temporary)
        { Creation = creation; Permanent = permanent; Temporary = temporary; }
    }

    public static class SmallBoxModel
    {
        public static CardGenerationResult Apply(CombatContext source, int sourceCardId)
        {
            IReadOnlyList<BoxedCardState>? contents = source.FindCard(sourceCardId)?.Permanent.BoxedCards;
            if (contents == null) return Unsupported("Unboxing requires captured permanent boxed-card templates.");
            int count = Math.Min(contents.Count, Math.Max(0, source.MaxHandSize - source.Cards.Hand.Count));
            // Native shuffles the whole list, including when the hand has no space.
            ShuffleResult<BoxedCardState> shuffled = source.BattleRng.Shuffle(contents);
            CombatContext context = source.WithBattleRng(shuffled.State);
            if (count == 0) return new CardGenerationResult(context, Array.Empty<CardToken>());
            if (source.Relics?.Any(relic => relic.CardModifiers?.Count > 0 || relic.CardHealthModifiers?.Count > 0 ||
                relic.CardAttackModifiers?.Count > 0 || relic.MagicPowerModifiers?.Count > 0 || relic.CardTypeCosts?.Count > 0) == true)
                return Unsupported("Unboxing with relic modifiers after copied upgrades requires native calibration.");
            var added = new List<CardToken>();
            foreach (BoxedCardState boxed in shuffled.Items.Take(count))
            {
                CardCreationRule definition = boxed.Creation;
                if (boxed.Permanent.BoxedCards?.Count > 0 || boxed.Temporary.BoxedCards?.Count > 0)
                    return Unsupported("Nested boxed-card copies require native calibration.");
                // CardState(data, setupStartingUpgrades:false), followed by both copies.
                var creation = new CardCreationRule(definition.DataId, CardModifiers.Empty(), definition.EffectCounters,
                    definition.ExternalInteractions, definition.DamageScalingTraits, definition.StatusScalingTraits,
                    definition.UnitUpgradeScalingTraits, definition.CapacityScalingTraits, definition.EquippedUnitId,
                    definition.MaskDescriptor, definition.ForgeCountScalingTraits, definition.CanBeCopied,
                    definition.TrainHealScalingTraits);
                var template = new CardInstanceState(0, definition.DataId, boxed.Permanent, boxed.Temporary, 0, 0, 0,
                    definition.ExternalInteractions, maskDescriptor: definition.MaskDescriptor);
                CardGenerationResult generated = CardGenerationModel.ApplyCachedCopy(context, creation, template);
                if (!generated.Supported) return generated;
                context = generated.Context!;
                // TryAddCachedTemporaryCardToHand dispatches Drawn then Updated
                // to the selected room after each copied card enters the hand.
                foreach (CardToken card in generated.AddedCards)
                {
                    context = ConduitModel.Drawn(context, new[] { card.InstanceId });
                    context = ConduitModel.Updated(context, card.InstanceId);
                }
                added.AddRange(generated.AddedCards);
            }
            return new CardGenerationResult(context, added);
        }
        private static CardGenerationResult Unsupported(string error) => new(null, Array.Empty<CardToken>(), error);
    }
}

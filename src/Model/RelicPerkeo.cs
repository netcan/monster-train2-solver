using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicPerkeoModel
    {
        internal const string DataId = "94f7f915-2a2d-4b56-afe6-76ce875ba54d";
        internal const string UpgradeId = "95278115-d14a-45f4-aec7-6cb437f441c1";
        internal const string SourceCardId = "21038774-c0a4-413c-ab7e-27e458228e5f";
        internal const string DestinationCardId = "d0e1fe8a-fb2a-44e0-9d9b-a3ddd7475c34";
        internal static bool IsConversionTrigger(CardLifecycleTrigger trigger) =>
            trigger.Type == "OnUnplayedPositive" && trigger.Effects.SequenceEqual(new[] { "CardEffectBalatroConvertCard" }) &&
            trigger.ExternalInteractions.Count == 0 && trigger.Actions.Count == 1 && trigger.Actions[0].Type == "BalatroConvert" &&
            trigger.Actions[0].Value == 1 && trigger.Actions[0].Generation is CardGenerationRule rule &&
            rule.Count == 1 && rule.Pool.Count == 1 && rule.Pool[0].DataId == DestinationCardId && rule.Permanent == true && rule.DeterministicPoolSelection &&
            rule.Upgrade == null && !rule.CopyModifiers && rule.ExternalInteractions.Count == 0;
        internal static bool IsConversionUpgrade(CardUpgradeModifier upgrade) =>
            upgrade.DataId == UpgradeId && upgrade.AssetKey == "CalcifiedEmberConvert" &&
            upgrade.ExternalInteractions.Count == 0 && !upgrade.Unique && !upgrade.ExcludeFromClones && !upgrade.RemoveOnDiscard &&
            upgrade.Statuses.Count == 0 && upgrade.UnhealedHealth == 0 && upgrade.DamageBuff == 0 &&
            upgrade.Stats.Damage == 0 && upgrade.Stats.Health == 0 && upgrade.Stats.Cost == 0 && upgrade.Stats.Heal == 0 &&
            upgrade.Stats.Size == 0 && upgrade.Stats.XCost == 0 && upgrade.Stats.EquipmentLimit == 0 && upgrade.Stats.UpgradeSlotCount == 0 &&
            upgrade.Lifecycle?.Triggers.Count == 1 &&
            IsConversionTrigger(upgrade.Lifecycle.Triggers[0]);
        internal static bool HasConversion(CardInstanceState? card) => card != null &&
            card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(IsConversionUpgrade);
        internal static string? Validate(CombatRelicState owner, RelicResourceEffect effect)
        {
            CardGenerationRule? rule = effect.CardGeneration;
            return owner.DataId == DataId && owner.AssetKey == "BalatroEventPerkeo" && owner.IsCovenant == false &&
                owner.DisallowedInPlacementPhase == false && owner.EffectTypes.SequenceEqual(new[] { RelicCardGenerationModel.StartOfBattle }) &&
                effect.EffectIndex == 0 && effect.Conditions.Count == 0 && effect.RequiredMutatorId == null && rule != null &&
                rule.Destination == "DeckPileRandom" && rule.Count == 1 && rule.Permanent == true && rule.UpgradeIsPermanent &&
                rule.Pool.Count == 1 && rule.Pool[0].DataId == SourceCardId && rule.Upgrade != null && IsConversionUpgrade(rule.Upgrade) &&
                !rule.DeterministicPoolSelection && !rule.SelectedPoolIndex.HasValue && !rule.CopyModifiers &&
                !rule.RequireCopyableSource && !rule.SkipDuplicateInHand && !rule.RequireHandSpace && !rule.IgnoreTemporaryModifiers &&
                rule.DiscardCopyUpgrades.Count == 0 && rule.ExternalInteractions.Count == 0
                ? null : "Missing or changed original Perkeo generation/conversion definition.";
        }
        internal static CardGenerationResult Convert(CombatContext source, int cardId, CardCreationRule creation)
        {
            CardToken? card = source.Cards.Hand.SingleOrDefault(item => item.InstanceId == cardId);
            if (card == null || !HasConversion(source.FindCard(cardId)) || source.CardInstances == null ||
                source.CardRegistry == null || source.OtherPiles == null || source.Statistics?.DeckCards == null ||
                source.Resources == null || !source.Statistics.DeckCards.Contains(cardId))
                return Fail("Perkeo conversion requires its captured upgraded permanent hand card and deck state.");
            CardUpgradeModifier upgrade = source.FindCard(cardId)!.Permanent.Upgrades.Concat(source.FindCard(cardId)!.Temporary.Upgrades)
                .Single(IsConversionUpgrade);
            CardCreationRule expected = upgrade.Lifecycle!.Triggers[0].Actions[0].Generation!.Pool.Single();
            if (expected.DataId != creation.DataId) return Fail("Perkeo conversion destination differs from its original upgrade.");
            CardPileState? purged = source.OtherPiles.SingleOrDefault(pile => pile.Name == "Purged");
            if (purged == null) return Fail("Perkeo conversion requires the native Purged pile.");
            CardCycleState cycle = source.Cards;
            CombatContext context = source.WithCards(new CardCycleState(cycle.Hand.Where(item => item.InstanceId != cardId).ToArray(),
                cycle.Draw, cycle.Discard, cycle.Rng, cycle.DrawModifier, cycle.ExternalInteractions, cycle.BonusDraw,
                cycle.NextDrawUpgrades, cycle.NextUpgradeReferenceId))
                .WithOtherPiles(source.OtherPiles.Select(pile => pile.Name == "Purged" ? CardPileModel.Add(pile, card) : pile).ToArray())
                .WithStatistics(source.Statistics.RemoveDeckCard(cardId))
                .WithResources(source.Resources.WithVisibleDeckCount(source.Resources.VisibleDeckCount - 1));
            return CardGenerationModel.Apply(context, new CardGenerationRule("HandPile", 1, new[] { creation },
                deterministicPoolSelection: true, permanent: true));
        }
        internal static CardGenerationResult ApplyUnplayed(CombatContext source)
        {
            CombatContext context = source;
            foreach (CardToken token in source.Cards.Hand.Where(card => HasConversion(source.FindCard(card.InstanceId))))
            {
                CardUpgradeModifier upgrade = context.FindCard(token.InstanceId)!.Permanent.Upgrades.Concat(context.FindCard(token.InstanceId)!.Temporary.Upgrades)
                    .Single(IsConversionUpgrade);
                CardGenerationResult converted = Convert(context, token.InstanceId, upgrade.Lifecycle!.Triggers[0].Actions[0].Generation!.Pool.Single());
                if (!converted.Supported) return converted;
                context = converted.Context!;
            }
            return new CardGenerationResult(context, Array.Empty<CardToken>());
        }
        private static CardGenerationResult Fail(string message) => new(null, Array.Empty<CardToken>(), message);
    }
}

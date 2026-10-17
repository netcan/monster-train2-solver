using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    // Private CardTraitExhaustState fields. The retained card reference survives ResetTraits.
    public sealed class CardExhaustTraitState
    {
        public int TraitIndex { get; }
        public bool IsExhausted { get; }
        public int? ExhaustedCardId { get; }
        public CardExhaustTraitState(int traitIndex, bool isExhausted = false, int? exhaustedCardId = null)
        { TraitIndex = traitIndex; IsExhausted = isExhausted; ExhaustedCardId = exhaustedCardId; }
    }
    public sealed class CardExhaustState
    {
        public int PlayCountModifier { get; }
        public IReadOnlyList<CardExhaustTraitState> BaseTraits { get; }
        public CardExhaustTraitState? UpgradeTrait { get; }
        public CardExhaustState(int playCountModifier, IReadOnlyList<CardExhaustTraitState> baseTraits,
            CardExhaustTraitState? upgradeTrait = null)
        { PlayCountModifier = playCountModifier; BaseTraits = Array.AsReadOnly(baseTraits.ToArray()); UpgradeTrait = upgradeTrait; }
    }
    public sealed class CardExhaustStandbyCondition
    {
        public int CardId { get; }
        public int? ExhaustedCardId { get; }
        public bool KeepInStandBy { get; }
        public CardExhaustStandbyCondition(int cardId, int? exhaustedCardId, bool keepInStandBy = false)
        { CardId = cardId; ExhaustedCardId = exhaustedCardId; KeepInStandBy = keepInStandBy; }
    }
    public static class CardExhaustModel
    {
        private const string Trait = "CardTraitExhaustState";
        private static CardTraitCompositionState RefreshTraits(CardInstanceState card)
        {
            CardMaskDescriptor? descriptor = card.MaskDescriptor;
            if (descriptor == null) return new CardTraitCompositionState([], [], [], [], []);
            return CardTraitCompositionModel.Refresh(new CardTraitCompositionState(descriptor.BaseTraits,
                descriptor.TemporaryTraits, card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Select(upgrade =>
                    (IReadOnlyList<CardTraitValue>)(upgrade.Lifecycle?.AddedTraits ?? Array.Empty<CardTraitValue>())).ToArray(),
                card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Where(upgrade => upgrade.Lifecycle != null)
                    .SelectMany(upgrade => upgrade.Lifecycle!.RemovedRuntimeTypes).ToArray(),
                descriptor.PermanentReplacements));
        }
        private static IReadOnlyList<CardTraitValue> BaseTraits(CardInstanceState card) => RefreshTraits(card).BaseTraits;
        public static bool HasConsume(CardInstanceState card) => RefreshTraits(card).CombinedTraits?.Any(trait => trait.RuntimeType == Trait) == true;
        internal static string? PermanentUpgradeError(CardInstanceState card, CardUpgradeModifier upgrade)
        {
            CardTraitValue[] additions = upgrade.Lifecycle?.AddedTraits.Where(trait => trait.RuntimeType == Trait).ToArray() ?? Array.Empty<CardTraitValue>();
            if (additions.Length == 0) return null;
            if (card.MaskDescriptor == null || card.ExhaustState == null)
                return "Permanent Consume installation requires branch-owned trait and private-state metadata.";
            if (additions.Any(trait => trait.StackMode != 0))
                return "Permanent Consume stack modes other than non-stacking installation are not modeled.";
            return null;
        }
        internal static CardMaskDescriptor? InstallPermanentConsumeTraits(CardInstanceState source,
            IReadOnlyList<CardUpgradeModifier> addedUpgrades)
        {
            CardMaskDescriptor? descriptor = source.MaskDescriptor;
            CardTraitValue[] added = addedUpgrades.SelectMany(upgrade => upgrade.Lifecycle?.AddedTraits ?? Array.Empty<CardTraitValue>())
                .Where(trait => trait.RuntimeType == Trait).ToArray();
            if (descriptor == null || added.Length == 0 || HasConsume(source)) return descriptor;
            return
                descriptor.WithBaseTraits(descriptor.BaseTraits.Concat(added).ToArray());
        }
        internal static string? CreationError(CardCreationRule creation) =>
            creation.StartingModifiers.Upgrades.Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true) &&
                creation.MaskDescriptor?.BaseTraits.Any(trait => trait.RuntimeType == Trait) != true
                ? "Generated permanent Consume upgrades require their installed base trait descriptor." : null;
        private static int ActiveBaseIndex(CardInstanceState card) => HasConsume(card)
            ? BaseTraits(card).Select((trait, index) => new { trait, index })
                .Where(item => item.trait.RuntimeType == Trait).Select(item => item.index).DefaultIfEmpty(-1).Last() : -1;
        public static bool ShouldExhaustOnPlayedDiscard(CardInstanceState card) => HasConsume(card) &&
            unchecked(card.PlayCount + 1) >= unchecked(1 + (card.ExhaustState ??
                throw new InvalidOperationException("Consume requires captured private trait state.")).PlayCountModifier);
        internal static string? Validate(CardInstanceState card)
        {
            if (card.ExhaustState == null) return null;
            if (card.MaskDescriptor == null) return "Consume state requires a branch-owned trait descriptor.";
            int[] indices = BaseTraits(card).Select((trait, index) => new { trait, index })
                .Where(item => item.trait.RuntimeType == Trait).Select(item => item.index).ToArray();
            if (!card.ExhaustState.BaseTraits.Select(item => item.TraitIndex).SequenceEqual(indices) ||
                card.ExhaustState.BaseTraits.Any(item => item.ExhaustedCardId <= 0) ||
                card.ExhaustState.UpgradeTrait?.ExhaustedCardId <= 0)
                return "Invalid private Consume trait state.";
            bool upgradeActive = HasConsume(card) && ActiveBaseIndex(card) < 0;
            if (upgradeActive != (card.ExhaustState.UpgradeTrait != null)) return "Consume upgrade trait state differs from the active composition.";
            return null;
        }
        public static CardInstanceState Initialize(CardInstanceState card, int playCountModifier)
        {
            if (card.MaskDescriptor == null) throw new InvalidOperationException("New Consume state requires a trait descriptor.");
            var bases = BaseTraits(card).Select((trait, index) => new { trait, index })
                .Where(item => item.trait.RuntimeType == Trait).Select(item => new CardExhaustTraitState(item.index)).ToArray();
            return card.WithExhaustState(new CardExhaustState(playCountModifier, bases,
                HasConsume(card) && ActiveBaseIndex(card) < 0 ? new CardExhaustTraitState(-1) : null));
        }
        internal static CardInstanceState InitializeCreated(CardInstanceState card, CombatContext context)
        {
            CardExhaustState? template = context.CardRegistry?.Select(item => item.ExhaustState).FirstOrDefault(item => item != null)
                ?? context.CardInstances?.Select(item => item.ExhaustState).FirstOrDefault(item => item != null);
            return template == null ? card : Initialize(card, template.PlayCountModifier);
        }
        // Native rebuilds traits contributed by upgrades whenever the modifier list is dirtied.
        internal static CardInstanceState RefreshUpgradeTrait(CardInstanceState card)
        {
            CardExhaustState? state = card.ExhaustState;
            if (state == null) return HasConsume(card) ? Initialize(card, 0) : card;
            CardMaskDescriptor descriptor = card.MaskDescriptor ??
                throw new InvalidOperationException("Consume state requires a branch-owned trait descriptor.");
            int[] indices = BaseTraits(card).Select((trait, index) => new { trait, index })
                .Where(item => item.trait.RuntimeType == Trait).Select(item => item.index).ToArray();
            var previous = state.BaseTraits.ToDictionary(item => item.TraitIndex);
            CardExhaustTraitState[] bases = indices.Select(index => previous.TryGetValue(index, out CardExhaustTraitState? item)
                ? item : new CardExhaustTraitState(index)).ToArray();
            return card.WithExhaustState(new CardExhaustState(state.PlayCountModifier, bases,
                HasConsume(card) && ActiveBaseIndex(card) < 0 ? new CardExhaustTraitState(-1) : null));
        }
        internal static CardInstanceState MarkPlayedDiscard(CardInstanceState card)
        {
            if (!ShouldExhaustOnPlayedDiscard(card)) return card;
            CardExhaustState state = card.ExhaustState!;
            int active = ActiveBaseIndex(card);
            return card.WithExhaustState(new CardExhaustState(state.PlayCountModifier,
                state.BaseTraits.Select(item => item.TraitIndex == active ? new CardExhaustTraitState(active, true, card.InstanceId) : item).ToArray(),
                active < 0 ? new CardExhaustTraitState(-1, true, card.InstanceId) : state.UpgradeTrait));
        }
        public static CardInstanceState ResetTraits(CardInstanceState card)
        {
            if (!HasConsume(card) || card.ExhaustState == null) return card;
            CardExhaustState state = card.ExhaustState; int active = ActiveBaseIndex(card);
            return card.WithExhaustState(new CardExhaustState(state.PlayCountModifier,
                state.BaseTraits.Select(item => item.TraitIndex == active ? new CardExhaustTraitState(active, false, item.ExhaustedCardId) : item).ToArray(),
                active < 0 ? new CardExhaustTraitState(-1, false, state.UpgradeTrait!.ExhaustedCardId) : state.UpgradeTrait));
        }
        internal static CombatContext MoveConsumedToStandby(CombatContext context, CardToken card)
        {
            CardPileState? standby = context.OtherPiles?.SingleOrDefault(pile => pile.Name == "Standby");
            CardPileState? exhausted = context.OtherPiles?.SingleOrDefault(pile => pile.Name == "Exhausted");
            if (standby == null || exhausted == null) throw new InvalidOperationException("Consume requires standby and exhausted piles.");
            bool alreadyStandby = standby.Cards.Any(item => item.InstanceId == card.InstanceId);
            standby = alreadyStandby
                ? CardPileModel.UnbindUnit(CardPileModel.BindExhaust(standby,
                    new CardExhaustStandbyCondition(card.InstanceId, null, keepInStandBy: true)), card.InstanceId)
                : CardPileModel.BindExhaust(CardPileModel.Add(standby, card),
                    new CardExhaustStandbyCondition(card.InstanceId, card.InstanceId));
            CardCycleState cards = context.Cards;
            return context.WithCards(new CardCycleState(cards.Hand.Where(item => item.InstanceId != card.InstanceId).ToArray(),
                cards.Draw.Where(item => item.InstanceId != card.InstanceId).ToArray(),
                cards.Discard.Where(item => item.InstanceId != card.InstanceId).ToArray(), cards.Rng,
                cards.DrawModifier, cards.ExternalInteractions, cards.BonusDraw, cards.NextDrawUpgrades, cards.NextUpgradeReferenceId))
                .WithOtherPiles(context.OtherPiles!.Select(pile => pile.Name == "Standby" ? standby : pile.Name == "Exhausted"
                    ? CardPileModel.Add(CardPileModel.Remove(pile, card.InstanceId), card) : pile).ToArray())
                .WithStatistics(context.Statistics?.Increment(card.InstanceId, "TimesExhausted"));
        }
        public static CombatContext ReturnReady(CombatContext context)
        {
            CardPileState? standby = context.OtherPiles?.SingleOrDefault(pile => pile.Name == "Standby");
            foreach (CardExhaustStandbyCondition condition in standby?.ExhaustConditions ?? Array.Empty<CardExhaustStandbyCondition>())
            {
                if (condition.KeepInStandBy) continue;
                CardToken card = standby!.Cards.Single(item => item.InstanceId == condition.CardId);
                CardInstanceState? retained = condition.ExhaustedCardId.HasValue ? context.FindCard(condition.ExhaustedCardId.Value) : null;
                if (condition.ExhaustedCardId.HasValue && retained == null) throw new InvalidOperationException("Missing retained Consume standby card.");
                bool discard = retained != null && !HasConsume(retained);
                context = context.WithOtherPiles(context.OtherPiles!.Select(pile => pile.Name == "Standby" ? CardPileModel.Remove(pile, card.InstanceId)
                    : pile.Name == "Exhausted" ? discard ? CardPileModel.Remove(pile, card.InstanceId)
                    : CardPileModel.Add(CardPileModel.Remove(pile, card.InstanceId), card) : pile).ToArray());
                if (discard)
                {
                    CardCycleState cards = context.Cards;
                    context = context.WithCards(new CardCycleState(cards.Hand, cards.Draw, cards.Discard.Concat(new[] { card }).ToArray(),
                        cards.Rng, cards.DrawModifier, cards.ExternalInteractions, cards.BonusDraw, cards.NextDrawUpgrades, cards.NextUpgradeReferenceId));
                }
                else context = context.WithStatistics(context.Statistics?.Increment(card.InstanceId, "TimesExhausted"));
            }
            return context;
        }
    }
}

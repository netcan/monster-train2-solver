using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class CardRetainModel
    {
        public const string Trait = "CardTraitRetain";
        public static bool Present(CardInstanceState card) => card.MaskDescriptor != null &&
            (card.MaskDescriptor.BaseTraits.Concat(card.MaskDescriptor.TemporaryTraits).Any(trait => trait.RuntimeType == Trait) ||
             card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == Trait) == true) ||
             card.MaskDescriptor.PermanentReplacements.Any(rule => rule.OldName == Trait || rule.NewRuntimeType == Trait));

        public static string? Validate(CardInstanceState card)
        {
            if (!Present(card)) return null;
            var descriptor = card.MaskDescriptor!;
            var traits = CardExhaustModel.RefreshTraits(card).CombinedTraits!.Where(trait => trait.RuntimeType == Trait).ToArray();
            if (traits.Length == 0) return null;
            if (traits.Any(trait => trait.DeclaredName != Trait || !trait.HoldoverPending.HasValue))
                return "Holdover requires captured private pending state for every active trait instance.";
            bool upgradeOwned = !CardExhaustModel.RefreshTraits(card).BaseTraits.Concat(descriptor.TemporaryTraits)
                .Any(trait => trait.RuntimeType == Trait);
            if (upgradeOwned && !descriptor.HoldoverUpgradePending.HasValue)
                return "Holdover upgrade trait requires its captured private pending state.";
            return null;
        }
        public static CardInstanceState Discarded(CardInstanceState card, bool wasPlayed,
            bool triggersOnUnplayed = false, bool isRecastCopy = false)
        {
            string? error = Validate(card);
            if (error != null) throw new InvalidOperationException(error);
            if (!Present(card) || isRecastCopy) return card;
            var active = CardExhaustModel.RefreshTraits(card).CombinedTraits!;
            if (!active.Any(trait => trait.RuntimeType == Trait)) return card;
            return wasPlayed || triggersOnUnplayed && active.Any(trait => trait.RuntimeType == "CardTraitUnplayable")
                ? Set(card, true) : card;
        }
        public static CardInstanceState Reset(CardInstanceState card)
        {
            string? error = Validate(card);
            if (error != null) throw new InvalidOperationException(error);
            return Present(card) && CardExhaustModel.RefreshTraits(card).CombinedTraits!.Any(trait => trait.RuntimeType == Trait)
                ? Set(card, false) : card;
        }
        public static bool TriggersOnUnplayed(CardInstanceState card, CardPlayRule? definition) =>
            definition?.OnUnplayedNegativeEffects.Count > 0 || definition?.HasUnsupportedUnplayedTriggers == true ||
            card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade =>
                upgrade.Lifecycle?.Triggers.Any(trigger => trigger.Type is "OnUnplayedNegative" or "OnUnplayedPositive") == true);
        private static CardInstanceState Set(CardInstanceState card, bool pending)
        {
            var descriptor = card.MaskDescriptor!;
            var bases = CardExhaustModel.RefreshTraits(card).BaseTraits;
            bool installed = bases.Concat(descriptor.TemporaryTraits).Any(trait => trait.RuntimeType == Trait);
            return card.WithMaskDescriptor(descriptor.WithHoldover(
                bases.Select(trait => trait.RuntimeType == Trait ? trait.WithHoldover(pending) : trait).ToArray(),
                descriptor.TemporaryTraits.Select(trait => trait.RuntimeType == Trait ? trait.WithHoldover(pending) : trait).ToArray(),
                installed ? null : pending));
        }
        // Native rebuilds upgrade-owned trait instances when modifiers become dirty;
        // directly installed base/temporary instances keep their private state.
        internal static CardInstanceState RefreshUpgradeTrait(CardInstanceState card)
        {
            if (card.MaskDescriptor == null) return card;
            var descriptor = card.MaskDescriptor;
            var refreshed = CardExhaustModel.RefreshTraits(card);
            bool installed = refreshed.BaseTraits.Concat(descriptor.TemporaryTraits).Any(trait => trait.RuntimeType == Trait);
            bool active = refreshed.CombinedTraits!.Any(trait => trait.RuntimeType == Trait);
            return card.WithMaskDescriptor(descriptor.WithHoldover(refreshed.BaseTraits, descriptor.TemporaryTraits,
                active && !installed ? false : (bool?)null));
        }

        public static TypedDrawResult PreDrawCard(CombatContext source, int cardId, BattlePlayRules? definitions = null)
        {
            var card = source.FindCard(cardId);
            if (card == null) return new(null, [], "Holdover pre-draw requires a captured card instance.");
            string? error = Validate(card);
            if (error != null) return new(null, [], error);
            if (!Present(card)) return new(source, []);
            var active = CardExhaustModel.RefreshTraits(card).CombinedTraits!;
            if (!active.Any(trait => trait.RuntimeType == Trait && trait.HoldoverPending == true)) return new(source, []);
            CombatContext context = source.WithCard(Set(card, false));
            if (context.Cards.Hand.Any(token => token.InstanceId == cardId)) return new(context, []);
            TypedDrawResult drawn = NextDrawUpgradeModel.DrawSpecific(context, cardId, 0, definitions);
            if (!drawn.Supported) return drawn;
            var cards = drawn.Context!.Cards;
            // Native spends a normal draw even if DrawSpecificCard fails at capacity.
            context = drawn.Context.WithCards(new CardCycleState(cards.Hand, cards.Draw, cards.Discard, cards.Rng,
                unchecked(cards.DrawModifier - 1), cards.ExternalInteractions, cards.BonusDraw,
                cards.NextDrawUpgrades, cards.NextUpgradeReferenceId, cards.DeckDrawDisabledThisTurn));
            return new(context, drawn.DrawnCardIds);
        }
        public static TypedDrawResult PreDrawHand(CombatContext source, BattlePlayRules? definitions = null, bool opening = false)
        {
            if (!opening && (source.Cards.Hand.Count == source.MaxHandSize ||
                source.Cards.Hand.Count + source.Cards.Draw.Count + source.Cards.Discard.Count == 0)) return new(source, []);
            CombatContext context = source;
            foreach (int pile in new[] { 0, 1, 2 })
            {
                var tokens = pile == 0 ? context.Cards.Hand : pile == 1 ? context.Cards.Draw : context.Cards.Discard;
                foreach (CardToken token in tokens.Reverse().ToArray())
                {
                    var card = context.FindCard(token.InstanceId);
                    if (card == null || !Present(card)) continue;
                    TypedDrawResult drawn = PreDrawCard(context, token.InstanceId, definitions);
                    if (!drawn.Supported) return drawn;
                    context = drawn.Context!;
                }
            }
            if (opening)
                foreach (CardToken token in context.Cards.Draw)
                {
                    var card = context.FindCard(token.InstanceId);
                    if (card != null && Present(card))
                    {
                        string? error = Validate(card);
                        if (error != null) return new(null, [], error);
                        context = context.WithCard(Reset(card));
                    }
                }
            return new(context, context.Cards.Hand.Where(token => source.Cards.Hand.All(prior => prior.InstanceId != token.InstanceId))
                .Select(token => token.InstanceId).ToArray());
        }
    }
}

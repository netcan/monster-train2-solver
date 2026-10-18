using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class NextDrawUpgradeRule
    {
        public CardUpgradeModifier Upgrade { get; }
        public IReadOnlyList<CardUpgradeMaskRule> Filters { get; }
        public NextDrawUpgradeRule(CardUpgradeModifier upgrade, IReadOnlyList<CardUpgradeMaskRule> filters)
        { Upgrade = upgrade; Filters = Array.AsReadOnly(filters.ToArray()); }
    }

    public static class NextDrawUpgradeModel
    {
        public static CardCycleState Schedule(CardCycleState cards, NextDrawUpgradeRule rule)
        {
            var pending = cards.NextDrawUpgrades ?? throw new ArgumentException("Missing captured next-draw upgrade queue.", nameof(cards));
            if (!cards.NextUpgradeReferenceId.HasValue)
                throw new ArgumentException("Missing captured next-draw upgrade reference allocation.", nameof(cards));
            if (rule.Upgrade.Unique && rule.Upgrade.DataId.Length > 0 && pending.Any(item => item.Upgrade.DataId == rule.Upgrade.DataId)) return cards;
            int? referenceId = cards.NextUpgradeReferenceId;
            if (referenceId <= 0 || referenceId == int.MaxValue)
                throw new ArgumentException("Invalid next-draw upgrade reference allocation.", nameof(cards));
            // Each effect constructs a new native object, even when its data ID
            // repeats. A rejected unique addition never consumes an identity.
            var scheduled = new NextDrawUpgradeRule(rule.Upgrade.WithSharedReference(referenceId), rule.Filters);
            return new CardCycleState(cards.Hand, cards.Draw, cards.Discard, cards.Rng, cards.DrawModifier,
                cards.ExternalInteractions, cards.BonusDraw, pending.Concat(new[] { scheduled }).ToArray(),
                referenceId.HasValue ? referenceId.Value + 1 : (int?)null);
        }

        // This is the native effect body; callers perform TestEffect first.
        public static CombatContext Schedule(CombatContext context, NextDrawUpgradeRule rule, int requiredGold = 0)
        {
            int cost = Math.Abs(requiredGold);
            if (cost > 0) context = context.WithGold(Math.Max(0, unchecked(context.Gold - cost)));
            return context.WithCards(Schedule(context.Cards, rule));
        }

        // The complete-train path settles the gold signal before constructing
        // the new native upgrade and appending it to the draw queue.
        public static TrainCombatResult Schedule(TrainCombatState source, NextDrawUpgradeRule rule, int requiredGold = 0)
        {
            if (requiredGold == int.MinValue) return GoldModel.Fail("Native next-draw gold cost overflows Math.Abs.");
            int cost = Math.Abs(requiredGold);
            TrainCombatResult paid = cost > 0 ? GoldModel.Adjust(source, -cost) : GoldModel.Success(source);
            if (!paid.Supported) return paid;
            if (paid.State!.Context == null) return GoldModel.Fail("Next-draw upgrades require shared battle context.");
            return GoldModel.Success(CardSpellModel.WithContext(paid.State,
                paid.State.Context.WithCards(Schedule(paid.State.Context.Cards, rule))));
        }

        public static CardCycleState Clear(CardCycleState cards) => cards.NextDrawUpgrades?.Count > 0
            ? WithPending(cards, Array.Empty<NextDrawUpgradeRule>()) : cards;

        public static TypedDrawResult DrawSpecific(CombatContext source, int cardId, int playedCardId,
            BattlePlayRules? definitions = null)
        {
            if (source.Cards.ExternalInteractions.Count > 0)
                return new TypedDrawResult(null, Array.Empty<int>(), string.Join("; ", source.Cards.ExternalInteractions));
            if (source.MaxHandSize < 0 || source.Cards.Hand.Count > source.MaxHandSize)
                return new TypedDrawResult(null, Array.Empty<int>(), "Invalid draw hand size.");
            if (source.Cards.Hand.Count == source.MaxHandSize) return new TypedDrawResult(source, Array.Empty<int>());
            CardCycleState cards = source.Cards;
            CardToken? token = cards.Draw.Concat(cards.Discard).FirstOrDefault(card => card.InstanceId == cardId);
            if (token == null) return new TypedDrawResult(null, Array.Empty<int>(), "The specific drawn card is not in the deck or discard pile.");
            var moved = new CardCycleState(new[] { token }.Concat(cards.Hand).ToArray(),
                cards.Draw.Where(card => card.InstanceId != cardId).ToArray(), cards.Discard.Where(card => card.InstanceId != cardId).ToArray(),
                cards.Rng, cards.DrawModifier, cards.ExternalInteractions, cards.BonusDraw, cards.NextDrawUpgrades, cards.NextUpgradeReferenceId);
            CombatContext context = source.WithCards(moved).WithStatistics(source.Statistics?.Increment(cardId, "TimesDrawn"));
            context = Apply(context, new[] { cardId }, playedCardId, definitions, out string? error);
            if (error == null && RelicCorruptedAngelsModel.Enabled(context))
            {
                var added = RelicFirstCardCostModel.ApplyOnCardAdded(context, cardId, definitions);
                if (!added.Supported) return new(null, Array.Empty<int>(), added.UnsupportedReason);
                context = added.Context;
                var existing = new HashSet<int>(source.Cards.Hand.Select(card => card.InstanceId));
                return new(context, context.Cards.Hand.Reverse().Where(card => !existing.Contains(card.InstanceId)).Select(card => card.InstanceId).ToArray());
            }
            return error == null ? new TypedDrawResult(context, new[] { cardId }) : new TypedDrawResult(null, Array.Empty<int>(), error);
        }

        // Spell draws apply upgrades as each card enters the hand. The ordinary
        // draw-completion signal clears bonus listeners only after the last card.
        public static TypedDrawResult DrawCards(CombatContext source, int count, int playedCardId,
            BattlePlayRules? definitions = null)
        {
            string? error = source.Cards.ExternalInteractions.Count > 0 ? string.Join("; ", source.Cards.ExternalInteractions) :
                source.MaxHandSize < 0 || source.Cards.Hand.Count > source.MaxHandSize ? "Invalid draw hand size." : BonusDrawModel.Validate(source.Cards);
            if (error != null) return new TypedDrawResult(null, Array.Empty<int>(), error);
            CombatContext context = source;
            var drawn = new List<int>();
            int capacity = source.MaxHandSize - source.Cards.Hand.Count +
                (source.Cards.Hand.Any(card => card.InstanceId == playedCardId) ? 1 : 0);
            for (int index = 0; index < Math.Min(count, capacity); index++)
            {
                CardCycleState cards = context.Cards;
                if (cards.Draw.Count == 0)
                {
                    ShuffleResult<CardToken> shuffled = cards.Rng.Shuffle(cards.Discard);
                    cards = new CardCycleState(cards.Hand, shuffled.Items, Array.Empty<CardToken>(), shuffled.State,
                        cards.DrawModifier, cards.ExternalInteractions, cards.BonusDraw, cards.NextDrawUpgrades, cards.NextUpgradeReferenceId);
                    context = context.WithCards(cards);
                }
                CardToken? token = cards.Draw.LastOrDefault(card => card.InstanceId != playedCardId);
                if (token == null) break;
                TypedDrawResult step = DrawSpecific(context, token.InstanceId, playedCardId, definitions);
                if (!step.Supported) return step;
                context = step.Context!; drawn.AddRange(step.DrawnCardIds);
            }
            CardCycleState final = context.Cards;
            BonusDrawState? bonus = BonusDrawModel.CompleteDraw(final.BonusDraw, Array.Empty<CardToken>(),
                source.Cards.Hand.Count, -1, new List<BonusUpgradeApplication>());
            context = context.WithCards(new CardCycleState(final.Hand, final.Draw, final.Discard, final.Rng,
                final.DrawModifier, final.ExternalInteractions, bonus, final.NextDrawUpgrades, final.NextUpgradeReferenceId));
            return new TypedDrawResult(context, drawn);
        }

        // The native queue survives each individual draw. It is cleared only by
        // the surrounding card-effect lifecycle, so every draw may receive it.
        public static CombatContext Apply(CombatContext source, IReadOnlyList<int> drawnCardIds,
            int playedCardId, BattlePlayRules? definitions, out string? error)
        {
            error = null;
            var pending = source.Cards.NextDrawUpgrades;
            if (pending == null || pending.Count == 0 || drawnCardIds.Count == 0)
                return RelicExpensiveCardUpgradeModel.Drawn(source, drawnCardIds, out error, definitions);
            if (!source.Cards.NextUpgradeReferenceId.HasValue || pending.Any(rule => !rule.Upgrade.SharedReferenceId.HasValue))
            { error = "Missing captured next-draw upgrade reference identity."; return source; }
            CombatContext context = source;
            CardInstanceState? played = playedCardId == 0 ? null : context.FindCard(playedCardId);
            if (CardDrawUpgradeScalingModel.Present(played))
            {
                if (pending.Count != 1 || drawnCardIds.Count != 1 || pending[0].Filters.Count != 0)
                { error = "Prism Retrieval scaling requires one unfiltered queued upgrade and one drawn card."; return source; }
                UnitUpgradeScalingResult scaled = CardDrawUpgradeScalingModel.Apply(context, playedCardId, pending[0].Upgrade);
                if (!scaled.Supported) { error = scaled.UnsupportedReason; return source; }
                pending = new[] { new NextDrawUpgradeRule(scaled.Upgrade!, pending[0].Filters) };
                context = scaled.Context!.WithCards(new CardCycleState(context.Cards.Hand, context.Cards.Draw, context.Cards.Discard,
                    context.Cards.Rng, context.Cards.DrawModifier, context.Cards.ExternalInteractions, context.Cards.BonusDraw,
                    pending, context.Cards.NextUpgradeReferenceId));
            }
            foreach (int id in drawnCardIds)
            {
                CardInstanceState? card = context.FindCard(id);
                CardPlayRule? definition = definitions?.Cards.FirstOrDefault(rule => rule.DataId == card?.DataId);
                if (card == null || definition == null)
                { error = "Missing next-draw card instance or definition."; return source; }
                var interactions = definition.UpgradeInteractions ?? definition.ExternalInteractions;
                if (interactions.Count > 0)
                { error = string.Join("; ", interactions); return source; }
                if (card.ExternalInteractions.Count > 0)
                { error = string.Join("; ", card.ExternalInteractions); return source; }
                bool accepted = true;
                foreach (NextDrawUpgradeRule entry in pending)
                {
                    if (entry.Upgrade.ExternalInteractions.Count > 0)
                    { error = string.Join("; ", entry.Upgrade.ExternalInteractions); return source; }
                    // CardManager shares this flag across all queued upgrades.
                    // Once a filter rejects, later filters and additions stop.
                    if (accepted)
                        foreach (CardUpgradeMaskRule filter in entry.Filters)
                        {
                            if (card.MaskDescriptor == null)
                            { error = "Filtered next-draw upgrades require branch-owned card metadata."; return source; }
                            if (!CardUpgradeMaskModel.FilterCard(filter, CardBranchMaskModel.Resolve(card)))
                            { accepted = false; break; }
                        }
                    if (accepted)
                        card = card.WithModifiers(card.Permanent, UnitModifierModel.Add(card.Temporary, entry.Upgrade));
                }
                // The supported OnCardDrawn callbacks do not mutate these
                // upgrades. Unsupported trait callbacks are rejected by capture.
                if (accepted)
                    foreach (NextDrawUpgradeRule entry in pending)
                    {
                        CardModifiers temporary = RemoveFirst(card.Temporary, entry.Upgrade);
                        card = card.WithModifiers(card.Permanent, temporary);
                        int multiplier = MagicMultiplier(card);
                        CardUpgradeModifier original = entry.Upgrade;
                        CardStatModifier stats = original.Stats;
                        var scaled = new CardStatModifier(unchecked(stats.Damage * multiplier), stats.Health, stats.Cost,
                            unchecked(stats.Heal * multiplier), stats.Size, stats.XCost, stats.EquipmentLimit, stats.UpgradeSlotCount);
                        CardUpgradeModifier copy = CopyWithStats(original, scaled);
                        card = card.WithModifiers(card.Permanent, UnitModifierModel.Add(card.Temporary, copy));
                    }
                context = context.WithCard(card);
            }
            return RelicExpensiveCardUpgradeModel.Drawn(context, drawnCardIds, out error, definitions);
        }

        private static CardCycleState WithPending(CardCycleState cards, IReadOnlyList<NextDrawUpgradeRule> pending) =>
            new CardCycleState(cards.Hand, cards.Draw, cards.Discard, cards.Rng, cards.DrawModifier,
                cards.ExternalInteractions, cards.BonusDraw, pending, cards.NextUpgradeReferenceId);

        private static CardModifiers RemoveFirst(CardModifiers source, CardUpgradeModifier upgrade)
        {
            var items = source.Upgrades.ToList();
            int index = upgrade.DataId.Length > 0 ? items.FindIndex(item => item.DataId == upgrade.DataId) : items.IndexOf(upgrade);
            if (index >= 0) items.RemoveAt(index);
            return new CardModifiers(source.Offsets, items, source.PersistentHealth, source.ExternalInteractions);
        }

        private static IReadOnlyList<CardTraitValue> Traits(CardInstanceState card) =>
            CardTraitRefreshModel.Ensure(CardBranchMaskModel.OwnedState(card).Traits).Composition.CombinedTraits!;

        private static int MagicMultiplier(CardInstanceState card)
        {
            if (card.MaskDescriptor == null) return 1;
            int result = 1;
            foreach (CardTraitValue trait in Traits(card))
            {
                int factor = 1;
                if (trait.RuntimeType == "CardTraitStrongerMagicPower") factor = 5;
                else if (trait.RuntimeType == "CardTraitScalingMagicPowerOnMoonPhase")
                {
                    double rounded = Math.Round(trait.MagicPowerScale ?? 1f, MidpointRounding.ToEven);
                    factor = double.IsNaN(rounded) || rounded < int.MinValue || rounded > int.MaxValue ? int.MinValue : (int)rounded;
                }
                result = unchecked(result * factor);
            }
            return result;
        }

        private static CardUpgradeModifier CopyWithStats(CardUpgradeModifier upgrade, CardStatModifier stats)
        {
            CardLifecycleUpgrade? lifecycle = upgrade.Lifecycle;
            if (lifecycle != null)
            {
                CardMaskUpgrade values = lifecycle.Values;
                lifecycle = lifecycle.WithValues(new CardMaskUpgrade(values.DataId, stats, values.Statuses,
                    values.HasIcon, values.HideIcon, values.RegionRun, values.UnitAbility));
            }
            return new CardUpgradeModifier(upgrade.DataId, upgrade.AssetKey, stats, upgrade.Statuses,
                upgrade.RemoveOnDiscard, upgrade.Unique, upgrade.ExcludeFromClones, upgrade.UnhealedHealth,
                upgrade.DamageBuff, upgrade.ExternalInteractions, upgrade.RestrictSizeToRoomCapacity,
                upgrade.MagicPowerTraitScalingOnly, upgrade.CloneDamageBase, upgrade.CloneHealBase,
                upgrade.EquipmentSourceCardId, upgrade.EquipmentSourceUpgradeIndex, upgrade.TriggerUpgrades,
                upgrade.AbilityUpgrade, upgrade.DoNotReplaceExistingAbility, upgrade.MaskMetadata,
                upgrade.CopyRemoveOnDiscard, upgrade.CopyUnique, upgrade.CopyExcludeFromClones, lifecycle);
        }
    }
}

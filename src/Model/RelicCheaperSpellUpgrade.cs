using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicCheaperSpellUpgradeModifier
    {
        public int EffectIndex { get; }
        public string CardType { get; }
        public CardUpgradeModifier Upgrade { get; }
        public IReadOnlyList<CardUpgradeMaskRule> Filters { get; }
        public IReadOnlyList<int> TrackedCardIds { get; }

        public RelicCheaperSpellUpgradeModifier(int effectIndex, string cardType, CardUpgradeModifier upgrade,
            IReadOnlyList<CardUpgradeMaskRule> filters, IReadOnlyList<int>? trackedCardIds = null)
        {
            EffectIndex = effectIndex;
            CardType = cardType;
            Upgrade = upgrade;
            Filters = Array.AsReadOnly(filters.ToArray());
            TrackedCardIds = Array.AsReadOnly((trackedCardIds ?? Array.Empty<int>()).ToArray());
        }

        internal RelicCheaperSpellUpgradeModifier Track(int cardId) =>
            new RelicCheaperSpellUpgradeModifier(EffectIndex, CardType, Upgrade, Filters, TrackedCardIds.Concat(new[] { cardId }).ToArray());

        internal RelicCheaperSpellUpgradeModifier UntrackFirst(int cardId)
        {
            int index = Array.IndexOf(TrackedCardIds.ToArray(), cardId);
            return index < 0 ? this : new RelicCheaperSpellUpgradeModifier(EffectIndex, CardType, Upgrade, Filters,
                TrackedCardIds.Where((_, offset) => offset != index).ToArray());
        }
    }

    internal sealed class RelicCheaperSpellUpgradeResult
    {
        internal CombatContext Context { get; }
        internal string? UnsupportedReason { get; }
        internal bool Supported => UnsupportedReason == null;
        internal RelicCheaperSpellUpgradeResult(CombatContext context, string? unsupportedReason = null)
        { Context = context; UnsupportedReason = unsupportedReason; }
    }

    internal static class RelicCheaperSpellUpgradeModel
    {
        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type == RelicModel.AddTempUpgradeToHandCardsIfCheaper)
                .Select(item => item.index).ToArray();
            if (indices.Length == 0)
                return relic.CheaperSpellUpgradeModifiers?.Count > 0 ? "Unexpected cheaper-spell upgrade definitions." : null;
            if (relic.CheaperSpellUpgradeModifiers == null ||
                !indices.SequenceEqual(relic.CheaperSpellUpgradeModifiers.Select(effect => effect.EffectIndex)))
                return "Missing ordered native cheaper-spell upgrade definitions.";
            foreach (RelicCheaperSpellUpgradeModifier effect in relic.CheaperSpellUpgradeModifiers)
            {
                if (effect.CardType != "Spell" || string.IsNullOrEmpty(effect.Upgrade.DataId) || effect.Upgrade.MaskMetadata == null ||
                    effect.Filters.Count == 0 || effect.TrackedCardIds.Any(id => id <= 0))
                    return "Incomplete native cheaper-spell upgrade definition.";
                CardStatModifier stats = effect.Upgrade.Stats;
                if (stats.Damage != 0 || stats.Health != 0 || stats.Cost >= 0 || stats.Heal != 0 || stats.Size != 0 ||
                    stats.XCost != 0 || stats.EquipmentLimit != 0 || stats.UpgradeSlotCount != 0 || effect.Upgrade.Statuses.Count != 0 ||
                    effect.Upgrade.RemoveOnDiscard || effect.Upgrade.ExternalInteractions.Count != 0 ||
                    effect.Upgrade.TriggerUpgrades?.Count > 0 || effect.Upgrade.AbilityUpgrade != null ||
                    effect.Upgrade.Lifecycle?.AddedTraits.Count > 0 || effect.Upgrade.Lifecycle?.RemovedRuntimeTypes.Count > 0 ||
                    effect.Upgrade.Lifecycle?.Triggers.Count > 0)
                    return "Cheaper-spell relic upgrade contains unsupported non-cost behavior.";
            }
            return null;
        }

        internal static RelicCheaperSpellUpgradeResult ApplyOnCardPlayed(CombatContext source, int playedCardId,
            BattlePlayRules? definitions, int relicIndex, int effectIndex)
        {
            if (source.Relics == null || relicIndex < 0 || relicIndex >= source.Relics.Count)
                return Fail(source, "Missing cheaper-spell relic state.");
            CombatRelicState relic = source.Relics[relicIndex];
            if (effectIndex < 0 || effectIndex >= relic.EffectTypes.Count ||
                relic.EffectTypes[effectIndex] != RelicModel.AddTempUpgradeToHandCardsIfCheaper)
                return Fail(source, "Cheaper-spell relic effect order changed.");
            RelicCheaperSpellUpgradeModifier? effect = relic.CheaperSpellUpgradeModifiers?.SingleOrDefault(item => item.EffectIndex == effectIndex);
            if (effect == null) return Fail(source, "Missing ordered native cheaper-spell upgrade definition.");

            CardInstanceState? played = source.FindCard(playedCardId);
            if (played?.MaskDescriptor == null || played.MaskDescriptor.Definition.DataId != played.DataId)
                return Fail(source, "Cheaper-spell relic requires matching played-card mask metadata.");
            if (played.MaskDescriptor.Definition.CardType != effect.CardType) return new RelicCheaperSpellUpgradeResult(source);
            CardPlayRule? playedRule = definitions?.Cards.SingleOrDefault(item => item.DataId == played.DataId);
            if (playedRule == null) return Fail(source, "Cheaper-spell relic requires the played card's cost-type rule.");
            if (effect.CardType == "Spell" && playedRule.IsAnyAbility == true) return new RelicCheaperSpellUpgradeResult(source);

            int playedCost;
            if (playedRule.CostType == "ConsumeRemainingEnergy")
            {
                CardPlayedCost? paid = source.Statistics?.PlayedCosts.SingleOrDefault(item => item.CardId == playedCardId);
                if (paid == null) return Fail(source, "Consume-remaining-energy relic checks require captured unmodified played cost.");
                playedCost = paid.Cost;
            }
            else
            {
                try { playedCost = CardBranchMaskModel.Resolve(played).CostWithoutTraits; }
                catch (InvalidOperationException error) { return Fail(source, error.Message); }
            }

            var targets = new List<CardInstanceState>();
            foreach (CardToken token in source.Cards.Hand)
            {
                if (token.InstanceId == playedCardId) continue;
                CardInstanceState? candidate = source.FindCard(token.InstanceId);
                if (candidate?.MaskDescriptor == null || candidate.MaskDescriptor.Definition.DataId != candidate.DataId)
                    return Fail(source, "Cheaper-spell relic requires matching masks for cards in hand.");
                if (candidate.MaskDescriptor.Definition.CardType != effect.CardType) continue;
                CardPlayRule? candidateRule = definitions?.Cards.SingleOrDefault(item => item.DataId == candidate.DataId);
                if (candidateRule == null) return Fail(source, "Cheaper-spell relic requires each candidate card's cost-type rule.");
                CardUpgradeMaskCard mask;
                try { mask = CardBranchMaskModel.Resolve(candidate); }
                catch (InvalidOperationException error) { return Fail(source, error.Message); }
                int candidateCost = candidateRule.CostType == "ConsumeRemainingEnergy" ? 99 : mask.CostWithoutTraits;
                if (candidateCost >= playedCost || effect.Filters.Any(filter => !CardUpgradeMaskModel.FilterCard(filter, mask))) continue;
                targets.Add(candidate);
            }

            CombatContext context = source;
            foreach (CardInstanceState target in targets)
            {
                CardInstanceState current = context.FindCard(target.InstanceId)!;
                CardUpgradeModifier upgrade = effect.Upgrade;
                bool alreadyUnique = upgrade.Unique && current.Temporary.Upgrades.Any(item => item.DataId == upgrade.DataId);
                if (!alreadyUnique)
                {
                    CardModifiers temporary = current.Temporary;
                    var changed = new CardModifiers(temporary.Offsets, temporary.Upgrades.Concat(new[] { upgrade }).ToArray(),
                        temporary.PersistentHealth, temporary.ExternalInteractions);
                    current = current.WithModifiers(current.Permanent, changed);
                    context = context.WithCard(current);
                }
                context = Track(context, relicIndex, effectIndex, current.InstanceId);
            }
            return new RelicCheaperSpellUpgradeResult(context);
        }

        internal static RelicCheaperSpellUpgradeResult CardDiscarded(CombatContext source, int cardId)
        {
            CombatContext context = source;
            for (int relicIndex = 0; relicIndex < (context.Relics?.Count ?? 0); relicIndex++)
            {
                CombatRelicState relic = context.Relics![relicIndex];
                foreach (RelicCheaperSpellUpgradeModifier effect in relic.CheaperSpellUpgradeModifiers ?? Array.Empty<RelicCheaperSpellUpgradeModifier>())
                {
                    CardInstanceState? card = context.FindCard(cardId);
                    if (card == null) return Fail(context, "Discarded cheaper-spell card is missing from owned state.");
                    bool tracked = effect.TrackedCardIds.Contains(cardId);
                    bool hasUpgrade = card.Temporary.Upgrades.Any(item => item.DataId == effect.Upgrade.DataId);
                    if (!tracked && !hasUpgrade) continue;
                    context = RemoveFromCard(context, card, effect.Upgrade.DataId);
                    context = Untrack(context, relicIndex, effect.EffectIndex, cardId);
                }
            }
            return new RelicCheaperSpellUpgradeResult(context);
        }

        internal static RelicCheaperSpellUpgradeResult EndTurnPreHandDiscard(CombatContext source)
        {
            CombatContext context = source;
            for (int relicIndex = 0; relicIndex < (context.Relics?.Count ?? 0); relicIndex++)
            {
                CombatRelicState relic = context.Relics![relicIndex];
                foreach (RelicCheaperSpellUpgradeModifier effect in relic.CheaperSpellUpgradeModifiers ?? Array.Empty<RelicCheaperSpellUpgradeModifier>())
                {
                    foreach (CardToken token in context.Cards.Hand)
                    {
                        if (!effect.TrackedCardIds.Contains(token.InstanceId)) continue;
                        CardInstanceState? card = context.FindCard(token.InstanceId);
                        if (card == null) return Fail(context, "Tracked cheaper-spell card is missing from owned state.");
                        if (!card.Temporary.Upgrades.Any(item => item.DataId == effect.Upgrade.DataId)) continue;
                        context = RemoveFromCard(context, card, effect.Upgrade.DataId);
                        context = Untrack(context, relicIndex, effect.EffectIndex, token.InstanceId);
                    }
                }
            }
            return new RelicCheaperSpellUpgradeResult(context);
        }

        private static CombatContext Track(CombatContext source, int relicIndex, int effectIndex, int cardId)
        {
            CombatRelicState[] relics = source.Relics!.ToArray();
            RelicCheaperSpellUpgradeModifier[] effects = relics[relicIndex].CheaperSpellUpgradeModifiers!.ToArray();
            int offset = Array.FindIndex(effects, item => item.EffectIndex == effectIndex);
            effects[offset] = effects[offset].Track(cardId);
            relics[relicIndex] = relics[relicIndex].WithCheaperSpellUpgradeModifiers(effects);
            return source.WithRelics(relics);
        }

        private static CombatContext Untrack(CombatContext source, int relicIndex, int effectIndex, int cardId)
        {
            CombatRelicState[] relics = source.Relics!.ToArray();
            RelicCheaperSpellUpgradeModifier[] effects = relics[relicIndex].CheaperSpellUpgradeModifiers!.ToArray();
            int offset = Array.FindIndex(effects, item => item.EffectIndex == effectIndex);
            effects[offset] = effects[offset].UntrackFirst(cardId);
            relics[relicIndex] = relics[relicIndex].WithCheaperSpellUpgradeModifiers(effects);
            return source.WithRelics(relics);
        }

        private static CombatContext RemoveFromCard(CombatContext source, CardInstanceState card, string upgradeId)
        {
            CardUpgradeModifier[] upgrades = card.Temporary.Upgrades.ToArray();
            int index = Array.FindIndex(upgrades, item => item.DataId == upgradeId);
            if (index < 0) return source;
            CardModifiers temporary = new CardModifiers(card.Temporary.Offsets,
                upgrades.Where((_, offset) => offset != index).ToArray(), card.Temporary.PersistentHealth, card.Temporary.ExternalInteractions);
            return source.WithCard(card.WithModifiers(card.Permanent, temporary));
        }

        private static RelicCheaperSpellUpgradeResult Fail(CombatContext source, string error) =>
            new RelicCheaperSpellUpgradeResult(source, error);
    }
}

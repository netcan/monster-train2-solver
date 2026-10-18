using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicRandomHandUpgrade
    {
        public int Count { get; }
        public string CardType { get; }
        public bool CanApplyInPreviewMode { get; }
        public CardUpgradeModifier Upgrade { get; }
        public IReadOnlyList<CardUpgradeMaskRule> Filters { get; }
        public IReadOnlyList<int> SelectedCardIds { get; }
        public IReadOnlyList<int> UpgradedCardIds { get; }
        public RelicRandomHandUpgrade(int count, string cardType, bool canApplyInPreviewMode,
            CardUpgradeModifier upgrade, IReadOnlyList<CardUpgradeMaskRule> filters,
            IReadOnlyList<int>? selectedCardIds = null, IReadOnlyList<int>? upgradedCardIds = null)
        { Count = count; CardType = cardType; CanApplyInPreviewMode = canApplyInPreviewMode; Upgrade = upgrade;
            Filters = Array.AsReadOnly(filters.ToArray());
            SelectedCardIds = Array.AsReadOnly((selectedCardIds ?? Array.Empty<int>()).ToArray());
            UpgradedCardIds = Array.AsReadOnly((upgradedCardIds ?? Array.Empty<int>()).ToArray()); }
        internal RelicRandomHandUpgrade WithState(IReadOnlyList<int> selected, IReadOnlyList<int> upgraded) =>
            new(Count, CardType, CanApplyInPreviewMode, Upgrade, Filters, selected, upgraded);
    }

    internal static class RelicRandomHandUpgradeModel
    {
        internal const string Effect = "RelicEffectAddTempUpgradeToRandomCardInHand";
        internal const string DataId = "128dd519-611c-4999-b7ac-e63ea027b1fa";
        internal const string AssetKey = "ReduceRandomCardCostOnUnitPlay";
        internal const string UpgradeId = "988193e9-2933-4c84-a957-71b97809170e";

        internal static string? Validate(CombatRelicState owner, RelicCardModifier modifier)
        {
            RelicRandomHandUpgrade? rule = modifier.RandomHandUpgrade;
            CardUpgradeModifier? upgrade = rule?.Upgrade;
            bool Empty<T>(UpgradeMaskContent<T> content, int required = 0) => content.Required.Count == 0 &&
                content.Excluded.Count == 0 && content.RequiredOperator == required && content.ExcludedOperator == 0;
            bool Filter(CardUpgradeMaskRule filter, bool cost) => filter.AssetKey == (cost ? "ExcludeZeroAndXCost" : "ExcludeUnplayable") &&
                filter.CardType == "Invalid" && filter.AdditionalCardTypes.Count == 0 && filter.MinCost == (cost ? 1 : 0) && filter.MaxCost == 99 &&
                filter.ExcludeXCost == cost && !filter.RequireXCost && !filter.ExcludeNonAttackingMonsters && !filter.ExcludeIfHasUnitAbility &&
                !filter.ExcludeIfHasGraftedEquipment && !filter.ExcludeIfHasAnyUpgrades && !filter.ExcludeIfHasNoUpgrades && filter.TargetMode == 0 &&
                filter.RequiredSizes.Count == 0 && filter.ExcludedSizes.Count == 0 && filter.AllowedPools.Count == 0 && filter.DisallowedPools.Count == 0 &&
                Empty(filter.Rarities, 1) && Empty(filter.Subtypes) && Empty(filter.Statuses) && Empty(filter.Effects) && Empty(filter.LinkedClans) && Empty(filter.Upgrades) &&
                filter.Traits.Required.Count == 0 && filter.Traits.RequiredOperator == 0 && filter.Traits.ExcludedOperator == 0 &&
                filter.Traits.Excluded.SequenceEqual(cost ? Array.Empty<string>() : new[] { "CardTraitUnplayable" });
            return owner.DataId == DataId && owner.AssetKey == AssetKey && owner.IsCovenant == false && owner.DisallowedInPlacementPhase == true &&
                owner.EffectTypes.SequenceEqual(new[] { Effect }) && modifier.EffectIndex == 0 && modifier.ConditionCount == 0 && modifier.Conditions.Count == 0 &&
                modifier.Rule == null && modifier.Upgrade == null && modifier.TraitReplacement == null && modifier.NoUpgradeCost == null &&
                modifier.MoonPhaseHandUpgrade == null && modifier.FreezeHandUpgrade == null && modifier.RandomFreeze == null && !modifier.ApplyToCardlessSpawns &&
                rule?.Count == 1 && rule.CardType == "Monster" && !rule.CanApplyInPreviewMode && rule.Filters.Count == 2 && Filter(rule.Filters[0], true) && Filter(rule.Filters[1], false) &&
                upgrade?.DataId == UpgradeId && upgrade.AssetKey == "FreeFromBlessing" && upgrade.Unique && !upgrade.RemoveOnDiscard && !upgrade.ExcludeFromClones &&
                upgrade.Stats.Cost == -99 && new[] { "Damage", "Health", "Heal", "Size", "XCost", "EquipmentLimit", "UpgradeSlotCount" }.All(stat => upgrade.Stats.Value(stat) == 0) &&
                upgrade.UnhealedHealth == 0 && upgrade.DamageBuff == 0 && upgrade.Statuses.Count == 0 && upgrade.MaskMetadata != null && !CardModifierModel.UpgradeInteractions(upgrade).Any()
                ? null : "Missing or changed original ReduceRandomCardCostOnUnitPlay definition.";
        }

        internal static RelicCheaperSpellUpgradeResult Test(CombatContext source, int playedId, out bool passed)
        {
            passed = false; int index = OwnerIndex(source);
            if (index < 0) return new(source);
            CombatRelicState owner = source.Relics![index]; RelicCardModifier modifier = owner.CardModifiers!.Single();
            string? error = Validate(owner, modifier); if (error != null) return new(source, error);
            CardInstanceState? played = source.FindCard(playedId);
            if (played?.MaskDescriptor == null) return new(source, "Missing played random-upgrade card metadata.");
            RelicRandomHandUpgrade rule = modifier.RandomHandUpgrade!;
            // Native returns before clearing cardsToUpgrade for the wrong played type.
            if (played.MaskDescriptor.Definition.CardType != rule.CardType) return new(source);
            var candidates = new List<int>();
            try
            {
                foreach (CardToken token in source.Cards.Hand)
                {
                    if (token.InstanceId == playedId) continue;
                    CardInstanceState? card = source.FindCard(token.InstanceId);
                    if (card?.MaskDescriptor == null || card.DataId != token.DataId) return new(source, "Missing random-upgrade hand metadata.");
                    CardUpgradeMaskCard mask = CardBranchMaskModel.Resolve(card);
                    if (rule.Filters.All(filter => CardUpgradeMaskModel.FilterCard(filter, mask))) candidates.Add(card.InstanceId);
                }
            }
            catch (InvalidOperationException failure) { return new(source, failure.Message); }
            CombatContext context = source;
            if (rule.Count < candidates.Count)
            {
                ShuffleResult<int> shuffled = context.BattleRng.Shuffle(candidates);
                candidates = shuffled.Items.Take(rule.Count).ToList(); context = context.WithBattleRng(shuffled.State);
            }
            passed = candidates.Count > 0;
            return new(Replace(context, index, rule.WithState(candidates, rule.UpgradedCardIds)));
        }

        internal static RelicCheaperSpellUpgradeResult Apply(CombatContext source)
        {
            int index = OwnerIndex(source); if (index < 0) return new(source);
            RelicRandomHandUpgrade rule = source.Relics![index].CardModifiers!.Single().RandomHandUpgrade!;
            CombatContext context = source; var tracked = rule.UpgradedCardIds.ToList();
            try
            {
                foreach (int id in rule.SelectedCardIds)
                {
                    CardInstanceState? card = context.FindCard(id);
                    if (card?.MaskDescriptor == null) return new(source, "Missing selected random-upgrade card.");
                    CardUpgradeMaskCard mask = CardBranchMaskModel.Resolve(card);
                    if (!rule.Filters.All(filter => CardUpgradeMaskModel.FilterCard(filter, mask))) continue;
                    if (mask.Traits.Any(trait => trait is "CardTraitInfusion")) return new(source, "Unmodeled random-upgrade trait callback.");
                    CardUpgradeModifier[] upgrades = card.Temporary.Upgrades.Where(item => !rule.Upgrade.Unique || item.DataId != rule.Upgrade.DataId)
                        .Concat(new[] { rule.Upgrade }).ToArray();
                    CardInstanceState changed = card.WithModifiers(card.Permanent, new CardModifiers(card.Temporary.Offsets, upgrades,
                        card.Temporary.PersistentHealth, card.Temporary.ExternalInteractions));
                    context = CardMagicPowerModel.Refresh(context, changed, out _); tracked.Add(id);
                }
            }
            catch (InvalidOperationException failure) { return new(source, failure.Message); }
            return new(Replace(context, index, rule.WithState(rule.SelectedCardIds, tracked)));
        }

        internal static RelicCheaperSpellUpgradeResult Played(CombatContext source, int playedId)
        {
            var tested = Test(source, playedId, out bool passed);
            return !tested.Supported || !passed ? tested : Apply(tested.Context);
        }
        internal static RelicCheaperSpellUpgradeResult Discarded(CombatContext source, int id)
        {
            int index = OwnerIndex(source); if (index < 0) return new(source);
            RelicRandomHandUpgrade rule = source.Relics![index].CardModifiers!.Single().RandomHandUpgrade!;
            int offset = Array.IndexOf(rule.UpgradedCardIds.ToArray(), id); if (offset < 0) return new(source);
            CardInstanceState? card = source.FindCard(id); if (card == null) return new(source, "Missing discarded random-upgrade card.");
            int remove = Array.FindIndex(card.Temporary.Upgrades.ToArray(), upgrade => upgrade.DataId == rule.Upgrade.DataId);
            var temporary = new CardModifiers(card.Temporary.Offsets, card.Temporary.Upgrades.Where((_, position) => position != remove).ToArray(),
                card.Temporary.PersistentHealth, card.Temporary.ExternalInteractions);
            CombatContext context = source.WithCard(card.WithModifiers(card.Permanent, temporary));
            return new(Replace(context, index, rule.WithState(rule.SelectedCardIds, rule.UpgradedCardIds.Where((_, position) => position != offset).ToArray())));
        }
        private static int OwnerIndex(CombatContext context) => Array.FindIndex((context.Relics ?? Array.Empty<CombatRelicState>()).ToArray(), owner => owner.EffectTypes.Contains(Effect));
        private static CombatContext Replace(CombatContext context, int index, RelicRandomHandUpgrade rule)
        {
            CombatRelicState[] owners = context.Relics!.ToArray(); RelicCardModifier old = owners[index].CardModifiers!.Single();
            owners[index] = owners[index].WithCardModifiers(new[] { new RelicCardModifier(old.EffectIndex, null, null, false, old.Conditions, randomHandUpgrade: rule) });
            return context.WithRelics(owners);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicExpensiveCardUpgradeRule
    {
        public string CardType { get; }
        public int MinimumCost { get; }
        public bool IncludeXCost { get; }
        public CardUpgradeModifier Upgrade { get; }
        public IReadOnlyList<CardUpgradeMaskRule> Filters { get; }
        public IReadOnlyList<string> ExternalInteractions { get; }
        public RelicExpensiveCardUpgradeRule(string cardType, int minimumCost, bool includeXCost,
            CardUpgradeModifier upgrade, IReadOnlyList<CardUpgradeMaskRule> filters, IReadOnlyList<string> externalInteractions)
        { CardType = cardType; MinimumCost = minimumCost; IncludeXCost = includeXCost; Upgrade = upgrade;
            Filters = Array.AsReadOnly(filters.ToArray()); ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray()); }
    }
    internal static class RelicExpensiveCardUpgradeModel
    {
        internal const string Effect = "RelicEffectAddCardUpgradeToExpensiveCards";
        internal const string DataId = "4df794d8-3dd3-4ca0-8690-727a85414968";
        internal const string AssetKey = "ApplyDazedOnAscendOrDescend";
        internal const string Trait = "CardTraitPermafrost";
        internal static string? Validate(CombatRelicState owner, RelicCardModifier modifier)
        {
            RelicExpensiveCardUpgradeRule? rule = modifier.ExpensiveCardUpgrade;
            CardUpgradeModifier? upgrade = rule?.Upgrade;
            return owner.DataId == DataId && owner.AssetKey == AssetKey && owner.IsCovenant == false && owner.DisallowedInPlacementPhase == false &&
                owner.EffectTypes.SequenceEqual(new[] { Effect }) && modifier.EffectIndex == 0 && modifier.Rule == null && modifier.Upgrade == null &&
                modifier.NoUpgradeCost == null && modifier.TraitReplacement == null && modifier.MoonPhaseHandUpgrade == null && modifier.FreezeHandUpgrade == null &&
                modifier.RandomFreeze == null && modifier.RandomHandUpgrade == null && !modifier.ApplyToCardlessSpawns && modifier.Conditions.Count == 0 && modifier.ConditionCount == 0 &&
                rule?.CardType == "Invalid" && rule.MinimumCost == 3 && rule.IncludeXCost && rule.ExternalInteractions.Count == 0 &&
                rule.Filters.Count == 1 && OriginalFilter(rule.Filters[0]) && upgrade != null && upgrade.DataId == Effect && upgrade.Unique &&
                !upgrade.RemoveOnDiscard && !upgrade.ExcludeFromClones && upgrade.UnhealedHealth == 0 && upgrade.DamageBuff == 0 &&
                !upgrade.RestrictSizeToRoomCapacity && !upgrade.MagicPowerTraitScalingOnly && upgrade.AbilityUpgrade == null &&
                (upgrade.TriggerUpgrades?.Count ?? 0) == 0 && upgrade.Statuses.Count == 0 && !CardModifierModel.UpgradeInteractions(upgrade).Any() &&
                new[] { "Damage", "Health", "Cost", "Heal", "Size", "XCost", "EquipmentLimit", "UpgradeSlotCount" }.All(stat => upgrade.Stats.Value(stat) == 0) &&
                upgrade.MaskMetadata != null && upgrade.Lifecycle != null && upgrade.Lifecycle.AddedTraits.Count == 1 &&
                upgrade.Lifecycle.AddedTraits[0].RuntimeType == Trait && upgrade.Lifecycle.AddedTraits[0].DeclaredName == Trait &&
                upgrade.Lifecycle.RemovedRuntimeTypes.Count == 0 && upgrade.Lifecycle.Triggers.Count == 0 && upgrade.Lifecycle.ReplacedAssets.Count == 0
                ? null : "Missing or changed original expensive-card Permafrost definition.";
        }
        private static bool OriginalFilter(CardUpgradeMaskRule filter) => filter.AssetKey == "CostGreaterThan3" && filter.CardType == "Invalid" &&
            filter.MinCost == 3 && filter.MaxCost == 99 && filter.AdditionalCardTypes.Count == 0 && filter.Rarities.Required.Count == 0 && filter.Rarities.Excluded.Count == 0 &&
            filter.Subtypes.Required.Count == 0 && filter.Subtypes.Excluded.Count == 0 && filter.Statuses.Required.Count == 0 && filter.Statuses.Excluded.Count == 0 &&
            filter.Traits.Required.Count == 0 && filter.Traits.Excluded.Count == 0 && filter.Effects.Required.Count == 0 && filter.Effects.Excluded.Count == 0 &&
            filter.LinkedClans.Required.Count == 0 && filter.LinkedClans.Excluded.Count == 0 && filter.Upgrades.Required.Count == 0 && filter.Upgrades.Excluded.Count == 0 &&
            filter.AllowedPools.All(pool => pool.Count == 0) && filter.DisallowedPools.All(pool => pool.Count == 0) && filter.RequiredSizes.Count == 0 && filter.ExcludedSizes.Count == 0 &&
            filter.TargetMode == 0 && !filter.ExcludeNonAttackingMonsters && !filter.RequireXCost && !filter.ExcludeXCost && !filter.ExcludeIfHasUnitAbility &&
            !filter.ExcludeIfHasGraftedEquipment && !filter.ExcludeIfHasAnyUpgrades && !filter.ExcludeIfHasNoUpgrades;
        internal static bool HasUpgrade(CardInstanceState card) => card.Temporary.Upgrades.Any(upgrade => upgrade.DataId == Effect);
        internal static bool MeetsCost(CardInstanceState card, RelicExpensiveCardUpgradeRule rule) =>
            card.MaskDescriptor!.Definition.XCost ? rule.IncludeXCost : CardModifierModel.UpgradedStat(card.RawCostOverride ?? card.MaskDescriptor.BaseCost,
                "Cost", true, card.Permanent, card.Temporary) >= rule.MinimumCost;
        internal static bool Interested(CardInstanceState card, RelicExpensiveCardUpgradeRule rule) =>
            (rule.CardType == "Invalid" || card.MaskDescriptor!.Definition.CardType == rule.CardType) && (HasUpgrade(card) || MeetsCost(card, rule));
        internal static bool MissingCombatFlag(CardInstanceState card, RelicExpensiveCardUpgradeRule rule, CombatContext context) =>
            !HasUpgrade(card) && Interested(card, rule) && context.QueryFrame?.RunningCombat == null;
        internal static CardInstanceState Refresh(CardInstanceState card, RelicExpensiveCardUpgradeRule rule, bool notifyTraits, out bool returned)
        {
            if (card.MaskDescriptor == null) throw new InvalidOperationException("Expensive-card upgrade requires captured card masks.");
            returned = Interested(card, rule); if (!returned) return card;
            bool has = HasUpgrade(card), meets = MeetsCost(card, rule);
            if (has && !meets) return card.WithModifiers(card.Permanent, new CardModifiers(card.Temporary.Offsets,
                card.Temporary.Upgrades.Where(upgrade => upgrade.DataId != Effect).ToArray(), card.Temporary.PersistentHealth, card.Temporary.ExternalInteractions));
            if (!has && meets)
            {
                card = card.WithModifiers(card.Permanent, UnitModifierModel.Add(card.Temporary, rule.Upgrade));
                if (notifyTraits) card = Freeze(card);
            }
            return card;
        }
        // Permafrost's temporary Freeze is independent of the source upgrade.
        internal static CardInstanceState Freeze(CardInstanceState card)
        {
            if (CardBranchMaskModel.Resolve(card).Traits.Contains("CardTraitFreeze")) return card;
            CardMaskDescriptor descriptor = card.MaskDescriptor!;
            return card.WithMaskDescriptor(new CardMaskDescriptor(descriptor.Definition, descriptor.BaseCost, descriptor.BaseTraits,
                descriptor.AuthoredCastEffects, descriptor.Purified, descriptor.PermanentGraft,
                descriptor.TemporaryTraits.Concat(new[] { new CardTraitValue("CardTraitFreeze", "CardTraitFreeze", 0, 0, true, 0) }).ToArray(), descriptor.PermanentReplacements, descriptor.SourceTraits));
        }
        internal static RelicFirstCardCostResult Added(CombatContext source, int cardId)
        {
            CombatContext context = source;
            foreach (CombatRelicState owner in source.Relics ?? Array.Empty<CombatRelicState>())
            foreach (RelicCardModifier modifier in owner.CardModifiers ?? Array.Empty<RelicCardModifier>())
            {
                if (owner.EffectTypes[modifier.EffectIndex] != Effect) continue;
                string? error = Validate(owner, modifier); if (error != null) return new(source, error);
                CardInstanceState? card = context.FindCard(cardId);
                if (card?.MaskDescriptor == null) return new(source, "Missing expensive-card hand-addition inputs.");
                if (MissingCombatFlag(card, modifier.ExpensiveCardUpgrade!, context))
                    return new(source, "Missing running-combat flag for expensive-card trait notification.");
                context = context.WithCard(Refresh(card, modifier.ExpensiveCardUpgrade!, context.QueryFrame?.RunningCombat == true, out _));
            }
            return new(context);
        }
        internal static CombatContext Drawn(CombatContext source, IReadOnlyList<int> ids, out string? error, BattlePlayRules? definitions = null)
        {
            error = null; CombatContext context = source;
            foreach (int id in ids)
            {
                context = CardMoonUpgradeModel.RefreshHand(context, out error, new[] { id });
                if (error != null) return source;
                CardInstanceState? card = context.FindCard(id);
                if (card?.MaskDescriptor != null && CardBranchMaskModel.Resolve(card).Traits.Contains(Trait)) context = context.WithCard(Freeze(card));
                context = ConduitModel.Drawn(context, new[] { id });
                // Native dispatch walks owners in acquisition order; recursive draws
                // finish their own complete owner walk before the parent resumes.
                foreach (CombatRelicState owner in source.Relics ?? Array.Empty<CombatRelicState>())
                {
                    if (owner.EffectTypes.Contains(RelicRandomDrawCostModel.Effect))
                    {
                        var cost = RelicRandomDrawCostModel.Apply(context, id, out _, selectedOwner: owner);
                        if (!cost.Supported) { error = cost.UnsupportedReason; return source; }
                        context = cost.Context;
                    }
                    if (owner.EffectTypes.Contains(RelicCorruptedAngelsModel.Effect))
                    {
                        var angels = RelicCorruptedAngelsModel.Apply(context, id, out _, definitions, selectedOwner: owner);
                        if (!angels.Supported) { error = angels.UnsupportedReason; return source; }
                        context = angels.Context;
                    }
                }
                var added = Added(context, id); if (!added.Supported) { error = added.UnsupportedReason; return source; }
                context = added.Context;
            }
            return context;
        }
    }
}

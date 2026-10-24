using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal sealed class RelicMoonPhaseHandUpgradeResult
    {
        internal CombatContext Context { get; }
        internal string? UnsupportedReason { get; }
        internal bool Supported => UnsupportedReason == null;
        internal RelicMoonPhaseHandUpgradeResult(CombatContext context, string? unsupportedReason = null)
        { Context = context; UnsupportedReason = unsupportedReason; }
    }

    internal static class RelicMoonPhaseHandUpgradeModel
    {
        internal const string OwnerId = "e96497a1-3c1e-4adf-b897-8ef09b931d1d";
        internal const string OwnerKey = "MoonPhaseRoomsGainMagicPower";

        // The original numeric hand upgrade has a native composition witness
        // with moon-owned traits, repeated stacks and room-mask refreshes.
        internal static bool CompatibleMoonSpells(CombatRelicState owner)
        {
            if (owner.DataId != OwnerId || owner.AssetKey != OwnerKey || owner.IsCovenant != false ||
                owner.DisallowedInPlacementPhase != false ||
                !owner.EffectTypes.SequenceEqual(new[] { RelicModel.ApplyCardUpgradeToHandEndOfTurnOnMoon }) ||
                owner.CardModifiers?.Count != 1) return false;
            RelicCardModifier effect = owner.CardModifiers[0];
            RelicMoonPhaseHandUpgrade? rule = effect.MoonPhaseHandUpgrade;
            if (effect.EffectIndex != 0 || effect.ConditionCount != 0 || effect.Conditions.Count != 0 || rule == null ||
                rule.MoonPhaseMask != 2 || rule.RemoveOnDiscard || rule.CanApplyInPreviewMode || rule.ConditionCount != 0 ||
                rule.Filters.Count != 2) return false;
            CardUpgradeModifier upgrade = rule.Upgrade;
            CardStatModifier stats = upgrade.Stats;
            CardLifecycleUpgrade? lifecycle = upgrade.Lifecycle;
            return upgrade.DataId == "ba427938-c3ad-480b-aa27-44a6426a059c" && upgrade.AssetKey == "MoonPhaseRooms_UpgradeData" &&
                stats.Damage == 10 && stats.Heal == 10 && stats.Health == 0 && stats.Cost == 0 && stats.Size == 0 &&
                stats.XCost == 0 && stats.EquipmentLimit == 0 && stats.UpgradeSlotCount == 0 &&
                upgrade.CloneDamageBase == 10 && upgrade.CloneHealBase == 10 && !upgrade.Unique && !upgrade.RemoveOnDiscard &&
                !upgrade.ExcludeFromClones && upgrade.Statuses.Count == 0 && upgrade.ExternalInteractions.Count == 0 &&
                lifecycle != null && lifecycle.AddedTraits.Count == 0 && lifecycle.RemovedRuntimeTypes.Count == 0 &&
                lifecycle.ReplacedAssets.Count == 0 && lifecycle.Triggers.Count == 0 &&
                OriginalFilter(rule.Filters[0], positive: true) && OriginalFilter(rule.Filters[1], positive: false);
        }

        private static bool OriginalFilter(CardUpgradeMaskRule filter, bool positive)
        {
            bool Empty<T>(UpgradeMaskContent<T> content, int op = 0) => content.Required.Count == 0 &&
                content.Excluded.Count == 0 && content.RequiredOperator == op && content.ExcludedOperator == 0;
            string[] excluded = { "d861b6a6-d8ef-4d58-8ebd-9f0abc276456", "537f6adf-0fd2-4c6b-88a8-e6cefeb52a7e",
                "6f23bf7b-49c3-4a30-aa84-542ab9f939d0", "10f64bfb-81cd-4b7c-bf46-6fc1fa832008" };
            string[] effects = { "CardEffectDamage", "CardEffectHeal", "CardEffectHealAndDamageRelative",
                "CardEffectHealAndOverheal", "CardEffectDamagePerSourceAttack", "CardEffectDamagePerHp" };
            return filter.AssetKey == (positive ? "MagicPowerSpells" : "ExcludedSpellsFromMagicPower") &&
                filter.CardType == (positive ? "Spell" : "Invalid") && filter.AdditionalCardTypes.Count == 0 &&
                Empty(filter.Rarities, 1) && Empty(filter.Subtypes) && Empty(filter.Statuses) && Empty(filter.Traits) &&
                Empty(filter.LinkedClans) && Empty(filter.Upgrades) && filter.AllowedPools.Count == 0 &&
                (positive ? filter.DisallowedPools.Count == 0 && filter.Effects.Required.SequenceEqual(effects) &&
                    filter.Effects.Excluded.Count == 0 && filter.Effects.RequiredOperator == 1 && filter.Effects.ExcludedOperator == 0
                    : filter.DisallowedPools.Count == 1 && filter.DisallowedPools[0].SequenceEqual(excluded) && Empty(filter.Effects)) &&
                filter.RequiredSizes.Count == 0 && filter.ExcludedSizes.Count == 0 && filter.MinCost == 0 && filter.MaxCost == 99 &&
                filter.TargetMode == 0 && !filter.ExcludeNonAttackingMonsters && !filter.RequireXCost && !filter.ExcludeXCost &&
                !filter.ExcludeIfHasUnitAbility && !filter.ExcludeIfHasGraftedEquipment && !filter.ExcludeIfHasAnyUpgrades &&
                !filter.ExcludeIfHasNoUpgrades;
        }

        internal static RelicMoonPhaseHandUpgradeResult Apply(CombatContext source, int moonPhase,
            bool deployment, bool preview)
        {
            CombatContext context = source;
            IReadOnlyList<CombatRelicState>? relics = context.Relics;
            if (relics == null) return new RelicMoonPhaseHandUpgradeResult(context);
            string? validation = RelicModel.Validate(relics);
            if (validation != null) return Fail(context, validation);
            for (int relicIndex = 0; relicIndex < relics.Count; relicIndex++)
            {
                CombatRelicState relic = relics[relicIndex];
                for (int effectIndex = 0; effectIndex < relic.EffectTypes.Count; effectIndex++)
                {
                    if (relic.EffectTypes[effectIndex] != RelicModel.ApplyCardUpgradeToHandEndOfTurnOnMoon) continue;
                    RelicCardModifier effect = relic.CardModifiers!.Single(item => item.EffectIndex == effectIndex);
                    RelicMoonPhaseHandUpgrade rule = effect.MoonPhaseHandUpgrade!;
                    if (relic.DisallowedInPlacementPhase == true && deployment || preview && !rule.CanApplyInPreviewMode ||
                        (rule.MoonPhaseMask & moonPhase) == 0) continue;

                    foreach (CardToken token in context.Cards.Hand)
                    {
                        CardInstanceState? card = context.FindCard(token.InstanceId);
                        if (card == null || card.DataId != token.DataId)
                            return Fail(context, "Moon-phase hand upgrade requires matching owned card instances.");
                        if (rule.Filters.Count > 0)
                        {
                            if (card.MaskDescriptor == null || card.MaskDescriptor.Definition.DataId != card.DataId)
                                return Fail(context, "Moon-phase hand upgrade filters require matching card mask metadata.");
                            CardUpgradeMaskCard mask;
                            try { mask = CardBranchMaskModel.Resolve(card); }
                            catch (InvalidOperationException error) { return Fail(context, error.Message); }
                            if (rule.Filters.Any(filter => !CardUpgradeMaskModel.FilterCard(filter, mask))) continue;
                        }

                        CardInstanceState changed = card;
                        bool uniqueExists = rule.Upgrade.Unique && card.Temporary.Upgrades.Any(item => item.DataId == rule.Upgrade.DataId);
                        if (!uniqueExists)
                        {
                            CardModifiers temporary = new CardModifiers(card.Temporary.Offsets,
                                card.Temporary.Upgrades.Concat(new[] { rule.Upgrade }).ToArray(),
                                card.Temporary.PersistentHealth, card.Temporary.ExternalInteractions);
                            changed = card.WithModifiers(card.Permanent, temporary);
                        }
                        try { context = CardMagicPowerModel.Refresh(context, changed, out _); }
                        catch (InvalidOperationException error) { return Fail(context, error.Message); }
                        // Native ReapplyMagicPowerScaling dispatches CardUpdated.
                        // Conduit then removes/reappends its tracked upgrade after
                        // the new hand upgrade, before discard refreshes it again.
                        context = ConduitModel.Updated(context, token.InstanceId);
                    }
                }
            }
            return new RelicMoonPhaseHandUpgradeResult(context);
        }

        private static RelicMoonPhaseHandUpgradeResult Fail(CombatContext context, string error) =>
            new RelicMoonPhaseHandUpgradeResult(context, error);
    }
}

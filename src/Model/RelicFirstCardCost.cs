using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal sealed class RelicFirstCardCostResult
    {
        internal CombatContext Context { get; }
        internal string? UnsupportedReason { get; }
        internal bool Supported => UnsupportedReason == null;
        internal RelicFirstCardCostResult(CombatContext context, string? unsupportedReason = null)
        { Context = context; UnsupportedReason = unsupportedReason; }
    }

    internal static class RelicFirstCardCostModel
    {
        internal const string Effect = "RelicEffectModifyFirstCardCostPerPile";
        internal const string DataId = "64d9943e-02e7-469d-b516-c483a3ad8ccf";
        internal const string AssetKey = "ReduceFirstSpellCostPerDeckSize";

        internal static string? Validate(CombatRelicState owner, RelicCardTypeCost rule)
        {
            CardUpgradeModifier? upgrade = rule.FirstCardUpgrade;
            if (owner.DataId != DataId || owner.AssetKey != AssetKey || owner.EffectTypes.Count != 1 ||
                owner.EffectTypes[rule.EffectIndex] != Effect || owner.DisallowedInPlacementPhase != false ||
                rule.CardType != "Spell" || rule.Amount != 15 || rule.IgnoreRestrictions ||
                rule.RequiredSubtype != null || rule.ConditionCount != 0 || rule.PileType != "DeckPile" ||
                !rule.WaitingForFirstCard.HasValue || upgrade == null || upgrade.DataId != Effect ||
                !upgrade.Unique || !upgrade.ExcludeFromClones || !upgrade.RemoveOnDiscard ||
                upgrade.Stats.Damage != 0 || upgrade.Stats.Health != 0 || upgrade.Stats.Cost > 0 ||
                upgrade.Stats.Heal != 0 || upgrade.Stats.Size != 0 || upgrade.Stats.XCost < 0 ||
                upgrade.Stats.XCost != -upgrade.Stats.Cost || upgrade.Stats.EquipmentLimit != 0 ||
                upgrade.Stats.UpgradeSlotCount != 0 || upgrade.Statuses.Count != 0 ||
                upgrade.ExternalInteractions.Count != 0 || upgrade.TriggerUpgrades?.Count > 0 ||
                upgrade.AbilityUpgrade != null || upgrade.Lifecycle?.AddedTraits.Count > 0 ||
                upgrade.Lifecycle?.RemovedRuntimeTypes.Count > 0 || upgrade.Lifecycle?.Triggers.Count > 0)
                return "Missing or changed native first-Spell-per-pile cost payload.";
            return null;
        }

        internal static RelicFirstCardCostResult ApplyOnCardAdded(CombatContext source, int cardId,
            BattlePlayRules? definitions)
        {
            CombatContext context = source;
            for (int relicIndex = 0; relicIndex < (context.Relics?.Count ?? 0); relicIndex++)
            {
                CombatRelicState relic = context.Relics![relicIndex];
                foreach (RelicCardTypeCost rule in relic.CardTypeCosts ?? Array.Empty<RelicCardTypeCost>())
                {
                    if (rule.FirstCardUpgrade == null || !rule.WaitingForFirstCard!.Value) continue;
                    if (!Eligible(context, cardId, rule, definitions, out bool eligible, out string? error))
                        return Fail(context, error!);
                    if (!eligible) continue;

                    CardInstanceState card = context.FindCard(cardId)!;
                    if (card.Temporary.Upgrades.Any(item => item.DataId == rule.FirstCardUpgrade.DataId)) continue;
                    if (context.Resources == null)
                        return Fail(context, "First-Spell cost requires captured visible deck count.");
                    int reduction = context.Resources.VisibleDeckCount / rule.Amount;
                    CardUpgradeModifier upgrade = CostUpgrade(rule.FirstCardUpgrade, reduction);
                    CombatRelicState updatedOwner = ReplaceRule(relic, rule, upgrade, waiting: true);
                    var relics = context.Relics!.ToArray(); relics[relicIndex] = updatedOwner;
                    context = context.WithRelics(relics);
                    CardModifiers temporary = card.Temporary;
                    context = context.WithCard(card.WithModifiers(card.Permanent,
                        new CardModifiers(temporary.Offsets, temporary.Upgrades.Concat(new[] { upgrade }).ToArray(),
                            temporary.PersistentHealth, temporary.ExternalInteractions)));
                    relic = updatedOwner;
                }
            }
            return new RelicFirstCardCostResult(context);
        }

        internal static RelicFirstCardCostResult StartTurnBeforeDraw(CombatContext source,
            BattlePlayRules? definitions)
        {
            RelicFirstCardCostResult reset = SetWaiting(source, true);
            if (!reset.Supported) return reset;
            CombatContext context = reset.Context;
            foreach (CardToken card in context.Cards.Hand)
            {
                RelicFirstCardCostResult applied = ApplyOnCardAdded(context, card.InstanceId, definitions);
                if (!applied.Supported) return applied;
                context = applied.Context;
            }
            return new RelicFirstCardCostResult(context);
        }

        internal static RelicFirstCardCostResult ApplyOnCardPlayed(CombatContext source, int cardId,
            BattlePlayRules? definitions)
        {
            CombatContext context = source;
            for (int relicIndex = 0; relicIndex < (context.Relics?.Count ?? 0); relicIndex++)
            {
                CombatRelicState relic = context.Relics![relicIndex];
                foreach (RelicCardTypeCost rule in relic.CardTypeCosts ?? Array.Empty<RelicCardTypeCost>())
                {
                    if (rule.FirstCardUpgrade == null || !rule.WaitingForFirstCard!.Value) continue;
                    if (!Eligible(context, cardId, rule, definitions, out bool eligible, out string? error))
                        return Fail(context, error!);
                    if (!eligible) continue;

                    CombatRelicState updatedOwner = ReplaceRule(relic, rule, rule.FirstCardUpgrade, waiting: false);
                    var relics = context.Relics!.ToArray(); relics[relicIndex] = updatedOwner;
                    context = context.WithRelics(relics);
                    HashSet<int> hand = context.Cards.Hand.Select(item => item.InstanceId).ToHashSet();
                    foreach (int id in hand)
                    {
                        CardInstanceState? card = context.FindCard(id);
                        if (card == null) return Fail(context, "First-Spell cost cleanup requires complete hand-card state.");
                        CardModifiers temporary = card.Temporary;
                        if (!temporary.Upgrades.Any(item => item.DataId == rule.FirstCardUpgrade.DataId)) continue;
                        CardModifiers cleaned = new(temporary.Offsets,
                            temporary.Upgrades.Where(item => item.DataId != rule.FirstCardUpgrade.DataId).ToArray(),
                            temporary.PersistentHealth, temporary.ExternalInteractions);
                        context = context.WithCard(card.WithModifiers(card.Permanent, cleaned));
                    }
                    relic = updatedOwner;
                }
            }
            return new RelicFirstCardCostResult(context);
        }

        internal static RelicFirstCardCostResult EndRoomCombat(CombatContext source) => SetWaiting(source, true);

        private static RelicFirstCardCostResult SetWaiting(CombatContext source, bool waiting)
        {
            CombatRelicState[]? relics = source.Relics?.ToArray();
            if (relics == null) return new RelicFirstCardCostResult(source);
            for (int relicIndex = 0; relicIndex < relics.Length; relicIndex++)
            {
                CombatRelicState owner = relics[relicIndex];
                RelicCardTypeCost[] rules = owner.CardTypeCosts?.ToArray() ?? Array.Empty<RelicCardTypeCost>();
                bool changed = false;
                for (int index = 0; index < rules.Length; index++)
                {
                    RelicCardTypeCost rule = rules[index];
                    if (rule.FirstCardUpgrade == null) continue;
                    rules[index] = rule.WithFirstCardState(rule.FirstCardUpgrade, waiting);
                    changed = true;
                }
                if (changed) relics[relicIndex] = owner.WithCardTypeCosts(rules);
            }
            return new RelicFirstCardCostResult(source.WithRelics(relics));
        }

        private static bool Eligible(CombatContext context, int cardId, RelicCardTypeCost rule,
            BattlePlayRules? definitions, out bool eligible, out string? error)
        {
            eligible = false; error = null;
            CardInstanceState? card = context.FindCard(cardId);
            if (card?.MaskDescriptor == null || card.MaskDescriptor.Definition.DataId != card.DataId)
            { error = "First-Spell cost requires matching native card masks."; return false; }
            if (card.MaskDescriptor.Definition.CardType != rule.CardType) return true;
            CardPlayRule? playRule = definitions?.Cards.SingleOrDefault(item => item.DataId == card.DataId);
            if (playRule == null)
            { error = "First-Spell cost requires the card ability classification."; return false; }
            eligible = playRule.IsAnyAbility != true && playRule.Ability == null;
            return true;
        }

        private static CardUpgradeModifier CostUpgrade(CardUpgradeModifier source, int reduction)
        {
            CardStatModifier stats = source.Stats;
            return source.WithStats(new CardStatModifier(stats.Damage, stats.Health, -reduction, stats.Heal,
                stats.Size, reduction, stats.EquipmentLimit, stats.UpgradeSlotCount));
        }

        private static CombatRelicState ReplaceRule(CombatRelicState owner, RelicCardTypeCost rule,
            CardUpgradeModifier upgrade, bool waiting) => owner.WithCardTypeCosts((owner.CardTypeCosts ?? Array.Empty<RelicCardTypeCost>())
                .Select(item => item.EffectIndex == rule.EffectIndex ? item.WithFirstCardState(upgrade, waiting) : item).ToArray());

        private static RelicFirstCardCostResult Fail(CombatContext context, string error) =>
            new RelicFirstCardCostResult(context, error);
    }
}

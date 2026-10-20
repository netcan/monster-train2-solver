using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal sealed class ForgeUpgradeModelResult
    {
        internal CombatContext? Context { get; }
        internal string? UnsupportedReason { get; }
        internal bool Supported => Context != null;
        internal ForgeUpgradeModelResult(CombatContext? context, string? reason = null)
        { Context = context; UnsupportedReason = reason; }
    }

    internal static class ForgeUpgradeModel
    {
        internal static ForgeUpgradeModelResult BeforeDirectCardPlay(CombatContext source,
            BattlePlayRules playRules, CardPlayRule cardRule, CardInstanceState? card, int forgePoints)
        {
            if (forgePoints <= 0) return new ForgeUpgradeModelResult(source);
            StatisticQueryFrame? frame = source.QueryFrame;
            // Earlier fixtures predate forge-action capture and cannot describe this transition.
            if (playRules.ForgeUpgrade == null && !cardRule.ForgeEligible.HasValue &&
                frame?.ForgeTrackingActive == null && frame?.ForgeToggleActive == null)
                return new ForgeUpgradeModelResult(source);
            if (frame == null || frame.ForgePoints != forgePoints)
                return Unsupported("Forge spend requires the current native forge-point frame.");
            if (!frame.ForgeTrackingActive.HasValue || !frame.ForgeToggleActive.HasValue)
                return Unsupported("Forge spend requires captured tracking and toggle state.");
            if (!frame.ForgeTrackingActive.Value || !frame.ForgeToggleActive.Value)
                return new ForgeUpgradeModelResult(source);
            if (!cardRule.ForgeEligible.HasValue)
                return Unsupported("Forge-eligible card state was not captured.");
            if (!cardRule.ForgeEligible.Value) return new ForgeUpgradeModelResult(source);
            if (cardRule.Effect != "SpawnMonster" && cardRule.Effect != "Equipment")
                return Unsupported("Automatic forge is only modeled for ordinary monster summons and equipment plays.");
            if (card == null) return Unsupported("Forge-eligible play is missing its card instance.");
            ForgeUpgradeDefinition? definition = playRules.ForgeUpgrade;
            if (definition == null) return Unsupported("Native forge upgrade definitions were not captured.");
            string? validation = Validate(source, definition);
            if (validation != null) return Unsupported(validation);
            if (definition.HasRoomOverride)
                return Unsupported("Room-specific forge upgrade replacement is not modeled.");
            if (definition.DefaultUpgrade == null) return new ForgeUpgradeModelResult(source);

            var maskCard = (CardUpgradeMaskCard?)null;
            if (definition.RelicUpgrades.Any(upgrade => upgrade.Filters.Count > 0))
            {
                try { maskCard = CardBranchMaskModel.Resolve(card); }
                catch (InvalidOperationException error) { return Unsupported("Forge upgrade filter: " + error.Message); }
            }
            var additions = new System.Collections.Generic.List<CardUpgradeModifier>();
            additions.Add(definition.DefaultUpgrade.WithForgePoints(forgePoints));
            foreach (ForgeUpgradeModifier modifier in definition.RelicUpgrades)
            {
                if (modifier.Filters.Any(filter => !CardUpgradeMaskModel.FilterCard(filter, maskCard))) continue;
                additions.Add(modifier.Upgrade.WithForgePoints(forgePoints));
            }
            if (additions.Any(upgrade => CardModifierModel.UpgradeInteractions(upgrade).Any()))
                return Unsupported("Forge upgrade lifecycle interactions are not modeled.");

            var temporary = card.Temporary.Upgrades.ToList();
            foreach (CardUpgradeModifier upgrade in additions)
            {
                if (!upgrade.Unique || upgrade.DataId.Length == 0 ||
                    !temporary.Any(existing => existing.DataId == upgrade.DataId))
                    temporary.Add(upgrade);
            }
            var modifiers = new CardModifiers(card.Temporary.Offsets, temporary,
                card.Temporary.PersistentHealth, card.Temporary.ExternalInteractions);
            CardInstanceState changed = card.WithModifiers(card.Permanent, modifiers).WithLastForgedAmount(forgePoints);
            foreach (CardActionEffect effect in cardRule.OnForgeEffects ?? Array.Empty<CardActionEffect>())
            {
                CardUpgradeModifier? upgrade = effect.Upgrade;
                if (effect.Type != "UpgradeSource" || effect.Target != "Self" || effect.Lifetime != "Permanent" ||
                    effect.Value != 1 || !effect.ScaleUpgrade || upgrade == null || upgrade.Unique ||
                    CardModifierModel.UpgradeInteractions(upgrade).Any() ||
                    !new[] { "Damage", "Health", "Cost", "Heal", "Size", "XCost", "EquipmentLimit", "UpgradeSlotCount" }
                        .All(stat => upgrade.Stats.Value(stat) == 0) ||
                    upgrade.Statuses.Count != 1 || upgrade.Statuses[0].Id != "armor" || upgrade.Statuses[0].Stacks <= 0)
                    return Unsupported("Uncalibrated OnForge source upgrade; only permanent fixed armor per Forge Point is supported.");
                // Native accumulates a scalable existing instance only when this
                // invocation spends more than one point; one point appends a new upgrade.
                changed = changed.WithPermanentUpgrade(upgrade.WithSharedReference(null).WithForgePoints(forgePoints), forgePoints > 1);
            }
            string? cardError = CardModifierModel.UnsupportedReason(changed);
            if (cardError != null) return Unsupported("Forged card: " + cardError);
            return new ForgeUpgradeModelResult(source.WithCard(changed).WithQueryFrame(frame.With(forgePoints: 0)));
        }

        private static string? Validate(CombatContext source, ForgeUpgradeDefinition definition)
        {
            if (definition.RelicUpgrades.Select(upgrade => (upgrade.RelicDataId, upgrade.EffectIndex)).Distinct().Count() !=
                definition.RelicUpgrades.Count)
                return "Duplicate captured forge relic upgrade identity.";
            if (source.Relics == null) return "Forge relic owners were not captured.";
            var owners = source.Relics.SelectMany(relic => relic.EffectTypes.Select((type, index) =>
                (Relic: relic, Type: type, Index: index)))
                .Where(item => item.Type == RelicModel.ModifyForgeUpgrade).ToArray();
            if (owners.Length != definition.RelicUpgrades.Count)
                return "Captured forge relic owner count differs from the battle context.";
            for (int index = 0; index < owners.Length; index++)
            {
                var owner = owners[index]; ForgeUpgradeModifier modifier = definition.RelicUpgrades[index];
                if (owner.Relic.DataId != modifier.RelicDataId || owner.Relic.AssetKey != modifier.RelicAssetKey ||
                    owner.Index != modifier.EffectIndex)
                    return "Captured forge relic owner order or identity differs.";
                if (modifier.Upgrade.ExternalInteractions.Count > 0)
                    return "Forge relic upgrade contains unmodeled card upgrade interactions.";
            }
            return definition.DefaultUpgrade?.ExternalInteractions.Count > 0
                ? "Default forge upgrade contains unmodeled card upgrade interactions." : null;
        }

        private static ForgeUpgradeModelResult Unsupported(string reason) => new ForgeUpgradeModelResult(null, reason);
    }
}

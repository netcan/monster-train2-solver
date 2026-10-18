using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class CardMagicPowerModel
    {
        public static int Multiplier(CardInstanceState card)
        {
            if (card.MaskDescriptor == null) return 1;
            var traits = CardTraitRefreshModel.Ensure(CardBranchMaskModel.OwnedState(card).Traits).Composition.CombinedTraits!;
            return Multiplier(traits);
        }
        internal static int Multiplier(IEnumerable<CardTraitValue> traits)
        {
            int multiplier = 1;
            foreach (CardTraitValue trait in traits)
            {
                int factor = 1;
                if (trait.RuntimeType == "CardTraitStrongerMagicPower") factor = 5;
                else if (trait.RuntimeType == "CardTraitScalingMagicPowerOnMoonPhase")
                {
                    double rounded = Math.Round(trait.MagicPowerScale ?? 1f, MidpointRounding.ToEven);
                    factor = double.IsNaN(rounded) || rounded < int.MinValue || rounded > int.MaxValue ? int.MinValue : (int)rounded;
                }
                multiplier = unchecked(multiplier * factor);
            }
            return multiplier;
        }

        // ApplyPermanentUpgrade and DrawSpecificCard scale the current values,
        // including negative and anonymous payloads.
        public static CardUpgradeModifier Scale(CardUpgradeModifier upgrade, int multiplier) => WithValues(upgrade,
            unchecked(upgrade.Stats.Damage * multiplier), unchecked(upgrade.Stats.Heal * multiplier));

        // CardUpgradeHelper refreshes every installed upgrade after application.
        // Native only resets positive authored bases whose scaling is not owned
        // by another trait; zero, negative and anonymous values are preserved.
        public static CardInstanceState Refresh(CardInstanceState card)
            => Refresh(card, Multiplier(card));
        internal static CardInstanceState Refresh(CardInstanceState card, int multiplier)
        {
            CardModifiers RefreshModifiers(CardModifiers modifiers) => new CardModifiers(modifiers.Offsets,
                modifiers.Upgrades.Select(upgrade => WithValues(upgrade,
                    upgrade.CloneDamageBase.HasValue ? unchecked(upgrade.CloneDamageBase.Value * multiplier) : upgrade.Stats.Damage,
                    upgrade.CloneHealBase.HasValue ? unchecked(upgrade.CloneHealBase.Value * multiplier) : upgrade.Stats.Heal)).ToArray(),
                modifiers.PersistentHealth, modifiers.ExternalInteractions);
            return card.WithModifiers(RefreshModifiers(card.Permanent), RefreshModifiers(card.Temporary), refreshTraits: false);
        }

        // Native refresh mutates upgrade objects in place. A next-draw original
        // may remain installed on several cards after its queue was cleared.
        // Replacing every alias in this branch preserves that behavior without
        // writing to the immutable parent or relying on definition IDs.
        public static CombatContext Refresh(CombatContext context, CardInstanceState card, out CardInstanceState refreshed)
        {
            var shared = new Dictionary<int, CardUpgradeModifier>();
            refreshed = Refresh(card);
            foreach (CardUpgradeModifier upgrade in refreshed.Permanent.Upgrades.Concat(refreshed.Temporary.Upgrades))
                if (upgrade.SharedReferenceId.HasValue) shared[upgrade.SharedReferenceId.Value] = upgrade;
            CardUpgradeModifier Replace(CardUpgradeModifier upgrade) =>
                upgrade.SharedReferenceId.HasValue && shared.TryGetValue(upgrade.SharedReferenceId.Value, out CardUpgradeModifier? alias)
                    ? alias : upgrade;
            CardModifiers ReplaceModifiers(CardModifiers modifiers) => new CardModifiers(modifiers.Offsets,
                modifiers.Upgrades.Select(Replace).ToArray(), modifiers.PersistentHealth, modifiers.ExternalInteractions);
            CardInstanceState ReplaceCard(CardInstanceState target) => target.WithModifiers(
                ReplaceModifiers(target.Permanent), ReplaceModifiers(target.Temporary), refreshTraits: false);
            context = context.WithCard(refreshed);
            if (shared.Count == 0) return context;
            CardCycleState cards = context.Cards;
            BonusDrawState? bonus = cards.BonusDraw == null ? null : new BonusDrawState(cards.BonusDraw.Counters,
                cards.BonusDraw.Listeners.Select(listener => new BonusDrawListener(listener.Key, Replace(listener.Upgrade))).ToArray());
            var next = cards.NextDrawUpgrades?.Select(rule => new NextDrawUpgradeRule(Replace(rule.Upgrade), rule.Filters)).ToArray();
            return new CombatContext(new CardCycleState(cards.Hand, cards.Draw, cards.Discard, cards.Rng, cards.DrawModifier,
                cards.ExternalInteractions, bonus, next, cards.NextUpgradeReferenceId, cards.DeckDrawDisabledThisTurn), context.BattleRng, context.Gold,
                context.NextCardId, context.MaxHandSize, context.StatusRules, context.Statistics,
                context.CardInstances?.Select(ReplaceCard).ToArray(), context.CardRegistry?.Select(ReplaceCard).ToArray(),
                context.AllScenarioBossesDead, context.NextAddedTemporaryUpgrades?.Select(Replace).ToArray(), context.OtherPiles,
                context.QueryFrame, context.KillCamActivated, context.MagicPower, context.IsolatedBattlePreview, context.EnergyState,
                context.RoomCapacities, context.AbilityCardCache, context.LastAbilityActivatorUnitId, context.PermanentlyDisabledAbilities,
                context.LastSpawnedUnitId, context.NextUnitId, context.SpawnPoints, context.SummonCatalog, context.Enchantments,
                context.PurifyBlockedTriggers, context.Relics, context.TriggerCounts, context.Resources, context.MixState);
        }

        private static CardUpgradeModifier WithValues(CardUpgradeModifier upgrade, int damage, int heal)
        {
            CardStatModifier original = upgrade.Stats;
            var stats = new CardStatModifier(damage, original.Health, original.Cost, heal, original.Size,
                original.XCost, original.EquipmentLimit, original.UpgradeSlotCount);
            return upgrade.WithStats(stats);
        }
    }
}

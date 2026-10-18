using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class ElixirRandomMixModel
    {
        public static CardGenerationResult Apply(CombatContext source, ElixirCatalog? catalog, int amount)
        {
            if (catalog == null || source.MixState == null || source.OtherPiles == null || source.CardInstances == null ||
                source.CardRegistry == null || amount < 0 || amount > 256)
                return Unsupported("Random mixing requires the elixir catalog, complete piles and a bounded upgrade count.");
            CardToken? current = source.Cards.Hand.FirstOrDefault(card => catalog.Recipes.Any(recipe => recipe.Creation.DataId == card.DataId));
            if (current == null && source.Cards.Hand.Count >= source.MaxHandSize)
                return new CardGenerationResult(source, Array.Empty<CardToken>());
            if (source.NextAddedTemporaryUpgrades?.Count > 0)
                return Unsupported("Random mixing with pending final-generation upgrades requires intermediate-copy calibration.");
            if (source.Relics?.Any(relic => relic.CardModifiers?.Count > 0 || relic.CardHealthModifiers?.Count > 0 ||
                relic.CardAttackModifiers?.Count > 0 || relic.MagicPowerModifiers?.Count > 0 || relic.CardTypeCosts?.Count > 0 ||
                relic.ResourceEffects?.Any(rule => rule.Type == RelicResourceModel.AdjustMixAmount) == true) == true)
                return Unsupported("Random mixing with intermediate-card relic modifiers or additional draft mixes requires calibration.");
            CombatContext context = source;
            CardGenerationResult generated;
            ElixirRecipe recipe;
            string? lastUpgrade = source.MixState.LastUpgradeDataId;
            if (current == null)
            {
                if (catalog.Recipes.Count <= 1)
                    return Unsupported("Random new-elixir selection requires a nonempty pool after excluding the previous recipe.");
                // With the original multi-recipe catalog this is the same one
                // recipe draw and base upgrade as GenerateNewElixir. Its base
                // upgrade does not write LastMixUpgradeSelected.
                generated = ElixirMixModel.Apply(context, catalog, 1, 0);
                if (!generated.Supported) return generated;
                context = generated.Context!;
                recipe = catalog.Recipes.Single(item => item.Creation.DataId == generated.AddedCards.Single().DataId);
                amount--;
            }
            else
            {
                recipe = catalog.Recipes.Single(item => item.Creation.DataId == current.DataId);
                CardPileState? standby = context.OtherPiles!.FirstOrDefault(pile => pile.Name == "Standby");
                if (standby == null) return Unsupported("Random mixing requires the Standby pile for its previous elixir.");
                context = context.WithCards(new CardCycleState(context.Cards.Hand.Where(card => card.InstanceId != current.InstanceId).ToArray(),
                    context.Cards.Draw, context.Cards.Discard, context.Cards.Rng, context.Cards.DrawModifier,
                    context.Cards.ExternalInteractions, context.Cards.BonusDraw, context.Cards.NextDrawUpgrades, context.Cards.NextUpgradeReferenceId));
                context = context.WithOtherPiles(context.OtherPiles.Select(pile => pile.Name == "Standby" ? CardPileModel.Add(pile, current) : pile).ToArray());
                generated = CardGenerationModel.Apply(context, new CardGenerationRule("HandPile", 1, new[] { recipe.Creation },
                    copyModifiers: true, deterministicPoolSelection: true), current.InstanceId);
                if (!generated.Supported) return generated;
                context = generated.Context!;
            }
            if (recipe.Upgrades.Count == 0 && amount > 0)
                return Unsupported("Random elixir upgrades require a nonempty original upgrade pool.");
            if (recipe.Upgrades.Any(upgrade => upgrade.RemoveOnDiscard || upgrade.Unique || upgrade.ExcludeFromClones ||
                upgrade.Lifecycle?.AddedTraits.Count > 0 || upgrade.Lifecycle?.RemovedRuntimeTypes.Count > 0))
                return Unsupported("Random elixir upgrades with copy flags or trait callbacks require intermediate-copy calibration.");
            CardInstanceState completed = context.FindCard(generated.AddedCards.Single().InstanceId)!;
            for (int index = 0; index < amount; index++)
            {
                RngDraw draw = context.BattleRng.Range(0, recipe.Upgrades.Count);
                context = context.WithBattleRng(draw.State);
                CardUpgradeModifier upgrade = recipe.Upgrades[draw.Value];
                if (upgrade.ExternalInteractions.Count > 0) return Unsupported(string.Join("; ", upgrade.ExternalInteractions));
                context = CardMagicPowerModel.Refresh(context, completed.WithPermanentUpgrade(upgrade.WithSharedReference(null), false), out completed);
                lastUpgrade = upgrade.DataId;
            }
            if (completed.MaskDescriptor != null)
            {
                // MergeUpgradeTriggers groups equal upgrade definitions in
                // first-occurrence order, while retaining every numeric upgrade.
                string[] castEffects = recipe.Creation.MaskDescriptor!.AuthoredCastEffects.Concat(completed.Permanent.Upgrades
                    .GroupBy(upgrade => upgrade.DataId, StringComparer.Ordinal).Select(group => group.First())
                    .SelectMany(upgrade => upgrade.Lifecycle?.Triggers ?? Array.Empty<CardLifecycleTrigger>())
                    .Where(trigger => trigger.Type == "OnCast").SelectMany(trigger => trigger.Effects)).ToArray();
                completed = completed.WithMaskDescriptor(completed.MaskDescriptor.WithAuthoredCastEffects(castEffects));
            }
            return new CardGenerationResult(context.WithCard(completed).WithMixState(new MixRuntimeState(
                current == null ? recipe.Creation.DataId : source.MixState.LastCardDataId, lastUpgrade)), generated.AddedCards);
        }
        private static CardGenerationResult Unsupported(string error) => new(null, Array.Empty<CardToken>(), error);
    }
}

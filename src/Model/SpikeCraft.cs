using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class SpikeCraftModel
    {
        public static ElixirDraftOptions Draft(ElixirRecipe recipe, UnityRng rng)
        {
            rng = rng.Range(0, 1).State; // The native one-member spike pool still draws.
            var available = recipe.Upgrades.ToList();
            var offers = new List<ElixirDraftOption>();
            for (int index = 0; index < 2; index++)
            {
                CardUpgradeModifier? upgrade = recipe.BaseUpgrade;
                if (upgrade == null && available.Count > 0)
                {
                    RngDraw draw = rng.Range(0, available.Count); rng = draw.State;
                    upgrade = available[draw.Value]; available.RemoveAt(draw.Value);
                }
                offers.Add(new ElixirDraftOption(recipe, upgrade));
            }
            return new ElixirDraftOptions(offers, rng);
        }
        public static IEnumerable<PlayCardAction> Actions(CardPlayRule rule, PlayCardAction action)
        {
            int index = rule.Effects.ToList().FindIndex(effect => effect.Type == "CraftSpike");
            if (index < 0) { yield return action; yield break; }
            for (int choice = 0; choice < 2; choice++)
                yield return new PlayCardAction(action.CardInstanceId, action.RoomIndex, action.PlayerPosition,
                    action.TargetUnitId, action.ActivatorUnitId, action.TargetSpawnPointIndex, action.RecursionSelections,
                    new[] { new ElixirMixSelection(index, 0, choice) }, action.ActivatorRoomIndex);
        }
        public static CardGenerationResult Apply(CombatContext source, CardActionEffect effect, int choice)
        {
            if (effect.SpikeRecipe == null || choice < 0 || choice > 1)
                return new(null, Array.Empty<CardToken>(), "Spike crafting requires one of its two captured draft selections.");
            ElixirDraftOptions draft = Draft(effect.SpikeRecipe, source.BattleRng);
            ElixirDraftOption offer = draft.Offers[choice];
            CardGenerationResult generated = CardGenerationModel.Apply(source.WithBattleRng(draft.Rng),
                new CardGenerationRule("HandPile", 1, new[] { offer.Recipe.Creation }, upgrade: offer.Upgrade,
                    upgradeIsPermanent: true, deterministicPoolSelection: true, permanent: false));
            if (!generated.Supported || generated.AddedCards.Count == 0) return generated;
            CombatContext context = generated.Context!;
            CardInstanceState result = context.FindCard(generated.AddedCards[0].InstanceId)!;
            if (result.MaskDescriptor != null)
            {
                string[] castEffects = result.MaskDescriptor.AuthoredCastEffects.Concat(result.Permanent.Upgrades
                    .GroupBy(upgrade => upgrade.DataId, StringComparer.Ordinal).Select(group => group.First())
                    .SelectMany(upgrade => upgrade.Lifecycle?.Triggers ?? Array.Empty<CardLifecycleTrigger>())
                    .Where(trigger => trigger.Type == "OnCast").SelectMany(trigger => trigger.Effects)).ToArray();
                context = context.WithCard(result.WithMaskDescriptor(result.MaskDescriptor.WithAuthoredCastEffects(castEffects)));
            }
            return new(context, generated.AddedCards);
        }
    }
}

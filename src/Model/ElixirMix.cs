using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class ElixirRecipe
    {
        public CardCreationRule Creation { get; }
        public CardUpgradeModifier? BaseUpgrade { get; }
        public IReadOnlyList<CardUpgradeModifier> Upgrades { get; }

        public ElixirRecipe(CardCreationRule creation, CardUpgradeModifier? baseUpgrade,
            IReadOnlyList<CardUpgradeModifier> upgrades)
        {
            Creation = creation;
            BaseUpgrade = baseUpgrade;
            Upgrades = Array.AsReadOnly(upgrades.ToArray());
        }
    }

    public sealed class ElixirCatalog
    {
        public IReadOnlyList<ElixirRecipe> Recipes { get; }

        public ElixirCatalog(IReadOnlyList<ElixirRecipe> recipes) =>
            Recipes = Array.AsReadOnly(recipes.ToArray());
    }

    public sealed class MixRuntimeState
    {
        public string? LastCardDataId { get; }
        public string? LastUpgradeDataId { get; }

        public MixRuntimeState(string? lastCardDataId, string? lastUpgradeDataId)
        {
            LastCardDataId = lastCardDataId;
            LastUpgradeDataId = lastUpgradeDataId;
        }
    }

    public sealed class ElixirDraftOption
    {
        public ElixirRecipe Recipe { get; }
        public CardUpgradeModifier? Upgrade { get; }

        public ElixirDraftOption(ElixirRecipe recipe, CardUpgradeModifier? upgrade)
        { Recipe = recipe; Upgrade = upgrade; }
    }

    public sealed class ElixirDraftOptions
    {
        public IReadOnlyList<ElixirDraftOption> Offers { get; }
        public UnityRng Rng { get; }

        public ElixirDraftOptions(IReadOnlyList<ElixirDraftOption> offers, UnityRng rng)
        { Offers = Array.AsReadOnly(offers.ToArray()); Rng = rng; }
    }

    public static class ElixirMixModel
    {
        public static ElixirDraftOptions NewDraft(ElixirCatalog catalog, UnityRng rng, int amount,
            string? lastCardDataId)
        {
            if (amount < 1) throw new ArgumentOutOfRangeException(nameof(amount));
            var candidates = catalog.Recipes.ToList();
            if (candidates.Count > amount)
            {
                int prior = candidates.FindIndex(recipe => recipe.Creation.DataId == lastCardDataId);
                if (prior >= 0) candidates.RemoveAt(prior);
            }
            var selected = new List<ElixirRecipe>();
            while (candidates.Count > 0 && selected.Count < amount)
            {
                RngDraw draw = rng.Range(0, candidates.Count);
                rng = draw.State;
                selected.Add(candidates[draw.Value]);
                candidates.RemoveAt(draw.Value);
            }
            var usedUpgrades = new HashSet<string>(StringComparer.Ordinal);
            var offers = new List<ElixirDraftOption>();
            foreach (ElixirRecipe recipe in selected)
            {
                CardUpgradeModifier? upgrade = recipe.BaseUpgrade;
                if (upgrade == null)
                {
                    CardUpgradeModifier[] available = recipe.Upgrades
                        .Where(item => !usedUpgrades.Contains(item.DataId)).ToArray();
                    if (available.Length > 0)
                    {
                        RngDraw draw = rng.Range(0, available.Length);
                        rng = draw.State;
                        upgrade = available[draw.Value];
                        usedUpgrades.Add(upgrade.DataId);
                    }
                }
                offers.Add(new ElixirDraftOption(recipe, upgrade));
            }
            return new ElixirDraftOptions(offers, rng);
        }

        public static ElixirDraftOptions UpgradeDraft(ElixirRecipe recipe, UnityRng rng, int amount)
        {
            if (amount < 1) throw new ArgumentOutOfRangeException(nameof(amount));
            var candidates = recipe.Upgrades.ToList();
            var offers = new List<ElixirDraftOption>();
            while (candidates.Count > 0 && offers.Count < amount)
            {
                RngDraw draw = rng.Range(0, candidates.Count);
                rng = draw.State;
                CardUpgradeModifier upgrade = candidates[draw.Value];
                offers.Add(new ElixirDraftOption(recipe, upgrade));
                candidates.RemoveAt(draw.Value);
            }
            return new ElixirDraftOptions(offers, rng);
        }
    }
}

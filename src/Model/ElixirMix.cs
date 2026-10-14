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
}

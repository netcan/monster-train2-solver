using System.Linq;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class ElixirCatalogProbe
    {
        internal static ElixirCatalog Capture(AllGameManagers managers)
        {
            ElixirConfigurationData configuration = managers.GetAllGameData().GetBalanceData().GetElixirConfigurationData();
            return new ElixirCatalog(configuration.Elixirs.Select(elixir => new ElixirRecipe(
                CardGenerationProbe.Creation(elixir.Elixir),
                elixir.BaseUpgrade == null ? null : Upgrade(elixir.BaseUpgrade),
                elixir.Upgrades.Select(Upgrade).ToArray())).ToArray());
        }

        private static CardUpgradeModifier Upgrade(CardUpgradeData data)
        {
            var state = new CardUpgradeState();
            state.Setup(data);
            return CardModifierProbe.Upgrade(state);
        }
    }
}

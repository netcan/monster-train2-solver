using System;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class UnitUpgradeScalingProbe
    {
        internal static bool Known(string name) => name == "CardTraitScalingUpgradeUnitAttack" || name == "CardTraitScalingUpgradeUnitHealth";
        internal static ScalingUnitUpgradeTrait[]? Capture(CardState card)
        {
            ScalingUnitUpgradeTrait[] traits = card.GetTraitStates().Where(trait => Known(trait.GetType().Name)).Select(trait =>
                Definition(trait, (int)AccessTools.Field(typeof(CardState), "cost").GetValue(card), card.IsConsumeRemainingEnergyCostType())).ToArray();
            return traits.Length == 0 ? null : traits;
        }
        internal static ScalingUnitUpgradeTrait[]? Creation(CardData card)
        {
            ScalingUnitUpgradeTrait[] traits = card.GetTraits().Where(data => Known(data.GetTraitStateName())).Select(data =>
            {
                var trait = (CardTraitState)Activator.CreateInstance(typeof(CardTraitState).Assembly.GetType(data.GetTraitStateName())!)!;
                trait.Setup(data, CardState.None); return Definition(trait, card.GetCost(), card.GetCostType() == CardData.CostType.ConsumeRemainingEnergy);
            }).ToArray();
            return traits.Length == 0 ? null : traits;
        }
        private static ScalingUnitUpgradeTrait Definition(CardTraitState trait, int rawCost, bool variable) => new ScalingUnitUpgradeTrait(
            DamageScalingProbe.Query(trait.StatValueData, rawCost, variable), trait is CardTraitScalingUpgradeUnitAttack ? "Damage" : "Health",
            trait.GetParamInt(), trait.GetParamInt3());
    }
}

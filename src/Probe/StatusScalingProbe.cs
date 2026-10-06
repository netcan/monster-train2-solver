using System;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class StatusScalingProbe
    {
        internal static ScalingStatusTrait[]? Capture(CardState card)
        {
            ScalingStatusTrait[] traits = card.GetTraitStates().OfType<CardTraitScalingAddStatusEffect>()
                .Select(trait => Definition(trait, (int)AccessTools.Field(typeof(CardState), "cost").GetValue(card), card.IsConsumeRemainingEnergyCostType())).ToArray();
            return traits.Length == 0 ? null : traits;
        }
        internal static ScalingStatusTrait[]? Creation(CardData card)
        {
            ScalingStatusTrait[] traits = card.GetTraits().Where(data => data.GetTraitStateName() == "CardTraitScalingAddStatusEffect").Select(data =>
            {
                var trait = new CardTraitScalingAddStatusEffect(); trait.Setup(data, CardState.None);
                return Definition(trait, card.GetCost(), card.GetCostType() == CardData.CostType.ConsumeRemainingEnergy);
            }).ToArray();
            return traits.Length == 0 ? null : traits;
        }
        private static ScalingStatusTrait Definition(CardTraitScalingAddStatusEffect trait, int rawCost, bool variable)
        {
            var definitions = AllGameManagers.Instance!.GetStatusEffectManager().GetAllStatusEffectsData().GetStatusEffectData();
            return new ScalingStatusTrait(DamageScalingProbe.Query(trait.StatValueData, rawCost, variable), trait.GetParamInt(), trait.GetParamBool(),
                trait.GetParamInt2(), trait.GetParamStatusEffects().Select(status => status.statusId).ToArray(),
                definitions.Where(status => status.IsPropagatable() && !status.GetExcludeHeroPropagation()).Select(status => status.GetStatusId()).ToArray(),
                definitions.Where(status => status.IsPropagatable() && !status.GetExcludeMonsterPropagation()).Select(status => status.GetStatusId()).ToArray());
        }
    }
}

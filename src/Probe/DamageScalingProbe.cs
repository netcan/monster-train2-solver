using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class DamageScalingProbe
    {
        internal static bool Known(string name) => name == "CardTraitSelfPurge" || name == "CardTraitScalingAddDamage" || name == "CardTraitScalingAddStatusEffect" || UnitUpgradeScalingProbe.Known(name);
        internal static ScalingDamageTrait[]? Capture(CardState card)
        {
            ScalingDamageTrait[] traits = card.GetTraitStates().OfType<CardTraitScalingAddDamage>().Select(trait =>
                new ScalingDamageTrait(Query(trait.StatValueData, (int)AccessTools.Field(typeof(CardState), "cost").GetValue(card),
                    card.IsConsumeRemainingEnergyCostType()), trait.GetParamInt(), trait.GetParamFloat(), trait.GetParamBool())).ToArray();
            return traits.Length == 0 ? null : traits;
        }
        internal static ScalingDamageTrait[]? Creation(CardData card)
        {
            ScalingDamageTrait[] traits = card.GetTraits().Where(data => data.GetTraitStateName() == "CardTraitScalingAddDamage").Select(data =>
            {
                // Setup only copies immutable parameters; it does not register or notify the game.
                var trait = new CardTraitScalingAddDamage(); trait.Setup(data, CardState.None);
                return new ScalingDamageTrait(Query(trait.StatValueData, card.GetCost(), card.GetCostType() == CardData.CostType.ConsumeRemainingEnergy),
                    trait.GetParamInt(), trait.GetParamFloat(), trait.GetParamBool());
            }).ToArray();
            return traits.Length == 0 ? null : traits;
        }
        internal static CardStatisticQuery Query(CardStatistics.StatValueData data, int rawCost, bool variable)
        {
            // Masks cover every static definition, including cards that future effects may generate.
            IReadOnlyList<CardData> definitions = AllGameManagers.Instance!.GetSaveManager().GetAllGameData().GetAllCardData();
            string[]? typeMask = data.cardTypeTarget == CardStatistics.CardTypeTarget.Any ? null : definitions
                .Where(card => card.GetCardType().ToString() == data.cardTypeTarget.ToString()).Select(card => card.GetID()).ToArray();
            string[] subtypeMask = data.paramSubtype == null ? Array.Empty<string>() : definitions.Where(card =>
                card.GetSpawnCharacterData()?.GetSubtypes().Contains(data.paramSubtype) == true).Select(card => card.GetID()).ToArray();
            return new CardStatisticQuery(data.trackedValue.ToString(), data.entryDuration.ToString(), typeMask, subtypeMask,
                data.paramSubtype == null || data.paramSubtype.IsNone, data.paramSubtype?.Key, data.paramCardData?.GetID(), rawCost, variable);
        }
    }
}

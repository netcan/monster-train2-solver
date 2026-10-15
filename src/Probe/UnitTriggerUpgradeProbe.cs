using System;
using System.Collections.Generic;
using System.Linq;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class UnitTriggerUpgradeProbe
    {
        internal static bool Known(string type) => type == "CardEffectAddCardUpgradeToUnits" ||
            type == "CardEffectAddTempCardUpgradeToUnits" || type == "CardEffectRemoveTempUpgradeFromUnit" || type == "CardEffectBuffEquipment";

        internal static CardActionEffect? Capture(CardEffectState effect, List<string> interactions) =>
            Definition(effect.GetSourceCardEffectData(), interactions);

        internal static CardActionEffect? Definition(CardEffectData effect, List<string> interactions)
        {
            if (!Known(effect.GetEffectStateName())) return null;
            if (effect.GetUseIntRange() || effect.GetCopyModifiersFromSource() || effect.GetFilterBasedOnMainSubClass() ||
                effect.GetUseStatusEffectStackMultiplier() || effect.GetUseHealthMissingStackMultiplier() ||
                effect.GetUseMagicPowerMultiplier() || !effect.GetParamSubtype().IsNone)
                interactions.Add("Unmodeled triggered unit upgrade parameters");
            bool equipment = effect.GetEffectStateName() == "CardEffectBuffEquipment";
            bool scaleUpgrade = (effect.GetEffectStateName() == "CardEffectAddCardUpgradeToUnits" || equipment) && effect.GetParamBool();
            CardUpgradeModifier? upgrade = null;
            if (effect.GetParamCardUpgradeData() == null) interactions.Add("Missing triggered unit upgrade definition");
            else
            {
                CardUpgradeData data = effect.GetParamCardUpgradeData();
                if (scaleUpgrade && !data.CanScaleUpgradeToSingleInstance())
                    interactions.Add("Invalid scaled triggered unit upgrade definition");
                var state = new CardUpgradeState(); state.Setup(data);
                bool filtersIgnoredByEffect = effect.GetEffectStateName() == "CardEffectAddCardUpgradeToUnits" || equipment;
                upgrade = CardModifierProbe.Upgrade(state, false, filtersAlreadyApplied: filtersIgnoredByEffect);
                interactions.AddRange(upgrade.ExternalInteractions);
            }
            var excluded = new List<SubtypeData>(); effect.GetTargetCharacterExcludedSubtypes(excluded);
            string type = equipment ? "BuffEquipment" : effect.GetEffectStateName() == "CardEffectRemoveTempUpgradeFromUnit" ? "RemoveUnitUpgrade" : "UnitUpgrade";
            return new CardActionEffect(type, effect.GetTargetMode().ToString(), effect.GetParamInt(),
                effect.GetTargetTeamType().HasFlag(Team.Type.Heroes), effect.GetTargetTeamType().HasFlag(Team.Type.Monsters),
                Array.Empty<CombatStatus>(), upgrade, equipment ? "TemporaryUntilUnitDeath" : ((UnitUpgradeLifetime)effect.GetAdditionalParamInt1()).ToString(),
                new CardEffectTests(effect.GetShouldTest(), effect.GetShouldFailToCastIfTestFails(),
                    effect.GetShouldCancelSubsequentEffectsIfTestFails(), false, true), filters:
                new CardTargetFilters(effect.GetTargetModeHealthFilter().ToString(), effect.GetTargetModeStatusEffectsFilter(),
                    effect.GetTargetModeStatusEffectsExcludedFilter(), effect.GetTargetIgnoreBosses(),
                    effect.GetTargetCharacterSubtype().IsNone ? "" : effect.GetTargetCharacterSubtype().Key,
                    excluded.Select(subtype => subtype.IsNone ? "" : subtype.Key).ToArray()), scaleUpgrade: scaleUpgrade);
        }
    }
}

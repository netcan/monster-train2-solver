using System;
using System.Collections.Generic;
using System.Linq;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class UnitTriggerUpgradeProbe
    {
        internal static bool Known(string type) => type == "CardEffectAddCardUpgradeToUnits" ||
            type == "CardEffectAddTempCardUpgradeToUnits" || type == "CardEffectRemoveTempUpgradeFromUnit";

        internal static CardActionEffect? Capture(CardEffectState effect, List<string> interactions) =>
            Definition(effect.GetSourceCardEffectData(), interactions);

        internal static CardActionEffect? Definition(CardEffectData effect, List<string> interactions)
        {
            if (!Known(effect.GetEffectStateName())) return null;
            if (effect.GetUseIntRange() || effect.GetCopyModifiersFromSource() || effect.GetFilterBasedOnMainSubClass() ||
                effect.GetUseStatusEffectStackMultiplier() || effect.GetUseHealthMissingStackMultiplier() ||
                effect.GetUseMagicPowerMultiplier() || !effect.GetParamSubtype().IsNone)
                interactions.Add("Unmodeled triggered unit upgrade parameters");
            if (effect.GetEffectStateName() == "CardEffectAddCardUpgradeToUnits" && effect.GetParamBool())
                interactions.Add("Triggered single-instance upgrade accumulation");
            CardUpgradeModifier? upgrade = null;
            if (effect.GetParamCardUpgradeData() == null) interactions.Add("Missing triggered unit upgrade definition");
            else
            {
                var state = new CardUpgradeState(); state.Setup(effect.GetParamCardUpgradeData());
                upgrade = CardModifierProbe.Upgrade(state); interactions.AddRange(upgrade.ExternalInteractions);
            }
            var excluded = new List<SubtypeData>(); effect.GetTargetCharacterExcludedSubtypes(excluded);
            string type = effect.GetEffectStateName() == "CardEffectRemoveTempUpgradeFromUnit" ? "RemoveUnitUpgrade" : "UnitUpgrade";
            return new CardActionEffect(type, effect.GetTargetMode().ToString(), effect.GetParamInt(),
                effect.GetTargetTeamType().HasFlag(Team.Type.Heroes), effect.GetTargetTeamType().HasFlag(Team.Type.Monsters),
                Array.Empty<CombatStatus>(), upgrade, ((UnitUpgradeLifetime)effect.GetAdditionalParamInt1()).ToString(),
                new CardEffectTests(effect.GetShouldTest(), effect.GetShouldFailToCastIfTestFails(),
                    effect.GetShouldCancelSubsequentEffectsIfTestFails(), false, true), filters:
                new CardTargetFilters(effect.GetTargetModeHealthFilter().ToString(), effect.GetTargetModeStatusEffectsFilter(),
                    effect.GetTargetModeStatusEffectsExcludedFilter(), effect.GetTargetIgnoreBosses(),
                    effect.GetTargetCharacterSubtype().IsNone ? "" : effect.GetTargetCharacterSubtype().Key,
                    excluded.Select(subtype => subtype.IsNone ? "" : subtype.Key).ToArray()));
        }
    }
}

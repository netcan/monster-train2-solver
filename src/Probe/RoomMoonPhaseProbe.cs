using System;
using System.Collections.Generic;
using System.Linq;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RoomMoonPhaseProbe
    {
        internal static RoomMoonPhaseRule? Rule(CardUpgradeData upgrade, List<string> interactions)
        {
            RoomModifierData[] modifiers = upgrade.GetRoomModifierUpgrades().ToArray();
            RoomModifierData? moon = modifiers.SingleOrDefault(modifier => modifier.GetRoomStateModifierClassName() == "RoomStateNewMoonAlwaysActiveModifier");
            if (moon == null) return null;
            RoomModifierData? post = modifiers.SingleOrDefault(modifier => modifier.GetRoomStateModifierClassName() == "RoomStateAddEffectPostCombatModifier");
            CardData? support = moon.GetParamCardData();
            if (modifiers.Length != 2 || post == null || support?.GetEffects().Count != 1 ||
                support.GetEffects()[0].GetEffectStateName() != "CardEffectUpdateMoonPhaseResource" || support.GetEffects()[0].GetParamBool())
            { interactions.Add("Unmodeled moon room modifier composition or support card"); return null; }
            var rule = new RoomMoonPhaseRule(1, support.GetID(), post.GetParamCardEffectData()
                .Select(effect => BattleActionProbe.DescribeTriggerEffect(effect, interactions)).ToArray(), post.DisallowedInDeploymentPhase);
            string? error = RoomMoonPhaseModel.Validate(rule);
            if (error != null) interactions.Add(error);
            return rule;
        }
    }
}

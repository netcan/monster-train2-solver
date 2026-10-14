using System;
using System.Collections.Generic;
using System.Linq;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class UnitTriggerActionProbe
    {
        private static bool StatusEffect(string type) =>
            type is "CardEffectAddStatusEffect" or "CardEffectAddStatusEffectIfNoPlayerUnits" or
                "CardEffectAddStatusEffectPerSelfAttack";
        private static bool DamageEffect(string type) =>
            type is "CardEffectDamage" or "CardEffectPyreDamageIfNoPlayerUnits";

        internal static CardActionEffect? Capture(CardEffectState state)
        {
            if (AbilityLifecycleProbe.Known(state.GetCardEffect().GetType().Name)) return AbilityLifecycleProbe.Capture(state);
            if (AbilityCooldownProbe.Known(state.GetCardEffect().GetType().Name)) return AbilityCooldownProbe.Describe(state.GetSourceCardEffectData(), state.GetParamInt(), state.GetParamBool());
            if (!(state.GetCardEffect() is CardEffectNULL) && !(state.GetCardEffect() is CardEffectHeal) && !(state.GetCardEffect() is CardEffectDamage) && !(state.GetCardEffect() is CardEffectAddStatusEffect) &&
                !EnergyModel.IsNativeEffect(state.GetCardEffect().GetType().Name) && !(state.GetCardEffect() is CardEffectDrawAdditionalNextTurn) &&
                !(state.GetCardEffect() is CardEffectAdjustRoomCapacity) && !(state.GetCardEffect() is CardEffectAdjustForgePoints) &&
                !(state.GetCardEffect() is CardEffectAdvanceMoonPhase)) return null;
            CardEffectData effect = state.GetSourceCardEffectData();
            return Describe(effect, state.GetParamInt(), state.GetUseIntRange() ?
                new CardEffectRange(state.GetParamMinInt(), state.GetParamMaxInt(), state.GetParamMultiplier()) : null,
                state.GetCardEffect() is CardEffectAddStatusEffect ? state.GetParamStatusEffectStackData().Select(status =>
                    BattleActionProbe.Status(status.statusId, status.count)).ToArray() : null);
        }
        internal static CardActionEffect? Definition(CardEffectData effect) =>
            AbilityLifecycleProbe.Known(effect.GetEffectStateName()) ? AbilityLifecycleProbe.Describe(effect) :
            AbilityCooldownProbe.Known(effect.GetEffectStateName()) ? AbilityCooldownProbe.Describe(effect, effect.GetParamInt(), effect.GetParamBool()) :
            effect.GetEffectStateName() != "CardEffectNULL" && effect.GetEffectStateName() != "CardEffectHeal" && !DamageEffect(effect.GetEffectStateName()) && !StatusEffect(effect.GetEffectStateName()) &&
                !EnergyModel.IsNativeEffect(effect.GetEffectStateName()) && effect.GetEffectStateName() != "CardEffectDrawAdditionalNextTurn" && effect.GetEffectStateName() != "CardEffectAdjustRoomCapacity" && effect.GetEffectStateName() != "CardEffectAdjustForgePoints" &&
                effect.GetEffectStateName() != "CardEffectAdvanceMoonPhase" ? null :
            // Effect states have no parent card; native damage/heal getters still clamp endpoints.
            Describe(effect, Numeric(effect, effect.GetParamInt()), effect.GetUseIntRange() ?
                new CardEffectRange(Numeric(effect, effect.GetParamMinInt()), Numeric(effect, effect.GetParamMaxInt()), effect.GetParamMultiplier()) : null);
        internal static TriggeredStatusScaling? Scaling(CardEffectState effect) => effect.GetCardEffect() is CardEffectAddStatusEffect ?
            new TriggeredStatusScaling(effect.GetUseStatusEffectStackMultiplier() ? effect.GetStatusEffectStackMultiplier() : null,
                effect.GetUseHealthMissingStackMultiplier(), effect.GetUseMagicPowerMultiplier(), effect.GetParamBool2(),
                effect.GetParamSubtype().IsNone ? "" : effect.GetParamSubtype().Key) : null;
        internal static TriggeredStatusScaling? Scaling(CardEffectData effect) => StatusEffect(effect.GetEffectStateName()) ?
            new TriggeredStatusScaling(effect.GetUseStatusEffectStackMultiplier() ? effect.GetStatusEffectStackMultiplier() : null,
                effect.GetUseHealthMissingStackMultiplier(), effect.GetUseMagicPowerMultiplier(), effect.GetParamBool2(),
                effect.GetParamSubtype().IsNone ? "" : effect.GetParamSubtype().Key) : null;
        private static int Numeric(CardEffectData effect, int value) => effect.GetEffectStateName() == "CardEffectNULL" || StatusEffect(effect.GetEffectStateName()) || effect.GetEffectStateName() == "CardEffectAdjustForgePoints" ||
            EnergyModel.IsNativeEffect(effect.GetEffectStateName()) || (effect.GetEffectStateName() == "CardEffectDrawAdditionalNextTurn" || effect.GetEffectStateName() == "CardEffectAdjustRoomCapacity" ||
                effect.GetEffectStateName() == "CardEffectAdvanceMoonPhase") ? value : Math.Max(0, value);
        private static CardActionEffect Describe(CardEffectData effect, int value, CardEffectRange? range, CombatStatus[]? statuses = null)
        {
            var excluded = new List<SubtypeData>(); effect.GetTargetCharacterExcludedSubtypes(excluded);
            bool energy = EnergyModel.IsNativeEffect(effect.GetEffectStateName());
            bool capacity = effect.GetEffectStateName() == "CardEffectAdjustRoomCapacity";
            bool drawNext = effect.GetEffectStateName() == "CardEffectDrawAdditionalNextTurn";
            bool forgePoints = effect.GetEffectStateName() == "CardEffectAdjustForgePoints";
            bool moonAdvance = effect.GetEffectStateName() == "CardEffectAdvanceMoonPhase";
            bool nullEffect = effect.GetEffectStateName() == "CardEffectNULL";
            return new CardActionEffect(nullEffect ? "Null" : capacity ? "AdjustCapacity" : drawNext ? "DrawNextTurn" : forgePoints ? ForgePointsModel.Effect : moonAdvance ? "AdvanceMoonPhase" : energy ? EnergyEffectProbe.Type(effect) : DamageEffect(effect.GetEffectStateName()) ? "Damage" : effect.GetEffectStateName() == "CardEffectHeal" ? "Heal" : "AddStatus", effect.GetTargetMode().ToString(), value,
                effect.GetTargetTeamType().HasFlag(Team.Type.Heroes), effect.GetTargetTeamType().HasFlag(Team.Type.Monsters),
                statuses ?? (StatusEffect(effect.GetEffectStateName()) ? effect.GetParamStatusEffects().Select(status => BattleActionProbe.Status(status.statusId, status.count)).ToArray() : Array.Empty<CombatStatus>()),
                upgrade: drawNext ? BonusDrawProbe.Upgrade(effect) : null, tests: new CardEffectTests(effect.GetShouldTest(), effect.GetShouldFailToCastIfTestFails(),
                    effect.GetShouldCancelSubsequentEffectsIfTestFails(), StatusEffect(effect.GetEffectStateName()) && effect.GetParamBool(), forgePoints ? true : energy || drawNext || capacity ? false : (bool?)null),
                range: range,
                filters: new CardTargetFilters(effect.GetTargetModeHealthFilter().ToString(), effect.GetTargetModeStatusEffectsFilter(),
                    effect.GetTargetModeStatusEffectsExcludedFilter(), effect.GetTargetIgnoreBosses(),
                effect.GetTargetCharacterSubtype().IsNone ? "" : effect.GetTargetCharacterSubtype().Key,
                    excluded.Select(subtype => subtype.IsNone ? "" : subtype.Key).ToArray()), onlyIfNoEnemies: capacity && effect.GetParamBool(),
                nullTargetless: nullEffect && effect.GetParamBool2(), nullForwardSelf: nullEffect && effect.GetAdditionalParamInt() == 1);
        }
    }
}

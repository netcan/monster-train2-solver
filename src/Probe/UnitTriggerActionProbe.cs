using System;
using System.Collections.Generic;
using System.Linq;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class UnitTriggerActionProbe
    {
        internal static CardActionEffect? Capture(CardEffectState state)
        {
            if (!(state.GetCardEffect() is CardEffectHeal) && !(state.GetCardEffect() is CardEffectDamage) && !(state.GetCardEffect() is CardEffectAddStatusEffect) &&
                !EnergyModel.IsNativeEffect(state.GetCardEffect().GetType().Name) && !(state.GetCardEffect() is CardEffectDrawAdditionalNextTurn)) return null;
            CardEffectData effect = state.GetSourceCardEffectData();
            return Describe(effect, state.GetParamInt(), state.GetUseIntRange() ?
                new CardEffectRange(state.GetParamMinInt(), state.GetParamMaxInt(), state.GetParamMultiplier()) : null,
                state.GetCardEffect() is CardEffectAddStatusEffect ? state.GetParamStatusEffectStackData().Select(status =>
                    BattleActionProbe.Status(status.statusId, status.count)).ToArray() : null);
        }
        internal static CardActionEffect? Definition(CardEffectData effect) =>
            effect.GetEffectStateName() != "CardEffectHeal" && effect.GetEffectStateName() != "CardEffectDamage" && effect.GetEffectStateName() != "CardEffectAddStatusEffect" &&
                !EnergyModel.IsNativeEffect(effect.GetEffectStateName()) && effect.GetEffectStateName() != "CardEffectDrawAdditionalNextTurn" ? null :
            // Effect states have no parent card; native damage/heal getters still clamp endpoints.
            Describe(effect, Numeric(effect, effect.GetParamInt()), effect.GetUseIntRange() ?
                new CardEffectRange(Numeric(effect, effect.GetParamMinInt()), Numeric(effect, effect.GetParamMaxInt()), effect.GetParamMultiplier()) : null);
        internal static TriggeredStatusScaling? Scaling(CardEffectState effect) => effect.GetCardEffect() is CardEffectAddStatusEffect ?
            new TriggeredStatusScaling(effect.GetUseStatusEffectStackMultiplier() ? effect.GetStatusEffectStackMultiplier() : null,
                effect.GetUseHealthMissingStackMultiplier(), effect.GetUseMagicPowerMultiplier(), effect.GetParamBool2(),
                effect.GetParamSubtype().IsNone ? "" : effect.GetParamSubtype().Key) : null;
        internal static TriggeredStatusScaling? Scaling(CardEffectData effect) => effect.GetEffectStateName() == "CardEffectAddStatusEffect" ?
            new TriggeredStatusScaling(effect.GetUseStatusEffectStackMultiplier() ? effect.GetStatusEffectStackMultiplier() : null,
                effect.GetUseHealthMissingStackMultiplier(), effect.GetUseMagicPowerMultiplier(), effect.GetParamBool2(),
                effect.GetParamSubtype().IsNone ? "" : effect.GetParamSubtype().Key) : null;
        private static int Numeric(CardEffectData effect, int value) => effect.GetEffectStateName() == "CardEffectAddStatusEffect" ||
            EnergyModel.IsNativeEffect(effect.GetEffectStateName()) || effect.GetEffectStateName() == "CardEffectDrawAdditionalNextTurn" ? value : Math.Max(0, value);
        private static CardActionEffect Describe(CardEffectData effect, int value, CardEffectRange? range, CombatStatus[]? statuses = null)
        {
            var excluded = new List<SubtypeData>(); effect.GetTargetCharacterExcludedSubtypes(excluded);
            bool energy = EnergyModel.IsNativeEffect(effect.GetEffectStateName());
            bool drawNext = effect.GetEffectStateName() == "CardEffectDrawAdditionalNextTurn";
            return new CardActionEffect(drawNext ? "DrawNextTurn" : energy ? EnergyEffectProbe.Type(effect) : effect.GetEffectStateName() == "CardEffectDamage" ? "Damage" : effect.GetEffectStateName() == "CardEffectHeal" ? "Heal" : "AddStatus", effect.GetTargetMode().ToString(), value,
                effect.GetTargetTeamType().HasFlag(Team.Type.Heroes), effect.GetTargetTeamType().HasFlag(Team.Type.Monsters),
                statuses ?? (effect.GetEffectStateName() == "CardEffectAddStatusEffect" ? effect.GetParamStatusEffects().Select(status => BattleActionProbe.Status(status.statusId, status.count)).ToArray() : Array.Empty<CombatStatus>()),
                upgrade: drawNext ? BonusDrawProbe.Upgrade(effect) : null, tests: new CardEffectTests(effect.GetShouldTest(), effect.GetShouldFailToCastIfTestFails(),
                    effect.GetShouldCancelSubsequentEffectsIfTestFails(), effect.GetEffectStateName() == "CardEffectAddStatusEffect" && effect.GetParamBool(), energy || drawNext ? false : (bool?)null),
                range: range,
                filters: new CardTargetFilters(effect.GetTargetModeHealthFilter().ToString(), effect.GetTargetModeStatusEffectsFilter(),
                    effect.GetTargetModeStatusEffectsExcludedFilter(), effect.GetTargetIgnoreBosses(),
                    effect.GetTargetCharacterSubtype().IsNone ? "" : effect.GetTargetCharacterSubtype().Key,
                    excluded.Select(subtype => subtype.IsNone ? "" : subtype.Key).ToArray()));
        }
    }
}

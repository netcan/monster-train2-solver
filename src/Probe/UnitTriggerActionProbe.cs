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
            if (!(state.GetCardEffect() is CardEffectHeal)) return null;
            CardEffectData effect = state.GetSourceCardEffectData();
            return Describe(effect, state.GetParamInt(), state.GetUseIntRange() ?
                new CardEffectRange(state.GetParamMinInt(), state.GetParamMaxInt(), state.GetParamMultiplier()) : null);
        }
        internal static CardActionEffect? Definition(CardEffectData effect) => effect.GetEffectStateName() != "CardEffectHeal" ? null :
            // Character trigger effects have no parent card, but native stat getters still clamp healing endpoints.
            Describe(effect, Math.Max(0, effect.GetParamInt()), effect.GetUseIntRange() ?
                new CardEffectRange(Math.Max(0, effect.GetParamMinInt()), Math.Max(0, effect.GetParamMaxInt()), effect.GetParamMultiplier()) : null);
        private static CardActionEffect Describe(CardEffectData effect, int value, CardEffectRange? range)
        {
            var excluded = new List<SubtypeData>(); effect.GetTargetCharacterExcludedSubtypes(excluded);
            return new CardActionEffect("Heal", effect.GetTargetMode().ToString(), value,
                effect.GetTargetTeamType().HasFlag(Team.Type.Heroes), effect.GetTargetTeamType().HasFlag(Team.Type.Monsters),
                Array.Empty<CombatStatus>(), tests: new CardEffectTests(effect.GetShouldTest(), effect.GetShouldFailToCastIfTestFails(),
                    effect.GetShouldCancelSubsequentEffectsIfTestFails(), false),
                range: range,
                filters: new CardTargetFilters(effect.GetTargetModeHealthFilter().ToString(), effect.GetTargetModeStatusEffectsFilter(),
                    effect.GetTargetModeStatusEffectsExcludedFilter(), effect.GetTargetIgnoreBosses(),
                    effect.GetTargetCharacterSubtype().IsNone ? "" : effect.GetTargetCharacterSubtype().Key,
                    excluded.Select(subtype => subtype.IsNone ? "" : subtype.Key).ToArray()));
        }
    }
}

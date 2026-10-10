using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using TypeNameCache = ShinyShoe.TypeNameCache;

namespace MonsterTrain2Poju.Probe
{
    internal static class CardTraitCompositionProbe
    {
        internal static CardTraitValue Definition(CardTraitData data) => new CardTraitValue(
            TypeNameCache.GetType(data.GetTraitStateName()).FullName, data.GetTraitStateName(), data.GetParamInt(), data.GetParamInt(),
            data.GetTraitIsRemovable(), (int)data.GetStackMode(), parameterUpgradeCastEffects: CastEffects(data));
        private static string[] CastEffects(CardTraitData data) => data.GetCardUpgradeDataParam()?.GetCardTriggerUpgrades()
            .Where(trigger => trigger.GetTrigger() == CardTriggerType.OnCast).SelectMany(trigger => trigger.GetCardEffects())
            .Select(effect => effect.GetEffectStateName()).ToArray() ?? Array.Empty<string>();
        internal static CardTraitValue Value(CardTraitState trait)
        {
            CardTraitData? data = trait.GetCardTraitData();
            return new CardTraitValue(trait.GetType().FullName, data?.GetTraitStateName(), data?.GetParamInt() ?? 0,
                trait.GetParamInt(), trait.GetTraitIsRemovable(), (int)trait.GetStackMode(), trait.IsTemporaryReplacement(),
                data == null ? Array.Empty<string>() : CastEffects(data), data?.GetTraitIsRemovable(),
                trait is CardTraitScalingMagicPowerOnMoonPhase ? (float?)AccessTools.Field(typeof(CardTraitScalingMagicPowerOnMoonPhase), "MagicPowerMultiplier").GetValue(trait) : null);
        }
        internal static List<CardTraitState> Bases(CardState card) => (List<CardTraitState>)AccessTools.Field(typeof(CardState), "traits").GetValue(card);
        internal static List<CardTraitState> Combined(CardState card) => (List<CardTraitState>)AccessTools.Field(typeof(CardState), "combinedTraits").GetValue(card);
        internal static CardTraitCompositionState Capture(CardState card, bool combined = false)
        {
            var permanent = card.GetCardStateModifiers(); var temporary = card.GetTemporaryCardStateModifiers();
            return new CardTraitCompositionState(Bases(card).Select(Value).ToArray(), temporary.GetTemporaryTraits().Select(Value).ToArray(),
                temporary.GetCardUpgrades().Select(upgrade => (IReadOnlyList<CardTraitValue>)upgrade.GetTraitDataUpgrades().Select(Definition).ToArray()).ToArray(),
                permanent.GetCardUpgrades().Concat(temporary.GetCardUpgrades()).SelectMany(upgrade => upgrade.GetRemoveTraitUpgrades())
                    .Select(name => TypeNameCache.GetType(name).FullName).ToArray(),
                permanent.GetTraitReplacements().Select(rule => new CardTraitReplacement(rule.oldTrait, rule.newTrait, TypeNameCache.GetType(rule.newTrait).FullName)).ToArray(),
                combined ? Combined(card).Select(Value).ToArray() : null);
        }
        internal static string[] Origins(CardState card) => Combined(card).Select(trait =>
        {
            int index = Bases(card).IndexOf(trait); if (index >= 0) return "base:" + index;
            index = card.GetTemporaryCardStateModifiers().GetTemporaryTraits().IndexOf(trait);
            return index >= 0 ? "temporary:" + index : "created";
        }).ToArray();
    }
}

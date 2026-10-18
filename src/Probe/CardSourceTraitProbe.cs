using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class CardSourceTraitProbe
    {
        private sealed class Source
        {
            internal readonly CardSourceTrait[] Traits;
            internal Source(CardData data) { Traits = data.GetTraits().Select(Capture).ToArray(); }
        }
        private static readonly ConditionalWeakTable<CardState, Source> Sources = new ConditionalWeakTable<CardState, Source>();
        internal static CardSourceTrait[]? Capture(CardState card) => Sources.TryGetValue(card, out Source source) ? source.Traits : null;
        internal static CardSourceTrait Capture(CardTraitData trait) => new CardSourceTrait(trait.GetTraitStateName(),
            trait.GetParamInt(), trait.GetParamInt2(), trait.GetParamInt3(), trait.GetParamBool(), trait.GetParamStr(),
            trait.GetParamFloat(), trait.GetUseScalingParams(), trait.GetParamTrackedValue().ToString(), trait.GetParamEntryDuration().ToString(),
            trait.GetParamTrackedValue() == CardStatistics.TrackedValueType.StatusEffectCountInTargetRoom ? trait.GetParamTeamType().ToString() : null,
            trait.GetParamTrackedValue() == CardStatistics.TrackedValueType.StatusEffectCountInTargetRoom ?
                trait.GetParamStatusEffects().Select(status => status.statusId).ToArray() : null,
            trait.GetTraitStateName() == "CardTraitIncreaseCostOfCardsInHand" ? trait.GetParamCardType().ToString() : null);
        [HarmonyPatch(typeof(CardState), nameof(CardState.Setup))]
        private static class SetupPatch
        {
            private static void Postfix(CardState __instance, CardData setCardData)
            {
                if (!CardBranchMaskProbe.Enabled) return;
                Sources.Remove(__instance);
                Sources.Add(__instance, new Source(setCardData));
            }
        }
    }
}

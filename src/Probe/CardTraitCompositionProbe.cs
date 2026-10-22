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
            data.GetTraitIsRemovable(), (int)data.GetStackMode(), parameterUpgradeCastEffects: CastEffects(data), feedingQuery: FeedingQuery(data),
            handCostTargetType: data.GetTraitStateName() == "CardTraitIncreaseCostOfCardsInHand" ? data.GetParamCardType().ToString() : null,
            graftSevered: data.GetTraitStateName() == CardGraftedEquipmentModel.Trait ? false : (bool?)null,
            holdoverPending: data.GetTraitStateName() == CardRetainModel.Trait ? false : (bool?)null,
            copyOnPlayUpgrade: CopyUpgrade(data));
        private static CardUpgradeModifier? CopyUpgrade(CardTraitData data)
        {
            if (data.GetTraitStateName() != CardCopyOnPlayModel.Trait || data.GetCardUpgradeDataParam() == null) return null;
            var definition = data.GetCardUpgradeDataParam();
            if (definition.name != "CopyOnPlayTraitUpgrade") return null;
            var state = new CardUpgradeState(); state.Setup(definition);
            return CardModifierProbe.Upgrade(state, rejectFilters: false, filtersAlreadyApplied: true);
        }
        private static CardStatisticQuery? FeedingQuery(CardTraitData data)
        {
            if (data.GetTraitStateName() != "CardTraitScalingFeederRules") return null;
            var trait = new CardTraitScalingFeederRules(); trait.Setup(data, CardState.None);
            return FeedingQuery(trait.StatValueData);
        }
        private static CardStatisticQuery FeedingQuery(CardStatistics.StatValueData data)
        {
            CardStatisticQuery query = DamageScalingProbe.Query(data, 0, false);
            // Capture static query parameters. Evaluation supplies the live
            // source cost and type from the branch-owned card descriptor.
            return new CardStatisticQuery(query.Type, query.Duration, query.TypeMatchDataIds, query.SubtypeMatchDataIds,
                query.SubtypeIsNone, query.Subtype, query.SpecificCardDataId, targetTeam: query.TargetTeam);
        }
        private static string[] CastEffects(CardTraitData data) => data.GetCardUpgradeDataParam()?.GetCardTriggerUpgrades()
            .Where(trigger => trigger.GetTrigger() == CardTriggerType.OnCast).SelectMany(trigger => trigger.GetCardEffects())
            .Select(effect => effect.GetEffectStateName()).ToArray() ?? Array.Empty<string>();
        internal static CardTraitValue Value(CardTraitState trait)
        {
            CardTraitData? data = trait.GetCardTraitData();
            return new CardTraitValue(trait.GetType().FullName, data?.GetTraitStateName(), data?.GetParamInt() ?? 0,
                trait.GetParamInt(), trait.GetTraitIsRemovable(), (int)trait.GetStackMode(), trait.IsTemporaryReplacement(),
                data == null ? Array.Empty<string>() : CastEffects(data), data?.GetTraitIsRemovable(),
                trait is CardTraitScalingMagicPowerOnMoonPhase ? (float?)AccessTools.Field(typeof(CardTraitScalingMagicPowerOnMoonPhase), "MagicPowerMultiplier").GetValue(trait) : null,
                feedingQuery: trait is CardTraitScalingFeederRules ? FeedingQuery(trait.StatValueData) : null,
                handCostTargetType: trait is CardTraitIncreaseCostOfCardsInHand ? trait.GetParamCardType().ToString() : null,
                moonPhaseUpgradeState: (CardBranchMaskProbe.Enabled || MoonPhaseCardUpgradeCalibration.Capturing) && trait is CardTraitAddCardUpgradeOnMoonPhase
                    ? MoonPhaseCardUpgradeCalibration.State(trait) : null,
                graftSevered: trait is CardTraitGraftedEquipment graft ? graft.Severed : (bool?)null,
                holdoverPending: trait is CardTraitRetain ? (bool?)AccessTools.Field(typeof(CardTraitRetain), "retained").GetValue(trait) : null,
                copyOnPlayUpgrade: data == null ? null : CopyUpgrade(data));
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

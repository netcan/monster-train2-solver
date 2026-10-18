using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicCardTypeSequenceRule
    {
        public IReadOnlyList<string> PlayedTypes { get; }
        public RelicCardTypeSequenceRule(IReadOnlyList<string> playedTypes)
        { PlayedTypes = Array.AsReadOnly(playedTypes.ToArray()); }
    }

    internal static class RelicCardTypeSequenceModel
    {
        internal const string Effect = "RelicEffectEnergyAndCardDrawOnCardPlaySequence";
        internal const string DataId = "a67438aa-e1d7-45d6-bf62-fceddd2a723f";
        internal const string AssetKey = "GainBonusOnThreeCardsPlayed";
        internal static string? Validate(CombatRelicState owner, RelicResourceEffect resource)
        {
            RelicConditionState? condition = resource.Conditions.Count == 1 ? resource.Conditions[0] : null;
            return owner.DataId == DataId && owner.AssetKey == AssetKey && owner.IsCovenant == false &&
                owner.DisallowedInPlacementPhase == true && owner.EffectTypes.SequenceEqual(new[] { Effect }) &&
                resource.EffectIndex == 0 && resource.ParamInt == 1 && resource.ParamInt2 == 1 && resource.Amount == 1 &&
                resource.RequiredMutatorId == null && !resource.CanApplyInPreviewMode &&
                resource.CardTypeSequence != null && resource.CardTypeSequence.PlayedTypes.Count <= 3 &&
                resource.CardTypeSequence.PlayedTypes.Distinct().Count() == resource.CardTypeSequence.PlayedTypes.Count &&
                !resource.CardTypeSequence.PlayedTypes.Any(string.IsNullOrEmpty) &&
                condition?.TrackTriggerCount == true && condition.AllowMultiple && condition.Comparator == 3 && condition.Value == 3 &&
                condition.Query.Duration == "ThisTurn" && condition.Query.Type == "SubtypeInDeck" && condition.Query.SubtypeIsNone
                ? null : "Missing or changed original three-card-type sequence definition.";
        }
        internal static RelicFirstCardCostResult Test(CombatContext source, string? cardType, bool? isAbility, out bool passed)
        {
            passed = false; CombatContext context = source;
            for (int index = 0; index < (source.Relics?.Count ?? 0); index++)
            {
                RelicResourceEffect? resource = source.Relics![index].ResourceEffects?.SingleOrDefault(rule => rule.Type == Effect);
                if (resource == null) continue;
                string? error = Validate(source.Relics[index], resource); if (error != null) return new(source, error);
                IReadOnlyList<string> types = resource.CardTypeSequence!.PlayedTypes;
                if (types.Count == 3) continue;
                if (!isAbility.HasValue) return new(source, "Missing native sequence ability classification.");
                if (isAbility.Value) continue;
                if (string.IsNullOrEmpty(cardType)) return new(source, "Missing native sequence card type.");
                if (types.Contains(cardType!)) continue;
                context = Replace(context, index, resource.WithCardTypeSequence(new(types.Concat(new[] { cardType! }).ToArray())));
                passed = true;
            }
            return new(context);
        }
        // Raw Apply is unconditional; the manager performs conditions/Test first.
        internal static RelicFirstCardCostResult Apply(CombatContext source, int playedCardId = 0, BattlePlayRules? definitions = null)
        {
            CombatContext context = source;
            for (int index = 0; index < (source.Relics?.Count ?? 0); index++)
            {
                RelicResourceEffect? resource = context.Relics![index].ResourceEffects?.SingleOrDefault(rule => rule.Type == Effect);
                if (resource == null) continue;
                string? error = Validate(context.Relics[index], resource); if (error != null) return new(source, error);
                if (resource.CardTypeSequence!.PlayedTypes.Count == 3)
                {
                    error = EnergyModel.Validate(context); if (error != null) return new(source, error);
                    context = EnergyModel.Apply(context, "GainEnergy", resource.ParamInt);
                    TypedDrawResult drawn = NextDrawUpgradeModel.DrawCards(context, resource.ParamInt2, 0, definitions);
                    if (!drawn.Supported) return new(source, drawn.UnsupportedReason);
                    context = drawn.Context!;
                }
                RelicConditionRecord recorded = RelicConditionModel.Record(context, resource.Conditions);
                if (!recorded.Supported) return new(source, recorded.UnsupportedReason);
                context = Replace(recorded.Context, index, resource.WithConditions(recorded.Conditions));
            }
            return new(context);
        }
        internal static RelicFirstCardCostResult Played(CombatContext source, int playedCardId, CardPlayRule? card,
            BattlePlayRules? definitions, bool placement, bool preview)
        {
            if (placement || preview) return new(source);
            RelicResourceEffect? resource = source.Relics?.SingleOrDefault(owner => owner.DataId == DataId)?.ResourceEffects?.SingleOrDefault(rule => rule.Type == Effect);
            if (resource == null) return new(source);
            RelicConditionEvaluation evaluated = RelicConditionModel.Evaluate(source, resource.Conditions);
            if (!evaluated.Supported) return new(source, evaluated.UnsupportedReason);
            if (!evaluated.Passed) return new(evaluated.Context);
            RelicFirstCardCostResult tested = Test(evaluated.Context, card?.CardType, card?.IsAnyAbility, out bool passed);
            return !tested.Supported || !passed ? tested : Apply(tested.Context, playedCardId, definitions);
        }
        internal static RelicFirstCardCostResult StartTurn(CombatContext source)
        {
            CombatContext context = source;
            for (int index = 0; index < (source.Relics?.Count ?? 0); index++)
            {
                RelicResourceEffect? resource = source.Relics![index].ResourceEffects?.SingleOrDefault(rule => rule.Type == Effect);
                if (resource == null) continue;
                string? error = Validate(source.Relics[index], resource); if (error != null) return new(source, error);
                context = Replace(context, index, resource.WithCardTypeSequence(new(Array.Empty<string>())));
            }
            return new(context);
        }
        private static CombatContext Replace(CombatContext source, int index, RelicResourceEffect resource)
        {
            CombatRelicState[] owners = source.Relics!.ToArray();
            owners[index] = owners[index].WithResourceEffects(owners[index].ResourceEffects!
                .Select(rule => rule.EffectIndex == resource.EffectIndex ? resource : rule).ToArray());
            return source.WithRelics(owners);
        }
    }
}

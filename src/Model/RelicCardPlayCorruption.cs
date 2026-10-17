using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicCardPlayCorruptionModifier
    {
        public int EffectIndex { get; }
        public int Amount { get; }
        public IReadOnlyList<string> RequiredTraits { get; }
        public IReadOnlyList<RelicConditionState> Conditions { get; }
        public int ConditionCount { get; }
        public bool CanApplyInPreviewMode { get; }

        public RelicCardPlayCorruptionModifier(int effectIndex, int amount,
            IReadOnlyList<string>? requiredTraits = null, IReadOnlyList<RelicConditionState>? conditions = null,
            int conditionCount = 0, bool canApplyInPreviewMode = false)
        {
            EffectIndex = effectIndex;
            Amount = amount;
            RequiredTraits = Array.AsReadOnly((requiredTraits ?? Array.Empty<string>()).ToArray());
            Conditions = Array.AsReadOnly((conditions ?? Array.Empty<RelicConditionState>()).ToArray());
            ConditionCount = conditions?.Count ?? conditionCount;
            CanApplyInPreviewMode = canApplyInPreviewMode;
        }
    }

    internal static class RelicCardPlayCorruptionModel
    {
        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type == RelicModel.ModifyCorruptionOnCardPlayed)
                .Select(item => item.index).ToArray();
            if (indices.Length == 0)
                return relic.CardPlayCorruptionModifiers?.Count > 0 ? "Unexpected card-play corruption definitions." : null;
            if (relic.CardPlayCorruptionModifiers == null ||
                !indices.SequenceEqual(relic.CardPlayCorruptionModifiers.Select(effect => effect.EffectIndex)))
                return "Missing ordered native card-play corruption definitions.";
            foreach (RelicCardPlayCorruptionModifier effect in relic.CardPlayCorruptionModifiers)
                if (effect.ConditionCount != effect.Conditions.Count || effect.RequiredTraits.Any(string.IsNullOrEmpty))
                    return "Incomplete native card-play corruption definition.";
            return null;
        }

        internal static TrainCombatResult ApplyOne(TrainCombatState source, int playedCardId,
            int selectedRoomIndex, int relicIndex, int effectIndex, bool placement, bool preview)
        {
            TrainCombatResult Fail(string reason) => new(null, RoomOutcome.Unsupported,
                Array.Empty<RoomCombatResult>(), reason);
            TrainCombatResult Return(TrainCombatState train) => new(train, RoomOutcome.Exchanged, Array.Empty<RoomCombatResult>());

            CombatContext? context = source.Context;
            if (context?.Relics == null || relicIndex < 0 || relicIndex >= context.Relics.Count)
                return Fail("Missing card-play corruption relic state.");
            CombatRelicState relic = context.Relics[relicIndex];
            if (effectIndex < 0 || effectIndex >= relic.EffectTypes.Count ||
                relic.EffectTypes[effectIndex] != RelicModel.ModifyCorruptionOnCardPlayed)
                return Fail("Card-play corruption effect order changed.");
            string? validation = Validate(relic);
            if (validation != null) return Fail(validation);
            RelicCardPlayCorruptionModifier? effect = relic.CardPlayCorruptionModifiers!
                .SingleOrDefault(item => item.EffectIndex == effectIndex);
            if (effect == null) return Fail("Missing ordered native card-play corruption definition.");
            if (placement && relic.DisallowedInPlacementPhase == true || preview && !effect.CanApplyInPreviewMode)
                return Return(CardSpellModel.WithContext(source, context));

            RelicConditionEvaluation conditions = RelicConditionModel.Evaluate(context, effect.Conditions);
            if (!conditions.Supported) return Fail(conditions.UnsupportedReason!);
            context = conditions.Context;
            TrainCombatState current = CardSpellModel.WithContext(source, context);
            if (!conditions.Passed) return Return(current);

            CardInstanceState? card = context.CardInstances?.SingleOrDefault(item => item.InstanceId == playedCardId);
            if (card == null) return Fail("Card-play corruption requires the played owned card.");
            if (effect.RequiredTraits.Count > 0)
            {
                if (card.MaskDescriptor == null || card.MaskDescriptor.Definition.DataId != card.DataId)
                    return Fail("Required card-play corruption traits need matching played-card mask metadata.");
                IReadOnlyList<CardTraitValue>? traits;
                try
                {
                    traits = CardTraitRefreshModel.Ensure(CardBranchMaskModel.OwnedState(card).Traits)
                        .Composition.CombinedTraits;
                }
                catch (InvalidOperationException error) { return Fail(error.Message); }
                if (traits == null) return Fail("Required card-play corruption traits were not captured.");
                var names = new HashSet<string>(traits.Select(trait => trait.RuntimeType), StringComparer.Ordinal);
                if (effect.RequiredTraits.Any(required => !names.Contains(required))) return Return(current);
            }

            if (selectedRoomIndex < 0)
                return Fail("Card-play corruption requires the selected room.");
            if (effect.Amount == 0) return Return(current);
            if (effect.Amount == int.MinValue)
                return Fail("The minimum signed corruption modifier cannot be represented as a removal amount.");

            bool add = effect.Amount > 0;
            int amount = add ? effect.Amount : -effect.Amount;
            RoomCorruptionResult changed = RoomCorruptionModel.Apply(current, selectedRoomIndex,
                new CardActionEffect("AdjustCorruption", "Room", amount, false, false,
                    Array.Empty<CombatStatus>(), corruptionAdd: add));
            if (!changed.Supported) return Fail(changed.UnsupportedReason!);
            return Return(CardSpellModel.WithContext(current, changed.Context!));
        }
    }
}

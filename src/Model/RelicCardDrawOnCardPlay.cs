using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicCardDrawOnCardPlayModifier
    {
        public int EffectIndex { get; }
        public int Count { get; }
        public string CardType { get; }
        public string? RequiredSubtype { get; }
        public IReadOnlyList<string> RequiredTraits { get; }
        public int ConditionCount { get; }
        public IReadOnlyList<RelicConditionState> Conditions { get; }

        public RelicCardDrawOnCardPlayModifier(int effectIndex, int count, string cardType,
            IReadOnlyList<RelicConditionState>? conditions = null, int conditionCount = 0, string? requiredSubtype = null,
            IReadOnlyList<string>? requiredTraits = null)
        {
            EffectIndex = effectIndex;
            Count = count;
            CardType = cardType;
            RequiredSubtype = requiredSubtype;
            RequiredTraits = Array.AsReadOnly((requiredTraits ?? Array.Empty<string>()).ToArray());
            Conditions = Array.AsReadOnly((conditions ?? Array.Empty<RelicConditionState>()).ToArray());
            ConditionCount = conditions?.Count ?? conditionCount;
        }

        internal RelicCardDrawOnCardPlayModifier WithConditions(IReadOnlyList<RelicConditionState> conditions) =>
            new RelicCardDrawOnCardPlayModifier(EffectIndex, Count, CardType, conditions, requiredSubtype: RequiredSubtype, requiredTraits: RequiredTraits);
    }

    internal sealed class RelicCardDrawOnCardPlayResult
    {
        internal CombatContext Context { get; }
        internal string? UnsupportedReason { get; }
        internal bool Supported => UnsupportedReason == null;
        internal RelicCardDrawOnCardPlayResult(CombatContext context, string? unsupportedReason = null)
        { Context = context; UnsupportedReason = unsupportedReason; }
    }

    internal static class RelicCardDrawOnCardPlayModel
    {
        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type == RelicModel.CardDrawOnCardPlay)
                .Select(item => item.index).ToArray();
            if (indices.Length == 0)
                return relic.CardDrawOnCardPlayModifiers?.Count > 0 ? "Unexpected card-draw-on-card-play definitions." : null;
            if (relic.CardDrawOnCardPlayModifiers == null ||
                !indices.SequenceEqual(relic.CardDrawOnCardPlayModifiers.Select(effect => effect.EffectIndex)))
                return "Missing ordered native card-draw-on-card-play definitions.";
            foreach (RelicCardDrawOnCardPlayModifier effect in relic.CardDrawOnCardPlayModifiers)
                if (string.IsNullOrEmpty(effect.CardType) || effect.ConditionCount != effect.Conditions.Count ||
                    effect.RequiredTraits.Any(string.IsNullOrEmpty))
                    return "Incomplete native card-draw-on-card-play definition.";
            return null;
        }

        internal static RelicCardDrawOnCardPlayResult ApplyOne(CombatContext source, int playedCardId,
            BattlePlayRules? definitions, int relicIndex, int effectIndex, int currentTurn)
        {
            if (source.Relics == null || relicIndex < 0 || relicIndex >= source.Relics.Count)
                return Fail(source, "Missing card-draw-on-card-play relic state.");
            CombatRelicState relic = source.Relics[relicIndex];
            if (effectIndex < 0 || effectIndex >= relic.EffectTypes.Count ||
                relic.EffectTypes[effectIndex] != RelicModel.CardDrawOnCardPlay)
                return Fail(source, "Card-draw-on-card-play effect order changed.");
            RelicCardDrawOnCardPlayModifier? effect = relic.CardDrawOnCardPlayModifiers?.SingleOrDefault(item => item.EffectIndex == effectIndex);
            if (effect == null) return Fail(source, "Missing ordered native card-draw-on-card-play definition.");

            CombatContext context = source;
            if (effect.RequiredTraits.Count > 0)
            {
                CardInstanceState? played = context.FindCard(playedCardId);
                if (played?.MaskDescriptor == null || played.MaskDescriptor.Definition.DataId != played.DataId)
                    return Fail(context, "Required card-draw relic traits need matching played-card metadata.");
                IReadOnlyList<CardTraitValue>? traits;
                try { traits = CardTraitRefreshModel.Ensure(CardBranchMaskModel.OwnedState(played).Traits).Composition.CombinedTraits; }
                catch (InvalidOperationException error) { return Fail(context, error.Message); }
                if (traits == null) return Fail(context, "Required card-draw relic traits were not captured.");
                if (effect.RequiredTraits.Any(required => !traits.Any(trait => trait.RuntimeType == required)))
                    return new RelicCardDrawOnCardPlayResult(context);
            }
            if (effect.CardType != "Invalid" || !string.IsNullOrEmpty(effect.RequiredSubtype))
            {
                CardInstanceState? played = context.FindCard(playedCardId);
                if (played?.MaskDescriptor == null || played.MaskDescriptor.Definition.DataId != played.DataId)
                    return Fail(context, "Filtered relic draws require matching played-card metadata.");
                if (effect.CardType != "Invalid" && played.MaskDescriptor.Definition.CardType != effect.CardType)
                    return new RelicCardDrawOnCardPlayResult(context);
                if (!string.IsNullOrEmpty(effect.RequiredSubtype) &&
                    !played.MaskDescriptor.Definition.Subtypes.Contains(effect.RequiredSubtype))
                    return new RelicCardDrawOnCardPlayResult(context);
            }

            if (currentTurn == 0 && effect.Conditions.Any(condition =>
                condition.Query.Duration == "ThisTurn" && !condition.TrackTriggerCount))
                return new RelicCardDrawOnCardPlayResult(context);

            if (effect.Conditions.Count > 0)
            {
                RelicConditionEvaluation evaluation = RelicConditionModel.Evaluate(context, effect.Conditions);
                if (!evaluation.Supported) return Fail(evaluation.Context, evaluation.UnsupportedReason!);
                context = evaluation.Context;
                if (!evaluation.Passed) return new RelicCardDrawOnCardPlayResult(context);
            }

            TypedDrawResult drawn = NextDrawUpgradeModel.DrawCards(context, effect.Count, playedCardId, definitions);
            if (!drawn.Supported) return Fail(context, drawn.UnsupportedReason!);
            context = drawn.Context!;
            if (effect.Conditions.Count > 0)
            {
                RelicConditionRecord record = RelicConditionModel.Record(context, effect.Conditions);
                if (!record.Supported) return Fail(context, record.UnsupportedReason!);
                CombatRelicState[] relics = record.Context.Relics!.ToArray();
                RelicCardDrawOnCardPlayModifier[] modifiers = relics[relicIndex].CardDrawOnCardPlayModifiers!.ToArray();
                int modifierIndex = Array.FindIndex(modifiers, item => item.EffectIndex == effectIndex);
                if (modifierIndex < 0) return Fail(record.Context, "Card-draw-on-card-play effect order changed while recording its condition.");
                modifiers[modifierIndex] = effect.WithConditions(record.Conditions);
                relics[relicIndex] = relics[relicIndex].WithCardDrawOnCardPlayModifiers(modifiers);
                context = record.Context.WithRelics(relics);
            }
            return new RelicCardDrawOnCardPlayResult(context);
        }

        private static RelicCardDrawOnCardPlayResult Fail(CombatContext source, string error) =>
            new RelicCardDrawOnCardPlayResult(source, error);
    }
}

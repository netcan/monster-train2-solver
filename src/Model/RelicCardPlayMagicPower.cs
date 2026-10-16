using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicCardPlayMagicPowerModifier
    {
        public int EffectIndex { get; }
        public int PowerModifier { get; }
        public IReadOnlyList<string> RequiredTraits { get; }
        public int ConditionCount { get; }
        public IReadOnlyList<RelicConditionState> Conditions { get; }

        public RelicCardPlayMagicPowerModifier(int effectIndex, int powerModifier,
            IReadOnlyList<string>? requiredTraits = null, IReadOnlyList<RelicConditionState>? conditions = null,
            int conditionCount = 0)
        {
            EffectIndex = effectIndex;
            PowerModifier = powerModifier;
            RequiredTraits = Array.AsReadOnly((requiredTraits ?? Array.Empty<string>()).ToArray());
            Conditions = Array.AsReadOnly((conditions ?? Array.Empty<RelicConditionState>()).ToArray());
            ConditionCount = conditions?.Count ?? conditionCount;
        }

        internal RelicCardPlayMagicPowerModifier WithConditions(IReadOnlyList<RelicConditionState> conditions) =>
            new RelicCardPlayMagicPowerModifier(EffectIndex, PowerModifier, RequiredTraits, conditions);
    }

    internal sealed class RelicCardPlayMagicPowerDispatch
    {
        internal int RelicIndex { get; }
        internal int EffectIndex { get; }
        internal bool Applied { get; }
        internal IReadOnlyList<int> ChangedCards { get; }
        internal RelicCardPlayMagicPowerDispatch(int relicIndex, int effectIndex, bool applied, IReadOnlyList<int> changedCards)
        { RelicIndex = relicIndex; EffectIndex = effectIndex; Applied = applied; ChangedCards = Array.AsReadOnly(changedCards.ToArray()); }
    }

    internal sealed class RelicCardPlayMagicPowerResult
    {
        internal CombatContext Context { get; }
        internal string? UnsupportedReason { get; }
        internal bool Supported => UnsupportedReason == null;
        internal IReadOnlyList<RelicCardPlayMagicPowerDispatch> Dispatches { get; }
        internal RelicCardPlayMagicPowerResult(CombatContext context,
            IReadOnlyList<RelicCardPlayMagicPowerDispatch>? dispatches = null, string? unsupportedReason = null)
        { Context = context; Dispatches = Array.AsReadOnly((dispatches ?? Array.Empty<RelicCardPlayMagicPowerDispatch>()).ToArray()); UnsupportedReason = unsupportedReason; }
    }

    internal static class RelicCardPlayMagicPowerModel
    {
        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type == RelicModel.ModifyCardMagicPowerOnCardPlayed)
                .Select(item => item.index).ToArray();
            if (indices.Length == 0)
                return relic.CardPlayMagicPowerModifiers?.Count > 0 ? "Unexpected card-play magic-power definitions." : null;
            if (relic.CardPlayMagicPowerModifiers == null ||
                !indices.SequenceEqual(relic.CardPlayMagicPowerModifiers.Select(effect => effect.EffectIndex)))
                return "Missing ordered native card-play magic-power definitions.";
            foreach (RelicCardPlayMagicPowerModifier effect in relic.CardPlayMagicPowerModifiers)
                if (effect.ConditionCount != effect.Conditions.Count || effect.RequiredTraits.Any(string.IsNullOrEmpty))
                    return "Incomplete native card-play magic-power definition.";
            return null;
        }

        internal static RelicCardPlayMagicPowerResult Apply(CombatContext source, int playedCardId,
            BattlePlayRules? definitions = null, int currentTurn = -1, int? selectedRoomIndex = null)
        {
            bool hasMagicPower = source.Relics?.Any(relic => relic.EffectTypes.Contains(RelicModel.ModifyCardMagicPowerOnCardPlayed)) == true;
            bool hasCheaperSpellUpgrade = source.Relics?.Any(relic => relic.EffectTypes.Contains(RelicModel.AddTempUpgradeToHandCardsIfCheaper)) == true;
            bool hasCardCopy = source.Relics?.Any(relic => relic.EffectTypes.Contains(RelicModel.CopyCardOnCardPlay)) == true;
            bool hasCardDraw = source.Relics?.Any(relic => relic.EffectTypes.Contains(RelicModel.CardDrawOnCardPlay)) == true;
            bool hasRoomCapacity = source.Relics?.Any(relic => relic.EffectTypes.Contains(RelicModel.ModifyRoomCapacityOnCardPlay)) == true;
            bool hasRoomAttachmentReplication = source.Relics?.Any(relic => relic.EffectTypes.Contains(RelicModel.FirstRoomCardOnAllFloors)) == true;
            if (!hasMagicPower && !hasCheaperSpellUpgrade && !hasCardCopy && !hasCardDraw && !hasRoomCapacity && !hasRoomAttachmentReplication)
                return new RelicCardPlayMagicPowerResult(source);
            if ((hasMagicPower || hasCheaperSpellUpgrade || hasCardCopy) && source.CardInstances == null)
                return Fail(source, "Native card-play relic effects require the current owned-card list.");
            if ((hasMagicPower || hasCheaperSpellUpgrade || hasCardCopy) && !source.CardInstances!.Any(card => card.InstanceId == playedCardId))
                return Fail(source, "Native card-play relic effects require the played card in the owned-card list.");

            CombatContext context = source;
            var dispatches = new List<RelicCardPlayMagicPowerDispatch>();
            for (int relicIndex = 0; relicIndex < context.Relics!.Count; relicIndex++)
            {
                for (int effectIndex = 0; effectIndex < context.Relics![relicIndex].EffectTypes.Count; effectIndex++)
                {
                    CombatRelicState relic = context.Relics[relicIndex];
                    if (relic.EffectTypes[effectIndex] == RelicModel.CopyCardOnCardPlay)
                    {
                        RelicCopyCardOnCardPlayResult copied = RelicCopyCardOnCardPlayModel.ApplyOne(
                            context, playedCardId, definitions, relicIndex, effectIndex);
                        if (!copied.Supported) return Fail(context, copied.UnsupportedReason!);
                        context = copied.Context;
                        if (copied.Applied)
                            dispatches.Add(new RelicCardPlayMagicPowerDispatch(relicIndex, effectIndex, true, copied.CopiedCardIds));
                        continue;
                    }
                    if (relic.EffectTypes[effectIndex] == RelicModel.CardDrawOnCardPlay)
                    {
                        RelicCardDrawOnCardPlayResult drawn = RelicCardDrawOnCardPlayModel.ApplyOne(
                            context, playedCardId, definitions, relicIndex, effectIndex, currentTurn);
                        if (!drawn.Supported) return Fail(context, drawn.UnsupportedReason!);
                        context = drawn.Context;
                        continue;
                    }
                    if (relic.EffectTypes[effectIndex] == RelicModel.ModifyRoomCapacityOnCardPlay)
                    {
                        int selectedRoom = context.QueryFrame?.SelectedRoomIndex ?? selectedRoomIndex ?? -1;
                        RoomCapacityResult adjusted = RelicCardPlayRoomCapacityModel.ApplyOne(
                            context, playedCardId, definitions, relicIndex, effectIndex, selectedRoom);
                        if (!adjusted.Supported) return Fail(context, adjusted.UnsupportedReason!);
                        context = adjusted.Context!;
                        continue;
                    }
                    if (relic.EffectTypes[effectIndex] == RelicModel.FirstRoomCardOnAllFloors)
                    {
                        int selectedRoom = context.QueryFrame?.SelectedRoomIndex ?? selectedRoomIndex ?? -1;
                        RoomCapacityResult replicated = RelicRoomAttachmentReplicationModel.ApplyOne(
                            context, playedCardId, definitions, relicIndex, effectIndex, selectedRoom);
                        if (!replicated.Supported) return Fail(context, replicated.UnsupportedReason!);
                        context = replicated.Context!;
                        continue;
                    }
                    if (relic.EffectTypes[effectIndex] == RelicModel.AddTempUpgradeToHandCardsIfCheaper)
                    {
                        RelicCheaperSpellUpgradeResult upgraded = RelicCheaperSpellUpgradeModel.ApplyOnCardPlayed(
                            context, playedCardId, definitions, relicIndex, effectIndex);
                        if (!upgraded.Supported) return Fail(context, upgraded.UnsupportedReason!);
                        context = upgraded.Context;
                        continue;
                    }
                    if (relic.EffectTypes[effectIndex] != RelicModel.ModifyCardMagicPowerOnCardPlayed) continue;
                    var magicDefinitions = (relic.CardPlayMagicPowerModifiers ?? Array.Empty<RelicCardPlayMagicPowerModifier>())
                        .ToDictionary(effect => effect.EffectIndex);
                    if (!magicDefinitions.TryGetValue(effectIndex, out RelicCardPlayMagicPowerModifier? effect))
                        return Fail(context, "Missing ordered native card-play magic-power definition.");
                    if (effect!.Conditions.Count > 0)
                    {
                        RelicConditionEvaluation evaluation = RelicConditionModel.Evaluate(context, effect.Conditions);
                        if (!evaluation.Supported) return Fail(context, evaluation.UnsupportedReason!);
                        context = evaluation.Context;
                        if (!evaluation.Passed) continue;
                    }

                    if (effect.RequiredTraits.Count > 0)
                    {
                        CardInstanceState? played = context.CardInstances!.FirstOrDefault(card => card.InstanceId == playedCardId);
                        if (played?.MaskDescriptor == null || played.MaskDescriptor.Definition.DataId != played.DataId)
                            return Fail(context, "Required card-play relic traits need matching played-card mask metadata.");
                        IReadOnlyList<CardTraitValue>? traits;
                        try
                        {
                            CardTraitRefreshState refreshed = CardTraitRefreshModel.Ensure(CardBranchMaskModel.OwnedState(played).Traits);
                            traits = refreshed.Composition.CombinedTraits;
                        }
                        catch (InvalidOperationException error) { return Fail(context, error.Message); }
                        if (traits == null) return Fail(context, "Required card-play relic traits were not captured.");
                        var names = new HashSet<string>(traits.Select(trait => trait.RuntimeType), StringComparer.Ordinal);
                        if (effect.RequiredTraits.Any(required => !names.Contains(required))) continue;
                    }

                    CardInstanceState[] owned = context.CardInstances!.ToArray();
                    if (owned.Any(card => card.MaskDescriptor == null || card.MaskDescriptor.Definition.DataId != card.DataId))
                        return Fail(context, "Card-play relic magic power requires matching metadata for every owned card.");
                    CardInstanceState[] spells = owned.Where(card => card.MaskDescriptor!.Definition.CardType == "Spell").ToArray();
                    if (spells.Length == 0) continue;

                    var changed = new List<int>();
                    foreach (CardInstanceState spell in spells)
                    {
                        bool? canScale = spell.MaskDescriptor!.Definition.CanScaleWithMagicPower;
                        if (!canScale.HasValue)
                            return Fail(context, "Card-play relic magic-power eligibility was not captured from an owned spell.");
                        if (!canScale.Value) continue;
                        CardInstanceState? updated = RelicMagicPowerModel.ApplyToCard(spell,
                            new RelicMagicPowerModifier(effect.EffectIndex, effect.PowerModifier), out string? error);
                        if (updated == null) return Fail(context, error!);
                        context = context.WithCard(updated);
                        changed.Add(spell.InstanceId);
                    }

                    if (effect.Conditions.Count > 0)
                    {
                        RelicConditionRecord record = RelicConditionModel.Record(context, effect.Conditions);
                        if (!record.Supported) return Fail(context, record.UnsupportedReason!);
                        context = record.Context;
                        var updatedEffects = context.Relics![relicIndex].CardPlayMagicPowerModifiers!.ToArray();
                        int offset = Array.FindIndex(updatedEffects, item => item.EffectIndex == effect.EffectIndex);
                        if (offset < 0) return Fail(context, "Card-play magic-power effect order changed while recording its condition.");
                        updatedEffects[offset] = effect.WithConditions(record.Conditions);
                        CombatRelicState[] updatedRelics = context.Relics.ToArray();
                        updatedRelics[relicIndex] = updatedRelics[relicIndex].WithCardPlayMagicPowerModifiers(updatedEffects);
                        context = context.WithRelics(updatedRelics);
                    }
                    dispatches.Add(new RelicCardPlayMagicPowerDispatch(relicIndex, effect.EffectIndex, true, changed));
                }
            }
            return new RelicCardPlayMagicPowerResult(context, dispatches);
        }

        private static RelicCardPlayMagicPowerResult Fail(CombatContext source, string error) =>
            new RelicCardPlayMagicPowerResult(source, unsupportedReason: error);
    }
}

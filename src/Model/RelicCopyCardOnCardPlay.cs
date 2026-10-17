using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicCopyCardOnCardPlayModifier
    {
        public int EffectIndex { get; }
        public int Count { get; }
        public string CardType { get; }
        public bool PermanentCopy { get; }
        public bool ExcludeChampions { get; }
        public IReadOnlyList<string> RequiredTraits { get; }
        public CardUpgradeModifier? Upgrade { get; }
        public int ConditionCount { get; }
        public IReadOnlyList<RelicConditionState> Conditions { get; }

        public RelicCopyCardOnCardPlayModifier(int effectIndex, int count, string cardType, bool permanentCopy,
            bool excludeChampions, IReadOnlyList<string>? requiredTraits, CardUpgradeModifier? upgrade,
            IReadOnlyList<RelicConditionState>? conditions = null, int conditionCount = 0)
        {
            EffectIndex = effectIndex;
            Count = count;
            CardType = cardType;
            PermanentCopy = permanentCopy;
            ExcludeChampions = excludeChampions;
            RequiredTraits = Array.AsReadOnly((requiredTraits ?? Array.Empty<string>()).ToArray());
            Upgrade = upgrade;
            Conditions = Array.AsReadOnly((conditions ?? Array.Empty<RelicConditionState>()).ToArray());
            ConditionCount = conditions?.Count ?? conditionCount;
        }

        internal RelicCopyCardOnCardPlayModifier WithConditions(IReadOnlyList<RelicConditionState> conditions) =>
            new RelicCopyCardOnCardPlayModifier(EffectIndex, Count, CardType, PermanentCopy, ExcludeChampions,
                RequiredTraits, Upgrade, conditions);
    }

    internal sealed class RelicCopyCardOnCardPlayResult
    {
        internal CombatContext Context { get; }
        internal string? UnsupportedReason { get; }
        internal bool Supported => UnsupportedReason == null;
        internal bool Applied { get; }
        internal IReadOnlyList<int> CopiedCardIds { get; }
        internal RelicCopyCardOnCardPlayResult(CombatContext context, bool applied = false,
            IReadOnlyList<int>? copiedCardIds = null, string? unsupportedReason = null)
        {
            Context = context;
            Applied = applied;
            CopiedCardIds = Array.AsReadOnly((copiedCardIds ?? Array.Empty<int>()).ToArray());
            UnsupportedReason = unsupportedReason;
        }
    }

    internal static class RelicCopyCardOnCardPlayModel
    {
        internal const string RelicId = "0f4a9e72-cd34-4667-95f6-6a0ca69832f8";
        internal const string RelicKey = "CopyFirstSpellPlayed";
        internal const string ConsumeRelicId = "b7207406-3c55-4771-b9c9-ed99dd9e1f9b";
        internal const string ConsumeRelicKey = "CopyFirstConsumePlayed";
        internal const string CopyConsumedId = "51368ce4-b037-49ef-801c-545405b78800";
        internal const string CopyConsumedKey = "CopyConsumedCard";
        internal const string MothersRemnant3Id = "6b5ccd78-056a-4599-b05a-368bb9e1d6bc";
        internal const string MothersRemnant3Key = "MothersRemnant3Relic";
        private const string EphemeralUpgradeId = "80a5edf4-6dc8-41a7-ab4e-9d0e914e0848";

        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type == RelicModel.CopyCardOnCardPlay)
                .Select(item => item.index).ToArray();
            if (indices.Length == 0)
                return relic.CopyCardOnCardPlayModifiers?.Count > 0 ? "Unexpected card-copy-on-card-play definitions." : null;
            bool copyFirstSpell = relic.DataId == RelicId && relic.AssetKey == RelicKey;
            bool copyFirstConsume = relic.DataId == ConsumeRelicId && relic.AssetKey == ConsumeRelicKey;
            bool copyConsumed = relic.DataId == CopyConsumedId && relic.AssetKey == CopyConsumedKey;
            bool mothersRemnant3 = relic.DataId == MothersRemnant3Id && relic.AssetKey == MothersRemnant3Key;
            if ((!copyFirstSpell && !copyFirstConsume && !copyConsumed && !mothersRemnant3) || indices.Length != 1 || indices[0] != 0 ||
                relic.EffectTypes.Count != 1 || relic.CopyCardOnCardPlayModifiers?.Count != 1 ||
                relic.CopyCardOnCardPlayModifiers[0].EffectIndex != 0)
                return "Only exact original card-copy-on-play relic owners are modeled.";

            RelicCopyCardOnCardPlayModifier effect = relic.CopyCardOnCardPlayModifiers[0];
            CardUpgradeModifier? upgrade = effect.Upgrade;
            RelicConditionState[] conditions = effect.Conditions.ToArray();
            string expectedDuration = copyFirstSpell || mothersRemnant3 ? "ThisTurn" : "ThisBattle";
            bool validUpgrade = copyFirstSpell
                ? upgrade != null && upgrade.DataId == EphemeralUpgradeId && upgrade.AssetKey == "AddEphemeral" &&
                    upgrade.Stats.Damage == 0 && upgrade.Stats.Health == 0 && upgrade.Stats.Cost == 0 &&
                    upgrade.Stats.Heal == 0 && upgrade.Stats.Size == 0 && upgrade.Stats.XCost == 0 &&
                    upgrade.Stats.EquipmentLimit == 0 && upgrade.Stats.UpgradeSlotCount == 0 &&
                    upgrade.Statuses.Count == 0 && upgrade.ExternalInteractions.Count == 0 &&
                    upgrade.Lifecycle?.AddedTraits.Any(trait => trait.RuntimeType == "CardTraitEphemeral") == true &&
                    upgrade.Lifecycle.RemovedRuntimeTypes.Count == 0 && upgrade.Lifecycle.Triggers.Count == 0
                : upgrade == null;
            string[] expectedTraits = copyFirstConsume || copyConsumed ? ["CardTraitExhaustState"] : Array.Empty<string>();
            int expectedCount = copyConsumed ? 2 : 1;
            string expectedCardType = copyConsumed || mothersRemnant3 ? "Invalid" : "Spell";
            int expectedComparator = copyConsumed ? 6 : mothersRemnant3 ? 3 : 2;
            bool expectedPermanent = copyFirstConsume || mothersRemnant3;
            if (effect.Count != expectedCount || effect.CardType != expectedCardType || effect.PermanentCopy != expectedPermanent ||
                effect.ExcludeChampions != mothersRemnant3 ||
                !effect.RequiredTraits.SequenceEqual(expectedTraits) || effect.ConditionCount != 1 || conditions.Length != 1 ||
                conditions[0].Query.Duration != expectedDuration || !conditions[0].TrackTriggerCount ||
                conditions[0].Comparator != expectedComparator || conditions[0].Value != 1 || conditions[0].AllowMultiple ||
                !validUpgrade)
                return "Incomplete or unsupported native card-copy-on-card-play payload.";
            return null;
        }

        internal static RelicCopyCardOnCardPlayResult ApplyOne(CombatContext source, int playedCardId,
            BattlePlayRules? definitions, int relicIndex, int effectIndex)
        {
            if (source.Relics == null || relicIndex < 0 || relicIndex >= source.Relics.Count)
                return Fail(source, "Missing card-copy-on-card-play relic state.");
            CombatRelicState relic = source.Relics[relicIndex];
            if (effectIndex < 0 || effectIndex >= relic.EffectTypes.Count ||
                relic.EffectTypes[effectIndex] != RelicModel.CopyCardOnCardPlay)
                return Fail(source, "Card-copy-on-card-play effect order changed.");
            RelicCopyCardOnCardPlayModifier? effect = relic.CopyCardOnCardPlayModifiers?.SingleOrDefault(item => item.EffectIndex == effectIndex);
            if (effect == null) return Fail(source, "Missing ordered native card-copy-on-card-play definition.");

            CardInstanceState? played = source.FindCard(playedCardId);
            if (played == null) return Fail(source, "Card-copy-on-card-play requires the captured played card.");
            if (!played.CanBeCopied.HasValue) return Fail(source, "Card-copy-on-card-play requires captured CanBeCopied state.");
            if (!played.CanBeCopied.Value) return new RelicCopyCardOnCardPlayResult(source);
            if (played.MaskDescriptor == null || played.MaskDescriptor.Definition.DataId != played.DataId)
                return Fail(source, "Card-copy-on-card-play requires matching played-card mask metadata.");
            if (effect.RequiredTraits.Count > 0)
            {
                IReadOnlyList<CardTraitValue>? traits;
                try { traits = CardTraitRefreshModel.Ensure(CardBranchMaskModel.OwnedState(played).Traits).Composition.CombinedTraits; }
                catch (InvalidOperationException error) { return Fail(source, error.Message); }
                if (traits == null) return Fail(source, "Card-copy-on-card-play required traits were not captured.");
                if (effect.RequiredTraits.Any(required => !traits.Any(trait => trait.RuntimeType == required)))
                    return new RelicCopyCardOnCardPlayResult(source);
            }
            if (effect.CardType != "Invalid" && played.MaskDescriptor.Definition.CardType != effect.CardType)
                return new RelicCopyCardOnCardPlayResult(source);

            CardPlayRule? playRule = definitions?.Cards.SingleOrDefault(item => item.DataId == played.DataId);
            if (playRule == null || playRule.CardType == null || !playRule.IsAnyAbility.HasValue)
                return Fail(source, "Card-copy-on-card-play requires captured card type and ability metadata.");
            if ((effect.CardType != "Invalid" && playRule.CardType != effect.CardType) ||
                playRule.IsAnyAbility.Value || playRule.Ability != null)
                return new RelicCopyCardOnCardPlayResult(source);
            if (effect.ExcludeChampions)
            {
                bool? isChampion = played.MaskDescriptor.Definition.IsChampionCard;
                if (!isChampion.HasValue)
                    return Fail(source, "Champion-excluding card-copy-on-play requires captured champion-card classification.");
                if (isChampion.Value) return new RelicCopyCardOnCardPlayResult(source);
            }

            if (effect.Conditions.Count > 0)
            {
                RelicConditionEvaluation evaluation = RelicConditionModel.Evaluate(source, effect.Conditions);
                if (!evaluation.Supported) return Fail(evaluation.Context, evaluation.UnsupportedReason!);
                if (!evaluation.Passed) return new RelicCopyCardOnCardPlayResult(evaluation.Context);
                source = evaluation.Context;
            }

            var creation = new CardCreationRule(played.DataId, CardModifiers.Empty(), played.EffectCounters,
                played.ExternalInteractions, played.DamageScalingTraits, played.StatusScalingTraits,
                played.UnitUpgradeScalingTraits, played.CapacityScalingTraits, played.EquippedUnitId,
                played.MaskDescriptor, played.ForgeCountScalingTraits, played.CanBeCopied, played.TrainHealScalingTraits);
            bool handHasSpace = source.Cards.Hand.Count < source.MaxHandSize;
            bool copyHasEphemeralUpgrade = effect.Upgrade?.Lifecycle?.AddedTraits.Any(trait =>
                trait.RuntimeType == "CardTraitEphemeral") == true;
            string destination = handHasSpace ? "HandPile" : copyHasEphemeralUpgrade ? "DiscardPile" : "DeckPile";
            var generation = new CardGenerationRule(destination, effect.Count,
                new[] { creation }, copyModifiers: true, upgrade: effect.Upgrade, deterministicPoolSelection: true,
                requireCopyableSource: true,
                permanent: effect.PermanentCopy);
            CardGenerationResult copied = CardGenerationModel.Apply(source, generation, playedCardId);
            if (!copied.Supported) return Fail(source, copied.UnsupportedReason!);
            source = copied.Context!;
            if (copied.AddedCards.Count == 0)
                return new RelicCopyCardOnCardPlayResult(source);

            if (destination == "DiscardPile")
            {
                if (source.OtherPiles == null) return Fail(source, "Purging a full-hand ephemeral copy requires captured card piles.");
                CardPileState? purged = source.OtherPiles.SingleOrDefault(pile => pile.Name == "Purged");
                if (purged == null || copied.AddedCards.Any(card => !source.Cards.Discard.Any(item => item.InstanceId == card.InstanceId)))
                    return Fail(source, "Purging a full-hand ephemeral copy requires native Discard and Purged piles.");
                int[] ids = copied.AddedCards.Select(card => card.InstanceId).ToArray();
                CardToken[] discard = source.Cards.Discard.Where(card => !ids.Contains(card.InstanceId)).ToArray();
                CardPileState[] piles = source.OtherPiles.Select(pile => pile.Name == "Purged"
                    ? copied.AddedCards.Aggregate(pile, CardPileModel.Add)
                    : pile).ToArray();
                source = source.WithCards(new CardCycleState(source.Cards.Hand, source.Cards.Draw, discard,
                    source.Cards.Rng, source.Cards.DrawModifier, source.Cards.ExternalInteractions,
                    source.Cards.BonusDraw, source.Cards.NextDrawUpgrades, source.Cards.NextUpgradeReferenceId))
                    .WithOtherPiles(piles)
                    .WithStatistics(ids.Aggregate(source.Statistics, (statistics, id) => statistics?.RemoveDeckCard(id)));
            }

            if (effect.Conditions.Count > 0)
            {
                RelicConditionRecord record = RelicConditionModel.Record(source, effect.Conditions);
                if (!record.Supported) return Fail(record.Context, record.UnsupportedReason!);
                CombatRelicState[] relics = record.Context.Relics!.ToArray();
                RelicCopyCardOnCardPlayModifier[] modifiers = relics[relicIndex].CopyCardOnCardPlayModifiers!.ToArray();
                int modifierIndex = Array.FindIndex(modifiers, item => item.EffectIndex == effectIndex);
                if (modifierIndex < 0) return Fail(record.Context, "Card-copy-on-card-play effect order changed while recording its condition.");
                modifiers[modifierIndex] = effect.WithConditions(record.Conditions);
                relics[relicIndex] = relics[relicIndex].WithCopyCardOnCardPlayModifiers(modifiers);
                source = record.Context.WithRelics(relics);
            }
            return new RelicCopyCardOnCardPlayResult(source, true, copied.AddedCards.Select(card => card.InstanceId).ToArray());
        }

        private static RelicCopyCardOnCardPlayResult Fail(CombatContext source, string error) =>
            new RelicCopyCardOnCardPlayResult(source, unsupportedReason: error);
    }
}

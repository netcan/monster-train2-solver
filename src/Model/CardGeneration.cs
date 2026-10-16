using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class CardCreationRule
    {
        public string DataId { get; }
        public CardModifiers StartingModifiers { get; }
        public IReadOnlyList<CardEffectCounter>? EffectCounters { get; }
        public IReadOnlyList<string> ExternalInteractions { get; }
        public IReadOnlyList<ScalingDamageTrait>? DamageScalingTraits { get; }
        public IReadOnlyList<ScalingStatusTrait>? StatusScalingTraits { get; }
        public IReadOnlyList<ScalingUnitUpgradeTrait>? UnitUpgradeScalingTraits { get; }
        public IReadOnlyList<ScalingCapacityTrait>? CapacityScalingTraits { get; }
        public IReadOnlyList<ScalingForgeCountTrait>? ForgeCountScalingTraits { get; }
        public IReadOnlyList<ScalingTrainHealTrait>? TrainHealScalingTraits { get; }
        public int? EquippedUnitId { get; }
        public CardMaskDescriptor? MaskDescriptor { get; }
        public bool? CanBeCopied { get; }
        public CardCreationRule(string dataId, CardModifiers startingModifiers, IReadOnlyList<CardEffectCounter>? effectCounters,
            IReadOnlyList<string> externalInteractions, IReadOnlyList<ScalingDamageTrait>? damageScalingTraits = null,
            IReadOnlyList<ScalingStatusTrait>? statusScalingTraits = null, IReadOnlyList<ScalingUnitUpgradeTrait>? unitUpgradeScalingTraits = null,
            IReadOnlyList<ScalingCapacityTrait>? capacityScalingTraits = null, int? equippedUnitId = null, CardMaskDescriptor? maskDescriptor = null,
            IReadOnlyList<ScalingForgeCountTrait>? forgeCountScalingTraits = null, bool? canBeCopied = null,
            IReadOnlyList<ScalingTrainHealTrait>? trainHealScalingTraits = null)
        { DataId = dataId; StartingModifiers = startingModifiers; EffectCounters = effectCounters == null ? null : Array.AsReadOnly(effectCounters.ToArray());
            ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray());
            DamageScalingTraits = damageScalingTraits == null ? null : Array.AsReadOnly(damageScalingTraits.ToArray());
            StatusScalingTraits = statusScalingTraits == null ? null : Array.AsReadOnly(statusScalingTraits.ToArray());
            UnitUpgradeScalingTraits = unitUpgradeScalingTraits == null ? null : Array.AsReadOnly(unitUpgradeScalingTraits.ToArray());
            CapacityScalingTraits = capacityScalingTraits == null ? null : Array.AsReadOnly(capacityScalingTraits.ToArray());
            ForgeCountScalingTraits = forgeCountScalingTraits == null ? null : Array.AsReadOnly(forgeCountScalingTraits.ToArray());
            TrainHealScalingTraits = trainHealScalingTraits == null ? null : Array.AsReadOnly(trainHealScalingTraits.ToArray());
            EquippedUnitId = equippedUnitId; MaskDescriptor = maskDescriptor; CanBeCopied = canBeCopied; }
    }

    public sealed class DiscardGenerationUpgrade
    {
        public string RequiredUpgradeId { get; }
        public CardUpgradeModifier Upgrade { get; }
        public DiscardGenerationUpgrade(string requiredUpgradeId, CardUpgradeModifier upgrade)
        { RequiredUpgradeId = requiredUpgradeId; Upgrade = upgrade; }
    }

    public sealed class CardGenerationRule
    {
        public string Destination { get; }
        public int Count { get; }
        public IReadOnlyList<CardCreationRule> Pool { get; }
        public bool SkipDuplicateInHand { get; }
        public bool RequireHandSpace { get; }
        public bool CopyModifiers { get; }
        public bool IgnoreTemporaryModifiers { get; }
        public CardUpgradeModifier? Upgrade { get; }
        public IReadOnlyList<DiscardGenerationUpgrade> DiscardCopyUpgrades { get; }
        public IReadOnlyList<string> ExternalInteractions { get; }
        public bool DeterministicPoolSelection { get; }
        public bool RequireCopyableSource { get; }
        public bool UpgradeIsPermanent { get; }
        public bool? Permanent { get; }
        public int? SelectedPoolIndex { get; }
        public CardGenerationRule(string destination, int count, IReadOnlyList<CardCreationRule> pool,
            bool skipDuplicateInHand = false, bool requireHandSpace = false, bool copyModifiers = false,
            bool ignoreTemporaryModifiers = false, CardUpgradeModifier? upgrade = null,
            IReadOnlyList<DiscardGenerationUpgrade>? discardCopyUpgrades = null, IReadOnlyList<string>? externalInteractions = null,
            bool deterministicPoolSelection = false, bool requireCopyableSource = false, bool upgradeIsPermanent = false,
            bool? permanent = null, int? selectedPoolIndex = null)
        { Destination = destination; Count = count; Pool = Array.AsReadOnly(pool.ToArray()); SkipDuplicateInHand = skipDuplicateInHand;
            RequireHandSpace = requireHandSpace; CopyModifiers = copyModifiers; IgnoreTemporaryModifiers = ignoreTemporaryModifiers; Upgrade = upgrade;
            DiscardCopyUpgrades = Array.AsReadOnly((discardCopyUpgrades ?? Array.Empty<DiscardGenerationUpgrade>()).ToArray());
            ExternalInteractions = Array.AsReadOnly((externalInteractions ?? Array.Empty<string>()).ToArray());
            DeterministicPoolSelection = deterministicPoolSelection; RequireCopyableSource = requireCopyableSource;
            UpgradeIsPermanent = upgradeIsPermanent; Permanent = permanent; SelectedPoolIndex = selectedPoolIndex; }
        internal CardGenerationRule WithSelectedPoolIndex(int index) => new(Destination, Count, Pool, SkipDuplicateInHand,
            RequireHandSpace, CopyModifiers, IgnoreTemporaryModifiers, Upgrade, DiscardCopyUpgrades, ExternalInteractions,
            DeterministicPoolSelection, RequireCopyableSource, UpgradeIsPermanent, Permanent, index);
    }

    public sealed class CardGenerationResult
    {
        public CombatContext? Context { get; }
        public IReadOnlyList<CardToken> AddedCards { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => Context != null;
        internal CardGenerationResult(CombatContext? context, IReadOnlyList<CardToken> cards, string? error = null)
        { Context = context; AddedCards = Array.AsReadOnly(cards.ToArray()); UnsupportedReason = error; }
    }

    public static class CardGenerationModel
    {
        // A fallback spawner uses CardState.Setup without AddCardImpl ownership or callbacks.
        public static CardGenerationResult CreateDetached(CombatContext source, CardCreationRule creation)
        {
            string? descriptorError = CardBranchMaskModel.ValidateCreation(source, creation);
            if (descriptorError != null) return Unsupported(descriptorError);
            if (source.CardRegistry == null || source.CardInstances == null || source.NextCardId <= 0 ||
                source.NextCardId == int.MaxValue || source.CardRegistry.Any(card => card.InstanceId >= source.NextCardId))
                return Unsupported("Fresh detached card creation requires a complete identity registry.");
            var card = new CardInstanceState(source.NextCardId, creation.DataId, creation.StartingModifiers,
                CardModifiers.Empty(), 0, 0, 0, creation.ExternalInteractions, creation.EffectCounters,
                creation.DamageScalingTraits, creation.StatusScalingTraits, creation.UnitUpgradeScalingTraits,
                creation.CapacityScalingTraits, creation.EquippedUnitId,
                source.CardRegistry.Any(item => item.PlayedRoomUnitIds != null) ? Array.Empty<int>() : null,
                source.CardRegistry.Any(item => item.RawPlayedRoomUnitIds != null) ? Array.Empty<int>() : null, creation.MaskDescriptor,
                forgeCountScalingTraits: creation.ForgeCountScalingTraits, canBeCopied: creation.CanBeCopied,
                trainHealScalingTraits: creation.TrainHealScalingTraits);
            card = CardExhaustModel.InitializeCreated(card, source);
            string? error = CardModifierModel.UnsupportedReason(card);
            if (error != null) return Unsupported(error);
            return new CardGenerationResult(source.WithCardRegistry(source.CardRegistry.Concat(new[] { card }).ToArray())
                .WithNextCardId(source.NextCardId + 1), new[] { new CardToken(card.InstanceId, card.DataId) });
        }

        // CopyCardState creates a source reference without AddCardImpl ownership,
        // RNG selection, statistic tracking or one-shot generation upgrades.
        public static CardGenerationResult CloneDetached(CombatContext source, CardCreationRule creation, int sourceCardId)
        {
            string? descriptorError = CardBranchMaskModel.ValidateCreation(source, creation);
            if (descriptorError != null) return Unsupported(descriptorError);
            CardInstanceState? copying = source.FindCard(sourceCardId);
            if (copying == null || source.CardRegistry == null || source.CardInstances == null)
                return Unsupported("Detached card cloning requires a complete source and identity registry.");
            if (source.NextCardId <= 0 || source.NextCardId == int.MaxValue || source.CardRegistry.Any(card => card.InstanceId >= source.NextCardId))
                return Unsupported("Invalid detached card identity allocation.");
            if (copying.DataId != creation.DataId) return Unsupported("Detached clone definition differs from its source.");
            string? error = CardModifierModel.UnsupportedReason(copying);
            if (error != null) return Unsupported(error);
            // CopyCardState does not pass an exclusion list. Clone-exclusion flags
            // apply to AddCardImpl copies, while these detached references retain them.
            var ignored = new HashSet<string>(StringComparer.Ordinal);
            CardModifiers permanent = Copy(CardModifiers.Empty(), copying.Permanent, ignored);
            CardModifiers temporary = Copy(CardModifiers.Empty(), copying.Temporary, ignored);
            var clone = new CardInstanceState(source.NextCardId, creation.DataId, CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0,
                creation.ExternalInteractions, creation.EffectCounters, creation.DamageScalingTraits, creation.StatusScalingTraits,
                creation.UnitUpgradeScalingTraits, creation.CapacityScalingTraits, creation.EquippedUnitId,
                copying.PlayedRoomUnitIds == null ? null : Array.Empty<int>(),
                copying.RawPlayedRoomUnitIds == null ? null : Array.Empty<int>(), creation.MaskDescriptor,
                forgeCountScalingTraits: creation.ForgeCountScalingTraits, canBeCopied: creation.CanBeCopied,
                trainHealScalingTraits: creation.TrainHealScalingTraits);
            clone = CardExhaustModel.InitializeCreated(clone, source);
            foreach (CardUpgradeModifier upgrade in permanent.Upgrades)
            {
                error = CardExhaustModel.PermanentUpgradeError(clone, upgrade);
                if (error != null) return Unsupported(error);
            }
            clone = CardMagicPowerModel.Refresh(clone.WithModifiers(permanent, temporary));
            error = CardModifierModel.UnsupportedReason(clone);
            if (error != null) return Unsupported(error);
            CombatContext context = source.WithCardRegistry(source.CardRegistry.Concat(new[] { clone }).ToArray())
                .WithNextCardId(source.NextCardId + 1);
            return new CardGenerationResult(context, new[] { new CardToken(clone.InstanceId, clone.DataId) });
        }

        public static CardGenerationResult Apply(CombatContext source, CardGenerationRule rule, int sourceCardId = 0)
            => Apply(source, rule, sourceCardId, false);

        internal static CardGenerationResult ApplyPartFactory(CombatContext source, CardGenerationRule rule)
            => Apply(source, rule, 0, true);

        private static CardGenerationResult Apply(CombatContext source, CardGenerationRule rule, int sourceCardId,
            bool allowDeferredGraftedSpawnCard)
        {
            if (source.Cards.ExternalInteractions.Count > 0) return Unsupported(string.Join("; ", source.Cards.ExternalInteractions));
            if (rule.ExternalInteractions.Count > 0) return Unsupported(string.Join("; ", rule.ExternalInteractions));
            if (!new[] { "HandPile", "DiscardPile", "DeckPile", "DeckPileTop", "DeckPileRandom" }.Contains(rule.Destination))
                return Unsupported("Unmodeled generated card destination " + rule.Destination);
            if (rule.RequireCopyableSource)
            {
                CardInstanceState? sourceCard = source.FindCard(sourceCardId);
                if (sourceCard == null) return Unsupported("Copy generation requires a captured source card.");
                string? copyError = CopyabilityError(sourceCard);
                if (copyError != null) return Unsupported(copyError);
                if (sourceCard.CanBeCopied == false) return new CardGenerationResult(source, Array.Empty<CardToken>());
            }
            CombatContext context = source;
            var added = new List<CardToken>();
            for (int index = 0; index < Math.Max(1, rule.Count); index++)
            {
                if (rule.Pool.Count == 0) continue; // Native pool filtering failed: no selection or allocation.
                int poolIndex = 0;
                if (rule.SelectedPoolIndex is int selectedPoolIndex)
                {
                    if (selectedPoolIndex < 0 || selectedPoolIndex >= rule.Pool.Count)
                        return Unsupported("Captured generated-card pool selection is outside its native pool.");
                    poolIndex = selectedPoolIndex;
                }
                else if (!rule.DeterministicPoolSelection)
                {
                    RngDraw selected = context.BattleRng.Range(0, rule.Pool.Count);
                    context = context.WithBattleRng(selected.State);
                    poolIndex = selected.Value;
                }
                CardCreationRule creation = rule.Pool[poolIndex];
                if (rule.Destination == "HandPile" && (context.Cards.Hand.Count >= context.MaxHandSize ||
                    rule.SkipDuplicateInHand && context.Cards.Hand.Any(card => card.DataId == creation.DataId))) continue;
                string? descriptorError = CardBranchMaskModel.ValidateCreation(context, creation);
                if (descriptorError != null) return Unsupported(descriptorError);
                if (context.CardInstances == null && (HasModifiers(creation.StartingModifiers) || creation.EffectCounters?.Count > 0 || creation.DamageScalingTraits?.Count > 0 || creation.StatusScalingTraits?.Count > 0 || creation.UnitUpgradeScalingTraits?.Count > 0 || creation.ForgeCountScalingTraits?.Count > 0 || creation.TrainHealScalingTraits?.Count > 0 ||
                    rule.Upgrade != null || context.NextAddedTemporaryUpgrades?.Count > 0 || rule.CopyModifiers && context.FindCard(sourceCardId) != null))
                    return Unsupported("Modified generation requires complete card instance state.");
                CardModifiers permanent = Copy(CardModifiers.Empty(), creation.StartingModifiers, new HashSet<string>(StringComparer.Ordinal)),
                    temporary = CardModifiers.Empty();
                CardInstanceState candidate = new(context.NextCardId, creation.DataId, permanent, temporary, 0, 0, 0,
                    creation.ExternalInteractions, creation.EffectCounters, creation.DamageScalingTraits, creation.StatusScalingTraits, creation.UnitUpgradeScalingTraits, creation.CapacityScalingTraits, creation.EquippedUnitId,
                    context.CardRegistry?.Any(card => card.PlayedRoomUnitIds != null) == true ? Array.Empty<int>() : null,
                    context.CardRegistry?.Any(card => card.RawPlayedRoomUnitIds != null) == true ? Array.Empty<int>() : null, creation.MaskDescriptor,
                    forgeCountScalingTraits: creation.ForgeCountScalingTraits,
                    trainHealScalingTraits: creation.TrainHealScalingTraits);
                void RefreshUpgrades()
                {
                    context = CardMagicPowerModel.Refresh(context, candidate.WithModifiers(permanent, temporary), out CardInstanceState refreshed);
                    candidate = refreshed;
                    permanent = refreshed.Permanent; temporary = refreshed.Temporary;
                }
                candidate = CardExhaustModel.InitializeCreated(candidate, context);
                string? error = CardModifierModel.UnsupportedReason(candidate);
                if (error != null && !(allowDeferredGraftedSpawnCard &&
                    error == "Unmodeled unit card statistics; Grafted equipment" &&
                    RelicCardGenerationModel.IsPartFactoryGraftedCard(candidate))) return Unsupported(error);
                // AddCardImpl applies the manager's current relics before optional,
                // copied, discard-conditional and pending temporary upgrades.
                if (context.Relics?.Any(relic => relic.CardModifiers?.Count > 0 || relic.CardHealthModifiers?.Count > 0 ||
                    relic.CardAttackModifiers?.Count > 0 ||
                    relic.MagicPowerModifiers?.Count > 0 || relic.CardTypeCosts?.Count > 0) == true)
                {
                    var relicModifiers = RelicCardModifierModel.Apply(candidate, context.Relics, context: context);
                    if (!relicModifiers.Supported) return Unsupported(relicModifiers.UnsupportedReason!);
                    permanent = relicModifiers.Card!.Permanent;
                    temporary = relicModifiers.Card.Temporary;
                    context = relicModifiers.Context ?? context;
                }
                if (rule.Upgrade != null)
                {
                    if (rule.Upgrade.ExternalInteractions.Count > 0) return Unsupported(string.Join("; ", rule.Upgrade.ExternalInteractions));
                    if (rule.UpgradeIsPermanent)
                    {
                        error = CardExhaustModel.PermanentUpgradeError(candidate, rule.Upgrade);
                        if (error != null) return Unsupported(error);
                        permanent = UnitModifierModel.Add(permanent, rule.Upgrade.WithSharedReference(null));
                    }
                    else temporary = UnitModifierModel.Add(temporary, rule.Upgrade.WithSharedReference(null));
                    RefreshUpgrades();
                }
                CardInstanceState? copying = rule.CopyModifiers ? context.FindCard(sourceCardId) : null;
                if (copying != null)
                {
                    error = CardModifierModel.UnsupportedReason(copying);
                    if (error != null) return Unsupported(error);
                    if (rule.Destination == "DiscardPile")
                        foreach (DiscardGenerationUpgrade conditional in rule.DiscardCopyUpgrades.Where(item => item.RequiredUpgradeId.Length == 0 ||
                            copying.Permanent.Upgrades.Concat(copying.Temporary.Upgrades).Any(upgrade => upgrade.DataId == item.RequiredUpgradeId)))
                        {
                            CardUpgradeModifier upgrade = conditional.Upgrade;
                            if (upgrade.ExternalInteractions.Count > 0) return Unsupported(string.Join("; ", upgrade.ExternalInteractions));
                            temporary = UnitModifierModel.Add(temporary, upgrade.WithSharedReference(null));
                            RefreshUpgrades();
                        }
                    foreach (CardUpgradeModifier upgrade in copying.Permanent.Upgrades)
                    {
                        error = CardExhaustModel.PermanentUpgradeError(candidate, upgrade);
                        if (error != null) return Unsupported(error);
                    }
                    // Same-card clones retain their own starting upgrades, rather than copying a second copy.
                    var ignored = new HashSet<string>(copying.DataId == creation.DataId ? permanent.Upgrades.Select(upgrade => upgrade.DataId) :
                        Array.Empty<string>(), StringComparer.Ordinal);
                    foreach (CardUpgradeModifier upgrade in copying.Permanent.Upgrades.Where(upgrade => upgrade.ExcludeFromClones)) ignored.Add(upgrade.DataId);
                    permanent = Copy(permanent, copying.Permanent, ignored, forAddCardPipeline: true);
                    if (!rule.IgnoreTemporaryModifiers)
                    {
                        foreach (CardUpgradeModifier upgrade in copying.Temporary.Upgrades.Where(upgrade => upgrade.ExcludeFromClones)) ignored.Add(upgrade.DataId);
                        temporary = Copy(temporary, copying.Temporary, ignored, forAddCardPipeline: true);
                    }
                    // Copying and each helper application refresh all installed
                    // upgrades against the destination card's current traits.
                    RefreshUpgrades();
                }
                foreach (CardUpgradeModifier upgrade in context.NextAddedTemporaryUpgrades ?? Array.Empty<CardUpgradeModifier>())
                {
                    if (upgrade.ExternalInteractions.Count > 0) return Unsupported(string.Join("; ", upgrade.ExternalInteractions));
                    temporary = UnitModifierModel.Add(temporary, upgrade);
                    RefreshUpgrades();
                }
                var card = new CardToken(context.NextCardId, creation.DataId);
                var hand = context.Cards.Hand.ToList(); var draw = context.Cards.Draw.ToList(); var discard = context.Cards.Discard.ToList();
                UnityRng rng = context.BattleRng;
                switch (rule.Destination)
                {
                    case "HandPile": hand.Insert(0, card); break;
                    case "DiscardPile": discard.Insert(0, card); break;
                    case "DeckPileTop": draw.Add(card); break;
                    case "DeckPileRandom":
                        RngDraw position = rng.Range(0, draw.Count); rng = position.State;
                        draw.Insert(position.Value, card); break;
                    default: draw.Insert(0, card); break;
                }
                candidate = new CardInstanceState(card.InstanceId, card.DataId, permanent, temporary, 0, 0, 0,
                    creation.ExternalInteractions, creation.EffectCounters, creation.DamageScalingTraits, creation.StatusScalingTraits, creation.UnitUpgradeScalingTraits, creation.CapacityScalingTraits, creation.EquippedUnitId,
                    context.CardRegistry?.Any(card => card.PlayedRoomUnitIds != null) == true ? Array.Empty<int>() : null,
                    context.CardRegistry?.Any(card => card.RawPlayedRoomUnitIds != null) == true ? Array.Empty<int>() : null, candidate.MaskDescriptor,
                    forgeCountScalingTraits: creation.ForgeCountScalingTraits, canBeCopied: creation.CanBeCopied,
                    trainHealScalingTraits: creation.TrainHealScalingTraits);
                candidate = CardExhaustModel.InitializeCreated(candidate, context);
                context = new CombatContext(new CardCycleState(hand, draw, discard, context.Cards.Rng, context.Cards.DrawModifier,
                    context.Cards.ExternalInteractions, context.Cards.BonusDraw, context.Cards.NextDrawUpgrades, context.Cards.NextUpgradeReferenceId), rng, context.Gold, checked(context.NextCardId + 1), context.MaxHandSize,
                    context.StatusRules, context.Statistics?.TrackCards(new[] { card.InstanceId }),
                    context.CardInstances?.Concat(new[] { candidate }).ToArray(), context.CardRegistry, context.AllScenarioBossesDead,
                    context.NextAddedTemporaryUpgrades == null ? null : Array.Empty<CardUpgradeModifier>(), context.OtherPiles, context.QueryFrame, context.KillCamActivated, context.MagicPower, context.IsolatedBattlePreview, context.EnergyState, context.RoomCapacities, context.AbilityCardCache, context.LastAbilityActivatorUnitId, context.PermanentlyDisabledAbilities, context.LastSpawnedUnitId, context.NextUnitId, context.SpawnPoints, context.SummonCatalog, context.Enchantments, context.PurifyBlockedTriggers, context.Relics, context.TriggerCounts, context.Resources, context.MixState);
                // Setup/upgrade refresh builds scaling trait text in an active battle.
                // Those native statistic queries refresh deckStats after the new card
                // enters its pile, even when the displayed scaling value is zero.
                if (context.QueryFrame?.ActiveBattle == true && context.Statistics?.StoredCards != null &&
                    (creation.DamageScalingTraits?.Count > 0 || creation.StatusScalingTraits?.Count > 0 || creation.UnitUpgradeScalingTraits?.Count > 0 ||
                        creation.ForgeCountScalingTraits?.Count > 0 || creation.TrainHealScalingTraits?.Count > 0 ||
                        creation.MaskDescriptor?.BaseTraits.Any(trait => trait.RuntimeType == "CardTraitShowCardTargets") == true &&
                        creation.MaskDescriptor.Definition.MainEffectNames.Contains("CardEffectDrawType")))
                    context = context.WithStatistics(context.Statistics.RefreshOwnedCards(context.CardInstances!.Select(card => card.InstanceId)));
                added.Add(card);
            }
            if (rule.Permanent == true && added.Count > 0 && context.Resources != null)
                context = context.WithResources(context.Resources.WithVisibleDeckCount(
                    checked(context.Resources.VisibleDeckCount + added.Count)));
            return new CardGenerationResult(context, added);
        }
        private static CardModifiers Copy(CardModifiers destination, CardModifiers source, HashSet<string> ignored,
            bool forAddCardPipeline = false)
        {
            CardModifiers result = new CardModifiers(source.Offsets, destination.Upgrades, source.PersistentHealth, source.ExternalInteractions);
            foreach (CardUpgradeModifier upgrade in source.Upgrades.Where(upgrade => !ignored.Contains(upgrade.DataId)))
                result = UnitModifierModel.Add(result, forAddCardPipeline ? upgrade.ForAddCardCopy() : upgrade.WithSharedReference(null));
            return result;
        }
        private static bool HasModifiers(CardModifiers modifiers) => modifiers.Upgrades.Count > 0 || modifiers.PersistentHealth != 0 ||
            new[] { "Damage", "Health", "Cost", "Heal", "Size", "XCost", "EquipmentLimit", "UpgradeSlotCount" }
                .Any(stat => modifiers.Offsets.Value(stat) != 0);
        private static string? CopyabilityError(CardInstanceState card)
        {
            if (!card.CanBeCopied.HasValue) return "Copy generation requires captured CardState.CanBeCopied state.";
            if (card.MaskDescriptor == null) return "Copy generation requires branch-owned source traits.";
            if (card.MaskDescriptor.TemporaryTraits.Count > 0 || card.MaskDescriptor.PermanentReplacements.Count > 0 ||
                card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade =>
                    upgrade.Lifecycle?.AddedTraits.Count > 0 || upgrade.Lifecycle?.RemovedRuntimeTypes.Count > 0))
                return "Copyability changes from card trait upgrades are not modeled.";
            return null;
        }
        private static CardGenerationResult Unsupported(string error) => new CardGenerationResult(null, Array.Empty<CardToken>(), error);
    }
}

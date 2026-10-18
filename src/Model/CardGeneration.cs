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
        public IReadOnlyList<CardUpgradeModifier> AddCardUpgrades { get; }
        public CardCreationRule(string dataId, CardModifiers startingModifiers, IReadOnlyList<CardEffectCounter>? effectCounters,
            IReadOnlyList<string> externalInteractions, IReadOnlyList<ScalingDamageTrait>? damageScalingTraits = null,
            IReadOnlyList<ScalingStatusTrait>? statusScalingTraits = null, IReadOnlyList<ScalingUnitUpgradeTrait>? unitUpgradeScalingTraits = null,
            IReadOnlyList<ScalingCapacityTrait>? capacityScalingTraits = null, int? equippedUnitId = null, CardMaskDescriptor? maskDescriptor = null,
            IReadOnlyList<ScalingForgeCountTrait>? forgeCountScalingTraits = null, bool? canBeCopied = null,
            IReadOnlyList<ScalingTrainHealTrait>? trainHealScalingTraits = null, IReadOnlyList<CardUpgradeModifier>? addCardUpgrades = null)
        { DataId = dataId; StartingModifiers = startingModifiers; EffectCounters = effectCounters == null ? null : Array.AsReadOnly(effectCounters.ToArray());
            ExternalInteractions = Array.AsReadOnly(externalInteractions.ToArray());
            DamageScalingTraits = damageScalingTraits == null ? null : Array.AsReadOnly(damageScalingTraits.ToArray());
            StatusScalingTraits = statusScalingTraits == null ? null : Array.AsReadOnly(statusScalingTraits.ToArray());
            UnitUpgradeScalingTraits = unitUpgradeScalingTraits == null ? null : Array.AsReadOnly(unitUpgradeScalingTraits.ToArray());
            CapacityScalingTraits = capacityScalingTraits == null ? null : Array.AsReadOnly(capacityScalingTraits.ToArray());
            ForgeCountScalingTraits = forgeCountScalingTraits == null ? null : Array.AsReadOnly(forgeCountScalingTraits.ToArray());
            TrainHealScalingTraits = trainHealScalingTraits == null ? null : Array.AsReadOnly(trainHealScalingTraits.ToArray());
            EquippedUnitId = equippedUnitId; MaskDescriptor = maskDescriptor; CanBeCopied = canBeCopied;
            AddCardUpgrades = Array.AsReadOnly((addCardUpgrades ?? Array.Empty<CardUpgradeModifier>()).ToArray()); }
        internal CardCreationRule WithAddCardUpgrades(IReadOnlyList<CardUpgradeModifier> upgrades) => new(DataId,
            StartingModifiers, EffectCounters, ExternalInteractions, DamageScalingTraits, StatusScalingTraits,
            UnitUpgradeScalingTraits, CapacityScalingTraits, EquippedUnitId, MaskDescriptor, ForgeCountScalingTraits,
            CanBeCopied, TrainHealScalingTraits, upgrades);
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
        public bool? IgnoreIfEphemeral { get; }
        public bool? OnlyUnequippedTargets { get; }
        public int? SelectedPoolIndex { get; }
        public IReadOnlyList<bool>? PoolEligibility { get; }
        public IReadOnlyList<string>? ChampionCardDataIds { get; }
        public IReadOnlyList<CardUpgradeMaskRule>? OptionalUpgradeFilters { get; }
        public CardGenerationRule(string destination, int count, IReadOnlyList<CardCreationRule> pool,
            bool skipDuplicateInHand = false, bool requireHandSpace = false, bool copyModifiers = false,
            bool ignoreTemporaryModifiers = false, CardUpgradeModifier? upgrade = null,
            IReadOnlyList<DiscardGenerationUpgrade>? discardCopyUpgrades = null, IReadOnlyList<string>? externalInteractions = null,
            bool deterministicPoolSelection = false, bool requireCopyableSource = false, bool upgradeIsPermanent = false,
            bool? permanent = null, int? selectedPoolIndex = null, IReadOnlyList<bool>? poolEligibility = null,
            IReadOnlyList<string>? championCardDataIds = null, bool? ignoreIfEphemeral = null, bool? onlyUnequippedTargets = null,
            IReadOnlyList<CardUpgradeMaskRule>? optionalUpgradeFilters = null)
        { Destination = destination; Count = count; Pool = Array.AsReadOnly(pool.ToArray()); SkipDuplicateInHand = skipDuplicateInHand;
            RequireHandSpace = requireHandSpace; CopyModifiers = copyModifiers; IgnoreTemporaryModifiers = ignoreTemporaryModifiers; Upgrade = upgrade;
            DiscardCopyUpgrades = Array.AsReadOnly((discardCopyUpgrades ?? Array.Empty<DiscardGenerationUpgrade>()).ToArray());
            ExternalInteractions = Array.AsReadOnly((externalInteractions ?? Array.Empty<string>()).ToArray());
            DeterministicPoolSelection = deterministicPoolSelection; RequireCopyableSource = requireCopyableSource;
            UpgradeIsPermanent = upgradeIsPermanent; Permanent = permanent; IgnoreIfEphemeral = ignoreIfEphemeral; SelectedPoolIndex = selectedPoolIndex;
            OnlyUnequippedTargets = onlyUnequippedTargets;
            OptionalUpgradeFilters = optionalUpgradeFilters == null ? null : Array.AsReadOnly(optionalUpgradeFilters.ToArray());
            PoolEligibility = poolEligibility == null ? null : Array.AsReadOnly(poolEligibility.ToArray());
            ChampionCardDataIds = championCardDataIds == null ? null : Array.AsReadOnly(championCardDataIds.Distinct(StringComparer.Ordinal).ToArray()); }
        public bool ShouldSerializePoolEligibility() => PoolEligibility != null;
        internal CardGenerationRule WithSelectedPoolIndex(int index) => new(Destination, Count, Pool, SkipDuplicateInHand,
            RequireHandSpace, CopyModifiers, IgnoreTemporaryModifiers, Upgrade, DiscardCopyUpgrades, ExternalInteractions,
            DeterministicPoolSelection, RequireCopyableSource, UpgradeIsPermanent, Permanent, index, PoolEligibility, ChampionCardDataIds, IgnoreIfEphemeral, OnlyUnequippedTargets, OptionalUpgradeFilters);
        internal CardGenerationRule WithPoolEligibility(IReadOnlyList<bool> eligibility) => new(Destination, Count, Pool,
            SkipDuplicateInHand, RequireHandSpace, CopyModifiers, IgnoreTemporaryModifiers, Upgrade, DiscardCopyUpgrades,
            ExternalInteractions, DeterministicPoolSelection, RequireCopyableSource, UpgradeIsPermanent, Permanent,
            SelectedPoolIndex, eligibility, ChampionCardDataIds, IgnoreIfEphemeral, OnlyUnequippedTargets, OptionalUpgradeFilters);
        internal CardGenerationRule ForTargetCount(int count) => new(Destination, count, Pool, SkipDuplicateInHand,
            RequireHandSpace, CopyModifiers, IgnoreTemporaryModifiers, Upgrade, DiscardCopyUpgrades, ExternalInteractions,
            DeterministicPoolSelection, RequireCopyableSource, UpgradeIsPermanent, Permanent, SelectedPoolIndex,
            PoolEligibility, ChampionCardDataIds, IgnoreIfEphemeral, optionalUpgradeFilters: OptionalUpgradeFilters);
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
            if (CardGraftedEquipmentModel.Present(copying)) return Unsupported("Grafted equipment cloning requires native private-state calibration.");
            if (copying.Permanent.MergedEquipment != null) return Unsupported("Merged equipment cloning requires separate native calibration.");
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

        internal static CardGenerationResult ApplyCachedCopy(CombatContext source, CardCreationRule creation, CardInstanceState copying)
            => Apply(source, new CardGenerationRule("HandPile", 1, new[] { creation },
                copyModifiers: true, deterministicPoolSelection: true), 0, false, copying);

        public static CardGenerationResult ApplyForTargets(CombatContext source, CardGenerationRule rule,
            IReadOnlyList<CombatUnit> targets, int sourceCardId = 0)
        {
            if (!rule.OnlyUnequippedTargets.HasValue) return Unsupported("Missing per-target generation rule.");
            if (rule.OnlyUnequippedTargets.Value && targets.Any(unit => unit.EquipmentCards == null))
                return Unsupported("Unequipped generation requires captured equipment membership.");
            int count = targets.Count(unit => !rule.OnlyUnequippedTargets.Value || unit.EquipmentCards!.Count == 0);
            if (count == 0) return new CardGenerationResult(source, Array.Empty<CardToken>());
            long total = (long)Math.Max(1, rule.Count) * count;
            if (total > 4096) return Unsupported("Per-target generation exceeds the calibrated allocation bound.");
            return Apply(source, rule.ForTargetCount((int)total), sourceCardId);
        }

        internal static CardGenerationResult ApplyPartFactory(CombatContext source, CardGenerationRule rule)
            => Apply(source, rule, 0, true);

        private static CardGenerationResult Apply(CombatContext source, CardGenerationRule rule, int sourceCardId,
            bool allowDeferredGraftedSpawnCard, CardInstanceState? cachedCopy = null)
        {
            if (rule.OnlyUnequippedTargets.HasValue) return Unsupported("Per-target generation requires captured target units.");
            if (rule.IgnoreIfEphemeral == true && source.FindCard(sourceCardId) is CardInstanceState parent &&
                CardBranchMaskModel.Resolve(parent).Traits.Contains("CardTraitEphemeral"))
                return new CardGenerationResult(source, Array.Empty<CardToken>());
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
                bool overflow = rule.Permanent == true && rule.Destination == "HandPile" && context.Cards.Hand.Count >= context.MaxHandSize;
                if (overflow && (context.OtherPiles?.Any(pile => pile.Name == "Standby") != true ||
                    context.OtherPiles?.Any(pile => pile.Name == "Exhausted") != true))
                    return Unsupported("Permanent hand overflow requires standby and exhausted piles.");
                if (rule.Destination == "HandPile" && (rule.Permanent != true && context.Cards.Hand.Count >= context.MaxHandSize ||
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
                foreach (CardUpgradeModifier upgrade in creation.AddCardUpgrades)
                {
                    if (upgrade.ExternalInteractions.Count > 0)
                        return Unsupported("Champion grant upgrades with external interactions are not modeled: " +
                            string.Join("; ", upgrade.ExternalInteractions));
                    error = CardExhaustModel.PermanentUpgradeError(candidate, upgrade);
                    if (error != null) return Unsupported(error);
                    permanent = UnitModifierModel.Add(permanent, upgrade.WithSharedReference(null));
                    RefreshUpgrades();
                }
                bool applyOptionalUpgrade = rule.Upgrade != null;
                if (applyOptionalUpgrade && rule.OptionalUpgradeFilters?.Count > 0)
                {
                    if (candidate.MaskDescriptor == null) return Unsupported("Optional generation-upgrade filters require captured card metadata.");
                    CardUpgradeMaskCard maskCard = CardBranchMaskModel.Resolve(candidate.WithModifiers(permanent, temporary));
                    applyOptionalUpgrade = rule.OptionalUpgradeFilters.All(filter => CardUpgradeMaskModel.FilterCard(filter, maskCard));
                }
                if (applyOptionalUpgrade)
                {
                    if (rule.Upgrade!.ExternalInteractions.Count > 0) return Unsupported(string.Join("; ", rule.Upgrade.ExternalInteractions));
                    UnitUpgradeScalingResult scaled = CardConsumedUpgradeModel.Apply(context, sourceCardId, sourceCardId, rule.Upgrade);
                    if (!scaled.Supported) return Unsupported(scaled.UnsupportedReason!);
                    context = scaled.Context!;
                    CardUpgradeModifier optionalUpgrade = scaled.Upgrade!;
                    if (rule.UpgradeIsPermanent)
                    {
                        error = CardExhaustModel.PermanentUpgradeError(candidate, optionalUpgrade);
                        if (error != null) return Unsupported(error);
                        permanent = UnitModifierModel.Add(permanent, optionalUpgrade.WithSharedReference(null));
                    }
                    else temporary = UnitModifierModel.Add(temporary, optionalUpgrade.WithSharedReference(null));
                    RefreshUpgrades();
                }
                CardInstanceState? copying = rule.CopyModifiers ? cachedCopy ?? context.FindCard(sourceCardId) : null;
                if (copying?.Permanent.MergedEquipment != null) return Unsupported("Merged equipment copying requires separate native calibration.");
                if (copying != null)
                {
                    if (CardGraftedEquipmentModel.Present(copying)) return Unsupported("Grafted equipment copying requires native private-state calibration.");
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
                    var ignored = new HashSet<string>(cachedCopy == null && copying.DataId == creation.DataId ? permanent.Upgrades.Select(upgrade => upgrade.DataId) :
                        Array.Empty<string>(), StringComparer.Ordinal);
                    if (cachedCopy == null)
                        foreach (CardUpgradeModifier upgrade in copying.Permanent.Upgrades.Where(upgrade => upgrade.ExcludeFromClones)) ignored.Add(upgrade.DataId);
                    permanent = Copy(permanent, copying.Permanent, ignored, forAddCardPipeline: true);
                    if (!rule.IgnoreTemporaryModifiers)
                    {
                        if (cachedCopy == null)
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
                    case "HandPile": if (!overflow) hand.Insert(0, card); break;
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
                    context.Cards.ExternalInteractions, context.Cards.BonusDraw, context.Cards.NextDrawUpgrades, context.Cards.NextUpgradeReferenceId, context.Cards.DeckDrawDisabledThisTurn), rng, context.Gold, checked(context.NextCardId + 1), context.MaxHandSize,
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
                if (overflow)
                    context = context.WithOtherPiles(context.OtherPiles!.Select(pile => pile.Name == "Standby"
                        ? CardPileModel.BindExhaust(CardPileModel.Add(pile, card), new CardExhaustStandbyCondition(card.InstanceId, null, keepInStandBy: true))
                        : pile.Name == "Exhausted" ? CardPileModel.Add(pile, card) : pile).ToArray())
                        .WithStatistics(context.Statistics?.Increment(card.InstanceId, "TimesExhausted"));
                added.Add(card);
            }
            if (rule.Permanent == true && added.Count > 0 && context.Resources != null)
                context = context.WithResources(context.Resources.WithVisibleDeckCount(
                    checked(context.Resources.VisibleDeckCount + added.Count)));
            if (rule.Permanent == true && added.Count > 0 && context.Statistics?.DeckCards != null)
                context = context.WithStatistics(context.Statistics.AddDeckCards(added.Select(card => card.InstanceId)));
            return new CardGenerationResult(context, added);
        }
        private static CardModifiers Copy(CardModifiers destination, CardModifiers source, HashSet<string> ignored,
            bool forAddCardPipeline = false)
        {
            IReadOnlyList<BoxedCardState>? boxes = source.BoxedCards == null && destination.BoxedCards == null ? null :
                (destination.BoxedCards ?? Array.Empty<BoxedCardState>()).Concat(source.BoxedCards ?? Array.Empty<BoxedCardState>()).ToArray();
            CardModifiers result = new CardModifiers(source.Offsets, destination.Upgrades, source.PersistentHealth, source.ExternalInteractions, boxes, source.MergedEquipment ?? destination.MergedEquipment);
            foreach (CardUpgradeModifier upgrade in source.Upgrades.Where(upgrade => !ignored.Contains(upgrade.DataId)))
                result = UnitModifierModel.Add(result, forAddCardPipeline ? upgrade.ForAddCardCopy() : upgrade.WithSharedReference(null));
            return result;
        }
        private static bool HasModifiers(CardModifiers modifiers) => modifiers.Upgrades.Count > 0 || modifiers.PersistentHealth != 0 || modifiers.BoxedCards?.Count > 0 ||
            new[] { "Damage", "Health", "Cost", "Heal", "Size", "XCost", "EquipmentLimit", "UpgradeSlotCount" }
                .Any(stat => modifiers.Offsets.Value(stat) != 0);
        private static string? CopyabilityError(CardInstanceState card)
        {
            if (CardGraftedEquipmentModel.Present(card)) return "Grafted equipment copying requires native private-state calibration.";
            if (!card.CanBeCopied.HasValue) return "Copy generation requires captured CardState.CanBeCopied state.";
            if (card.MaskDescriptor == null) return "Copy generation requires branch-owned source traits.";
            // Freeze and Ephemeral inherit GetIsCopyable=true; their discard/play
            // callbacks do not invalidate the captured CanBeCopied query.
            bool Neutral(CardTraitValue trait) => trait.RuntimeType is "CardTraitFreeze" or "CardTraitEphemeral" && trait.DeclaredName == trait.RuntimeType;
            if (card.MaskDescriptor.TemporaryTraits.Any(trait => !Neutral(trait)) || card.MaskDescriptor.PermanentReplacements.Count > 0 ||
                card.Permanent.Upgrades.Concat(card.Temporary.Upgrades).Any(upgrade =>
                    upgrade.Lifecycle?.AddedTraits.Any(trait => !Neutral(trait)) == true || upgrade.Lifecycle?.RemovedRuntimeTypes.Count > 0))
                return "Copyability changes from card trait upgrades are not modeled.";
            return null;
        }
        private static CardGenerationResult Unsupported(string error) => new CardGenerationResult(null, Array.Empty<CardToken>(), error);
    }
}

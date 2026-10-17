using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicCardHealthModifier
    {
        public int EffectIndex { get; }
        public string CharacterName { get; }
        public string CharacterSubtypeKey { get; }
        public int HealthModifier { get; }
        public int HealthMultiplier { get; }
        public string CardType { get; }
        public bool MultiplyByUpgradeCount { get; }
        public string EffectType { get; }
        public string CardRarityType { get; }
        public string SourceTeam { get; }
        public string Trigger { get; }
        public IReadOnlyList<RelicConditionState> Conditions { get; }
        public int ConditionCount { get; }
        public RelicCardHealthModifier(int effectIndex, string characterName, string characterSubtypeKey, int healthModifier,
            int healthMultiplier, string cardType, bool multiplyByUpgradeCount,
            IReadOnlyList<RelicConditionState>? conditions = null, int conditionCount = 0,
            string effectType = RelicModel.ModifyCardHealth, string cardRarityType = "", string sourceTeam = "", string trigger = "")
        { EffectIndex = effectIndex; CharacterName = characterName; CharacterSubtypeKey = characterSubtypeKey;
            HealthModifier = healthModifier; HealthMultiplier = healthMultiplier; CardType = cardType;
            MultiplyByUpgradeCount = multiplyByUpgradeCount; EffectType = effectType; CardRarityType = cardRarityType;
            SourceTeam = sourceTeam; Trigger = trigger;
            Conditions = Array.AsReadOnly((conditions ?? Array.Empty<RelicConditionState>()).ToArray());
            ConditionCount = conditions?.Count ?? conditionCount; }
    }

    public sealed class RelicMoonPhaseHandUpgrade
    {
        public int MoonPhaseMask { get; }
        public bool RemoveOnDiscard { get; }
        public bool CanApplyInPreviewMode { get; }
        public int ConditionCount { get; }
        public CardUpgradeModifier Upgrade { get; }
        public IReadOnlyList<CardUpgradeMaskRule> Filters { get; }
        public RelicMoonPhaseHandUpgrade(int moonPhaseMask, bool removeOnDiscard, bool canApplyInPreviewMode,
            int conditionCount, CardUpgradeModifier upgrade, IReadOnlyList<CardUpgradeMaskRule> filters)
        { MoonPhaseMask = moonPhaseMask; RemoveOnDiscard = removeOnDiscard; CanApplyInPreviewMode = canApplyInPreviewMode;
            ConditionCount = conditionCount; Upgrade = upgrade; Filters = Array.AsReadOnly(filters.ToArray()); }
    }

    public sealed class RelicCardModifier
    {
        public int EffectIndex { get; }
        public RelicCardUpgradeRule? Rule { get; }
        public CardUpgradeModifier? Upgrade { get; }
        public bool ApplyToCardlessSpawns { get; }
        public int ConditionCount { get; }
        public IReadOnlyList<RelicConditionState> Conditions { get; }
        public RelicTraitReplacement? TraitReplacement { get; }
        public RelicNoUpgradeCardCostModifier? NoUpgradeCost { get; }
        public RelicMoonPhaseHandUpgrade? MoonPhaseHandUpgrade { get; }
        public RelicCardModifier(int effectIndex, RelicCardUpgradeRule? rule, CardUpgradeModifier? upgrade,
            bool applyToCardlessSpawns, IReadOnlyList<RelicConditionState>? conditions = null, int conditionCount = 0,
            RelicTraitReplacement? traitReplacement = null, RelicNoUpgradeCardCostModifier? noUpgradeCost = null,
            RelicMoonPhaseHandUpgrade? moonPhaseHandUpgrade = null)
        { EffectIndex = effectIndex; Rule = rule; Upgrade = upgrade; ApplyToCardlessSpawns = applyToCardlessSpawns;
            Conditions = Array.AsReadOnly((conditions ?? Array.Empty<RelicConditionState>()).ToArray());
            ConditionCount = conditions?.Count ?? conditionCount; TraitReplacement = traitReplacement; NoUpgradeCost = noUpgradeCost;
            MoonPhaseHandUpgrade = moonPhaseHandUpgrade; }
        internal RelicCardModifier WithConditions(IReadOnlyList<RelicConditionState> conditions) =>
            new RelicCardModifier(EffectIndex, Rule, Upgrade, ApplyToCardlessSpawns, conditions,
                traitReplacement: TraitReplacement, noUpgradeCost: NoUpgradeCost, moonPhaseHandUpgrade: MoonPhaseHandUpgrade);
    }
    public sealed class RelicNoUpgradeCardCostModifier
    {
        public int EffectIndex { get; }
        public string ExclusiveType { get; }
        public int CostChangeAmount { get; }
        public CardUpgradeModifier Upgrade { get; }
        public RelicNoUpgradeCardCostModifier(int effectIndex, string exclusiveType, int costChangeAmount,
            CardUpgradeModifier upgrade)
        { EffectIndex = effectIndex; ExclusiveType = exclusiveType; CostChangeAmount = costChangeAmount; Upgrade = upgrade; }
    }
    public sealed class RelicTraitReplacement
    {
        public string SourceTrait { get; }
        public string TargetTrait { get; }
        public CardUpgradeMaskRule? CardFilter { get; }
        public CardUpgradeMaskRule? CardFilterSecondary { get; }
        public RelicTraitReplacement(string sourceTrait, string targetTrait, CardUpgradeMaskRule? cardFilter,
            CardUpgradeMaskRule? cardFilterSecondary)
        { SourceTrait = sourceTrait; TargetTrait = targetTrait; CardFilter = cardFilter; CardFilterSecondary = cardFilterSecondary; }
    }
    public sealed class RelicCardModifierDispatch
    {
        public int RelicIndex { get; }
        public int EffectIndex { get; }
        public bool Returned { get; }
        public bool UpgradeAdded { get; }
        public IReadOnlyList<RelicCardFilterResult> Filters { get; }
        public RelicCardModifierDispatch(int relicIndex, int effectIndex, bool returned, bool upgradeAdded,
            IReadOnlyList<RelicCardFilterResult> filters)
        { RelicIndex = relicIndex; EffectIndex = effectIndex; Returned = returned; UpgradeAdded = upgradeAdded;
            Filters = Array.AsReadOnly(filters.ToArray()); }
    }
    public sealed class RelicCardModifierResult
    {
        public CardInstanceState? Card { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => Card != null;
        public IReadOnlyList<RelicCardModifierDispatch> Dispatches { get; }
        public CombatContext? Context { get; }
        public RelicCardModifierResult(CardInstanceState? card, IReadOnlyList<RelicCardModifierDispatch>? dispatches = null,
            string? unsupportedReason = null, CombatContext? context = null)
        { Card = card; Dispatches = Array.AsReadOnly((dispatches ?? Array.Empty<RelicCardModifierDispatch>()).ToArray());
            UnsupportedReason = unsupportedReason; Context = context; }
    }
    public static class RelicCardModifierModel
    {
        internal static string? Validate(CombatRelicState relic)
        {
            var indices = relic.EffectTypes.Select((type, index) => (type, index)).Where(item =>
                    item.type == RelicModel.AddTempUpgrade || item.type == RelicModel.ModifyTrait ||
                    item.type == RelicModel.ModifyCardCostIfNoUpgrades || item.type == RelicModel.ApplyCardUpgradeToHandEndOfTurnOnMoon)
                .Select(item => item.index).ToArray();
            if (indices.Length == 0) return relic.CardModifiers?.Count > 0 ? "Unexpected relic card modifier definitions." : null;
            if (relic.CardModifiers == null || !indices.SequenceEqual(relic.CardModifiers.Select(rule => rule.EffectIndex)))
                return "Missing ordered native relic card modifier definitions.";
            foreach (var effect in relic.CardModifiers)
            {
                string type = relic.EffectTypes[effect.EffectIndex];
                if (type == RelicModel.ModifyTrait)
                {
                    RelicTraitReplacement? replacement = effect.TraitReplacement;
                    if (relic.DataId != RelicModel.PurgeToExhaustId || relic.AssetKey != RelicModel.PurgeToExhaustKey ||
                        relic.EffectTypes.Count != 1 || effect.EffectIndex != 0 || replacement == null || effect.Rule != null ||
                        effect.Upgrade != null || effect.ApplyToCardlessSpawns || effect.ConditionCount != 0 || effect.Conditions.Count != 0 ||
                        replacement.SourceTrait != "CardTraitSelfPurge" || replacement.TargetTrait != "CardTraitExhaustState" ||
                        !OnlyCardType(replacement.CardFilter, "OnlySpells", "Spell") ||
                        !OnlyCardType(replacement.CardFilterSecondary, "OnlyEquipment", "Equipment"))
                        return "Unmodeled RelicEffectModifyTrait payload or owner.";
                    continue;
                }
                if (type == RelicModel.ModifyCardCostIfNoUpgrades)
                {
                    if (effect.NoUpgradeCost == null || effect.Rule != null || effect.Upgrade != null ||
                        effect.TraitReplacement != null || effect.ApplyToCardlessSpawns || effect.ConditionCount != 0 ||
                        effect.Conditions.Count != 0)
                        return "Unmodeled unupgraded-card cost modifier payload.";
                    string? costError = RelicNoUpgradeCardCostModel.Validate(relic, effect.NoUpgradeCost);
                    if (costError != null) return costError;
                    continue;
                }
                if (type == RelicModel.ApplyCardUpgradeToHandEndOfTurnOnMoon)
                {
                    RelicMoonPhaseHandUpgrade? handUpgrade = effect.MoonPhaseHandUpgrade;
                    if (relic.DataId != RelicMoonPhaseHandUpgradeModel.OwnerId || relic.AssetKey != RelicMoonPhaseHandUpgradeModel.OwnerKey ||
                        relic.EffectTypes.Count != 1 || relic.IsCovenant == null || relic.DisallowedInPlacementPhase == null ||
                        handUpgrade == null || effect.Rule != null || effect.Upgrade != null || effect.TraitReplacement != null ||
                        effect.NoUpgradeCost != null || effect.ApplyToCardlessSpawns || effect.Conditions.Count != 0 || effect.ConditionCount != 0 ||
                        handUpgrade.MoonPhaseMask < 1 || handUpgrade.MoonPhaseMask > 3 || handUpgrade.ConditionCount != 0 ||
                        handUpgrade.Upgrade.MaskMetadata == null || handUpgrade.RemoveOnDiscard != handUpgrade.Upgrade.RemoveOnDiscard ||
                        CardModifierModel.UpgradeInteractions(handUpgrade.Upgrade).Any())
                        return "Unmodeled moon-phase end-turn hand upgrade payload or owner.";
                    continue;
                }
                if (effect.Rule == null || effect.TraitReplacement != null)
                    return "Relic card upgrade payload is missing or mixed with an unmodeled modifier.";
                if (effect.ConditionCount != effect.Conditions.Count) return "Relic card modifier condition definitions are incomplete.";
                if (effect.Conditions.Any(condition => condition.Comparator < 0 || condition.Comparator > 7))
                    return "Malformed native relic card modifier conditions.";
                if (effect.ApplyToCardlessSpawns && (relic.IsCovenant == null || relic.DisallowedInPlacementPhase == null))
                    return "Cardless relic upgrades require native covenant and placement metadata.";
                var template = effect.Rule.Upgrade; var upgrade = effect.Upgrade;
                if (template == null ? upgrade != null : upgrade == null || template.Values.DataId != upgrade.DataId ||
                    template.AssetKey != upgrade.AssetKey || template.Unique != upgrade.Unique || template.RemoveOnDiscard != upgrade.RemoveOnDiscard)
                    return "Relic card upgrade payload identity differs.";
                bool supportedTraitsOnly = template != null &&
                    (template.AddedTraits.Count > 0 || template.RemovedRuntimeTypes.Count > 0) &&
                    template.AddedTraits.All(trait => trait.RuntimeType == trait.DeclaredName &&
                        trait.RuntimeType is "CardTraitIgnoreArmor" or "CardTraitSelfPurge" or "CardTraitExhaustState" or "CardTraitDamageOverflow" or
                            "CardTraitEphemeral" or "CardTraitDrawInDeploymentPhase") &&
                    template.RemovedRuntimeTypes.All(type => type is "CardTraitIgnoreArmor" or "CardTraitExhaustState");
                bool hasTriggerLifecycle = template?.Triggers.Count > 0;
                bool supportedTriggerLifecycle = template != null && template.Triggers.All(trigger => trigger.Type == "OnCast" &&
                    trigger.ExternalInteractions.Count == 0 && trigger.Effects.Count == trigger.Actions.Count) &&
                    (!hasTriggerLifecycle || TriggerPayloadsMatch(template, upgrade?.Lifecycle));
                if (template != null && (template.AddedTraits.Count > 0 || template.RemovedRuntimeTypes.Count > 0) && !supportedTraitsOnly ||
                    hasTriggerLifecycle && !supportedTriggerLifecycle)
                    return "Unmodeled relic upgrade trait/trigger lifecycle in battle.";
                if (supportedTraitsOnly && (upgrade?.Lifecycle == null || upgrade.Lifecycle.AddedTraits.Count != template!.AddedTraits.Count ||
                    !upgrade.Lifecycle.AddedTraits.Select(trait => trait.DeclaredName).SequenceEqual(template.AddedTraits.Select(trait => trait.DeclaredName)) ||
                    !upgrade.Lifecycle.RemovedRuntimeTypes.SequenceEqual(template.RemovedRuntimeTypes)))
                    return "Supported relic upgrade trait lifecycle is missing or differs from its native definition.";
                if (template?.ReplacedAssets.Count > 0 && (effect.ApplyToCardlessSpawns || upgrade?.Lifecycle == null ||
                    !upgrade.Lifecycle.ReplacedAssets.SequenceEqual(template.ReplacedAssets)))
                    return "Relic upgrade replacement metadata is missing, inconsistent or used for a cardless spawn.";
                if (upgrade != null && (upgrade.MaskMetadata == null || CardModifierModel.UpgradeInteractions(upgrade).Any()))
                    return "Unmodeled relic card upgrade payload: " + string.Join("; ", upgrade.ExternalInteractions);
                bool expectsAbility = template?.Values.UnitAbility == true;
                bool hasAbility = upgrade?.AbilityUpgrade?.Definition != null;
                if (expectsAbility != hasAbility || hasAbility && (upgrade!.AbilityUpgrade!.Definition!.IsUnitAbility != true ||
                    upgrade.AbilityUpgrade.CommonTriggers.Count == 0))
                    return "Relic unit ability upgrade definition or common triggers are missing or inconsistent.";
                if (template != null && upgrade != null && new[] { "Damage", "Health", "Cost", "Heal", "Size", "XCost", "EquipmentLimit", "UpgradeSlotCount" }
                    .Any(stat => template.Values.Stats.Value(stat) != upgrade.Stats.Value(stat))) return "Relic card upgrade numeric payload differs.";
            }
            return null;
        }

        private static bool OnlyCardType(CardUpgradeMaskRule? filter, string assetKey, string cardType) =>
            filter != null && filter.AssetKey == assetKey && filter.CardType == cardType && filter.AdditionalCardTypes.Count == 0 &&
            filter.Rarities.Required.Count == 0 && filter.Rarities.Excluded.Count == 0 &&
            filter.Subtypes.Required.Count == 0 && filter.Subtypes.Excluded.Count == 0 &&
            filter.Statuses.Required.Count == 0 && filter.Statuses.Excluded.Count == 0 &&
            filter.Traits.Required.Count == 0 && filter.Traits.Excluded.Count == 0 &&
            filter.Effects.Required.Count == 0 && filter.Effects.Excluded.Count == 0 &&
            filter.LinkedClans.Required.Count == 0 && filter.LinkedClans.Excluded.Count == 0 &&
            filter.Upgrades.Required.Count == 0 && filter.Upgrades.Excluded.Count == 0 &&
            filter.AllowedPools.All(pool => pool.Count == 0) && filter.DisallowedPools.All(pool => pool.Count == 0) &&
            filter.RequiredSizes.Count == 0 && filter.ExcludedSizes.Count == 0 && filter.MinCost == 0 && filter.MaxCost == 99 &&
            filter.TargetMode == 0 && !filter.ExcludeNonAttackingMonsters && !filter.RequireXCost && !filter.ExcludeXCost &&
            !filter.ExcludeIfHasUnitAbility && !filter.ExcludeIfHasGraftedEquipment && !filter.ExcludeIfHasAnyUpgrades && !filter.ExcludeIfHasNoUpgrades;

        private static bool TriggerPayloadsMatch(CardLifecycleUpgrade template, CardLifecycleUpgrade? captured)
        {
            if (captured == null || template.Triggers.Count != captured.Triggers.Count) return false;
            for (int index = 0; index < template.Triggers.Count; index++)
            {
                CardLifecycleTrigger expected = template.Triggers[index], actual = captured.Triggers[index];
                if (expected.Type != actual.Type || expected.DescriptionKey != actual.DescriptionKey ||
                    !expected.Effects.SequenceEqual(actual.Effects) || !expected.ExternalInteractions.SequenceEqual(actual.ExternalInteractions) ||
                    expected.Actions.Count != actual.Actions.Count || expected.Actions.Where((action, actionIndex) =>
                        action.Type != actual.Actions[actionIndex].Type || action.Target != actual.Actions[actionIndex].Target ||
                        action.Value != actual.Actions[actionIndex].Value).Any()) return false;
            }
            return true;
        }

        public static RelicCardModifierResult Apply(CardInstanceState source, IReadOnlyList<CombatRelicState>? relics,
            bool resetTemporary = true, CombatContext? context = null)
        {
            relics ??= context?.Relics;
            var workingRelics = relics?.ToArray();
            if (context != null) context = context.WithRelics(workingRelics);
            string? error = RelicModel.Validate(relics);
            if (error != null) return Fail(error);
            if (workingRelics?.Any(relic => relic.CardModifiers?.Any(effect => effect.Conditions.Count > 0) == true ||
                relic.CardHealthModifiers?.Any(effect => effect.Conditions.Count > 0) == true) == true && context == null)
                return Fail("Relic card modifier conditions require the current combat context.");
            if (!resetTemporary && workingRelics?.Any(relic => relic.CardModifiers?.Count > 0 || relic.CardHealthModifiers?.Count > 0 ||
                relic.CardAttackModifiers?.Count > 0 ||
                relic.MagicPowerModifiers?.Count > 0 || relic.CardTypeCosts?.Count > 0) != true)
                return new RelicCardModifierResult(source, context: context);
            error = CardModifierModel.UnsupportedReason(source);
            if (error != null) return Fail(error);
            if (source.MaskDescriptor == null) return Fail("Relic card modifiers require branch-owned mask metadata.");
            var card = source;
            var owned = CardBranchMaskModel.OwnedState(card);
            int next = 1;
            var temporaryPayloads = new Dictionary<int, CardUpgradeModifier>();
            CardLifecycleUpgrade Payload(CardUpgradeModifier upgrade, bool trackTemporary = false)
            {
                CardLifecycleUpgrade? lifecycle = upgrade.Lifecycle;
                var payload = new CardLifecycleUpgrade(upgrade.MaskMetadata!.Current(upgrade), upgrade.AssetKey, upgrade.Unique,
                    upgrade.RemoveOnDiscard, lifecycle?.AddedTraits ?? Array.Empty<CardTraitValue>(),
                    lifecycle?.RemovedRuntimeTypes ?? Array.Empty<string>(), lifecycle?.AvoidClobbering ?? false,
                    lifecycle?.TraitsModified ?? Array.Empty<string>(), lifecycle?.ReplacedAssets ?? Array.Empty<string>(),
                    lifecycle?.Triggers ?? Array.Empty<CardLifecycleTrigger>(), upgrade.CloneDamageBase ?? lifecycle?.OriginalDamage,
                    upgrade.CloneHealBase ?? lifecycle?.OriginalHeal, next++);
                if (trackTemporary) temporaryPayloads.Add(payload.InstanceId, upgrade);
                return payload;
            }
            var state = new CardUpgradeLifecycleState(owned, card.Permanent.Upgrades.Select(upgrade => Payload(upgrade)).ToArray(),
                card.Temporary.Upgrades.Select(upgrade => Payload(upgrade, trackTemporary: true)).ToArray(),
                new[] { new CardLifecycleTrigger("OnCast", null, owned.InstalledCastEffects) },
                Array.Empty<CardLifecycleTrigger>(), false, "None");
            CardLifecycleUpgrade[] originalTemporary = state.Temporary.ToArray();
            var originalTemporaryIds = originalTemporary.Select(upgrade => upgrade.InstanceId).ToHashSet();
            if (resetTemporary)
            {
                state = CardUpgradeLifecycleModel.Reset(state).State;
                temporaryPayloads.Clear();
                card = card.WithModifiers(new CardModifiers(card.Permanent.Offsets, card.Permanent.Upgrades.Select((upgrade, index) =>
                    upgrade.WithStats(state.Permanent[index].Values.Stats)).ToArray(), card.Permanent.PersistentHealth,
                    card.Permanent.ExternalInteractions), CardModifiers.Empty());
                if (workingRelics != null)
                {
                    for (int relicIndex = 0; relicIndex < workingRelics.Length; relicIndex++)
                    {
                        CombatRelicState relic = workingRelics[relicIndex];
                        if (relic.CardAttackModifiers == null) continue;
                        RelicCardAttackModifier[] attacks = relic.CardAttackModifiers.Select(effect =>
                            effect.AppliedCardAttacks == null ? effect : effect.WithAppliedCardAttacks(
                                effect.AppliedCardAttacks.Where(item => item.CardInstanceId != source.InstanceId).ToArray())).ToArray();
                        workingRelics[relicIndex] = relic.WithCardAttackModifiers(attacks);
                    }
                    context = context?.WithRelics(workingRelics);
                }
            }
            var dispatches = new List<RelicCardModifierDispatch>();
            for (int relicIndex = 0; relicIndex < (workingRelics?.Length ?? 0); relicIndex++)
            {
                CombatRelicState relic = workingRelics![relicIndex];
                var cardEffects = (relic.CardModifiers ?? Array.Empty<RelicCardModifier>()).ToDictionary(effect => effect.EffectIndex);
                var costEffects = (relic.CardTypeCosts ?? Array.Empty<RelicCardTypeCost>()).ToDictionary(effect => effect.EffectIndex);
                var healthEffects = (relic.CardHealthModifiers ?? Array.Empty<RelicCardHealthModifier>()).ToDictionary(effect => effect.EffectIndex);
                var attackEffects = (relic.CardAttackModifiers ?? Array.Empty<RelicCardAttackModifier>()).ToDictionary(effect => effect.EffectIndex);
                var magicEffects = (relic.MagicPowerModifiers ?? Array.Empty<RelicMagicPowerModifier>()).ToDictionary(effect => effect.EffectIndex);
                for (int effectIndex = 0; effectIndex < relic.EffectTypes.Count; effectIndex++)
                {
                    string type = relic.EffectTypes[effectIndex];
                    if (type == RelicModel.ModifyCardTypeCost)
                    {
                        if (!costEffects.TryGetValue(effectIndex, out RelicCardTypeCost? costEffect))
                            return Fail("Missing ordered relic card-type cost definition.");
                        card = RelicCardTypeCostModel.Apply(card, costEffect!, out bool returned);
                        if (returned)
                        {
                            // Later upgrade filters read the cost after this native effect.
                            // Keep lifecycle identities and trait/trigger state while updating its offsets.
                            var current = state.Card;
                            var updated = new CardOwnedMaskState(current.Definition, current.BaseCost, current.Permanent,
                                new CardMaskModifiers(card.Temporary.Offsets, current.Temporary.Upgrades), current.Traits,
                                current.InstalledCastEffects, current.Purified, current.PermanentGraft);
                            state = new CardUpgradeLifecycleState(updated, state.Permanent, state.Temporary,
                                state.AuthoredTriggers, state.UpgradeTriggers, state.StandbyOverride, state.StandbyPile);
                        }
                        dispatches.Add(new RelicCardModifierDispatch(relicIndex, effectIndex, returned,
                            false, Array.Empty<RelicCardFilterResult>()));
                        continue;
                    }
                    if (type == RelicModel.ModifyCardCostIfNoUpgrades)
                    {
                        if (!cardEffects.TryGetValue(effectIndex, out RelicCardModifier? costModifier) ||
                            costModifier!.NoUpgradeCost == null)
                            return Fail("Missing unupgraded-card cost definition.");
                        bool returned = RelicNoUpgradeCardCostModel.IsCardOfInterest(card,
                            costModifier.NoUpgradeCost, out error);
                        if (error != null) return Fail(error);
                        int noUpgradeTemporaryCountBefore = state.Temporary.Count;
                        if (returned)
                        {
                            if (!RelicNoUpgradeCardCostModel.HasSlottedUpgrade(card, out bool hasSlottedUpgrade, out error))
                                return Fail(error!);
                            int costUpgradeCount = card.Temporary.Upgrades.Count(upgrade =>
                                upgrade.DataId == costModifier.NoUpgradeCost.Upgrade.DataId);
                            if (!resetTemporary && costUpgradeCount > 1)
                                return Fail("Unique unupgraded-card cost modifier is duplicated.");
                            bool hasCostUpgrade = costUpgradeCount > 0;
                            if (hasSlottedUpgrade && hasCostUpgrade)
                                state = CardUpgradeLifecycleModel.RemoveById(state,
                                    costModifier.NoUpgradeCost.Upgrade.DataId).State;
                            else if (!hasSlottedUpgrade && !hasCostUpgrade)
                            {
                                CardLifecycleUpgrade payload = Payload(costModifier.NoUpgradeCost.Upgrade, trackTemporary: true);
                                state = CardUpgradeLifecycleModel.ApplyTemporary(state, payload).State;
                            }
                            if (state.Temporary.Count != noUpgradeTemporaryCountBefore || hasCostUpgrade && hasSlottedUpgrade)
                            {
                                var synchronized = new List<CardUpgradeModifier>(state.Temporary.Count);
                                foreach (CardLifecycleUpgrade temporary in state.Temporary)
                                {
                                    if (!temporaryPayloads.TryGetValue(temporary.InstanceId, out CardUpgradeModifier? modifier))
                                        return Fail("Unupgraded-card cost lost a temporary upgrade payload.");
                                    synchronized.Add(modifier);
                                }
                                card = card.WithModifiers(card.Permanent, new CardModifiers(card.Temporary.Offsets,
                                    synchronized, card.Temporary.PersistentHealth, card.Temporary.ExternalInteractions));
                                if (!RefreshTemporaryPayloads(card.Temporary, state, temporaryPayloads))
                                    return Fail("Unupgraded-card cost changed temporary upgrade lifecycle order.");
                            }
                        }
                        dispatches.Add(new RelicCardModifierDispatch(relicIndex, effectIndex, returned,
                            state.Temporary.Count > noUpgradeTemporaryCountBefore, Array.Empty<RelicCardFilterResult>()));
                        continue;
                    }
                    if (type == RelicModel.ModifyTrait)
                    {
                        if (!cardEffects.TryGetValue(effectIndex, out RelicCardModifier? traitEffect) || traitEffect.TraitReplacement == null)
                            return Fail("Missing ordered relic trait-replacement definition.");
                        card = RelicTraitReplacementModel.Apply(card, traitEffect.TraitReplacement, out bool returned,
                            out error, out var filters)!;
                        if (card == null) return Fail(error!);
                        dispatches.Add(new RelicCardModifierDispatch(relicIndex, effectIndex, returned, false, filters));
                        continue;
                    }
                    if (type == RelicModel.ModifyCardHealth || type == RelicModel.ModifyCardHealthPerRarityType)
                    {
                        if (!healthEffects.TryGetValue(effectIndex, out RelicCardHealthModifier? healthEffect))
                            return Fail("Missing ordered relic card-health definition.");
                        card = RelicCardHealthModel.Apply(card, healthEffect!, context, out bool returned, out error)!;
                        if (card == null) return Fail(error!);
                        dispatches.Add(new RelicCardModifierDispatch(relicIndex, healthEffect!.EffectIndex, returned,
                            false, Array.Empty<RelicCardFilterResult>()));
                        continue;
                    }
                    if (type == RelicModel.ModifyCardMagicalPower || type == RelicModel.ModifyCardHealingPower)
                    {
                        if (!magicEffects.TryGetValue(effectIndex, out RelicMagicPowerModifier? magicEffect))
                            return Fail("Missing ordered relic magic-power definition.");
                        bool healingOnly = type == RelicModel.ModifyCardHealingPower;
                        bool returned = healingOnly ? card.MaskDescriptor?.Definition.CardType == "Spell" :
                            card.MaskDescriptor?.Definition.CanScaleWithMagicPower == true;
                        card = RelicMagicPowerModel.ApplyToCard(card, magicEffect!, out error, healingOnly)!;
                        if (card == null) return Fail(error!);
                        if (!RefreshTemporaryPayloads(card.Temporary, state, temporaryPayloads))
                            return Fail("Relic magic-power refresh changed the temporary upgrade lifecycle order.");
                        dispatches.Add(new RelicCardModifierDispatch(relicIndex, magicEffect!.EffectIndex, returned,
                            false, Array.Empty<RelicCardFilterResult>()));
                        continue;
                    }
                    if (type == RelicModel.ModifyCardAttack)
                    {
                        if (!attackEffects.TryGetValue(effectIndex, out RelicCardAttackModifier? attackEffect))
                            return Fail("Missing ordered relic card-attack definition.");
                        CardInstanceState beforeAttack = card;
                        card = RelicCardAttackModel.Apply(card, attackEffect!, out bool returned, out error, out var filters, workingRelics)!;
                        if (card == null) return Fail(error!);
                        if (returned && attackEffect!.AppliedCardAttacks != null)
                        {
                            bool inDeck = context?.CardInstances?.Any(item => item.InstanceId == source.InstanceId) == true &&
                                (context.OtherPiles ?? Array.Empty<CardPileState>()).Where(pile => pile.Name == "Purged")
                                    .SelectMany(pile => pile.Cards).All(item => item.InstanceId != source.InstanceId);
                            int amount = unchecked(card.Temporary.Offsets.Damage - beforeAttack.Temporary.Offsets.Damage);
                            RelicCardAttackModifier updated = attackEffect.WithAppliedCardAttacks(
                                attackEffect.AppliedCardAttacks.Concat(new[] { new RelicCardAttackContribution(source.InstanceId, amount, inDeck) }).ToArray());
                            RelicCardAttackModifier[] attacks = workingRelics![relicIndex].CardAttackModifiers!
                                .Select(item => item.EffectIndex == effectIndex ? updated : item).ToArray();
                            workingRelics[relicIndex] = workingRelics[relicIndex].WithCardAttackModifiers(attacks);
                            context = context?.WithRelics(workingRelics);
                        }
                        dispatches.Add(new RelicCardModifierDispatch(relicIndex, attackEffect!.EffectIndex, returned,
                            false, filters));
                        continue;
                    }
                    if (type != RelicModel.AddTempUpgrade) continue;
                    if (!cardEffects.TryGetValue(effectIndex, out RelicCardModifier? cardEffect))
                        return Fail("Missing ordered relic card-upgrade definition.");
                    RelicConditionEvaluation? conditionEvaluation = null;
                    if (cardEffect!.Conditions.Count > 0)
                    {
                        conditionEvaluation = RelicConditionModel.Evaluate(context!, cardEffect.Conditions);
                        if (!conditionEvaluation.Supported) return Fail(conditionEvaluation.UnsupportedReason!);
                        context = conditionEvaluation.Context;
                        if (!conditionEvaluation.Passed) continue;
                    }
                    int addedInstanceId = next;
                    int temporaryCountBefore = state.Temporary.Count;
                    var applied = RelicCardUpgradeModel.Apply(state, cardEffect.Rule!, next);
                    state = applied.State; next = applied.NextUpgradeInstanceId;
                    if (applied.UpgradeAdded)
                    {
                        if (state.Temporary.Any(item => item.InstanceId == addedInstanceId))
                            temporaryPayloads[addedInstanceId] = cardEffect.Upgrade!;
                        var synchronized = new List<CardUpgradeModifier>(state.Temporary.Count);
                        foreach (CardLifecycleUpgrade temporary in state.Temporary)
                        {
                            if (!temporaryPayloads.TryGetValue(temporary.InstanceId, out CardUpgradeModifier? modifier))
                                return Fail("Relic replacement lost the matching temporary card modifier payload.");
                            synchronized.Add(modifier);
                        }
                        var modifiers = new CardModifiers(card.Temporary.Offsets, synchronized, card.Temporary.PersistentHealth,
                            card.Temporary.ExternalInteractions);
                        card = card.WithModifiers(card.Permanent, modifiers);
                        if (!RefreshTemporaryPayloads(card.Temporary, state, temporaryPayloads))
                            return Fail("Relic replacement changed the temporary upgrade lifecycle order.");
                    }
                    if (applied.Returned && conditionEvaluation != null)
                    {
                        RelicConditionRecord record = RelicConditionModel.Record(context!, cardEffect.Conditions);
                        if (!record.Supported) return Fail(record.UnsupportedReason!);
                        context = record.Context;
                        var updatedEffects = workingRelics[relicIndex].CardModifiers!.ToArray();
                        int effectOffset = Array.FindIndex(updatedEffects, item => item.EffectIndex == cardEffect.EffectIndex);
                        if (effectOffset < 0) return Fail("Relic card modifier order changed while recording its condition.");
                        updatedEffects[effectOffset] = cardEffect.WithConditions(record.Conditions);
                        workingRelics[relicIndex] = workingRelics[relicIndex].WithCardModifiers(updatedEffects);
                        context = context!.WithRelics(workingRelics);
                    }
                    dispatches.Add(new RelicCardModifierDispatch(relicIndex, cardEffect.EffectIndex, applied.Returned,
                        state.Temporary.Count > temporaryCountBefore, applied.Filters));
                }
            }
            var installedCastEffects = source.MaskDescriptor.AuthoredCastEffects.ToList();
            var finalTemporaryIds = state.Temporary.Select(upgrade => upgrade.InstanceId).ToHashSet();
            foreach (CardLifecycleUpgrade removed in originalTemporary.Where(upgrade => !finalTemporaryIds.Contains(upgrade.InstanceId)))
                foreach (string effect in removed.Triggers.Where(trigger => trigger.Type == "OnCast").SelectMany(trigger => trigger.Effects).Reverse())
                {
                    int index = installedCastEffects.FindLastIndex(value => value == effect);
                    if (index >= 0) installedCastEffects.RemoveAt(index);
                }
            foreach (CardLifecycleUpgrade added in state.Temporary.Where(upgrade => !originalTemporaryIds.Contains(upgrade.InstanceId)))
                installedCastEffects.AddRange(added.Triggers.Where(trigger => trigger.Type == "OnCast").SelectMany(trigger => trigger.Effects));
            card = card.WithMaskDescriptor((card.MaskDescriptor ?? source.MaskDescriptor).WithAuthoredCastEffects(installedCastEffects));
            return new RelicCardModifierResult(card, dispatches, context: context);

            static bool RefreshTemporaryPayloads(CardModifiers modifiers, CardUpgradeLifecycleState lifecycle,
                Dictionary<int, CardUpgradeModifier> payloads)
            {
                if (modifiers.Upgrades.Count != lifecycle.Temporary.Count) return false;
                for (int index = 0; index < lifecycle.Temporary.Count; index++)
                    payloads[lifecycle.Temporary[index].InstanceId] = modifiers.Upgrades[index];
                return true;
            }
        }

        internal static RoomCombatResult ApplyCardlessSpawn(RoomCombatState source, int unitId, int fromCardId,
            RelicCardModifier effect)
        {
            CardUpgradeModifier? upgrade = effect.Upgrade;
            if (effect.Conditions.Count > 0)
            {
                if (source.Context == null) return FailRoom("Relic card modifier conditions require the current combat context.");
                RelicConditionEvaluation evaluation = RelicConditionModel.Evaluate(source.Context, effect.Conditions);
                if (!evaluation.Supported) return FailRoom(evaluation.UnsupportedReason!);
                source = WithContext(source, evaluation.Context);
                if (!evaluation.Passed) return Match(source);
            }
            if (!effect.ApplyToCardlessSpawns || upgrade == null) return Match(source);
            CombatUnit? actor = source.Units.FirstOrDefault(unit => unit.Id == unitId);
            if (actor == null) return FailRoom("Missing cardless relic birth unit.");
            if (!(actor.Team == CombatTeam.Player ? effect.Rule!.SourceMonsters : effect.Rule!.SourceHeroes) ||
                actor.Status("cardless") == null) return Match(source);
            if (actor.Modifiers == null) return FailRoom("Cardless relic upgrades require retained unit modifiers.");
            if (actor.Modifiers.IsClone) return Match(source);
            if (source.Context == null) return FailRoom("Cardless relic upgrades require a combat context.");
            CardInstanceState? fromCard = fromCardId == 0 ? null : source.Context.FindCard(fromCardId);
            if (fromCardId < 0 || fromCardId > 0 && fromCard == null)
                return FailRoom("Missing cardless relic source card.");

            CardUpgradeMaskCard? cardMask = null;
            if (effect.Rule!.Filters.Count > 0 && fromCard != null)
            {
                if (fromCard.MaskDescriptor == null) return FailRoom("Cardless relic filters require source-card mask metadata.");
                cardMask = CardBranchMaskModel.Resolve(fromCard);
            }
            bool needsStatusRegistry = effect.Rule.Filters.Any(filter =>
                filter.Statuses.Required.Count > 0 || filter.Statuses.Excluded.Count > 0);
            if (needsStatusRegistry && actor.StatusRegistry == null)
                return FailRoom("Cardless relic filters require the native character status registry.");
            var characterMask = new CardUpgradeMaskCharacter(actor.Subtypes,
                (actor.StatusRegistry ?? actor.Statuses).Select(status => status.Id).ToArray(), actor.Size);
            foreach (CardUpgradeMaskRule filter in effect.Rule.Filters)
            {
                if (!CardUpgradeMaskModel.FilterCard(filter, cardMask) ||
                    !CardUpgradeMaskModel.FilterCharacter(filter, characterMask)) return Match(source);
            }
            if (upgrade.Unique && fromCard != null && fromCard.Permanent.Upgrades.Concat(fromCard.Temporary.Upgrades)
                .Any(item => item.DataId == upgrade.DataId)) return Match(source);
            return UnitModifierModel.ApplyDirectDeferred(source, unitId, upgrade);
        }

        private static RoomCombatResult Match(RoomCombatState state) =>
            new RoomCombatResult(state, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
        private static RoomCombatState WithContext(RoomCombatState state, CombatContext context) =>
            state.WithContext(context);
        private static RelicCardModifierResult Fail(string error) => new RelicCardModifierResult(null, unsupportedReason: error);
        private static RoomCombatResult FailRoom(string error) =>
            new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error);
    }

    internal static class RelicTraitReplacementModel
    {
        internal static CardInstanceState? Apply(CardInstanceState source, RelicTraitReplacement effect,
            out bool returned, out string? error, out IReadOnlyList<RelicCardFilterResult> filters)
        {
            returned = false;
            error = null;
            var observations = new List<RelicCardFilterResult>();
            filters = observations;
            if (source.MaskDescriptor == null)
            { error = "Relic trait replacements require branch-owned card mask metadata."; return null; }
            error = CardModifierModel.UnsupportedReason(source);
            if (error != null) return null;

            CardInstanceState card = source;
            if (Matches(effect.CardFilter, card, out bool primaryAccepted))
            {
                observations.Add(new RelicCardFilterResult(effect.CardFilter!.AssetKey, primaryAccepted));
                if (primaryAccepted) card = Convert(card, effect, out returned);
            }
            if (!returned && Matches(effect.CardFilterSecondary, card, out bool secondaryAccepted))
            {
                observations.Add(new RelicCardFilterResult(effect.CardFilterSecondary!.AssetKey, secondaryAccepted));
                if (secondaryAccepted) card = Convert(card, effect, out returned);
            }
            return card;

            static bool Matches(CardUpgradeMaskRule? filter, CardInstanceState current, out bool accepted)
            {
                if (filter == null) { accepted = false; return false; }
                accepted = CardUpgradeMaskModel.FilterCard(filter, CardBranchMaskModel.Resolve(current));
                return true;
            }

            static CardInstanceState Convert(CardInstanceState current, RelicTraitReplacement replacement, out bool hasSource)
            {
                hasSource = CardBranchMaskModel.Resolve(current).Traits.Contains(replacement.SourceTrait, StringComparer.Ordinal);
                CardMaskDescriptor descriptor = current.MaskDescriptor!;
                if (!descriptor.PermanentReplacements.Any(rule => rule.OldName == replacement.SourceTrait &&
                    rule.NewName == replacement.TargetTrait))
                {
                    var converted = new CardTraitReplacement(replacement.SourceTrait, replacement.TargetTrait, replacement.TargetTrait);
                    CardTraitValue[] baseTraits = descriptor.BaseTraits.ToArray();
                    if (hasSource)
                    {
                        int sourceIndex = Array.FindIndex(baseTraits, trait => trait.RuntimeType == replacement.SourceTrait);
                        if (sourceIndex >= 0) baseTraits[sourceIndex] = baseTraits[sourceIndex].Retyped(converted, temporary: false);
                    }
                    descriptor = new CardMaskDescriptor(descriptor.Definition, descriptor.BaseCost, baseTraits,
                        descriptor.AuthoredCastEffects, descriptor.Purified, descriptor.PermanentGraft,
                        descriptor.TemporaryTraits, descriptor.PermanentReplacements.Concat(new[] { converted }).ToArray());
                    current = current.WithMaskDescriptor(descriptor);
                }
                return hasSource ? CardExhaustModel.RefreshUpgradeTrait(current) : current;
            }
        }
    }

    internal static class RelicCardHealthModel
    {
        private static readonly string[] TribouletEffects = {
            RelicModel.ModifyCardHealthPerRarityType,
            RelicModel.ModifyCharacterMaxHealthPerRarityType,
            RelicModel.ModifyCharacterMaxHealthPerRarityTypeTrigger
        };

        internal static string? Validate(CombatRelicState relic)
        {
            var indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type == RelicModel.ModifyCardHealth || TribouletEffects.Contains(item.type, StringComparer.Ordinal))
                .Select(item => item.index).ToArray();
            if (indices.Length == 0) return relic.CardHealthModifiers?.Count > 0
                ? "Unexpected relic card-health definitions." : null;
            if (relic.CardHealthModifiers == null || !indices.SequenceEqual(relic.CardHealthModifiers.Select(effect => effect.EffectIndex)))
                return "Missing ordered native relic card-health definitions.";
            bool hasTribouletEffect = relic.EffectTypes.Any(type => TribouletEffects.Contains(type, StringComparer.Ordinal));
            if (hasTribouletEffect)
            {
                if (relic.DataId != "dfe0d28c-ff6a-4a8b-8290-aefb32d0c1d6" || relic.AssetKey != "BalatroEventTriboulet" ||
                    !relic.EffectTypes.SequenceEqual(TribouletEffects, StringComparer.Ordinal) || relic.CardHealthModifiers.Count != 3)
                    return "Per-rarity character health effects are modeled only for the exact Triboulet owner.";
                RelicCardHealthModifier[] effects = relic.CardHealthModifiers.ToArray();
                if (!Plain(effects[0], RelicModel.ModifyCardHealthPerRarityType, 0, "Monster", 3, "Rare", "", "") ||
                    !Plain(effects[1], RelicModel.ModifyCharacterMaxHealthPerRarityType, 1, "", 2, "Rare", "Monsters", "") ||
                    !Plain(effects[2], RelicModel.ModifyCharacterMaxHealthPerRarityTypeTrigger, 2, "", 2, "Rare", "Monsters", "OnSpawnNotFromCard"))
                    return "Triboulet's ordered rarity-health payload changed.";
                return null;
            }
            foreach (RelicCardHealthModifier effect in relic.CardHealthModifiers)
            {
                if (effect.EffectType != RelicModel.ModifyCardHealth)
                    return "Unexpected per-rarity relic health effect.";
                if (effect.ConditionCount != effect.Conditions.Count || effect.Conditions.Count != 0)
                    return "Relic card-health conditions are not modeled.";
                if (effect.CharacterName.Length != 0)
                    return "Relic card-health card-name filters are not modeled.";
                if (effect.HealthMultiplier != 0)
                    return "Relic card-health multipliers are not modeled.";
                if (effect.CharacterSubtypeKey.Length == 0 && effect.CardType.Length == 0)
                    return "Relic card-health effect has no supported subtype or card-type filter.";
            }
            return null;

            static bool Plain(RelicCardHealthModifier effect, string type, int index, string cardType,
                int amount, string rarity, string sourceTeam, string trigger) =>
                effect.EffectType == type && effect.EffectIndex == index && effect.CharacterName.Length == 0 &&
                effect.CharacterSubtypeKey.Length == 0 && effect.HealthModifier == amount && effect.HealthMultiplier == 0 &&
                effect.CardType == cardType && !effect.MultiplyByUpgradeCount && effect.CardRarityType == rarity &&
                effect.SourceTeam == sourceTeam && effect.Trigger == trigger && effect.ConditionCount == 0 && effect.Conditions.Count == 0;
        }

        internal static CardInstanceState? Apply(CardInstanceState source, RelicCardHealthModifier effect,
            CombatContext? context, out bool returned, out string? error)
        {
            returned = false; error = null;
            CardMaskDescriptor? descriptor = source.MaskDescriptor;
            if (descriptor == null)
            { error = "Relic card-health modifiers require branch-owned mask metadata."; return null; }
            int amount = effect.HealthModifier;
            if (effect.EffectType == RelicModel.ModifyCardHealthPerRarityType)
            {
                if (context?.Statistics?.DeckCards == null)
                { error = "Triboulet's per-rarity card health requires permanent deck membership."; return null; }
                int count = 0;
                foreach (int id in context.Statistics.DeckCards)
                {
                    CardInstanceState? deckCard = context.FindCard(id);
                    if (deckCard?.MaskDescriptor == null)
                    { error = "Triboulet's rarity count requires metadata for every deck card."; return null; }
                    if (deckCard.MaskDescriptor.Definition.Rarity == effect.CardRarityType) count++;
                }
                amount = Math.Max(unchecked(count * amount), 0);
            }
            else if (effect.EffectType != RelicModel.ModifyCardHealth)
            { error = "Character-only relic health effects cannot modify card masks."; return null; }
            if (effect.MultiplyByUpgradeCount)
            {
                if (!RelicCardUpgradeCount.TryCount(source, out int count, out error)) return null;
                amount = unchecked(amount * count);
            }
            bool matches;
            if (effect.CharacterSubtypeKey.Length > 0)
            {
                matches = descriptor.Definition.Subtypes.Contains(effect.CharacterSubtypeKey, StringComparer.Ordinal);
                if (matches && descriptor.Definition.MainEffectNames.Count(name => name == "CardEffectSpawnMonster") != 1)
                { error = "Subtype-filtered relic health needs exactly one captured spawned character."; return null; }
            }
            else matches = descriptor.Definition.CardType == effect.CardType;
            if (!matches || amount == 0) return source;
            returned = true;
            CardStatModifier offsets = source.Temporary.Offsets;
            var updated = new CardModifiers(new CardStatModifier(offsets.Damage, unchecked(offsets.Health + amount),
                offsets.Cost, offsets.Heal, offsets.Size, offsets.XCost, offsets.EquipmentLimit, offsets.UpgradeSlotCount),
                source.Temporary.Upgrades, source.Temporary.PersistentHealth, source.Temporary.ExternalInteractions);
            return source.WithModifiers(source.Permanent, updated);
        }
    }

    internal static class RelicCardUpgradeCount
    {
        // Both native relics call CardUpgradeState.ShouldShowIcon on permanent
        // upgrades. Unlike the upgrade-mask count, this includes region upgrades.
        internal static bool TryCount(CardInstanceState card, out int count, out string? error)
        {
            count = 0; error = null;
            foreach (CardUpgradeModifier upgrade in card.Permanent.Upgrades)
            {
                CardUpgradeMaskMetadata? metadata = upgrade.MaskMetadata;
                if (metadata == null)
                { error = "Relic upgrade-count scaling requires permanent upgrade icon metadata."; return false; }
                if (metadata.HasIcon && !metadata.HideIcon) count++;
            }
            return true;
        }
    }
}

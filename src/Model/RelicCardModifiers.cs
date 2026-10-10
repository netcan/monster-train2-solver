using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicCardModifier
    {
        public int EffectIndex { get; }
        public RelicCardUpgradeRule Rule { get; }
        public CardUpgradeModifier? Upgrade { get; }
        public bool ApplyToCardlessSpawns { get; }
        public int ConditionCount { get; }
        public IReadOnlyList<RelicConditionState> Conditions { get; }
        public RelicCardModifier(int effectIndex, RelicCardUpgradeRule rule, CardUpgradeModifier? upgrade,
            bool applyToCardlessSpawns, IReadOnlyList<RelicConditionState>? conditions = null, int conditionCount = 0)
        { EffectIndex = effectIndex; Rule = rule; Upgrade = upgrade; ApplyToCardlessSpawns = applyToCardlessSpawns;
            Conditions = Array.AsReadOnly((conditions ?? Array.Empty<RelicConditionState>()).ToArray());
            ConditionCount = conditions?.Count ?? conditionCount; }
        internal RelicCardModifier WithConditions(IReadOnlyList<RelicConditionState> conditions) =>
            new RelicCardModifier(EffectIndex, Rule, Upgrade, ApplyToCardlessSpawns, conditions);
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
            var indices = relic.EffectTypes.Select((type, index) => (type, index)).Where(item => item.type == RelicModel.AddTempUpgrade)
                .Select(item => item.index).ToArray();
            if (indices.Length == 0) return relic.CardModifiers?.Count > 0 ? "Unexpected relic card modifier definitions." : null;
            if (relic.CardModifiers == null || !indices.SequenceEqual(relic.CardModifiers.Select(rule => rule.EffectIndex)))
                return "Missing ordered native relic card modifier definitions.";
            foreach (var effect in relic.CardModifiers)
            {
                if (effect.ConditionCount != effect.Conditions.Count) return "Relic card modifier condition definitions are incomplete.";
                if (effect.Conditions.Any(condition => condition.Comparator < 0 || condition.Comparator > 7))
                    return "Malformed native relic card modifier conditions.";
                if (effect.ApplyToCardlessSpawns && (relic.IsCovenant == null || relic.DisallowedInPlacementPhase == null))
                    return "Cardless relic upgrades require native covenant and placement metadata.";
                var template = effect.Rule.Upgrade; var upgrade = effect.Upgrade;
                if (template == null ? upgrade != null : upgrade == null || template.Values.DataId != upgrade.DataId ||
                    template.AssetKey != upgrade.AssetKey || template.Unique != upgrade.Unique || template.RemoveOnDiscard != upgrade.RemoveOnDiscard)
                    return "Relic card upgrade payload identity differs.";
                bool supportedTraitsOnly = template?.AddedTraits.Count > 0 && template.AddedTraits.All(trait =>
                    trait.RuntimeType == trait.DeclaredName &&
                    trait.RuntimeType is "CardTraitIgnoreArmor" or "CardTraitSelfPurge") &&
                    template.RemovedRuntimeTypes.Count == 0 && template.ReplacedAssets.Count == 0 && template.Triggers.Count == 0;
                if (template != null && (template.AddedTraits.Count > 0 && !supportedTraitsOnly ||
                    template.RemovedRuntimeTypes.Count > 0 || template.ReplacedAssets.Count > 0 || template.Triggers.Count > 0))
                    return "Unmodeled relic upgrade trait/trigger lifecycle in battle.";
                if (supportedTraitsOnly && (upgrade?.Lifecycle == null || upgrade.Lifecycle.AddedTraits.Count != template!.AddedTraits.Count ||
                    !upgrade.Lifecycle.AddedTraits.Select(trait => trait.DeclaredName).SequenceEqual(template.AddedTraits.Select(trait => trait.DeclaredName))))
                    return "Supported relic upgrade trait lifecycle is missing or differs from its native definition.";
                if (upgrade != null && (upgrade.MaskMetadata == null || upgrade.ExternalInteractions.Count > 0))
                    return "Unmodeled relic card upgrade payload: " + string.Join("; ", upgrade.ExternalInteractions);
                bool expectsAbility = template?.Values.UnitAbility == true;
                bool hasAbility = upgrade?.AbilityUpgrade?.Definition != null;
                if (expectsAbility != hasAbility || hasAbility && (upgrade!.AbilityUpgrade!.Definition!.IsUnitAbility != true ||
                    upgrade.AbilityUpgrade.CommonTriggers.Count == 0))
                    return "Relic unit ability upgrade definition or common triggers are missing or inconsistent.";
                if (upgrade != null && (upgrade.UnhealedHealth != 0 || upgrade.DamageBuff != 0))
                    return "Unmodeled relic additional unit health/damage behavior.";
                if (template != null && upgrade != null && new[] { "Damage", "Health", "Cost", "Heal", "Size", "XCost", "EquipmentLimit", "UpgradeSlotCount" }
                    .Any(stat => template.Values.Stats.Value(stat) != upgrade.Stats.Value(stat))) return "Relic card upgrade numeric payload differs.";
            }
            return null;
        }

        public static RelicCardModifierResult Apply(CardInstanceState source, IReadOnlyList<CombatRelicState>? relics,
            bool resetTemporary = true, CombatContext? context = null)
        {
            relics ??= context?.Relics;
            var workingRelics = relics?.ToArray();
            if (context != null) context = context.WithRelics(workingRelics);
            string? error = RelicModel.Validate(relics);
            if (error != null) return Fail(error);
            if (workingRelics?.Any(relic => relic.CardModifiers?.Any(effect => effect.Conditions.Count > 0) == true) == true && context == null)
                return Fail("Relic card modifier conditions require the current combat context.");
            if (!resetTemporary && workingRelics?.Any(relic => relic.CardModifiers?.Count > 0) != true)
                return new RelicCardModifierResult(source, context: context);
            error = CardModifierModel.UnsupportedReason(source);
            if (error != null) return Fail(error);
            if (source.MaskDescriptor == null) return Fail("Relic card modifiers require branch-owned mask metadata.");
            var card = source;
            var owned = CardBranchMaskModel.OwnedState(card);
            int next = 1;
            CardLifecycleUpgrade Payload(CardUpgradeModifier upgrade)
            {
                CardLifecycleUpgrade? lifecycle = upgrade.Lifecycle;
                return new CardLifecycleUpgrade(upgrade.MaskMetadata!.Current(upgrade), upgrade.AssetKey, upgrade.Unique,
                    upgrade.RemoveOnDiscard, lifecycle?.AddedTraits ?? Array.Empty<CardTraitValue>(),
                    lifecycle?.RemovedRuntimeTypes ?? Array.Empty<string>(), lifecycle?.AvoidClobbering ?? false,
                    lifecycle?.TraitsModified ?? Array.Empty<string>(), lifecycle?.ReplacedAssets ?? Array.Empty<string>(),
                    lifecycle?.Triggers ?? Array.Empty<CardLifecycleTrigger>(), upgrade.CloneDamageBase ?? lifecycle?.OriginalDamage,
                    upgrade.CloneHealBase ?? lifecycle?.OriginalHeal, next++);
            }
            var state = new CardUpgradeLifecycleState(owned, card.Permanent.Upgrades.Select(Payload).ToArray(),
                card.Temporary.Upgrades.Select(Payload).ToArray(), new[] { new CardLifecycleTrigger("OnCast", null, owned.InstalledCastEffects) },
                Array.Empty<CardLifecycleTrigger>(), false, "None");
            if (resetTemporary)
            {
                state = CardUpgradeLifecycleModel.Reset(state).State;
                card = card.WithModifiers(new CardModifiers(card.Permanent.Offsets, card.Permanent.Upgrades.Select((upgrade, index) =>
                    upgrade.WithStats(state.Permanent[index].Values.Stats)).ToArray(), card.Permanent.PersistentHealth,
                    card.Permanent.ExternalInteractions), CardModifiers.Empty());
            }
            var dispatches = new List<RelicCardModifierDispatch>();
            for (int relicIndex = 0; relicIndex < (workingRelics?.Length ?? 0); relicIndex++)
            foreach (var effect in workingRelics![relicIndex].CardModifiers ?? Array.Empty<RelicCardModifier>())
            {
                RelicConditionEvaluation? conditionEvaluation = null;
                if (effect.Conditions.Count > 0)
                {
                    conditionEvaluation = RelicConditionModel.Evaluate(context!, effect.Conditions);
                    if (!conditionEvaluation.Supported) return Fail(conditionEvaluation.UnsupportedReason!);
                    context = conditionEvaluation.Context;
                    if (!conditionEvaluation.Passed) continue;
                }
                var applied = RelicCardUpgradeModel.Apply(state, effect.Rule, next);
                state = applied.State; next = applied.NextUpgradeInstanceId;
                if (applied.UpgradeAdded) card = card.WithModifiers(card.Permanent, UnitModifierModel.Add(card.Temporary, effect.Upgrade!));
                if (applied.Returned && conditionEvaluation != null)
                {
                    RelicConditionRecord record = RelicConditionModel.Record(context!, effect.Conditions);
                    if (!record.Supported) return Fail(record.UnsupportedReason!);
                    context = record.Context;
                    var updatedEffects = workingRelics[relicIndex].CardModifiers!.ToArray();
                    int effectOffset = Array.FindIndex(updatedEffects, item => item.EffectIndex == effect.EffectIndex);
                    if (effectOffset < 0) return Fail("Relic card modifier order changed while recording its condition.");
                    updatedEffects[effectOffset] = effect.WithConditions(record.Conditions);
                    workingRelics[relicIndex] = workingRelics[relicIndex].WithCardModifiers(updatedEffects);
                    context = context!.WithRelics(workingRelics);
                }
                dispatches.Add(new RelicCardModifierDispatch(relicIndex, effect.EffectIndex, applied.Returned, applied.UpgradeAdded, applied.Filters));
            }
            return new RelicCardModifierResult(card, dispatches, context: context);
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
            if (!(actor.Team == CombatTeam.Player ? effect.Rule.SourceMonsters : effect.Rule.SourceHeroes) ||
                actor.Status("cardless") == null) return Match(source);
            if (actor.Modifiers == null) return FailRoom("Cardless relic upgrades require retained unit modifiers.");
            if (actor.Modifiers.IsClone) return Match(source);
            if (source.Context == null) return FailRoom("Cardless relic upgrades require a combat context.");
            CardInstanceState? fromCard = fromCardId == 0 ? null : source.Context.FindCard(fromCardId);
            if (fromCardId < 0 || fromCardId > 0 && fromCard == null)
                return FailRoom("Missing cardless relic source card.");

            CardUpgradeMaskCard? cardMask = null;
            if (effect.Rule.Filters.Count > 0 && fromCard != null)
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
            new RoomCombatState(state.RoomIndex, state.Deployment, state.Units, state.ExternalInteractions, context, state.Preview);
        private static RelicCardModifierResult Fail(string error) => new RelicCardModifierResult(null, unsupportedReason: error);
        private static RoomCombatResult FailRoom(string error) =>
            new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error);
    }
}

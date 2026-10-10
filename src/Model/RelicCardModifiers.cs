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
        public RelicCardModifier(int effectIndex, RelicCardUpgradeRule rule, CardUpgradeModifier? upgrade,
            bool applyToCardlessSpawns, int conditionCount)
        { EffectIndex = effectIndex; Rule = rule; Upgrade = upgrade; ApplyToCardlessSpawns = applyToCardlessSpawns; ConditionCount = conditionCount; }
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
        public RelicCardModifierResult(CardInstanceState? card, IReadOnlyList<RelicCardModifierDispatch>? dispatches = null, string? unsupportedReason = null)
        { Card = card; Dispatches = Array.AsReadOnly((dispatches ?? Array.Empty<RelicCardModifierDispatch>()).ToArray()); UnsupportedReason = unsupportedReason; }
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
                if (effect.ConditionCount != 0) return "Unmodeled relic card modifier conditions.";
                if (effect.ApplyToCardlessSpawns) return "Unmodeled relic cardless upgrade dispatch.";
                var template = effect.Rule.Upgrade; var upgrade = effect.Upgrade;
                if (template == null ? upgrade != null : upgrade == null || template.Values.DataId != upgrade.DataId ||
                    template.AssetKey != upgrade.AssetKey || template.Unique != upgrade.Unique || template.RemoveOnDiscard != upgrade.RemoveOnDiscard)
                    return "Relic card upgrade payload identity differs.";
                if (template?.AddedTraits.Count > 0 || template?.RemovedRuntimeTypes.Count > 0 ||
                    template?.ReplacedAssets.Count > 0 || template?.Triggers.Count > 0)
                    return "Unmodeled relic upgrade trait/trigger lifecycle in battle.";
                if (upgrade != null && (upgrade.MaskMetadata == null || upgrade.ExternalInteractions.Count > 0))
                    return "Unmodeled relic card upgrade payload: " + string.Join("; ", upgrade.ExternalInteractions);
                if (upgrade != null && (upgrade.AbilityUpgrade != null || upgrade.UnhealedHealth != 0 || upgrade.DamageBuff != 0))
                    return "Unmodeled relic upgrade ability or additional unit health/damage behavior.";
                if (template != null && upgrade != null && new[] { "Damage", "Health", "Cost", "Heal", "Size", "XCost", "EquipmentLimit", "UpgradeSlotCount" }
                    .Any(stat => template.Values.Stats.Value(stat) != upgrade.Stats.Value(stat))) return "Relic card upgrade numeric payload differs.";
            }
            return null;
        }

        public static RelicCardModifierResult Apply(CardInstanceState source, IReadOnlyList<CombatRelicState>? relics,
            bool resetTemporary = true)
        {
            string? error = RelicModel.Validate(relics);
            if (error != null) return Fail(error);
            if (!resetTemporary && relics?.Any(relic => relic.CardModifiers?.Count > 0) != true) return new RelicCardModifierResult(source);
            error = CardModifierModel.UnsupportedReason(source);
            if (error != null) return Fail(error);
            if (source.MaskDescriptor == null) return Fail("Relic card modifiers require branch-owned mask metadata.");
            var card = source;
            var owned = CardBranchMaskModel.OwnedState(card);
            int next = 1;
            CardLifecycleUpgrade Payload(CardUpgradeModifier upgrade) => new CardLifecycleUpgrade(upgrade.MaskMetadata!.Current(upgrade),
                upgrade.AssetKey, upgrade.Unique, upgrade.RemoveOnDiscard, Array.Empty<CardTraitValue>(), Array.Empty<string>(),
                false, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<CardLifecycleTrigger>(), upgrade.CloneDamageBase,
                upgrade.CloneHealBase, next++);
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
            for (int relicIndex = 0; relicIndex < (relics?.Count ?? 0); relicIndex++)
            foreach (var effect in relics![relicIndex].CardModifiers ?? Array.Empty<RelicCardModifier>())
            {
                var applied = RelicCardUpgradeModel.Apply(state, effect.Rule, next);
                state = applied.State; next = applied.NextUpgradeInstanceId;
                if (applied.UpgradeAdded) card = card.WithModifiers(card.Permanent, UnitModifierModel.Add(card.Temporary, effect.Upgrade!));
                dispatches.Add(new RelicCardModifierDispatch(relicIndex, effect.EffectIndex, applied.Returned, applied.UpgradeAdded, applied.Filters));
            }
            return new RelicCardModifierResult(card, dispatches);
        }
        private static RelicCardModifierResult Fail(string error) => new RelicCardModifierResult(null, unsupportedReason: error);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicStatusAddedEffect
    {
        public int UniqueRequiredStatusCount { get; }
        public bool TriggerAtExactCount { get; }
        public string OuterTargetMode { get; }
        public bool CanApplyInPreviewMode { get; }
        public string CardEffectType { get; }
        public string TargetMode { get; }
        public bool TargetHeroes { get; }
        public bool TargetMonsters { get; }
        public int Adjustment { get; }
        public int StatusFilter { get; }
        public bool RequireValidTargets { get; }
        public IReadOnlyList<string> StatusIds { get; }

        public RelicStatusAddedEffect(int uniqueRequiredStatusCount, bool triggerAtExactCount, string outerTargetMode,
            bool canApplyInPreviewMode, string cardEffectType, string targetMode, bool targetHeroes,
            bool targetMonsters, int adjustment, int statusFilter, bool requireValidTargets,
            IReadOnlyList<string> statusIds)
        {
            UniqueRequiredStatusCount = uniqueRequiredStatusCount;
            TriggerAtExactCount = triggerAtExactCount;
            OuterTargetMode = outerTargetMode;
            CanApplyInPreviewMode = canApplyInPreviewMode;
            CardEffectType = cardEffectType;
            TargetMode = targetMode;
            TargetHeroes = targetHeroes;
            TargetMonsters = targetMonsters;
            Adjustment = adjustment;
            StatusFilter = statusFilter;
            RequireValidTargets = requireValidTargets;
            StatusIds = Array.AsReadOnly(statusIds.ToArray());
        }
    }

    public sealed class RelicCharacterTriggers
    {
        public int EffectIndex { get; }
        public bool SourcePlayer { get; }
        public bool SourceEnemy { get; }
        public string? RequiredSubtype { get; }
        public bool SubtypeIsNone { get; }
        public bool SubtypeIsPyre { get; }
        public bool NoCountModifiersAllowed { get; }
        public IReadOnlyList<string> CharacterAssetKeys { get; }
        public IReadOnlyList<CombatTrigger> Triggers { get; }
        public IReadOnlyList<RelicConditionState> Conditions { get; }
        public IReadOnlyList<string> ExternalInteractions { get; }
        public RelicStatusAddedEffect? StatusAddedEffect { get; }
        public RelicCharacterTriggers(int effectIndex, bool sourcePlayer, bool sourceEnemy,
            IReadOnlyList<CombatTrigger> triggers, bool noCountModifiersAllowed, string? requiredSubtype = null,
            bool subtypeIsNone = true, bool subtypeIsPyre = false, IReadOnlyList<string>? characterAssetKeys = null,
            IReadOnlyList<RelicConditionState>? conditions = null, IReadOnlyList<string>? externalInteractions = null,
            RelicStatusAddedEffect? statusAddedEffect = null)
        {
            EffectIndex = effectIndex; SourcePlayer = sourcePlayer; SourceEnemy = sourceEnemy;
            RequiredSubtype = requiredSubtype; SubtypeIsNone = subtypeIsNone; SubtypeIsPyre = subtypeIsPyre;
            NoCountModifiersAllowed = noCountModifiersAllowed;
            CharacterAssetKeys = Array.AsReadOnly((characterAssetKeys ?? Array.Empty<string>()).ToArray());
            Triggers = Array.AsReadOnly(triggers.ToArray());
            Conditions = Array.AsReadOnly((conditions ?? Array.Empty<RelicConditionState>()).ToArray());
            ExternalInteractions = Array.AsReadOnly((externalInteractions ?? Array.Empty<string>()).ToArray());
            StatusAddedEffect = statusAddedEffect;
        }
        internal RelicCharacterTriggers WithConditions(IReadOnlyList<RelicConditionState> conditions) => new RelicCharacterTriggers(
            EffectIndex, SourcePlayer, SourceEnemy, Triggers, NoCountModifiersAllowed, RequiredSubtype, SubtypeIsNone,
            SubtypeIsPyre, CharacterAssetKeys, conditions, ExternalInteractions, StatusAddedEffect);
    }

    internal static class RelicCharacterTriggerModel
    {
        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type == RelicModel.AddTrigger || item.type == RelicModel.AddEffectOnStatusAdded)
                .Select(item => item.index).ToArray();
            if (indices.Length == 0) return relic.TriggerAdditions?.Count > 0 ? "Unexpected relic character triggers." : null;
            if (relic.IsCovenant == null || relic.DisallowedInPlacementPhase == null || relic.TriggerAdditions == null ||
                !indices.SequenceEqual(relic.TriggerAdditions.Select(rule => rule.EffectIndex)))
                return "Missing ordered native relic character trigger definitions.";
            foreach (RelicCharacterTriggers rule in relic.TriggerAdditions)
            {
                if (rule.ExternalInteractions.Count > 0) return string.Join("; ", rule.ExternalInteractions);
                if (!rule.SubtypeIsNone && string.IsNullOrEmpty(rule.RequiredSubtype) ||
                    rule.CharacterAssetKeys.Any(string.IsNullOrEmpty) ||
                    rule.Conditions.Any(condition => condition.Comparator < 0 || condition.Comparator > 7))
                    return "Malformed native relic character trigger definition.";
                bool installsTriggers = relic.EffectTypes[rule.EffectIndex] == RelicModel.AddTrigger;
                if (installsTriggers != (rule.StatusAddedEffect == null) ||
                    installsTriggers && rule.Triggers.Count == 0 || !installsTriggers && rule.Triggers.Count != 0)
                    return "Malformed native relic character-action definition.";
                if (!installsTriggers)
                {
                    string? actionError = RelicStatusAddedEffectModel.Validate(relic, rule);
                    if (actionError != null) return actionError;
                }
            }
            return null;
        }

        internal static RoomCombatResult Apply(RoomCombatState source, int unitId, RelicCharacterTriggers rule)
        {
            RelicConditionEvaluation evaluated = RelicConditionModel.Evaluate(source.Context!, rule.Conditions);
            if (!evaluated.Supported) return Fail(evaluated.UnsupportedReason!);
            RoomCombatState state = new RoomCombatState(source.RoomIndex, source.Deployment, source.Units,
                source.ExternalInteractions, evaluated.Context, source.Preview, source.ActiveCompanionBossIds);
            CombatUnit? actor = state.Units.FirstOrDefault(unit => unit.Id == unitId);
            if (actor == null) return Fail("Relic trigger installation requires a retained native actor.");
            bool character = rule.CharacterAssetKeys.Count == 0
                ? (actor.Team == CombatTeam.Player ? rule.SourcePlayer : rule.SourceEnemy)
                : rule.CharacterAssetKeys.Contains(actor.AssetKey, StringComparer.Ordinal);
            bool subtype = rule.SubtypeIsNone ? actor.Team == CombatTeam.Enemy || !actor.IsPyre : actor.Subtypes.Contains(rule.RequiredSubtype!) &&
                (actor.Team == CombatTeam.Enemy || rule.SubtypeIsPyre == actor.IsPyre);
            if (!evaluated.Passed || !character || !subtype || state.Preview || rule.Triggers.Count == 0) return Match(state);
            if (!actor.NextTriggerId.HasValue) return Fail("Missing native trigger allocation for relic installation.");
            var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
            int next = actor.NextTriggerId.Value;
            foreach (CombatTrigger definition in rule.Triggers)
            {
                actor = state.Units.Single(unit => unit.Id == unitId);
                var triggers = actor.Triggers.ToList();
                // Each installation creates a fresh runtime object. Native prepending
                // reverses the authored list and retains an empty non-equipment origin.
                var installed = new CombatTrigger(definition.Kind, definition.Once, false, definition.IgnoreSilence,
                    1, definition.Effects, definition.SkipDuringDeployment, definition.TriggerAtThreshold,
                    new CombatTriggerOrigin("", 0, false, definition.Origin?.OnlyIfEquipped == true), next++,
                    definition.Conditions, definition.RemoveOnRelentlessChange, rule.NoCountModifiersAllowed);
                triggers.Insert(0, installed);
                actor = actor.WithTriggerAllocation(triggers, next);
                state = new RoomCombatState(state.RoomIndex, state.Deployment,
                    state.Units.Select(unit => unit.Id == unitId ? actor : unit).ToArray(), state.ExternalInteractions,
                    state.Context, state.Preview, state.ActiveCompanionBossIds);
                // CharacterState.AddTrigger updates Deathwish immediately for
                // OnDeathwish, before the next authored trigger is prepended.
                if (definition.Kind == "OnDeathwish")
                {
                    RoomCombatResult updated = DeathwishModel.Update(state);
                    if (!updated.Supported) return updated;
                    state = updated.State!; callbacks.AddRange(updated.PendingCallbacks);
                }
            }
            actor = state.Units.Single(unit => unit.Id == unitId);
            actor = TriggerCountModel.Refresh(state.Context, actor, out string? error);
            if (error != null) return Fail(error);
            state = new RoomCombatState(state.RoomIndex, state.Deployment,
                state.Units.Select(unit => unit.Id == unitId ? actor : unit).ToArray(), state.ExternalInteractions,
                state.Context, state.Preview, state.ActiveCompanionBossIds);
            error = RoomCombatModel.Validate(state);
            return error == null ? new RoomCombatResult(state, RoomOutcome.Exchanged, 0, new List<CombatEvent>(),
                pendingCallbacks: callbacks) : Fail(error);
            // This native effect does not notify its relic or record its conditions.
        }
        private static RoomCombatResult Match(RoomCombatState state) => new RoomCombatResult(state, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
        private static RoomCombatResult Fail(string error) => new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error);
    }
}

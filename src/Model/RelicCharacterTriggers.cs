using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
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
        public RelicCharacterTriggers(int effectIndex, bool sourcePlayer, bool sourceEnemy,
            IReadOnlyList<CombatTrigger> triggers, bool noCountModifiersAllowed, string? requiredSubtype = null,
            bool subtypeIsNone = true, bool subtypeIsPyre = false, IReadOnlyList<string>? characterAssetKeys = null,
            IReadOnlyList<RelicConditionState>? conditions = null, IReadOnlyList<string>? externalInteractions = null)
        {
            EffectIndex = effectIndex; SourcePlayer = sourcePlayer; SourceEnemy = sourceEnemy;
            RequiredSubtype = requiredSubtype; SubtypeIsNone = subtypeIsNone; SubtypeIsPyre = subtypeIsPyre;
            NoCountModifiersAllowed = noCountModifiersAllowed;
            CharacterAssetKeys = Array.AsReadOnly((characterAssetKeys ?? Array.Empty<string>()).ToArray());
            Triggers = Array.AsReadOnly(triggers.ToArray());
            Conditions = Array.AsReadOnly((conditions ?? Array.Empty<RelicConditionState>()).ToArray());
            ExternalInteractions = Array.AsReadOnly((externalInteractions ?? Array.Empty<string>()).ToArray());
        }
        internal RelicCharacterTriggers WithConditions(IReadOnlyList<RelicConditionState> conditions) => new RelicCharacterTriggers(
            EffectIndex, SourcePlayer, SourceEnemy, Triggers, NoCountModifiersAllowed, RequiredSubtype, SubtypeIsNone,
            SubtypeIsPyre, CharacterAssetKeys, conditions, ExternalInteractions);
    }

    internal static class RelicCharacterTriggerModel
    {
        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type == RelicModel.AddTrigger).Select(item => item.index).ToArray();
            if (indices.Length == 0) return relic.TriggerAdditions?.Count > 0 ? "Unexpected relic character triggers." : null;
            if (relic.IsCovenant == null || relic.DisallowedInPlacementPhase == null || relic.TriggerAdditions == null ||
                !indices.SequenceEqual(relic.TriggerAdditions.Select(rule => rule.EffectIndex)))
                return "Missing ordered native relic character trigger definitions.";
            foreach (RelicCharacterTriggers rule in relic.TriggerAdditions)
            {
                if (rule.ExternalInteractions.Count > 0) return string.Join("; ", rule.ExternalInteractions);
                if (!rule.SubtypeIsNone && string.IsNullOrEmpty(rule.RequiredSubtype) ||
                    rule.Conditions.Any(condition => condition.Comparator < 0 || condition.Comparator > 7))
                    return "Malformed native relic character trigger definition.";
                // Definition-ID matching is deliberately guarded until captured independently.
                if (rule.CharacterAssetKeys.Count > 0) return "Relic added-trigger character definition filters are unmodeled.";
            }
            return null;
        }

        internal static RoomCombatResult Apply(RoomCombatState source, int unitId, RelicCharacterTriggers rule)
        {
            RelicConditionEvaluation evaluated = RelicConditionModel.Evaluate(source.Context!, rule.Conditions);
            if (!evaluated.Supported) return Fail(evaluated.UnsupportedReason!);
            RoomCombatState state = new RoomCombatState(source.RoomIndex, source.Deployment, source.Units,
                source.ExternalInteractions, evaluated.Context, source.Preview);
            CombatUnit? actor = state.Units.FirstOrDefault(unit => unit.Id == unitId);
            if (actor == null) return Fail("Relic trigger installation requires a retained native actor.");
            bool team = actor.Team == CombatTeam.Player ? rule.SourcePlayer : rule.SourceEnemy;
            bool subtype = rule.SubtypeIsNone ? actor.Team == CombatTeam.Enemy || !actor.IsPyre : actor.Subtypes.Contains(rule.RequiredSubtype!) &&
                (actor.Team == CombatTeam.Enemy || rule.SubtypeIsPyre == actor.IsPyre);
            if (!evaluated.Passed || !team || !subtype || state.Preview || rule.Triggers.Count == 0) return Match(state);
            if (!actor.NextTriggerId.HasValue) return Fail("Missing native trigger allocation for relic installation.");
            var triggers = actor.Triggers.ToList();
            int next = actor.NextTriggerId.Value;
            foreach (CombatTrigger definition in rule.Triggers)
            {
                // Each installation creates a fresh runtime object. Native prepending
                // reverses the authored list and retains an empty non-equipment origin.
                var installed = new CombatTrigger(definition.Kind, definition.Once, false, definition.IgnoreSilence,
                    1, definition.Effects, definition.SkipDuringDeployment, definition.TriggerAtThreshold,
                    new CombatTriggerOrigin("", 0, false, definition.Origin?.OnlyIfEquipped == true), next++,
                    definition.Conditions, definition.RemoveOnRelentlessChange, rule.NoCountModifiersAllowed);
                triggers.Insert(0, installed);
            }
            actor = actor.WithTriggerAllocation(triggers, next);
            actor = TriggerCountModel.Refresh(state.Context, actor, out string? error);
            if (error != null) return Fail(error);
            state = new RoomCombatState(state.RoomIndex, state.Deployment,
                state.Units.Select(unit => unit.Id == unitId ? actor : unit).ToArray(), state.ExternalInteractions,
                state.Context, state.Preview);
            error = RoomCombatModel.Validate(state);
            return error == null ? Match(state) : Fail(error);
            // This native effect does not notify its relic or record its conditions.
        }
        private static RoomCombatResult Match(RoomCombatState state) => new RoomCombatResult(state, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
        private static RoomCombatResult Fail(string error) => new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error);
    }
}

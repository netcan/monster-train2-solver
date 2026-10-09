using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicConditionState
    {
        public CardStatisticQuery Query { get; }
        public bool TrackTriggerCount { get; }
        public int Comparator { get; }
        public int Value { get; }
        public bool AllowMultiple { get; }
        public bool Triggered { get; }
        public int ValueAtLastTrigger { get; }
        public int DurationTriggerCount { get; }
        public RelicConditionState(CardStatisticQuery query, bool trackTriggerCount, int comparator, int value,
            bool allowMultiple, bool triggered = false, int valueAtLastTrigger = 0, int durationTriggerCount = 0)
        { Query = query; TrackTriggerCount = trackTriggerCount; Comparator = comparator; Value = value;
            AllowMultiple = allowMultiple; Triggered = triggered; ValueAtLastTrigger = valueAtLastTrigger; DurationTriggerCount = durationTriggerCount; }
        internal RelicConditionState Recorded(int current) => new RelicConditionState(Query, TrackTriggerCount, Comparator,
            Value, AllowMultiple, true, current, unchecked(DurationTriggerCount + 1));
        internal RelicConditionState Reset(string duration) => Query.Duration == duration ?
            new RelicConditionState(Query, TrackTriggerCount, Comparator, Value, AllowMultiple) : this;
    }

    public sealed class RelicSpawnStatus
    {
        public int EffectIndex { get; }
        public bool TargetPlayer { get; }
        public bool TargetEnemy { get; }
        public IReadOnlyList<CombatStatus> Statuses { get; }
        public string? RequiredSubtype { get; }
        public bool SubtypeIsNone { get; }
        public bool SubtypeIsPyre { get; }
        public IReadOnlyList<string> ExcludedSubtypes { get; }
        public int? RestrictedRoom { get; }
        public int HpPercentAsStacks { get; }
        public string SourceKind { get; }
        public bool RequireGraft { get; }
        public IReadOnlyList<string> CharacterAssetKeys { get; }
        public IReadOnlyList<RelicConditionState> Conditions { get; }
        public RelicSpawnStatus(int effectIndex, bool targetPlayer, bool targetEnemy, IReadOnlyList<CombatStatus> statuses,
            string? requiredSubtype = null, bool subtypeIsNone = true, bool subtypeIsPyre = false,
            IReadOnlyList<string>? excludedSubtypes = null, int? restrictedRoom = null, int hpPercentAsStacks = 0,
            string sourceKind = "", bool requireGraft = false, IReadOnlyList<string>? characterAssetKeys = null,
            IReadOnlyList<RelicConditionState>? conditions = null)
        { EffectIndex = effectIndex; TargetPlayer = targetPlayer; TargetEnemy = targetEnemy;
            Statuses = Array.AsReadOnly(statuses.ToArray()); RequiredSubtype = requiredSubtype;
            SubtypeIsNone = subtypeIsNone; SubtypeIsPyre = subtypeIsPyre;
            ExcludedSubtypes = Array.AsReadOnly((excludedSubtypes ?? Array.Empty<string>()).ToArray());
            RestrictedRoom = restrictedRoom; HpPercentAsStacks = hpPercentAsStacks; SourceKind = sourceKind; RequireGraft = requireGraft;
            CharacterAssetKeys = Array.AsReadOnly((characterAssetKeys ?? Array.Empty<string>()).ToArray());
            Conditions = Array.AsReadOnly((conditions ?? Array.Empty<RelicConditionState>()).ToArray()); }
        internal RelicSpawnStatus WithConditions(IReadOnlyList<RelicConditionState> conditions) =>
            new RelicSpawnStatus(EffectIndex, TargetPlayer, TargetEnemy, Statuses, RequiredSubtype, SubtypeIsNone, SubtypeIsPyre,
                ExcludedSubtypes, RestrictedRoom, HpPercentAsStacks, SourceKind, RequireGraft, CharacterAssetKeys, conditions);
    }

    public static class RelicSpawnStatusModel
    {
        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type == RelicModel.AddStatusOnSpawn).Select(item => item.index).ToArray();
            if (indices.Length == 0) return relic.SpawnStatuses?.Count > 0 ? "Unexpected relic spawn status definitions." : null;
            if (relic.IsCovenant == null || relic.DisallowedInPlacementPhase == null || relic.SpawnStatuses == null ||
                !indices.SequenceEqual(relic.SpawnStatuses.Select(rule => rule.EffectIndex)))
                return "Missing ordered native relic spawn status definitions.";
            foreach (RelicSpawnStatus rule in relic.SpawnStatuses)
            {
                if (!rule.SubtypeIsNone && string.IsNullOrEmpty(rule.RequiredSubtype) ||
                    rule.ExcludedSubtypes.Any(string.IsNullOrEmpty) || rule.CharacterAssetKeys.Any(string.IsNullOrEmpty) ||
                    rule.Conditions.Any(condition => condition.Comparator < 0 || condition.Comparator > 7))
                    return "Malformed native relic spawn status definition.";
                if (rule.Statuses.Any(status => !RoomCombatModel.KnowsStatus(status.Id)))
                    return "Unmodeled relic spawn status.";
            }
            return null;
        }

        internal static CombatContext EndDuration(CombatContext context, string duration) => context.Relics == null ? context :
            context.WithRelics(context.Relics.Select(relic => relic.SpawnStatuses == null ? relic :
                relic.WithSpawnStatuses(relic.SpawnStatuses.Select(rule => rule.WithConditions(rule.Conditions.Select(condition =>
                    condition.Reset(duration)).ToArray())).ToArray())).ToArray());

        // Native CharacterAdded leaves status callbacks on the shared queue for its caller.
        public static RoomCombatResult CharacterAdded(RoomCombatState source, int unitId, int fromCardId, bool onlyCovenants)
        {
            string? error = RelicModel.Validate(source.Context);
            if (error != null) return Fail(error);
            if (source.Context?.Relics == null || !source.Context.Relics.Any(relic => relic.SpawnStatuses?.Count > 0)) return Match(source);
            if (!source.Units.Any(unit => unit.Id == unitId) || fromCardId < 0 ||
                fromCardId > 0 && source.Context.FindCard(fromCardId) == null) return Fail("Missing native relic birth references.");
            RoomCombatState state = source;
            var callbacks = new List<RoomCombatModel.QueuedCharacterTrigger>();
            var events = new List<CombatEvent>();
            for (int relicIndex = 0; relicIndex < state.Context!.Relics!.Count; relicIndex++)
            {
                CombatRelicState relic = state.Context!.Relics![relicIndex];
                if (relic.SpawnStatuses == null || relic.IsCovenant != onlyCovenants ||
                    relic.DisallowedInPlacementPhase == true && state.Deployment) continue;
                for (int ruleIndex = 0; ruleIndex < relic.SpawnStatuses!.Count; ruleIndex++)
                {
                    RelicSpawnStatus rule = relic.SpawnStatuses[ruleIndex];
                    bool admitted = true;
                    foreach (RelicConditionState condition in rule.Conditions)
                    {
                        if (!condition.AllowMultiple && condition.Triggered) { admitted = false; break; }
                        int value = Value(condition);
                        if (error != null) return Fail(error);
                        int delta = unchecked(value - condition.ValueAtLastTrigger);
                        if (!(condition.TrackTriggerCount ? delta < condition.Value :
                            (condition.Comparator & 2) != 0 && delta == condition.Value ||
                            (condition.Comparator & 4) != 0 && delta > condition.Value ||
                            (condition.Comparator & 1) != 0 && delta < condition.Value)) { admitted = false; break; }
                    }
                    if (!admitted) continue;
                    CombatUnit? actor = state.Units.FirstOrDefault(unit => unit.Id == unitId);
                    if (actor == null) return Fail("Relic birth removal requires a retained native actor.");
                    if (rule.CharacterAssetKeys.Count > 0 && !rule.CharacterAssetKeys.Contains(actor.AssetKey)) continue;
                    if (fromCardId > 0 && actor.Modifiers == null) return Fail("Relic birth requires source-definition matching metadata.");
                    bool fromCard = fromCardId > 0 && actor.Modifiers!.SpawnerMatchesDefinition && actor.Status("cardless") == null;
                    if (fromCard && rule.SourceKind == "NoCard" || !fromCard && rule.SourceKind == "OnlyFromCard" ||
                        actor.Team == CombatTeam.Player && !rule.TargetPlayer || actor.Team == CombatTeam.Enemy && !rule.TargetEnemy ||
                        !rule.SubtypeIsPyre && actor.Status("immune") != null || actor.Status("purify") != null || actor.Status("untouchable") != null)
                        continue;
                    if (rule.RequireGraft) return Fail("Relic birth requires native graft definition and source-card graft metadata.");
                    if (rule.ExcludedSubtypes.Any(actor.Subtypes.Contains) || rule.RestrictedRoom.HasValue && rule.RestrictedRoom != state.RoomIndex ||
                        rule.Statuses.Count == 0) continue;
                    RngDraw draw = state.Context!.BattleRng.Range(0, rule.Statuses.Count);
                    SetContext(state.Context.WithBattleRng(draw.State));
                    int stacks = rule.HpPercentAsStacks > 0 ? (int)Math.Floor((float)actor.MaxHealth * ((float)rule.HpPercentAsStacks / 100f)) :
                        Math.Max(1, rule.Statuses[draw.Value].Stacks);
                    // Selection is before the required-subtype test, including a one-element list.
                    bool matches = rule.SubtypeIsNone ? actor.Team == CombatTeam.Enemy || !actor.IsPyre :
                        actor.Subtypes.Contains(rule.RequiredSubtype!) && (actor.Team == CombatTeam.Enemy || (rule.SubtypeIsPyre == actor.IsPyre));
                    if (!matches) continue;
                    RoomCombatResult added = StatusApplicationModel.ApplyRetained(state, actor.Id,
                        rule.Statuses[draw.Value].WithStacks(stacks), 0, overrideImmunity: rule.SubtypeIsPyre);
                    if (!added.Supported) return added;
                    state = added.State!; callbacks.AddRange(added.PendingCallbacks); events.AddRange(added.Events);
                    var conditions = new List<RelicConditionState>();
                    foreach (RelicConditionState condition in rule.Conditions)
                    {
                        int value = Value(condition);
                        if (error != null) return Fail(error);
                        conditions.Add(condition.Recorded(value));
                    }
                    var rules = relic.SpawnStatuses.ToArray(); rules[ruleIndex] = rule.WithConditions(conditions);
                    relic = relic.WithSpawnStatuses(rules);
                    var relics = state.Context!.Relics!.ToArray(); relics[relicIndex] = relic;
                    SetContext(state.Context.WithRelics(relics));
                }
            }
            return new RoomCombatResult(state, RoomOutcome.Exchanged, 0, events, pendingCallbacks: callbacks);

            int Value(RelicConditionState condition)
            {
                if (condition.TrackTriggerCount) return condition.DurationTriggerCount;
                StatisticQueryResult query = StatisticQueryModel.Evaluate(state.Context!, condition.Query);
                if (!query.Supported) { error = query.UnsupportedReason; return 0; }
                SetContext(query.Context!); return query.Value;
            }
            void SetContext(CombatContext context) => state = new RoomCombatState(state.RoomIndex, state.Deployment,
                state.Units, state.ExternalInteractions, context, state.Preview);
        }
        private static RoomCombatResult Match(RoomCombatState state) => new RoomCombatResult(state, RoomOutcome.Exchanged, 0, new List<CombatEvent>());
        private static RoomCombatResult Fail(string error) => new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error);
    }
}

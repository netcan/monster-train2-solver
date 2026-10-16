using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicSummonDamageRule
    {
        public int EffectIndex { get; }
        public int DamageAmount { get; }
        public string TargetTeam { get; }
        public string TargetMode { get; }
        public bool CanApplyInPreview { get; }
        public int ConditionCount { get; }
        public IReadOnlyList<RelicConditionState> Conditions { get; }

        public RelicSummonDamageRule(int effectIndex, int damageAmount, string targetTeam, string targetMode,
            bool canApplyInPreview, IReadOnlyList<RelicConditionState>? conditions = null, int conditionCount = 0)
        {
            EffectIndex = effectIndex; DamageAmount = damageAmount; TargetTeam = targetTeam; TargetMode = targetMode;
            CanApplyInPreview = canApplyInPreview;
            Conditions = Array.AsReadOnly((conditions ?? Array.Empty<RelicConditionState>()).ToArray());
            ConditionCount = conditions?.Count ?? conditionCount;
        }

        internal RelicSummonDamageRule WithConditions(IReadOnlyList<RelicConditionState> conditions) =>
            new RelicSummonDamageRule(EffectIndex, DamageAmount, TargetTeam, TargetMode, CanApplyInPreview, conditions);
    }

    public sealed class RelicSummonDamageResult
    {
        public TrainCombatState? State { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => State != null;
        public RoomOutcome Outcome { get; }
        public IReadOnlyList<int> TargetUnitIds { get; }
        public IReadOnlyList<string> Notifications { get; }
        public IReadOnlyList<CombatEvent> Events { get; }
        internal IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger> PendingCallbacks { get; }
        internal IReadOnlyList<CharacterTriggerDispatch> Dispatches { get; }
        internal IReadOnlyList<UnitCloneCallback> Dispatched { get; }
        internal RelicSummonDamageResult(TrainCombatState? state, string? error,
            IReadOnlyList<int>? targets = null, IReadOnlyList<string>? notifications = null,
            IReadOnlyList<CombatEvent>? events = null, RoomOutcome outcome = RoomOutcome.Exchanged,
            IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger>? pending = null,
            IReadOnlyList<CharacterTriggerDispatch>? dispatches = null, IReadOnlyList<UnitCloneCallback>? dispatched = null)
        { State = state; UnsupportedReason = error; TargetUnitIds = Array.AsReadOnly((targets ?? Array.Empty<int>()).ToArray());
            Notifications = Array.AsReadOnly((notifications ?? Array.Empty<string>()).ToArray());
            Events = Array.AsReadOnly((events ?? Array.Empty<CombatEvent>()).ToArray()); Outcome = outcome;
            PendingCallbacks = Array.AsReadOnly((pending ?? Array.Empty<RoomCombatModel.QueuedCharacterTrigger>()).ToArray());
            Dispatches = Array.AsReadOnly((dispatches ?? Array.Empty<CharacterTriggerDispatch>()).ToArray());
            Dispatched = Array.AsReadOnly((dispatched ?? Array.Empty<UnitCloneCallback>()).ToArray()); }
    }

    public static class RelicSummonDamageModel
    {
        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type == RelicModel.DamageOnMonsterSummoned)
                .Select(item => item.index).ToArray();
            if (indices.Length == 0)
                return relic.SummonDamageRules?.Count > 0 ? "Unexpected summon-damage relic rules." : null;
            if (relic.SummonDamageRules == null || !indices.SequenceEqual(relic.SummonDamageRules.Select(rule => rule.EffectIndex)))
                return "Missing ordered native summon-damage definition.";
            foreach (RelicSummonDamageRule rule in relic.SummonDamageRules)
                if (rule.DamageAmount <= 0 || rule.TargetMode != "Tower" || rule.TargetTeam != "Heroes" ||
                    !rule.CanApplyInPreview || rule.ConditionCount != rule.Conditions.Count ||
                    rule.Conditions.Any(condition => condition.Comparator < 0 || condition.Comparator > 7))
                    return "Unsupported native summon-damage parameters.";
            return null;
        }

        public static RelicSummonDamageResult CharacterAdded(TrainCombatState source, int unitId)
            => CharacterAdded(source, unitId, Array.Empty<RoomCombatModel.QueuedCharacterTrigger>());

        internal static RelicSummonDamageResult CharacterAdded(TrainCombatState source, int unitId,
            IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger> initialPending)
        {
            CombatContext? initial = source.Context;
            if (initial?.Relics == null) return Match(source);
            string? error = RelicModel.Validate(initial);
            if (error != null) return Fail(error);
            CombatUnit? actor = source.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == unitId);
            if (actor == null) return Fail("Missing summon-damage birth actor.");

            TrainCombatState state = source;
            var targets = new List<int>();
            var notifications = new List<string>();
            var events = new List<CombatEvent>();
            var pending = initialPending.ToList();
            var dispatches = new List<CharacterTriggerDispatch>();
            var dispatched = new List<UnitCloneCallback>();
            RoomOutcome outcome = RoomOutcome.Exchanged;
            for (int relicIndex = 0; relicIndex < state.Context!.Relics!.Count; relicIndex++)
            {
                CombatRelicState relic = state.Context.Relics[relicIndex];
                if (relic.IsCovenant != false || relic.DisallowedInPlacementPhase == true &&
                    state.Rooms.First(room => room.Units.Any(unit => unit.Id == unitId)).Deployment) continue;
                foreach (int effectIndex in relic.EffectTypes.Select((type, index) => new { type, index })
                    .Where(item => item.type == RelicModel.DamageOnMonsterSummoned).Select(item => item.index))
                {
                    relic = state.Context!.Relics![relicIndex];
                    RelicSummonDamageRule rule = relic.SummonDamageRules!.Single(item => item.EffectIndex == effectIndex);
                    RelicConditionEvaluation evaluation = RelicConditionModel.Evaluate(state.Context!, rule.Conditions);
                    if (!evaluation.Supported) return Fail(evaluation.UnsupportedReason!, targets, notifications, events);
                    state = CardSpellModel.WithContext(state, evaluation.Context);
                    if (!evaluation.Passed || actor.Team != CombatTeam.Player || actor.IsPyre) continue;

                    RoomCombatState actorRoom = state.Rooms.Single(room => room.Units.Any(unit => unit.Id == unitId));
                    IEnumerable<CombatUnit> ordered = state.Rooms.OrderByDescending(room => room.RoomIndex)
                        .SelectMany(room => BattleSpawnPointModel.Order(state.Context!.SpawnPoints, room.RoomIndex, room.Units)
                            .Where(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0 &&
                                unit.DeathState?.IsDespawned != true && unit.DeathState?.IsDestroyed != true));
                    CombatUnit[] candidates = ordered.ToArray();
                    if (state.Context!.SpawnPoints == null && candidates.GroupBy(unit =>
                        state.Rooms.Single(room => room.Units.Any(item => item.Id == unit.Id)).RoomIndex).Any(group => group.Count() > 1))
                        return Fail("Tower summon damage requires captured physical order for multiple targets on one floor.", targets, notifications, events);

                    int? previousRoom = null;
                    foreach (CombatUnit candidate in candidates)
                    {
                        RoomCombatState targetRoom = state.Rooms.Single(room => room.Units.Any(unit => unit.Id == candidate.Id));
                        CombatUnit? target = targetRoom.Units.FirstOrDefault(unit => unit.Id == candidate.Id);
                        if (target == null || target.Health <= 0 || target.DeathState?.IsDestroyed == true ||
                            target.DeathState?.IsDespawned == true || target.Statuses.Any(status => status.Id == "untouchable")) continue;

                        if (previousRoom != targetRoom.RoomIndex)
                        {
                            if (!RemoveDeadCharacters(ref state, pending, events, dispatches, dispatched, ref outcome, out error))
                                return Fail(error!, targets, notifications, events, pending, dispatches, dispatched);
                            notifications.Add("Room:" + targetRoom.RoomIndex);
                            if (!RecordNotification(ref state, relicIndex, effectIndex, out error))
                                return Fail(error!, targets, notifications, events, pending, dispatches, dispatched);
                            previousRoom = targetRoom.RoomIndex;
                        }
                        notifications.Add("Unit:" + target.Id);
                        if (!RecordNotification(ref state, relicIndex, effectIndex, out error))
                            return Fail(error!, targets, notifications, events, pending, dispatches, dispatched);

                        targetRoom = state.Rooms.Single(room => room.Units.Any(unit => unit.Id == target.Id));
                        RoomCombatResult damaged = RoomCombatModel.ApplyRelicDamage(targetRoom, target.Id, rule.DamageAmount,
                            callback => pending.Add(callback.WithRelicDamageRemoval()));
                        if (!damaged.Supported) return Fail(damaged.UnsupportedReason!, targets, notifications, events, pending, dispatches, dispatched);
                        Import(ref state, damaged.State!);
                        if (damaged.Outcome != RoomOutcome.Exchanged) outcome = damaged.Outcome;
                        targets.Add(target.Id); events.AddRange(damaged.Events); pending.AddRange(damaged.PendingCallbacks);
                        dispatches.AddRange(damaged.Dispatches);
                        dispatched.AddRange(damaged.Dispatches.Select(ToCallback));
                        if (!RemoveDeadCharacters(ref state, pending, events, dispatches, dispatched, ref outcome, out error))
                            return Fail(error!, targets, notifications, events, pending, dispatches, dispatched);
                    }
                }
            }
            if (targets.Count > 0 && !RemoveDeadCharacters(ref state, pending, events, dispatches, dispatched, ref outcome, out error))
                return Fail(error!, targets, notifications, events, pending, dispatches, dispatched);
            return new RelicSummonDamageResult(state, null, targets, notifications, events, outcome, pending, dispatches, dispatched);
        }

        private static bool RecordNotification(ref TrainCombatState state, int relicIndex, int effectIndex, out string? error)
        {
            error = null;
            RelicSummonDamageRule rule = state.Context!.Relics![relicIndex].SummonDamageRules!.Single(item => item.EffectIndex == effectIndex);
            if (rule.Conditions.Count == 0) return true;
            RelicConditionRecord record = RelicConditionModel.Record(state.Context!, rule.Conditions);
            if (!record.Supported) { error = record.UnsupportedReason; return false; }
            CombatRelicState[] relics = record.Context.Relics!.ToArray();
            RelicSummonDamageRule[] rules = relics[relicIndex].SummonDamageRules!.ToArray();
            int offset = Array.FindIndex(rules, item => item.EffectIndex == effectIndex);
            if (offset < 0) { error = "Summon-damage effect order changed while recording its condition."; return false; }
            rules[offset] = rule.WithConditions(record.Conditions);
            relics[relicIndex] = relics[relicIndex].WithSummonDamageRules(rules);
            state = CardSpellModel.WithContext(state, record.Context.WithRelics(relics));
            return true;
        }

        private static bool RemoveDeadCharacters(ref TrainCombatState state,
            List<RoomCombatModel.QueuedCharacterTrigger> pending, List<CombatEvent> events,
            List<CharacterTriggerDispatch> dispatches, List<UnitCloneCallback> dispatched,
            ref RoomOutcome outcome, out string? error)
        {
            error = null;
            if (pending.Count > 0)
            {
                UnitCloneResult drained = UnitCloneModel.Drain(new UnitCloneResult(state, 0, outcome, null,
                    Array.Empty<CombatEvent>(), Array.Empty<UnitCloneBoundary>(), pending));
                if (!drained.Supported) { error = drained.UnsupportedReason; return false; }
                state = drained.State!; pending.Clear(); events.AddRange(drained.Events);
                dispatched.AddRange(drained.Dispatched);
                dispatches.AddRange(drained.Dispatched.Select(ToDispatch));
                if (drained.Outcome != RoomOutcome.Exchanged) outcome = drained.Outcome;
            }

            foreach (RoomCombatState room in state.Rooms.ToArray())
            foreach (CombatUnit dead in room.Units.Where(unit => unit.Health <= 0 || unit.DeathState?.IsBeingRemoved == true).ToArray())
            {
                UnitSpawnPointState? point = state.Context?.SpawnPoints?.Units.FirstOrDefault(item => item.UnitId == dead.Id);
                if (point?.Current == null) continue;
                RoomCombatState current = state.Rooms.Single(item => item.RoomIndex == room.RoomIndex);
                SpawnPointGroupState? group = state.Context!.SpawnPoints!.SelectedGroup(room.RoomIndex, dead.Team);
                bool vacancyBefore = point.Current.RoomIndex == room.RoomIndex && point.Current.Team == dead.Team &&
                    point.Current.Index > 0 && group?.Occupants.Take(point.Current.Index).Any(id => id == 0) == true;
                if (vacancyBefore)
                {
                    BattleSpawnPointResult compacted = BattleSpawnPointModel.Apply(state.Context.SpawnPoints, current,
                        "Compact", dead.Team);
                    if (!compacted.Supported) { error = compacted.Error; return false; }
                    state = CardSpellModel.WithContext(state, state.Context.WithSpawnPoints(compacted.State!));
                    current = state.Rooms.Single(item => item.RoomIndex == room.RoomIndex);
                }
                RoomCombatResult removed = RoomCombatModel.SettleQueuedSpawner(current, dead);
                if (!removed.Supported) { error = removed.UnsupportedReason; return false; }
                Import(ref state, removed.State!); events.AddRange(removed.Events);
            }

            var livingEnemies = state.Rooms.SelectMany(room => room.Units)
                .Where(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0 && unit.DeathState?.IsDestroyed != true)
                .Select(unit => unit.Id).ToHashSet();
            if (state.Movement.Any(rule => !livingEnemies.Contains(rule.UnitId)))
                state = new TrainCombatState(state.Rooms, state.Movement.Where(rule => livingEnemies.Contains(rule.UnitId)).ToArray(),
                    state.EnemySlotsPerRoom, state.Context);
            return true;
        }

        private static CharacterTriggerDispatch ToDispatch(UnitCloneCallback callback) => new CharacterTriggerDispatch(
            callback.ActorId, callback.Kind, callback.DyingId, callback.TriggerCount, callback.ParamInt,
            callback.ParamInt2, callback.ParamString, callback.OverrideTargetId, callback.LastSpawnedOverrideUnitId,
            callback.ExclusiveTriggerStateId);

        private static UnitCloneCallback ToCallback(CharacterTriggerDispatch callback) => new UnitCloneCallback(
            callback.ActorId, callback.Kind, callback.ParamInt, callback.ParamInt2, callback.ParamString,
            callback.TriggerCount, callback.DyingId, callback.OverrideTargetId, callback.LastSpawnedOverrideUnitId,
            callback.ExclusiveTriggerStateId);

        private static void Import(ref TrainCombatState state, RoomCombatState changed)
        {
            CombatContext context = changed.Context!;
            RoomCombatState[] rooms = state.Rooms.Select(room => room.RoomIndex == changed.RoomIndex ? changed :
                new RoomCombatState(room.RoomIndex, room.Deployment, room.Units, room.ExternalInteractions,
                    context, room.Preview, room.ActiveCompanionBossIds)).ToArray();
            state = new TrainCombatState(rooms, state.Movement, state.EnemySlotsPerRoom, context);
        }

        private static RelicSummonDamageResult Match(TrainCombatState state) => new RelicSummonDamageResult(state, null);
        private static RelicSummonDamageResult Fail(string reason, IReadOnlyList<int>? targets = null,
            IReadOnlyList<string>? notifications = null, IReadOnlyList<CombatEvent>? events = null,
            IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger>? pending = null,
            IReadOnlyList<CharacterTriggerDispatch>? dispatches = null, IReadOnlyList<UnitCloneCallback>? dispatched = null) =>
            new RelicSummonDamageResult(null, reason, targets, notifications, events,
                pending: pending, dispatches: dispatches, dispatched: dispatched);
    }
}

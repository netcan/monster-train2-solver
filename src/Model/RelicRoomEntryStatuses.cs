using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicRoomEntryStatus
    {
        public int EffectIndex { get; }
        public int TargetRoomIndex { get; }
        public bool TargetMonsters { get; }
        public bool TargetHeroes { get; }
        public bool FirstWaveOnly { get; }
        public IReadOnlyList<CombatStatus> Statuses { get; }
        public string? MovementTrigger { get; }
        public int MovementRestrictions { get; }
        public IReadOnlyList<string> ExcludedSubtypes { get; }

        public RelicRoomEntryStatus(int effectIndex, int targetRoomIndex, bool targetMonsters, bool targetHeroes,
            bool firstWaveOnly, IReadOnlyList<CombatStatus> statuses, string? movementTrigger = null,
            int movementRestrictions = 0, IReadOnlyList<string>? excludedSubtypes = null)
        {
            EffectIndex = effectIndex; TargetRoomIndex = targetRoomIndex; TargetMonsters = targetMonsters;
            TargetHeroes = targetHeroes; FirstWaveOnly = firstWaveOnly;
            Statuses = Array.AsReadOnly(statuses.ToArray());
            MovementTrigger = movementTrigger; MovementRestrictions = movementRestrictions;
            ExcludedSubtypes = Array.AsReadOnly((excludedSubtypes ?? Array.Empty<string>()).ToArray());
        }
    }

    internal static class RelicRoomEntryStatusModel
    {
        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type == RelicModel.AddStatusOnRoomEntry ||
                    item.type == RelicModel.AddStatusOnUnitMovedUpDown).Select(item => item.index).ToArray();
            if (indices.Length == 0)
                return relic.RoomEntryStatuses?.Count > 0 ? "Unexpected relic room-entry status definitions." : null;
            if (relic.RoomEntryStatuses == null || !indices.SequenceEqual(relic.RoomEntryStatuses.Select(rule => rule.EffectIndex)))
                return "Missing ordered native relic room-entry status definitions.";
            foreach (RelicRoomEntryStatus rule in relic.RoomEntryStatuses)
            {
                if (rule.EffectIndex < 0 || rule.Statuses.Count == 0 || !rule.TargetMonsters && !rule.TargetHeroes ||
                    rule.ExcludedSubtypes.Any(string.IsNullOrWhiteSpace))
                    return "Malformed native relic room-phase status definition.";
                if (relic.EffectTypes[rule.EffectIndex] == RelicModel.AddStatusOnRoomEntry)
                {
                    if (rule.MovementTrigger != null || rule.MovementRestrictions != 0 || rule.ExcludedSubtypes.Count != 0)
                        return "Room-entry status contains unexpected movement parameters.";
                    if (rule.FirstWaveOnly)
                        return "First-wave-only relic room-entry status requires captured native battle phase.";
                }
                else if (RelicMovementStatusModel.Validate(relic, rule) is string movementError) return movementError;
                if (rule.Statuses.Any(status => !RoomCombatModel.KnowsStatus(status.Id)))
                    return "Unmodeled relic room-entry status.";
            }
            return null;
        }

        internal static RoomCombatResult ApplyAfterEntry(RoomCombatResult triggered,
            RoomCombatModel.QueuedCharacterTrigger queued, Action<RoomCombatModel.QueuedCharacterTrigger> enqueue)
        {
            if (!triggered.Supported) return triggered;
            RoomCombatState state = triggered.State!;
            CombatContext? context = state.Context;
            if (context?.Relics == null) return triggered;
            foreach (CombatRelicState relic in context.Relics)
            {
                string? error = Validate(relic);
                if (error != null) return Fail(error);
            }
            if (queued.Kind != "AfterSpawnEnchant" && queued.Kind != "PostAscension" ||
                state.Preview || state.RoomIndex != queued.RoomIndex) return triggered;
            context = state.Context!;
            CombatUnit? actor = state.Units.FirstOrDefault(unit => unit.Id == queued.Unit.Id);
            if (actor == null || actor.DeathState?.IsDestroyed == true || actor.Status("untouchable") != null)
                return triggered;

            var events = triggered.Events.ToList();
            var retained = triggered.RetainedUnits.ToDictionary(unit => unit.Id);
            for (int relicIndex = 0; relicIndex < context.Relics!.Count; relicIndex++)
            {
                CombatRelicState relic = context.Relics[relicIndex];
                if (relic.RoomEntryStatuses == null) continue;
                foreach (RelicRoomEntryStatus rule in relic.RoomEntryStatuses)
                {
                    if (rule.MovementTrigger != null) continue;
                    if (rule.TargetRoomIndex != state.RoomIndex ||
                        actor.Team == CombatTeam.Player && !rule.TargetMonsters ||
                        actor.Team == CombatTeam.Enemy && !rule.TargetHeroes) continue;
                    if (rule.Statuses.Count == 0) return Fail("Relic room-entry status selection has an empty status list.");

                    RngDraw draw = context.BattleRng.Range(0, rule.Statuses.Count);
                    state = WithContext(state, context.WithBattleRng(draw.State));
                    context = state.Context!;
                    CombatStatus selected = rule.Statuses[draw.Value].WithStacks(Math.Max(1, rule.Statuses[draw.Value].Stacks));
                    RoomCombatResult applied = StatusApplicationModel.ApplyRetained(state, actor.Id, selected, 0);
                    if (!applied.Supported) return applied;
                    state = applied.State!;
                    context = state.Context!;
                    events.AddRange(applied.Events);
                    foreach (RoomCombatModel.QueuedCharacterTrigger callback in applied.PendingCallbacks) enqueue(callback);
                    foreach (CombatUnit unit in applied.RetainedUnits) retained[unit.Id] = unit;
                    actor = state.Units.FirstOrDefault(unit => unit.Id == queued.Unit.Id) ?? actor;
                }
            }
            return new RoomCombatResult(state, triggered.Outcome, triggered.Rounds, events,
                pendingCallbacks: triggered.PendingCallbacks, retainedUnits: retained.Values.ToArray(), dispatches: triggered.Dispatches);
        }

        private static RoomCombatState WithContext(RoomCombatState state, CombatContext context) =>
            new(state.RoomIndex, state.Deployment, state.Units, state.ExternalInteractions, context, state.Preview,
                state.ActiveCompanionBossIds);

        private static RoomCombatResult Fail(string error) =>
            new(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error);
    }

    internal static class RelicMovementStatusModel
    {
        private const string RelicId = "394fb724-046f-448f-9531-f9d6caadae77";
        private const string RelicKey = "PyregelOnAscend_DH";

        internal static string? Validate(CombatRelicState relic, RelicRoomEntryStatus rule)
        {
            return relic.DataId == RelicId && relic.AssetKey == RelicKey && relic.EffectTypes.Count == 1 &&
                relic.EffectTypes[0] == RelicModel.AddStatusOnUnitMovedUpDown && rule.EffectIndex == 0 &&
                rule.TargetRoomIndex == -1 && !rule.TargetMonsters && rule.TargetHeroes && !rule.FirstWaveOnly &&
                rule.MovementTrigger == "PostAscension" && rule.MovementRestrictions == 5 &&
                rule.ExcludedSubtypes.Count == 0 && rule.Statuses.Count == 1 &&
                rule.Statuses[0].Id == "pyregel" && rule.Statuses[0].Stacks == 2 &&
                relic.DisallowedInPlacementPhase == false
                    ? null : "Unmodeled original movement-status relic or payload.";
        }

        internal static bool TryApplyBeforeMovementTrigger(RoomCombatState state,
            RoomCombatModel.QueuedCharacterTrigger queued, Action<RoomCombatModel.QueuedCharacterTrigger> enqueue,
            out RoomCombatResult result)
        {
            result = null!;
            CombatRelicState[] relics = state.Context?.Relics?.ToArray() ?? Array.Empty<CombatRelicState>();
            if (queued.RelicMovementStatusApplied || !relics.Any(relic =>
                relic.RoomEntryStatuses?.Any(rule => rule.MovementTrigger == queued.Kind) == true)) return false;
            string? error = RelicModel.Validate(relics);
            if (error != null)
            {
                result = Fail(error);
                return true;
            }
            if (state.Preview || queued.Kind != "PostAscension" || state.RoomIndex != queued.RoomIndex)
                return false;
            CombatUnit? actor = state.Units.FirstOrDefault(unit => unit.Id == queued.Unit.Id);
            if (actor == null || actor.DeathState?.IsDestroyed == true || actor.IsPyre || actor.Team != CombatTeam.Enemy ||
                actor.Status("immune") != null || actor.Status("purify") != null) return false;

            var events = new List<CombatEvent>();
            var retained = new Dictionary<int, CombatUnit>();
            foreach (CombatRelicState relic in relics)
            foreach (RelicRoomEntryStatus rule in relic.RoomEntryStatuses ?? Array.Empty<RelicRoomEntryStatus>())
            {
                if (rule.MovementTrigger != queued.Kind || rule.TargetRoomIndex != -1 ||
                    actor.Subtypes.Any(rule.ExcludedSubtypes.Contains)) continue;
                RngDraw draw = state.Context!.BattleRng.Range(0, rule.Statuses.Count);
                state = WithContext(state, state.Context.WithBattleRng(draw.State));
                RoomCombatResult applied = StatusApplicationModel.ApplyRetained(state, actor.Id,
                    rule.Statuses[draw.Value].WithStacks(Math.Max(1, rule.Statuses[draw.Value].Stacks)), 0);
                if (!applied.Supported)
                {
                    result = applied;
                    return true;
                }
                state = applied.State!;
                events.AddRange(applied.Events);
                foreach (RoomCombatModel.QueuedCharacterTrigger callback in applied.PendingCallbacks) enqueue(callback);
                foreach (CombatUnit unit in applied.RetainedUnits) retained[unit.Id] = unit;
            }
            enqueue(queued.WithRelicMovementStatusApplied());
            result = new RoomCombatResult(state, RoomOutcome.Exchanged, 0, events,
                retainedUnits: retained.Values.ToArray());
            return true;
        }

        private static RoomCombatState WithContext(RoomCombatState state, CombatContext context) =>
            new(state.RoomIndex, state.Deployment, state.Units, state.ExternalInteractions, context, state.Preview,
                state.ActiveCompanionBossIds);

        private static RoomCombatResult Fail(string error) =>
            new(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error);
    }
}

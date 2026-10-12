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

        public RelicRoomEntryStatus(int effectIndex, int targetRoomIndex, bool targetMonsters, bool targetHeroes,
            bool firstWaveOnly, IReadOnlyList<CombatStatus> statuses)
        {
            EffectIndex = effectIndex; TargetRoomIndex = targetRoomIndex; TargetMonsters = targetMonsters;
            TargetHeroes = targetHeroes; FirstWaveOnly = firstWaveOnly;
            Statuses = Array.AsReadOnly(statuses.ToArray());
        }
    }

    internal static class RelicRoomEntryStatusModel
    {
        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type == RelicModel.AddStatusOnRoomEntry).Select(item => item.index).ToArray();
            if (indices.Length == 0)
                return relic.RoomEntryStatuses?.Count > 0 ? "Unexpected relic room-entry status definitions." : null;
            if (relic.RoomEntryStatuses == null || !indices.SequenceEqual(relic.RoomEntryStatuses.Select(rule => rule.EffectIndex)))
                return "Missing ordered native relic room-entry status definitions.";
            foreach (RelicRoomEntryStatus rule in relic.RoomEntryStatuses)
            {
                if (rule.EffectIndex < 0 || rule.Statuses.Count == 0 || !rule.TargetMonsters && !rule.TargetHeroes)
                    return "Malformed native relic room-entry status definition.";
                if (rule.FirstWaveOnly)
                    return "First-wave-only relic room-entry status requires captured native battle phase.";
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
}

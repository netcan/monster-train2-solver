using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class HordeMergeResult
    {
        public RoomCombatResult Result { get; }
        public CombatUnit? SourceAfter { get; }
        public bool Supported => Result.Supported;
        internal HordeMergeResult(RoomCombatResult result, CombatUnit? sourceAfter)
        { Result = result; SourceAfter = sourceAfter; }
    }

    public static class HordeMergeModel
    {
        // Selection uses the same-team manager order; the direct API below has different gates.
        public static int Select(RoomCombatState? room, CombatUnit? source)
        {
            if (room == null || source?.Status("horde") == null) return 0;
            return room.Units.FirstOrDefault(unit => unit.Id != source.Id && unit.Team == source.Team &&
                unit.Status("horde") != null)?.Id ?? 0;
        }

        public static HordeMergeResult Clone(RoomCombatState room, int sourceId)
        {
            CombatUnit? source = room.Units.FirstOrDefault(unit => unit.Id == sourceId);
            if (sourceId == 0) return Match(room, null);
            if (source == null) return Unsupported("Missing retained Horde clone actor.");
            if (source.Status("horde") == null) return Unsupported("Ordinary unit cloning requires a birth model.");
            string? error = RoomCombatModel.Validate(room);
            if (error != null) return Unsupported(error);
            RoomCombatResult result = StatusApplicationModel.ApplyRetained(room, source.Id,
                source.RegisteredStatus("horde")!.WithStacks(1), 0);
            return new HordeMergeResult(result, result.State?.Units.FirstOrDefault(unit => unit.Id == sourceId));
        }

        // Temporary character/save states roll back, while native weak room caches remain shared.
        public static RoomCombatResult RestorePreview(RoomCombatState primary, RoomCombatState preview)
        {
            if (primary.Preview || !preview.Preview || primary.Context == null || preview.Context == null ||
                primary.Context.NextUnitId != preview.Context.NextUnitId || primary.Context.NextCardId != preview.Context.NextCardId)
                return Unsupported("Horde preview restoration requires matching primary/temporary identity contexts.").Result;
            CombatContext context = primary.Context;
            var local = primary.Units.ToDictionary(unit => unit.Id);
            foreach (CardInstanceState changed in preview.Context.CardRegistry ?? preview.Context.CardInstances ?? Array.Empty<CardInstanceState>())
            {
                CardInstanceState? original = context.FindCard(changed.InstanceId);
                if (original == null || original.DataId != changed.DataId)
                    return Unsupported("Horde preview restoration encountered an unobserved shared card.").Result;
                if (changed.PlayedRoomUnitIds == null) continue;
                if (changed.RawPlayedRoomUnitIds == null)
                    return Unsupported("Horde preview restoration requires retained raw weak room references.").Result;
                int[] visible = changed.RawPlayedRoomUnitIds.Where(id => local.TryGetValue(id, out CombatUnit? unit)
                    ? unit.Health > 0 && unit.DeathState?.IsDespawned != true && unit.DeathState?.IsDestroyed != true
                    : original.PlayedRoomUnitIds?.Contains(id) == true).ToArray();
                context = context.WithCard(original.WithRoomCacheState(visible, changed.RawPlayedRoomUnitIds));
            }
            return new RoomCombatResult(new RoomCombatState(primary.RoomIndex, primary.Deployment, primary.Units,
                primary.ExternalInteractions, context), RoomOutcome.Exchanged, 0, new List<CombatEvent>());
        }

        public static HordeMergeResult Merge(RoomCombatState room, int sourceId, int targetId, bool fromBump = false)
        {
            CombatUnit? source = room.Units.FirstOrDefault(unit => unit.Id == sourceId);
            CombatUnit? target = room.Units.FirstOrDefault(unit => unit.Id == targetId);
            if (sourceId == 0 || targetId == 0) return Match(room, source);
            if (source == null || target == null) return Unsupported("Missing retained Horde merge actor.");
            int stacks = source.Status("horde")?.Stacks ?? 0;
            if (stacks == 0 || target.Status("horde") == null) return Match(room, source);
            if (source.DeathState?.IsDespawned == null || source.DeathState.IsDestroyed == null)
                return Unsupported("Horde merge requires captured source despawn and destruction state.");
            string? error = RoomCombatModel.Validate(room);
            if (error != null) return Unsupported(error);

            RoomCombatResult added = StatusApplicationModel.ApplyRetained(room, targetId,
                target.RegisteredStatus("horde")!.WithStacks(stacks), 0, suppressHordeSpawnCallbacks: fromBump);
            if (!added.Supported) return new HordeMergeResult(added, null);
            RoomCombatState state = added.State!;
            // Self-merges add to the same object before removing it. Immunity still allows removal.
            CombatUnit removed = state.Units.Single(unit => unit.Id == sourceId);
            removed = removed.WithDeathState(removed.DeathState!.WithLifecycle(true, state.Preview ? removed.DeathState.IsDestroyed : true));
            CombatContext? context = state.Context;
            if (context?.SpawnPoints == null) return Unsupported("Horde merge requires retained physical points.");
            bool ownedPoint = context.SpawnPoints.Units.SingleOrDefault(unit => unit.UnitId == sourceId)?.Current != null;
            if (!state.Preview)
            {
                var physical = BattleSpawnPointModel.Apply(context.SpawnPoints, state, "Remove", removed.Team, sourceId);
                if (!physical.Supported) return Unsupported(physical.Error!);
                context = context.WithSpawnPoints(physical.State!);
            }
            CombatUnit[] survivors = state.Units.Where(unit => unit.Id != sourceId).ToArray();
            state = new RoomCombatState(state.RoomIndex, state.Deployment, survivors, state.ExternalInteractions, context, state.Preview);
            if (ownedPoint && !state.Preview)
            {
                foreach (CombatTeam team in new[] { CombatTeam.Enemy, CombatTeam.Player })
                {
                    var centered = BattleSpawnPointModel.Apply(context.SpawnPoints!, state, "Compact", team);
                    if (!centered.Supported) return Unsupported(centered.Error!);
                    context = context.WithSpawnPoints(centered.State!);
                }
            }
            if (!state.Preview && removed.Team == CombatTeam.Player) context = EquipmentModel.ReturnAttached(context, removed);
            foreach (CardInstanceState card in context.CardRegistry ?? context.CardInstances ?? Array.Empty<CardInstanceState>())
                if (card.PlayedRoomUnitIds?.Contains(sourceId) == true)
                    context = context.WithCard(card.WithRoomCacheState(card.PlayedRoomUnitIds.Where(id => id != sourceId).ToArray(), card.RawPlayedRoomUnitIds));
            foreach (RoomCombatModel.QueuedCharacterTrigger callback in added.PendingCallbacks)
                if (callback.Unit.Id == sourceId) callback.Unit = removed;
            return new HordeMergeResult(new RoomCombatResult(new RoomCombatState(state.RoomIndex, state.Deployment,
                BattleSpawnPointModel.Order(context.SpawnPoints, state.RoomIndex, survivors), state.ExternalInteractions, context, state.Preview),
                RoomOutcome.Exchanged, 0, new List<CombatEvent>(), pendingCallbacks: added.PendingCallbacks, retainedUnits: new[] { removed }), removed);
        }
        private static HordeMergeResult Match(RoomCombatState state, CombatUnit? source) => new HordeMergeResult(
            new RoomCombatResult(state, RoomOutcome.Exchanged, 0, new List<CombatEvent>()), source);
        private static HordeMergeResult Unsupported(string error) => new HordeMergeResult(
            new RoomCombatResult(null, RoomOutcome.Unsupported, 0, new List<CombatEvent>(), error), null);
    }
}

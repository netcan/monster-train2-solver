using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class UnitBumpRules
    {
        public bool Loops { get; }
        public bool CompanionBoss { get; }
        public bool OuterBoss { get; }
        public UnitBumpRules(bool loops, bool companionBoss, bool outerBoss)
        { Loops = loops; CompanionBoss = companionBoss; OuterBoss = outerBoss; }
    }

    public sealed class BumpResult
    {
        public TrainCombatState? State { get; }
        public RoomOutcome Outcome { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => State != null;
        public IReadOnlyList<CombatEvent> Events { get; }
        public IReadOnlyList<CombatUnit> RetainedUnits { get; }
        internal IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger> PendingCallbacks { get; }
        internal IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger> Dispatched { get; }
        internal BumpResult(TrainCombatState? state, RoomOutcome outcome, string? error,
            IReadOnlyList<CombatEvent> events, IReadOnlyList<CombatUnit> retained,
            IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger> pending,
            IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger> dispatched)
        { State = state; Outcome = outcome; UnsupportedReason = error; Events = Array.AsReadOnly(events.ToArray());
            RetainedUnits = Array.AsReadOnly(retained.ToArray()); PendingCallbacks = Array.AsReadOnly(pending.ToArray());
            Dispatched = Array.AsReadOnly(dispatched.ToArray()); }
    }

    public static class BumpModel
    {
        public static BumpResult Apply(TrainCombatState source, IReadOnlyList<int> targets, int amount,
            IReadOnlyList<RoomPlayRule> roomRules, int sourceCardId = 0) => Apply(source, targets, amount, roomRules, sourceCardId, null);

        internal static BumpResult Apply(TrainCombatState source, IReadOnlyList<int> targets, int amount,
            IReadOnlyList<RoomPlayRule> roomRules, int sourceCardId,
            IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger>? pending)
        {
            string? error = TrainCombatModel.Validate(source);
            var events = new List<CombatEvent>(); var retained = new Dictionary<int, CombatUnit>();
            var dispatched = new List<RoomCombatModel.QueuedCharacterTrigger>();
            var queue = (pending ?? Array.Empty<RoomCombatModel.QueuedCharacterTrigger>()).ToList();
            TrainCombatState state = source; RoomOutcome outcome = RoomOutcome.Exchanged;
            if (error != null) return Fail(error);
            if (source.Context?.SpawnPoints == null || source.Rooms.Any(room => room.Preview) ||
                source.Rooms.Any(room => roomRules.Count(rule => rule.RoomIndex == room.RoomIndex) != 1) ||
                roomRules.Count(rule => rule.IsPyre) != 1 || !roomRules.Last().IsPyre)
                return Fail("Bump requires captured physical points, all room rules and a primary state.");
            int pyre = source.Rooms.Count - 1;
            var moved = new List<(int Id, int OldRoom, int NewRoom)>(); var attempted = new List<int>();
            // Native snapshots the target list, moves all actors first, then starts callbacks.
            foreach (int id in targets)
            {
                RoomCombatState? origin = FindRoom(id); CombatUnit? actor = origin?.Units.FirstOrDefault(unit => unit.Id == id);
                SpawnPointReference? old = state.Context!.SpawnPoints!.Units.SingleOrDefault(unit => unit.UnitId == id)?.Current;
                if (actor == null || old == null) continue;
                if (actor.BumpRules == null) return Fail("Missing native Bump movement properties for actor " + id);
                if (actor.BumpRules.OuterBoss || actor.BumpRules.CompanionBoss || actor.Status("relentless") != null)
                    return Fail("Boss Bump movement and room destruction require their dedicated transition model.");
                if (actor.Status("immobile") != null) { attempted.Add(id); continue; }
                if (actor.Status("rooted") != null)
                {
                    RoomCombatResult removed = StatusRemovalModel.Remove(origin!, id, "rooted", 1, sourceCardId);
                    if (!Accept(removed)) return Fail(error!);
                    attempted.Add(id); continue;
                }
                if (actor.IsPyre) continue;
                int steps = Math.Max(-pyre, Math.Min(pyre, amount)), direction = steps > 0 ? 1 : -1;
                bool looping = actor.BumpRules.Loops && direction > 0 && old.RoomIndex + steps >= pyre;
                if (looping) { direction = -1; steps = old.RoomIndex; }
                SpawnPointReference? destination = null; bool blocked = false;
                for (int step = 1; step <= Math.Abs(steps); step++)
                {
                    int floor = Math.Max(0, Math.Min(pyre, old.RoomIndex + step * direction));
                    RoomPlayRule rule = roomRules.Single(item => item.RoomIndex == floor);
                    if (!rule.Enabled || actor.Team == CombatTeam.Player && rule.IsPyre) { blocked = true; break; }
                    int recipient = actor.Team == CombatTeam.Player ? HordeMergeModel.Select(state.Rooms[floor], actor) : 0;
                    int point = recipient == 0 ? state.Context!.SpawnPoints!.FirstEmpty(floor, actor.Team) :
                        state.Context!.SpawnPoints!.Units.Single(unit => unit.UnitId == recipient).Current?.Index ?? -1;
                    if (point < 0) { blocked = true; break; }
                    destination = new SpawnPointReference(floor, actor.Team, point);
                }
                if (destination == null || destination.RoomIndex == old.RoomIndex) { attempted.Add(id); continue; }
                if (looping && !blocked) queue.Add(new RoomCombatModel.QueuedCharacterTrigger(origin!.RoomIndex, actor, "OnTrainRoomLoop"));
                if (HordeMergeModel.Select(state.Rooms[destination.RoomIndex], actor) == 0)
                {
                    var placed = BattleSpawnPointModel.Apply(state.Context!.SpawnPoints!, origin!, "Set", actor.Team, id, target: destination);
                    if (!placed.Supported) return Fail(placed.Error!);
                    CombatContext context = state.Context.WithSpawnPoints(placed.State!);
                    var rooms = state.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                        room.Units.Where(unit => unit.Id != id).Concat(room.RoomIndex == destination.RoomIndex
                            ? new[] { actor } : Array.Empty<CombatUnit>()).ToArray(), room.ExternalInteractions, context)).ToArray();
                    state = CardSpellModel.WithContext(new TrainCombatState(rooms, state.Movement, state.EnemySlotsPerRoom, context), context);
                }
                moved.Add((id, old.RoomIndex, destination.RoomIndex));
            }
            // Position updates happen after the entire first pass; compaction can affect both
            // the departure and destination of another target in the same card effect.
            foreach (var move in moved)
            {
                CombatUnit actor = Get(move.Id)!;
                if (HordeMergeModel.Select(state.Rooms[move.NewRoom], actor) == 0)
                    foreach (int floor in new[] { move.OldRoom, move.NewRoom })
                    {
                        var centered = BattleSpawnPointModel.Apply(state.Context!.SpawnPoints!, state.Rooms[floor], "Compact", actor.Team);
                        if (!centered.Supported) return Fail(centered.Error!);
                        state = CardSpellModel.WithContext(state, state.Context.WithSpawnPoints(centered.State!));
                        Order();
                    }
            }
            foreach (var move in moved)
            {
                CombatUnit actor = Get(move.Id)!;
                int recipient = HordeMergeModel.Select(state.Rooms[move.NewRoom], actor);
                if (recipient != 0)
                {
                    TrainHordeMergeResult merge = HordeMergeModel.MergeAcrossRooms(state, actor.Id, recipient, fromBump: true);
                    if (!merge.Supported) return Fail(merge.UnsupportedReason!);
                    state = merge.State!; retained[actor.Id] = actor = merge.SourceAfter!; queue.AddRange(merge.PendingCallbacks);
                }
                if (actor.Team == CombatTeam.Enemy)
                {
                    queue.Add(new RoomCombatModel.QueuedCharacterTrigger(FindRoom(actor.Id)?.RoomIndex ?? move.OldRoom,
                        actor, amount > 0 ? "PostAscension" : "PostDescension"));
                    if (!Drain()) return Fail(error!);
                }
                foreach (int floor in new[] { move.OldRoom, move.NewRoom })
                {
                    int[] members = state.Rooms[floor].Units.Where(unit => unit.Team == actor.Team).Select(unit => unit.Id).ToArray();
                    foreach (int id in members)
                    {
                        CombatUnit shifted = Get(id) ?? retained[id];
                        queue.Add(new RoomCombatModel.QueuedCharacterTrigger(floor, shifted, "OnShift"));
                        if (!Drain()) return Fail(error!);
                        shifted = Get(id) ?? retained.GetValueOrDefault(id) ?? shifted;
                        foreach (CombatUnit guard in state.Rooms[floor].Units.Where(unit => unit.Team != shifted.Team))
                            queue.Add(new RoomCombatModel.QueuedCharacterTrigger(floor, guard, "OnSentry", overrideTarget: shifted, paramString: ""));
                        if (!Drain()) return Fail(error!);
                    }
                }
            }
            foreach (int id in attempted)
            {
                CombatUnit? actor = Get(id);
                if (actor?.Team != CombatTeam.Enemy) continue;
                queue.Add(new RoomCombatModel.QueuedCharacterTrigger(FindRoom(id)!.RoomIndex, actor,
                    amount > 0 ? "PostAttemptedAscension" : "PostAttemptedDescension"));
                if (!Drain()) return Fail(error!);
            }
            // Native room-order handling always runs the remaining queue, including rooted
            // player status notifications when nothing moved.
            if (!Drain()) return Fail(error!);
            return new BumpResult(state, outcome, null, events, retained.Values.ToArray(), queue, dispatched);

            CombatUnit? Get(int id) => state.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == id);
            RoomCombatState? FindRoom(int id) => state.Rooms.FirstOrDefault(room => room.Units.Any(unit => unit.Id == id));
            BumpResult Fail(string reason) => new BumpResult(null, RoomOutcome.Unsupported, reason, events,
                retained.Values.ToArray(), queue, dispatched);
            void Order()
            {
                state = new TrainCombatState(state.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                    BattleSpawnPointModel.Order(state.Context!.SpawnPoints, room.RoomIndex, room.Units), room.ExternalInteractions,
                    state.Context)).ToArray(), state.Movement, state.EnemySlotsPerRoom, state.Context);
            }
            bool Accept(RoomCombatResult result)
            {
                if (!result.Supported) { error = result.UnsupportedReason; return false; }
                var rooms = state.Rooms.Select(room => room.RoomIndex == result.State!.RoomIndex ? result.State : room).ToArray();
                var alive = rooms.SelectMany(room => room.Units).Select(unit => unit.Id).ToHashSet();
                state = CardSpellModel.WithContext(new TrainCombatState(rooms, state.Movement.Where(rule => alive.Contains(rule.UnitId)).ToArray(),
                    state.EnemySlotsPerRoom, result.State!.Context), result.State.Context!);
                events.AddRange(result.Events); queue.AddRange(result.PendingCallbacks);
                foreach (CombatUnit unit in result.RetainedUnits) retained[unit.Id] = unit;
                if (result.Outcome == RoomOutcome.BattleWon || result.Outcome == RoomOutcome.PlayerDefeated) outcome = result.Outcome;
                return true;
            }
            bool Drain()
            {
                for (int index = 0; index < queue.Count; index++)
                {
                    RoomCombatState? current = FindRoom(queue[index].Unit.Id);
                    if (current != null && current.RoomIndex != queue[index].RoomIndex)
                        queue[index] = queue[index].InRoom(current.RoomIndex);
                }
                bool okay = RoomCombatModel.DrainCharacterQueue(queue, callback =>
                {
                    dispatched.Add(callback);
                    RoomCombatResult fired = RoomCombatModel.ApplyQueuedCharacterTrigger(state.Rooms[callback.RoomIndex], callback, queue.Add);
                    return Accept(fired);
                }, callback => Accept(RoomCombatModel.SettleQueuedSpawnerAndCenter(state.Rooms[callback.RoomIndex], callback.Unit)),
                () =>
                {
                    TrainCombatResult updated = EnchantmentWorldModel.UpdateAll(state, queue.Add);
                    if (!updated.Supported) { error = updated.UnsupportedReason; return false; }
                    state = updated.State!; return true;
                });
                if (okay) queue.Clear();
                return okay;
            }
        }
    }
}

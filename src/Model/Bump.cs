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
        public bool OuterBossRelentlessPhase { get; }
        public int PairedBossUnitId { get; }
        public UnitBumpRules(bool loops, bool companionBoss, bool outerBoss, bool outerBossRelentlessPhase = false,
            int pairedBossUnitId = 0)
        { Loops = loops; CompanionBoss = companionBoss; OuterBoss = outerBoss;
            OuterBossRelentlessPhase = outerBossRelentlessPhase; PairedBossUnitId = pairedBossUnitId; }
    }

    public sealed class BumpResult
    {
        public TrainCombatState? State { get; }
        public RoomOutcome Outcome { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => State != null;
        public IReadOnlyList<CombatEvent> Events { get; }
        public IReadOnlyList<CombatUnit> RetainedUnits { get; }
        public IReadOnlyList<RoomPlayRule> RoomRules { get; }
        internal IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger> PendingCallbacks { get; }
        internal IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger> Dispatched { get; }
        internal BumpResult(TrainCombatState? state, RoomOutcome outcome, string? error,
            IReadOnlyList<CombatEvent> events, IReadOnlyList<CombatUnit> retained,
            IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger> pending,
            IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger> dispatched,
            IReadOnlyList<RoomPlayRule>? roomRules = null)
        { State = state; Outcome = outcome; UnsupportedReason = error; Events = Array.AsReadOnly(events.ToArray());
            RetainedUnits = Array.AsReadOnly(retained.ToArray()); PendingCallbacks = Array.AsReadOnly(pending.ToArray());
            Dispatched = Array.AsReadOnly(dispatched.ToArray()); RoomRules = Array.AsReadOnly((roomRules ?? Array.Empty<RoomPlayRule>()).ToArray()); }
    }

    public static class BumpModel
    {
        public static BumpResult Apply(TrainCombatState source, IReadOnlyList<int> targets, int amount,
            IReadOnlyList<RoomPlayRule> roomRules, int sourceCardId = 0) => Apply(source, targets, amount, roomRules, sourceCardId, null);

        internal static BumpResult Apply(TrainCombatState source, IReadOnlyList<int> targets, int amount,
            IReadOnlyList<RoomPlayRule> roomRules, int sourceCardId,
            IReadOnlyList<RoomCombatModel.QueuedCharacterTrigger>? pending, SpawnPointReference? teleportGoal = null)
        {
            string? error = TrainCombatModel.Validate(source);
            var events = new List<CombatEvent>(); var retained = new Dictionary<int, CombatUnit>();
            var dispatched = new List<RoomCombatModel.QueuedCharacterTrigger>();
            var queue = (pending ?? Array.Empty<RoomCombatModel.QueuedCharacterTrigger>()).ToList();
            TrainCombatState state = source; RoomOutcome outcome = RoomOutcome.Exchanged;
            RoomPlayRule[] updatedRoomRules = roomRules.ToArray();
            if (error != null) return Fail(error);
            if (source.Context?.SpawnPoints == null || source.Rooms.Any(room => room.Preview) ||
                source.Rooms.Any(room => updatedRoomRules.Count(rule => rule.RoomIndex == room.RoomIndex) != 1) ||
                updatedRoomRules.Count(rule => rule.IsPyre) != 1 || !updatedRoomRules.Last().IsPyre)
                return Fail("Bump requires captured physical points, all room rules and a primary state.");
            int pyre = source.Rooms.Count - 1;
            var moved = new List<(int Id, int OldRoom, int NewRoom, bool ForcedBossMove)>(); var attempted = new List<int>();
            // Native snapshots the target list, moves all actors first, then starts callbacks.
            foreach (int id in targets)
            {
                RoomCombatState? origin = FindRoom(id); CombatUnit? actor = origin?.Units.FirstOrDefault(unit => unit.Id == id);
                SpawnPointReference? old = state.Context!.SpawnPoints!.Units.SingleOrDefault(unit => unit.UnitId == id)?.Current;
                if (actor == null || old == null) continue;
                if (actor.BumpRules == null) return Fail("Missing native Bump movement properties for actor " + id);
                if (actor.Status("immobile") != null) { attempted.Add(id); continue; }
                if (actor.Status("rooted") != null)
                {
                    RoomCombatResult removed = StatusRemovalModel.Remove(origin!, id, "rooted", 1, sourceCardId);
                    if (!Accept(removed)) return Fail(error!);
                    attempted.Add(id); continue;
                }
                if (actor.IsPyre) continue;
                if (actor.BumpRules.OuterBoss)
                {
                    if (actor.Team != CombatTeam.Enemy || !old.OuterBossAnchor)
                        return Fail("Outer Boss movement requires its captured Boss anchor.");
                    var outerDestination = FindForcedBossDestination(actor, old);
                    if (outerDestination.Point == null || outerDestination.Point.RoomIndex == old.RoomIndex)
                    {
                        attempted.Add(id); continue;
                    }

                    if (outerDestination.Looping && !outerDestination.Blocked)
                        queue.Add(new RoomCombatModel.QueuedCharacterTrigger(origin!.RoomIndex, actor, "OnTrainRoomLoop"));

                    int pairedBossId = FindPairedBossId(actor);
                    if (actor.Status("relentless") != null && pairedBossId != 0)
                    {
                        int pairedId = pairedBossId;
                        CombatUnit? paired = Get(pairedId);
                        SpawnPointReference? pairedPoint = state.Context!.SpawnPoints!.Units
                            .SingleOrDefault(unit => unit.UnitId == pairedId)?.Current;
                        if (paired == null || pairedPoint == null || paired.BumpRules?.CompanionBoss != true)
                            return Fail("Outer Boss pairing references a missing captured companion Boss.");

                        // Native FindBumpSpawnPoint refuses a paired relentless companion;
                        // otherwise ForceMoveBoss moves it before moving the Outer Boss.
                        bool pairedIsRelentlessAndPaired = paired.Status("relentless") != null &&
                            FindPairedBossId(paired) != 0;
                        if (!pairedIsRelentlessAndPaired)
                        {
                            var pairedDestination = FindForcedBossDestination(paired, pairedPoint);
                            if (pairedDestination.Point != null && pairedDestination.Point.RoomIndex != pairedPoint.RoomIndex)
                            {
                                if (pairedDestination.Looping && !pairedDestination.Blocked)
                                    queue.Add(new RoomCombatModel.QueuedCharacterTrigger(FindRoom(pairedId)!.RoomIndex,
                                        paired, "OnTrainRoomLoop"));
                                if (!ForceMoveBoss(pairedId, pairedDestination.Point)) return Fail(error!);
                            }
                        }

                        actor = Get(id) ?? retained.GetValueOrDefault(id) ?? actor;
                        old = state.Context!.SpawnPoints!.Units.SingleOrDefault(unit => unit.UnitId == id)?.Current;
                        if (old == null) return Fail("Outer Boss lost its anchor during paired movement.");
                        outerDestination = FindForcedBossDestination(actor, old);
                        if (outerDestination.Point == null || outerDestination.Point.RoomIndex == old.RoomIndex)
                            continue;
                        if (outerDestination.Looping && !outerDestination.Blocked)
                            queue.Add(new RoomCombatModel.QueuedCharacterTrigger(FindRoom(id)!.RoomIndex,
                                actor, "OnTrainRoomLoop"));
                    }

                    if (!ForceMoveBoss(id, outerDestination.Point)) return Fail(error!);
                    continue;
                }
                if (actor.BumpRules.CompanionBoss && actor.Status("relentless") != null && FindPairedBossId(actor) != 0)
                { attempted.Add(id); continue; }
                int steps = Math.Max(-pyre, Math.Min(pyre, amount)), direction = steps > 0 ? 1 : -1;
                bool looping = actor.BumpRules.Loops && actor.Status("relentless") == null && direction > 0 && old.RoomIndex + steps >= pyre;
                if (looping) { direction = -1; steps = old.RoomIndex; }
                SpawnPointReference? destination = null; bool blocked = false;
                for (int step = 1; step <= Math.Abs(steps); step++)
                {
                    int floor = Math.Max(0, Math.Min(pyre, old.RoomIndex + step * direction));
                    if (teleportGoal != null && floor != teleportGoal.RoomIndex) continue;
                    RoomPlayRule rule = updatedRoomRules.Single(item => item.RoomIndex == floor);
                    if (!rule.Enabled || actor.Team == CombatTeam.Player && rule.IsPyre) { blocked = true; break; }
                    int recipient = actor.Team == CombatTeam.Player ? HordeMergeModel.Select(state.Rooms[floor], actor) : 0;
                    int point = recipient == 0 ? state.Context!.SpawnPoints!.FirstEmpty(floor, actor.Team) :
                        state.Context!.SpawnPoints!.Units.Single(unit => unit.UnitId == recipient).Current?.Index ?? -1;
                    if (point < 0) { blocked = true; break; }
                    destination = new SpawnPointReference(floor, actor.Team, point);
                }
                if (destination == null || destination.RoomIndex == old.RoomIndex ||
                    teleportGoal != null && destination.RoomIndex != teleportGoal.RoomIndex) { attempted.Add(id); continue; }
                if (looping && !blocked) queue.Add(new RoomCombatModel.QueuedCharacterTrigger(origin!.RoomIndex, actor, "OnTrainRoomLoop"));
                if (HordeMergeModel.Select(state.Rooms[destination.RoomIndex], actor) == 0)
                {
                    var placed = BattleSpawnPointModel.Apply(state.Context!.SpawnPoints!, origin!, "Set", actor.Team, id, target: destination);
                    if (!placed.Supported) return Fail(placed.Error!);
                    CombatContext context = state.Context.WithSpawnPoints(placed.State!);
                    var rooms = state.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                        room.Units.Where(unit => unit.Id != id).Concat(room.RoomIndex == destination.RoomIndex
                            ? new[] { actor } : Array.Empty<CombatUnit>()).ToArray(), room.ExternalInteractions, context,
                        room.Preview, room.ActiveCompanionBossIds)).ToArray();
                    state = CardSpellModel.WithContext(new TrainCombatState(rooms, state.Movement, state.EnemySlotsPerRoom, context), context);
                }
                moved.Add((id, old.RoomIndex, destination.RoomIndex, false));
            }
            // Position updates happen after the entire first pass; compaction can affect both
            // the departure and destination of another target in the same card effect.
            foreach (var move in moved)
            {
                CombatUnit actor = Get(move.Id) ?? retained.GetValueOrDefault(move.Id)!;
                if (actor.Status("relentless") != null && state.Context?.QueryFrame is StatisticQueryFrame queryFrame)
                    state = CardSpellModel.WithContext(state, state.Context.WithQueryFrame(queryFrame.With(selectedRoomIndex: move.NewRoom)));
                if (move.ForcedBossMove) continue;
                if (HordeMergeModel.Select(state.Rooms[move.NewRoom], actor) == 0)
                    foreach (int floor in new[] { move.OldRoom, move.NewRoom })
                    {
                        var centered = BattleSpawnPointModel.Apply(state.Context!.SpawnPoints!, state.Rooms[floor], "Compact", actor.Team);
                        if (!centered.Supported) return Fail(centered.Error!);
                        state = CardSpellModel.WithContext(state, state.Context.WithSpawnPoints(centered.State!));
                        Order();
                    }
            }
            if (teleportGoal != null)
            {
                foreach (var move in moved.Where(move => targets.Count == 1 && move.Id == targets[0] &&
                    move.NewRoom == teleportGoal.RoomIndex))
                {
                    SpawnPointReference? current = state.Context!.SpawnPoints!.Units.Single(unit => unit.UnitId == move.Id).Current;
                    if (current == null) return Fail("Teleported actor lost its destination spawn point.");
                    if (current.Index == teleportGoal.Index) continue;
                    RoomCombatState room = state.Rooms[move.NewRoom];
                    var rearranged = BattleSpawnPointModel.Apply(state.Context.SpawnPoints!, room, "Rearrange",
                        current.Team, move.Id, index: current.Index, targetIndex: teleportGoal.Index);
                    if (!rearranged.Supported) return Fail(rearranged.Error!);
                    state = CardSpellModel.WithContext(state, state.Context.WithSpawnPoints(rearranged.State!));
                    Order();
                }
            }
            foreach (var move in moved)
            {
                if (move.ForcedBossMove) continue;
                CombatUnit actor = Get(move.Id)!;
                if (actor.Status("relentless") != null && move.NewRoom > move.OldRoom)
                    for (int floor = move.OldRoom; floor < move.NewRoom; floor++)
                        if (!DestroyRoom(floor)) return Fail(error!);
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
                    int[] members = state.Rooms[floor].Units.Where(unit => unit.Team == actor.Team && unit.BumpRules?.OuterBoss != true)
                        .Select(unit => unit.Id).ToArray();
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
            return new BumpResult(state, outcome, null, events, retained.Values.ToArray(), queue, dispatched, updatedRoomRules);

            CombatUnit? Get(int id) => state.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == id);
            RoomCombatState? FindRoom(int id) => state.Rooms.FirstOrDefault(room => room.Units.Any(unit => unit.Id == id));
            BumpResult Fail(string reason) => new BumpResult(null, RoomOutcome.Unsupported, reason, events,
                retained.Values.ToArray(), queue, dispatched);
            void Order()
            {
                state = new TrainCombatState(state.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                    BattleSpawnPointModel.Order(state.Context!.SpawnPoints, room.RoomIndex, room.Units), room.ExternalInteractions,
                    state.Context, room.Preview, room.ActiveCompanionBossIds)).ToArray(),
                    state.Movement, state.EnemySlotsPerRoom, state.Context);
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
                    if (!callback.IsDeathRoomFocus && !callback.RelicDamageApplied) dispatched.Add(callback);
                    RoomCombatResult fired = RoomCombatModel.ApplyQueuedCharacterTrigger(state.Rooms[callback.RoomIndex], callback, queue.Add);
                    return Accept(fired);
                }, callback => Accept(RoomCombatModel.SettleQueuedSpawnerAndCenter(state.Rooms[callback.RoomIndex], callback.Unit)),
                () =>
                {
                    TrainCombatResult updated = EnchantmentWorldModel.UpdateAll(state, queue.Add);
                    if (!updated.Supported) { error = updated.UnsupportedReason; return false; }
                    state = updated.State!; return true;
                }, () => state.Context, message => error = message);
                if (okay) queue.Clear();
                return okay;
            }
            bool DestroyRoom(int floor)
            {
                updatedRoomRules = updatedRoomRules.Select(rule => rule.RoomIndex == floor ? rule.WithEnabled(false) : rule).ToArray();
                foreach (CombatUnit listed in state.Rooms[floor].Units.ToArray())
                {
                    if (listed.BumpRules?.OuterBoss == true) continue;
                    CombatUnit? actor = Get(listed.Id) ?? retained.GetValueOrDefault(listed.Id);
                    if (actor == null || actor.Health <= 0 || actor.DeathState?.IsDestroyed == true) continue;
                    if (actor.Status("undying") is CombatStatus undying)
                    {
                        RoomCombatResult removed = StatusRemovalModel.Remove(state.Rooms[floor], actor.Id, "undying", undying.Stacks);
                        if (!Accept(removed) || !Drain()) return false;
                        actor = Get(listed.Id) ?? retained.GetValueOrDefault(listed.Id);
                        if (actor == null) continue;
                    }
                    RoomCombatResult sacrificed = RoomCombatModel.QueueSacrifice(state.Rooms[floor], actor, 0, queue.Add);
                    if (!Accept(sacrificed) || !Drain()) return false;
                }
                return true;
            }
            int FindPairedBossId(CombatUnit boss)
            {
                int capturedId = boss.BumpRules?.PairedBossUnitId ?? 0;
                if (capturedId != 0 && Get(capturedId) is CombatUnit captured && captured.Health > 0 &&
                    captured.DeathState?.IsDestroyed != true) return capturedId;
                return state.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id != boss.Id &&
                    unit.BumpRules?.CompanionBoss == true && unit.Health > 0 && unit.DeathState?.IsDestroyed != true)?.Id ?? 0;
            }
            (SpawnPointReference? Point, bool Looping, bool Blocked) FindForcedBossDestination(
                CombatUnit boss, SpawnPointReference current)
            {
                UnitBumpRules rules = boss.BumpRules!;
                int steps = Math.Max(-pyre, Math.Min(pyre, amount));
                int direction = steps > 0 ? 1 : -1;
                bool looping = rules.Loops && boss.Status("relentless") == null && direction > 0 &&
                    current.RoomIndex + steps >= pyre;
                if (looping) { direction = -1; steps = current.RoomIndex; }
                SpawnPointReference? destination = null;
                bool blocked = false;
                for (int step = 1; step <= Math.Abs(steps); step++)
                {
                    int floor = Math.Max(0, Math.Min(pyre, current.RoomIndex + step * direction));
                    RoomPlayRule rule = updatedRoomRules.Single(item => item.RoomIndex == floor);
                    if (!rule.Enabled || rules.OuterBoss && rule.IsPyre && !rules.OuterBossRelentlessPhase)
                    { blocked = true; break; }
                    if (rules.OuterBoss)
                    {
                        destination = new SpawnPointReference(floor, CombatTeam.Enemy, 0, outerBossAnchor: true);
                        continue;
                    }
                    int point = state.Context!.SpawnPoints!.FirstEmpty(floor, boss.Team);
                    if (point < 0) { blocked = true; break; }
                    destination = new SpawnPointReference(floor, boss.Team, point);
                }
                return (destination, looping, blocked);
            }
            bool ForceMoveBoss(int id, SpawnPointReference destination)
            {
                CombatUnit? boss = Get(id) ?? retained.GetValueOrDefault(id);
                RoomCombatState? origin = FindRoom(id);
                SpawnPointReference? oldPoint = state.Context!.SpawnPoints!.Units
                    .SingleOrDefault(unit => unit.UnitId == id)?.Current;
                if (boss == null || origin == null || oldPoint == null)
                { error = "A forced paired Boss move lost its actor or physical position."; return false; }
                var placed = BattleSpawnPointModel.Apply(state.Context!.SpawnPoints!, origin, "Set", boss.Team,
                    id, target: destination);
                if (!placed.Supported) { error = placed.Error; return false; }
                CombatContext context = state.Context.WithSpawnPoints(placed.State!);
                RoomCombatState[] rooms = state.Rooms.Select(room =>
                {
                    var units = room.Units.Where(unit => unit.Team == CombatTeam.Enemy && unit.Id != id).ToList();
                    if (room.RoomIndex == destination.RoomIndex && boss.Team == CombatTeam.Enemy) units.Add(boss);
                    units.AddRange(room.Units.Where(unit => unit.Team == CombatTeam.Player && unit.Id != id));
                    if (room.RoomIndex == destination.RoomIndex && boss.Team == CombatTeam.Player) units.Add(boss);
                    return new RoomCombatState(room.RoomIndex, room.Deployment, units, room.ExternalInteractions,
                        context, room.Preview, room.ActiveCompanionBossIds);
                }).ToArray();
                state = CardSpellModel.WithContext(new TrainCombatState(rooms, state.Movement,
                    state.EnemySlotsPerRoom, context), context);
                Order();
                moved.Add((id, oldPoint.RoomIndex, destination.RoomIndex, true));

                string movementTrigger = amount > 0 ? "PostAscension" : "PostDescension";
                queue.Add(new RoomCombatModel.QueuedCharacterTrigger(destination.RoomIndex, boss, movementTrigger));
                if (!Drain()) return false;
                boss = Get(id) ?? retained.GetValueOrDefault(id) ?? boss;
                queue.Add(new RoomCombatModel.QueuedCharacterTrigger(destination.RoomIndex, boss, "OnShift"));
                if (!Drain()) return false;
                boss = Get(id) ?? retained.GetValueOrDefault(id) ?? boss;
                foreach (CombatUnit guard in state.Rooms[destination.RoomIndex].Units.Where(unit => unit.Team != boss.Team))
                    queue.Add(new RoomCombatModel.QueuedCharacterTrigger(destination.RoomIndex, guard, "OnSentry",
                        overrideTarget: boss, paramString: ""));
                return Drain();
            }
        }
    }
}

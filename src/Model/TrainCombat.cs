using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class EnemyMovement
    {
        public int UnitId { get; }
        public int Speed { get; }
        public bool Ascends { get; }
        public bool Loops { get; }
        public bool CompanionBoss { get; }
        // Intrinsic speed before movement statuses. The native default is one floor.
        public EnemyMovement(int unitId, int speed, bool ascends, bool loops, bool companionBoss = false)
        { UnitId = unitId; Speed = speed; Ascends = ascends; Loops = loops; CompanionBoss = companionBoss; }
    }

    public sealed class TrainCombatState
    {
        public IReadOnlyList<RoomCombatState> Rooms { get; }
        public IReadOnlyList<EnemyMovement> Movement { get; }
        public int EnemySlotsPerRoom { get; }
        public CombatContext? Context { get; }
        public TrainCombatState(IReadOnlyList<RoomCombatState> rooms, IReadOnlyList<EnemyMovement> movement,
            int enemySlotsPerRoom, CombatContext? context = null)
        {
            Rooms = Array.AsReadOnly(rooms.OrderBy(room => room.RoomIndex).ToArray());
            // Native capture enumerates movement by current floor/front-to-back order,
            // including new lower-floor waves before surviving enemies upstairs.
            var positions = Rooms.SelectMany(room => room.Units.Where(unit => unit.Team == CombatTeam.Enemy))
                .Select((unit, index) => new { unit.Id, Index = index }).GroupBy(unit => unit.Id)
                .ToDictionary(group => group.Key, group => group.First().Index);
            Movement = Array.AsReadOnly(movement.OrderBy(rule => positions.TryGetValue(rule.UnitId, out int position) ? position : int.MaxValue).ToArray());
            EnemySlotsPerRoom = enemySlotsPerRoom;
            Context = context;
        }
    }

    public sealed class TrainCombatResult
    {
        public TrainCombatState? State { get; }
        public RoomOutcome Outcome { get; }
        public IReadOnlyList<RoomCombatResult> RoomResults { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => State != null;
        internal TrainCombatResult(TrainCombatState? state, RoomOutcome outcome,
            IReadOnlyList<RoomCombatResult> roomResults, string? reason = null)
        { State = state; Outcome = outcome; RoomResults = Array.AsReadOnly(roomResults.ToArray()); UnsupportedReason = reason; }
    }

    public static class TrainCombatModel
    {
        // Native ProcessRemovals marks objects Destroyed before advancing the turn;
        // Unity completes destruction at frame end. Stable model boundaries normalize
        // those references. Terminal combat skips this phase.
        internal static TrainCombatState ProcessRemovals(TrainCombatState source)
        {
            var activeIds = new HashSet<int>(source.Rooms.SelectMany(room => room.Units).Select(unit => unit.Id));
            return ClearRemovedReferences(source, activeIds);
        }

        private static TrainCombatState ClearRemovedReferences(TrainCombatState source, HashSet<int> activeIds)
        {
            CombatContext? context = source.Context;
            foreach (CardInstanceState card in context?.CardRegistry ?? context?.CardInstances ?? Array.Empty<CardInstanceState>())
                if (card.EquippedUnitId > 0 && !activeIds.Contains(card.EquippedUnitId.Value)) context = context!.WithCard(card.WithEquippedUnit(0));
            return new TrainCombatState(source.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                room.Units.Select(unit => unit.WithoutRemovedAttacker(activeIds)).ToArray(), room.ExternalInteractions, context, room.Preview)).ToArray(),
                source.Movement, source.EnemySlotsPerRoom, context);
        }

        public static TrainCombatResult EndTurnPreHandDiscard(TrainCombatState source, CombatTeam team)
            => CharacterPhase(source, team, "EndTurnPreHandDiscard");

        public static TrainCombatResult PreCombat(TrainCombatState source, CombatTeam team)
            => CharacterPhase(source, team, "PreCombat");

        private static TrainCombatResult CharacterPhase(TrainCombatState source, CombatTeam team, string kind)
        {
            string? error = Validate(source);
            if (error != null || team != CombatTeam.Player && team != CombatTeam.Enemy)
                return Unsupported(error ?? "Invalid combat team.");
            RoomCombatState[] rooms = source.Rooms.ToArray();
            CombatContext? context = source.Context;
            var results = new List<RoomCombatResult>();
            var queue = new List<RoomCombatModel.QueuedCharacterTrigger>();
            RoomOutcome outcome = RoomOutcome.Exchanged;
            // Unit identities are assigned at creation. Native active character lists append
            // at creation and keep that order when units change floor or physical position.
            int[] actors = rooms.SelectMany(room => room.Units).Where(unit => unit.Team == team)
                .OrderBy(unit => unit.Id).Select(unit => unit.Id).ToArray();
            foreach (int actor in actors)
            {
                int index = Array.FindIndex(rooms, room => room.Units.Any(unit => unit.Id == actor));
                if (index < 0) continue; // An earlier queued trigger may remove a later actor.
                RoomCombatResult result = RoomCombatModel.ApplyCharacterPhase(WithContext(rooms[index], context), actor, kind, queue.Add);
                if (!result.Supported) return Unsupported(result.UnsupportedReason!);
                rooms[index] = result.State!; context = result.State!.Context; results.Add(result);
                if (Terminal(result.Outcome))
                { outcome = result.Outcome; break; }
            }
            // Ordinary callbacks drain first; damage deaths then enter native unit removal.
            string? queueError = null;
            bool drained = RoomCombatModel.DrainCharacterQueue(queue, queued =>
            {
                RoomCombatResult result = RoomCombatModel.ApplyQueuedCharacterTrigger(WithContext(rooms[queued.RoomIndex], context), queued, queue.Add);
                if (!result.Supported) { queueError = result.UnsupportedReason; return false; }
                rooms[queued.RoomIndex] = result.State!; context = result.State!.Context; results.Add(result);
                if (Terminal(result.Outcome)) outcome = result.Outcome;
                return true;
            }, queued =>
            {
                RoomCombatResult result = RoomCombatModel.SettleQueuedSpawner(WithContext(rooms[queued.RoomIndex], context), queued.Unit);
                if (!result.Supported) { queueError = result.UnsupportedReason; return false; }
                rooms[queued.RoomIndex] = result.State!; context = result.State!.Context; results.Add(result);
                return true;
            });
            if (!drained) return Unsupported(queueError ?? "Character removal queue failed.");
            return new TrainCombatResult(Freeze(source, rooms, context), outcome, results);
        }

        public static TrainCombatResult ResolveCombat(TrainCombatState source)
        {
            string? error = Validate(source);
            if (error != null) return Unsupported(error);
            RoomCombatState[] rooms = source.Rooms.ToArray();
            CombatContext? context = source.Context;
            var results = new List<RoomCombatResult>();
            for (int index = rooms.Length - 1; index >= 0; index--)
            {
                RoomCombatResult result = RoomCombatModel.Resolve(WithContext(rooms[index], context));
                if (!result.Supported) return Unsupported(result.UnsupportedReason!);
                rooms[index] = result.State!; results.Add(result);
                context = result.State!.Context;
                if (Terminal(result.Outcome))
                    return new TrainCombatResult(Freeze(source, rooms, context), result.Outcome, results);
            }
            return new TrainCombatResult(Freeze(source, rooms, context), RoomOutcome.Cleared, results);
        }

        public static TrainCombatResult Ascend(TrainCombatState source)
            => Ascend(source, null, false);

        internal static TrainCombatResult Ascend(TrainCombatState source, int? onlyUnitId, bool forceLoop)
        {
            string? error = Validate(source);
            if (error != null) return Unsupported(error);
            var rooms = source.Rooms.Select(room => room.Units.ToList()).ToArray();
            int pyre = rooms.Length - 1;
            bool enteredPyre = false;
            var moved = new HashSet<int>();
            var movements = source.Movement.ToDictionary(rule => rule.UnitId);
            var looped = new List<int>();
            var shifted = new List<(int Destination, int UnitId)>();
            var movementResults = new List<RoomCombatResult>();
            // Reserve destinations from the top down; each enemy moves only once.
            for (int index = pyre; index >= 0; index--)
            {
                foreach (CombatUnit enemy in rooms[index].Where(unit => unit.Team == CombatTeam.Enemy).ToArray())
                {
                    if (onlyUnitId.HasValue && enemy.Id != onlyUnitId) continue;
                    if (!moved.Add(enemy.Id)) continue;
                    if (!movements.TryGetValue(enemy.Id, out EnemyMovement? rule))
                        return Unsupported("Missing enemy movement rule for " + enemy.Id);
                    int speed = rule.Ascends ? rule.Speed : 0;
                    if (enemy.Statuses.Any(status => status.Id == "rooted" || status.Id == "immobile")) speed = 0;
                    else if (speed > 0 && enemy.Statuses.Any(status => status.Id == "haste"))
                        speed = Math.Max(1, pyre - 1 - index);
                    CombatUnit arriving = enemy;
                    if (speed != 1)
                    {
                        CombatStatus[] statuses = enemy.Statuses.Select(status =>
                            (status.Id == "rooted" || status.Id == "haste" || status.Id == "immobile") &&
                            status.RemoveWhenTriggered && (!source.Rooms[index].Deployment || status.RemoveDuringDeployment)
                                ? status.WithStacks(status.Stacks - 1) : status).Where(status => status.Stacks > 0).ToArray();
                        arriving = new CombatUnit(enemy.Id, enemy.AssetKey, enemy.Team, enemy.BaseAttack, enemy.Health,
                            enemy.MaxHealth, enemy.CanAttack, enemy.IsPyre, enemy.EndsBattleOnDeath, statuses, enemy.Triggers, enemy.SpawnerCardId, enemy.Size, enemy.StatusImmunities, enemy.Subtypes, enemy.Modifiers, enemy.IsBoss, enemy.LastAttackerId, enemy.StatusRegistry, enemy.EquipmentCards, enemy.NextTriggerId, enemy.Ability, enemy.StatusDictionary);
                    }
                    int destination = Math.Max(0, Math.Min(pyre, index + speed));
                    if ((destination == pyre || forceLoop) && rule.Loops && !enemy.Statuses.Any(status => status.Id == "relentless"))
                        destination = 0;
                    if (destination == index)
                    {
                        rooms[index][rooms[index].IndexOf(enemy)] = arriving;
                        continue;
                    }
                    while (destination <= pyre && rooms[destination].Count(unit => unit.Team == CombatTeam.Enemy)
                        >= source.EnemySlotsPerRoom) destination++;
                    if (destination > pyre) return Unsupported("No enemy spawn point remains in the train.");
                    rooms[index].Remove(enemy);
                    int playerIndex = rooms[destination].FindIndex(unit => unit.Team == CombatTeam.Player);
                    rooms[destination].Insert(playerIndex < 0 ? rooms[destination].Count : playerIndex, arriving);
                    if (destination < index && rule.Loops && enemy.Status("relentless") == null) looped.Add(enemy.Id);
                    else shifted.Add((destination, enemy.Id));
                    enteredPyre |= destination == pyre;
                }
            }
            RoomCombatState[] next = source.Rooms.Select((room, index) => new RoomCombatState(room.RoomIndex,
                room.Deployment, rooms[index], room.ExternalInteractions, source.Context)).ToArray();
            TrainCombatState moving = Freeze(source, next, source.Context);
            if (looped.Count > 0)
            {
                var callbacks = looped.Select(id => (id, "OnTrainRoomLoop")).Concat(looped.SelectMany(id =>
                    new[] { (id, "PostAscension"), (id, "OnShift") })).ToArray();
                TrainCombatResult fired = ApplyMovementTriggers(moving, callbacks);
                if (!fired.Supported) return fired;
                movementResults.AddRange(fired.RoomResults);
                if (Terminal(fired.Outcome)) return new TrainCombatResult(fired.State, fired.Outcome, movementResults);
                moving = fired.State!;
            }
            foreach (var group in shifted.GroupBy(item => item.Destination).OrderByDescending(group => group.Key))
            {
                TrainCombatResult fired = ApplyMovementTriggers(moving, group.SelectMany(item =>
                    new[] { (item.UnitId, "PostAscension"), (item.UnitId, "OnShift") }).ToArray());
                if (!fired.Supported) return fired;
                movementResults.AddRange(fired.RoomResults);
                if (Terminal(fired.Outcome)) return new TrainCombatResult(fired.State, fired.Outcome, movementResults);
                moving = fired.State!;
            }
            next = moving.Rooms.ToArray();
            // Enemies that reach the Pyre fight immediately during ascension, within this same turn.
            if (enteredPyre)
            {
                RoomCombatResult result = RoomCombatModel.Resolve(next[pyre]);
                if (!result.Supported) return Unsupported(result.UnsupportedReason!);
                next[pyre] = result.State!;
                movementResults.Add(result);
                return new TrainCombatResult(Freeze(source, next, result.State!.Context), result.Outcome, movementResults);
            }
            return new TrainCombatResult(moving, RoomOutcome.Cleared, movementResults);
        }

        internal static TrainCombatResult ApplyMovementTriggers(TrainCombatState source, IReadOnlyList<(int UnitId, string Kind)> triggers)
        {
            var queue = new List<RoomCombatModel.QueuedCharacterTrigger>();
            foreach (var trigger in triggers)
            {
                int index = source.Rooms.ToList().FindIndex(room => room.Units.Any(unit => unit.Id == trigger.UnitId));
                if (index >= 0) queue.Add(new RoomCombatModel.QueuedCharacterTrigger(index,
                    source.Rooms[index].Units.First(unit => unit.Id == trigger.UnitId), trigger.Kind));
                if (index >= 0 && trigger.Kind == "OnShift") QueueSentries(source.Rooms[index], trigger.UnitId, queue);
            }
            return ApplyCharacterQueue(source, queue);
        }

        public static TrainCombatResult Sentry(TrainCombatState source, int movedUnitId)
        {
            string? error = Validate(source);
            if (error != null) return Unsupported(error);
            RoomCombatState? room = source.Rooms.FirstOrDefault(item => item.Units.Any(unit => unit.Id == movedUnitId));
            if (room == null) return Unsupported("Sentry requires the moved character in its destination room.");
            var queue = new List<RoomCombatModel.QueuedCharacterTrigger>();
            QueueSentries(room, movedUnitId, queue);
            return ApplyCharacterQueue(source, queue);
        }

        private static void QueueSentries(RoomCombatState room, int movedUnitId, List<RoomCombatModel.QueuedCharacterTrigger> queue)
        {
            CombatUnit moved = room.Units.Single(unit => unit.Id == movedUnitId);
            foreach (CombatUnit actor in room.Units.Where(unit => unit.Team != moved.Team))
                queue.Add(new RoomCombatModel.QueuedCharacterTrigger(room.RoomIndex, actor, "OnSentry", overrideTarget: moved));
        }

        internal static TrainCombatResult ApplyCharacterQueue(TrainCombatState source, List<RoomCombatModel.QueuedCharacterTrigger> queue)
        {
            RoomCombatState[] rooms = source.Rooms.ToArray(); CombatContext? context = source.Context;
            var results = new List<RoomCombatResult>(); RoomOutcome outcome = RoomOutcome.Exchanged; string? error = null;
            bool drained = RoomCombatModel.DrainCharacterQueue(queue, queued =>
            {
                RoomCombatResult fired = RoomCombatModel.ApplyQueuedCharacterTrigger(WithContext(rooms[queued.RoomIndex], context), queued, queue.Add);
                if (!fired.Supported) { error = fired.UnsupportedReason; return false; }
                rooms[queued.RoomIndex] = fired.State!; context = fired.State!.Context; results.Add(fired);
                if (Terminal(fired.Outcome)) outcome = fired.Outcome;
                return true;
            }, queued =>
            {
                RoomCombatResult returned = RoomCombatModel.SettleQueuedSpawner(WithContext(rooms[queued.RoomIndex], context), queued.Unit);
                if (!returned.Supported) { error = returned.UnsupportedReason; return false; }
                rooms[queued.RoomIndex] = returned.State!; context = returned.State!.Context; results.Add(returned); return true;
            });
            return drained ? new TrainCombatResult(Freeze(source, rooms, context), outcome, results) : Unsupported(error ?? "Movement callback queue failed.");
        }

        private static RoomCombatState WithContext(RoomCombatState room, CombatContext? context) =>
            new RoomCombatState(room.RoomIndex, room.Deployment, room.Units, room.ExternalInteractions, context, room.Preview);

        private static TrainCombatState Freeze(TrainCombatState source, RoomCombatState[] rooms, CombatContext? context)
        {
            var alive = new HashSet<int>(rooms.SelectMany(room => room.Units).Select(unit => unit.Id));
            return new TrainCombatState(rooms.Select(room => WithContext(room, context)).ToArray(),
                source.Movement.Where(rule => alive.Contains(rule.UnitId)).ToArray(), source.EnemySlotsPerRoom, context);
        }

        private static bool Terminal(RoomOutcome outcome) => outcome == RoomOutcome.BattleWon ||
            outcome == RoomOutcome.PlayerDefeated || outcome == RoomOutcome.Stalemate;

        internal static string? Validate(TrainCombatState source)
        {
            if (source.Rooms.Count < 2 || source.EnemySlotsPerRoom < 1 ||
                source.Rooms.Where((room, index) => room.RoomIndex != index).Any())
                return "Train rooms must be contiguous and include a Pyre room.";
            if (source.Rooms.SelectMany(room => room.Units).GroupBy(unit => unit.Id).Any(group => group.Count() > 1))
                return "Train unit identities must be unique.";
            if (source.Movement.GroupBy(rule => rule.UnitId).Any(group => group.Count() > 1))
                return "Enemy movement identities must be unique.";
            foreach (RoomCombatState room in source.Rooms)
            {
                string? error = RoomCombatModel.Validate(room);
                if (error != null) return error;
            }
            return null;
        }

        private static TrainCombatResult Unsupported(string error) => new TrainCombatResult(null,
            RoomOutcome.Unsupported, Array.Empty<RoomCombatResult>(), error);
    }
}

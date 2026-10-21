using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class BossRoomSelectionResult
    {
        public int Room { get; }
        public UnityRng Rng { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => UnsupportedReason == null;
        internal BossRoomSelectionResult(int room, UnityRng rng, string? reason = null)
        { Room = room; Rng = rng; UnsupportedReason = reason; }
    }

    public sealed class BossActionTargetState
    {
        public string Behavior { get; }
        public IReadOnlyList<bool> EnabledRooms { get; }
        public IReadOnlyList<bool> PlayerOccupiedRooms { get; }
        public IReadOnlyList<bool> TargetOccupiedRooms { get; }
        public IReadOnlyList<int> VisitedRooms { get; }
        public bool Initial { get; }
        public int CurrentRoom { get; }
        public int MovementSpeed { get; }
        public int TargetRoom { get; }
        public int PostActionRoom { get; }
        public bool SetupAsInitialAction { get; }
        public UnityRng Rng { get; }
        public BossActionTargetState(string behavior, IReadOnlyList<bool> enabledRooms,
            IReadOnlyList<bool> playerOccupiedRooms, IReadOnlyList<bool> targetOccupiedRooms,
            IReadOnlyList<int> visitedRooms, bool initial, int currentRoom, int movementSpeed,
            int targetRoom, int postActionRoom, bool setupAsInitialAction, UnityRng rng)
        {
            Behavior = behavior; EnabledRooms = Array.AsReadOnly(enabledRooms.ToArray());
            PlayerOccupiedRooms = Array.AsReadOnly(playerOccupiedRooms.ToArray());
            TargetOccupiedRooms = Array.AsReadOnly(targetOccupiedRooms.ToArray());
            VisitedRooms = Array.AsReadOnly(visitedRooms.ToArray()); Initial = initial; CurrentRoom = currentRoom;
            MovementSpeed = movementSpeed; TargetRoom = targetRoom; PostActionRoom = postActionRoom;
            SetupAsInitialAction = setupAsInitialAction; Rng = rng;
        }
    }

    public sealed class BossActionTargetResult
    {
        public int TargetRoom { get; }
        public int PostActionRoom { get; }
        public IReadOnlyList<int> VisitedRooms { get; }
        public bool SetupAsInitialAction { get; }
        public UnityRng Rng { get; }
        public string? UnsupportedReason { get; }
        public bool Supported => UnsupportedReason == null;
        public BossActionTargetResult(int targetRoom, int postActionRoom, IReadOnlyList<int> visitedRooms,
            bool setupAsInitialAction, UnityRng rng, string? unsupportedReason = null)
        {
            TargetRoom = targetRoom; PostActionRoom = postActionRoom;
            VisitedRooms = Array.AsReadOnly(visitedRooms.ToArray()); SetupAsInitialAction = setupAsInitialAction;
            Rng = rng; UnsupportedReason = unsupportedReason;
        }
    }

    // Native boss targeting only. Spell execution, movement, room destruction and
    // the action-group controller require their own battle-phase integration.
    public static class BossActionTargetModel
    {
        public static BossRoomSelectionResult SelectRandom(IReadOnlyList<int> rooms,
            IReadOnlyList<int>? visitedRooms, bool initial, UnityRng rng)
        {
            if (rooms.Count == 0) return new BossRoomSelectionResult(0, rng, "No candidate boss room.");
            if (initial) return new BossRoomSelectionResult(rooms[rooms.Count - 1], rng);
            int[] forced = visitedRooms == null ? Array.Empty<int>() : Enumerable.Range(0, visitedRooms.Count)
                .Where(index => visitedRooms[index] >= 2 && rooms.Contains(index)).ToArray();
            IReadOnlyList<int> candidates = forced.Length > 0 ? forced : rooms;
            RngDraw draw = rng.Range(0, candidates.Count);
            return new BossRoomSelectionResult(candidates[draw.Value], draw.State);
        }

        public static BossActionTargetResult Setup(BossActionTargetState source)
        {
            BossActionTargetResult Fail(string reason) => new BossActionTargetResult(source.TargetRoom,
                source.PostActionRoom, source.VisitedRooms, source.SetupAsInitialAction, source.Rng, reason);
            int count = source.EnabledRooms.Count;
            if (count == 0 || source.PlayerOccupiedRooms.Count != count || source.TargetOccupiedRooms.Count != count ||
                source.VisitedRooms.Count != count || !source.Initial && (source.CurrentRoom < 0 || source.CurrentRoom >= count))
                return Fail("Missing native boss room targeting state.");
            if (!source.Initial && source.SetupAsInitialAction)
                return new BossActionTargetResult(source.TargetRoom, source.PostActionRoom, source.VisitedRooms, true, source.Rng);
            int current = source.Initial ? -1 : source.CurrentRoom;
            UnityRng rng = source.Rng;
            BossRoomSelectionResult Random(bool initial, int exclude)
            {
                int[] candidates = Enumerable.Range(0, count)
                    .Where(index => index != exclude && (!initial || source.PlayerOccupiedRooms[index])).ToArray();
                if (initial && candidates.Length == 0)
                    candidates = Enumerable.Range(0, count).Where(index => index != exclude).ToArray();
                return SelectRandom(candidates, initial ? null : source.VisitedRooms, initial, rng);
            }
            BossRoomSelectionResult selected;
            if (source.Behavior == "RandomBossActionBehavior")
                // This native behavior deliberately omits currentRoom when it
                // calls SelectRandomRoom; choosing the current floor is legal.
                selected = Random(source.Initial, -1);
            else if (source.Behavior == "RandomOccupiedBossActionBehavior")
            {
                int[] occupied = Enumerable.Range(0, count)
                    .Where(index => index != current && source.TargetOccupiedRooms[index]).ToArray();
                selected = occupied.Length == 0 ? Random(source.Initial, current) :
                    SelectRandom(occupied, source.Initial ? null : source.VisitedRooms, source.Initial, rng);
            }
            else if (source.Behavior == "RoomDestroyActionBehavior")
            {
                int first = Enumerable.Range(0, count).FirstOrDefault(index => source.EnabledRooms[index]);
                selected = new BossRoomSelectionResult(first, rng);
            }
            else return Fail("Unknown boss room targeting behavior: " + source.Behavior);
            if (!selected.Supported) return Fail(selected.UnsupportedReason!);
            int target = selected.Room, post = target; rng = selected.Rng;
            if (!source.Initial)
            {
                if (source.MovementSpeed <= 0) target = current;
                else if (current == target)
                {
                    BossRoomSelectionResult next = Random(false, current);
                    if (!next.Supported) return Fail(next.UnsupportedReason!);
                    post = next.Room; rng = next.Rng;
                }
            }
            int[] visited = source.VisitedRooms.Select((value, index) => index == target ? 0 : value + 1).ToArray();
            return new BossActionTargetResult(target, post, visited, source.Initial, rng);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class CardTargets
    {
        public IReadOnlyList<int> UnitIds { get; }
        public string? UnsupportedReason { get; }
        public UnityRng? BattleRng { get; }
        public bool Supported => UnsupportedReason == null;
        internal CardTargets(IReadOnlyList<int> ids, string? error = null, UnityRng? battleRng = null)
        { UnitIds = Array.AsReadOnly(ids.ToArray()); UnsupportedReason = error; BattleRng = battleRng; }
    }

    public static class CardTargetModel
    {
        public static bool Supports(string mode) => new[] { "Room", "FrontInRoom", "BackInRoom", "Weakest", "RoomHealTargets",
            "DropTargetCharacter", "LastTargetedCharacters", "StrongestLastTargetedCharacters", "RandomInRoom" }.Contains(mode) || IsCrossRoom(mode);
        public static bool IsCrossRoom(string mode) => new[] { "Tower", "RandomFromAnyRoom", "FrontInAllRooms",
            "FrontInRoomAndRoomAbove", "WeakestAllRooms", "StrongestAllRooms", "StrongestLastTargetedCharactersRoom" }.Contains(mode);
        public static bool IsRandom(string mode) => mode == "RandomInRoom" || mode == "RandomFromAnyRoom";

        public static CardTargets Collect(TrainCombatState train, int roomIndex, CardActionEffect effect, IReadOnlyList<int> lastTargets,
            CombatTeam? dropTeam = null, int dropPosition = -1, bool firstEffect = false, bool isTesting = false, int? pyreRoomIndex = null,
            IReadOnlyDictionary<int, int>? pendingDeadRooms = null, IReadOnlyDictionary<int, int>? unitPositions = null)
        {
            RoomCombatState? room = train.Rooms.FirstOrDefault(item => item.RoomIndex == roomIndex);
            if (room == null) return new CardTargets(Array.Empty<int>(), "The selected target room does not exist.");
            string? filterError = effect.Filters?.Validate();
            if (filterError != null) return new CardTargets(Array.Empty<int>(), filterError);
            if (IsCrossRoom(effect.Target))
            {
                bool AllowsTeam(CombatTeam team) => team == CombatTeam.Enemy ? effect.AllowEnemy : effect.AllowPlayer;
                bool Allowed(CombatUnit unit) => AllowsTeam(unit.Team);
                bool Physical(CombatUnit unit) => Allowed(unit) && !unit.IsPyre && unit.Statuses.All(status => status.Id != "untouchable");
                bool Eligible(CombatUnit unit) => Physical(unit) && MatchesFilters(effect, unit, false, ref filterError);
                IEnumerable<CombatUnit> candidates;
                var positions = unitPositions ?? Positions(train);
                var collected = new List<CombatUnit>();
                void Add(RoomCombatState item, CombatTeam team)
                {
                    collected.AddRange(item.Units.Where(unit => unit.Team == team));
                    // RoomState re-sorts the entire accumulated list on every room/team addition.
                    collected.Sort((left, right) => left.Team == right.Team ? positions[left.Id].CompareTo(positions[right.Id]) : left.Team.CompareTo(right.Team));
                }
                if (effect.Target == "StrongestLastTargetedCharactersRoom")
                {
                    if (firstEffect || lastTargets.Count == 0) return new CardTargets(Array.Empty<int>());
                    RoomCombatState? lastRoom = train.Rooms.FirstOrDefault(item => item.Units.Any(unit => unit.Id == lastTargets[0]));
                    // The last damage victim keeps its spawn point until the next native trigger queue drains.
                    if (lastRoom == null && pendingDeadRooms?.TryGetValue(lastTargets[0], out int retainedRoom) == true)
                        lastRoom = train.Rooms.FirstOrDefault(item => item.RoomIndex == retainedRoom);
                    if (lastRoom == null) return new CardTargets(Array.Empty<int>());
                    candidates = lastRoom.Units.OrderBy(unit => unit.Team).Where(Eligible).OrderByDescending(unit => unit.Health).Take(1);
                }
                else if (effect.Target == "FrontInAllRooms" || effect.Target == "FrontInRoomAndRoomAbove")
                {
                    int? pyre = pyreRoomIndex ?? train.Rooms.FirstOrDefault(item => item.Units.Any(unit => unit.IsPyre))?.RoomIndex;
                    if (!pyre.HasValue) return new CardTargets(Array.Empty<int>(), "Front room ranges require the Pyre room definition.");
                    IEnumerable<RoomCombatState> affected = train.Rooms.Where(item => effect.Target == "FrontInAllRooms"
                        ? item.RoomIndex < pyre.Value : item.RoomIndex == roomIndex || item.RoomIndex == roomIndex + 1 && item.RoomIndex < pyre.Value);
                    if (effect.Target == "FrontInAllRooms")
                    {
                        foreach (RoomCombatState item in affected.OrderBy(item => item.RoomIndex))
                            foreach (CombatTeam team in new[] { CombatTeam.Enemy, CombatTeam.Player }) if (AllowsTeam(team)) Add(item, team);
                        var fronts = new HashSet<int>(affected.SelectMany(item => Front(item, CombatTeam.Enemy).Concat(Front(item, CombatTeam.Player))).Select(unit => unit.Id));
                        candidates = collected.Where(unit => fronts.Contains(unit.Id));
                    }
                    else candidates = new[] { CombatTeam.Enemy, CombatTeam.Player }.SelectMany(team => affected.OrderBy(item => item.RoomIndex)
                        .SelectMany(item => Front(item, team)));
                    IEnumerable<CombatUnit> Front(RoomCombatState item, CombatTeam team) => item.Units
                        .Where(unit => unit.Team == team && (effect.Target == "FrontInAllRooms" ? Physical(unit) : Eligible(unit))).Take(1);
                }
                else if (effect.Target == "WeakestAllRooms" || effect.Target == "StrongestAllRooms")
                {
                    foreach (RoomCombatState item in train.Rooms.OrderByDescending(item => item.RoomIndex))
                        foreach (CombatTeam team in new[] { CombatTeam.Enemy, CombatTeam.Player }) if (AllowsTeam(team)) Add(item, team);
                    candidates = collected.Where(Eligible);
                    candidates = effect.Target == "WeakestAllRooms" ? candidates.OrderBy(unit => unit.Health).Take(1) :
                        candidates.OrderByDescending(unit => unit.Health).Take(1);
                }
                else
                {
                    foreach (CombatTeam team in new[] { CombatTeam.Enemy, CombatTeam.Player }) if (AllowsTeam(team))
                        foreach (RoomCombatState item in train.Rooms.OrderBy(item => item.RoomIndex)) Add(item, team);
                    candidates = collected.Where(Eligible);
                }
                CombatUnit[] targets = candidates.ToArray();
                if (filterError != null) return new CardTargets(Array.Empty<int>(), filterError);
                return effect.Target == "RandomFromAnyRoom" ? Random(targets, train.Context, isTesting) :
                    new CardTargets(targets.Select(unit => unit.Id).ToArray());
            }
            if (effect.Target == "LastTargetedCharacters" || effect.Target == "StrongestLastTargetedCharacters")
                room = new RoomCombatState(roomIndex, room.Deployment, train.Rooms.SelectMany(item => item.Units).ToArray(),
                    Array.Empty<string>(), train.Context, room.Preview);
            return Collect(room, effect, lastTargets, dropTeam, dropPosition, firstEffect, isTesting);
        }

        public static CardTargets Collect(RoomCombatState room, CardActionEffect effect, IReadOnlyList<int> lastTargets,
            CombatTeam? dropTeam = null, int dropPosition = -1, bool firstEffect = false, bool isTesting = false)
        {
            if (!Supports(effect.Target)) return new CardTargets(Array.Empty<int>(), "Unmodeled target mode " + effect.Target);
            if (IsCrossRoom(effect.Target)) return new CardTargets(Array.Empty<int>(), "Cross-room targeting requires the complete train.");
            string? filterError = effect.Filters?.Validate();
            if (filterError != null) return new CardTargets(Array.Empty<int>(), filterError);
            if (effect.Target == "LastTargetedCharacters")
                return new CardTargets(firstEffect ? Array.Empty<int>() : lastTargets.Where(id => room.Units.Any(unit => unit.Id == id && Allowed(unit))).ToArray());
            if (effect.Target == "StrongestLastTargetedCharacters")
            {
                // Native strongest-last ignores team filters and keeps dead references (with zero HP).
                int[] ordered = firstEffect ? Array.Empty<int>() : lastTargets.OrderByDescending(id =>
                    room.Units.FirstOrDefault(unit => unit.Id == id)?.Health ?? 0).ToArray();
                return new CardTargets(ordered.Take(1).ToArray());
            }
            if (effect.Target == "DropTargetCharacter")
            {
                CombatUnit? occupant = dropPosition < 0 ? null : room.Units.Where(unit => unit.Team == dropTeam).ElementAtOrDefault(dropPosition);
                bool accepted = occupant != null && Allowed(occupant) && MatchesFilters(effect, occupant, true, ref filterError);
                return new CardTargets(accepted ? new[] { occupant!.Id } : Array.Empty<int>(), filterError);
            }
            CombatUnit[] candidates = room.Units.OrderBy(unit => unit.Team).Where(unit => Allowed(unit) && !unit.IsPyre &&
                !unit.Statuses.Any(status => status.Id == "untouchable") && MatchesFilters(effect, unit, false, ref filterError)).ToArray();
            if (filterError != null) return new CardTargets(Array.Empty<int>(), filterError);
            if (effect.Target == "RandomInRoom")
                return Random(candidates, room.Context, isTesting);
            if (effect.Target == "RoomHealTargets")
            {
                if (candidates.Any(unit => unit.Modifiers == null))
                    return new CardTargets(Array.Empty<int>(), "Heal target collection requires healability state.");
                candidates = candidates.Where(unit => unit.Modifiers!.CanBeHealed).ToArray();
            }
            else if (effect.Target == "FrontInRoom") candidates = candidates.Take(1).ToArray();
            else if (effect.Target == "BackInRoom") candidates = candidates.Reverse().Take(1).ToArray();
            else if (effect.Target == "Weakest") candidates = candidates.OrderBy(unit => unit.Health).Take(1).ToArray();
            return new CardTargets(candidates.Select(unit => unit.Id).ToArray());
            bool Allowed(CombatUnit unit) => unit.Team == CombatTeam.Enemy ? effect.AllowEnemy : effect.AllowPlayer;
        }

        private static CardTargets Random(IReadOnlyList<CombatUnit> candidates, CombatContext? context, bool isTesting)
        {
            if (candidates.Count == 0) return new CardTargets(Array.Empty<int>());
            // Supported effect tests depend on target count. They do not consume gameplay RNG.
            if (isTesting) return new CardTargets(new[] { candidates[0].Id });
            if (context == null) return new CardTargets(Array.Empty<int>(), "Random targeting requires Battle RNG.");
            RngDraw chosen = context.BattleRng.Range(0, candidates.Count);
            return new CardTargets(new[] { candidates[chosen.Value].Id }, battleRng: chosen.State);
        }

        internal static CardTargets RandomCandidates(TrainCombatState train, int roomIndex, CardActionEffect effect) => Collect(train, roomIndex,
            new CardActionEffect(effect.Type, effect.Target == "RandomFromAnyRoom" ? "Tower" : "Room", effect.Value, effect.AllowEnemy,
                effect.AllowPlayer, effect.Statuses, effect.Upgrade, effect.Lifetime, effect.Tests, effect.Range, effect.Filters), Array.Empty<int>());

        private static bool MatchesFilters(CardActionEffect effect, CombatUnit unit, bool dropOverride, ref string? error)
        {
            if (effect.Filters == null) return true;
            bool matches = effect.Filters.Matches(unit, dropOverride, out string? filterError);
            error ??= filterError; return matches;
        }

        internal static Dictionary<int, int> Positions(TrainCombatState train) => train.Rooms.SelectMany(room =>
            new[] { CombatTeam.Enemy, CombatTeam.Player }.SelectMany(team => room.Units.Where(unit => unit.Team == team)
                .Select((unit, index) => new { unit.Id, Index = index }))).ToDictionary(item => item.Id, item => item.Index);
    }
}

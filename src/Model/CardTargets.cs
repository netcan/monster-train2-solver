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
            "DropTargetCharacter", "LastTargetedCharacters", "StrongestLastTargetedCharacters", "RandomInRoom" }.Contains(mode);

        public static CardTargets Collect(TrainCombatState train, int roomIndex, CardActionEffect effect, IReadOnlyList<int> lastTargets,
            CombatTeam? dropTeam = null, int dropPosition = -1, bool firstEffect = false, bool isTesting = false)
        {
            RoomCombatState? room = train.Rooms.FirstOrDefault(item => item.RoomIndex == roomIndex);
            if (room == null) return new CardTargets(Array.Empty<int>(), "The selected target room does not exist.");
            if (effect.Target == "LastTargetedCharacters" || effect.Target == "StrongestLastTargetedCharacters")
                room = new RoomCombatState(roomIndex, room.Deployment, train.Rooms.SelectMany(item => item.Units).ToArray(),
                    Array.Empty<string>(), train.Context, room.Preview);
            return Collect(room, effect, lastTargets, dropTeam, dropPosition, firstEffect, isTesting);
        }

        public static CardTargets Collect(RoomCombatState room, CardActionEffect effect, IReadOnlyList<int> lastTargets,
            CombatTeam? dropTeam = null, int dropPosition = -1, bool firstEffect = false, bool isTesting = false)
        {
            if (!Supports(effect.Target)) return new CardTargets(Array.Empty<int>(), "Unmodeled target mode " + effect.Target);
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
                return new CardTargets(occupant != null && Allowed(occupant) ? new[] { occupant.Id } : Array.Empty<int>());
            }
            CombatUnit[] candidates = room.Units.OrderBy(unit => unit.Team).Where(unit => Allowed(unit) && !unit.IsPyre &&
                !unit.Statuses.Any(status => status.Id == "untouchable")).ToArray();
            if (effect.Target == "RandomInRoom")
            {
                if (candidates.Length == 0) return new CardTargets(Array.Empty<int>());
                // Supported effect tests depend on target count. They do not consume gameplay RNG.
                if (isTesting) return new CardTargets(new[] { candidates[0].Id });
                if (room.Context == null) return new CardTargets(Array.Empty<int>(), "Random targeting requires Battle RNG.");
                RngDraw chosen = room.Context.BattleRng.Range(0, candidates.Length);
                return new CardTargets(new[] { candidates[chosen.Value].Id }, battleRng: chosen.State);
            }
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
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class EnchantmentSourcePosition
    {
        public int UnitId { get; }
        public CombatTeam Team { get; }
        public int RoomIndex { get; }
        public int IndexInRoom { get; }
        public EnchantmentSourcePosition(int unitId, CombatTeam team, int roomIndex, int indexInRoom)
        { UnitId = unitId; Team = team; RoomIndex = roomIndex; IndexInRoom = indexInRoom; }
    }

    internal static class EnchantmentSourceOrderModel
    {
        // AddCharactersToList sorts the ENTIRE accumulated list on every room call,
        // including empty rooms. Native Team.Heroes=1 and Team.Monsters=2.
        internal static int[] Collect(int roomCount, IReadOnlyList<EnchantmentSourcePosition> positions,
            IReadOnlyList<EnchantmentSourcePosition>? initial = null)
        {
            var actors = (initial ?? Array.Empty<EnchantmentSourcePosition>()).ToList();
            var existing = actors.Select(actor => actor.UnitId).ToHashSet();
            foreach (CombatTeam team in new[] { CombatTeam.Player, CombatTeam.Enemy })
                for (int room = 0; room < roomCount; room++)
                {
                    foreach (var actor in positions.Where(item => item.RoomIndex == room && item.Team == team).OrderBy(item => item.IndexInRoom))
                        if (existing.Add(actor.UnitId)) actors.Add(actor);
                    actors.Sort((a, b) => a.Team != b.Team ? (a.Team == CombatTeam.Enemy ? -1 : 1) : a.IndexInRoom.CompareTo(b.IndexInRoom));
                }
            return actors.Select(item => item.UnitId).ToArray();
        }
    }
}

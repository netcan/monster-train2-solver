using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class CardPlayedTriggerModel
    {
        internal static TrainCombatResult Rally(TrainCombatState source, CombatTeam team, IReadOnlyList<int> cachedUnitIds)
        {
            string? error = TrainCombatModel.Validate(source);
            if (error != null) return new TrainCombatResult(null, RoomOutcome.Unsupported, System.Array.Empty<RoomCombatResult>(), error);
            var cached = new HashSet<int>(cachedUnitIds);
            // CardState caches the original room's living, non-spawning actors.
            // Movement keeps membership, while deaths and newly created actors do not.
            var queue = source.Rooms.SelectMany(room => room.Units.Where(unit => unit.Team == team && unit.Health > 0 && cached.Contains(unit.Id))
                .Select(unit => new RoomCombatModel.QueuedCharacterTrigger(room.RoomIndex, unit, "CardMonsterPlayed")))
                .OrderBy(item => item.Unit.Id).ToList();
            return TrainCombatModel.ApplyCharacterQueue(source, queue);
        }
    }
}

using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class EncounterCompleteModel
    {
        // StopCombatLoop's terminal snapshot precedes this native callback. The
        // roster order is MonsterManager.activeMonsters, independent of floors.
        public static TrainCombatResult Apply(TrainCombatState source, IReadOnlyList<int> activeMonsterOrder)
        {
            source = EnterPhase(source);
            var roster = source.Rooms.SelectMany(room => room.Units.Select(unit => (Room: room, Unit: unit)))
                .Where(item => item.Unit.Team == CombatTeam.Player).ToArray();
            if (activeMonsterOrder.Distinct().Count() != activeMonsterOrder.Count ||
                activeMonsterOrder.Any(id => !roster.Any(item => item.Unit.Id == id)))
                return new TrainCombatResult(null, RoomOutcome.Unsupported, new List<RoomCombatResult>(),
                    "Encounter completion requires the captured native active-monster order.");
            var queue = new List<RoomCombatModel.QueuedCharacterTrigger>();
            foreach (int id in activeMonsterOrder)
            {
                var actor = roster.Single(item => item.Unit.Id == id);
                queue.Add(new RoomCombatModel.QueuedCharacterTrigger(actor.Room.RoomIndex, actor.Unit, "OnEncounterComplete"));
            }
            return TrainCombatModel.ApplyCharacterQueue(source, queue);
        }
        // StopCombatLoop sets the phase after the terminal combat snapshot,
        // before MonsterManager drains encounter-complete triggers.
        internal static TrainCombatState EnterPhase(TrainCombatState source) => source.Context?.EnergyState is BattleEnergyState energy
            ? CardSpellModel.WithContext(source, source.Context.WithEnergyState(energy.With(phase: "EndOfCombat"))) : source;
    }
}

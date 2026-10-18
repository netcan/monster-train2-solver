using System.Linq;
using System.Collections.Generic;

namespace MonsterTrain2Poju.Model
{
    internal static class MoonPhaseStatusModel
    {
        internal static bool IsTracking(CombatContext? context, IEnumerable<CombatUnit> units) =>
            context?.QueryFrame?.MoonPhaseTrackingActive ?? units.Any(unit =>
                unit.Status("newmoon")?.Stacks > 0 || unit.Status("fullmoon")?.Stacks > 0);

        internal static CombatUnit WithPhase(CombatUnit unit, int phase)
        {
            if (unit.IsPyre || phase != 1 && phase != 2) return unit;
            string id = phase == 1 ? "newmoon" : "fullmoon";
            var statuses = unit.Statuses.Where(status => status.Id != "newmoon" && status.Id != "fullmoon").ToList();
            statuses.Add(new CombatStatus(id, 1, 1, triggerVfxEnemy: false, triggerVfxPlayer: false,
                stackable: false, hidden: true, displayCategory: "Persistent", propagatable: false,
                excludeHeroPropagation: false, excludeMonsterPropagation: false));
            return CardSpellModel.Copy(unit, unit.Health, statuses);
        }

        internal static TrainCombatState WithPhase(TrainCombatState source, int phase)
        {
            if (phase != 1 && phase != 2) return source;
            RoomCombatState[] rooms = source.Rooms.Select(room => new RoomCombatState(room.RoomIndex, room.Deployment,
                room.Units.Select(unit => WithPhase(unit, phase)
                    .WithMoonPhaseMask(RoomMoonPhaseModel.Effective(source.Context, room.RoomIndex, phase))).ToArray(), room.ExternalInteractions,
                room.Context, room.Preview, room.ActiveCompanionBossIds)).ToArray();
            return CardSpellModel.WithContext(new TrainCombatState(rooms, source.Movement,
                source.EnemySlotsPerRoom, source.Context), source.Context!);
        }
    }
}

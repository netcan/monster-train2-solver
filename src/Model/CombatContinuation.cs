using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public static class CombatContinuationModel
    {
        // CheckIfUnitCombatShouldContinue excludes life-linked actors from
        // either team's surviving combatants. They still participate in the
        // first exchange; ordinary wave enemies can keep a Pyre fight going.
        internal static bool Counts(bool alive, bool inRoom, bool untouchable, bool lifeLink)
            => alive && inRoom && !untouchable && !lifeLink;
        public static bool ShouldContinue(RoomCombatState room, bool relentlessRoom)
        {
            bool Fighting(CombatUnit unit) => Counts(unit.Health > 0 && unit.DeathState?.IsDestroyed != true,
                true, unit.Status("untouchable") != null, unit.Status("lifelink") != null);
            return relentlessRoom && room.Units.Any(unit => unit.Team == CombatTeam.Player && Fighting(unit)) &&
                room.Units.Any(unit => unit.Team == CombatTeam.Enemy && Fighting(unit));
        }
    }
}

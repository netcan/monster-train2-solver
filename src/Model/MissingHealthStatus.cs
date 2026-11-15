using System;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class MissingHealthStatusModel
    {
        internal const string FriendlyEffect = "AddStatusPerFriendlyMissingHp";
        internal const string CasterDamage = "DamagePerCasterStatus";
        internal static int FriendlyMissingHealth(TrainCombatState state, int actorId, bool allFloors)
        {
            RoomCombatState? actorRoom = state.Rooms.FirstOrDefault(room => room.Units.Any(unit => unit.Id == actorId));
            if (!allFloors && actorRoom == null) return 0;
            int total = 0;
            foreach (CombatUnit unit in state.Rooms.Where(room => allFloors || room == actorRoom).SelectMany(room => room.Units)
                .Where(unit => unit.Team == CombatTeam.Player && unit.Health > 0 && unit.DeathState?.IsDestroyed != true))
            {
                int missing = unchecked(unit.MaxHealth - unit.Health);
                if (missing > 0) total = unchecked(total + missing);
            }
            return total;
        }
        internal static CardActionEffect AsStatus(CardActionEffect effect, int count) =>
            new CardActionEffect("AddStatus", effect.Target, 0, effect.AllowEnemy, effect.AllowPlayer,
                new[] { effect.Statuses[0].WithStacks(count) }, tests: effect.Tests, filters: effect.Filters,
                ignoreDualism: effect.Type == FriendlyEffect && effect.IgnoreDualism);
    }
}

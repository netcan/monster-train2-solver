using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RoomHealthAttackModel
    {
        // The existing serialized SelfAttackRules list stores native room damage
        // modifiers on their owner; friendly rules are resolved by room, not self.
        internal static bool HasSource(CombatUnit unit) => unit.Modifiers?.SelfAttackRules?
            .Any(rule => rule.Type == RoomHealthDamageModel.FriendlyOwn) == true;
        internal static bool EligibleSource(CombatUnit unit) => unit.Health > 0 &&
            unit.DeathState?.IsDestroyed != true && unit.DeathState?.IsDespawned != true &&
            unit.Status("untouchable") == null && !PyreBaneModel.ExcludedFromRoomMode(unit, "Room", false);
        internal static int Additional(CombatUnit target, CombatContext? context, RoomCombatState? room = null)
        {
            int? floor = context?.SpawnPoints?.Units.SingleOrDefault(point => point.UnitId == target.Id)?.Current?.RoomIndex;
            if (target.Team != CombatTeam.Player || !floor.HasValue) return 0;
            room ??= context?.Enchantments?.Rooms.SingleOrDefault(value => value.RoomIndex == floor.Value);
            if (room == null || room.RoomIndex != floor.Value) return 0;
            return Additional(target, room.Units.Where(source => source.Team == target.Team && EligibleSource(source)));
        }
        internal static int Additional(CombatUnit target, IEnumerable<CombatUnit> eligibleSources, int? health = null)
        {
            if (target.Team != CombatTeam.Player) return 0;
            int result = 0;
            foreach (CombatUnit source in eligibleSources)
            foreach (UnitSelfAttackRule rule in source.Modifiers?.SelfAttackRules ?? Array.Empty<UnitSelfAttackRule>())
                if (rule.Type == RoomHealthDamageModel.FriendlyOwn)
                    result = unchecked(result + RoomHealthDamageModel.Query(rule.Type, rule.Coefficient, rule.Divisor,
                        target.MaxHealth, health ?? target.Health, true, true, true, false, true));
            return result;
        }
    }
}

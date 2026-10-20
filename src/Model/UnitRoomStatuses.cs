using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class UnitRoomStatusRule
    {
        public string Type { get; }
        public string StatusId { get; }
        public int Amount { get; }
        public UnitRoomStatusRule(string type, string statusId, int amount)
        { Type = type; StatusId = statusId; Amount = amount; }
    }
    internal static class UnitRoomStatusModel
    {
        internal static string Status(string type) => type == "RoomStateBuffModifier" ? "buff" :
            type == "RoomStateSpikesDamageModifier" ? "spikes" : type == "RoomStateValorModifier" ? "valor" : "";
        internal static bool Known(string type) => Status(type).Length != 0;
        internal static string? Validate(CombatUnit unit) => unit.Modifiers?.RoomStatuses?.Any(rule =>
            !Known(rule.Type) || rule.StatusId != Status(rule.Type) || rule.Amount != (rule.StatusId == "buff" ? 3 : 1)) == true
            ? "Uncalibrated unit room status modifier." : null;
        internal static int Modifier(IEnumerable<CombatUnit> units, CombatTeam team, string status) => units
            .Where(unit => unit.Team == team && unit.Health > 0 && unit.DeathState?.IsDestroyed != true)
            .Sum(unit => unit.Modifiers?.RoomStatuses?.Where(rule => rule.StatusId == status).Sum(rule => rule.Amount) ?? 0);
        internal static int AdditionalAttack(RoomCombatState room, CombatUnit unit) =>
            unchecked(Modifier(room.Units, unit.Team, "buff") * (unit.Status("buff")?.Stacks ?? 0) +
                Modifier(room.Units, unit.Team, "valor") * (unit.Status("valor")?.Stacks ?? 0));
    }
}

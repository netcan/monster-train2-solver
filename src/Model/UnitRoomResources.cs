using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class UnitRoomResourceRule
    {
        public string Type { get; }
        public int Amount { get; }
        public bool DisallowedInDeployment { get; }
        public UnitRoomResourceRule(string type, int amount, bool disallowedInDeployment)
        { Type = type; Amount = amount; DisallowedInDeployment = disallowedInDeployment; }
    }

    internal static class UnitRoomResourceModel
    {
        internal const string Energy = "RoomStateEnergyModifier";
        internal const string Draw = "RoomStateHandSizeModifier";
        internal static bool Known(string type) => type is Energy or Draw;
        internal static string? Validate(CombatUnit unit) => unit.Modifiers?.RoomResources?.Any(rule =>
            !Known(rule.Type) || rule.DisallowedInDeployment || rule.Amount != (rule.Type == Energy ? 2 : 1)) == true
            ? "Uncalibrated unit room resource modifier." : null;
        internal static int Amount(TrainCombatState train, string type) => train.Rooms.Where(room => !room.Preview).SelectMany(room => room.Units)
            .Where(unit => unit.Team == CombatTeam.Player && unit.Health > 0 && unit.DeathState?.IsDestroyed != true)
            .Sum(unit => unit.Modifiers?.RoomResources?.Where(rule => rule.Type == type).Sum(rule => rule.Amount) ?? 0);
    }
}

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
        internal const string HealCost = "RoomStateHealCostModifier";
        internal static bool Known(string type) => type is Energy or Draw or HealCost;
        internal static string? Validate(CombatUnit unit) => unit.Modifiers?.RoomResources?.Any(rule =>
            !Known(rule.Type) || rule.DisallowedInDeployment || rule.Amount != (rule.Type == Energy ? 2 : rule.Type == Draw ? 1 : -1)) == true
            ? "Uncalibrated unit room resource modifier." : null;
        internal static int Amount(TrainCombatState train, string type, int? roomIndex = null) => train.Rooms
            .Where(room => !room.Preview && (!roomIndex.HasValue || room.RoomIndex == roomIndex)).SelectMany(room => room.Units)
            .Where(unit => unit.Team == CombatTeam.Player && unit.Health > 0 && unit.DeathState?.IsDestroyed != true)
            .Sum(unit => unit.Modifiers?.RoomResources?.Where(rule => rule.Type == type).Sum(rule => rule.Amount) ?? 0);
        internal static bool EligibleForHealingCost(CardPlayRule rule, CardInstanceState? card) =>
            rule.Effects.Any(effect => effect.Type == "Heal" || effect.Type == "AddStatus" && effect.Statuses.Any(status => status.Id == "regen")) ||
                rule.OnCastEffects.Concat(rule.OnUnplayedNegativeEffects).Concat(rule.OnForgeEffects ?? Array.Empty<CardActionEffect>()).Any(effect => effect.Type == "Heal") ||
                card?.MaskDescriptor?.Purified != true && rule.SpawnUnit?.Triggers.Any(trigger => trigger.Kind == "OnSpawn" &&
                    trigger.Effects.Any(effect => effect.Action is { Type: "AddStatus" } action && action.Statuses.Any(status => status.Id == "regen"))) == true;
        internal static int HealingCostDelta(TrainCombatState train, int roomIndex, CardPlayRule rule, CardInstanceState? card)
        {
            int amount = Amount(train, HealCost, roomIndex);
            return amount != 0 && EligibleForHealingCost(rule, card) ? amount : 0;
        }
    }
}

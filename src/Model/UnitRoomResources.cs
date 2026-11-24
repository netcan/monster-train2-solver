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
        public string? Subtype { get; }
        public UnitRoomResourceRule(string type, int amount, bool disallowedInDeployment, string? subtype = null)
        { Type = type; Amount = amount; DisallowedInDeployment = disallowedInDeployment; Subtype = subtype; }
    }

    internal static class UnitRoomResourceModel
    {
        internal const string Energy = "RoomStateEnergyModifier";
        internal const string Draw = "RoomStateHandSizeModifier";
        internal const string HealCost = "RoomStateHealCostModifier";
        internal const string SubtypeCost = "RoomStateUnitCostModifier";
        internal static bool Known(string type) => type is Energy or Draw or HealCost or SubtypeCost;
        internal static string? Validate(CombatUnit unit)
        {
            var rules = unit.Modifiers?.RoomResources;
            if (rules == null) return null;
            if (rules.Any(rule => !Known(rule.Type) || rule.DisallowedInDeployment ||
                rule.Amount != (rule.Type == Energy ? 2 : rule.Type == Draw ? 1 : -1) ||
                (rule.Type == SubtypeCost
                    ? rule.Subtype != CardShardtailImpCostModel.Subtype || unit.AssetKey != CardShardtailImpCostModel.UnitKey || unit.Team != CombatTeam.Player
                    : rule.Subtype != null)) || rules.Count(rule => rule.Type == SubtypeCost) > 1)
                return "Uncalibrated unit room resource modifier.";
            return null;
        }
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

        internal static int UnitCostDelta(TrainCombatState train, int roomIndex, CardPlayRule rule)
        {
            if (rule.CardType != "Monster" || rule.SpawnUnit == null) return 0;
            int amount = Amount(train, SubtypeCost, roomIndex);
            if (amount == 0) return 0;
            bool deployment = train.Rooms.Any(room => room.Deployment);
            return rule.SpawnUnit.Subtypes.Contains(CardShardtailImpCostModel.Subtype) ||
                OriginalAllSubtypesModel.Active(train.Context, deployment) ? amount : 0;
        }
    }
}

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
        internal const string QueuedStatus = "RoomStateAddStatusEffectOnStatusApplied";
        internal const string StackModifier = "RoomStateStatusEffectStackModifier";
        internal static string Status(string type) => type == "RoomStateBuffModifier" ? "buff" :
            type == "RoomStateSpikesDamageModifier" ? "spikes" : type == "RoomStateValorModifier" ? "valor" : type == QueuedStatus ? "conduit" : type == StackModifier ? "pyregel" : "";
        internal static bool Known(string type) => Status(type).Length != 0;
        internal static string? Validate(CombatUnit unit) => unit.Modifiers?.RoomStatuses?.Any(rule =>
            !Known(rule.Type) || rule.StatusId != Status(rule.Type) || rule.Amount != (rule.StatusId == "buff" ? 3 : 1)) == true
            ? "Uncalibrated unit room status modifier." : null;
        internal static int Modifier(IEnumerable<CombatUnit> units, CombatTeam team, string status) => units
            .Where(unit => unit.Team == team && unit.Health > 0 && unit.DeathState?.IsDestroyed != true)
            .Sum(unit => unit.Modifiers?.RoomStatuses?.Where(rule => rule.StatusId == status).Sum(rule => rule.Amount) ?? 0);
        internal static int AddedStacks(IEnumerable<CombatUnit> units, string status) => units
            .Where(unit => unit.Team == CombatTeam.Player && unit.Health > 0 && unit.DeathState?.IsDestroyed != true)
            .Sum(unit => unit.Modifiers?.RoomStatuses?.Where(rule => rule.Type == StackModifier && rule.StatusId == status)
                .Sum(rule => rule.Amount) ?? 0);
        internal static int AdditionalAttack(RoomCombatState room, CombatUnit unit) =>
            unchecked(Modifier(room.Units, unit.Team, "buff") * (unit.Status("buff")?.Stacks ?? 0) +
                Modifier(room.Units, unit.Team, "valor") * (unit.Status("valor")?.Stacks ?? 0));

        internal static void QueueApplied(RoomCombatState room, CombatUnit target, string status,
            ICollection<RoomCombatModel.QueuedCharacterTrigger> queue)
        {
            foreach (CombatUnit owner in room.Units.Where(unit => unit.Team == CombatTeam.Player && unit.Health > 0 && unit.DeathState?.IsDestroyed != true))
            foreach (UnitRoomStatusRule rule in owner.Modifiers?.RoomStatuses ?? Array.Empty<UnitRoomStatusRule>())
                if (rule.Type == QueuedStatus && rule.StatusId == status)
                    // The native process-global tracking counter is used only as an
                    // equality token. Branch-local owner identities preserve that routing.
                    queue.Add(new RoomCombatModel.QueuedCharacterTrigger(room.RoomIndex, target, "OnQueuedStatusEffectToAdd",
                        paramInt: rule.Amount, paramInt2: owner.Id, paramString: status));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class UnitSelfAttackRule
    {
        public string Type { get; }
        public int Coefficient { get; }
        public int Divisor { get; }
        public string StatusId { get; }
        public UnitSelfAttackRule(string type, int coefficient, int divisor = 1, string statusId = "")
        { Type = type; Coefficient = coefficient; Divisor = divisor; StatusId = statusId; }
    }

    internal static class UnitSelfAttackModel
    {
        internal static bool Known(string type) => type is "RoomStateSelfDamagePerGoldModifier" or
            "RoomStateSelfDamagePerCorruptionModifier" or "RoomStateSelfDamagePerStatusEffectStack";

        internal static string? Validate(RoomCombatState source, CombatUnit unit)
        {
            if (unit.Modifiers?.SelfAttackRules is not { Count: > 0 } rules) return null;
            if (source.Context?.SpawnPoints == null)
                return "Self attack modifiers require captured physical position ownership.";
            foreach (UnitSelfAttackRule rule in rules)
            {
                if (!Known(rule.Type) || rule.Divisor <= 0 ||
                    rule.Type == "RoomStateSelfDamagePerStatusEffectStack" && rule.StatusId.Length == 0)
                    return "Unsupported self attack modifier definition.";
                if (rule.Type == "RoomStateSelfDamagePerGoldModifier" &&
                    (source.Context.QueryFrame?.RunningCombat == null || source.Context.Statistics == null))
                    return "Self attack gold scaling requires captured combat-loop state and turn-start statistics.";
                if (rule.Type == "RoomStateSelfDamagePerCorruptionModifier" &&
                    source.Context.RoomCapacities?.SingleOrDefault(room => room.RoomIndex == source.RoomIndex)?.Corruption == null)
                    return "Self attack corruption scaling requires captured room corruption.";
            }
            return null;
        }

        internal static int Additional(CombatUnit unit, CombatContext? context, IEnumerable<CombatStatus>? statuses = null)
        {
            if (unit.Modifiers?.SelfAttackRules is not { Count: > 0 } rules) return 0;
            int? roomIndex = context?.SpawnPoints?.Units.SingleOrDefault(point => point.UnitId == unit.Id)?.Current?.RoomIndex;
            if (!roomIndex.HasValue) return 0;
            int value = 0;
            foreach (UnitSelfAttackRule rule in rules)
            {
                int count = rule.Type == "RoomStateSelfDamagePerGoldModifier" ?
                    (context!.QueryFrame!.RunningCombat == true ? context.Statistics!.GoldStartOfThisTurn : context.Gold) / rule.Divisor :
                    rule.Type == "RoomStateSelfDamagePerCorruptionModifier" ? context!.RoomCapacities!.Single(room => room.RoomIndex == roomIndex.Value).Corruption!.Value :
                    (statuses ?? unit.Statuses).FirstOrDefault(status => status.Id == rule.StatusId)?.Stacks ?? 0;
                value = unchecked(value + unchecked(count * rule.Coefficient));
            }
            return value;
        }
    }
}

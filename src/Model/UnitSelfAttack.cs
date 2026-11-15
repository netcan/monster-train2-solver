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
        internal const string MissingHealth = "RoomStateSelfDamagePerMissingHp";
        internal const string FriendlyMissingHealth = RoomHealthDamageModel.FriendlyOwn;
        internal static bool Known(string type) => type is "RoomStateSelfDamagePerGoldModifier" or
            "RoomStateSelfDamagePerCorruptionModifier" or "RoomStateSelfDamagePerStatusEffectStack" or MissingHealth or FriendlyMissingHealth;

        internal static string? Validate(RoomCombatState source, CombatUnit unit)
        {
            if (unit.Modifiers?.SelfAttackRules is not { Count: > 0 } rules) return null;
            if (source.Context?.SpawnPoints == null)
                return "Self attack modifiers require captured physical position ownership.";
            if (rules.Any(rule => rule.Type == FriendlyMissingHealth) && source.Context.Enchantments == null)
                return "Friendly room attack modifiers require the captured actor world.";
            foreach (UnitSelfAttackRule rule in rules)
            {
                if (!Known(rule.Type) || rule.Divisor <= 0 ||
                    rule.Type == MissingHealth && (rule.Coefficient != 1 || rule.Divisor != 1 || rule.StatusId.Length != 0) ||
                    rule.Type == FriendlyMissingHealth && rule.StatusId.Length != 0 ||
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

        internal static int Additional(CombatUnit unit, CombatContext? context, IEnumerable<CombatStatus>? statuses = null,
            int? currentHealth = null)
        {
            if (unit.Modifiers?.SelfAttackRules is not { Count: > 0 } rules) return 0;
            int? roomIndex = context?.SpawnPoints?.Units.SingleOrDefault(point => point.UnitId == unit.Id)?.Current?.RoomIndex;
            if (!roomIndex.HasValue) return 0;
            int value = 0;
            foreach (UnitSelfAttackRule rule in rules)
            {
                // This source's friendly modifier is collected by the room query,
                // which can also affect other units on its floor.
                if (rule.Type == FriendlyMissingHealth) continue;
                if (rule.Type == MissingHealth)
                {
                    value = unchecked(value + RoomHealthDamageModel.Query(rule.Type, rule.Coefficient, rule.Divisor,
                        unit.MaxHealth, currentHealth ?? unit.Health, unit.Team == CombatTeam.Player,
                        true, true, true, true));
                    continue;
                }
                int count = rule.Type == "RoomStateSelfDamagePerGoldModifier" ?
                    (context!.QueryFrame!.RunningCombat == true ? context.Statistics!.GoldStartOfThisTurn : context.Gold) / rule.Divisor :
                    rule.Type == "RoomStateSelfDamagePerCorruptionModifier" ? context!.RoomCapacities!.Single(room => room.RoomIndex == roomIndex.Value).Corruption!.Value :
                    (statuses ?? unit.Statuses).FirstOrDefault(status => status.Id == rule.StatusId)?.Stacks ?? 0;
                value = unchecked(value + unchecked(count * rule.Coefficient));
            }
            return value;
        }
    }

    internal static class UnitSelfAttackUpgradeModel
    {
        internal static string? Validate(CardUpgradeModifier upgrade) => upgrade.SelfAttackRules?.Any(rule =>
            rule.StatusId.Length != 0 || (rule.Type == UnitSelfAttackModel.FriendlyMissingHealth
                ? rule.Divisor <= 0 : rule.Type != UnitSelfAttackModel.MissingHealth || rule.Coefficient != 1 || rule.Divisor != 1)) == true
            ? "Uncalibrated upgrade self attack modifier." : null;

        internal static IReadOnlyList<UnitSelfAttackRule>? BirthRules(IReadOnlyList<UnitSelfAttackRule>? original,
            IEnumerable<CardUpgradeModifier> upgrades)
        {
            var added = upgrades.SelectMany(upgrade => upgrade.SelfAttackRules ?? Array.Empty<UnitSelfAttackRule>()).ToArray();
            return added.Length == 0 ? original : (original ?? Array.Empty<UnitSelfAttackRule>()).Concat(added).ToArray();
        }

        internal static IReadOnlyList<UnitSelfAttackRule>? Change(IReadOnlyList<UnitSelfAttackRule>? original,
            CardUpgradeModifier upgrade, bool remove, out string? error)
        {
            error = Validate(upgrade);
            if (error != null || upgrade.SelfAttackRules?.Count is not > 0) return original;
            var rules = (original ?? Array.Empty<UnitSelfAttackRule>()).ToList();
            foreach (var rule in upgrade.SelfAttackRules)
            {
                if (!remove) rules.Add(rule);
                else
                {
                    int index = rules.FindLastIndex(value => value.Type == rule.Type && value.Coefficient == rule.Coefficient &&
                        value.Divisor == rule.Divisor && value.StatusId == rule.StatusId);
                    if (index < 0) { error = "Removing an upgrade requires its applied self attack modifier."; return original; }
                    rules.RemoveAt(index);
                }
            }
            return rules.Count == 0 ? null : rules.ToArray();
        }
    }
}

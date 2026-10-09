using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class TriggerCountModifier
    {
        public string Kind { get; }
        public int Value { get; }
        public TriggerCountModifier(string kind, int value) { Kind = kind; Value = value; }
    }

    public sealed class EquipmentTriggerDefinition
    {
        public string DataId { get; }
        public IReadOnlyList<string> Kinds { get; }
        public EquipmentTriggerDefinition(string dataId, IReadOnlyList<string> kinds)
        { DataId = dataId; Kinds = Array.AsReadOnly(kinds.ToArray()); }
    }

    // Registration-time native cache. Conditions are tested when registering the
    // relic, not each time a unit queries its fire count. Preserve that history.
    public sealed class TriggerCountState
    {
        public IReadOnlyList<TriggerCountModifier> Modifiers { get; }
        public IReadOnlyList<string> EnemyAllowedKinds { get; }
        public IReadOnlyList<string> ExcludedCardTriggers { get; }
        // Only the original AttachEquipment parameter's character triggers grant
        // enemy eligibility; permanent/temporary card upgrades do not grant it.
        public IReadOnlyList<EquipmentTriggerDefinition> EquipmentDefinitions { get; }
        public TriggerCountState(IReadOnlyList<TriggerCountModifier> modifiers, IReadOnlyList<string> enemyAllowedKinds,
            IReadOnlyList<string> excludedCardTriggers, IReadOnlyList<EquipmentTriggerDefinition> equipmentDefinitions)
        {
            Modifiers = Array.AsReadOnly(modifiers.ToArray());
            EnemyAllowedKinds = Array.AsReadOnly(enemyAllowedKinds.ToArray());
            ExcludedCardTriggers = Array.AsReadOnly(excludedCardTriggers.ToArray());
            EquipmentDefinitions = Array.AsReadOnly(equipmentDefinitions.ToArray());
        }
    }

    internal static class TriggerCountModel
    {
        internal static string? Validate(TriggerCountState? state)
        {
            if (state == null) return null;
            if (state.Modifiers.Any(item => string.IsNullOrEmpty(item.Kind)) ||
                state.Modifiers.Select(item => item.Kind).Distinct(StringComparer.Ordinal).Count() != state.Modifiers.Count ||
                state.EnemyAllowedKinds.Any(string.IsNullOrEmpty) || state.EnemyAllowedKinds.Distinct().Count() != state.EnemyAllowedKinds.Count ||
                state.ExcludedCardTriggers.Any(string.IsNullOrEmpty) || state.ExcludedCardTriggers.Distinct().Count() != state.ExcludedCardTriggers.Count ||
                state.EquipmentDefinitions.Any(item => string.IsNullOrEmpty(item.DataId) || item.Kinds.Any(string.IsNullOrEmpty)) ||
                state.EquipmentDefinitions.Select(item => item.DataId).Distinct().Count() != state.EquipmentDefinitions.Count)
                return "Invalid captured trigger count cache.";
            return null;
        }

        internal static int Query(CombatContext? context, CombatUnit unit, CombatTrigger trigger, out string? error)
        {
            error = null;
            TriggerCountState? state = context?.TriggerCounts;
            if (state == null) return trigger.FireCount; // Historical captures.
            if (!trigger.NoCountModifiersAllowed.HasValue)
            { error = "Missing native trigger count admission flag."; return 0; }
            if (unit.Team == CombatTeam.Enemy)
            {
                if (!state.EnemyAllowedKinds.Contains(trigger.Kind)) return 1;
                if (unit.EquipmentCards == null)
                { error = "Missing attached equipment for enemy trigger count query."; return 0; }
                bool eligible = false;
                foreach (int id in unit.EquipmentCards)
                {
                    CardInstanceState? card = context!.FindCard(id);
                    EquipmentTriggerDefinition? definition = state.EquipmentDefinitions.FirstOrDefault(item => item.DataId == card?.DataId);
                    if (definition == null)
                    { error = "Missing original equipment trigger definition."; return 0; }
                    eligible |= definition.Kinds.Contains(trigger.Kind);
                }
                if (!eligible) return 1;
            }
            if (trigger.NoCountModifiersAllowed == true) return 1;
            // Room modifiers remain guarded by the existing room/upgrade capture
            // boundary. Signed and overflowing native counts can be non-positive.
            return unchecked(1 + (state.Modifiers.FirstOrDefault(item => item.Kind == trigger.Kind)?.Value ?? 0));
        }

        internal static CombatUnit Refresh(CombatContext? context, CombatUnit unit, out string? error)
        {
            error = null;
            if (context?.TriggerCounts == null) return unit;
            var triggers = unit.Triggers.ToArray();
            bool changed = false;
            for (int index = 0; index < triggers.Length; index++)
            {
                int count = Query(context, unit, triggers[index], out error);
                if (error != null) return unit;
                if (count == triggers[index].FireCount) continue;
                triggers[index] = triggers[index].WithFireCount(count); changed = true;
            }
            return changed ? unit.WithTriggers(triggers) : unit;
        }
    }
}

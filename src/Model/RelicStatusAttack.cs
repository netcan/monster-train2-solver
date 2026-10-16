using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicStatusAttackModifier
    {
        public int EffectIndex { get; }
        public string StatusId { get; }
        public string Team { get; }
        public int Multiplier { get; }
        public RelicStatusAttackModifier(int effectIndex, string statusId, string team, int multiplier)
        { EffectIndex = effectIndex; StatusId = statusId; Team = team; Multiplier = multiplier; }
    }

    internal static class RelicStatusAttackModel
    {
        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type == "RelicEffectAdjustAttackByStatusEffect").Select(item => item.index).ToArray();
            if (indices.Length == 0) return relic.StatusAttackModifiers?.Count > 0 ? "Unexpected status attack definitions." : null;
            if (relic.StatusAttackModifiers == null || !indices.SequenceEqual(relic.StatusAttackModifiers.Select(item => item.EffectIndex)))
                return "Missing ordered native status attack definitions.";
            return relic.StatusAttackModifiers.Any(item => string.IsNullOrEmpty(item.StatusId) ||
                item.Team is not ("Monsters" or "Heroes") || !RoomCombatModel.KnowsStatus(item.StatusId))
                ? "Unmodeled native status attack filter." : null;
        }
        internal static int AdditionalAttack(IReadOnlyList<RelicStatusAttackModifier>? modifiers,
            CombatTeam team, IReadOnlyList<CombatStatus> statuses)
        {
            if (modifiers == null) return 0;
            int total = 0;
            foreach (var modifier in modifiers)
            {
                if (modifier.Team != (team == CombatTeam.Player ? "Monsters" : "Heroes")) continue;
                int stacks = statuses.FirstOrDefault(status => status.Id == modifier.StatusId)?.Stacks ?? 0;
                total = unchecked(total + unchecked(stacks * modifier.Multiplier));
            }
            return total;
        }
    }
}

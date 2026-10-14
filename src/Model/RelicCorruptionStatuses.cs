using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class RelicCorruptionStatus
    {
        public int EffectIndex { get; }
        public string TargetMode { get; }
        public bool TargetMonsters { get; }
        public bool TargetHeroes { get; }
        public bool OnlyDuringMonsterTurn { get; }
        public bool CanApplyInPreview { get; }
        public bool HasConditions { get; }
        public IReadOnlyList<CombatStatus> Statuses { get; }

        public RelicCorruptionStatus(int effectIndex, string targetMode, bool targetMonsters, bool targetHeroes,
            bool onlyDuringMonsterTurn, bool canApplyInPreview, bool hasConditions, IReadOnlyList<CombatStatus> statuses)
        {
            EffectIndex = effectIndex; TargetMode = targetMode; TargetMonsters = targetMonsters;
            TargetHeroes = targetHeroes; OnlyDuringMonsterTurn = onlyDuringMonsterTurn;
            CanApplyInPreview = canApplyInPreview; HasConditions = hasConditions;
            Statuses = Array.AsReadOnly(statuses.ToArray());
        }
    }

    internal static class RelicCorruptionStatusModel
    {
        internal static string? Validate(CombatRelicState relic)
        {
            int[] indices = relic.EffectTypes.Select((type, index) => (type, index))
                .Where(item => item.type == RelicModel.AddStatusOnCorruption).Select(item => item.index).ToArray();
            if (indices.Length == 0)
                return relic.CorruptionStatuses?.Count > 0 ? "Unexpected relic corruption-status definitions." : null;
            if (relic.CorruptionStatuses == null || !indices.SequenceEqual(relic.CorruptionStatuses.Select(rule => rule.EffectIndex)))
                return "Missing ordered native relic corruption-status definitions.";
            foreach (RelicCorruptionStatus rule in relic.CorruptionStatuses)
            {
                if (rule.TargetMode != "RandomInRoom" || !rule.TargetMonsters && !rule.TargetHeroes ||
                    rule.Statuses.Count == 0 || rule.HasConditions ||
                    rule.Statuses.Any(status => !RoomCombatModel.KnowsStatus(status.Id)))
                    return "Unmodeled relic corruption-status definition.";
            }
            return null;
        }

        internal static CardTargets Collect(RoomCombatState room, RelicCorruptionStatus rule)
        {
            RoomCombatState eligible = room.WithUnits(room.Units.Where(unit =>
                !(unit.Health == 1 && unit.Status("pyrebane") != null && unit.Status("lifelink") != null)).ToArray());
            return CardTargetModel.Collect(eligible, new CardActionEffect("AddStatus", rule.TargetMode, 0,
                rule.TargetHeroes, rule.TargetMonsters, rule.Statuses), Array.Empty<int>());
        }
    }
}

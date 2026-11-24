using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicAscensionHealModel
    {
        internal const string Effect = "RelicEffectHealEnemyOnAscend";
        internal const string DataId = "804934f0-cfa6-4c17-badf-8c0f918c9953";
        internal const string Boss = "SubtypesData_Boss";
        internal const string TreasureCollector = "SubtypesData_TreasureCollector";

        internal static bool IsOwner(CombatRelicState relic) => relic is
        {
            DataId: DataId, AssetKey: "Enemy_HealOnAscend", IsCovenant: false,
            DisallowedInPlacementPhase: false, EffectTypes: [Effect]
        };

        internal static string? Validate(CombatRelicState owner, RelicResourceEffect rule) =>
            IsOwner(owner) && rule.EffectIndex == 0 && rule.ParamInt == 150 && rule.Amount == 150 &&
            rule.RequiredMutatorId == null && rule.Conditions.Count == 0
                ? null : "Missing or changed original Ascend trial healing definition.";

        internal static bool Eligible(CombatUnit unit) =>
            !unit.Subtypes.Contains(Boss) && !unit.Subtypes.Contains(TreasureCollector);
    }
}

using System.Linq;

namespace MonsterTrain2Poju.Model
{
    internal static class RelicSkullCollectorsModel
    {
        internal const string OwnerId = "32d52c9d-cc1d-4f01-931a-8c232b687a35";
        internal const string OwnerKey = "SkullCollectors";
        internal const string Effect = "RelicEffectSubstituteTreasureUnitOnSpawn";
        internal const string StockTierOneId = "1ff637e2-d8a8-412f-afad-f557e45edaa7";
        internal const string ReplacementTierOneId = "d399bb13-2d74-42f3-a99d-f6db22afca8f";
        internal const string ReplacementTierOneKey = "TreasureBagLvl1Gold_SkullCollectors";
        internal const string TierOne = "SubtypesData_TreasureCollectorT1";
        internal const int HealthBonus = 30;
        internal const int AttackBonus = 5;

        internal static bool IsOriginal(CombatRelicState relic) => relic is
        {
            DataId: OwnerId, AssetKey: OwnerKey, IsCovenant: false, DisallowedInPlacementPhase: false,
            EffectTypes: [Effect, RelicModel.ModifyCharacterMaxHealth, RelicModel.ModifyCharacterMaxHealth,
                RelicModel.ModifyCharacterMaxHealth, RelicModel.ModifyCharacterAttackDamage,
                RelicModel.ModifyCharacterAttackDamage, RelicModel.ModifyCharacterAttackDamage]
        };

        internal static bool IsTierOne(CombatUnit unit) => unit.Team == CombatTeam.Enemy &&
            unit.AssetKey == ReplacementTierOneKey && unit.Subtypes.Contains(TierOne) && unit.Modifiers?.IsClone == false;

        internal static bool AllowsTreasure(CombatContext? context, EnemyDefinition definition)
        {
            bool active = context?.Relics?.Any(IsOriginal) == true;
            if (!active) return definition.TreasureOriginalId == null;
            return definition.TreasureOriginalId == StockTierOneId && IsTierOne(definition.Unit);
        }
    }
}

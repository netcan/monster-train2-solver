using System;
using System.Collections.Generic;
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

        internal static readonly IReadOnlyList<(int Distance, string Scenario, string OriginalId,
            string ReplacementId, string ReplacementKey, string Subtype, int Health, int Attack)> Tiers = Array.AsReadOnly(new[]
        {
            (1, "Level2BattleJunker", StockTierOneId, ReplacementTierOneId, ReplacementTierOneKey, TierOne, HealthBonus, AttackBonus),
            (3, "Level4BattleHunterAttackers", "0af268b6-ccf5-4707-b695-8ef2db08f9f4", "6ebc26b8-061b-4ffa-b7ba-b2c5c584f824",
                "TreasureBagLvl2Gold_SkullCollectors", "SubtypesData_TreasureCollectorT2", 60, 15),
            (6, "Level7BattleHordeSilence", "828ee3b1-ce27-43f2-9864-bd4126a17974", "f1cee43a-b773-4d8c-b5de-0df9a2ea4eb7",
                "TreasureBagLvl3Gold_SkullCollectors", "SubtypesData_TreasureCollectorT3", 90, 30)
        });

        internal static bool IsOriginal(CombatRelicState relic) => relic is
        {
            DataId: OwnerId, AssetKey: OwnerKey, IsCovenant: false, DisallowedInPlacementPhase: false,
            EffectTypes: [Effect, RelicModel.ModifyCharacterMaxHealth, RelicModel.ModifyCharacterMaxHealth,
                RelicModel.ModifyCharacterMaxHealth, RelicModel.ModifyCharacterAttackDamage,
                RelicModel.ModifyCharacterAttackDamage, RelicModel.ModifyCharacterAttackDamage]
        };

        internal static bool IsTierOne(CombatUnit unit) => MatchesTier(unit, 0);

        internal static int HealthBonusFor(CombatUnit unit, int effectIndex)
        {
            int tier = effectIndex - 1;
            return MatchesTier(unit, tier) ? Tiers[tier].Health : 0;
        }

        internal static int AttackBonusFor(CombatUnit unit, int effectIndex)
        {
            int tier = effectIndex - 4;
            return MatchesTier(unit, tier) ? Tiers[tier].Attack : 0;
        }

        private static bool MatchesTier(CombatUnit unit, int index) => index >= 0 && index < Tiers.Count &&
            unit.Team == CombatTeam.Enemy && unit.AssetKey == Tiers[index].ReplacementKey &&
            unit.Subtypes.Contains(Tiers[index].Subtype) && unit.Modifiers?.IsClone == false;

        internal static bool AllowsTreasure(CombatContext? context, EnemyDefinition definition)
        {
            bool active = context?.Relics?.Any(IsOriginal) == true;
            if (!active) return definition.TreasureOriginalId == null;
            for (int index = 0; index < Tiers.Count; index++)
                if (definition.TreasureOriginalId == Tiers[index].OriginalId && MatchesTier(definition.Unit, index))
                    return true;
            return false;
        }
    }
}

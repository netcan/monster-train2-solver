using System;
using System.Collections.Generic;
using System.Linq;

namespace MonsterTrain2Poju.Model
{
    public sealed class UnitModifiers
    {
        public int AttackDamage { get; }
        public int AttackDamageAdded { get; }
        public int DamageBuff { get; }
        public int RawSize { get; }
        public int EquipmentLimit { get; }
        public bool CanBeHealed { get; }
        public bool IsClone { get; }
        public bool SpawnerMatchesDefinition { get; }
        public IReadOnlyList<CardUpgradeModifier> Upgrades { get; }
        public IReadOnlyList<StatisticCount> HealthFromUpgrades { get; }
        public UnitModifiers(int attackDamage, int attackDamageAdded, int damageBuff, int rawSize, int equipmentLimit,
            bool canBeHealed, bool isClone, IReadOnlyList<CardUpgradeModifier> upgrades, IReadOnlyList<StatisticCount>? healthFromUpgrades = null,
            bool spawnerMatchesDefinition = true)
        {
            AttackDamage = attackDamage; AttackDamageAdded = attackDamageAdded; DamageBuff = damageBuff; RawSize = rawSize;
            EquipmentLimit = equipmentLimit; CanBeHealed = canBeHealed; IsClone = isClone; Upgrades = Array.AsReadOnly(upgrades.ToArray());
            HealthFromUpgrades = Array.AsReadOnly((healthFromUpgrades ?? Array.Empty<StatisticCount>()).ToArray());
            SpawnerMatchesDefinition = spawnerMatchesDefinition;
        }
    }

}

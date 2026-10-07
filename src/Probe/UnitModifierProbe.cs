using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class UnitModifierProbe
    {
        internal static UnitModifiers Capture(CharacterState unit)
        {
            object state = AccessTools.Property(typeof(CharacterState), "PrimaryStateInformation").GetValue(unit);
            int rawSize = (int)AccessTools.Property(state.GetType(), "Size").GetValue(state);
            var health = (Dictionary<string, int>)AccessTools.Field(state.GetType(), "maxHpFromUpgrades").GetValue(state);
            CardState? spawner = unit.GetSpawnerCard();
            return new UnitModifiers(unit.GetUnbuffedAttackDamage(), unit.GetAttackDamageAddedFromUpgrades(), unit.GetDamageBuff(),
                rawSize, unit.GetEquipmentLimit(), unit.GetCanBeHealed(), unit.GetIsClone(),
                unit.GetAppliedCardUpgrades().Select(upgrade => FullBattleTrace.Active?.CaptureAppliedUpgrade(upgrade) ?? CardModifierProbe.Upgrade(upgrade)).ToArray(), health.OrderBy(item => item.Key)
                    .Select(item => new StatisticCount(FullBattleTrace.Active?.UpgradeKey(item.Key) ?? item.Key, item.Value)).ToArray(),
                spawner?.GetSpawnCharacterData() == null || spawner.GetSpawnCharacterData() == unit.GetSourceCharacterData());
        }
        internal static UnitModifiers Definition(CharacterData data) => new UnitModifiers(data.GetAttackDamage(), 0, 0,
            data.GetSize(), data.GetEquipmentLimit(), data.GetCanBeHealed(), false, System.Array.Empty<CardUpgradeModifier>());
    }
}

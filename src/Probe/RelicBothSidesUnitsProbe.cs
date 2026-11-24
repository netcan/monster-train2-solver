using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicBothSidesUnitsProbe
    {
        private static readonly (string Class, string Team, string Trigger, int Amount, string CardType)[] Effects =
        {
            (nameof(RelicEffectModifyCharacterMaxHealth), "Heroes", "OnSpawn", 10, "Invalid"),
            (nameof(RelicEffectModifyCharacterMaxHealth), "Monsters", "OnSpawnNotFromCard", 10, "Invalid"),
            (nameof(RelicEffectModifyCharacterAttackDamage), "Heroes", "OnSpawn", 5, "Invalid"),
            (nameof(RelicEffectModifyCharacterAttackDamage), "Monsters", "OnSpawnNotFromCard", 5, "Invalid"),
            (nameof(RelicEffectModifyCardAttack), "Monsters", "OnSpawn", 5, "Monster"),
            (nameof(RelicEffectModifyCardHealth), "Monsters", "OnSpawn", 10, "Monster"),
            (nameof(RelicEffectTowerDamageDealtModified), "Monsters", "OnSpawn", 10, "Monster")
        };

        internal static bool Matches(RelicState owner)
        {
            var source = owner.GetSourceRelicData();
            var installed = owner.GetEffects();
            return owner.GetRelicDataID() == RelicBothSidesUnitsModel.DataId &&
                source is MutatorData && source.name == RelicBothSidesUnitsModel.AssetKey &&
                !owner.DisallowedInPlacementPhase && installed.Count == Effects.Length &&
                installed.Select((effect, index) => Matches(effect, index)).All(valid => valid);
        }

        private static bool Matches(IRelicEffect effect, int index)
        {
            var data = (RelicEffectData)AccessTools.Field(typeof(RelicEffectBase), "_srcRelicEffectData").GetValue(effect);
            return Effects[index] == (effect.GetType().Name, data.GetParamSourceTeam().ToString(),
                data.GetParamTrigger().ToString(), data.GetParamInt(), data.GetParamCardType().ToString()) &&
                data.GetTargetMode() == TargetMode.FrontInRoom && data.GetParamCharacterSubtype()?.IsNone == true &&
                data.GetEffectConditions().Count == 0 && data.GetParamInt2() == 0 && data.GetParamFloat() == 0 &&
                !data.GetParamBool() && !data.GetParamBool2() && !data.GetParamBool3() && !data.GetParamBool4() &&
                data.GetParamMutatorData() == null && data.GetParamCardUpgradeData() == null &&
                data.GetParamCardFilter() == null && string.IsNullOrEmpty(data.GetParamString()) &&
                (effect is not RelicEffectModifyCharacterAttackDamage ||
                    data.GetParamExcludeCharacterSubtypes().Length == 0);
        }
    }
}

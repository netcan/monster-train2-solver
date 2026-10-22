using System.Collections.Generic;

namespace MonsterTrain2Poju.Probe
{
    internal static class OuterBossSubstitutionProbe
    {
        // Mirror the native ordered CreateHeroState lookup without creating a
        // unit, changing assets or RNG. Only three exact original mappings are
        // calibrated; any other applied mapping remains an external guard.
        internal static CharacterData Resolve(CharacterData data, out bool supported)
        {
            supported = true;
            foreach (RelicEffectOnSpawnHeroSubstitution effect in AllGameManagers.Instance.GetRelicManager()
                .GetRelicEffects(new List<RelicEffectOnSpawnHeroSubstitution>()))
            {
                if (!effect.TryGetSubstitution(data, out CharacterData? replacement)) continue;
                string before = data.GetAssetKey();
                supported &= before is "Boss_Level8_AllyBuffer" or "Boss_Level8_Fragile" or "Boss_Level8_Corrupter" &&
                    replacement != null && replacement.GetAssetKey() == before + "_TitanTrial";
                if (replacement != null) data = replacement;
            }
            return data;
        }
    }
}

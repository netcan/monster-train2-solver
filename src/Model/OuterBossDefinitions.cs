namespace MonsterTrain2Poju.Model
{
    public static class OuterBossDefinitions
    {
        public static bool Supports(string assetKey) => assetKey is
            "Boss_Level8_AllyBuffer" or "Boss_Level8_Fragile" or "Boss_Level8_Corrupter";
    }
}

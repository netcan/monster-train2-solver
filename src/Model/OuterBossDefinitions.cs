namespace MonsterTrain2Poju.Model
{
    public static class OuterBossDefinitions
    {
        public static bool IsAngelDuo(string assetKey) => assetKey == "Boss_Level6_AngelDuo_Entropic_Variant2_IncantHealth";
        public static bool Supports(string assetKey) => assetKey is
            "Boss_Level8_AllyBuffer" or "Boss_Level8_Fragile" or "Boss_Level8_Corrupter" or
            "Boss_Level8_AllyBuffer_TitanTrial" or "Boss_Level8_Fragile_TitanTrial" or "Boss_Level8_Corrupter_TitanTrial" || IsAngelDuo(assetKey);
    }
}

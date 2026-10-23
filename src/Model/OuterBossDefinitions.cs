namespace MonsterTrain2Poju.Model
{
    public static class OuterBossDefinitions
    {
        public static string? AngelDuoCompanion(string assetKey) => assetKey switch
        {
            "Boss_Level6_AngelDuo_Entropic_Variant1_ResolveHealth" => "Boss_Level6_AngelDuo_Savage_Variant1_LoopAttack",
            "Boss_Level6_AngelDuo_Entropic_Variant2_IncantHealth" => "Boss_Level6_AngelDuo_Savage_Variant2_SlayAttack",
            "Boss_Level6_AngelDuo_Entropic_Variant3_HarvestHealth" => "Boss_Level6_AngelDuo_Savage_Variant3_RevengeAttack",
            _ => null
        };
        public static bool IsAngelDuo(string assetKey) => AngelDuoCompanion(assetKey) != null;
        public static bool IsAngelDuoInner(string assetKey) => assetKey is
            "Boss_Level6_AngelDuo_Savage_Variant1_LoopAttack" or "Boss_Level6_AngelDuo_Savage_Variant2_SlayAttack" or
            "Boss_Level6_AngelDuo_Savage_Variant3_RevengeAttack";
        public static bool IsAngelDuoActor(string assetKey) => IsAngelDuo(assetKey) || IsAngelDuoInner(assetKey);
        public static bool IsAngelDuoScenario(string? assetKey) => assetKey is
            "Level6AngelDuo_LoopAttackResolveHealth" or "Level6AngelDuo_RallyAttackIncantHealth" or
            "Level6AngelDuo_RevengeAttackHarvestHealth";
        public static bool IsCael(string assetKey) => assetKey is "Boss_Level6_Cael_AtkHP" or
            "Boss_Level6_Cael_DamageShield" or "Boss_Level6_Cael_Lifesteal";
        public static bool Supports(string assetKey) => assetKey is
            "Boss_Level8_AllyBuffer" or "Boss_Level8_Fragile" or "Boss_Level8_Corrupter" or
            "Boss_Level8_AllyBuffer_TitanTrial" or "Boss_Level8_Fragile_TitanTrial" or "Boss_Level8_Corrupter_TitanTrial" || IsAngelDuo(assetKey) || IsCael(assetKey);
    }
}

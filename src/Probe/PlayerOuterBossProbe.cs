using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class PlayerOuterBossProbe
    {
        // Only calibrated definitions enter player capture. Exact payload
        // validation still applies; bounded advice need not predict a victory.
        internal static bool Enabled
        {
            get
            {
                bool playerCapture = AdvisorRuntime.PlayerSession;
#if DEVELOPMENT_PROBE
                // Keep the independent native comparison in the same capture
                // mode as the separately loaded player assembly.
                playerCapture |= PlayerAdvisorProbe.Enabled;
#endif
                if (!playerCapture) return false;
                HeroManager? heroes = AllGameManagers.Instance?.GetHeroManager();
                if (heroes == null) return false;
                var pattern = (SpawnPatternData?)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(heroes);
                if (pattern == null || pattern.GetBossType() == SpawnPatternData.BossType.None) return false;
                var harder = (bool)AccessTools.Field(typeof(HeroManager), "harderOuterTrainBossEnabled").GetValue(heroes);
                CharacterData? data = pattern.GetOuterTrainBossCharacter(harder);
                if (data == null) return false;
                data = OuterBossSubstitutionProbe.Resolve(data, out bool substitutionsSupported);
                if (!substitutionsSupported) return false;
                return (data.GetAssetKey() is "Boss_Level8_AllyBuffer" or "Boss_Level8_Fragile" or "Boss_Level8_Corrupter" or
                    "Boss_Level8_AllyBuffer_TitanTrial" or "Boss_Level8_Fragile_TitanTrial" or "Boss_Level8_Corrupter_TitanTrial" ||
                    data.GetAssetKey() is "Boss_Level6_AngelDuo_Entropic_Variant1_ResolveHealth" or
                        "Boss_Level6_AngelDuo_Entropic_Variant2_IncantHealth") &&
                    OuterBossPhaseProbe.Supports(data);
            }
        }
    }
}

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
                return data?.GetAssetKey() is "Boss_Level8_AllyBuffer" or "Boss_Level8_Fragile" &&
                    OuterBossPhaseProbe.Supports(data);
            }
        }
    }
}

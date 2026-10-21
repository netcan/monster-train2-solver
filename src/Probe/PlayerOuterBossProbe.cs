using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class PlayerOuterBossProbe
    {
        // Only this original boss has calibrated creation, casting,
        // relentless and Pyre routing models. Other definitions keep the
        // existing unsupported marker; model validators inspect its payloads.
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
                return pattern.GetOuterTrainBossCharacter(harder)?.GetAssetKey() == "Boss_Level8_AllyBuffer";
            }
        }
    }
}

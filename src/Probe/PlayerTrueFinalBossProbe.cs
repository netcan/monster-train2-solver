using System.Linq;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class PlayerTrueFinalBossProbe
    {
        internal static bool Enabled
        {
            get
            {
                bool playerCapture = AdvisorRuntime.PlayerSession;
#if DEVELOPMENT_PROBE
                playerCapture |= PlayerAdvisorProbe.Enabled;
#endif
                if (!playerCapture) return false;
                AllGameManagers? managers = AllGameManagers.Instance;
                HeroManager? heroes = managers?.GetHeroManager();
                if (heroes == null || managers!.GetSaveManager().GetCovenantsForSpawnPattern().Count != 0) return false;
                var pattern = (SpawnPatternData?)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(heroes);
                if (pattern == null || pattern.GetBossType() != SpawnPatternData.BossType.TrueFinalBoss ||
                    managers.GetSaveManager().GetCurrentScenarioData()?.name != "Level9TrueFinalBoss_V2") return false;
                string[] assets = pattern.GetTrueFinalBosses().SelectMany(group => group.characters)
                    .Select(data => data.GetAssetKey()).OrderBy(key => key).ToArray();
                return assets.SequenceEqual(new[] { "Boss_Level9_Dominion_V2", "Boss_Level9_Entropy_Bot_V2",
                    "Boss_Level9_Entropy_Mid_V2", "Boss_Level9_Entropy_Top_V2", "Boss_Level9_Savagery_V2" });
            }
        }
    }
}

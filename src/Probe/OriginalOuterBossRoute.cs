using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    // Explicit original scenario selection in a disposable development profile.
    // This changes the route, never the selected scenario or character assets.
    internal static class OriginalOuterBossRoute
    {
        internal static object? Control { get; private set; }
        internal static void Select(SaveManager save, int distance, ManualLogSource log)
        {
            string? variant = Environment.GetEnvironmentVariable("MT2_PROBE_ORIGINAL_OUTER_BOSS");
            if (string.IsNullOrEmpty(variant)) return;
            if (variant != "Corrupter" || distance != 7 || Control != null)
                throw new InvalidOperationException("Original outer-boss control requires one explicit Ring8 Corrupter selection.");
            string boss = "Boss_Level8_" + variant;
            var scenarios = new List<ScenarioData>();
            save.GetRunData().GetDistanceData(distance).GetAllScenarioDatas(scenarios);
            ScenarioData selected = scenarios.Single(scenario =>
                scenario.GetSpawnPattern().GetOuterTrainBossCharacter(false)?.GetAssetKey() == boss);
            NodeState node = save.GetRunState().GetNodeStateAtDistance(distance);
            int previous = node.scenarioIndex, index = scenarios.IndexOf(selected);
            string? generatedBefore = node.bossId;
            string? generatedAfter = selected.GetBossAtIndex(0)?.GetID();
            object Rng() => Enum.GetValues(typeof(RngId)).Cast<RngId>()
                .Select(id => new { Id = id.ToString(), Words = RngCalibration.Words(RandomManager.GetState(id)) }).ToArray();
            object before = Rng();
            node.scenarioIndex = index;
            node.bossId = generatedAfter;
            object after = Rng();
            if (!JToken.DeepEquals(JToken.FromObject(before), JToken.FromObject(after)))
                throw new InvalidOperationException("Original scenario selection consumed native RNG.");
            Control = new { Kind = "SelectOriginalOuterBossScenario", Distance = distance, Boss = boss,
                PreviousIndex = previous, PreviousScenario = scenarios[previous].name,
                SelectedIndex = index, SelectedScenario = selected.name,
                GeneratedWaveBossBefore = generatedBefore, GeneratedWaveBossAfter = generatedAfter,
                RngBefore = before, RngAfter = after, ScenarioAssetsUnchanged = true };
            log.LogInfo("ORIGINAL-OUTER-BOSS-ROUTE boss=" + boss + " scenario=" + selected.name + " previous=" + scenarios[previous].name);
        }
    }
}

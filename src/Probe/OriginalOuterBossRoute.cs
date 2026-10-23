using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    // Explicit original scenario selection in a disposable development profile.
    // This changes the route, never the selected scenario or character assets.
    internal static class OriginalOuterBossRoute
    {
        internal static object? Control { get; private set; }
        internal static object? TrialIntroControl { get; private set; }
        private static Action? resumeSelection;
        internal static void ArmResume(SaveManager save, int distance, ManualLogSource log)
        {
            if (resumeSelection != null || Control != null) throw new InvalidOperationException("Original resume route was armed twice.");
            resumeSelection = () => Select(save, distance, log);
        }
        [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.StartLoad))]
        private static class ResumeRoutePatch
        {
            private static void Prefix(RunType runType, ref Action<bool>? doneCallback)
            {
                if (resumeSelection == null || runType != RunType.Class) return;
                Action select = resumeSelection;
                resumeSelection = null;
                Action<bool>? native = doneCallback;
                doneCallback = success =>
                {
                    if (success) select();
                    native?.Invoke(success);
                };
            }
        }
        private static object Rng() => Enum.GetValues(typeof(RngId)).Cast<RngId>()
            .Select(id => new { Id = id.ToString(), Words = RngCalibration.Words(RandomManager.GetState(id)) }).ToArray();
        internal static void PrepareIntro(SaveManager save, BattleIntroScreen intro, ManualLogSource log)
        {
            if (Environment.GetEnvironmentVariable("MT2_PROBE_ORIGINAL_OUTER_BOSS_TRIAL") != "1" || TrialIntroControl != null) return;
            if (Control == null) throw new InvalidOperationException("Trial intro lacks an explicit original route selection.");
            bool eligibleBefore = (bool)AccessTools.Field(typeof(BattleIntroScreen), "showTitanTrial").GetValue(intro);
            bool enabledBefore = save.IsTitanTrialEnabled();
            bool uiBefore = (bool)AccessTools.Field(typeof(BattleIntroScreen), "titanTrialEnabled").GetValue(intro);
            object before = Rng();
            // A fresh map-jump run has not earned the trial. Record this explicit
            // isolated eligibility control, then use the original UI toggle.
            AccessTools.Field(typeof(BattleIntroScreen), "showTitanTrial").SetValue(intro, true);
            if (!uiBefore) AccessTools.Method(typeof(BattleIntroScreen), "HandleTrialToggleButton").Invoke(intro, Array.Empty<object>());
            bool uiAfter = (bool)AccessTools.Field(typeof(BattleIntroScreen), "titanTrialEnabled").GetValue(intro);
            object after = Rng();
            string[] changed = JArray.FromObject(before).Zip(JArray.FromObject(after), (a, b) =>
                JToken.DeepEquals(a, b) ? "" : a["Id"]!.ToString()).Where(id => id.Length > 0).ToArray();
            // Native UI refresh uses NonDeterministic for presentation. Every
            // gameplay RNG stream must remain exactly unchanged.
            if (!uiAfter || !save.IsTitanTrialEnabled() || changed.Any(id => id != "NonDeterministic"))
                throw new InvalidOperationException("Original trial UI toggle failed or consumed native RNG: ui=" + uiBefore + "->" + uiAfter +
                    " save=" + enabledBefore + "->" + save.IsTitanTrialEnabled() + " fight=" + intro.GetFightButton().interactable +
                    " showBossSin=" + AccessTools.Field(typeof(BattleIntroScreen), "showBossSin").GetValue(intro) +
                    " rngChanged=" + string.Join(",", changed));
            TrialIntroControl = new { Kind = "EnableOriginalTitanTrialAtIntro", EligibleBefore = eligibleBefore,
                EnabledBefore = enabledBefore, UiBefore = uiBefore, UiAfter = uiAfter, EnabledAfter = save.IsTitanTrialEnabled(),
                RngBefore = before, RngAfter = after, ChangedRngIds = changed };
            log.LogInfo("ORIGINAL-OUTER-BOSS-TRIAL-INTRO original toggle enabled; isolated eligibility control=" + !eligibleBefore);
        }
        internal static void Select(SaveManager save, int distance, ManualLogSource log)
        {
            string? duo = Environment.GetEnvironmentVariable("MT2_PROBE_ORIGINAL_ANGEL_DUO");
            string? variant = Environment.GetEnvironmentVariable("MT2_PROBE_ORIGINAL_OUTER_BOSS");
            string? early = Environment.GetEnvironmentVariable("MT2_PROBE_ORIGINAL_EARLY_OUTER_BOSS");
            if (!string.IsNullOrEmpty(early))
            {
                if (!string.IsNullOrEmpty(duo) || !string.IsNullOrEmpty(variant))
                    throw new InvalidOperationException("Original boss route controls cannot be combined.");
                SelectEarly(save, distance, early, log);
                return;
            }
            if (!string.IsNullOrEmpty(duo))
            {
                if (!string.IsNullOrEmpty(variant)) throw new InvalidOperationException("Original boss route controls cannot be combined.");
                SelectAngelDuo(save, distance, duo, log);
                return;
            }
            if (string.IsNullOrEmpty(variant)) return;
            if (variant is not ("Corrupter" or "Fragile" or "AllyBuffer") || distance != 7 || Control != null)
                throw new InvalidOperationException("Original outer-boss control requires one explicit Ring8 selection.");
            bool trial = Environment.GetEnvironmentVariable("MT2_PROBE_ORIGINAL_OUTER_BOSS_TRIAL") == "1";
            string boss = "Boss_Level8_" + variant + (trial ? "_TitanTrial" : "");
            var scenarios = new List<ScenarioData>();
            save.GetRunData().GetDistanceData(distance).GetAllScenarioDatas(scenarios);
            ScenarioData selected = scenarios.Single(scenario =>
                scenario.GetSpawnPattern().GetOuterTrainBossCharacter(false)?.GetAssetKey() == "Boss_Level8_" + variant);
            NodeState node = save.GetRunState().GetNodeStateAtDistance(distance);
            int previous = node.scenarioIndex, index = scenarios.IndexOf(selected);
            string? generatedBefore = node.bossId;
            string? generatedAfter = selected.GetBossAtIndex(0)?.GetID();
            object before = Rng();
            object? trialControl = null;
            if (trial)
            {
                RelicData sin = selected.GetTitanTrialSin();
                if (sin == null) throw new InvalidOperationException("The original scenario has no Titan Trial sin.");
                trialControl = new { EnabledBefore = save.IsTitanTrialEnabled(), RequestedEnabled = true,
                    SinDataId = sin.GetID(), SinAssetKey = sin.name,
                    EffectTypes = sin.GetEffects().Select(effect => effect.GetEffectClassName()).ToArray(),
                    Substitutions = sin.GetEffects().Where(effect => effect.GetParamCharacterSubstitution() != null)
                        .SelectMany(effect => effect.GetParamCharacterSubstitution().Entries)
                        .Select(entry => new { Before = entry.UnitBefore.GetAssetKey(), After = entry.UnitAfter.GetAssetKey() }).ToArray() };
                log.LogInfo("ORIGINAL-OUTER-BOSS-TRIAL sin=" + sin.name + " boss=" + boss);
            }
            node.scenarioIndex = index;
            node.bossId = generatedAfter;
            object after = Rng();
            if (!JToken.DeepEquals(JToken.FromObject(before), JToken.FromObject(after)))
                throw new InvalidOperationException("Original scenario selection consumed native RNG.");
            Control = new { Kind = "SelectOriginalOuterBossScenario", Distance = distance, Boss = boss,
                PreviousIndex = previous, PreviousScenario = scenarios[previous].name,
                SelectedIndex = index, SelectedScenario = selected.name,
                GeneratedWaveBossBefore = generatedBefore, GeneratedWaveBossAfter = generatedAfter,
                RngBefore = before, RngAfter = after, ScenarioAssetsUnchanged = true,
                TitanTrial = trialControl };
            log.LogInfo("ORIGINAL-OUTER-BOSS-ROUTE boss=" + boss + " scenario=" + selected.name + " previous=" + scenarios[previous].name);
        }

        private static void SelectEarly(SaveManager save, int distance, string variant, ManualLogSource log)
        {
            string boss = variant switch
            {
                "ArkionPush" => "Boss_Level3_Arkion_PushAttack",
                "ArkionPyre" => "Boss_Level3_Arkion_DamagePyre",
                "ArkionRage" => "Boss_Level3_Arkion_SiphonRage",
                "FleshweaverCorruption" => "Boss_Level3_Fleshweaver_Corruption",
                "FleshweaverMass" => "Boss_Level3_Fleshweaver_MoreMass",
                "FleshweaverSpikes" => "Boss_Level3_Fleshweaver_Spike",
                "CaelAtkHP" => "Boss_Level6_Cael_AtkHP",
                "CaelDamageShield" => "Boss_Level6_Cael_DamageShield",
                "CaelLifesteal" => "Boss_Level6_Cael_Lifesteal",
                _ => throw new InvalidOperationException("Unknown original early outer-boss variant.")
            };
            int expected = boss.StartsWith("Boss_Level6_", StringComparison.Ordinal) ? 5 : 2;
            if (distance != expected || save.GetCurrentDistance() != expected || Control != null ||
                save.GetGameSequence() != SaveData.GameSequence.BattleIntro)
                throw new InvalidOperationException("Original early-boss selection requires its progressed native BattleIntro checkpoint.");
            var scenarios = new List<ScenarioData>();
            save.GetRunData().GetDistanceData(distance).GetAllScenarioDatas(scenarios);
            ScenarioData selected = scenarios.Single(scenario => scenario.GetOuterTrainBoss(false)?.GetAssetKey() == boss);
            NodeState node = save.GetRunState().GetNodeStateAtDistance(distance);
            int previous = node.scenarioIndex, index = scenarios.IndexOf(selected);
            string? generatedBefore = node.bossId, generatedAfter = selected.GetBossAtIndex(0)?.GetID();
            object before = Rng();
            node.scenarioIndex = index;
            node.bossId = generatedAfter;
            object after = Rng();
            if (!JToken.DeepEquals(JToken.FromObject(before), JToken.FromObject(after)))
                throw new InvalidOperationException("Original early-boss scenario selection consumed native RNG.");
            Control = new { Kind = "SelectOriginalEarlyOuterBossScenario", Distance = distance, Boss = boss,
                PreviousIndex = previous, PreviousScenario = scenarios[previous].name,
                SelectedIndex = index, SelectedScenario = selected.name,
                GeneratedWaveBossBefore = generatedBefore, GeneratedWaveBossAfter = generatedAfter,
                RngBefore = before, RngAfter = after, ScenarioAssetsUnchanged = true };
            log.LogInfo("ORIGINAL-EARLY-OUTER-BOSS-ROUTE boss=" + boss + " scenario=" + selected.name);
        }

        private static void SelectAngelDuo(SaveManager save, int distance, string variant, ManualLogSource log)
        {
            string boss = variant switch
            {
                "LoopResolve" => "Boss_Level6_AngelDuo_Entropic_Variant1_ResolveHealth",
                "SlayIncant" => "Boss_Level6_AngelDuo_Entropic_Variant2_IncantHealth",
                "RevengeHarvest" => "Boss_Level6_AngelDuo_Entropic_Variant3_HarvestHealth",
                _ => throw new InvalidOperationException("Unknown original Angel Duo variant.")
            };
            if (distance != 5 || Control != null || save.GetCurrentDistance() != 5 ||
                save.GetGameSequence() != SaveData.GameSequence.BattleIntro)
                throw new InvalidOperationException("Original Angel Duo selection requires a Ring6 checkpoint after five native wins.");
            var scenarios = new List<ScenarioData>();
            save.GetRunData().GetDistanceData(distance).GetAllScenarioDatas(scenarios);
            ScenarioData selected = scenarios.Single(scenario => scenario.GetOuterTrainBoss(false)?.GetAssetKey() == boss);
            NodeState node = save.GetRunState().GetNodeStateAtDistance(distance);
            int previous = node.scenarioIndex, index = scenarios.IndexOf(selected);
            string? generatedBefore = node.bossId, generatedAfter = selected.GetBossAtIndex(0)?.GetID();
            object before = Rng();
            node.scenarioIndex = index;
            node.bossId = generatedAfter;
            object after = Rng();
            if (!JToken.DeepEquals(JToken.FromObject(before), JToken.FromObject(after)))
                throw new InvalidOperationException("Original Angel Duo selection consumed native RNG.");
            Control = new { Kind = "SelectOriginalAngelDuoScenario", Distance = distance, Boss = boss,
                PreviousIndex = previous, PreviousScenario = scenarios[previous].name,
                SelectedIndex = index, SelectedScenario = selected.name,
                GeneratedWaveBossBefore = generatedBefore, GeneratedWaveBossAfter = generatedAfter,
                RngBefore = before, RngAfter = after, ScenarioAssetsUnchanged = true };
            log.LogInfo("ORIGINAL-ANGEL-DUO-ROUTE boss=" + boss + " scenario=" + selected.name + " previous=" + scenarios[previous].name);
        }
    }
}

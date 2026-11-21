using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    // Explicit original scenario selection in a disposable development profile.
    // This changes the route, never the selected scenario or character assets.
    internal static class OriginalOuterBossRoute
    {
        internal static object? Control { get; private set; }
        internal static object? TrialIntroControl { get; private set; }
#if DEVELOPMENT_PROBE
        internal static bool EarlyTrialSubstitution => Environment.GetEnvironmentVariable("MT2_PROBE_ORIGINAL_OUTER_BOSS_TRIAL") == "1" &&
            EarlyTrialVariant() != null;
        internal static bool EarlyTrialFullBattle => EarlyTrialSubstitution &&
            Environment.GetEnvironmentVariable("MT2_PROBE_ORIGINAL_EARLY_TRIAL_FULL_BATTLE") == "1";
        internal static bool CaelShieldTrialDiagnostic => EarlyTrialSubstitution &&
            Environment.GetEnvironmentVariable("MT2_PROBE_ORIGINAL_EARLY_OUTER_BOSS") == "CaelDamageShield";
        private static (string Scenario, string Owner, string OwnerId, string Source, string Replacement)? EarlyTrialVariant() =>
            Environment.GetEnvironmentVariable("MT2_PROBE_ORIGINAL_EARLY_OUTER_BOSS") switch
            {
                "FleshweaverCorruption" => ("Level3ImmortalMass_Corruption", "Enemy_Boss_Level3_ImmortalMass_Corruption_TitanTrial",
                    "52dc060c-bd51-4b7b-865a-6d70f9806e53", "Boss_Level3_MiniFleshweaver_Corruption", "Boss_Level3_MiniFleshweaver_Corruption_TitanTrial"),
                "FleshweaverMass" => ("Level3ImmortalMass_MoreMass", "Enemy_Boss_Level3_ImmortalMass_MoreMass_TitanTrial",
                    "28c0e907-d882-4052-93f4-0a07d88496af", "Boss_Level3_MiniFleshweaver_MoreMass", "Boss_Level3_MiniFleshweaver_MoreMass_TitanTrial"),
                "FleshweaverSpikes" => (OuterBossSubstitutionProbe.FleshweaverSpikesTrialScenario, OuterBossSubstitutionProbe.FleshweaverSpikesTrialOwner,
                    OuterBossSubstitutionProbe.FleshweaverSpikesTrialOwnerId, OuterBossSubstitutionProbe.FleshweaverSpikesMini,
                    OuterBossSubstitutionProbe.FleshweaverSpikesTrialMini),
                "CaelDamageShield" => ("Level6BattleEaterDamageShield", "Enemy_SpawnHeroEaterFeedDamageShield_TitanTrial",
                    "12f05d0b-df25-4cad-b7f3-9c52eac46e04", "Boss_Level6_Cael_DamageShield", "Boss_Level6_Cael_DamageShield_TitanTrial"),
                _ => null
            };
        private static RelicData? earlyTrialOwner;
        private static IReadOnlyList<CharacterSubstitution.Entry>? earlyTrialEntries;
        private static CharacterState? substitutedUnit;
        private static CharacterData? requestedSubstitution;
        private static CharacterData? callbackSubstitution;
        private static bool earlyTrialCaptured;
#endif
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
#if DEVELOPMENT_PROBE
            if (EarlyTrialSubstitution)
            {
                var trial = EarlyTrialVariant()!.Value;
                RelicData sin = selected.GetTitanTrialSin();
                if (selected.name != trial.Scenario || sin == null || sin.name != trial.Owner || sin.GetID() != trial.OwnerId ||
                    CaelShieldTrialDiagnostic && save.GetAscensionLevel() != 0)
                    throw new InvalidOperationException("Expected the installed original Titan Trial owner and checkpoint.");
                var substitutions = sin.GetEffects().Single(effect => effect.GetEffectClassName() == "RelicEffectOnSpawnHeroSubstitution")
                    .GetParamCharacterSubstitution();
                if (substitutions == null || substitutions.Entries.Count == 0)
                    throw new InvalidOperationException("Expected the installed trial's original substitution entries.");
                earlyTrialOwner = sin;
                earlyTrialEntries = substitutions.Entries;
                log.LogInfo("ORIGINAL-EARLY-TRIAL-INPUT owner=" + sin.name + " entries=" + string.Join(",", earlyTrialEntries.Select(entry =>
                    entry.UnitBefore.GetAssetKey() + "->" + entry.UnitAfter.GetAssetKey())));
            }
#endif
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

#if DEVELOPMENT_PROBE
        internal static bool CaptureEarlyTrialBirth(AllGameManagers managers, ManualLogSource log)
        {
            if (earlyTrialCaptured) return true;
            CharacterState? unit = substitutedUnit;
            if (unit == null) return false;
            var trial = EarlyTrialVariant()!.Value;
            SaveManager save = managers.GetSaveManager();
            CharacterData source = unit.GetOriginalCharacterData();
            CharacterData replacement = unit.GetSourceCharacterData();
            if (earlyTrialOwner == null || earlyTrialEntries == null || source == null || requestedSubstitution != source || callbackSubstitution != replacement ||
                TrialIntroControl == null || save.InTestScenario || !save.IsTitanTrialEnabled() || save.GetCurrentScenarioData().name != trial.Scenario ||
                source.GetAssetKey() != trial.Source || replacement.GetAssetKey() != trial.Replacement ||
                !earlyTrialEntries.Any(entry => entry.UnitBefore.GetID() == source.GetID() && entry.UnitAfter == replacement))
                throw new InvalidOperationException("The installed trial did not create an authored substitution.");
            var active = managers.GetRelicManager().GetRelicEffects(new List<RelicEffectOnSpawnHeroSubstitution>());
            if (active.Count != 1 || active[0].SourceRelicData != earlyTrialOwner ||
                !active[0].TryGetSubstitution(source, out CharacterData selected) || selected != replacement)
                throw new InvalidOperationException("Expected one active native substitution supplying the created boss.");

            if (!EarlyTrialFullBattle) OuterBossBirthProbe.Write(log);
            FullBattleTrace trace = FullBattleTrace.Active!;
            var projected = EnemySpawningProbe.Definition(source);
            using var archive = NativeFixtureCapture.Capture(new { Schema = 1, Boundary = "NativeOriginalEarlyTitanTrialSubstitution",
                GameVersion = UnityEngine.Application.version, GameModuleMvid = typeof(CardState).Module.ModuleVersionId,
                Scenario = save.GetCurrentScenarioData().name, OwnerId = earlyTrialOwner.GetID(), OwnerKey = earlyTrialOwner.name,
                AscensionLevel = save.GetAscensionLevel(), OwnerEffects = earlyTrialOwner.GetEffects().Select(effect => effect.GetEffectClassName()).ToArray(),
                Source = source.GetAssetKey(), SourceId = source.GetID(), Replacement = replacement.GetAssetKey(), ReplacementId = replacement.GetID(),
                OuterBoss = unit.IsOuterTrainBoss(), Turn = managers.GetCombatManager().GetTurnCount(), Room = unit.GetCurrentRoomIndex(),
                Projection = new { Key = projected.Unit.AssetKey, projected.ExternalInteractions }, trace.Unsupported, trace.Mismatches,
                BranchCardMasksEnabled = CardBranchMaskProbe.Enabled,
                IsolatePreviewRng = PreviewRngIsolation.Enabled, IsolateUiRng = UiRngIsolation.Enabled,
                IsolateTestQueries = UiRngIsolation.CaelShieldTrialTestQueries,
                MoonPhaseMasksCaptured = MoonPhaseCaptureProbe.CaptureMasks,
                Mappings = earlyTrialEntries.Select(entry => new { Source = entry.UnitBefore.GetAssetKey(), SourceId = entry.UnitBefore.GetID(),
                    Replacement = entry.UnitAfter.GetAssetKey(), ReplacementId = entry.UnitAfter.GetID() }).ToArray(),
                Unit = trace.CaptureUnit(unit), Intro = TrialIntroControl, TitanTrial = save.IsTitanTrialEnabled() });
            using var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "early-titan-trial-substitution.mt2f"));
            archive.Write(output);
            earlyTrialCaptured = true;
            log.LogInfo("ORIGINAL-EARLY-TRIAL-BIRTH matched " + source.GetAssetKey() + " -> " + replacement.GetAssetKey());
            return true;
        }

        [HarmonyPatch(typeof(HeroManager), "CreateHeroState")]
        private static class EarlyTrialCreationPatch
        {
            private static void Prefix(CharacterData heroData, ref Action<CharacterState, CharacterData> afterCharacterCreated)
            {
                if (substitutedUnit != null || earlyTrialEntries == null || AllGameManagers.Instance.GetSaveManager().PreviewMode ||
                    heroData.GetAssetKey() != EarlyTrialVariant()!.Value.Source ||
                    !earlyTrialEntries.Any(entry => entry.UnitBefore.GetID() == heroData.GetID())) return;
                Action<CharacterState, CharacterData>? original = afterCharacterCreated;
                afterCharacterCreated = (unit, data) =>
                {
                    if (substitutedUnit == null)
                    {
                        substitutedUnit = unit;
                        requestedSubstitution = heroData;
                        callbackSubstitution = data;
                    }
                    original?.Invoke(unit, data);
                };
            }
        }
#endif

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

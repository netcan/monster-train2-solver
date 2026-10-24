using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using ShinyShoe;
using ShinyShoe.Logging;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    [BepInPlugin(PluginId, "Monster Train 2 Isolation Probe", "0.1.0")]
    public sealed class ProbePlugin : BaseUnityPlugin
    {
        internal const string PluginId = "netcan.monstertrain2.isolationprobe";
        private const string EnvironmentVariable = "MT2_PROBE_DATA_DIR";
        private const string ScenarioVariable = "MT2_PROBE_SCENARIO";
        private static readonly string AllowedParent = Path.GetFullPath(@"E:\Workspace\Program\monster-train2-poju\.probe-runs")
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        private static ManualLogSource? log;
        private static string? isolatedRoot;
        private static bool patchInstalled;
        private static bool rootLogged;
        private static TwoTurnScenario? scenario;
        private static NativeReplayScenario? nativeReplayScenario;
        private static CampaignProgressScenario? campaignScenario;
        private static int lastTickFrame = -1;

        internal static void ContinueCampaignAfterVictory()
        {
            // The accepted combat snapshot has already been exported. Leave the
            // native StopCombat coroutine running so rewards and saves persist.
            FullBattleTrace.Active?.Detach();
            BattleAdvisorOverlay.Active?.Dispose();
            nativeReplayScenario = null;
            campaignScenario = new CampaignProgressScenario(log!, afterBattle: true);
        }

        private void Awake()
        {
            log = Logger;
            Application.quitting += () => BattleAdvisorOverlay.Active?.Dispose();
            EnchanterRemovalProbe.Configure(Logger);
            if (AppManager.PlatformServices != null)
            {
                Abort("AppManager already initialized before the save-root patch.");
            }

            isolatedRoot = ValidateRoot(Environment.GetEnvironmentVariable(EnvironmentVariable));
            ProbeGameSpeed.Configure(Logger);
            if (Environment.GetEnvironmentVariable(ScenarioVariable) != "advisor")
            {
                AudioListener.volume = 0f;
                Logger.LogInfo("Isolated probe audio muted.");
            }
            try
            {
                var isolation = new Harmony(PluginId);
                if (Environment.GetEnvironmentVariable(ScenarioVariable) == "player-ui")
                {
                    // Test the actual player entry point without supplying a
                    // probe update driver or any development observation hooks.
                    isolation.CreateClassProcessor(typeof(AppStartupPatch)).Patch();
                    isolation.CreateClassProcessor(typeof(SaveRootPatch)).Patch();
                    isolation.CreateClassProcessor(typeof(GameLogRootPatch)).Patch();
                }
                else isolation.PatchAll(typeof(ProbePlugin).Assembly);
                patchInstalled = true;
            }
            catch (Exception ex)
            {
                Abort("Could not install the save-root patch: " + ex);
            }

            Logger.LogInfo("Probe patches installed before AppManager startup; isolated root: " + isolatedRoot);
            string? scenarioName = Environment.GetEnvironmentVariable(ScenarioVariable);
            if (scenarioName == "two-turns")
            {
                scenario = new TwoTurnScenario(Logger);
                Logger.LogInfo("Two-turn smoke scenario armed.");
            }
            else if (scenarioName == "native-replay")
            {
                if (Environment.GetEnvironmentVariable("MT2_PROBE_FULL_BATTLE") == "1")
                    new FullBattleTrace(Logger);
                if (BattleAdvisorOverlay.Enabled) new BattleAdvisorOverlay(Logger);
                nativeReplayScenario = new NativeReplayScenario(Logger);
                Logger.LogInfo("Native replay depth scenario armed.");
            }
            else if (scenarioName == "advisor")
            {
                new FullBattleTrace(Logger);
                new BattleAdvisorOverlay(Logger);
            }
            else if (scenarioName == "campaign")
            {
                campaignScenario = new CampaignProgressScenario(Logger);
            }
            else if (scenarioName == "player-ui")
            {
                Logger.LogInfo("Player UI bootstrap: save/log isolation only; probe update driver disabled.");
            }
            else if (!string.IsNullOrEmpty(scenarioName))
            {
                Abort("Unknown " + ScenarioVariable + " value: " + scenarioName);
            }
        }

        private void LateUpdate()
        {
            if (patchInstalled && Environment.GetEnvironmentVariable(ScenarioVariable) != "advisor") AudioListener.volume = 0f;
        }

        private static string ValidateRoot(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || !Path.IsPathRooted(raw))
            {
                Abort(EnvironmentVariable + " must be an absolute path under the workspace .probe-runs directory.");
            }

            string root;
            try
            {
                root = Path.GetFullPath(raw!).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch (Exception ex)
            {
                Abort("Invalid " + EnvironmentVariable + ": " + ex.Message);
                throw;
            }

            string defaultRoot = Path.GetFullPath(Application.persistentDataPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!string.Equals(Path.GetDirectoryName(root), AllowedParent, StringComparison.OrdinalIgnoreCase) ||
                IsWithin(root, defaultRoot) || IsWithin(defaultRoot, root))
            {
                Abort(EnvironmentVariable + " must be a direct child of " + AllowedParent + " and be separate from " + defaultRoot);
            }

            return root;
        }

        private static bool IsWithin(string path, string parent)
        {
            return string.Equals(path, parent, StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(parent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        private static void Abort(string reason)
        {
            log?.LogFatal(reason + " Exiting before any default save access.");
            Environment.Exit(2);
            throw new InvalidOperationException(reason);
        }

        [HarmonyPatch(typeof(AppManager), nameof(AppManager.Awake))]
        private static class AppStartupPatch
        {
            private static void Prefix()
            {
                if (!patchInstalled || isolatedRoot == null)
                {
                    Abort("AppManager started without the isolation patch.");
                }
                log?.LogInfo("AppManager.Awake reached with isolated save-root patch active.");
            }
        }

        [HarmonyPatch(typeof(AppManager), nameof(AppManager.Update))]
        private static class AppUpdatePatch
        {
            private static void Postfix()
            {
                if (lastTickFrame == Time.frameCount)
                {
                    return;
                }
                lastTickFrame = Time.frameCount;
                scenario?.Tick();
                BattleAdvisorOverlay.Active?.Tick();
                nativeReplayScenario?.Tick();
                campaignScenario?.Tick();
            }
        }

        [HarmonyPatch(typeof(SaveDirStandalone), nameof(SaveDirStandalone.GetRootDirectory))]
        private static class SaveRootPatch
        {
            private static bool Prefix(ref string __result)
            {
                if (!patchInstalled || isolatedRoot == null)
                {
                    Abort("Save root requested without the isolation patch.");
                }
                __result = isolatedRoot!;
                if (!rootLogged)
                {
                    rootLogged = true;
                    log?.LogInfo("SaveDirStandalone.GetRootDirectory redirected to " + __result);
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(StandaloneProdLogSystem), nameof(StandaloneProdLogSystem.GetLogRootDir))]
        private static class GameLogRootPatch
        {
            private static bool Prefix(ref string __result)
            {
                if (!patchInstalled || isolatedRoot == null)
                {
                    Abort("Game log root requested without the isolation patch.");
                }
                __result = isolatedRoot!;
                return false;
            }
        }
    }
}

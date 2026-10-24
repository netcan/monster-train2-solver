using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using MonsterTrain2Poju.Probe;
using UnityEngine;

namespace MonsterTrain2Poju.Advisor
{
    [BepInPlugin(PluginId, "Monster Train 2 Poju Advisor", Version)]
    public sealed class AdvisorPlugin : BaseUnityPlugin
    {
        public const string PluginId = "netcan.monstertrain2.advisor";
        public const string Version = "0.2.6";
        private Harmony? harmony;
        private BattleAdvisorOverlay? overlay;
        private bool updateLogged;

        private void Awake()
        {
            if (!Config.Bind("General", "Enabled", true, "是否启用战局建议；更改后重启游戏。").Value) return;
            try
            {
                // The initial scene destroys the default visible BepInEx host
                // at frame zero. Match BepInEx's hidden-manager protection so
                // this component survives even with its default loader config.
                gameObject.hideFlags |= HideFlags.HideAndDontSave;
                var settings = new PlayerAdvisorSettings
                {
                    Horizon = Config.Bind("Search", "Horizon", 3,
                        new ConfigDescription("规划未来回合数。", new AcceptableValueRange<int>(2, 16))).Value,
                    TimeLimitMs = Config.Bind("Search", "TimeLimitMs", 3000,
                        new ConfigDescription("每次后台搜索的毫秒预算。", new AcceptableValueRange<int>(100, 30000))).Value,
                    DeploymentRoom = Config.Bind("Search", "PreferredFloor", 3,
                        new ConfigDescription("优先部署层（1 为底层）。", new AcceptableValueRange<int>(1, 3))).Value - 1,
                    TacticalSeed = Config.Bind("Search", "TacticalSeed", false, "启用额外战术候选，可能增加计算时间。").Value,
                    PauseWhenHidden = Config.Bind("General", "PauseWhenHidden", true, "隐藏面板时暂停计算，重新显示后按最新局面计算。").Value,
                    ToggleKey = Config.Bind("General", "ToggleKey", KeyCode.F8, "面板显示/隐藏按键；Mac 键盘可能需要 Fn+F8。").Value
                };
                AdvisorRuntime.PlayerSession = true;
                harmony = new Harmony(PluginId);
                // Deliberately select observation/invalidating hooks. PatchAll(assembly)
                // would activate development instrumentation and is not a player entry point.
                FullBattleTrace.InstallAdvisorPatches(harmony);
                BattleSpawnPointProbe.InstallAdvisorPatches(harmony);
                CardSourceTraitProbe.InstallAdvisorPatches(harmony);
                TypedDrawProbe.InstallAdvisorPatches(harmony);
                TriggerIdentityProbe.InstallAdvisorPatches(harmony);
                BattleAdvisorOverlay.InstallAdvisorPatches(harmony);
                overlay = new BattleAdvisorOverlay(Logger, settings);
                Logger.LogInfo("POJU-READY version=" + Version + " platform=" + Application.platform +
                    " game=" + Application.version + " module=" + typeof(CardState).Assembly.ManifestModule.ModuleVersionId +
                    " horizon=" + settings.Horizon + " budgetMs=" + settings.TimeLimitMs +
                    " hostFlags=" + gameObject.hideFlags);
            }
            catch (Exception error)
            {
                Logger.LogError("破局助手启动失败，可继续正常游玩。\n" + error);
                Cleanup();
            }
        }

        private void Update()
        {
            if (overlay == null) return;
            if (!updateLogged)
            {
                updateLogged = true;
                Logger.LogInfo("POJU-UPDATE pluginEnabled=" + enabled +
                    " hostActive=" + gameObject.activeInHierarchy);
            }
            overlay.Tick();
        }
        private void OnDisable() => Logger.LogInfo("POJU-HOST disabled frame=" + Time.frameCount);

        private void OnDestroy()
        {
            Logger.LogInfo("POJU-HOST destroyed frame=" + Time.frameCount);
            Cleanup();
        }

        private void Cleanup()
        {
            overlay?.Dispose();
            overlay = null;
            FullBattleTrace.Active?.Detach();
            harmony?.UnpatchSelf();
            harmony = null;
            AdvisorRuntime.PlayerSession = false;
        }
    }
}

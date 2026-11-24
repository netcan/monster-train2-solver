using System;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class DevotionProbe
    {
        private static readonly System.Reflection.PropertyInfo? devoteInProgress =
            typeof(CharacterState).Assembly.GetType("MonolithHelper")?.GetProperty("IsDevoteInProgress");
        private static readonly System.Reflection.FieldInfo? hudMonolith = AccessTools.Field(typeof(Hud), "monolithUI");
        private static readonly System.Reflection.MethodInfo? isPunishedRun = hudMonolith == null ? null :
            AccessTools.Method(hudMonolith.FieldType, "IsPunishedRun", new[] { typeof(SaveManager) });
        private static readonly System.Reflection.MethodInfo? getDevoteButton = hudMonolith == null ? null :
            AccessTools.Method(hudMonolith.FieldType, "GetDevoteButton");
        internal static bool IsDevoteInProgress => devoteInProgress?.GetValue(null) is true;
        internal static bool IsPlayerDevoteOffered(AllGameManagers managers)
        {
            ScreenManager screens = managers.GetScreenManager();
            if (isPunishedRun == null || screens?.GetScreen(ScreenName.Hud) is not Hud hud ||
                hudMonolith?.GetValue(hud) is not UnityEngine.Object monolith || monolith == null)
                return false;
            // Card rules are cached for the battle. Read the authored clan gate
            // independently of a temporary deck or other screen over the HUD.
            return getDevoteButton?.Invoke(monolith, null) is UnityEngine.Object button && button != null &&
                isPunishedRun.Invoke(monolith, new object[] { managers.GetSaveManager() }) is true;
        }
        internal static int Read(object owner, string method) => (int)AccessTools.Method(owner.GetType(), method).Invoke(owner, null);
        internal static int? Requirement(CardEffectData effect, bool maximum = false) => ConditionalActivationProbe.HasVanguard
            ? Read(effect, maximum ? "GetDevotionRequirementMax" : "GetDevotionRequirement") : (int?)null;
        internal static DevotionState? Capture(AllGameManagers managers)
        {
            if (!ConditionalActivationProbe.HasVanguard) return null;
            SaveManager save = managers.GetSaveManager(); BalanceData balance = managers.GetAllGameData().GetBalanceData();
            return new DevotionState(Read(save, "GetDevotionLevel"), Read(save, "GetDevotionPoints"),
                Read(balance, "GetMonolithDevotionFirstThreshold"), Read(balance, "GetMonolithDevotionThresholdIncreasePerLevel"),
                Read(balance, "GetMonolithDevotionPassiveStacksPerLevel"),
                (int)AccessTools.Method(typeof(CharacterState).Assembly.GetType("MonolithHelper"), "GetDevotionPassiveMultiplier")
                    .Invoke(null, new object[] { managers.GetCoreManagers() }));
        }
    }
}

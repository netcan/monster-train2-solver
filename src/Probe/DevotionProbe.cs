using System;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class DevotionProbe
    {
        private static readonly System.Reflection.PropertyInfo? devoteInProgress =
            typeof(CharacterState).Assembly.GetType("MonolithHelper")?.GetProperty("IsDevoteInProgress");
        internal static bool IsDevoteInProgress => devoteInProgress?.GetValue(null) is true;
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

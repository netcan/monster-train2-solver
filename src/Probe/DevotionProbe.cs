using System;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class DevotionProbe
    {
        internal static int Read(object owner, string method) => (int)AccessTools.Method(owner.GetType(), method).Invoke(owner, null);
        internal static DevotionState? Capture(AllGameManagers managers)
        {
            if (!ConditionalActivationProbe.HasVanguard) return null;
            SaveManager save = managers.GetSaveManager(); BalanceData balance = managers.GetAllGameData().GetBalanceData();
            return new DevotionState(Read(save, "GetDevotionLevel"), Read(save, "GetDevotionPoints"),
                Read(balance, "GetMonolithDevotionFirstThreshold"), Read(balance, "GetMonolithDevotionThresholdIncreasePerLevel"),
                Read(balance, "GetMonolithDevotionPassiveStacksPerLevel"));
        }
    }
}

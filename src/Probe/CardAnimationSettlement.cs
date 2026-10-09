using System;
using System.Collections;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class CardAnimationSettlement
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_SETTLE_CARD_ANIMATIONS") == "1";
        internal static int PreviewWaits, DecisionWaits;
        internal static bool Pending
        {
            get
            {
                if (!Enabled || AllGameManagers.Instance == null) return false;
                HandUI? hand = AllGameManagers.Instance.GetHandUI();
                return CardAnimator.AreAnyTempCardsAnimating() || hand != null &&
                    ((ICollection)AccessTools.Field(typeof(HandUI), "discardingCards").GetValue(hand)).Count > 0;
            }
        }
        [HarmonyPatch(typeof(CombatManager), "HasGameCalmedDown")]
        private static class CalmPatch
        {
            private static void Postfix(ref bool __result)
            {
                if (__result && Pending) { __result = false; PreviewWaits++; }
            }
        }
        internal static bool Ready()
        {
            if (!Pending) return true;
            DecisionWaits++; return false;
        }
    }
}

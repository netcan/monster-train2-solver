using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class CardAnimationSettlement
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_SETTLE_CARD_ANIMATIONS") == "1";
        internal static int PreviewWaits, DecisionWaits;
        internal static int ScheduledMovements;
        private static readonly MethodInfo isActive = AccessTools.Method(AccessTools.TypeByName("DG.Tweening.TweenExtensions"), "IsActive");
        private static readonly MethodInfo isComplete = AccessTools.Method(AccessTools.TypeByName("DG.Tweening.TweenExtensions"), "IsComplete");
        internal static int PendingMovements
        {
            get
            {
                if (!Enabled) return 0;
                var animator = (CardAnimator?)AccessTools.Field(typeof(CardAnimator), "instance").GetValue(null);
                if (animator == null) return 0;
                int pending = 0;
                foreach (object tween in (IEnumerable)AccessTools.Field(typeof(CardAnimator), "cardPlayedTweens").GetValue(animator))
                    if ((bool)isActive.Invoke(null, new[] { tween }) && !(bool)isComplete.Invoke(null, new[] { tween })) pending++;
                return pending;
            }
        }
        internal static bool Pending
        {
            get
            {
                if (!Enabled || AllGameManagers.Instance == null) return false;
                HandUI? hand = AllGameManagers.Instance.GetHandUI();
                return PendingMovements > 0 || CardAnimator.AreAnyTempCardsAnimating() || hand != null &&
                    ((ICollection)AccessTools.Field(typeof(HandUI), "discardingCards").GetValue(hand)).Count > 0;
            }
        }
        // The played-monster FX path schedules the completion tween without
        // dispatching AnimationStarted. Observe the original callback owner.
        [HarmonyPatch(typeof(CardAnimator), nameof(CardAnimator.PlayCardMovementAnimation))]
        private static class MovementPatch
        {
            private static void Postfix() { if (Enabled) ScheduledMovements++; }
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

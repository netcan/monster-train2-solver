using System.Collections;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    // Room selection can enqueue a support spell before RunCombat starts it.
    // IsRunningTriggerQueue alone does not cover that pending effects queue.
    internal static class NativeEffectSettlement
    {
        // This readiness check runs even on settled idle frames. Resolve typed
        // readers once so polling does not reflect or box the boolean each frame.
        private static readonly AccessTools.FieldRef<CombatManager, ICollection> effects =
            AccessTools.FieldRefAccess<CombatManager, ICollection>("effectsQueue");
        private static readonly AccessTools.FieldRef<CombatManager, bool> resolving =
            AccessTools.FieldRefAccess<CombatManager, bool>("cardEffectResolving");
        internal static int PendingEffectWaits, ResolvingEffectWaits, InputWaits;
        internal static object Evidence => new { PendingEffectWaits, ResolvingEffectWaits, InputWaits };
        internal static bool Ready(CombatManager combat)
        {
            if (effects(combat).Count > 0) { PendingEffectWaits++; return false; }
            if (resolving(combat)) { ResolvingEffectWaits++; return false; }
            if (!combat.AllowExternalInput) { InputWaits++; return false; }
            return true;
        }
    }
}

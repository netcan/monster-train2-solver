using System.Collections;
using System.Reflection;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    // Room selection can enqueue a support spell before RunCombat starts it.
    // IsRunningTriggerQueue alone does not cover that pending effects queue.
    internal static class NativeEffectSettlement
    {
        private static readonly FieldInfo effects = AccessTools.Field(typeof(CombatManager), "effectsQueue");
        private static readonly FieldInfo resolving = AccessTools.Field(typeof(CombatManager), "cardEffectResolving");
        internal static int PendingEffectWaits, ResolvingEffectWaits, InputWaits;
        internal static object Evidence => new { PendingEffectWaits, ResolvingEffectWaits, InputWaits };
        internal static bool Ready(CombatManager combat)
        {
            if (((ICollection)effects.GetValue(combat)).Count > 0) { PendingEffectWaits++; return false; }
            if ((bool)resolving.GetValue(combat)) { ResolvingEffectWaits++; return false; }
            if (!combat.AllowExternalInput) { InputWaits++; return false; }
            return true;
        }
    }
}

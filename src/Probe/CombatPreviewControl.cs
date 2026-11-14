using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MonsterTrain2Poju.Probe
{
    // The old game exposes one Boolean; newer builds expose a counted Push/Pop
    // API. Keep the probe's counted ownership separate from native owners.
    internal static class CombatPreviewControl
    {
        private const BindingFlags instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly PropertyInfo? legacy = typeof(CombatManager).GetProperty("SuppressCombatPreviewUpdates", instance);
        private static readonly FieldInfo? count = typeof(CombatManager).GetField("combatPreviewSuppressionCount", instance);
        private static readonly MethodInfo? push = typeof(CombatManager).GetMethod("PushSuppressCombatPreviewUpdates", instance);
        private static readonly MethodInfo? pop = typeof(CombatManager).GetMethod("PopSuppressCombatPreviewUpdates", instance);
        private sealed class Ownership { internal bool Held; }
        private static readonly ConditionalWeakTable<CombatManager, Ownership> ownership = new ConditionalWeakTable<CombatManager, Ownership>();

        private static int Count(CombatManager combat)
        {
            if (count == null || push == null || pop == null)
                throw new NotSupportedException("Unknown native combat preview suppression API: " +
                    typeof(CombatManager).Assembly.ManifestModule.ModuleVersionId);
            return (int)count.GetValue(combat);
        }

        internal static bool Get(CombatManager combat) => legacy != null
            ? (bool)legacy.GetValue(combat) : Count(combat) > 0;

        // Saved values restore the old Boolean or our own counted token. A
        // native token present at entry must never become the probe's token.
        internal static bool Save(CombatManager combat)
        {
            if (legacy != null) return Get(combat);
            Ownership state = ownership.GetOrCreateValue(combat);
            if (Count(combat) == 0) state.Held = false; // native battle reset
            return state.Held;
        }

        internal static void Set(CombatManager combat, bool value)
        {
            if (legacy != null) { legacy.SetValue(combat, value); return; }
            Ownership state = ownership.GetOrCreateValue(combat);
            if (Count(combat) == 0) state.Held = false;
            if (state.Held == value) return;
            (value ? push! : pop!).Invoke(combat, null);
            state.Held = value;
        }

#if DEVELOPMENT_PROBE
        internal static void VerifyNative(CombatManager combat)
        {
            bool saved = Save(combat), visible = Get(combat);
            int baseline = legacy == null ? Count(combat) : 0;
            try
            {
                Set(combat, true);
                int acquired = legacy == null ? Count(combat) : 0;
                Set(combat, true);
                if (!Get(combat) || legacy == null &&
                    (acquired != baseline + (saved ? 0 : 1) || Count(combat) != acquired))
                    throw new InvalidOperationException("Repeated probe suppression changed native ownership.");
                bool nested = Save(combat);
                Set(combat, true); Set(combat, nested);
                if (legacy == null && Count(combat) != acquired)
                    throw new InvalidOperationException("Nested suppression changed native count.");
                if (legacy == null)
                {
                    push!.Invoke(combat, null); // independent native owner
                    try
                    {
                        Set(combat, false); Set(combat, false);
                        if (!Get(combat) || Count(combat) != acquired)
                            throw new InvalidOperationException("Probe released a native preview suppression token.");
                        Set(combat, true);
                        if (Count(combat) != acquired + 1)
                            throw new InvalidOperationException("Probe did not reacquire its own token.");
                    }
                    finally { pop!.Invoke(combat, null); }
                }
                else
                {
                    Set(combat, false);
                    if (Get(combat)) throw new InvalidOperationException("Legacy preview suppression did not release.");
                }
            }
            finally { Set(combat, saved); }
            if (Get(combat) != visible || legacy == null && Count(combat) != baseline)
                throw new InvalidOperationException("Native preview suppression baseline was not restored.");
            UnityEngine.Debug.Log("PREVIEW-SUPPRESSION-NATIVE PASS backend=" + (legacy == null ? "counter" : "boolean") +
                " repeated/nested/external-owner/restoration verified");
        }
#endif
    }
}

using System;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    // Select the game's own timing table for isolated direct probes. No preferences
    // are written, and preview/undo/fast-replay keep their native Instant behavior.
    internal static class ProbeGameSpeed
    {
        internal static SaveManager.GameSpeed? Requested { get; private set; }
        internal static int LiveOverrides { get; private set; }
        internal static void Configure(ManualLogSource log)
        {
            string? text = Environment.GetEnvironmentVariable("MT2_PROBE_GAME_SPEED");
            if (string.IsNullOrEmpty(text)) return;
            if (!Enum.TryParse(text, out SaveManager.GameSpeed speed) || !Enum.IsDefined(typeof(SaveManager.GameSpeed), speed))
                throw new InvalidOperationException("Invalid isolated probe game speed: " + text);
            Requested = speed;
            log.LogInfo("PROBE-GAME-SPEED configured=" + speed + "; native timing table; preferences unchanged.");
        }
        [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.GetActiveGameSpeed))]
        private static class ActiveSpeedPatch
        {
            private static void Postfix(SaveManager __instance, ref SaveManager.GameSpeed __result)
            {
                if (!Requested.HasValue || __instance.PreviewMode || __instance.IsInUndoMode || __result == SaveManager.GameSpeed.Instant) return;
                __result = Requested.Value; LiveOverrides++;
            }
        }
    }
}

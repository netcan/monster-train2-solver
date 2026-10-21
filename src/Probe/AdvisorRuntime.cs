using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class AdvisorRuntime
    {
        internal static bool PlayerSession { get; set; }
    }

    internal sealed class PlayerAdvisorSettings
    {
        internal int Horizon { get; set; } = 3;
        internal int TimeLimitMs { get; set; } = 3000;
        internal int DeploymentRoom { get; set; } = 2;
        internal bool TacticalSeed { get; set; }
        internal KeyCode ToggleKey { get; set; } = KeyCode.F8;
    }
}

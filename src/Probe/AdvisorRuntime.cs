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
        internal bool WholeBattle { get; set; } = true;
        internal int MaxSteps { get; set; } = 256;
        internal int SearchWorkers { get; set; } = 2;
        internal int DeploymentRoom { get; set; } = 2;
        internal bool TacticalSeed { get; set; }
        internal bool PauseWhenHidden { get; set; } = true;
        internal KeyCode ToggleKey { get; set; } = KeyCode.F8;
    }
}

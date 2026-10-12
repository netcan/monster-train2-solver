using System;
using BepInEx.Logging;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class AbilityTeleportScenario
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "ability-teleport";

        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            AbilityActivationScenario.Prepare(managers, log, false, false, teleport: true);
            log.LogInfo("ABILITY-TELEPORT-PREPARED target skill, same-room reorder and cross-room destination.");
        }

        internal static PlayCardAction? Choose(BattleTurnState decision) =>
            UnitAbilityModel.ChooseTeleportCalibrationAction(decision);
    }
}

using System;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class NativeStartingClans
    {
        internal static void Select(RunSetupScreen setup, SaveManager save, ManualLogSource log)
        {
            foreach (var selection in new[]
            {
                (Requested: Environment.GetEnvironmentVariable("MT2_PROBE_PRIMARY_CLAN"), IsMain: true),
                (Requested: Environment.GetEnvironmentVariable("MT2_PROBE_SECONDARY_CLAN"), IsMain: false)
            })
            {
                if (string.IsNullOrEmpty(selection.Requested)) continue;
                ClassData clan = save.GetAllGameData().GetAllClassDatas().Single(data =>
                    data.GetCardStyle().ToString().Equals(selection.Requested, StringComparison.OrdinalIgnoreCase));
                // Only the disposable profile is unlocked. Starting cards,
                // champions, relics and encounters still use native setup.
                if (clan.IsCrew() && save.GetMetagameSave().UnlockFeature(MetagameSaveData.UnlockedFeature.Crew))
                    log.LogInfo("ISOLATED-PROFILE-CREW-UNLOCK " + clan.GetCardStyle());
                if (!save.IsClassUnlockedInMetagame(clan))
                {
                    save.GetMetagameSave().UnlockClass(clan);
                    log.LogInfo("ISOLATED-PROFILE-CLAN-UNLOCK " + clan.GetCardStyle());
                }
                if (!save.IsUnlockedAndAvailableWhenStartingRun(clan))
                    throw new InvalidOperationException("Requested native starting clan is locked: " + selection.Requested +
                        " crew=" + clan.IsCrew() + " dlc=" + clan.GetRequiredDlc() +
                        " installed=" + save.IsDlcInstalled(clan.GetRequiredDlc()) +
                        " enabled=" + save.IsDlcAvailableWhenStartingRun(clan.GetRequiredDlc()));
                AccessTools.Method(typeof(RunSetupScreen), "HandleClanOptionsSelected").Invoke(setup,
                    new object[] { selection.IsMain, new RunSetupClanSelectionLayoutUI.ClanOptionData(clan, false) });
                log.LogInfo((selection.IsMain ? "NATIVE-STARTING-CLAN " : "NATIVE-SECONDARY-CLAN ") +
                    clan.GetCardStyle() + " id=" + clan.GetID());
                save.MarkOneTimeMessageComplete(OneTimeMessage.MoonPhase);
            }
        }
    }
}

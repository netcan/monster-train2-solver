using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class OuterBossPyreProbe
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_OUTER_BOSS_PYRE") == "1";
        private sealed class Record
        {
            public OuterBossPhaseState Before { get; set; } = null!;
            public BattleTurnState? Actual { get; set; }
            public RoomOutcome Outcome { get; set; }
            public bool NativeReturned { get; set; }
            public bool TerminalStopped { get; set; }
        }
        private static readonly List<Record> Records = new List<Record>();
        private static readonly List<string> Errors = new List<string>();
        private static IEnumerator Observe(IEnumerator native)
        {
            Record? row = null;
            if (Enabled && FullBattleTrace.Active != null && !AllGameManagers.Instance.GetSaveManager().PreviewMode)
                try
                {
                    row = new Record { Before = new OuterBossPhaseState(FullBattleTrace.Active.CaptureDecision(true, true), OuterBossPhaseProbe.CaptureProgress()) };
                    Records.Add(row);
                }
                catch (Exception error) { Errors.Add(error.ToString()); }
            bool completed = false;
            try { while (native.MoveNext()) yield return native.Current; completed = true; }
            finally
            {
                (native as IDisposable)?.Dispose();
                if (row != null && !row.TerminalStopped)
                    try
                    {
                        row.NativeReturned = completed;
                        row.Actual = FullBattleTrace.Active!.CaptureDecision(true, true);
                        row.Outcome = FullBattleTrace.Active.NativeWon.HasValue
                            ? FullBattleTrace.Active.NativeWon.Value ? RoomOutcome.BattleWon : RoomOutcome.PlayerDefeated : RoomOutcome.Exchanged;
                    }
                    catch (Exception error) { Errors.Add(error.ToString()); }
            }
        }
        internal static void CompleteTerminal()
        {
            if (!Enabled) return;
            foreach (Record row in Records.Where(value => !value.NativeReturned))
            {
                row.TerminalStopped = true;
                row.Actual = FullBattleTrace.Active!.CaptureDecision(true, true);
                row.Outcome = FullBattleTrace.Active.NativeWon == true ? RoomOutcome.BattleWon : RoomOutcome.PlayerDefeated;
            }
        }
        internal static void Write(ManualLogSource log)
        {
            if (!Enabled) return;
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                Boundary = "AfterDoBossTowerAttackOrTerminalStop", QueueControls = OuterBossPhaseProbe.Controls,
                Actions = Records, Errors }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "outer-boss-pyre-calibration.mt2f"))) document.Write(output);
            if (Records.Count == 0 || Records.Any(row => row.Actual == null || !row.NativeReturned && !row.TerminalStopped) || Errors.Count > 0)
                throw new InvalidOperationException("Native outer-boss Pyre capture failed.");
            log.LogInfo("OUTER-BOSS-PYRE-CAPTURE actions=" + Records.Count + " errors=" + Errors.Count);
        }
        [HarmonyPatch(typeof(HeroManager), nameof(HeroManager.DoBossTowerAttack))]
        private static class AttackPatch
        { private static void Postfix(ref IEnumerator __result) { if (Enabled) __result = Observe(__result); } }
    }
}

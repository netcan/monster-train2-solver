#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class AngelDuoCompanionPhaseProbe
    {
        private static readonly List<object> Rows = new List<object>();
        private static readonly List<string> Errors = new List<string>();
        private static IEnumerator Observe(IEnumerator native, CombatManager.Phase phase)
        {
            BattleTurnState? before = null;
            AllGameManagers managers = AllGameManagers.Instance;
            if (OuterBossPhaseProbe.TurnsEnabled && FullBattleTrace.Active != null &&
                !managers.GetSaveManager().PreviewMode && phase == CombatManager.Phase.BossActionPreCombat &&
                managers.GetSaveManager().GetCurrentScenarioData()?.name == "Level6AngelDuo_RallyAttackIncantHealth")
            {
                try
                {
                    var heroes = managers.GetHeroManager();
                    int next = (int)AccessTools.Field(typeof(HeroManager), "spawnPatternPhase").GetValue(heroes);
                    var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(heroes);
                    if (next >= pattern.GetNumGroups(managers.GetSaveManager()))
                        before = FullBattleTrace.Active.CaptureDecision(true, true);
                }
                catch (Exception error) { Errors.Add(error.ToString()); }
            }
            bool returned = false;
            try { while (native.MoveNext()) yield return native.Current; returned = true; }
            finally
            {
                (native as IDisposable)?.Dispose();
                if (before != null)
                    try { Rows.Add(new { Before = before, Actual = FullBattleTrace.Active!.CaptureDecision(true, true),
                        Phase = phase.ToString(), NativeReturned = returned }); }
                    catch (Exception error) { Errors.Add(error.ToString()); }
            }
        }
        internal static void Write(ManualLogSource log)
        {
            if (Rows.Count == 0 && Errors.Count == 0) return;
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId, Rows, Errors }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                "angel-duo-companion-phase-calibration.mt2f"))) document.Write(output);
            if (Errors.Count != 0) throw new InvalidOperationException("Angel Duo companion phase capture failed.");
            log.LogInfo("ANGEL-DUO-COMPANION-PHASE-CAPTURE rows=" + Rows.Count + " errors=0");
        }
        [HarmonyPatch(typeof(HeroManager), "PerformCompanionBossAction")]
        private static class PhasePatch
        { private static void Postfix(CombatManager.Phase phase, ref IEnumerator __result) => __result = Observe(__result, phase); }
    }
}
#endif

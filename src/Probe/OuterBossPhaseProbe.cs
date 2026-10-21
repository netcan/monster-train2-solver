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
    internal static class OuterBossPhaseProbe
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_OUTER_BOSS_PHASES") == "1";
        internal sealed class Record
        {
            public string Phase { get; set; } = "";
            public OuterBossPhaseState Before { get; set; } = null!;
            public OuterBossPhaseState? Actual { get; set; }
            public bool NativeReturned { get; set; }
        }
        private static readonly List<Record> Records = new List<Record>();
        private static readonly List<string> Errors = new List<string>();
        private static Record? pending;
        private static T Field<T>(object owner, string name) => (T)AccessTools.Field(owner.GetType(), name).GetValue(owner);
        private static bool Capturing => Enabled && FullBattleTrace.Active != null && !AllGameManagers.Instance.GetSaveManager().PreviewMode;
        private static OuterBossPhaseState Capture()
        {
            AllGameManagers managers = AllGameManagers.Instance;
            CharacterState boss = managers.GetHeroManager().GetOuterTrainBossCharacter();
            BossState state = boss.GetBossState();
            BossActionController controller = Field<BossActionController>(state, "_bossActionController");
            OuterBossActionProgress Action(BossActionState action)
            {
                object behavior = Field<object>(action, "_actionBehavior");
                Team.Type team = behavior is RandomOccupiedBossActionBehavior ? Field<Team.Type>(behavior, "_teamType") : Team.Type.Monsters;
                return new OuterBossActionProgress(behavior.GetType().Name, team == Team.Type.Monsters ? CombatTeam.Player : CombatTeam.Enemy,
                    action.GetTargetedRoomIndex(), action.GetPostActionRoomIndex(), Field<bool>(action, "_setupAsInitialAction"),
                    action.IsRoomDestroyAction(), action.IsEmptyAction());
            }
            object primary = AccessTools.Property(typeof(CharacterState), "PrimaryStateInformation").GetValue(boss);
            var progress = new OuterBossProgress(FullBattleTrace.Active!.UnitId(boss), boss.GetSourceCharacterData().GetAssetKey(),
                state.GetCurrentAttackPhase().ToString(), (int)Field<BossState.AttackPhase>(boss, "validBossAttackPhase"),
                Field<bool>(primary, "canAttack"), boss.GetCurrentRoomIndex(), boss.GetMovementSpeed(), state.GetVisitedRoomRecord(),
                BossActionControllerProbe.Snapshot(controller, managers.GetRoomManager()),
                Field<List<ActionGroupState>>(controller, "_actionGroups").Select(group => (IReadOnlyList<OuterBossActionProgress>)group.GetActions().Select(Action).ToArray()).ToArray(),
                Field<ActionGroupState>(controller, "_roomDestroyActionGroup").GetActions().Select(Action).ToArray());
            return new OuterBossPhaseState(FullBattleTrace.Active!.CaptureDecision(includeOuterBossForBump: true,
                ignoreOuterBossSpawnInteraction: true), progress);
        }
        private static IEnumerator Observe(IEnumerator native)
        {
            if (Capturing && AllGameManagers.Instance.GetHeroManager().GetOuterTrainBossCharacter()?.IsAlive == true)
                try
                {
                    if (pending != null) throw new InvalidOperationException("Previous native outer-boss phase has not settled.");
                    pending = new Record { Phase = AllGameManagers.Instance.GetCombatManager().GetCombatPhase().ToString(), Before = Capture() };
                    Records.Add(pending);
                }
                catch (Exception error) { Errors.Add(error.ToString()); }
            try { while (native.MoveNext()) yield return native.Current; }
            finally { (native as IDisposable)?.Dispose(); if (pending != null) pending.NativeReturned = true; }
        }
        private static IEnumerator Settle(IEnumerator native, Record row)
        {
            try { while (native.MoveNext()) yield return native.Current; }
            finally
            {
                (native as IDisposable)?.Dispose();
                try { row.Actual = Capture(); pending = null; }
                catch (Exception error) { Errors.Add(error.ToString()); }
            }
        }
        internal static void Write(ManualLogSource log)
        {
            if (!Enabled) return;
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1, GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                Boundary = "AfterBossActionRemoveDeadCharacters", Actions = Records, Errors }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "outer-boss-phase-calibration.mt2f"))) document.Write(output);
            int incomplete = Records.Count(row => row.Actual == null);
            log.LogInfo("OUTER-BOSS-PHASE-CAPTURE actions=" + Records.Count + " pending=" + incomplete + " errors=" + Errors.Count);
            if (Records.Count == 0 || incomplete != 0 || Errors.Count != 0) throw new InvalidOperationException("Native outer-boss phase capture failed.");
        }
        [HarmonyPatch(typeof(HeroManager), nameof(HeroManager.PerformBossAction))]
        private static class ActionPatch
        { private static void Postfix(ref IEnumerator __result) { if (Enabled) __result = Observe(__result); } }
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.RemoveDeadCharacters))]
        private static class SettlePatch
        {
            private static void Postfix(ref IEnumerator __result)
            { if (Capturing && pending?.NativeReturned == true) __result = Settle(__result, pending); }
        }
    }
}

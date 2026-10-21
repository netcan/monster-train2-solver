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
        internal static bool TurnsEnabled => Environment.GetEnvironmentVariable("MT2_PROBE_OUTER_BOSS_TURNS") == "1";
        internal static bool CaptureAnchors => Enabled || TurnsEnabled;
        internal static bool ModelsLiveBoss
        {
            get
            {
                CharacterState? boss = AllGameManagers.Instance?.GetHeroManager()?.GetOuterTrainBossCharacter();
                return TurnsEnabled && boss != null && boss.IsAlive && !boss.IsDestroyed && boss.GetSourceCharacterData().name == "Boss_Level8_AllyBuffer";
            }
        }
        private static bool ForceRelentless => Environment.GetEnvironmentVariable("MT2_PROBE_OUTER_BOSS_RELENTLESS") == "1";
        internal sealed class Record
        {
            public string Phase { get; set; } = "";
            public OuterBossPhaseState Before { get; set; } = null!;
            public OuterBossPhaseState? Actual { get; set; }
            public bool NativeReturned { get; set; }
        }
        private static readonly List<Record> Records = new List<Record>();
        private static readonly List<string> Errors = new List<string>();
        private static readonly List<object> QueueControls = new List<object>();
        internal static IReadOnlyList<object> Controls => QueueControls;
        private static Record? pending;
        private static T Field<T>(object owner, string name) => (T)AccessTools.Field(owner.GetType(), name).GetValue(owner);
        private static bool Capturing => Enabled && FullBattleTrace.Active != null && !AllGameManagers.Instance.GetSaveManager().PreviewMode;
        internal static int[]? AnchorSlots(CharacterState unit, BossState.AttackPhase phase)
        {
            if (!CaptureAnchors || !unit.IsOuterTrainBoss()) return null;
            return AnchorSlots(phase);
        }
        internal static int[] AnchorSlots(BossState.AttackPhase phase)
        {
            RoomManager rooms = AllGameManagers.Instance.GetRoomManager();
            return Enumerable.Range(0, rooms.GetNumRooms()).Select(index =>
            {
                RoomState room = rooms.GetRoom(index);
                ShinyShoe.BossAnchor? anchor = Field<ShinyShoe.BossAnchor[]>(room, "_bossAnchors").FirstOrDefault(value => value.AttackPhase == phase);
                var group = Field<SpawnPointGroup>(room, "heroSpawnPointGroup");
                return anchor == null ? -1 : Field<List<SpawnPoint>>(group, "_primarySpawnPoints").IndexOf(anchor.SpawnPoint);
            }).ToArray();
        }
        internal static OuterBossProgress CaptureProgress() => CaptureProgress(AllGameManagers.Instance.GetHeroManager().GetOuterTrainBossCharacter());
        internal static OuterBossProgress CaptureProgress(CharacterState boss)
        {
            BossState state = boss.GetBossState();
            BossActionController controller = Field<BossActionController>(state, "_bossActionController");
            object primary = AccessTools.Property(typeof(CharacterState), "PrimaryStateInformation").GetValue(boss);
            return CaptureProgress(controller, FullBattleTrace.Active!.UnitId(boss), boss.GetSourceCharacterData(),
                state.GetCurrentAttackPhase().ToString(), (int)Field<BossState.AttackPhase>(boss, "validBossAttackPhase"),
                Field<bool>(primary, "canAttack"), boss.GetCurrentRoomIndex(), boss.GetMovementSpeed(), state.GetVisitedRoomRecord());
        }
        internal static OuterBossProgress Definition(CharacterData data) => CaptureProgress(new BossActionController(data.GetBossActionData()),
            0, data, "Casting", (int)data.GetValidBossAttackPhase(), data.GetCanAttack(), -1, 1, Array.Empty<int>());
        private static OuterBossProgress CaptureProgress(BossActionController controller, int id, CharacterData data,
            string phase, int validPhases, bool canAttack, int room, int speed, IReadOnlyList<int> visits)
        {
            OuterBossActionProgress Action(BossActionState action)
            {
                object behavior = Field<object>(action, "_actionBehavior");
                Team.Type team = behavior is RandomOccupiedBossActionBehavior ? Field<Team.Type>(behavior, "_teamType") : Team.Type.Monsters;
                return new OuterBossActionProgress(behavior.GetType().Name, team == Team.Type.Monsters ? CombatTeam.Player : CombatTeam.Enemy,
                    action.GetTargetedRoomIndex(), action.GetPostActionRoomIndex(), Field<bool>(action, "_setupAsInitialAction"),
                    action.IsRoomDestroyAction(), action.IsEmptyAction());
            }
            return new OuterBossProgress(id, data.GetAssetKey(), phase, validPhases, canAttack, room, speed, visits,
                BossActionControllerProbe.Snapshot(controller, AllGameManagers.Instance.GetRoomManager()),
                Field<List<ActionGroupState>>(controller, "_actionGroups").Select(group => (IReadOnlyList<OuterBossActionProgress>)group.GetActions().Select(Action).ToArray()).ToArray(),
                Field<ActionGroupState>(controller, "_roomDestroyActionGroup").GetActions().Select(Action).ToArray());
        }
        private static OuterBossPhaseState Capture() => new OuterBossPhaseState(FullBattleTrace.Active!.CaptureDecision(
            includeOuterBossForBump: true, ignoreOuterBossSpawnInteraction: true), CaptureProgress());
        private static IEnumerator Observe(IEnumerator native)
        {
            Record? row = null;
            if (Capturing && AllGameManagers.Instance.GetHeroManager().GetOuterTrainBossCharacter()?.IsAlive == true)
                try
                {
                    if (pending != null) throw new InvalidOperationException("Previous native outer-boss phase has not settled.");
                    CombatManager combat = AllGameManagers.Instance.GetCombatManager();
                    if (ForceRelentless && QueueControls.Count == 0 && combat.GetTurnCount() == 2 &&
                        combat.GetCombatPhase() == CombatManager.Phase.BossActionPreCombat)
                    {
                        HeroManager heroes = AllGameManagers.Instance.GetHeroManager();
                        SpawnPatternData pattern = Field<SpawnPatternData>(heroes, "spawnPattern");
                        var waves = new List<SpawnGroupPoolData>(); pattern.GetUnlockedWaves(AllGameManagers.Instance.GetSaveManager(), waves);
                        int previous = Field<int>(heroes, "spawnPatternPhase");
                        AccessTools.Field(typeof(HeroManager), "spawnPatternPhase").SetValue(heroes, waves.Count);
                        QueueControls.Add(new { Kind = "ForceSpawnQueueEmpty", Turn = combat.GetTurnCount(),
                            PhaseBefore = previous, PhaseAfter = waves.Count, NativeWaveCount = waves.Count });
                    }
                    row = new Record { Phase = AllGameManagers.Instance.GetCombatManager().GetCombatPhase().ToString(), Before = Capture() };
                    pending = row; Records.Add(row);
                }
                catch (Exception error) { Errors.Add(error.ToString()); }
            bool completed = false;
            try { while (native.MoveNext()) yield return native.Current; completed = true; }
            finally { (native as IDisposable)?.Dispose(); if (row != null) row.NativeReturned = completed; }
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
                Boundary = "AfterBossActionRemoveDeadCharacters", ForcedSpawnQueueEmpty = ForceRelentless,
                QueueControls, Actions = Records, Errors }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "outer-boss-phase-calibration.mt2f"))) document.Write(output);
            int incomplete = Records.Count(row => row.Actual == null);
            log.LogInfo("OUTER-BOSS-PHASE-CAPTURE actions=" + Records.Count + " pending=" + incomplete + " errors=" + Errors.Count);
            if (Records.Count == 0 || incomplete != 0 || Errors.Count != 0) throw new InvalidOperationException("Native outer-boss phase capture failed.");
            if (ForceRelentless && QueueControls.Count != 1) throw new InvalidOperationException("Missing explicit native spawn-queue control.");
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

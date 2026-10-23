#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.IO;
using System.Linq;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class AngelDuoDeathScenario
    {
        internal static string Target => Environment.GetEnvironmentVariable("MT2_PROBE_ANGEL_DUO_DEATH") ?? "";
        internal static bool Enabled => Target.Length > 0;
        internal static bool Completed { get; private set; }
        internal static string? Error { get; private set; }
        private static bool started;
        private static float startedAt;
        internal static object? Transition { get; private set; }
        internal static object? Control => Enabled ? new { Target, Kind = "OriginalAngelDuoDebuffHealthToOneAndNativeDamage",
            Damage = 100, NaturalVictory = false } : null;

        internal static void Tick()
        {
            if (!Enabled || Completed || Error != null) return;
            CombatManager combat = AllGameManagers.Instance.GetCombatManager();
            if (started)
            { if (Time.realtimeSinceStartup - startedAt > 60) Error = "Angel Duo controlled native death timed out."; return; }
            if (combat.GetTurnCount() < 1 || combat.GetCombatPhase() != CombatManager.Phase.MonsterTurn ||
                !combat.HaveCardsBeenDrawnForCurrentTurn() || combat.IsRunningTriggerQueue || !CardAnimationSettlement.Ready()) return;
            started = true; startedAt = Time.realtimeSinceStartup;
            combat.StartCoroutine(Run());
        }

        private static IEnumerator Run()
        {
            AllGameManagers managers = AllGameManagers.Instance;
            CombatManager combat = managers.GetCombatManager();
            CharacterState? target = Target == "Outer" ? managers.GetHeroManager().GetOuterTrainBossCharacter() :
                managers.GetHeroManager().GetCharactersReadOnly().SingleOrDefault(actor =>
                    actor.GetSourceCharacterData().GetAssetKey() == "Boss_Level6_AngelDuo_Savage_Variant2_SlayAttack");
            if (target == null || !target.IsAlive || !target.IsCompanionBoss())
            { Error = "Missing original live Angel Duo death target."; yield break; }
            FullBattleTrace trace = FullBattleTrace.Active!;
            trace.Checkpoint("decision-before-controlled-angel-duo-death");
            var original = trace.CaptureDecision(true, true);
            int targetId = trace.UnitId(target), room = target.GetCurrentRoomIndex();
            target.DebuffHP(target.GetHP() - 1);
            var before = trace.CaptureDecision(true, true);
            bool readyBefore = NativeDecisionReadiness.CardsDrawn(combat);
            var roomBefore = trace.Capture(managers.GetRoomManager().GetRoom(room), includeOuterBossForBump: true);
            yield return combat.ApplyDamageToTarget(100, target, new CombatManager.ApplyDamageToTargetParameters
                { damageType = Damage.Type.Default, finalEffectInSequence = true });
            var afterDamage = trace.CaptureDecision(true, true);
            yield return combat.RemoveDeadCharacters();
            var actual = trace.CaptureDecision(true, true);
            if (target.IsAlive || !target.HasFinishedDying)
            { Error = "Controlled Angel Duo damage did not complete original death."; yield break; }
            Transition = new { Schema = 1,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                Control, TargetId = targetId, Room = room, Original = original, Before = before,
                RoomBefore = roomBefore, AfterDamage = afterDamage, Actual = actual,
                CardsReadyBefore = readyBefore, CardsDrawnAfter = combat.HaveCardsBeenDrawnForCurrentTurn(),
                CardsReadyAfter = NativeDecisionReadiness.CardsDrawn(combat) };
            using (var document = NativeFixtureCapture.Capture(Transition))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                "angel-duo-death-calibration.mt2f"))) document.Write(output);
            Completed = true;
        }
    }
}
#endif

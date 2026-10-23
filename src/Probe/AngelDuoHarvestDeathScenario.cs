#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.IO;
using System.Linq;
using MonsterTrain2Poju.Capture;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class AngelDuoHarvestDeathScenario
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_ANGEL_DUO_HARVEST_DEATH") == "1";
        internal static bool Started { get; private set; }
        internal static bool Completed { get; private set; }
        internal static string? Error { get; private set; }
        internal static object? Transition { get; private set; }
        private static float startedAt;

        internal static void Tick()
        {
            if (!Enabled || Completed || Error != null) return;
            if (Started)
            { if (Time.realtimeSinceStartup - startedAt > 60) Error = "Controlled native Harvest timed out."; return; }
            AllGameManagers managers = AllGameManagers.Instance;
            CombatManager combat = managers.GetCombatManager();
            if (combat.GetTurnCount() < 1 || combat.GetCombatPhase() != CombatManager.Phase.MonsterTurn ||
                !combat.HaveCardsBeenDrawnForCurrentTurn() || combat.IsRunningTriggerQueue || !CardAnimationSettlement.Ready()) return;
            CharacterState? outer = managers.GetHeroManager().GetOuterTrainBossCharacter();
            CharacterState? inner = managers.GetHeroManager().GetCharactersReadOnly().SingleOrDefault(unit =>
                unit.GetSourceCharacterData().GetAssetKey() == "Boss_Level6_AngelDuo_Savage_Variant3_RevengeAttack" && unit.IsAlive);
            if (outer?.GetSourceCharacterData().GetAssetKey() != "Boss_Level6_AngelDuo_Entropic_Variant3_HarvestHealth" ||
                !outer.IsAlive || inner == null || inner.GetCurrentRoomIndex() == outer.GetCurrentRoomIndex()) return;
            CharacterState? target = managers.GetHeroManager().GetCharactersReadOnly().FirstOrDefault(unit =>
                unit.IsAlive && !unit.IsDestroyed && !unit.IsAnyBoss() &&
                unit.GetCurrentRoomIndex() == outer.GetCurrentRoomIndex());
            if (target == null) return;
            Started = true; startedAt = Time.realtimeSinceStartup;
            combat.StartCoroutine(Run(target));
        }

        private static IEnumerator Run(CharacterState target)
        {
            AllGameManagers managers = AllGameManagers.Instance;
            CombatManager combat = managers.GetCombatManager();
            FullBattleTrace trace = FullBattleTrace.Active!;
            trace.Checkpoint("decision-before-controlled-angel-duo-harvest");
            var before = trace.CaptureDecision(true, true);
            int targetId = trace.UnitId(target), room = target.GetCurrentRoomIndex();
            // The native damage API kills an original wave minion. No authored
            // asset, deck, boss stat or target health is edited beforehand.
            yield return combat.ApplyDamageToTarget(10000, target, new CombatManager.ApplyDamageToTargetParameters
                { damageType = Damage.Type.Default, finalEffectInSequence = true });
            var afterDamage = trace.CaptureDecision(true, true);
            yield return combat.RemoveDeadCharacters();
            var actual = trace.CaptureDecision(true, true);
            if (target.IsAlive || !target.HasFinishedDying)
            { Error = "Controlled native Harvest target did not finish dying."; yield break; }
            Transition = new { Schema = 1, Kind = "NativeDamageOriginalMinionForRemoteHarvest",
                Damage = 10000, NoHealthSetup = true, AuthoredAssetsUnchanged = true, NaturalVictory = false,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                TargetId = targetId, Room = room, Before = before, AfterDamage = afterDamage, Actual = actual };
            using (var document = NativeFixtureCapture.Capture(Transition))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                "angel-duo-harvest-calibration.mt2f"))) document.Write(output);
            Completed = true;
        }
    }
}
#endif

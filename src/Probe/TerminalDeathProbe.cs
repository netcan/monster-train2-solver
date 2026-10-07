using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class TerminalDeathProbe
    {
        internal static readonly List<Record> Records = new List<Record>();
        internal static readonly List<KillCamRecord> KillCams = new List<KillCamRecord>();
        internal sealed class KillCamRecord
        {
            public int Sequence { get; set; }
            public int UnitId { get; set; }
            public bool DuringPreview { get; set; }
            public CombatContext Before { get; set; } = null!;
            public CombatContext? Actual { get; set; }
            public bool Completed { get; set; }
        }
        internal sealed class Record
        {
            public int Sequence { get; set; }
            public int UnitId { get; set; }
            public CombatTeam Team { get; set; }
            public int SpawnerCardId { get; set; }
            public int SourceCardId { get; set; }
            public int AttackerId { get; set; }
            public bool Terminal { get; set; }
            public bool Sacrifice { get; set; }
            public CombatContext Before { get; set; } = null!;
            public CombatContext? Actual { get; set; }
            public bool FinishedDying { get; set; }
            public bool Completed { get; set; }
        }
        private static bool Enabled() => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "terminal-death-damage" &&
            DamageDeathQueueScenario.Prepared && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode;
        private static IEnumerator WrapKillCam(IEnumerator native, CharacterState unit)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            var record = new KillCamRecord { Sequence = KillCams.Count, UnitId = trace.UnitId(unit),
                DuringPreview = AllGameManagers.Instance!.GetSaveManager().PreviewMode, Before = trace.CaptureContext() };
            KillCams.Add(record);
            try { while (native.MoveNext()) yield return native.Current; record.Completed = true; }
            finally { (native as IDisposable)?.Dispose(); record.Actual = trace.CaptureContext(); }
        }
        private static IEnumerator Wrap(IEnumerator native, CharacterState unit, CardState source, CharacterState attacker, bool sacrifice)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            CardState spawner = unit.GetSpawnerCard();
            var record = new Record { Sequence = Records.Count, UnitId = trace.UnitId(unit),
                Team = unit.GetTeamType() == Team.Type.Monsters ? CombatTeam.Player : CombatTeam.Enemy,
                SpawnerCardId = spawner == null ? 0 : trace.CardId(spawner), SourceCardId = source == null ? 0 : trace.CardId(source),
                AttackerId = attacker == null ? 0 : trace.UnitId(attacker), Sacrifice = sacrifice,
                Terminal = unit.IsPyreHeart() || (unit.IsMiniboss() || unit.IsOuterTrainBoss()) &&
                    AllGameManagers.Instance!.GetHeroManager()!.FindPairedCompanionBoss(unit) == null, Before = trace.CaptureContext() };
            Records.Add(record);
            try
            {
                while (native.MoveNext()) yield return native.Current;
                record.Completed = true;
            }
            finally
            {
                (native as IDisposable)?.Dispose(); record.FinishedDying = unit.HasFinishedDying;
                record.Actual = trace.CaptureContext();
            }
        }
        [HarmonyPatch(typeof(CharacterState), "CheckForDeath")]
        private static class DeathPatch
        {
            private static void Postfix(CharacterState __instance, CardState damageSourceCard, CharacterState attacker, bool forSacrifice, ref IEnumerator __result)
            {
                if (Enabled() && __instance.IsDead && !__instance.HasFinishedDying)
                    __result = Wrap(__result, __instance, damageSourceCard, attacker, forSacrifice);
            }
        }
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.ShowKillCam))]
        private static class KillCamPatch
        {
            private static void Postfix(CharacterState killedCharacter, ref IEnumerator __result)
            {
                // RunBossKillPreview invokes this while the temporary character states are active.
                if (Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "terminal-death-damage" &&
                    DamageDeathQueueScenario.Prepared && FullBattleTrace.Active != null)
                    __result = WrapKillCam(__result, killedCharacter);
            }
        }
    }
}

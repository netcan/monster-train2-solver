using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class CompanionBossProbe
    {
        internal sealed class Record
        {
            public string Phase { get; set; } = "";
            public EnemySpawnState Before { get; set; } = null!;
            public EnemySpawnState RawBefore { get; set; } = null!;
            public TrainCombatResult Predicted { get; set; } = null!;
            public TrainCombatState? RawAfter { get; set; }
            public TrainCombatState? Actual { get; set; }
            public TrainCombatState? RawActual { get; set; }
            public string? Difference { get; set; }
        }
        internal sealed class Removal
        {
            public CombatUnit Before { get; set; } = null!;
            public CombatUnit? Actual { get; set; }
            public int QueuedBefore { get; set; }
            public int QueuedAfter { get; set; }
            public string? Difference { get; set; }
        }
        internal static readonly List<Record> Records = new List<Record>();
        internal static readonly List<Removal> Removals = new List<Removal>();
        internal static int Mismatches => Records.Count(record => record.Difference != null) + Removals.Count(record => record.Difference != null);
        internal static int Unsupported => Records.Count(record => !record.Predicted.Supported);
        internal static int Pending => Records.Count(record => record.Actual == null) + Removals.Count(record => record.Actual == null);
        private static bool Enabled() => CompanionBossScenario.Prepared && FullBattleTrace.Active != null &&
            !AllGameManagers.Instance!.GetSaveManager().PreviewMode;
        private static IEnumerator Observe(IEnumerator native)
        {
            FullBattleTrace trace = FullBattleTrace.Active!; Record? record = null;
            try
            {
                record = new Record { Phase = AllGameManagers.Instance!.GetCombatManager()!.GetCombatPhase().ToString(),
                    RawBefore = trace.CaptureSpawnPhase(), Before = trace.CaptureCanonicalSpawn() };
                record.Predicted = CompanionBossModel.Resolve(record.Before, record.Phase == "BossActionPreCombat");
                Records.Add(record);
            }
            catch (Exception error) { trace.CaptureFailure(error); }
            try { while (native.MoveNext()) yield return native.Current; }
            finally
            {
                (native as IDisposable)?.Dispose();
                if (record != null)
                    try { record.RawAfter = trace.CaptureTrain(); }
                    catch (Exception error) { trace.CaptureFailure(error); }
            }
        }
        private static IEnumerator Settle(IEnumerator native, Record record)
        {
            try { while (native.MoveNext()) yield return native.Current; }
            finally
            {
                (native as IDisposable)?.Dispose();
                try
                {
                    record.RawActual = FullBattleTrace.Active!.CaptureTrain();
                    record.Actual = FullBattleTrace.Active!.CaptureCanonicalTrain();
                    record.Difference = !record.Predicted.Supported ? record.Predicted.UnsupportedReason :
                        JToken.DeepEquals(Comparable(record.Predicted.State!), Comparable(record.Actual)) ? null : "Companion Boss phase differs";
                }
                catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
            }
        }
        private static JToken Comparable(TrainCombatState state) => JToken.FromObject(new {
            state.Rooms, Movement = state.Movement.OrderBy(rule => rule.UnitId).ToArray(), state.EnemySlotsPerRoom, state.Context });
        private static int QueuedCount() => ((ICollection)AccessTools.Property(typeof(CombatManager), "TriggerQueue")
            .GetValue(AllGameManagers.Instance!.GetCombatManager()!)).Count;
        [HarmonyPatch(typeof(CombatManager), "PerformBossAction")]
        private static class ActionPatch
        { private static void Postfix(ref IEnumerator __result) { if (Enabled()) __result = Observe(__result); } }
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.RemoveDeadCharacters))]
        private static class SettlePatch
        {
            private static void Postfix(ref IEnumerator __result)
            {
                if (!Enabled()) return;
                Record? pending = Records.LastOrDefault(record => record.RawAfter != null && record.Actual == null);
                if (pending != null) __result = Settle(__result, pending);
            }
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.RemoveTriggersOnRelentlessChange))]
        private static class RemovalPatch
        {
            private static void Prefix(CharacterState __instance, out Removal? __state)
            {
                __state = null;
                if (!Enabled()) return;
                try
                {
                    __state = new Removal { Before = FullBattleTrace.Active!.CaptureUnit(__instance),
                        QueuedBefore = QueuedCount() };
                    Removals.Add(__state);
                }
                catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
            }
            private static void Postfix(CharacterState __instance, Removal? __state)
            {
                if (__state == null) return;
                try
                {
                    __state.Actual = FullBattleTrace.Active!.CaptureUnit(__instance);
                    __state.QueuedAfter = QueuedCount();
                    __state.Difference = JToken.DeepEquals(JToken.FromObject(CompanionBossModel.RemoveTriggers(__state.Before)),
                        JToken.FromObject(__state.Actual)) && __state.QueuedBefore == __state.QueuedAfter ? null : "Relentless removal state or queued callbacks differ";
                }
                catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    // Explicit solver protocol: before the game's existing turn-end removal flush,
    // await the original visual callbacks which populate its removal queues.
    internal static class DeathDissolveSettlement
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_SETTLE_DEATH_DISSOLVES") == "1";
        internal sealed class CallbackRecord
        {
            public int UnitId { get; set; }
            public int RegisteredFrame { get; set; }
            public int MinimumFrames { get; set; }
            public int? CompletedFrame { get; set; }
            public string? Error { get; set; }
        }
        internal sealed class SettlementRecord
        {
            public int Turn { get; set; }
            public int StartedFrame { get; set; }
            public int[] PendingUnitIds { get; set; } = Array.Empty<int>();
            public int FramesWaited { get; set; }
            public int PendingAfter { get; set; }
            public bool Completed { get; set; }
            public string? Error { get; set; }
        }
        internal static readonly List<CallbackRecord> Callbacks = new List<CallbackRecord>();
        internal static readonly List<SettlementRecord> Records = new List<SettlementRecord>();
        private static IEnumerable<CallbackRecord> Pending => Callbacks.Where(item => item.CompletedFrame == null);

        [HarmonyPatch(typeof(CharacterUI), nameof(CharacterUI.DoDeathDissolve))]
        private static class CallbackPatch
        {
            private static void Prefix(CharacterUI __instance, int minNumFrames, ref Action? callback)
            {
                if (!Enabled || callback == null || FullBattleTrace.Active == null || FullBattleTrace.Active.NativeWon != null ||
                    AllGameManagers.Instance == null || AllGameManagers.Instance.GetSaveManager().PreviewMode) return;
                CharacterState actor = (CharacterState)AccessTools.Field(typeof(CharacterUI), "_characterState").GetValue(__instance);
                if (actor == null || actor.SpawnedInPreviewMode || !actor.IsDestroyed) return;
                var record = new CallbackRecord { UnitId = FullBattleTrace.Active.UnitId(actor),
                    RegisteredFrame = Time.frameCount, MinimumFrames = minNumFrames };
                Callbacks.Add(record);
                Action original = callback;
                callback = () =>
                {
                    try { original(); }
                    catch (Exception error) { record.Error = error.ToString(); throw; }
                    finally { record.CompletedFrame = Time.frameCount; }
                };
            }
        }
        private static IEnumerator Wrap(IEnumerator native)
        {
            try { while (native.MoveNext()) yield return native.Current; }
            finally { (native as IDisposable)?.Dispose(); }
            if (FullBattleTrace.Active == null || FullBattleTrace.Active.NativeWon != null) yield break;
            var record = new SettlementRecord { Turn = AllGameManagers.Instance!.GetCombatManager()!.GetTurnCount(),
                StartedFrame = Time.frameCount, PendingUnitIds = Pending.Select(item => item.UnitId).ToArray() };
            Records.Add(record);
            float started = Time.realtimeSinceStartup;
            while (Pending.Any())
            {
                if (Time.realtimeSinceStartup - started > 15f)
                {
                    record.Error = "Original death dissolve callback did not complete before the turn-end removal flush.";
                    FullBattleTrace.Active.CaptureFailure(new InvalidOperationException(record.Error));
                    throw new InvalidOperationException(record.Error);
                }
                record.FramesWaited++;
                yield return null;
            }
            record.PendingAfter = Pending.Count(); record.Completed = true;
        }
        [HarmonyPatch(typeof(CombatManager), "RunEndMonsterTurn")]
        private static class EndTurnPatch
        {
            private static void Postfix(ref IEnumerator __result)
            { if (Enabled && FullBattleTrace.Active != null) __result = Wrap(__result); }
        }
    }
}

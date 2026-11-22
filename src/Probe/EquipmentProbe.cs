using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
#if DEVELOPMENT_PROBE
using MonsterTrain2Poju.Capture;
#endif
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class EquipmentProbe
    {
        internal sealed class Record
        {
            public int UnitId { get; set; }
            public int CardId { get; set; }
            public bool Remove { get; set; }
            public bool Grafted { get; set; }
            public bool Completed { get; set; }
            public bool DeferAbilityCallbacks { get; set; }
            public bool QueueRunning { get; set; }
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatState? After { get; set; }
            public BattlePlayRules Definitions { get; set; } = null!;
            public string? Difference { get; set; }
            public string? UnsupportedReason { get; set; }
#if DEVELOPMENT_PROBE
            public CardStatisticRefreshTraceProbe.Refresh[]? StatisticRefreshes { get; set; }
            public bool ShouldSerializeStatisticRefreshes() => StatisticRefreshes != null;
#endif
        }
        internal static readonly List<Record> Records = new List<Record>();
        private static IEnumerator Wrap(IEnumerator native, CharacterState host, CardState? card, bool remove, bool grafted = false)
        {
            FullBattleTrace? trace = FullBattleTrace.Active;
            Record? record = null; RoomCombatResult? predicted = null; RoomState? room = null;
            if (trace != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode && host.GetCurrentRoom() != null)
            {
                try
                {
                    room = host.GetCurrentRoom();
                    var decision = trace.CaptureDecision();
                    record = new Record { UnitId = trace.UnitId(host), CardId = card == null ? 0 : trace.CardId(card), Remove = remove, Grafted = grafted,
                        Before = trace.Capture(room), Definitions = decision.PlayRules!,
                        DeferAbilityCallbacks = AllGameManagers.Instance.GetReplayManager().IsCardPlaying() || AllGameManagers.Instance.GetCombatManager()!.IsRunningTriggerQueue,
                        QueueRunning = AllGameManagers.Instance.GetCombatManager()!.IsRunningTriggerQueue };
                    Records.Add(record);
                    predicted = RoomCombatModel.ApplyEquipment(record.Before, record.UnitId, record.CardId, record.Definitions, remove, record.DeferAbilityCallbacks, record.QueueRunning, grafted: grafted);
                    record.UnsupportedReason = predicted.UnsupportedReason;
                }
                catch (Exception error) { trace.CaptureFailure(error); }
            }
#if DEVELOPMENT_PROBE
            using var statisticsRefresh = record is { QueueRunning: true, Remove: false, Grafted: false } && card != null
                ? CardStatisticRefreshTraceProbe.ObserveEquipment(trace!, record.Before, record.UnitId, record.CardId, card) : null;
#endif
            while (native.MoveNext()) yield return native.Current;
            if (record == null) yield break;
#if DEVELOPMENT_PROBE
            record.StatisticRefreshes = statisticsRefresh?.Events.ToArray();
#endif
            try
            {
                record.After = trace!.Capture(room!); record.Completed = true;
                bool differs = predicted!.Supported && !JToken.DeepEquals(JToken.FromObject(predicted.State!), JToken.FromObject(record.After));
#if DEVELOPMENT_PROBE
                if (differs && EquipmentStatisticRefreshComparison.Matches(record.Before, predicted.State!, record.After, record.UnitId, record.CardId, record.StatisticRefreshes))
                    differs = false;
#endif
                if (differs)
                    record.Difference = "Equipment room/context differs";
            }
            catch (Exception error) { trace!.CaptureFailure(error); }
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.AddEquipment))]
        private static class AddPatch
        { private static void Postfix(CharacterState __instance, CardState equipmentCard, bool grafted, ref IEnumerator __result) => __result = Wrap(__result, __instance, equipmentCard, false, grafted); }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.RemoveEquipment))]
        private static class RemovePatch
        { private static void Postfix(CharacterState __instance, CardState equipmentCard, ref IEnumerator __result) => __result = Wrap(__result, __instance, equipmentCard, true); }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.RemoveAllEquipment))]
        private static class RemoveAllPatch
        { private static void Postfix(CharacterState __instance, ref IEnumerator __result) => __result = Wrap(__result, __instance, null, true); }
    }
}

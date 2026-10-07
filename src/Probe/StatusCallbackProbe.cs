using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class StatusCallbackProbe
    {
        internal static readonly List<Record> Records = new List<Record>();
        internal static readonly List<FireRecord> Fired = new List<FireRecord>();
        private static readonly HashSet<string> Kinds = new HashSet<string>(StringComparer.Ordinal)
        { "OnStatusEffectChanged", "OnArmorAdded", "OnPyregelAdded", "OnValiant", "OnSilence", "OnSilenceLost", "OnNewStatusEffectAdded" };
        internal sealed class FireRecord
        {
            public int Sequence { get; set; }
            public int ActorId { get; set; }
            public string Kind { get; set; } = "";
            public int ParamInt { get; set; }
            public int ParamInt2 { get; set; }
            public string? ParamString { get; set; }
            public int GoldBefore { get; set; }
            public int? GoldAfter { get; set; }
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatState? Actual { get; set; }
            public CombatUnit BeforeUnit { get; set; } = null!;
            public CombatUnit? ActualUnit { get; set; }
            public string[] Interactions { get; set; } = Array.Empty<string>();
            public bool Completed { get; set; }
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.FireTriggers))]
        private static class FirePatch
        {
            private static void Postfix(CharacterState __instance, CharacterTriggerData.Trigger trigger,
                CharacterState.FireTriggersData fireTriggersData, bool fromRunningTriggerQueue, ref IEnumerator __result)
            {
                if (Environment.GetEnvironmentVariable("MT2_PROBE_STATUS_CALLBACKS") == "1" && FullBattleTrace.Active != null &&
                    !AllGameManagers.Instance!.GetSaveManager().PreviewMode && fromRunningTriggerQueue &&
                    Kinds.Contains(trigger.ToString()))
                    __result = Wrap(__result, __instance, trigger, fireTriggersData);
            }
        }
        private static IEnumerator Wrap(IEnumerator native, CharacterState actor, CharacterTriggerData.Trigger kind, CharacterState.FireTriggersData data)
        {
            SaveManager save = AllGameManagers.Instance!.GetSaveManager();
            FullBattleTrace trace = FullBattleTrace.Active!;
            var interactions = new List<string>();
            // Starting/death callbacks can retain an actor with no current or last room.
            RoomState room = actor.GetCurrentRoom(allowLastKnownRoom: true) ?? AllGameManagers.Instance!.GetRoomManager()!.GetRoom(0);
            FireRecord? record = null;
            try
            {
                record = new FireRecord { Sequence = trace.NextPhaseSequence(), ActorId = trace.UnitId(actor), Kind = kind.ToString(),
                    ParamInt = data?.paramInt ?? 0, ParamInt2 = data?.paramInt2 ?? 0, ParamString = data?.paramString, GoldBefore = save.GetGold(),
                    Before = trace.Capture(room), BeforeUnit = trace.CaptureUnit(actor, interactions) };
                Fired.Add(record);
            }
            catch (Exception error) { trace.CaptureFailure(error); }
            try { while (native.MoveNext()) yield return native.Current; if (record != null) record.Completed = true; }
            finally
            {
                (native as IDisposable)?.Dispose();
                if (record != null)
                {
                    try
                    {
                        record.GoldAfter = save.GetGold();
                        record.Actual = trace.Capture(room); record.ActualUnit = trace.CaptureUnit(actor, interactions);
                        record.Interactions = interactions.Distinct().ToArray();
                    }
                    catch (Exception error) { trace.CaptureFailure(error); }
                }
            }
        }
        internal sealed class Record
        {
            public int Index { get; set; }
            public int ActorId { get; set; }
            public string Kind { get; set; } = "";
            public int ParamInt { get; set; }
            public int ParamInt2 { get; set; }
            public string? ParamString { get; set; }
        }
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.QueueTrigger), new[] { typeof(CharacterState), typeof(CharacterTriggerData.Trigger), typeof(CharacterState), typeof(bool), typeof(bool), typeof(CharacterState.FireTriggersData), typeof(int), typeof(CharacterTriggerState) })]
        private static class QueuePatch
        {
            private static void Prefix(CharacterState character, CharacterTriggerData.Trigger trigger, CharacterState.FireTriggersData fireTriggersData)
            {
                if (Environment.GetEnvironmentVariable("MT2_PROBE_STATUS_CALLBACKS") != "1" || FullBattleTrace.Active == null ||
                    AllGameManagers.Instance == null || AllGameManagers.Instance.GetSaveManager().PreviewMode) return;
                string kind = trigger.ToString();
                if (!Kinds.Contains(kind)) return;
                Records.Add(new Record { Index = Records.Count, ActorId = FullBattleTrace.Active.UnitId(character), Kind = kind,
                    ParamInt = fireTriggersData?.paramInt ?? 0, ParamInt2 = fireTriggersData?.paramInt2 ?? 0, ParamString = fireTriggersData?.paramString });
            }
        }
    }
}

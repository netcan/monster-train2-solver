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
        internal static readonly List<FireRecord> OtherFired = new List<FireRecord>();
        private static FireRecord? current;
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
            public bool CanFire { get; set; }
            public CombatUnit? OverrideTarget { get; set; }
            public bool ActiveBefore { get; set; }
            public bool? ActiveAfter { get; set; }
            public bool FinishedDyingBefore { get; set; }
            public bool? FinishedDyingAfter { get; set; }
            public bool BeingRemovedBefore { get; set; }
            public bool? BeingRemovedAfter { get; set; }
            public int GoldBefore { get; set; }
            public int? GoldAfter { get; set; }
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatState? Actual { get; set; }
            public CombatUnit BeforeUnit { get; set; } = null!;
            public CombatUnit? ActualUnit { get; set; }
            public string[] Interactions { get; set; } = Array.Empty<string>();
            public bool Completed { get; set; }
            public List<Record> Generated { get; set; } = new List<Record>();
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.FireTriggers))]
        private static class FirePatch
        {
            private static void Postfix(CharacterState __instance, CharacterTriggerData.Trigger trigger,
                CharacterState.FireTriggersData fireTriggersData, bool canFireTriggers, bool fromRunningTriggerQueue, ref IEnumerator __result)
            {
                if (Environment.GetEnvironmentVariable("MT2_PROBE_STATUS_CALLBACKS") == "1" && FullBattleTrace.Active != null &&
                    !AllGameManagers.Instance!.GetSaveManager().PreviewMode && fromRunningTriggerQueue &&
                    (Kinds.Contains(trigger.ToString()) || Environment.GetEnvironmentVariable("MT2_PROBE_STATUS_CALLBACK_ACTIONS") == "1"))
                    __result = Wrap(__result, __instance, trigger, fireTriggersData, canFireTriggers);
            }
        }
        private static IEnumerator Wrap(IEnumerator native, CharacterState actor, CharacterTriggerData.Trigger kind, CharacterState.FireTriggersData data, bool canFire)
        {
            SaveManager save = AllGameManagers.Instance!.GetSaveManager();
            FullBattleTrace trace = FullBattleTrace.Active!;
            var interactions = new List<string>();
            // Starting/death callbacks can retain an actor with no current or last room.
            RoomState room = actor.GetCurrentRoom(allowLastKnownRoom: true) ?? AllGameManagers.Instance!.GetRoomManager()!.GetRoom(0);
            FireRecord? record = null;
            FireRecord? parent = current;
            try
            {
                record = new FireRecord { Sequence = trace.NextPhaseSequence(), ActorId = trace.UnitId(actor), Kind = kind.ToString(),
                    ParamInt = data?.paramInt ?? 0, ParamInt2 = data?.paramInt2 ?? 0, ParamString = data?.paramString, GoldBefore = save.GetGold(),
                    Before = trace.Capture(room), BeforeUnit = trace.CaptureUnit(actor, interactions), CanFire = canFire,
                    OverrideTarget = data?.overrideTargetCharacter == null ? null : trace.CaptureUnit(data.overrideTargetCharacter, interactions),
                    ActiveBefore = Active(actor), FinishedDyingBefore = actor.HasFinishedDying, BeingRemovedBefore = actor.IsBeingRemoved() };
                (Kinds.Contains(kind.ToString()) ? Fired : OtherFired).Add(record);
                current = record;
            }
            catch (Exception error) { trace.CaptureFailure(error); }
            try { while (native.MoveNext()) yield return native.Current; if (record != null) record.Completed = true; }
            finally
            {
                (native as IDisposable)?.Dispose();
                current = parent;
                if (record != null)
                {
                    try
                    {
                        record.GoldAfter = save.GetGold();
                        record.Actual = trace.Capture(room); record.ActualUnit = trace.CaptureUnit(actor, interactions);
                        record.ActiveAfter = Active(actor); record.FinishedDyingAfter = actor.HasFinishedDying; record.BeingRemovedAfter = actor.IsBeingRemoved();
                        record.Interactions = interactions.Distinct().ToArray();
                    }
                    catch (Exception error) { trace.CaptureFailure(error); }
                }
            }
        }
        private static bool Active(CharacterState actor)
        {
            ICharacterManager manager = actor.GetCharacterManager();
            for (int index = 0; index < manager.GetNumCharacters(); index++) if (ReferenceEquals(manager.GetCharacter(index), actor)) return true;
            return false;
        }
        [HarmonyPatch(typeof(CharacterState), "RemoveCharacterInner")]
        private static class RemovalPatch
        {
            private static void Postfix(CharacterState __instance, bool death, bool vanishUnit, List<CharacterState> activeList,
                bool ignoreTriggers, ref IEnumerator __result)
            {
                if (Environment.GetEnvironmentVariable("MT2_PROBE_STATUS_CALLBACK_ACTIONS") == "1" && FullBattleTrace.Active != null &&
                    !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    __result = ObserveRemoval(__result, __instance, death, vanishUnit, activeList, ignoreTriggers);
            }
        }
        private static IEnumerator ObserveRemoval(IEnumerator native, CharacterState actor, bool death, bool vanish,
            List<CharacterState> active, bool ignore)
        {
            RemovalLog(actor, "begin", death, vanish, active.Contains(actor), ignore);
            try { while (native.MoveNext()) yield return native.Current; }
            finally { (native as IDisposable)?.Dispose(); RemovalLog(actor, "end", death, vanish, active.Contains(actor), ignore); }
        }
        private static void RemovalLog(CharacterState actor, string phase, bool death, bool vanish, bool active, bool ignore)
        {
            try
            {
                UnityEngine.Debug.Log("POJU-REMOVAL " + phase + " actor=" + FullBattleTrace.Active!.UnitId(actor) +
                    " death=" + death + " vanish=" + vanish + " active=" + active + " ignore=" + ignore +
                    " hp=" + actor.GetHP() + " finished=" + actor.HasFinishedDying + " removing=" + actor.IsBeingRemoved() +
                    " gold=" + AllGameManagers.Instance!.GetSaveManager().GetGold());
            }
            catch (Exception error) { FullBattleTrace.Active?.CaptureFailure(error); }
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
                var record = new Record { Index = Records.Count, ActorId = FullBattleTrace.Active.UnitId(character), Kind = kind,
                    ParamInt = fireTriggersData?.paramInt ?? 0, ParamInt2 = fireTriggersData?.paramInt2 ?? 0, ParamString = fireTriggersData?.paramString };
                current?.Generated.Add(record);
                if (Kinds.Contains(kind)) Records.Add(record);
            }
        }
    }
}

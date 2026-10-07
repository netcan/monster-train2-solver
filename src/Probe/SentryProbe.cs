using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class SentryProbe
    {
        internal sealed class Record
        {
            public int Sequence { get; set; }
            public RoomCombatState Before { get; set; } = null!;
            public CombatUnit Actor { get; set; } = null!;
            public CombatUnit Target { get; set; } = null!;
            public bool CanFire { get; set; }
            public RoomCombatResult Predicted { get; set; } = null!;
            public RoomCombatState? Actual { get; set; }
            public CombatUnit? ActualActor { get; set; }
            public CombatUnit? ActualTarget { get; set; }
            public string? Difference { get; set; }
            public bool Completed { get; set; }
        }
        internal static readonly List<Record> Records = new List<Record>();
        internal static int Mismatches => Records.Count(item => item.Difference != null);
        internal static int Unsupported => Records.Count(item => !item.Predicted.Supported);
        internal static int Pending => Records.Count(item => !item.Completed || item.Actual == null);
        private static CombatUnit Capture(FullBattleTrace trace, CharacterState unit)
        {
            using (new CharacterState.SetAllowDestroyedAccessHelper(unit, onlyIfDestroyed: true)) return trace.CaptureUnit(unit);
        }
        private static IEnumerator Observe(IEnumerator native, CharacterState actor, CharacterState target, bool canFire)
        {
            FullBattleTrace trace = FullBattleTrace.Active!; Record? record = null;
            RoomCombatModel.QueuedCharacterTrigger? queued = null;
            RoomState room = actor.GetCurrentRoom() ?? AllGameManagers.Instance!.GetRoomManager()!.GetRoom(target.GetCurrentRoomIndex());
            try
            {
                record = new Record { Sequence = trace.NextPhaseSequence(), Before = trace.Capture(room), Actor = Capture(trace, actor),
                    Target = Capture(trace, target), CanFire = canFire };
                queued = new RoomCombatModel.QueuedCharacterTrigger(room.GetRoomIndex(), record.Actor, "OnSentry",
                    overrideTarget: record.Target, canFireTriggers: canFire);
                record.Predicted = RoomCombatModel.ApplyQueuedCharacterTrigger(record.Before, queued, _ => { }); Records.Add(record);
            }
            catch (Exception error) { trace.CaptureFailure(error); }
            try { while (native.MoveNext()) yield return native.Current; if (record != null) record.Completed = true; }
            finally
            {
                (native as IDisposable)?.Dispose();
                if (record != null)
                    try
                    {
                        record.Actual = trace.Capture(room); record.ActualActor = Capture(trace, actor); record.ActualTarget = Capture(trace, target);
                        record.Difference = !record.Predicted.Supported ? record.Predicted.UnsupportedReason :
                            JToken.DeepEquals(JToken.FromObject(record.Predicted.State!), JToken.FromObject(record.Actual)) &&
                            JToken.DeepEquals(JToken.FromObject(queued!.Unit), JToken.FromObject(record.ActualActor)) &&
                            JToken.DeepEquals(JToken.FromObject(queued.OverrideTarget!), JToken.FromObject(record.ActualTarget))
                                ? null : "Sentry room/actor/retained target differs";
                    }
                    catch (Exception error) { trace.CaptureFailure(error); }
            }
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.FireTriggers))]
        private static class FirePatch
        {
            private static void Postfix(CharacterState __instance, CharacterTriggerData.Trigger trigger,
                CharacterState.FireTriggersData fireTriggersData, bool canFireTriggers, bool fromRunningTriggerQueue, ref IEnumerator __result)
            {
                if (trigger == CharacterTriggerData.Trigger.OnSentry && fromRunningTriggerQueue && SentryScenario.Prepared &&
                    FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode &&
                    __instance.GetTriggers().Any(state => state.GetTrigger() == trigger))
                    __result = Observe(__result, __instance, fireTriggersData.overrideTargetCharacter, canFireTriggers);
            }
        }
    }
}

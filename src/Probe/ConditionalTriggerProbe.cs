using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class ConditionalTriggerProbe
    {
        internal sealed class Record
        {
            public string Label { get; set; } = "";
            public string Kind { get; set; } = "";
            public int ParamInt { get; set; }
            public int TriggerCount { get; set; } = 1;
            public bool CanFire { get; set; }
            public int[] RequiredStackCounts { get; set; } = Array.Empty<int>();
            public RoomCombatState Before { get; set; } = null!;
            public CombatUnit Actor { get; set; } = null!;
            public CombatUnit? Dying { get; set; }
            public RoomCombatResult Predicted { get; set; } = null!;
            public RoomCombatState? After { get; set; }
            public CombatUnit? AfterActor { get; set; }
            public CombatUnit? AfterDying { get; set; }
            public string? Difference { get; set; }
            public bool Completed { get; set; }
        }
        internal static readonly List<Record> Records = new List<Record>();
        private static CombatUnit Unit(FullBattleTrace trace, CharacterState actor)
        { using (new CharacterState.SetAllowDestroyedAccessHelper(actor, onlyIfDestroyed: true)) return trace.CaptureUnit(actor); }
        private static IEnumerator Observe(IEnumerator native, CharacterState actor, CharacterState? dying,
            CharacterTriggerData.Trigger kind, CharacterState.FireTriggersData? data, bool canFire, int triggerCount)
        {
            FullBattleTrace trace = FullBattleTrace.Active!; Record? record = null;
            RoomCombatModel.QueuedCharacterTrigger? queued = null;
            RoomState room;
            using (new CharacterState.SetAllowDestroyedAccessHelper(actor, onlyIfDestroyed: true))
                room = actor.GetCurrentRoom() ?? AllGameManagers.Instance!.GetRoomManager()!.GetRoom(0);
            try
            {
                record = new Record { Label = ConditionalTriggerScenario.CurrentLabel ?? "natural:" + kind, Kind = kind.ToString(),
                    CanFire = canFire, TriggerCount = triggerCount, ParamInt = data?.paramInt ?? 0, Before = trace.Capture(room), Actor = Unit(trace, actor),
                    Dying = dying == null ? null : Unit(trace, dying), RequiredStackCounts = actor.GetTriggers().Where(state => state.GetTrigger() == kind)
                        .SelectMany(state => state.GetTriggerData().GetRequiredStatusEffects().Concat(state.GetTriggerData().GetRequiredStatusEffectsForDyingCharacter()))
                        .Select(status => status.count).ToArray() };
                queued = new RoomCombatModel.QueuedCharacterTrigger(room.GetRoomIndex(), record.Actor, record.Kind,
                    paramInt: record.ParamInt, dyingCharacter: record.Dying, canFireTriggers: record.CanFire, triggerCount: record.TriggerCount);
                record.Predicted = RoomCombatModel.ApplyQueuedCharacterTrigger(record.Before, queued, _ => { });
                Records.Add(record);
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
                        record.After = trace.Capture(room); record.AfterActor = Unit(trace, actor);
                        record.AfterDying = dying == null ? null : Unit(trace, dying);
                        record.Difference = !record.Predicted.Supported ? record.Predicted.UnsupportedReason :
                            JToken.DeepEquals(JToken.FromObject(record.Predicted.State!), JToken.FromObject(record.After)) &&
                            JToken.DeepEquals(JToken.FromObject(queued!.Unit), JToken.FromObject(record.AfterActor)) &&
                            JToken.DeepEquals(JToken.FromObject(new { Dying = queued.DyingCharacter }), JToken.FromObject(new { Dying = record.AfterDying }))
                                ? null : "Conditional trigger room/actor/dying state differs";
                    }
                    catch (Exception error) { trace.CaptureFailure(error); }
                }
            }
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.FireTriggers))]
        private static class PhasePatch
        {
            private static void Postfix(CharacterState __instance, CharacterTriggerData.Trigger trigger, CharacterState dyingCharacter,
                CharacterState.FireTriggersData fireTriggersData, bool canFireTriggers, bool fromRunningTriggerQueue, int triggerCount, ref IEnumerator __result)
            {
                if (fromRunningTriggerQueue && ConditionalTriggerScenario.Started && FullBattleTrace.Active != null &&
                    !AllGameManagers.Instance!.GetSaveManager().PreviewMode && __instance.GetTriggers().Any(state => state.GetTrigger() == trigger &&
                        (state.GetTriggerData().GetRequiredStatusEffects().Count > 0 || state.GetTriggerData().GetRequiredStatusEffectsForDyingCharacter().Count > 0)))
                    __result = Observe(__result, __instance, dyingCharacter, trigger, fireTriggersData, canFireTriggers, triggerCount);
            }
        }
    }
}

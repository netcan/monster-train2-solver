using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicUnitResourceProbe
    {
        internal static readonly List<Record> Records = new List<Record>();
        internal sealed class Record
        {
            public string Trigger { get; set; } = "";
            public int ParamInt { get; set; }
            public bool Alive { get; set; }
            public bool Placement { get; set; }
            public CombatUnit Actor { get; set; } = null!;
            public CombatContext Before { get; set; } = null!;
            public CombatContext? Predicted { get; set; }
            public CombatContext? Actual { get; set; }
            public string? UnsupportedReason { get; set; }
            public string? Difference { get; set; }
            public string? CaptureError { get; set; }
            public bool Completed { get; set; }
        }
        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.ApplyCharacterTriggerRelicEffect))]
        private static class ManagerPatch
        {
            private static void Postfix(CharacterTriggerData.Trigger trigger, CharacterState characterState, int paramInt, ref IEnumerator __result)
            {
                if ((OriginalUnitResourceScenario.Prepared || OriginalCardDrawOnHealScenario.Prepared) &&
                    FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    __result = Wrap(__result, trigger, characterState, paramInt);
            }
        }
        private static IEnumerator Wrap(IEnumerator native, CharacterTriggerData.Trigger trigger, CharacterState actor, int paramInt)
        {
            Record? record = null;
            try
            {
                try
                {
                    var trace = FullBattleTrace.Active!;
                    bool placement = AllGameManagers.Instance!.GetCombatManager().IsPlacementPhase;
                    var before = trace.CaptureContext();
                    var capturedActor = trace.CaptureUnit(actor);
                    record = new Record { Trigger = trigger.ToString(), ParamInt = paramInt, Alive = actor.IsAlive,
                        Placement = placement, Actor = capturedActor, Before = before };
                    Records.Add(record);
                    if (OriginalCardDrawOnHealScenario.Prepared)
                    {
                        var predicted = RelicCardDrawOnHealModel.Apply(before, capturedActor, trigger.ToString(), placement);
                        record.Predicted = predicted.Context;
                        record.UnsupportedReason = predicted.UnsupportedReason;
                    }
                    else
                    {
                        var predicted = RelicUnitResourceModel.Apply(before, capturedActor, trigger.ToString(), actor.IsAlive, paramInt, placement);
                        record.Predicted = predicted.Context;
                        record.UnsupportedReason = predicted.UnsupportedReason;
                    }
                }
                catch (Exception error)
                {
                    if (record != null) record.CaptureError = error.ToString();
                    FullBattleTrace.Active!.CaptureFailure(error);
                }
                while (native.MoveNext()) yield return native.Current;
                if (record != null) record.Completed = true;
            }
            finally
            {
                (native as IDisposable)?.Dispose();
                if (record != null) try
                {
                    record.Actual = FullBattleTrace.Active!.CaptureContext();
                    if (record.Predicted != null && record.Actual != null &&
                        !JToken.DeepEquals(JToken.FromObject(record.Predicted), JToken.FromObject(record.Actual)))
                        record.Difference = "Native manager callback context differs from the relic model.";
                }
                catch (Exception error) { record.CaptureError = error.ToString(); FullBattleTrace.Active!.CaptureFailure(error); }
            }
        }
    }
}

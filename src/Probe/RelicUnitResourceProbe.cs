using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using MonsterTrain2Poju.Model;

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
            public CombatContext? Actual { get; set; }
            public bool Completed { get; set; }
        }
        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.ApplyCharacterTriggerRelicEffect))]
        private static class ManagerPatch
        {
            private static void Postfix(CharacterTriggerData.Trigger trigger, CharacterState characterState, int paramInt, ref IEnumerator __result)
            {
                if (OriginalUnitResourceScenario.Prepared && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
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
                    record = new Record { Trigger = trigger.ToString(), ParamInt = paramInt, Alive = actor.IsAlive,
                        Placement = AllGameManagers.Instance!.GetCombatManager().IsPlacementPhase, Actor = trace.CaptureUnit(actor), Before = trace.CaptureContext() };
                    Records.Add(record);
                }
                catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
                while (native.MoveNext()) yield return native.Current;
                if (record != null) record.Completed = true;
            }
            finally
            {
                (native as IDisposable)?.Dispose();
                if (record != null) try { record.Actual = FullBattleTrace.Active!.CaptureContext(); }
                    catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
            }
        }
    }
}

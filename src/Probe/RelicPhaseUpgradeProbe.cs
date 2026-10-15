using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicPhaseUpgradeProbe
    {
        internal sealed class Record
        {
            public string Owner { get; set; } = "";
            public string Trigger { get; set; } = "";
            public bool Remove { get; set; }
            public bool Completed { get; set; }
            public CombatUnit Before { get; set; } = null!;
            public CombatUnit? Actual { get; set; }
            public CombatContext ContextBefore { get; set; } = null!;
            public CombatContext? ContextAfter { get; set; }
        }
        internal static readonly List<Record> Records = new List<Record>();
        internal static readonly List<object> Tests = new List<object>();
        [HarmonyPatch(typeof(RelicEffectAddRemoveTempUpgradeOnUnitTrigger), "TestCharacterTriggerEffect")]
        private static class TestPatch
        {
            private static void Postfix(RelicEffectAddRemoveTempUpgradeOnUnitTrigger __instance, CharacterTriggerRelicEffectParams relicEffectParams, bool __result)
            {
                if (!OriginalUnitPhaseUpgradeScenario.Prepared || AllGameManagers.Instance!.GetSaveManager().PreviewMode) return;
                var unit = relicEffectParams.characterState;
                Tests.Add(new { Owner = __instance.SourceRelicState.GetRelicDataID(), Trigger = relicEffectParams.trigger.ToString(), Returned = __result,
                    Team = unit?.GetTeamType().ToString(), Subtypes = unit?.GetSourceCharacterData().GetSubtypes().ToArray(), Alive = unit?.IsAlive, ParamInt = relicEffectParams.paramInt });
            }
        }
        [HarmonyPatch(typeof(RelicEffectAddRemoveTempUpgradeOnUnitTrigger), "ApplyCharacterTriggerEffect")]
        private static class ApplyPatch
        {
            private static void Postfix(RelicEffectAddRemoveTempUpgradeOnUnitTrigger __instance, CharacterTriggerRelicEffectParams relicEffectParams, ref IEnumerator __result)
            {
                if (OriginalUnitPhaseUpgradeScenario.Prepared && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    __result = Wrap(__result, __instance, relicEffectParams);
            }
        }
        private static IEnumerator Wrap(IEnumerator native, RelicEffectAddRemoveTempUpgradeOnUnitTrigger effect, CharacterTriggerRelicEffectParams parameters)
        {
            Record? record = null;
            try
            {
                try
                {
                    var trace = FullBattleTrace.Active!; var data = (RelicEffectData)AccessTools.Field(typeof(RelicEffectBase), "_srcRelicEffectData").GetValue(effect);
                    record = new Record { Owner = effect.SourceRelicState.GetRelicDataID(), Trigger = parameters.trigger.ToString(), Remove = data.GetParamBool(),
                        Before = trace.CaptureUnit(parameters.characterState), ContextBefore = trace.CaptureContext() }; Records.Add(record);
                }
                catch (Exception e) { FullBattleTrace.Active!.CaptureFailure(e); }
                while (native.MoveNext()) yield return native.Current;
                if (record != null) record.Completed = true;
            }
            finally
            {
                (native as IDisposable)?.Dispose();
                if (record != null) try { record.Actual = FullBattleTrace.Active!.CaptureUnit(parameters.characterState); record.ContextAfter = FullBattleTrace.Active!.CaptureContext(); }
                    catch (Exception e) { FullBattleTrace.Active!.CaptureFailure(e); }
            }
        }
    }
}

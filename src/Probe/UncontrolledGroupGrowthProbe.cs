using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace MonsterTrain2Poju.Probe
{
    internal static class UncontrolledGroupGrowthProbe
    {
        internal static readonly List<Record> Records = new List<Record>();

        internal sealed class Record
        {
            public int Sequence { get; set; }
            public string Stage { get; set; } = "OnMonsterTeamTurnBegin";
            public int UnitId { get; set; }
            public string Team { get; set; } = "";
            public int HealthBefore { get; set; }
            public int HealthAfter { get; set; }
            public int UnstableBefore { get; set; }
            public int UnstableAfter { get; set; }
            public bool Completed { get; set; }
            public string? Error { get; set; }
            public string? Difference { get; set; }
        }

        internal static void Reset() => Records.Clear();

        private static IEnumerator Wrap(IEnumerator native, CharacterState unit)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            Record? record = null;
            try
            {
                record = new Record
                {
                    Sequence = trace.NextPhaseSequence(),
                    UnitId = trace.UnitId(unit),
                    Team = unit.GetTeamType().ToString(),
                    HealthBefore = unit.GetHP(),
                    UnstableBefore = unit.GetStatusEffectStacks("unstable")
                };
                Records.Add(record);
            }
            catch (Exception error)
            {
                trace.CaptureFailure(error);
                if (record != null) record.Error = error.ToString();
            }
            try
            {
                while (native.MoveNext()) yield return native.Current;
                if (record != null) record.Completed = true;
            }
            finally
            {
                (native as IDisposable)?.Dispose();
                if (record != null) try
                {
                    record.HealthAfter = unit.GetHP();
                    record.UnstableAfter = unit.GetStatusEffectStacks("unstable");
                    if (record.HealthBefore > record.UnstableBefore &&
                        record.UnstableAfter != record.UnstableBefore + 5)
                        record.Difference = "Native UncontrolledGroup growth did not add five Unstable stacks.";
                }
                catch (Exception error)
                {
                    record.Error = error.ToString();
                    trace.CaptureFailure(error);
                }
            }
        }

        [HarmonyPatch(typeof(StatusEffectUnstableState), "OnTriggered")]
        private static class OnTriggeredPatch
        {
            [HarmonyPostfix]
            private static void Postfix(StatusEffectState.InputTriggerParams inputTriggerParams, ref IEnumerator __result)
            {
                if (MixScenario.UncontrolledGroupMode && MixScenario.Prepared && FullBattleTrace.Active != null &&
                    !AllGameManagers.Instance!.GetSaveManager().PreviewMode && inputTriggerParams.associatedCharacter != null)
                    __result = Wrap(__result, inputTriggerParams.associatedCharacter);
            }
        }
    }
}

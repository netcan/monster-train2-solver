using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class PostCombatHealingProbe
    {
        internal static readonly List<Record> Records = new List<Record>();
        internal sealed class Record
        {
            public int Sequence { get; set; }
            public int[] ActorIds { get; set; } = Array.Empty<int>();
            public int[] CannotAttackOrHeal { get; set; } = Array.Empty<int>();
            public int[] CannotFireTriggers { get; set; } = Array.Empty<int>();
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatResult Predicted { get; set; } = null!;
            public RoomCombatState? Actual { get; set; }
            public string? Difference { get; set; }
        }
        private static IEnumerator Wrap(IEnumerator native, CombatManager manager, RoomState room)
        {
            FullBattleTrace trace = FullBattleTrace.Active!; Record? record = null;
            try
            {
                RoomCombatState before = trace.Capture(room);
                int[] Blocked(string field) => ((IEnumerable)AccessTools.Field(typeof(CombatManager), field).GetValue(manager))
                    .Cast<CharacterState>().Select(trace.UnitId).Where(id => before.Units.Any(unit => unit.Id == id)).ToArray();
                record = new Record { Sequence = trace.NextPhaseSequence(), Before = before,
                    ActorIds = before.Units.OrderBy(unit => unit.Team).Where(unit => unit.Triggers.Any(trigger =>
                        trigger.Kind == "PostCombat" || trigger.Kind == "PostCombatHealing")).Select(unit => unit.Id).ToArray(),
                    CannotAttackOrHeal = Blocked("cannotAttackOrHealCharacters"), CannotFireTriggers = Blocked("cannotFireTriggersCharacters") };
                record.Predicted = RoomCombatModel.ApplyUnitPostCombat(before, record.CannotAttackOrHeal, record.CannotFireTriggers); Records.Add(record);
            }
            catch (Exception error) { trace.CaptureFailure(error); }
            try { while (native.MoveNext()) yield return native.Current; }
            finally
            {
                (native as IDisposable)?.Dispose();
                if (record != null)
                    try
                    {
                        record.Actual = trace.Capture(room); record.Difference = !record.Predicted.Supported ? record.Predicted.UnsupportedReason :
                            JToken.DeepEquals(JToken.FromObject(record.Predicted.State!), JToken.FromObject(record.Actual)) ? null : "Native unit post-combat phase differs.";
                    }
                    catch (Exception error) { trace.CaptureFailure(error); }
            }
        }
        [HarmonyPatch(typeof(CombatManager), "DoUnitPostCombat")]
        private static class PhasePatch
        {
            private static void Postfix(CombatManager __instance, RoomState room, ref IEnumerator __result)
            {
                if (Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "post-combat-healing" && PostCombatHealingScenario.Prepared &&
                    FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode) __result = Wrap(__result, __instance, room);
            }
        }
    }
}

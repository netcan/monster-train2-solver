using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class PreCombatProbe
    {
        internal static readonly List<Record> Records = new List<Record>();
        internal sealed class Record
        {
            public int Sequence { get; set; }
            public CombatTeam Team { get; set; }
            public int[] ActorIds { get; set; } = Array.Empty<int>();
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatResult Predicted { get; set; } = null!;
            public TrainCombatState? Actual { get; set; }
            public string? Difference { get; set; }
        }
        private static JToken Comparable(TrainCombatState state) => JToken.FromObject(new
        {
            state.Rooms, Movement = state.Movement.OrderBy(rule => rule.UnitId), state.EnemySlotsPerRoom, state.Context
        });
        private static IEnumerator Wrap(IEnumerator native, object manager, CombatTeam team)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            Record? record = null;
            try
            {
                TrainCombatState before = trace.CaptureTrain();
                var active = ((IEnumerable)AccessTools.Field(manager.GetType(), team == CombatTeam.Player ? "activeMonsters" : "activeHeroes")
                    .GetValue(manager)).Cast<CharacterState>();
                record = new Record { Sequence = trace.NextPhaseSequence(), Team = team,
                    ActorIds = active.Where(unit => unit.IsAlive && !unit.IsDestroyed).Select(trace.UnitId).ToArray(),
                    Before = before, Predicted = TrainCombatModel.PreCombat(before, team) };
                Records.Add(record);
            }
            catch (Exception error) { trace.CaptureFailure(error); }
            try { while (native.MoveNext()) yield return native.Current; }
            finally
            {
                (native as IDisposable)?.Dispose();
                if (record != null)
                {
                    try
                    {
                        record.Actual = trace.CaptureTrain();
                        record.Difference = !record.Predicted.Supported ? record.Predicted.UnsupportedReason :
                            JToken.DeepEquals(Comparable(record.Predicted.State!), Comparable(record.Actual)) ? null : "Native pre-combat state differs";
                    }
                    catch (Exception error) { trace.CaptureFailure(error); }
                }
            }
        }
        private static bool Enabled() => (Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") is "pre-combat" or "pre-combat-lethal") &&
            PreCombatScenario.Prepared && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode;
        [HarmonyPatch(typeof(MonsterManager), nameof(MonsterManager.PreCombat))]
        private static class PlayerPatch
        { private static void Postfix(MonsterManager __instance, ref IEnumerator __result) { if (Enabled()) __result = Wrap(__result, __instance, CombatTeam.Player); } }
        [HarmonyPatch(typeof(HeroManager), nameof(HeroManager.PreCombat))]
        private static class EnemyPatch
        { private static void Postfix(HeroManager __instance, ref IEnumerator __result) { if (Enabled()) __result = Wrap(__result, __instance, CombatTeam.Enemy); } }
    }
}

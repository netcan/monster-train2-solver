using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class TeamTurnBeginProbe
    {
        internal static readonly List<Record> Records = new List<Record>();
        internal sealed class Record
        {
            public int Sequence { get; set; }
            public CombatTeam Team { get; set; }
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatResult Predicted { get; set; } = null!;
            public RoomCombatState? Actual { get; set; }
            public string? Difference { get; set; }
        }
        private static IEnumerator Wrap(IEnumerator native, RoomState room, CombatTeam team)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            Record? record = null;
            try
            {
                RoomCombatState before = trace.Capture(room);
                record = new Record { Sequence = trace.NextPhaseSequence(), Team = team, Before = before, Predicted = RoomCombatModel.ApplyTeamTurnBegin(before, team) };
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
                        record.Actual = trace.Capture(room);
                        record.Difference = !record.Predicted.Supported ? record.Predicted.UnsupportedReason :
                            JToken.DeepEquals(JToken.FromObject(record.Predicted.State!), JToken.FromObject(record.Actual)) ? null : "Native team turn-start state differs";
                    }
                    catch (Exception error) { trace.CaptureFailure(error); }
                }
            }
        }
        private static bool Enabled() => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "team-turn-begin" &&
            FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode;
        [HarmonyPatch(typeof(HeroManager), nameof(HeroManager.DoOnTeamTurnBeginEffects))]
        private static class EnemyPatch
        { private static void Postfix(RoomState room, ref IEnumerator __result) { if (Enabled()) __result = Wrap(__result, room, CombatTeam.Enemy); } }
        [HarmonyPatch(typeof(MonsterManager), nameof(MonsterManager.DoOnTeamTurnBeginEffects))]
        private static class PlayerPatch
        { private static void Postfix(RoomState room, ref IEnumerator __result) { if (Enabled()) __result = Wrap(__result, room, CombatTeam.Player); } }
    }
}

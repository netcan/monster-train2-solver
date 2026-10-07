using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class UnitTurnBeginProbe
    {
        internal static readonly List<Record> Records = new List<Record>();
        private static Record? active;
        internal sealed class Record
        {
            public int Sequence { get; set; }
            public int ActorId { get; set; }
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatResult Predicted { get; set; } = null!;
            public RoomCombatState? Actual { get; set; }
            public List<int> AttackedTargetIds { get; set; } = new List<int>();
            public string? Difference { get; set; }
        }
        private static IEnumerator Wrap(IEnumerator native, CharacterState actor, int roomIndex)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            RoomState room = AllGameManagers.Instance!.GetRoomManager()!.GetRoom(roomIndex);
            Record? record = null;
            Record? previous = active;
            try
            {
                RoomCombatState before = trace.Capture(room);
                record = new Record { Sequence = trace.NextPhaseSequence(), ActorId = trace.UnitId(actor), Before = before };
                record.Predicted = RoomCombatModel.ApplyUnitTurn(before, record.ActorId);
                Records.Add(record); active = record;
            }
            catch (Exception error) { trace.CaptureFailure(error); }
            try { while (native.MoveNext()) yield return native.Current; }
            finally
            {
                (native as IDisposable)?.Dispose(); active = previous;
                if (record != null)
                {
                    try
                    {
                        record.Actual = trace.Capture(room);
                        record.Difference = !record.Predicted.Supported ? record.Predicted.UnsupportedReason :
                            JToken.DeepEquals(JToken.FromObject(record.Predicted.State!), JToken.FromObject(record.Actual)) ? null : "Native unit turn state differs";
                    }
                    catch (Exception error) { trace.CaptureFailure(error); }
                }
            }
        }
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.RunUnitTurn))]
        private static class TurnPatch
        {
            private static void Postfix(CharacterState attackerState, int roomIndex, ref IEnumerator __result)
            {
                if (Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") is "unit-turn-begin" or "team-turn-begin" or "hit-kill" or "dying-upgrades" or "attack-triggers" or "triggered-status" &&
                    FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    __result = Wrap(__result, attackerState, roomIndex);
            }
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.ApplyDamage))]
        private static class AttackPatch
        {
            private static void Prefix(CharacterState __instance, CharacterState.ApplyDamageParams damageParams)
            {
                if (active == null || damageParams.damageType != Damage.Type.DirectAttack || damageParams.attacker == null ||
                    AllGameManagers.Instance!.GetSaveManager().PreviewMode) return;
                FullBattleTrace trace = FullBattleTrace.Active!;
                if (trace.UnitId(damageParams.attacker) == active.ActorId) active.AttackedTargetIds.Add(trace.UnitId(__instance));
            }
        }
    }
}

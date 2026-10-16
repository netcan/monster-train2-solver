using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicPileStartTurnProbe
    {
        internal sealed class Record
        {
            public int VisibleDeckCount { get; set; }
            public int ApplicationsPerRoom { get; set; }
            public bool Completed { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Predicted { get; set; }
            public TrainCombatState? Actual { get; set; }
            public string? UnsupportedReason { get; set; }
            public string? Difference { get; set; }
            public string? CaptureError { get; set; }
        }

        internal static readonly List<Record> Records = new List<Record>();
        private static bool Active => OriginalFunguyDeckScalingScenario.Acquired && FullBattleTrace.Active != null &&
            !AllGameManagers.Instance!.GetSaveManager().PreviewMode;

        private static Record Begin()
        {
            var record = new Record();
            Records.Add(record);
            try
            {
                FullBattleTrace trace = FullBattleTrace.Active!;
                BattleTurnState captured = trace.CaptureDecision();
                record.Before = captured.Spawn.Train;
                record.VisibleDeckCount = record.Before.Context?.Resources?.VisibleDeckCount ?? -1;
                CombatRelicState? owner = record.Before.Context?.Relics?.SingleOrDefault(relic =>
                    relic.DataId == RelicPileStartTurnModel.DataId);
                int threshold = owner?.ResourceEffects?.SingleOrDefault(resource =>
                    resource.Type == RelicPileStartTurnModel.Effect)?.PileStartTurn?.Threshold ?? 0;
                record.ApplicationsPerRoom = RelicPileStartTurnModel.Applications(record.VisibleDeckCount, threshold);
                TrainSpellResult result = RelicPileStartTurnModel.Apply(record.Before, captured.PlayRules);
                record.UnsupportedReason = result.UnsupportedReason;
                record.Predicted = result.State;
            }
            catch (Exception error)
            {
                record.CaptureError = error.ToString();
            }
            return record;
        }

        [HarmonyPatch(typeof(RelicEffectAddEffectPerPileStartOfTurn), "ApplyEffectTurnPhaseTiming")]
        private static class ApplyPatch
        {
            private static void Postfix(ref IEnumerator __result)
            {
                if (!Active) return;
                try { __result = Wrap(__result, Begin()); }
                catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
            }
        }

        private static IEnumerator Wrap(IEnumerator native, Record record)
        {
            bool completed = false;
            try
            {
                while (native.MoveNext()) yield return native.Current;
                completed = true;
            }
            finally
            {
                (native as IDisposable)?.Dispose();
                try
                {
                    FullBattleTrace trace = FullBattleTrace.Active!;
                    record.Actual = trace.CaptureDecision().Spawn.Train;
                    record.Completed = completed;
                    if (record.Predicted != null)
                        record.Difference = JToken.DeepEquals(Comparable(record.Predicted), Comparable(record.Actual))
                            ? null : "Native relic callback train state differs from the model.";
                    if (!completed)
                        trace.CaptureFailure(new InvalidOperationException("Native deck-scaled relic callback did not complete."));
                }
                catch (Exception error)
                {
                    record.CaptureError = error.ToString();
                }
            }
        }

        private static JToken Comparable(TrainCombatState state) => JToken.FromObject(new
        {
            Rooms = state.Rooms.Select(room => new
            {
                room.RoomIndex, room.Deployment, room.Units, room.ExternalInteractions, room.Preview,
                room.ActiveCompanionBossIds
            }).ToArray(),
            Movement = state.Movement.OrderBy(rule => rule.UnitId).ToArray(),
            state.EnemySlotsPerRoom,
            Context = BattleTurnProbe.ComparableContext(state.Context)
        });
    }
}

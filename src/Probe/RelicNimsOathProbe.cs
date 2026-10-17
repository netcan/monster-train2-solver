using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicNimsOathProbe
    {
        internal sealed class RoomTestRecord
        {
            public int Turn { get; set; }
            public int RoomIndex { get; set; }
            public bool Result { get; set; }
        }

        internal sealed class TimingTestRecord
        {
            public int Turn { get; set; }
            public bool Result { get; set; }
            public RoomTestRecord[] RoomTests { get; set; } = Array.Empty<RoomTestRecord>();
        }

        internal sealed class RangeRecord
        {
            public int Minimum { get; set; }
            public int Maximum { get; set; }
            public string Rng { get; set; } = string.Empty;
            public int Result { get; set; }
        }

        internal sealed class ApplyRecord
        {
            public int Turn { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? After { get; set; }
            public RangeRecord[] Ranges { get; set; } = Array.Empty<RangeRecord>();
            public List<RoomTestRecord> RoomTests { get; } = new();
            public int[] ModelCandidateRooms { get; set; } = Array.Empty<int>();
            public int? ModelSelectedRoom { get; set; }
            public bool ModelSupported { get; set; }
            public string? ModelUnsupportedReason { get; set; }
            public string? Difference { get; set; }
            public bool Completed { get; set; }
            public string? Error { get; set; }
        }

        internal static readonly List<RoomTestRecord> RoomTests = new();
        internal static readonly List<TimingTestRecord> TimingTests = new();
        internal static readonly List<ApplyRecord> Applications = new();
        private static int roomTestStart = -1;
        private static ApplyRecord? currentApply;
        private static bool IsActive(RelicEffectAddEffectStartOfTurn effect) =>
            OriginalNimsOathScenario.Prepared && FullBattleTrace.Active != null &&
            effect.SourceRelicState?.GetRelicDataID() == OriginalNimsOathScenario.OwnerId &&
            !AllGameManagers.Instance!.GetSaveManager().PreviewMode;

        [HarmonyPatch(typeof(RelicEffectAddEffectStartOfTurn), nameof(RelicEffectAddEffectStartOfTurn.TestEffectTurnPhaseTiming))]
        private static class TimingTestPatch
        {
            private static void Prefix(RelicEffectAddEffectStartOfTurn __instance)
            {
                if (IsActive(__instance)) roomTestStart = RoomTests.Count;
            }

            private static void Postfix(RelicEffectAddEffectStartOfTurn __instance, bool __result)
            {
                if (!IsActive(__instance) || roomTestStart < 0) return;
                TimingTests.Add(new TimingTestRecord
                {
                    Turn = AllGameManagers.Instance!.GetCombatManager()!.GetTurnCount(),
                    Result = __result,
                    RoomTests = RoomTests.Skip(roomTestStart).ToArray()
                });
                roomTestStart = -1;
            }
        }

        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.TestEffects))]
        private static class RoomTestPatch
        {
            private static void Postfix(int __1, bool __result)
            {
                if (FullBattleTrace.Active == null || roomTestStart < 0 && currentApply == null) return;
                var record = new RoomTestRecord
                {
                    Turn = AllGameManagers.Instance!.GetCombatManager()!.GetTurnCount(),
                    RoomIndex = __1,
                    Result = __result
                };
                if (currentApply != null) currentApply.RoomTests.Add(record);
                else RoomTests.Add(record);
            }
        }

        [HarmonyPatch(typeof(RelicEffectAddEffectStartOfTurn), nameof(RelicEffectAddEffectStartOfTurn.ApplyEffectTurnPhaseTiming))]
        private static class TimingApplyPatch
        {
            private static void Postfix(RelicEffectAddEffectStartOfTurn __instance, ref IEnumerator __result)
            {
                if (IsActive(__instance)) __result = Observe(__result);
            }
        }

        [HarmonyPatch(typeof(RandomManager), nameof(RandomManager.Range), new[] { typeof(int), typeof(int), typeof(RngId) })]
        private static class RangePatch
        {
            private static void Postfix(int __0, int __1, RngId __2, int __result)
            {
                if (currentApply == null) return;
                currentApply.Ranges = currentApply.Ranges.Append(new RangeRecord
                { Minimum = __0, Maximum = __1, Rng = __2.ToString(), Result = __result }).ToArray();
            }
        }

        private static IEnumerator Observe(IEnumerator native)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            var record = new ApplyRecord
            {
                Turn = AllGameManagers.Instance!.GetCombatManager()!.GetTurnCount(),
                Before = trace.CaptureTrain()
            };
            Applications.Add(record);
            currentApply = record;
            RelicStartTurnStatusResult? predicted = Predict(record, trace);
            try
            {
                object? current;
                while (MoveNext(native, record, trace, out current)) yield return current;
                record.Completed = true;
            }
            finally
            {
                (native as IDisposable)?.Dispose();
                currentApply = null;
                CaptureAfter(record, trace);
                if (predicted?.Supported == true && record.After != null)
                    record.Difference = Same(predicted.State!, record.After)
                        ? null : "Native Nims Oath timing effect differs from the model.";
            }
        }

        private static RelicStartTurnStatusResult? Predict(ApplyRecord record, FullBattleTrace trace)
        {
            try
            {
                EnemySpawnState spawn = trace.CaptureCanonicalSpawn();
                BattlePlayRules playRules = trace.CapturePlayRules(spawn);
                RelicStartTurnStatusResult result = RelicStartTurnStatusModel.Apply(record.Before, playRules.Rooms, record.Turn);
                record.ModelCandidateRooms = result.CandidateRooms.ToArray();
                record.ModelSelectedRoom = result.SelectedRoom;
                record.ModelSupported = result.Supported;
                record.ModelUnsupportedReason = result.UnsupportedReason;
                return result;
            }
            catch (Exception error)
            {
                record.ModelUnsupportedReason = error.ToString();
                trace.CaptureFailure(error);
                return null;
            }
        }

        private static bool Same(TrainCombatState expected, TrainCombatState actual) => JToken.DeepEquals(
            Newtonsoft.Json.Linq.JToken.FromObject(new { expected.Rooms, Context = BattleTurnProbe.ComparableContext(expected.Context) }),
            Newtonsoft.Json.Linq.JToken.FromObject(new { actual.Rooms, Context = BattleTurnProbe.ComparableContext(actual.Context) }));

        private static bool MoveNext(IEnumerator native, ApplyRecord record, FullBattleTrace trace, out object? current)
        {
            try
            {
                bool hasNext = native.MoveNext();
                current = hasNext ? native.Current : null;
                return hasNext;
            }
            catch (Exception error)
            {
                record.Error = error.ToString();
                trace.CaptureFailure(error);
                throw;
            }
        }

        private static void CaptureAfter(ApplyRecord record, FullBattleTrace trace)
        {
            try { record.After = trace.CaptureTrain(); }
            catch (Exception error) { record.Error = error.ToString(); trace.CaptureFailure(error); }
        }
    }
}

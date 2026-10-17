using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicStartTurnHealProbe
    {
        internal sealed class TestRecord
        {
            public string OwnerDataId { get; set; } = "";
            public string Phase { get; set; } = "";
            public int Turn { get; set; }
            public int Counter { get; set; }
            public int MaxTurns { get; set; }
            public bool Returned { get; set; }
        }

        internal sealed class ResetRecord
        {
            public string OwnerDataId { get; set; } = "";
            public int BeforeCounter { get; set; }
            public int AfterCounter { get; set; }
            public bool Completed { get; set; }
        }

        internal sealed class OperationRecord
        {
            public string OwnerDataId { get; set; } = "";
            public int EffectIndex { get; set; }
            public int Turn { get; set; }
            public int SelectedRoom { get; set; }
            public int CounterBefore { get; set; }
            public int CounterAfter { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Actual { get; set; }
            public bool Completed { get; set; }
            public bool ModelSupported { get; set; }
            public string? UnsupportedReason { get; set; }
            public string? Difference { get; set; }
        }

        internal static readonly List<TestRecord> Tests = new List<TestRecord>();
        internal static readonly List<ResetRecord> Resets = new List<ResetRecord>();
        internal static readonly List<OperationRecord> Records = new List<OperationRecord>();
        private static bool Active(RelicEffectHealStartOfTurn effect) =>
            OriginalChampionStartTurnHealScenario.Acquired && FullBattleTrace.Active != null &&
            effect.SourceRelicState?.GetRelicDataID() == OriginalChampionStartTurnHealScenario.OwnerId &&
            !AllGameManagers.Instance!.GetSaveManager().PreviewMode;

        [HarmonyPatch(typeof(RelicEffectHealStartOfTurn), nameof(RelicEffectHealStartOfTurn.TestEffectStartOfBattle))]
        private static class StartBattleTestPatch
        {
            private static void Postfix(RelicEffectHealStartOfTurn __instance, bool __result)
            {
                if (!Active(__instance)) return;
                try { Tests.Add(Test(__instance, "TestEffectStartOfBattle", __result)); }
                catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
            }
        }

        [HarmonyPatch(typeof(RelicEffectHealStartOfTurn), nameof(RelicEffectHealStartOfTurn.ApplyEffectStartOfBattle))]
        private static class StartBattleApplyPatch
        {
            private static void Prefix(RelicEffectHealStartOfTurn __instance)
            {
                if (Active(__instance))
                    AccessTools.Field(__instance.GetType(), "healingTurnCount").SetValue(__instance, 4);
            }

            private static void Postfix(RelicEffectHealStartOfTurn __instance, ref IEnumerator __result)
            {
                if (Active(__instance)) __result = WrapReset(__result, __instance);
            }
        }

        [HarmonyPatch(typeof(RelicEffectHealStartOfTurn), nameof(RelicEffectHealStartOfTurn.TestEffectTurnPhaseTiming))]
        private static class TimingTestPatch
        {
            private static void Postfix(RelicEffectHealStartOfTurn __instance, bool __result)
            {
                if (!Active(__instance)) return;
                try { Tests.Add(Test(__instance, "TestEffectTurnPhaseTiming", __result)); }
                catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
            }
        }

        [HarmonyPatch(typeof(RelicEffectHealStartOfTurn), nameof(RelicEffectHealStartOfTurn.ApplyEffectTurnPhaseTiming))]
        private static class TimingApplyPatch
        {
            private static void Postfix(RelicEffectHealStartOfTurn __instance, ref IEnumerator __result)
            {
                if (Active(__instance)) __result = Wrap(__result, __instance);
            }
        }

        private static TestRecord Test(RelicEffectHealStartOfTurn effect, string phase, bool returned)
        {
            return new TestRecord
            {
                OwnerDataId = effect.SourceRelicState!.GetRelicDataID(), Phase = phase,
                Turn = AllGameManagers.Instance!.GetCombatManager()!.GetTurnCount(),
                Counter = Counter(effect), MaxTurns = (int)AccessTools.Field(effect.GetType(), "healingTurnCountMax").GetValue(effect),
                Returned = returned
            };
        }

        private static IEnumerator WrapReset(IEnumerator native, RelicEffectHealStartOfTurn effect)
        {
            var record = new ResetRecord
            {
                OwnerDataId = effect.SourceRelicState!.GetRelicDataID(), BeforeCounter = Counter(effect)
            };
            Resets.Add(record);
            try
            {
                while (native.MoveNext()) yield return native.Current;
                record.Completed = true;
            }
            finally
            {
                (native as IDisposable)?.Dispose();
                record.AfterCounter = Counter(effect);
            }
        }

        private static IEnumerator Wrap(IEnumerator native, RelicEffectHealStartOfTurn effect)
        {
            OperationRecord? record = null;
            try
            {
                AllGameManagers managers = AllGameManagers.Instance!;
                int turn = managers.GetCombatManager()!.GetTurnCount();
                record = new OperationRecord
                {
                    OwnerDataId = effect.SourceRelicState!.GetRelicDataID(),
                    EffectIndex = effect.SourceRelicState.GetEffects().FindIndex(item => ReferenceEquals(item, effect)),
                    Turn = turn, SelectedRoom = managers.GetRoomManager()!.GetSelectedRoom(),
                    CounterBefore = Counter(effect), Before = FullBattleTrace.Active!.CaptureTrain()
                };
                RelicStartTurnHealResult result = RelicStartTurnHealModel.Apply(record.Before, turn, record.SelectedRoom);
                record.ModelSupported = result.Supported;
                record.UnsupportedReason = result.UnsupportedReason;
                Records.Add(record);
            }
            catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
            try
            {
                while (native.MoveNext()) yield return native.Current;
                if (record != null) record.Completed = true;
            }
            finally
            {
                (native as IDisposable)?.Dispose();
                if (record != null)
                {
                    try
                    {
                        record.Actual = FullBattleTrace.Active!.CaptureTrain();
                        record.CounterAfter = Counter(effect);
                        if (record.ModelSupported)
                        {
                            RelicStartTurnHealResult replay = RelicStartTurnHealModel.Apply(record.Before,
                                record.Turn, record.SelectedRoom);
                            record.Difference = replay.Supported && Same(replay.State!, record.Actual)
                                ? null : replay.UnsupportedReason ?? "Native start-turn heal state differs from the model.";
                        }
                    }
                    catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
                }
            }
        }

        private static bool Same(TrainCombatState expected, TrainCombatState actual) => JToken.DeepEquals(
            JToken.FromObject(new { expected.Rooms, Context = BattleTurnProbe.ComparableContext(expected.Context) }),
            JToken.FromObject(new { actual.Rooms, Context = BattleTurnProbe.ComparableContext(actual.Context) }));

        private static int Counter(RelicEffectHealStartOfTurn effect) =>
            (int)AccessTools.Field(effect.GetType(), "healingTurnCount").GetValue(effect);
    }
}

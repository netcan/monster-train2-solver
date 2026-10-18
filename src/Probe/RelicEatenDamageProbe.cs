using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class RelicEatenDamageProbe
    {
        internal sealed class TestRecord
        {
            public CombatUnit Actor { get; set; } = null!;
            public string Trigger { get; set; } = "";
            public int RoomIndex { get; set; }
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Predicted { get; set; }
            public TrainCombatState? Actual { get; set; }
            public int[] Targets { get; set; } = Array.Empty<int>();
            public int[] CachedTargets { get; set; } = Array.Empty<int>();
            public bool Result { get; set; }
            public string? Difference { get; set; }
            public string? UnsupportedReason { get; set; }
            public string? Error { get; set; }
        }
        internal sealed class ApplicationRecord
        {
            public int[] Targets { get; set; } = Array.Empty<int>();
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Predicted { get; set; }
            public TrainCombatState? Actual { get; set; }
            public bool Completed { get; set; }
            public string? Difference { get; set; }
            public string? UnsupportedReason { get; set; }
            public string? Error { get; set; }
        }
        internal static readonly List<TestRecord> Tests = new List<TestRecord>();
        internal static readonly List<ApplicationRecord> Applications = new List<ApplicationRecord>();
        private static bool Active => OriginalEatenDamageScenario.Prepared && FullBattleTrace.Active != null &&
            !AllGameManagers.Instance!.GetSaveManager().PreviewMode;
        private static int[] Targets(RelicEffectDamageEnemyOnFriendlyTrigger effect) =>
            ((List<CharacterState>)AccessTools.Field(effect.GetType(), "_targets").GetValue(effect))
                .Select(FullBattleTrace.Active!.UnitId).ToArray();

        [HarmonyPatch(typeof(RelicEffectDamageEnemyOnFriendlyTrigger), nameof(RelicEffectDamageEnemyOnFriendlyTrigger.TestCharacterTriggerEffect))]
        private static class TestPatch
        {
            private static void Prefix(CharacterTriggerRelicEffectParams relicEffectParams, out TestRecord? __state)
            {
                __state = null; if (!Active) return;
                var row = new TestRecord(); __state = row; Tests.Add(row);
                try
                {
                    FullBattleTrace trace = FullBattleTrace.Active!;
                    // A spawning callback actor may not yet occupy a room. Register
                    // its identity before freezing the pre-Test context.
                    row.Actor = trace.CaptureUnit(relicEffectParams.characterState); row.Before = trace.CaptureTrain();
                    row.Trigger = relicEffectParams.trigger.ToString();
                    row.RoomIndex = relicEffectParams.characterState.GetSpawnPoint(allowLastKnownSpawnPoint: true)?.GetRoomOwner()?.GetRoomIndex()
                        ?? AllGameManagers.Instance!.GetRoomManager()!.GetSelectedRoom();
                    int owner = row.Before.Context!.Relics!.Select((item, index) => (item, index))
                        .Single(item => item.item.DataId == RelicFriendlyTriggerDamageModel.DataId).index;
                    RelicCardPlayDamageSelection selected = RelicFriendlyTriggerDamageModel.Select(row.Before, row.Actor,
                        row.Trigger, row.RoomIndex, owner, 0);
                    row.Predicted = selected.State; row.Targets = selected.Targets.ToArray(); row.UnsupportedReason = selected.UnsupportedReason;
                }
                catch (Exception error) { row.Error = error.ToString(); FullBattleTrace.Active!.CaptureFailure(error); }
            }
            private static void Postfix(RelicEffectDamageEnemyOnFriendlyTrigger __instance, bool __result, TestRecord? __state)
            {
                if (__state == null) return;
                try
                {
                    __state.Result = __result; __state.CachedTargets = Targets(__instance);
                    __state.Actual = FullBattleTrace.Active!.CaptureTrain();
                    if (__state.Predicted != null && (!JToken.DeepEquals(JToken.FromObject(__state.Predicted), JToken.FromObject(__state.Actual)) ||
                        __result != (__state.Targets.Length > 0) || __result && !__state.Targets.SequenceEqual(__state.CachedTargets)))
                        __state.Difference = "Native OnEaten Test/weakest target differs from model.";
                }
                catch (Exception error) { __state.Error = error.ToString(); FullBattleTrace.Active!.CaptureFailure(error); }
            }
        }
        [HarmonyPatch(typeof(RelicEffectDamageEnemyOnFriendlyTrigger), nameof(RelicEffectDamageEnemyOnFriendlyTrigger.ApplyCharacterTriggerEffect))]
        private static class ApplyPatch
        {
            private static void Postfix(RelicEffectDamageEnemyOnFriendlyTrigger __instance, ref IEnumerator __result)
            { if (Active) __result = Observe(__result, __instance); }
        }
        private static IEnumerator Observe(IEnumerator native, RelicEffectDamageEnemyOnFriendlyTrigger effect)
        {
            var row = new ApplicationRecord(); Applications.Add(row);
            try
            {
                row.Targets = Targets(effect); row.Before = FullBattleTrace.Active!.CaptureTrain();
                TrainCombatResult predicted = RelicFriendlyTriggerDamageModel.ApplySelected(row.Before, row.Targets, deferPhysicalRemoval: true);
                row.Predicted = predicted.State; row.UnsupportedReason = predicted.UnsupportedReason;
            }
            catch (Exception error) { row.Error = error.ToString(); FullBattleTrace.Active!.CaptureFailure(error); }
            try { while (native.MoveNext()) yield return native.Current; row.Completed = true; }
            finally
            {
                (native as IDisposable)?.Dispose();
                try
                {
                    row.Actual = FullBattleTrace.Active!.CaptureTrain();
                    if (row.Predicted != null && !JToken.DeepEquals(JToken.FromObject(row.Predicted), JToken.FromObject(row.Actual)))
                        row.Difference = "Native OnEaten damage/death queue differs from model.";
                }
                catch (Exception error) { row.Error = error.ToString(); FullBattleTrace.Active!.CaptureFailure(error); }
            }
        }
    }
}

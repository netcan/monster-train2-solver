using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalAddSnackOnKillProbe
    {
        internal static readonly List<Record> Records = new List<Record>();
        internal static readonly List<object> Tests = new List<object>();
        private static Record? active;

        internal static bool IsGeneratedCard(int instanceId) => Records.Any(record =>
            record.PredictedAddedCardInstanceIds.Contains(instanceId));

        internal sealed class Record
        {
            public string Trigger { get; set; } = "";
            public int ParamInt { get; set; }
            public int ParamInt2 { get; set; }
            public bool Alive { get; set; }
            public int Notifications { get; set; }
            public CombatUnit Actor { get; set; } = null!;
            public CombatContext Before { get; set; } = null!;
            public CombatContext? Predicted { get; set; }
            public string[] PredictedAddedCardDataIds { get; set; } = Array.Empty<string>();
            public int[] PredictedAddedCardInstanceIds { get; set; } = Array.Empty<int>();
            public CombatContext? Actual { get; set; }
            public string? UnsupportedReason { get; set; }
            public string? Difference { get; set; }
            public string? CaptureError { get; set; }
            public bool Completed { get; set; }
        }

        [HarmonyPatch(typeof(RelicEffectAddBattleCardToHandOnUnitTrigger), "TestCharacterTriggerEffect")]
        private static class TestPatch
        {
            private static void Postfix(RelicEffectAddBattleCardToHandOnUnitTrigger __instance,
                CharacterTriggerRelicEffectParams relicEffectParams, bool __result)
            {
                if (!OriginalAddSnackOnKillScenario.Prepared ||
                    __instance.SourceRelicState.GetRelicDataID() != OriginalAddSnackOnKillScenario.OwnerId ||
                    AllGameManagers.Instance!.GetSaveManager().PreviewMode) return;
                CharacterState actor = relicEffectParams.characterState;
                Tests.Add(new
                {
                    OwnerId = OriginalAddSnackOnKillScenario.OwnerId,
                    Trigger = relicEffectParams.trigger.ToString(),
                    Team = actor.GetTeamType().ToString(),
                    Subtypes = actor.GetSourceCharacterData().GetSubtypes().Select(item => item.Key).ToArray(),
                    Alive = actor.IsAlive,
                    ParamInt = relicEffectParams.paramInt,
                    Returned = __result
                });
            }
        }

        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.ApplyCharacterTriggerRelicEffect))]
        private static class ManagerPatch
        {
            private static void Postfix(CharacterTriggerData.Trigger trigger, CharacterState characterState,
                int paramInt, int paramInt2, ref IEnumerator __result)
            {
                if (!OriginalAddSnackOnKillScenario.Prepared || FullBattleTrace.Active == null ||
                    AllGameManagers.Instance!.GetSaveManager().PreviewMode) return;
                __result = Wrap(__result, trigger, characterState, paramInt, paramInt2);
            }
        }

        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.NotifyRelicTriggered))]
        private static class NotifyPatch
        {
            private static void Prefix(RelicState triggeredRelic, IRelicEffect triggeredEffect)
            {
                if (active != null && triggeredRelic?.GetRelicDataID() == OriginalAddSnackOnKillScenario.OwnerId &&
                    triggeredEffect is RelicEffectAddBattleCardToHandOnUnitTrigger) active.Notifications++;
            }
        }

        private static IEnumerator Wrap(IEnumerator native, CharacterTriggerData.Trigger trigger,
            CharacterState actor, int paramInt, int paramInt2)
        {
            Record? record = null;
            try
            {
                try
                {
                    FullBattleTrace trace = FullBattleTrace.Active!;
                    CombatContext before = trace.CaptureContext();
                    CombatUnit capturedActor = trace.CaptureUnit(actor);
                    CardGenerationResult predicted = RelicUnitTriggerCardGenerationModel.ApplyOne(before,
                        OriginalAddSnackOnKillScenario.OwnerId, 0, capturedActor, trigger.ToString(), actor.IsAlive,
                        paramInt, AllGameManagers.Instance!.GetCombatManager()!.IsPlacementPhase,
                        AllGameManagers.Instance!.GetSaveManager().PreviewMode);
                    record = new Record
                    {
                        Trigger = trigger.ToString(), ParamInt = paramInt, ParamInt2 = paramInt2,
                        Alive = actor.IsAlive, Actor = capturedActor, Before = before,
                        Predicted = predicted.Context,
                        PredictedAddedCardDataIds = predicted.AddedCards.Select(card => card.DataId).ToArray(),
                        PredictedAddedCardInstanceIds = predicted.AddedCards.Select(card => card.InstanceId).ToArray(),
                        UnsupportedReason = predicted.UnsupportedReason
                    };
                    Records.Add(record);
                }
                catch (Exception error)
                {
                    if (record != null) record.CaptureError = error.ToString();
                    FullBattleTrace.Active!.CaptureFailure(error);
                }
                while (true)
                {
                    active = record;
                    bool moved = native.MoveNext();
                    if (!moved) break;
                    yield return native.Current;
                }
                if (record != null) record.Completed = true;
            }
            finally
            {
                active = null;
                (native as IDisposable)?.Dispose();
                if (record != null) try
                {
                    CombatContext actual = FullBattleTrace.Active!.CaptureContext();
                    record.Actual = actual;
                    if (record.Predicted != null && !JToken.DeepEquals(JToken.FromObject(record.Predicted), JToken.FromObject(actual)))
                        record.Difference = "Native manager callback context differs from the AddSnackOnKill model.";
                }
                catch (Exception error)
                {
                    record.CaptureError = error.ToString();
                    FullBattleTrace.Active!.CaptureFailure(error);
                }
            }
        }
    }
}

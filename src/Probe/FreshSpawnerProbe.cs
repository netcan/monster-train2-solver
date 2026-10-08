using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class FreshSpawnerProbe
    {
        private static bool inFallback;
        internal sealed class Record
        {
            public CardCreationRule Creation { get; set; } = null!;
            public CombatContext Before { get; set; } = null!;
            public CombatContext? After { get; set; }
            public int CardId { get; set; }
            public bool Completed { get; set; }
            public string? Difference { get; set; }
        }
        internal static readonly List<Record> Records = new List<Record>();
        internal sealed class Check
        {
            public CombatContext Before { get; set; } = null!;
            public CombatContext? After { get; set; }
            public bool Completed { get; set; }
            public string? Difference { get; set; }
        }
        internal static readonly List<Check> Checks = new List<Check>();
        private static IEnumerator ObserveCheck(IEnumerator native)
        {
            var trace = FullBattleTrace.Active!;
            var record = new Check { Before = trace.CaptureContext() }; Checks.Add(record);
            var predicted = UnitStandbyModel.ReturnReady(record.Before);
            try { while (native.MoveNext()) yield return native.Current; record.Completed = true; }
            finally
            {
                (native as IDisposable)?.Dispose(); record.After = trace.CaptureContext();
                record.Difference = JToken.DeepEquals(JToken.FromObject(predicted), JToken.FromObject(record.After)) ? null : "Global unit standby check differs";
            }
        }
        [HarmonyPatch(typeof(CardManager), "CheckStandByConditions")]
        private static class CheckPatch
        {
            private static void Postfix(CharacterState character, CardState onlyCardToCheck, ref IEnumerator __result)
            {
                if (MultiSummonScenario.FreshSources && FullBattleTrace.Active != null && ReferenceEquals(character, null) &&
                    onlyCardToCheck == null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode) __result = ObserveCheck(__result);
            }
        }
        private static IEnumerator Observe(IEnumerator native)
        {
            try
            {
                while (true)
                {
                    bool previous = inFallback, next; object? current = null;
                    try { inFallback = true; next = native.MoveNext(); if (next) current = native.Current; }
                    finally { inFallback = previous; }
                    if (!next) break;
                    yield return current;
                }
            }
            finally { (native as IDisposable)?.Dispose(); }
        }
        [HarmonyPatch(typeof(CardEffectSpawnMonster), nameof(CardEffectSpawnMonster.ApplyEffect))]
        private static class EffectPatch
        {
            private static void Postfix(CardEffectState cardEffectState, ref IEnumerator __result)
            {
                if (MultiSummonScenario.FreshSources && FullBattleTrace.Active != null && cardEffectState.GetParamBool() &&
                    !AllGameManagers.Instance!.GetSaveManager().PreviewMode) __result = Observe(__result);
            }
        }
        [HarmonyPatch(typeof(CardState), nameof(CardState.Setup))]
        private static class SetupPatch
        {
            private static void Prefix(CardData setCardData, out Record? __state)
            {
                __state = null;
                if (!inFallback || FullBattleTrace.Active == null) return;
                __state = new Record { Creation = CardGenerationProbe.Creation(setCardData), Before = FullBattleTrace.Active.CaptureContext() };
                Records.Add(__state);
            }
            private static void Postfix(CardState __instance, Record? __state)
            {
                if (__state == null) return;
                __state.CardId = FullBattleTrace.Active!.CardId(__instance); __state.After = FullBattleTrace.Active.CaptureContext(); __state.Completed = true;
                var result = CardGenerationModel.CreateDetached(__state.Before, __state.Creation);
                __state.Difference = !result.Supported ? result.UnsupportedReason : result.AddedCards[0].InstanceId != __state.CardId ||
                    !JToken.DeepEquals(JToken.FromObject(result.Context!), JToken.FromObject(__state.After)) ? "Fresh detached setup differs" : null;
            }
        }
    }
}

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
    internal static class PoolSummonProbe
    {
        private static bool inEffect;
        internal sealed class Record
        {
            public string[] Pool { get; set; } = null!;
            public string? Selected { get; set; }
            public CombatContext Before { get; set; } = null!;
            public CombatContext? After { get; set; }
            public bool Completed { get; set; }
            public string? Difference { get; set; }
        }
        internal static readonly List<Record> Records = new List<Record>();
        private static IEnumerator Observe(IEnumerator native)
        {
            try
            {
                while (true)
                {
                    bool previous = inEffect, next; object? current = null;
                    try { inEffect = true; next = native.MoveNext(); if (next) current = native.Current; }
                    finally { inEffect = previous; }
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
                if (MultiSummonScenario.Pooled && FullBattleTrace.Active != null && cardEffectState.GetParamCharacterDataPool()?.Count > 0 &&
                    !AllGameManagers.Instance!.GetSaveManager().PreviewMode) __result = Observe(__result);
            }
        }
        [HarmonyPatch]
        private static class SelectionPatch
        {
            private static MethodBase TargetMethod() => typeof(IEnumerableUtility).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(method => method.Name == "RandomElement" && method.IsGenericMethodDefinition &&
                    method.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(List<>))
                .MakeGenericMethod(typeof(CharacterData));
            private static void Prefix(object __0, RngId __1, out Record? __state)
            {
                __state = null;
                if (!inEffect || __1 != RngId.Battle || !(__0 is List<CharacterData> pool) || FullBattleTrace.Active == null) return;
                __state = new Record { Pool = pool.Select(unit => unit.name).ToArray(), Before = FullBattleTrace.Active.CaptureContext() };
                Records.Add(__state);
            }
            private static void Postfix(object __result, Record? __state)
            {
                if (__state == null) return;
                __state.Selected = ((CharacterData)__result).name;
                __state.After = FullBattleTrace.Active!.CaptureContext(); __state.Completed = true;
                RngDraw draw = __state.Before.BattleRng.Range(0, __state.Pool.Length);
                __state.Difference = __state.Pool[draw.Value] == __state.Selected &&
                    JToken.DeepEquals(JToken.FromObject(__state.Before.WithBattleRng(draw.State)), JToken.FromObject(__state.After))
                    ? null : "Pooled character selection or complete RNG context differs";
            }
        }
    }
}

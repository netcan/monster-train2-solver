using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class TriggeredHealingProbe
    {
        internal static readonly List<Record> Records = new List<Record>();
        private static Record? current;
        internal sealed class Record
        {
            public int ActorId { get; set; }
            public string TriggerKind { get; set; } = "";
            public CardActionEffect Effect { get; set; } = null!;
            public int[] Targets { get; set; } = Array.Empty<int>();
            public UnityRng BeforeRng { get; set; }
            public UnityRng SampledRng { get; set; }
            public int Amount { get; set; }
            public bool Sampled { get; set; }
            public bool Completed { get; set; }
            public List<Request> Requests { get; set; } = new List<Request>();
            public string? Difference { get; set; }
        }
        internal sealed class Request
        {
            public int TargetId { get; set; }
            public int Amount { get; set; }
            public int Health { get; set; }
            public int MaxHealth { get; set; }
            public bool CanBeHealed { get; set; }
            public CombatStatus[] Statuses { get; set; } = Array.Empty<CombatStatus>();
            public int? AfterHealth { get; set; }
        }
        private static UnityRng Rng()
        { uint[] words = RngCalibration.Words(RandomManager.GetState(RngId.Battle)); return new UnityRng(words[0], words[1], words[2], words[3]); }
        private static bool Enabled() => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "triggered-healing" &&
            TriggeredHealingScenario.Prepared && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode;
        private static IEnumerator Wrap(IEnumerator native, CardEffectState effect, CardEffectParams parameters)
        {
            Record? parent = current;
            var record = new Record { ActorId = FullBattleTrace.Active!.UnitId(parameters.selfTarget!),
                TriggerKind = parameters.sourceCharacterTriggerState?.GetTrigger().ToString() ?? "",
                Effect = UnitTriggerActionProbe.Capture(effect)!, Targets = parameters.targets.Select(FullBattleTrace.Active.UnitId).ToArray() };
            Records.Add(record); current = record;
            try { while (native.MoveNext()) yield return native.Current; }
            finally { (native as IDisposable)?.Dispose(); record.Completed = true; current = parent; }
        }
        private static IEnumerator Heal(IEnumerator native, CharacterState unit, Request request)
        {
            try { while (native.MoveNext()) yield return native.Current; }
            finally { (native as IDisposable)?.Dispose(); request.AfterHealth = unit.GetHP(); }
        }
        [HarmonyPatch(typeof(CardEffectHeal), nameof(CardEffectHeal.ApplyEffect))]
        private static class EffectPatch
        {
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            { if (Enabled() && cardEffectParams.selfTarget != null) __result = Wrap(__result, cardEffectState, cardEffectParams); }
        }
        [HarmonyPatch(typeof(CardEffectState), nameof(CardEffectState.GetIntInRange))]
        private static class SamplePatch
        {
            private static void Prefix(CardEffectState __instance, out Record? __state)
            {
                __state = current != null && !current.Sampled && __instance.GetCardEffect() is CardEffectHeal ? current : null;
                if (__state != null) __state.BeforeRng = Rng();
            }
            private static void Postfix(int __result, Record? __state)
            {
                if (__state == null) return;
                __state.Amount = __result; __state.SampledRng = Rng(); __state.Sampled = true;
                RngDraw? predicted = __state.Effect.Range?.Sample(__state.BeforeRng);
                if ((predicted?.Value ?? __state.Effect.Value) != __result || !(predicted?.State ?? __state.BeforeRng).Equals(__state.SampledRng))
                    __state.Difference = "Native triggered healing sample differs.";
            }
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.ApplyHeal))]
        private static class HealPatch
        {
            private static void Postfix(CharacterState __instance, int amount, bool fromMaxHPChange, ref IEnumerator __result)
            {
                if (current == null || fromMaxHPChange || !Enabled()) return;
                FullBattleTrace trace = FullBattleTrace.Active!;
                CombatUnit unit = trace.Capture(__instance.GetCurrentRoom()).Units.Single(character => character.Id == trace.UnitId(__instance));
                var request = new Request { TargetId = unit.Id, Amount = amount, Health = unit.Health, MaxHealth = unit.MaxHealth,
                    CanBeHealed = unit.Modifiers!.CanBeHealed, Statuses = unit.Statuses.ToArray() };
                current.Requests.Add(request); __result = Heal(__result, __instance, request);
            }
        }
    }
}

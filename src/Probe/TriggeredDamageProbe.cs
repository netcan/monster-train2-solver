using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class TriggeredDamageProbe
    {
        internal static readonly List<Record> Records = new List<Record>();
        private static Record? current;
        internal sealed class Record
        {
            internal CardEffectState Native = null!;
            public string Stage { get; set; } = "";
            public int ActorId { get; set; }
            public int ActorCardId { get; set; }
            public string TriggerKind { get; set; } = "";
            public CardActionEffect Effect { get; set; } = null!;
            public string? StatusMultiplier { get; set; }
            public int MultiplierStacks { get; set; }
            public CombatStatus[] ActorStatuses { get; set; } = Array.Empty<CombatStatus>();
            public bool ActorPiercing { get; set; }
            public int[] Targets { get; set; } = Array.Empty<int>();
            public UnityRng BeforeRng { get; set; }
            public UnityRng SampledRng { get; set; }
            public int Amount { get; set; }
            public bool Sampled { get; set; }
            public bool Completed { get; set; }
            public bool? TestPassed { get; set; }
            public List<Request> Requests { get; set; } = new List<Request>();
            public string? Difference { get; set; }
        }
        internal sealed class Request
        {
            public int TargetId { get; set; }
            public int Amount { get; set; }
            public CombatUnit Before { get; set; } = null!;
            public CombatContext Context { get; set; } = null!;
            public int? AfterHealth { get; set; }
            public bool AttackerPreserved { get; set; }
            public bool DefaultDamage { get; set; }
            public bool NullSourceCard { get; set; }
            public int SourceCardId { get; set; }
        }
        private static bool Enabled() => (Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "triggered-damage" && TriggeredDamageScenario.Prepared ||
            Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "damage-death-queue" && DamageDeathQueueScenario.Prepared) && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode;
        private static UnityRng Rng()
        { uint[] words = RngCalibration.Words(RandomManager.GetState(RngId.Battle)); return new UnityRng(words[0], words[1], words[2], words[3]); }
        private static Record Create(CardEffectState effect, CardEffectParams parameters, string stage)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            CharacterState actor = parameters.selfTarget!;
            CombatUnit? unit = trace.Capture(actor.GetCurrentRoom()).Units.FirstOrDefault(candidate => candidate.Id == trace.UnitId(actor));
            string? multiplier = effect.GetUseStatusEffectStackMultiplier() ? effect.GetStatusEffectStackMultiplier() : null;
            CardState owner = actor.GetSpawnerCard();
            return new Record { Native = effect, Stage = stage, ActorId = trace.UnitId(actor), ActorCardId = owner == null ? 0 : trace.CardId(owner),
                TriggerKind = parameters.sourceCharacterTriggerState?.GetTrigger().ToString() ?? "",
                Effect = UnitTriggerActionProbe.Capture(effect)!, StatusMultiplier = multiplier,
                MultiplierStacks = multiplier == null ? 1 : actor.GetStatusEffectStacks(multiplier), ActorStatuses = unit?.Statuses.ToArray() ?? Array.Empty<CombatStatus>(),
                ActorPiercing = actor.HasStatusEffect("piercing"),
                Targets = parameters.targets.Select(trace.UnitId).ToArray() };
        }
        private static IEnumerator Wrap(IEnumerator native, CardEffectState effect, CardEffectParams parameters)
        {
            Record? parent = current; Record record = Create(effect, parameters, "Application"); Records.Add(record); current = record;
            try { while (native.MoveNext()) yield return native.Current; }
            finally { (native as IDisposable)?.Dispose(); record.Completed = true; current = parent; }
        }
        private static IEnumerator Apply(IEnumerator native, CharacterState unit, Request request)
        {
            try { while (native.MoveNext()) yield return native.Current; }
            finally { (native as IDisposable)?.Dispose(); request.AfterHealth = unit.IsDestroyed ? 0 : unit.GetHP(); }
        }
        [HarmonyPatch(typeof(CardEffectDamage), nameof(CardEffectDamage.ApplyEffect))]
        private static class EffectPatch
        {
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            { if (Enabled() && cardEffectParams.selfTarget != null) __result = Wrap(__result, cardEffectState, cardEffectParams); }
        }
        [HarmonyPatch(typeof(CardEffectDamage), nameof(CardEffectDamage.TestEffect))]
        private static class TestPatch
        {
            private static void Prefix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, out Record? __state)
            {
                __state = current;
                if (!Enabled() || cardEffectParams.selfTarget == null) return;
                current = Create(cardEffectState, cardEffectParams, "Test"); Records.Add(current);
            }
            private static void Postfix(bool __result, Record? __state)
            {
                if (current != __state && current?.Stage == "Test")
                {
                    current.TestPassed = __result; current.Completed = true;
                    bool predicted = current.Amount >= 0 && (current.Effect.Range == null || current.Effect.Range.Max > 0) &&
                        (current.Effect.Target != "DropTargetCharacter" || current.Targets.Length > 0);
                    if (predicted != __result) current.Difference = "Native triggered damage test differs.";
                }
                current = __state;
            }
        }
        [HarmonyPatch(typeof(CardEffectState), nameof(CardEffectState.GetIntInRange))]
        private static class SamplePatch
        {
            private static void Prefix(CardEffectState __instance, out Record? __state)
            {
                __state = current != null && !current.Sampled && ReferenceEquals(current.Native, __instance) ? current : null;
                if (__state != null) __state.BeforeRng = Rng();
            }
            private static void Postfix(int __result, Record? __state)
            {
                if (__state == null) return;
                __state.Amount = __result; __state.SampledRng = Rng(); __state.Sampled = true;
                RngDraw? draw = __state.Effect.Range?.Sample(__state.BeforeRng);
                if ((draw?.Value ?? __state.Effect.Value) != __result || !(draw?.State ?? __state.BeforeRng).Equals(__state.SampledRng))
                    __state.Difference = "Native triggered damage quantity/RNG differs.";
            }
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.ApplyDamage))]
        private static class DamagePatch
        {
            private static void Postfix(CharacterState __instance, int damage, CharacterState.ApplyDamageParams damageParams, ref IEnumerator __result)
            {
                if (current?.Stage != "Application" || !Enabled()) return;
                FullBattleTrace trace = FullBattleTrace.Active!;
                CombatUnit target = trace.Capture(__instance.GetCurrentRoom()).Units.Single(unit => unit.Id == trace.UnitId(__instance));
                var request = new Request { TargetId = target.Id, Amount = damage, Before = target, Context = trace.CaptureContext(),
                    AttackerPreserved = damageParams.attacker != null && trace.UnitId(damageParams.attacker) == current.ActorId,
                    DefaultDamage = damageParams.damageType == Damage.Type.Default, NullSourceCard = damageParams.damageSourceCard == null,
                    SourceCardId = damageParams.damageSourceCard == null ? 0 : trace.CardId(damageParams.damageSourceCard) };
                current.Requests.Add(request); __result = Apply(__result, __instance, request);
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class MissingHealthStatusProbe
    {
        internal static readonly List<Record> Records = new List<Record>();
        private static Record? current;
        internal sealed class Record
        {
            internal CardEffectState Native = null!;
            public string NativeType { get; set; } = "";
            public int SourceCardId { get; set; }
            public int ActorId { get; set; }
            public bool ActivatedAbility { get; set; }
            public int Value { get; set; }
            public bool AllFloors { get; set; }
            public bool Bool2 { get; set; }
            public CombatStatus[] Statuses { get; set; } = Array.Empty<CombatStatus>();
            public int[] Targets { get; set; } = Array.Empty<int>();
            public TrainCombatState Before { get; set; } = null!;
            public TrainCombatState? Actual { get; set; }
            public int? NativeTotalMissingHealth { get; set; }
            public int? SampledCoefficient { get; set; }
            public bool Completed { get; set; }
        }
        private static bool Enabled => OriginalMissingHealthStatusScenario.Prepared && FullBattleTrace.Active != null &&
            !AllGameManagers.Instance!.GetSaveManager().PreviewMode;
        private static IEnumerator Wrap(IEnumerator native, CardEffectState effect, CardEffectParams parameters)
        {
            FullBattleTrace trace = FullBattleTrace.Active!; Record? parent = current; Record? record = null;
            try
            {
                var actor = parameters.characterThatActivatedAbility ?? parameters.selfTarget;
                record = new Record { Native = effect, NativeType = effect.GetCardEffect().GetType().Name,
                    SourceCardId = parameters.playedCard == null ? 0 : trace.CardId(parameters.playedCard),
                    ActorId = actor == null ? 0 : trace.UnitId(actor), ActivatedAbility = parameters.characterThatActivatedAbility != null,
                    Value = effect.GetParamInt(), AllFloors = effect.GetParamBool(), Bool2 = effect.GetParamBool2(),
                    Statuses = effect.GetParamStatusEffectStackData().Select(status => BattleActionProbe.Status(status.statusId, status.count)).ToArray(),
                    Targets = parameters.targets.Select(trace.UnitId).ToArray(), Before = trace.CaptureTrain() };
                Records.Add(record); current = record;
            }
            catch (Exception error) { trace.CaptureFailure(error); }
            try { while (native.MoveNext()) yield return native.Current; if (record != null) record.Completed = true; }
            finally
            {
                (native as IDisposable)?.Dispose(); current = parent;
                if (record != null)
                    try { record.Actual = trace.CaptureTrain(); } catch (Exception error) { trace.CaptureFailure(error); }
            }
        }
        [HarmonyPatch(typeof(CardEffectAddStatusEffectPerFriendlyMissingHp), nameof(CardEffectAddStatusEffectPerFriendlyMissingHp.ApplyEffect))]
        private static class FriendlyPatch
        {
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            { if (Enabled) __result = Wrap(__result, cardEffectState, cardEffectParams); }
        }
        [HarmonyPatch(typeof(CardEffectAddStatusEffectPerMissingHp), nameof(CardEffectAddStatusEffectPerMissingHp.ApplyEffect))]
        private static class TargetPatch
        {
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            { if (Enabled) __result = Wrap(__result, cardEffectState, cardEffectParams); }
        }
        [HarmonyPatch(typeof(CardEffectDamagePerStatusEffect), nameof(CardEffectDamagePerStatusEffect.ApplyEffect))]
        private static class DamagePatch
        {
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            { if (Enabled) __result = Wrap(__result, cardEffectState, cardEffectParams); }
        }
        [HarmonyPatch(typeof(CardEffectAddStatusEffectPerFriendlyMissingHp), "GetTotalFriendlyMissingHp")]
        private static class TotalPatch
        {
            private static void Postfix(int __result)
            { if (current?.NativeType == "CardEffectAddStatusEffectPerFriendlyMissingHp") current.NativeTotalMissingHealth = __result; }
        }
        [HarmonyPatch(typeof(CardEffectState), nameof(CardEffectState.GetIntInRange), new Type[] { })]
        private static class CoefficientPatch
        {
            private static void Postfix(CardEffectState __instance, int __result)
            {
                if (current?.NativeType == "CardEffectDamagePerStatusEffect" && ReferenceEquals(current.Native, __instance))
                    current.SampledCoefficient = __result;
            }
        }
    }
}

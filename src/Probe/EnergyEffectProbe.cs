using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class EnergyEffectProbe
    {
        internal static string Type(CardEffectData effect) => effect.GetEffectStateName() == "CardEffectGainEnergy" && effect.GetParamBool()
            ? "GainEnergyMonsterTurn" : effect.GetEffectStateName().Substring("CardEffect".Length);
        internal static readonly List<Record> Records = new List<Record>();
        internal sealed class Record
        {
            public int Sequence { get; set; }
            public string Trigger { get; set; } = "";
            public CardActionEffect Effect { get; set; } = null!;
            public CombatContext Before { get; set; } = null!;
            public CombatContext? After { get; set; }
            public int Amount { get; set; }
            public bool Sampled { get; set; }
            public bool Completed { get; set; }
        }
        private static Record? current;
        [HarmonyPatch]
        private static class ApplyPatch
        {
            private static IEnumerable<MethodBase> TargetMethods() => new[] { typeof(CardEffectGainEnergy), typeof(CardEffectAdjustEnergy),
                typeof(CardEffectGainEnergyNextTurn), typeof(CardEffectGainEnergyEveryTurn) }.Select(type => AccessTools.Method(type, "ApplyEffect"));
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            {
                if (EnergyScenario.Prepared && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    __result = Observe(__result, cardEffectState, cardEffectParams);
            }
        }
        private static IEnumerator Observe(IEnumerator native, CardEffectState effect, CardEffectParams parameters)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            Record? parent = current;
            var record = new Record { Sequence = trace.NextPhaseSequence(), Effect = UnitTriggerActionProbe.Capture(effect)!,
                Trigger = parameters.sourceCharacterTriggerState?.GetTrigger().ToString() ?? "", Before = trace.CaptureContext() };
            Records.Add(record); current = record;
            try { while (native.MoveNext()) yield return native.Current; record.Completed = true; }
            finally { (native as IDisposable)?.Dispose(); current = parent; record.After = trace.CaptureContext(); }
        }
        [HarmonyPatch(typeof(CardEffectState), nameof(CardEffectState.GetIntInRange))]
        private static class SamplePatch
        {
            private static void Postfix(CardEffectState __instance, int __result)
            {
                if (current != null && !current.Sampled && EnergyModel.IsNativeEffect(__instance.GetCardEffect().GetType().Name))
                { current.Amount = __result; current.Sampled = true; }
            }
        }
    }
}

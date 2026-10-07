using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class TriggeredStatusProbe
    {
        internal static readonly List<Record> Records = new List<Record>();
        internal sealed class Record
        {
            public int Sequence { get; set; }
            public string Kind { get; set; } = "";
            public int ActorId { get; set; }
            public int SourceCardId { get; set; }
            public int[] Targets { get; set; } = Array.Empty<int>();
            public CombatEffect Effect { get; set; } = null!;
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatState? Actual { get; set; }
            public CombatUnit[] BeforeUnits { get; set; } = Array.Empty<CombatUnit>();
            public CombatUnit[]? ActualUnits { get; set; }
            public string[] Interactions { get; set; } = Array.Empty<string>();
            public bool Completed { get; set; }
        }
        private static IEnumerator Wrap(IEnumerator native, CardEffectState effect, CardEffectParams parameters)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            CharacterState actor = parameters.selfTarget!;
            CharacterState[] units = new[] { actor }.Concat(parameters.targets).Distinct().ToArray();
            RoomState room = actor.GetCurrentRoom(allowLastKnownRoom: true);
            var interactions = new List<string>();
            var record = new Record { Sequence = trace.NextPhaseSequence(), ActorId = trace.UnitId(actor),
                Kind = parameters.sourceCharacterTriggerState?.GetTrigger().ToString() ?? "",
                SourceCardId = parameters.playedCard == null ? 0 : trace.CardId(parameters.playedCard),
                Targets = parameters.targets.Select(trace.UnitId).ToArray(),
                Effect = new CombatEffect("CardEffectAddStatusEffect", effect.GetParamInt(), 0, "", 0, Array.Empty<string>(), false,
                    action: UnitTriggerActionProbe.Capture(effect), statusScaling: UnitTriggerActionProbe.Scaling(effect)),
                Before = trace.Capture(room), BeforeUnits = units.Select(unit => trace.CaptureUnit(unit, interactions)).ToArray() };
            Records.Add(record);
            try { while (native.MoveNext()) yield return native.Current; record.Completed = true; }
            finally
            {
                (native as IDisposable)?.Dispose(); record.Actual = trace.Capture(room);
                record.ActualUnits = units.Select(unit => trace.CaptureUnit(unit, interactions)).ToArray(); record.Interactions = interactions.Distinct().ToArray();
            }
        }
        [HarmonyPatch(typeof(CardEffectAddStatusEffect), nameof(CardEffectAddStatusEffect.ApplyEffect))]
        private static class EffectPatch
        {
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            {
                if (Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "triggered-status" && TriggeredStatusScenario.Prepared &&
                    FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode && cardEffectParams.selfTarget != null)
                    __result = Wrap(__result, cardEffectState, cardEffectParams);
            }
        }
    }
}

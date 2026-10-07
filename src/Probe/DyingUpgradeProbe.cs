using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class DyingUpgradeProbe
    {
        internal static readonly List<Record> Records = new List<Record>();
        internal sealed class Record
        {
            public int Sequence { get; set; }
            public string Kind { get; set; } = "";
            public int OwnerCardId { get; set; }
            public CardActionEffect Effect { get; set; } = null!;
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatState? Actual { get; set; }
            public CombatUnit[] BeforeUnits { get; set; } = Array.Empty<CombatUnit>();
            public CombatUnit[]? ActualUnits { get; set; }
            public string[] Interactions { get; set; } = Array.Empty<string>();
            public bool Completed { get; set; }
        }
        private static bool Enabled() => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == "dying-upgrades" &&
            DyingUpgradeScenario.Prepared && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode;
        private static IEnumerator Wrap(IEnumerator native, CardEffectState effect, CardEffectParams parameters)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            CharacterState[] targets = parameters.targets.ToArray();
            RoomState room = parameters.selfTarget!.GetCurrentRoom(allowLastKnownRoom: true);
            var interactions = new List<string>();
            var record = new Record { Sequence = trace.NextPhaseSequence(), Kind = parameters.sourceCharacterTriggerState?.GetTrigger().ToString() ?? "",
                OwnerCardId = parameters.playedCard == null ? 0 : trace.CardId(parameters.playedCard),
                Effect = UnitTriggerUpgradeProbe.Capture(effect, interactions)!, Before = trace.Capture(room),
                BeforeUnits = targets.Select(target => trace.CaptureUnit(target, interactions)).ToArray() };
            Records.Add(record);
            try { while (native.MoveNext()) yield return native.Current; record.Completed = true; }
            finally
            {
                (native as IDisposable)?.Dispose(); record.Actual = trace.Capture(room);
                record.ActualUnits = targets.Select(target => trace.CaptureUnit(target, interactions)).ToArray();
                record.Interactions = interactions.Distinct().ToArray();
            }
        }
        [HarmonyPatch]
        private static class EffectPatch
        {
            private static IEnumerable<MethodBase> TargetMethods() => new[] { typeof(CardEffectAddCardUpgradeToUnits), typeof(CardEffectRemoveTempUpgradeFromUnit) }
                .Select(type => (MethodBase)AccessTools.Method(type, "ApplyEffect"));
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            {
                if (Enabled() && cardEffectParams.selfTarget != null && cardEffectParams.targets.Count > 0)
                    __result = Wrap(__result, cardEffectState, cardEffectParams);
            }
        }
    }
}

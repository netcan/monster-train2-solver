using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class AbilityEffectProbe
    {
        internal sealed class Record
        {
            public CardActionEffect Effect { get; set; } = null!;
            public int SourceCardId { get; set; }
            public int[] Targets { get; set; } = null!;
            public bool RunningTriggerQueue { get; set; }
            public bool HasRelicGate { get; set; }
            public bool Completed { get; set; }
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatState? After { get; set; }
        }
        internal static readonly List<Record> Records = new List<Record>();
        [HarmonyPatch]
        private static class EffectPatch
        {
            private static IEnumerable<MethodBase> TargetMethods() => new[] { typeof(CardEffectSetUnitAbility), typeof(CardEffectRemoveAbility) }
                .Select(type => AccessTools.Method(type, "ApplyEffect"));
            private static void Postfix(CardEffectState cardEffectState, CardEffectParams cardEffectParams, ref IEnumerator __result)
            {
                if (AbilityEffectsScenario.Prepared && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    __result = Observe(__result, cardEffectState, cardEffectParams);
            }
        }
        private static IEnumerator Observe(IEnumerator native, CardEffectState effect, CardEffectParams parameters)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            RoomState room = AllGameManagers.Instance!.GetRoomManager()!.GetRoom(parameters.selectedRoom);
            var record = new Record { Effect = AbilityLifecycleProbe.Capture(effect), SourceCardId = parameters.playedCard == null ? 0 : trace.CardId(parameters.playedCard),
                Targets = parameters.targets.Select(trace.UnitId).ToArray(), RunningTriggerQueue = AllGameManagers.Instance.GetCombatManager()!.IsRunningTriggerQueue,
                HasRelicGate = effect.GetParamRelicData() != null, Before = trace.Capture(room) };
            Records.Add(record);
            try { while (native.MoveNext()) yield return native.Current; record.Completed = true; }
            finally { (native as IDisposable)?.Dispose(); record.After = trace.Capture(room); }
        }
    }
}

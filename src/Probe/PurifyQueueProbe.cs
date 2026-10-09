using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class PurifyQueueProbe
    {
        internal static readonly List<Record> Records = new List<Record>();
        internal sealed class Record
        {
            public int Sequence { get; set; }
            public string Overload { get; set; } = "";
            public int Turn { get; set; }
            public string Kind { get; set; } = "";
            public CombatUnit Actor { get; set; } = null!;
            public bool Purified { get; set; }
            public string[]? PurifyBlockedTriggers { get; set; }
            public string[]? DeploymentBlockedTriggers { get; set; }
            public int QueueBefore { get; set; }
            public int QueueAfter { get; set; }
            public bool Completed { get; set; }
            public string[] Interactions { get; set; } = Array.Empty<string>();
        }
        private static int Count(CombatManager manager) =>
            ((ICollection)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(manager)).Count;
        private static Record? Begin(CombatManager manager, CharacterState actor, CharacterTriggerData.Trigger kind, string overload)
        {
            if (!PurifyScenario.QueueCoverage || FullBattleTrace.Active == null ||
                AllGameManagers.Instance!.GetSaveManager().PreviewMode) return null;
            try
            {
                FullBattleTrace trace = FullBattleTrace.Active;
                var interactions = new List<string>();
                AllGameData? data = AccessTools.Field(typeof(CombatManager), "allGameData").GetValue(manager) as AllGameData;
                BalanceData? balance = data?.GetBalanceData();
                var record = new Record { Sequence = trace.NextPhaseSequence(), Overload = overload, Turn = manager.GetTurnCount(),
                    Kind = kind.ToString(), Actor = trace.CaptureUnit(actor, interactions), Purified = actor.IsPurified(),
                    PurifyBlockedTriggers = balance?.GetDisallowedPurifyCharacterTriggers().Select(item => item.ToString()).ToArray(),
                    DeploymentBlockedTriggers = balance?.GetDisallowedDeploymentPhaseCharacterTriggers().Select(item => item.ToString()).ToArray(),
                    QueueBefore = Count(manager), Interactions = interactions.Distinct().ToArray() };
                Records.Add(record); return record;
            }
            catch (Exception error) { FullBattleTrace.Active.CaptureFailure(error); return null; }
        }
        private static void Finish(CombatManager manager, Record? record)
        {
            if (record == null) return;
            try { record.QueueAfter = Count(manager); record.Completed = true; }
            catch (Exception error) { FullBattleTrace.Active?.CaptureFailure(error); }
        }
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.QueueTrigger), new[] { typeof(CharacterState), typeof(CharacterTriggerData.Trigger), typeof(CharacterState), typeof(bool), typeof(bool), typeof(CharacterState.FireTriggersData), typeof(int), typeof(CharacterTriggerState) })]
        private static class CharacterQueuePatch
        {
            private static void Prefix(CombatManager __instance, CharacterState character, CharacterTriggerData.Trigger trigger, out Record? __state)
                => __state = Begin(__instance, character, trigger, "Character");
            private static void Postfix(CombatManager __instance, Record? __state) => Finish(__instance, __state);
        }
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.QueueTrigger), new[] { typeof(CombatManager.TriggerQueueData) })]
        private static class DataQueuePatch
        {
            private static void Prefix(CombatManager __instance, CombatManager.TriggerQueueData triggerQueueData, out Record? __state)
                => __state = Begin(__instance, triggerQueueData.character, triggerQueueData.trigger, "QueueData");
            private static void Postfix(CombatManager __instance, Record? __state) => Finish(__instance, __state);
        }
    }
}

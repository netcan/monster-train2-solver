using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class RevivalProbe
    {
        internal sealed class Callback
        {
            public int ActorId { get; set; }
            public string Kind { get; set; } = "";
            public int DyingId { get; set; }
            public int ParamInt { get; set; }
            public int TriggerCount { get; set; }
        }
        internal sealed class Record
        {
            public string Label { get; set; } = "";
            public int ActorId { get; set; }
            public int SourceCardId { get; set; }
            public int AttackerId { get; set; }
            public bool QueueRunning { get; set; }
            public int AutomaticQueueDeferrals { get; set; }
            public RoomCombatState Before { get; set; } = null!;
            public RoomCombatState? After { get; set; }
            public CombatUnit? AfterActor { get; set; }
            public List<Callback> Queued { get; set; } = new List<Callback>();
            public bool Completed { get; set; }
            public string? Error { get; set; }
        }
        internal static readonly List<Record> Records = new List<Record>();
        private static Record? current;
        internal static Callback CaptureCallback(CharacterState actor, CharacterTriggerData.Trigger trigger,
            CharacterState? dying, CharacterState.FireTriggersData? data, int count) => new Callback {
                ActorId = FullBattleTrace.Active!.UnitId(actor), Kind = trigger.ToString(), DyingId = dying == null ? 0 : FullBattleTrace.Active!.UnitId(dying),
                ParamInt = data?.paramInt ?? 0, TriggerCount = count };
        private static IEnumerator Observe(IEnumerator native, CharacterState actor, CardState? source, CharacterState? attacker)
        {
            FullBattleTrace trace = FullBattleTrace.Active!; Record? record = null; Record? previous = current;
            RoomState room = actor.GetCurrentRoom(allowLastKnownRoom: true);
            try
            {
                record = new Record { Label = RevivalScenario.Label ?? "natural:revival", ActorId = trace.UnitId(actor),
                    SourceCardId = source == null ? 0 : trace.CardId(source), AttackerId = attacker == null ? 0 : trace.UnitId(attacker),
                    QueueRunning = AllGameManagers.Instance!.GetCombatManager()!.IsRunningTriggerQueue, Before = Before(trace, room, actor) };
                Records.Add(record); current = record;
            }
            catch (Exception error) { trace.CaptureFailure(error); }
            try { while (native.MoveNext()) yield return native.Current; if (record != null) record.Completed = true; }
            finally
            {
                (native as IDisposable)?.Dispose(); current = previous;
                if (record != null)
                {
                    try { record.After = trace.Capture(room); record.AfterActor = trace.CaptureUnit(actor); }
                    catch (Exception error) { record.Error = error.ToString(); trace.CaptureFailure(error); }
                }
            }
        }
        private static RoomCombatState Before(FullBattleTrace trace, RoomState room, CharacterState actor)
        {
            RoomCombatState state = trace.Capture(room);
            var characters = new List<CharacterState>();
            room.AddCharactersToList(characters, Team.Type.Heroes, allowDead: true); room.AddCharactersToList(characters, Team.Type.Monsters, allowDead: true);
            return new RoomCombatState(state.RoomIndex, state.Deployment, characters.Where(unit => unit == actor || unit.IsAlive && !unit.IsDestroyed)
                .Select(unit => trace.CaptureUnit(unit)).ToArray(), state.ExternalInteractions, state.Context, state.Preview);
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.ReviveFromUndyingStatus))]
        private static class RevivePatch
        {
            private static void Postfix(CharacterState __instance, CardState damageSourceCard, CharacterState attacker, ref IEnumerator __result)
            { if (RevivalScenario.Enabled && FullBattleTrace.Active != null && !AllGameManagers.Instance!.GetSaveManager().PreviewMode)
                    __result = Observe(__result, __instance, damageSourceCard, attacker); }
        }
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.QueueTrigger), new[] { typeof(CharacterState), typeof(CharacterTriggerData.Trigger),
            typeof(CharacterState), typeof(bool), typeof(bool), typeof(CharacterState.FireTriggersData), typeof(int), typeof(CharacterTriggerState) })]
        private static class QueuePatch
        {
            private static void Prefix(CombatManager __instance, out int __state) => __state = Count(__instance);
            private static void Postfix(CombatManager __instance, CharacterState character, CharacterTriggerData.Trigger trigger,
                CharacterState dyingCharacter, CharacterState.FireTriggersData fireTriggersData, int triggerCount, int __state)
            {
                if (current != null && Count(__instance) > __state && character.GetTriggers().Any(state => state.GetTrigger() == trigger))
                    current.Queued.Add(CaptureCallback(character, trigger, dyingCharacter, fireTriggersData, triggerCount));
            }
            private static int Count(CombatManager combat) => ((ICollection)AccessTools.Property(typeof(CombatManager), "TriggerQueue").GetValue(combat)).Count;
        }
        // The standalone setup runs outside the normal card/combat coroutine. Its UI
        // update may otherwise start an unrelated queue runner during a revival yield.
        // Keep this API boundary quiet; the native caller drains the queue after it.
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.RunTriggerQueue))]
        private static class AutomaticQueuePatch
        {
            private static bool Prefix(CombatManager __instance, ref IEnumerator __result)
            {
                if (current == null || RevivalScenario.Label == null || __instance.IsRunningTriggerQueue) return true;
                current.AutomaticQueueDeferrals++;
                __result = Empty(); return false;
            }
            private static IEnumerator Empty() { yield break; }
        }
    }
}

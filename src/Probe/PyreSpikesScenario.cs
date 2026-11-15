using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class PyreSpikesScenario
    {
        internal const string Prefix = "pyre-spikes-";
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS")?.StartsWith(Prefix, StringComparison.Ordinal) == true;
        internal static bool Sweep => Enabled && Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") != Prefix + "single";
        internal static int Health => Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS") == Prefix + "sustain" ? 6 : 3;
        internal static bool Prepared { get; private set; }
        internal static RoomCombatState? Initial { get; private set; }
        internal static readonly List<Write> Writes = new List<Write>();
        internal static readonly List<Entry> Entries = new List<Entry>();
        private static int LastDamage(CardStatistics statistics) => (int)AccessTools.Field(typeof(CardStatistics), "lastAttackDamageDealt").GetValue(statistics);
        internal sealed class Write
        {
            public int Sequence { get; set; }
            public int Turn { get; set; }
            public int[] Stages { get; set; } = Array.Empty<int>();
            public bool Preview { get; set; }
            public bool QueueRunning { get; set; }
            public int Damage { get; set; }
            public int Before { get; set; }
            public int? Actual { get; set; }
            public string Caller { get; set; } = "";
        }
        internal sealed class Entry
        {
            public int Sequence { get; set; }
            public int Turn { get; set; }
            public int[] Stages { get; set; } = Array.Empty<int>();
            public string Phase { get; set; } = "";
            public string Trigger { get; set; } = "";
            public int UnitId { get; set; }
            public int Health { get; set; }
            public bool Preview { get; set; }
            public bool QueueRunning { get; set; }
            public int? Damage { get; set; }
            public int LastDamage { get; set; }
        }
        internal static void Prepare(AllGameManagers managers, ManualLogSource log)
        {
            if (managers.GetSaveManager().PreviewMode || managers.GetCombatManager().GetTurnCount() != 0)
                throw new InvalidOperationException("Pyre spikes requires isolated deployment.");
            var guard = managers.GetSaveManager().GetAllGameData().GetAllCharacterData()
                .Single(unit => unit.name == "ShieldGuardT1_BasicNonAttack");
            AccessTools.Field(typeof(CharacterData), "health").SetValue(guard, 1000);
            var pyre = managers.GetRoomManager()!.GetPyreRoom().GetPyreHeart();
            pyre.DebuffHP(pyre.GetHP() - Health);
            if (!Sweep) pyre.RemoveStatusEffect("sweep", -1);
            if (pyre.GetHP() != Health || pyre.HasStatusEffect("sweep") != Sweep)
                throw new InvalidOperationException("Native Pyre spikes controls did not settle.");
            Initial = FullBattleTrace.Active!.Capture(pyre.GetCurrentRoom());
            Prepared = true;
            AccessTools.Field(typeof(CombatManager), "combatStateChanged").SetValue(managers.GetCombatManager(), true);
            log.LogInfo("PYRE-SPIKES-PREPARED hp=" + Health + " sweep=" + Sweep + " guardHp=1000 originalArmorAndSpikes=true noCards=true");
        }
        [HarmonyPatch(typeof(CardStatistics), nameof(CardStatistics.SetAttackDamageDealt))]
        private static class WritePatch
        {
            private static void Prefix(CardStatistics __instance, int damage, out Write? __state)
            {
                __state = null; if (!Prepared || FullBattleTrace.Active == null) return;
                try
                {
                    var managers = AllGameManagers.Instance!; var trace = FullBattleTrace.Active;
                    string caller = new StackTrace(false).GetFrames().Select(frame => frame.GetMethod()?.DeclaringType?.Name ?? "")
                        .FirstOrDefault(name => name.StartsWith("<ApplyDamage>", StringComparison.Ordinal) || name.StartsWith("<FireTriggers>", StringComparison.Ordinal)) ?? "unknown";
                    __state = new Write { Sequence = trace.NextPhaseSequence(), Turn = managers.GetCombatManager().GetTurnCount(),
                        Stages = trace.ActiveStageIds(), Preview = managers.GetSaveManager().PreviewMode,
                        QueueRunning = managers.GetCombatManager().IsRunningTriggerQueue, Damage = damage, Before = LastDamage(__instance), Caller = caller };
                    Writes.Add(__state);
                }
                catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
            }
            private static void Postfix(CardStatistics __instance, Write? __state)
            {
                if (__state != null) __state.Actual = LastDamage(__instance);
            }
        }
        private static IEnumerator Observe(IEnumerator native, CharacterState actor, CharacterTriggerData.Trigger trigger,
            CharacterState.FireTriggersData data, string phase)
        {
            try
            {
                try
                {
                    var managers = AllGameManagers.Instance!; var trace = FullBattleTrace.Active!;
                    using (new CharacterState.SetAllowDestroyedAccessHelper(actor, onlyIfDestroyed: true))
                        Entries.Add(new Entry { Sequence = trace.NextPhaseSequence(), Turn = managers.GetCombatManager().GetTurnCount(),
                            Stages = trace.ActiveStageIds(), Phase = phase, Trigger = trigger.ToString(), UnitId = trace.UnitId(actor),
                            Health = actor.GetHP(), Preview = managers.GetSaveManager().PreviewMode,
                            QueueRunning = managers.GetCombatManager().IsRunningTriggerQueue, Damage = data?.attackDamageDealt,
                            LastDamage = LastDamage(managers.GetCardStatistics()) });
                }
                catch (Exception error) { FullBattleTrace.Active!.CaptureFailure(error); }
                while (native.MoveNext()) yield return native.Current;
            }
            finally { (native as IDisposable)?.Dispose(); }
        }
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.QueueAndRunTrigger))]
        private static class QueuePatch
        {
            private static void Postfix(CharacterState character, CharacterTriggerData.Trigger trigger,
                CharacterState.FireTriggersData fireTriggersData, ref IEnumerator __result)
            { if (Prepared) __result = Observe(__result, character, trigger, fireTriggersData, "Queue"); }
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.FireTriggers))]
        private static class FirePatch
        {
            private static void Postfix(CharacterState __instance, CharacterTriggerData.Trigger trigger,
                CharacterState.FireTriggersData fireTriggersData, ref IEnumerator __result)
            { if (Prepared) __result = Observe(__result, __instance, trigger, fireTriggersData, "Fire"); }
        }
    }
}

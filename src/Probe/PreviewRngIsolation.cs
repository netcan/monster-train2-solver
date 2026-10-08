using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    // Opt-in solver protocol: previews start from Battle and restore both streams.
    // Their native attack statistic remains visible and is independently modeled.
    internal static class PreviewRngIsolation
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_ISOLATE_PREVIEW_RNG") == "1";
        internal static readonly List<Record> Records = new List<Record>();
        private static Scope? active;
        internal static bool Active => active != null;
        internal sealed class Record
        {
            public string Kind { get; set; } = "";
            public UnityRng BattleBefore { get; set; }
            public UnityRng TestBefore { get; set; }
            public UnityRng TestObserved { get; set; }
            public UnityRng BattleObserved { get; set; }
            public bool PrimaryRoomOrderCompleted { get; set; }
            public UnityRng? BattleAfter { get; set; }
            public UnityRng? TestAfter { get; set; }
            public bool Completed { get; set; }
        }
        private sealed class Scope
        {
            internal HadesRNG Battle = null!, Test = null!, SavedBattle = null!, SavedTest = null!;
            internal Record Record = null!;
            internal bool WaitingForPrimaryOrder;
        }
        private static HadesRNG Native(RngId id) => (HadesRNG)AccessTools.Method(typeof(RandomManager), "GetRng").Invoke(null, new object[] { id });
        private static UnityRng State(HadesRNG rng)
        { uint[] words = RngCalibration.Words(rng.GetState()); return new UnityRng(words[0], words[1], words[2], words[3]); }
        private static Scope? Begin(string kind)
        {
            if (!Enabled || active != null) return null;
            HadesRNG battle = Native(RngId.Battle), test = Native(RngId.BattleTest);
            var scope = new Scope { Battle = battle, Test = test, SavedBattle = new HadesRNG(battle), SavedTest = new HadesRNG(test),
                Record = new Record { Kind = kind, BattleBefore = State(battle), TestBefore = State(test) } };
            active = scope; Records.Add(scope.Record); test.Init(battle); return scope;
        }
        private static void End(Scope scope)
        {
            scope.Record.TestObserved = State(scope.Test); scope.Record.BattleObserved = State(scope.Battle);
            scope.Battle.Init(scope.SavedBattle); scope.Test.Init(scope.SavedTest);
            scope.Record.BattleAfter = State(scope.Battle); scope.Record.TestAfter = State(scope.Test); scope.Record.Completed = true;
            active = null;
        }
        [HarmonyPatch(typeof(CombatManager), "SetCharacterPreviewState")]
        private static class BattlePatch
        {
            private static void Prefix(CharacterState.CombatPreviewState previewState)
            { if (previewState == CharacterState.CombatPreviewState.Calculating) Begin("Battle"); }
            private static void Postfix(CharacterState.CombatPreviewState previewState)
            {
                if (active?.Record.Kind != "Battle") return;
                if (previewState == CharacterState.CombatPreviewState.On) active.WaitingForPrimaryOrder = true;
                else if (previewState == CharacterState.CombatPreviewState.Off) End(active);
            }
        }
        private static IEnumerator WrapPrimaryOrder(IEnumerator native, Scope scope)
        {
            try { while (native.MoveNext()) yield return native.Current; }
            finally
            {
                (native as IDisposable)?.Dispose(); scope.Record.PrimaryRoomOrderCompleted = true;
                End(scope);
            }
        }
        [HarmonyPatch(typeof(RoomManager), "HandleRoomUnitOrderPossiblyChanged")]
        private static class PrimaryOrderPatch
        {
            private static void Postfix(ref IEnumerator __result)
            {
                if (active?.Record.Kind != "Battle" || !active.WaitingForPrimaryOrder ||
                    AllGameManagers.Instance!.GetSaveManager().PreviewMode) return;
                Scope scope = active; scope.WaitingForPrimaryOrder = false;
                __result = WrapPrimaryOrder(__result, scope);
            }
        }
        private static IEnumerator Wrap(IEnumerator native)
        {
            Scope? scope = Begin("BossKill");
            try { while (native.MoveNext()) yield return native.Current; }
            finally { (native as IDisposable)?.Dispose(); if (scope != null) End(scope); }
        }
        [HarmonyPatch(typeof(CombatManager), "RunBossKillPreview")]
        private static class BossPatch
        {
            private static void Postfix(ref IEnumerator __result) { if (Enabled) __result = Wrap(__result); }
        }
    }
}

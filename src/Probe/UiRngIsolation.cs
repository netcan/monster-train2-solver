using System;
using System.Collections.Generic;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    // Experiments can preserve both streams. Player sessions preserve only the
    // UI test stream and retain the original query result/live Battle behavior.
    internal static class UiRngIsolation
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_ISOLATE_UI_RNG") == "1";
        internal static readonly List<Record> Records = new List<Record>();
        private static long playerCalls, playerRestoredDraws, playerExceptions;
        internal static object PlayerEvidence => new { Stream = "BattleTest", Calls = playerCalls,
            RestoredDraws = playerRestoredDraws, Exceptions = playerExceptions, PreservesLiveBattleRng = true };
        internal static void InstallAdvisorPatches(Harmony harmony)
        {
            if (!Enabled) harmony.CreateClassProcessor(typeof(HighlightPatch)).Patch();
        }
        private static HadesRNG Native(RngId id) => (HadesRNG)AccessTools.Method(typeof(RandomManager), "GetRng").Invoke(null, new object[] { id });
        private static UnityRng State(HadesRNG native)
        {
            uint[] words = RngCalibration.Words(native.GetState()); return new UnityRng(words[0], words[1], words[2], words[3]);
        }
        internal sealed class Record
        {
            public UnityRng BattleBefore { get; set; }
            public UnityRng BattleObserved { get; set; }
            public UnityRng BattleAfter { get; set; }
            public UnityRng TestBefore { get; set; }
            public UnityRng TestAfter { get; set; }
            public int BattleSeedBefore { get; set; }
            public int BattleSeedAfter { get; set; }
            public int TestSeedBefore { get; set; }
            public int TestSeedAfter { get; set; }
            public bool? Result { get; set; }
            public bool Exception { get; set; }
        }
        private sealed class Scope
        {
            internal HadesRNG Battle = null!, Test = null!, SavedBattle = null!, SavedTest = null!;
            internal Record Record = null!;
            internal bool TestOnly;
        }
        [HarmonyPatch(typeof(CardUI), nameof(CardUI.IsPlayableAndAffectsState))]
        private static class HighlightPatch
        {
            // Probe and Advisor compile this type into separate assemblies.
            // Avoid an inactive probe patch replacing the player's __state;
            // explicit development isolation owns its own full-stream guard.
            [HarmonyPrepare]
            private static bool Prepare() => Enabled || AdvisorRuntime.PlayerSession;

            private static void Prefix(out Scope? __state)
            {
                __state = null;
                if ((!Enabled && !AdvisorRuntime.PlayerSession) || AllGameManagers.Instance?.GetSaveManager() == null) return;
                // Hand highlights sample random targets even outside preview mode.
                // Preserve their test stream without recording an unbounded history
                // or reading/restoring the live Battle stream in the player mod.
                if (!Enabled && AdvisorRuntime.PlayerSession)
                {
                    HadesRNG playerTest = Native(RngId.BattleTest);
                    if (playerTest != null) __state = new Scope { Test = playerTest,
                        SavedTest = new HadesRNG(playerTest), TestOnly = true };
                    return;
                }
                HadesRNG battle = Native(RngId.Battle), test = Native(RngId.BattleTest);
                if (battle == null || test == null) return;
                __state = new Scope { Battle = battle, Test = test, SavedBattle = new HadesRNG(battle), SavedTest = new HadesRNG(test),
                    Record = new Record { BattleBefore = State(battle), TestBefore = State(test), BattleSeedBefore = battle.GetSeed(), TestSeedBefore = test.GetSeed() } };
            }
            private static void Postfix(bool __result, Scope? __state)
            { if (__state != null && !__state.TestOnly) __state.Record.Result = __result; }
            private static Exception? Finalizer(Exception? __exception, Scope? __state)
            {
                if (__state == null) return __exception;
                if (__state.TestOnly)
                {
                    playerCalls++;
                    if (!__state.Test.GetState().Equals(__state.SavedTest.GetState())) playerRestoredDraws++;
                    if (__exception != null) playerExceptions++;
                    __state.Test.Init(__state.SavedTest);
                    return __exception;
                }
                __state.Record.BattleObserved = State(__state.Battle);
                __state.Battle.Init(__state.SavedBattle); __state.Test.Init(__state.SavedTest);
                __state.Record.BattleAfter = State(__state.Battle); __state.Record.TestAfter = State(__state.Test);
                __state.Record.BattleSeedAfter = __state.Battle.GetSeed(); __state.Record.TestSeedAfter = __state.Test.GetSeed();
                __state.Record.Exception = __exception != null; Records.Add(__state.Record);
                return __exception;
            }
        }
    }
}

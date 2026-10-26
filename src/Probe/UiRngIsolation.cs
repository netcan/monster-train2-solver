using System;
using System.Collections.Generic;
using HarmonyLib;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    // Experiments can preserve both streams. Player sessions preserve only the
    // effect-query test stream and retain original results/live Battle behavior.
    internal static class UiRngIsolation
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_ISOLATE_UI_RNG") == "1";
        internal static readonly List<Record> Records = new List<Record>();
        private static long playerCalls, playerRestoredDraws, playerExceptions;
        [ThreadStatic] private static int playerQueryDepth;
        internal static object PlayerEvidence => new { Stream = "BattleTest", Calls = playerCalls,
            RestoredDraws = playerRestoredDraws, Exceptions = playerExceptions, PreservesLiveBattleRng = true,
            QueryBoundary = "CombatManager.TestEffects+GameEffectHelper.TestEffect" };
        internal static void InstallAdvisorPatches(Harmony harmony)
        {
            if (Enabled) return;
            harmony.CreateClassProcessor(typeof(EffectsQueryPatch)).Patch();
            harmony.CreateClassProcessor(typeof(EffectQueryPatch)).Patch();
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
        }
        private struct PlayerScope
        {
            internal bool Active;
            internal HadesRNG? Test, SavedTest;
        }
        private static PlayerScope BeginPlayerQuery()
        {
            if (Enabled || !AdvisorRuntime.PlayerSession || AllGameManagers.Instance?.GetSaveManager() == null) return default;
            // A composite TestEffects query can contain several TestEffect calls.
            // Keep their internal random sequence intact, then restore once at
            // the outer query boundary. Standalone effect tests are queries too.
            var scope = new PlayerScope { Active = true };
            if (playerQueryDepth == 0)
            {
                scope.Test = Native(RngId.BattleTest);
                scope.SavedTest = new HadesRNG(scope.Test);
            }
            playerQueryDepth++;
            return scope;
        }
        private static Exception? EndPlayerQuery(Exception? exception, PlayerScope scope)
        {
            if (!scope.Active) return exception;
            playerQueryDepth--;
            if (scope.Test == null || scope.SavedTest == null) return exception;
            playerCalls++;
            if (!scope.Test.GetState().Equals(scope.SavedTest.GetState())) playerRestoredDraws++;
            if (exception != null) playerExceptions++;
            scope.Test.Init(scope.SavedTest);
            return exception;
        }
        [HarmonyPatch(typeof(CombatManager), nameof(CombatManager.TestEffects))]
        private static class EffectsQueryPatch
        {
            [HarmonyPrepare] private static bool Prepare() => !Enabled && AdvisorRuntime.PlayerSession;
            private static void Prefix(out PlayerScope __state) => __state = BeginPlayerQuery();
            private static Exception? Finalizer(Exception? __exception, PlayerScope __state) => EndPlayerQuery(__exception, __state);
        }
        [HarmonyPatch(typeof(GameEffectHelper), nameof(GameEffectHelper.TestEffect))]
        private static class EffectQueryPatch
        {
            [HarmonyPrepare] private static bool Prepare() => !Enabled && AdvisorRuntime.PlayerSession;
            private static void Prefix(out PlayerScope __state) => __state = BeginPlayerQuery();
            private static Exception? Finalizer(Exception? __exception, PlayerScope __state) => EndPlayerQuery(__exception, __state);
        }
        [HarmonyPatch(typeof(CardUI), nameof(CardUI.IsPlayableAndAffectsState))]
        private static class HighlightPatch
        {
            // Probe and Advisor compile this type into separate assemblies.
            // Avoid an inactive probe patch replacing the player's __state;
            // explicit development isolation owns its own full-stream guard.
            [HarmonyPrepare]
            private static bool Prepare() => Enabled;

            private static void Prefix(out Scope? __state)
            {
                __state = null;
                if (!Enabled || AllGameManagers.Instance?.GetSaveManager() == null) return;
                HadesRNG battle = Native(RngId.Battle), test = Native(RngId.BattleTest);
                if (battle == null || test == null) return;
                __state = new Scope { Battle = battle, Test = test, SavedBattle = new HadesRNG(battle), SavedTest = new HadesRNG(test),
                    Record = new Record { BattleBefore = State(battle), TestBefore = State(test), BattleSeedBefore = battle.GetSeed(), TestSeedBefore = test.GetSeed() } };
            }
            private static void Postfix(bool __result, Scope? __state)
            { if (__state != null) __state.Record.Result = __result; }
            private static Exception? Finalizer(Exception? __exception, Scope? __state)
            {
                if (__state == null) return __exception;
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

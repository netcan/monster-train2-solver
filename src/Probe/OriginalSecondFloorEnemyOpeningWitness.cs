#if DEVELOPMENT_PROBE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class OriginalSecondFloorEnemyOpeningWitness
    {
        internal const string FileName = "second-floor-enemy-opening.mt2f";
        private const string OwnerId = RelicSecondFloorEnemyOpeningModel.OwnerId;
        private const string OwnerKey = RelicSecondFloorEnemyOpeningModel.OwnerKey;
        private static readonly Guid Module = new Guid("bf93f060-beda-46b1-b808-95ebf1e9a99d");
        private static readonly AccessTools.FieldRef<CombatManager, int> TurnModifier =
            AccessTools.FieldRefAccess<CombatManager, int>("turnCountModifier");
        private static readonly List<Phase> phases = new List<Phase>();
        private static BattleTurnState? deployment;
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_SECOND_FLOOR_ENEMY_OPENING") == "1";
        internal static bool Acquired { get; private set; }

        internal static void Acquire(SaveManager save)
        {
            if (Application.version != "2.3.1" || typeof(CombatManager).Module.ModuleVersionId != Module ||
                save.PreviewMode || save.InTestScenario || save.GetCurrentDistance() != 1 ||
                save.GetCovenants().Count != 0 || save.GetMutators().Count != 0 ||
                save.GetAllGameData().FindMutatorData(OwnerId) is not MutatorData original ||
                !RelicPercentageStatProbe.ValidSecondFloorEnemyOpeningDefinition(original))
                throw new InvalidOperationException("Expected the unchanged original second-floor mutator and stock Ring 2 checkpoint.");
            save.AddMutator(original);
            if (save.GetMutators() is not [var owner] || owner.GetRelicDataID() != OwnerId ||
                owner.GetEffects() is not [RelicEffectChangeHeroSpawnRoom { HeroesStarterRoomIndex: 1 }])
                throw new InvalidOperationException("Native second-floor mutator did not initialize.");
            Acquired = true;
        }

        internal static void CaptureDeployment(FullBattleTrace trace)
        {
            if (!Acquired || deployment != null)
                throw new InvalidOperationException("Expected exactly one original deployment decision.");
            deployment = trace.CaptureDecision();
            if (deployment.Spawn.Turn != 0 || deployment.Spawn.Phase != 0)
                throw new InvalidOperationException("Original deployment was already advanced.");
        }

        internal static void Write(FullBattleTrace trace)
        {
            BattleTurnState first = trace.CaptureDecision();
            CombatManager combat = AllGameManagers.Instance!.GetCombatManager();
            if (deployment == null || first.Spawn.Turn != 1 || phases.Count != 4 ||
                !phases.Select(phase => phase.Name).SequenceEqual(new[] { "InitialSpawn", "HeroTurn", "HeroEndOfTurn", "StartOfBattleRelics" }))
                throw new InvalidOperationException("The original second-floor opening boundaries were incomplete.");
            using var archive = NativeFixtureCapture.Capture(new
            {
                Schema = 1, Boundary = "OriginalSecondFloorEnemyOpening", GameVersion = Application.version, GameModuleMvid = Module,
                OwnerId, OwnerKey, StarterRoomIndex = 1, Deployment = deployment, Phases = phases,
                FirstDecision = first, FirstDecisionTurnModifier = TurnModifier(combat)
            });
            using var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, FileName));
            archive.Write(output);
        }

        private sealed class Phase(string name, Snapshot before, Snapshot actual)
        {
            public string Name { get; } = name;
            public Snapshot Before { get; } = before;
            public Snapshot Actual { get; } = actual;
        }

        private sealed class Snapshot(EnemySpawnState spawn, int turnModifier)
        {
            public EnemySpawnState Spawn { get; } = spawn;
            public int TurnModifier { get; } = turnModifier;
        }

        private static void Observe(string name, int expectedIndex, ref IEnumerator native)
        {
            if (!Enabled || deployment == null || phases.Count != expectedIndex ||
                AllGameManagers.Instance?.GetCombatManager() is not CombatManager combat || combat.GetTurnCount() != 1 ||
                FullBattleTrace.Active is not FullBattleTrace trace || AllGameManagers.Instance.GetSaveManager().PreviewMode)
                return;
            native = Record(native, name, combat, trace);
        }

        private static IEnumerator Record(IEnumerator native, string name, CombatManager combat, FullBattleTrace trace)
        {
            Snapshot Capture() => new Snapshot(trace.CaptureCanonicalSpawn(), TurnModifier(combat));
            Snapshot before = Capture();
            try { while (native.MoveNext()) yield return native.Current; }
            finally { (native as IDisposable)?.Dispose(); }
            phases.Add(new Phase(name, before, Capture()));
        }

        [HarmonyPatch(typeof(HeroManager), nameof(HeroManager.BeginInitialSpawn))]
        private static class InitialSpawnPatch
        {
            private static void Postfix(ref IEnumerator __result) => Observe("InitialSpawn", 0, ref __result);
        }

        [HarmonyPatch(typeof(HeroManager), nameof(HeroManager.DoTurn))]
        private static class HeroTurnPatch
        {
            private static void Postfix(ref IEnumerator __result) => Observe("HeroTurn", 1, ref __result);
        }

        [HarmonyPatch(typeof(HeroManager), nameof(HeroManager.DoEndOfTurn))]
        private static class HeroEndTurnPatch
        {
            private static void Postfix(ref IEnumerator __result) => Observe("HeroEndOfTurn", 2, ref __result);
        }

        [HarmonyPatch(typeof(RelicManager), nameof(RelicManager.ApplyStartOfBattleRelicEffects))]
        private static class StartOfBattlePatch
        {
            private static void Postfix(ref IEnumerator __result) => Observe("StartOfBattleRelics", 3, ref __result);
        }
    }
}
#endif

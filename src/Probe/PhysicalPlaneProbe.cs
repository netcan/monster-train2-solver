using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;

namespace MonsterTrain2Poju.Probe
{
    internal static class PhysicalPlaneProbe
    {
        internal sealed class Sample
        {
            public string Operation { get; set; } = "Set";
            public int UnitId { get; set; }
            public int RoomIndex { get; set; }
            public CombatTeam Team { get; set; }
            public int? PreviewCopyId { get; set; }
            public SpawnPointReference? Target { get; set; }
            public SpawnPointWorld Before { get; set; } = null!;
            public SpawnPointWorld? Actual { get; set; }
            public int Pivot { get; set; } = -1;
            public int PivotMoves { get; set; }
            public bool Completed { get; set; }
            public string? Difference { get; set; }
        }
        private static readonly List<Sample> samples = new List<Sample>();
        private static readonly List<string> errors = new List<string>();
        private static void Complete(Sample sample, SpawnPoint? target = null)
        {
            sample.Actual = BattleSpawnPointProbe.ActiveWorld(FullBattleTrace.Active!, target, sample.Before.Groups);
            var predicted = SpawnPointModel.Apply(sample.Before, sample.Operation, sample.RoomIndex, sample.Team,
                sample.UnitId, index: sample.Pivot, target: sample.Target, previewCopyId: sample.PreviewCopyId);
            sample.Difference = predicted.Supported && predicted.PivotMoves == sample.PivotMoves &&
                JToken.DeepEquals(JToken.FromObject(predicted.State!), JToken.FromObject(sample.Actual))
                ? null : predicted.UnsupportedReason ?? "Original physical point operation differs.";
            sample.Completed = true;
        }
        internal static void Write()
        {
            if (!PreviewReferenceProbe.Enabled || samples.Count == 0) return;
            using (var archive = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CharacterState).Assembly.ManifestModule.ModuleVersionId,
                Boundary = "OriginalSetSpawnPointAcrossCopies", GameplaySuppressed = false, WholeBattleVerified = false,
                Errors = errors, Samples = samples }))
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "physical-plane-calibration.mt2f")))
                archive.Write(stream);
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.SetSpawnPoint))]
        private static class SetPatch
        {
            private static void Prefix(CharacterState __instance, SpawnPoint? setSpawnPoint, bool animate,
                Action? onCharacterReachedPoint, out Sample? __state)
            {
                __state = null;
                if (!PreviewReferenceProbe.Enabled || !BattleSpawnPointProbe.Enabled || FullBattleTrace.Active == null ||
                    !EnchantmentBattleScenario.Prepared || animate || onCharacterReachedPoint != null ||
                    !(__instance.SpawnedInPreviewMode || __instance.PreviewMode || __instance.TemporaryPreviewsEnabled)) return;
                try
                {
                    var trace = FullBattleTrace.Active;
                    var before = BattleSpawnPointProbe.ActiveWorld(trace, setSpawnPoint);
                    var target = BattleSpawnPointProbe.Reference(setSpawnPoint);
                    var actor = before.Units.Single(unit => unit.UnitId == trace.UnitId(__instance));
                    if (!__instance.SpawnedInPreviewMode && actor.Current?.PreviewCopyId == target?.PreviewCopyId) return;
                    var selected = target ?? actor.Current ?? actor.LastKnown;
                    if (selected == null) return;
                    __state = new Sample { UnitId = actor.UnitId, RoomIndex = selected.RoomIndex, Team = selected.Team,
                        PreviewCopyId = selected.PreviewCopyId, Target = target, Before = before };
                    samples.Add(__state);
                }
                catch (Exception error) { errors.Add(error.ToString()); }
            }
            private static void Postfix(SpawnPoint? setSpawnPoint, Sample? __state)
            {
                if (__state == null) return;
                try
                {
                    Complete(__state, setSpawnPoint);
                }
                catch (Exception error) { errors.Add(error.ToString()); }
            }
        }
        [HarmonyPatch(typeof(RoomState), nameof(RoomState.ShiftSpawnPoints))]
        private static class CompactPatch
        {
            private static void Prefix(RoomState __instance, Team.Type team, int pivotIndex, out Sample? __state)
            {
                __state = null;
                if (!PreviewReferenceProbe.Enabled || !BattleSpawnPointProbe.Enabled || FullBattleTrace.Active == null ||
                    !EnchantmentBattleScenario.Prepared) return;
                var group = (SpawnPointGroup)AccessTools.Field(typeof(RoomState),
                    team == Team.Type.Heroes ? "heroSpawnPointGroup" : "monsterSpawnPointGroup").GetValue(__instance);
                if (!group.PreviewMode) return;
                try
                {
                    group.GetAll(out List<SpawnPoint> points, out _);
                    var point = BattleSpawnPointProbe.Reference(points[0])!;
                    __state = new Sample { Operation = "Compact", RoomIndex = __instance.GetRoomIndex(),
                        Team = team == Team.Type.Heroes ? CombatTeam.Enemy : CombatTeam.Player, PreviewCopyId = point.PreviewCopyId,
                        Before = BattleSpawnPointProbe.ActiveWorld(FullBattleTrace.Active, points[0]), Pivot = pivotIndex };
                    samples.Add(__state);
                }
                catch (Exception error) { errors.Add(error.ToString()); }
            }
            private static void Postfix(int __result, Sample? __state)
            {
                if (__state == null) return;
                try { __state.PivotMoves = __result; Complete(__state); }
                catch (Exception error) { errors.Add(error.ToString()); }
            }
        }
    }
}

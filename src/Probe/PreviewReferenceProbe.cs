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
    internal static class PreviewReferenceProbe
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_PREVIEW_REFERENCES") == "1";
        internal sealed class Birth
        {
            public int UnitId { get; set; }
            public CombatUnit Primary { get; set; } = null!;
            public int RoomIndex { get; set; }
            public int PointIndex { get; set; }
            public bool TemporaryPoint { get; set; }
            public int RemovalStage { get; set; }
            internal CharacterState Native = null!;
        }
        internal sealed class Sample
        {
            public int Frame { get; set; }
            public int ActorId { get; set; }
            public CombatUnit Before { get; set; } = null!;
            public CombatUnit? Preview { get; set; }
            public CombatUnit? Actual { get; set; }
            public int[] RemovedPreviewUnitIds { get; set; } = Array.Empty<int>();
            public string? Difference { get; set; }
            public bool Completed { get; set; }
        }
        private sealed class Scope
        {
            internal readonly List<(CharacterState Native, Sample Sample)> Actors = new List<(CharacterState, Sample)>();
            internal readonly List<Birth> Births = new List<Birth>();
        }
        private static Scope? active;
        private static readonly List<Sample> samples = new List<Sample>();
        private static readonly List<Birth> births = new List<Birth>();
        private static readonly List<string> errors = new List<string>();
        private static void Begin()
        {
            if (!Enabled || active != null || FullBattleTrace.Active == null || !EnchantmentBattleScenario.Prepared) return;
            FullBattleTrace trace = FullBattleTrace.Active;
            active = new Scope();
            foreach (CharacterState actor in trace.KnownUnits.Where(actor => actor != null && actor.IsAlive && !actor.IsDestroyed).ToArray())
            {
                CombatUnit unit = trace.CaptureDecision(() => trace.CaptureUnit(actor));
                if (!unit.Triggers.SelectMany(trigger => trigger.Effects).Any(effect => effect.Enchantment != null || effect.Summon != null)) continue;
                var sample = new Sample { Frame = UnityEngine.Time.frameCount, ActorId = unit.Id, Before = unit };
                samples.Add(sample); active.Actors.Add((actor, sample));
            }
        }
        private static void Observe()
        {
            if (active == null) return;
            foreach (var actor in active.Actors) actor.Sample.Preview = FullBattleTrace.Active!.CaptureUnit(actor.Native);
        }
        private static void Complete()
        {
            if (active == null) return;
            foreach (Birth birth in active.Births)
                birth.RemovalStage = Convert.ToInt32(AccessTools.Field(typeof(CharacterState), "destroyedState").GetValue(birth.Native));
            int[] removed = active.Births.Where(birth => birth.RemovalStage > 0).Select(birth => birth.UnitId).ToArray();
            foreach (var actor in active.Actors)
            {
                Sample sample = actor.Sample;
                sample.Actual = FullBattleTrace.Active!.CaptureDecision(() => FullBattleTrace.Active.CaptureUnit(actor.Native));
                sample.RemovedPreviewUnitIds = removed;
                CombatUnit result = PreviewEffectsModel.Restore(sample.Before, sample.Preview!, removed);
                sample.Difference = JToken.DeepEquals(JToken.FromObject(result), JToken.FromObject(sample.Actual)) ? null : "Native preview effect restoration differs.";
                sample.Completed = true;
            }
            active = null; Write();
        }
        private static void Write()
        {
            string path = Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "preview-reference-calibration.mt2f");
            using (var archive = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = UnityEngine.Application.version,
                GameModuleMvid = typeof(CharacterState).Assembly.ManifestModule.ModuleVersionId,
                Boundary = "OriginalSetCharacterPreviewStateOn", GameplaySuppressed = false, FullPreviewSimulationVerified = false,
                ModifierScenario = Environment.GetEnvironmentVariable("MT2_PROBE_MODIFIERS"), Errors = errors,
                Mismatches = samples.Count(sample => sample.Difference != null), Samples = samples, Births = births }))
            using (var stream = File.Create(path)) archive.Write(stream);
        }
        [HarmonyPatch(typeof(CharacterState), nameof(CharacterState.EnableCombatPreviews))]
        private static class BirthPatch
        {
            private static void Prefix(CharacterState __instance)
            {
                if (active == null || !__instance.SpawnedInPreviewMode || __instance.PreviewMode) return;
                try
                {
                    FullBattleTrace trace = FullBattleTrace.Active!;
                    SpawnPoint point = __instance.GetSpawnPoint() ?? throw new InvalidOperationException("A preview birth has no initial point.");
                    RoomState room = point.GetRoomOwner() ?? throw new InvalidOperationException("A preview birth has no room owner.");
                    object group = AccessTools.Field(typeof(RoomState), "monsterSpawnPointGroup").GetValue(room);
                    var temporary = (List<SpawnPoint>)AccessTools.Field(group.GetType(), "_temporarySpawnPoints").GetValue(group);
                    var birth = new Birth { UnitId = trace.UnitId(__instance), Primary = trace.CaptureUnit(__instance),
                        RoomIndex = room.GetRoomIndex(), PointIndex = point.GetIndexInRoom(), TemporaryPoint = temporary.Contains(point), Native = __instance };
                    active.Births.Add(birth); births.Add(birth);
                }
                catch (Exception error) { errors.Add(error.ToString()); }
            }
        }
        [HarmonyPatch(typeof(CombatManager), "SetCharacterPreviewState")]
        private static class StatePatch
        {
            private static void Prefix(CharacterState.CombatPreviewState previewState)
            {
                try { if (previewState == CharacterState.CombatPreviewState.Calculating) Begin(); else if (previewState == CharacterState.CombatPreviewState.On) Observe(); }
                catch (Exception error) { errors.Add(error.ToString()); }
            }
            private static void Postfix(CharacterState.CombatPreviewState previewState)
            {
                try { if (previewState == CharacterState.CombatPreviewState.On) Complete(); }
                catch (Exception error) { errors.Add(error.ToString()); Write(); }
            }
        }
    }
}

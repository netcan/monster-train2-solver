using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    internal static class TrueFinalBossSpawnCalibration
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_SPAWN") == "1";
        private static TrueFinalBossSpawnState? before;
        private static object? rngBefore;
        internal static IReadOnlyList<EnemyDefinition>? Definitions { get; private set; }
        private static object Rng() => Enum.GetValues(typeof(RngId)).Cast<RngId>().Select(id =>
            new { Id = id.ToString(), Words = RngCalibration.Words(RandomManager.GetState(id)) }).ToArray();
        private static TrueFinalBossSpawnState Capture(HeroManager heroes, IReadOnlyList<EnemyDefinition> definitions,
            IReadOnlyList<string> interactions)
        {
            FullBattleTrace trace = FullBattleTrace.Active!;
            RoomManager rooms = AllGameManagers.Instance.GetRoomManager();
            int Id(CharacterState? actor) => actor == null ? 0 : trace.UnitId(actor);
            return new TrueFinalBossSpawnState(trace.CaptureDecision(true, true), definitions,
                ((List<int>)AccessTools.Field(typeof(HeroManager), "queuedTrueFinalBosses").GetValue(heroes)).ToArray(),
                Id(heroes.GetOuterTrainBossCharacter()), Enumerable.Range(0, rooms.GetNumRooms())
                    .Select(index => Id(rooms.GetRoom(index).GetOuterTrainBoss())).ToArray(), interactions);
        }
        internal static void Begin(HeroManager heroes)
        {
            if (!Enabled) return;
            IReadOnlyList<EnemyDefinition> definitions = CaptureDefinitions(heroes, out IReadOnlyList<string> interactions);
            Definitions = definitions;
            before = Capture(heroes, definitions, interactions); rngBefore = Rng();
        }
        internal static IReadOnlyList<EnemyDefinition> CaptureDefinitions(HeroManager heroes, out IReadOnlyList<string> externalInteractions)
        {
            SpawnPatternData pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(heroes);
            var interactions = new List<string>();
            if (AllGameManagers.Instance.GetSaveManager().GetCovenantsForSpawnPattern().Count > 0 && TrueFinalBossCovenantScenario.Evidence == null &&
                !CovenantBirthStatProbe.FinalBossCalibrated(AllGameManagers.Instance.GetSaveManager()))
                interactions.Add("True-final-boss Covenant birth effects require separate calibration.");
            var definitions = pattern.GetTrueFinalBosses().SelectMany(group => group.characters).Select(data =>
            {
                EnemyDefinition raw = EnemySpawningProbe.Definition(data, true);
                // These flags guard later boss actions/status removal; this
                // boundary captures creation and OnSpawn only.
                return new EnemyDefinition(raw.Unit, raw.Ascends, raw.Loops,
                    raw.ExternalInteractions.Where(value => value != "Spawned outer Boss actions" &&
                        value != "Spawned status has custom relentless removal").ToArray(), raw.CompanionBoss,
                        TrueFinalBossCovenantScenario.CaptureStats(data));
            }).ToArray();
            externalInteractions = interactions;
            return definitions;
        }
        internal static void Complete(HeroManager heroes, bool nativeReturned)
        {
            if (!Enabled) return;
            if (before == null) throw new InvalidOperationException("Missing native true-final-boss spawn input.");
            TrueFinalBossSpawnState actual = Capture(heroes, before.Definitions, before.ExternalInteractions);
            using (var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, GameModuleMvid = typeof(CharacterState).Assembly.ManifestModule.ModuleVersionId,
                Boundary = "DoTrueFinalBossSpawning", Route = TrueFinalBossBirthProbe.Control,
                NativeReturned = nativeReturned, Before = before, Actual = actual, RngBefore = rngBefore, RngAfter = Rng()
            }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                "true-final-boss-spawn-calibration.mt2f"))) document.Write(output);
        }
    }
}

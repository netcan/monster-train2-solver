using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    // Pure capture is shared with the bounded player gate. Diagnostic writers
    // stay exclusive to the development assembly.
    internal static class TrueFinalBossTurnProbe
    {
        internal static bool Enabled
        {
            get
            {
#if DEVELOPMENT_PROBE
                return Environment.GetEnvironmentVariable("MT2_PROBE_ORIGINAL_TRUE_FINAL_BOSS") == "1" ||
                    Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_SCHEDULED_SPAWN") == "1";
#else
                return false;
#endif
            }
        }
        internal static bool CaptureEnabled => Enabled || PlayerTrueFinalBossProbe.Enabled;
        private static BattleTurnState? before;
        internal static bool HasQueuedBoss => CaptureEnabled && AllGameManagers.Instance?.GetHeroManager() is HeroManager heroes &&
            ((List<int>)AccessTools.Field(typeof(HeroManager), "queuedTrueFinalBosses").GetValue(heroes)).Count > 0;
        internal static TrueFinalBossSpawnDefinition? CaptureQueued(HeroManager heroes)
        {
            if (!HasQueuedBoss || heroes.GetOuterTrainBossCharacter() != null) return null;
            IReadOnlyList<EnemyDefinition> definitions = TrueFinalBossSpawnCalibration.CaptureDefinitions(heroes, out IReadOnlyList<string> interactions);
            SpawnPatternData pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(heroes);
            CharacterData top = pattern.GetTrueFinalBosses().SelectMany(group => group.characters)
                .Single(data => data.GetAssetKey() == "Boss_Level9_Entropy_Top_V2");
            return new TrueFinalBossSpawnDefinition(definitions,
                ((List<int>)AccessTools.Field(typeof(HeroManager), "queuedTrueFinalBosses").GetValue(heroes)).ToArray(),
                OuterBossPhaseProbe.Definition(top), interactions);
        }
        internal static TrueFinalBossPhaseFlags? CaptureFlags(HeroManager heroes)
        {
            if (!CaptureEnabled || heroes.GetOuterTrainBossCharacter()?.IsTrueFinalBoss() != true) return null;
            return new TrueFinalBossPhaseFlags((bool)AccessTools.Field(typeof(HeroManager), "hasHellforgedBoss").GetValue(heroes),
                (bool)AccessTools.Field(typeof(HeroManager), "hasHellforgedBossWaitedOnRelentless").GetValue(heroes),
                ((List<int>)AccessTools.Field(typeof(HeroManager), "queuedTrueFinalBosses").GetValue(heroes)).Count);
        }
        internal static void Begin()
        {
            if (Enabled) before = FullBattleTrace.Active!.CaptureDecision();
        }
        internal static void Complete(bool nativeReturned)
        {
            if (!Enabled) return;
            if (before == null) throw new InvalidOperationException("Missing scheduled true-final-boss birth input.");
            using (var document = NativeFixtureCapture.Capture(new
            {
                Schema = 1, GameModuleMvid = typeof(CharacterState).Assembly.ManifestModule.ModuleVersionId,
                Boundary = "ScheduledTrueFinalBossBirth", Route = TrueFinalBossBirthProbe.Control, NativeReturned = nativeReturned,
                Before = before, Actual = FullBattleTrace.Active!.CaptureDecision()
            }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                "true-final-boss-scheduled-spawn-calibration.mt2f"))) document.Write(output);
        }
    }
}

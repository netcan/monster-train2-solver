#if DEVELOPMENT_PROBE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json;

namespace MonsterTrain2Poju.Probe
{
    internal static class AngelDuoForcedRelentlessProbe
    {
        internal static bool Enabled => Environment.GetEnvironmentVariable("MT2_PROBE_ANGEL_DUO_FORCE_RELENTLESS") == "1";
        private sealed class Sample
        {
            public ForcedRelentlessProgress Before { get; set; } = null!;
            public ForcedRelentlessProgress Actual { get; set; } = null!;
            public BattleTurnState BeforeBattle { get; set; } = null!;
            public BattleTurnState ActualBattle { get; set; } = null!;
        }
        private static readonly List<Sample> Samples = new List<Sample>();
        private static BattleTurnState? Original, Restored;

        private static int Int(object actor, string name) => (int)AccessTools.Field(actor.GetType(), name).GetValue(actor);
        private static void Set(object actor, string name, int value) => AccessTools.Field(actor.GetType(), name).SetValue(actor, value);

        internal static void Run()
        {
            if (!Enabled || Original != null) return;
            FullBattleTrace trace = FullBattleTrace.Active!;
            AllGameManagers managers = AllGameManagers.Instance;
            CombatManager combat = managers.GetCombatManager(); HeroManager heroes = managers.GetHeroManager();
            if (!OuterBossDefinitions.IsAngelDuo(heroes.GetOuterTrainBossCharacter()?.GetSourceCharacterData().GetAssetKey() ?? ""))
                throw new InvalidOperationException("Forced Relentless calibration requires the original Angel Duo.");
            int turn = Int(combat, "turnCounter"), phase = Int(heroes, "spawnPatternPhase");
            int remaining = Int(combat, "turnsRemainingWhenRelentlessTriggeredEarly"), waves = Int(combat, "totalWaves");
            int modifier = Int(combat, "turnCountModifier");
            var pattern = (SpawnPatternData)AccessTools.Field(typeof(HeroManager), "spawnPattern").GetValue(heroes);
            object signal = combat.turnCountSignal;
            void Restore()
            {
                Set(combat, "turnCounter", turn); Set(heroes, "spawnPatternPhase", phase);
                Set(combat, "turnsRemainingWhenRelentlessTriggeredEarly", remaining);
                AccessTools.Method(signal.GetType(), "Dispatch", new[] { typeof(int), typeof(int), typeof(int), typeof(bool) })
                    .Invoke(signal, new object[] { turn, modifier, waves, pattern.GetIsLoopingScenario() });
            }
            Original = trace.CaptureDecision(true, true);
            try
            {
                foreach (int candidate in new[] { 0, 1, waves - 2, waves - 1, waves }.Distinct())
                {
                    Restore();
                    Set(combat, "turnCounter", candidate); Set(heroes, "spawnPatternPhase", Math.Min(2, waves - 1));
                    Set(combat, "turnsRemainingWhenRelentlessTriggeredEarly", 9);
                    var row = new Sample { Before = new ForcedRelentlessProgress(candidate, waves,
                        Int(heroes, "spawnPatternPhase"), 9), BeforeBattle = trace.CaptureDecision(true, true) };
                    combat.ForceRelentless();
                    row.Actual = new ForcedRelentlessProgress(Int(combat, "turnCounter"), waves,
                        Int(heroes, "spawnPatternPhase"), Int(combat, "turnsRemainingWhenRelentlessTriggeredEarly"));
                    row.ActualBattle = trace.CaptureDecision(true, true); Samples.Add(row);
                }
            }
            finally { Restore(); Restored = trace.CaptureDecision(true, true); }
            if (JsonConvert.SerializeObject(Original) != JsonConvert.SerializeObject(Restored))
                throw new InvalidOperationException("Forced Relentless native calibration did not restore complete battle state/RNG.");
        }

        internal static void Write(ManualLogSource log)
        {
            if (!Enabled) return;
            if (Samples.Count != 5 || Original == null || Restored == null)
                throw new InvalidOperationException("Incomplete forced Relentless native calibration.");
            using (var document = NativeFixtureCapture.Capture(new { Schema = 1,
                GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                Control = "TemporaryNativeTurnSpawnPhaseAndScoringCounter", Original, Samples, Restored }))
            using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                "angel-duo-forced-relentless-calibration.mt2f"))) document.Write(output);
            log.LogInfo("ANGEL-DUO-FORCED-RELENTLESS-CAPTURE samples=" + Samples.Count + " restored=True");
        }
    }
}
#endif

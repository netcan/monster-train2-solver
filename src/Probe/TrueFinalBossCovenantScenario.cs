using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using MonsterTrain2Poju.Model;

namespace MonsterTrain2Poju.Probe
{
    // Controlled original route only; this does not unlock a Covenant run.
    internal static class TrueFinalBossCovenantScenario
    {
        internal static object? Evidence { get; private set; }
        private static readonly List<object> statQueries = new List<object>();
        private static readonly List<object> waveStatQueries = new List<object>();
        private static readonly HashSet<string> waveAssets = new HashSet<string>();

        internal static void Prepare(SaveManager save)
        {
            string configured = Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_COVENANT") ?? "";
            if (configured.Length == 0) return;
            string[] assets = configured.Split(';');
            string[] supported = { "Covenant00_FTUE", "Covenant01_StrongerEnemiesMoreCards",
                "Covenant04_StrongerBosses", "Covenant08_StrongerEnemies" };
            if (!TrueFinalBossBirthProbe.RouteEnabled || Evidence != null || assets.Any(asset => !supported.Contains(asset)) ||
                assets.Distinct().Count() != assets.Length || save.GetCovenants().Count != 0)
                throw new InvalidOperationException("Covenant observation requires a fresh isolated original route.");
            CovenantData[] definitions = assets.Select(asset => save.GetAllGameData().GetAllCovenantDatas().Single(item => item.name == asset)).ToArray();
            object before = Rng();
            foreach (CovenantData data in definitions)
                ((IList<CovenantState>)save.GetCovenants()).Add(new CovenantState(data));
            object after = Rng();
            string[] ids = definitions.Select(data => data.GetID()).ToArray();
            if (!save.GetCovenants().Select(covenant => covenant.GetRelicDataID()).SequenceEqual(ids) ||
                JsonConvert.SerializeObject(before) != JsonConvert.SerializeObject(after))
                throw new InvalidOperationException("Controlled Covenant activation changed RNG or the active definition.");
            Evidence = new
            {
                Kind = assets.Length == 1 ? "ControlledOriginalCovenantBeforeTFBBirth" : "ControlledOriginalCovenantsBeforeTFBBirth",
                AssetKey = assets.Length == 1 ? assets[0] : null, Id = assets.Length == 1 ? ids[0] : null,
                EffectTypes = assets.Length == 1 ? definitions[0].GetEffects().Select(effect => effect.GetEffectClassName()).ToArray() : null,
                Owners = definitions.Select(data => new { AssetKey = data.name, Id = data.GetID(),
                    EffectTypes = data.GetEffects().Select(effect => effect.GetEffectClassName()).ToArray() }).ToArray(),
                ActiveBefore = Array.Empty<string>(), ActiveAfter = ids,
                StartOfRunEffectsApplied = false, RngBefore = before, RngAfter = after, StatQueries = statQueries,
                WaveStatQueries = waveStatQueries
            };
        }

        internal static EnemyBirthStatAdjustment? CaptureStats(CharacterData data) => CaptureStats(data, false);

        internal static EnemyBirthStatAdjustment? CaptureWaveStats(CharacterData data) =>
            data.name.StartsWith("Boss_Level9_", StringComparison.Ordinal) ? null : CaptureStats(data, true);

        private static EnemyBirthStatAdjustment? CaptureStats(CharacterData data, bool wave)
        {
            if (Evidence == null) return CovenantBirthStatProbe.Capture(data);
            EnemyBirthStatAdjustment adjustment = CovenantBirthStatProbe.Query(data, out object evidence);
            if (!wave || waveAssets.Add(data.name))
                (wave ? waveStatQueries : statQueries).Add(evidence);
            return adjustment;
        }

        private static object Rng() => Enum.GetValues(typeof(RngId)).Cast<RngId>().Select(id =>
            new { Id = id.ToString(), Words = RngCalibration.Words(RandomManager.GetState(id)) }).ToArray();
    }
}

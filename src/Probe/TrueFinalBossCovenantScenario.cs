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
            string asset = Environment.GetEnvironmentVariable("MT2_PROBE_TRUE_FINAL_BOSS_COVENANT") ?? "";
            if (asset.Length == 0) return;
            string[] supported = { "Covenant00_FTUE", "Covenant01_StrongerEnemiesMoreCards",
                "Covenant04_StrongerBosses", "Covenant08_StrongerEnemies" };
            if (!TrueFinalBossBirthProbe.RouteEnabled || Evidence != null || !supported.Contains(asset) || save.GetCovenants().Count != 0)
                throw new InvalidOperationException("Covenant observation requires a fresh isolated original route.");
            CovenantData data = save.GetAllGameData().GetAllCovenantDatas().Single(item => item.name == asset);
            object before = Rng();
            ((IList<CovenantState>)save.GetCovenants()).Add(new CovenantState(data));
            object after = Rng();
            if (save.GetCovenants().Count != 1 || save.GetCovenants()[0].GetRelicDataID() != data.GetID() ||
                JsonConvert.SerializeObject(before) != JsonConvert.SerializeObject(after))
                throw new InvalidOperationException("Controlled Covenant activation changed RNG or the active definition.");
            Evidence = new
            {
                Kind = "ControlledOriginalCovenantBeforeTFBBirth", AssetKey = asset, Id = data.GetID(),
                EffectTypes = data.GetEffects().Select(effect => effect.GetEffectClassName()).ToArray(),
                ActiveBefore = Array.Empty<string>(), ActiveAfter = new[] { data.GetID() },
                StartOfRunEffectsApplied = false, RngBefore = before, RngAfter = after, StatQueries = statQueries,
                WaveStatQueries = waveStatQueries
            };
        }

        internal static EnemyBirthStatAdjustment? CaptureStats(CharacterData data) => CaptureStats(data, false);

        internal static EnemyBirthStatAdjustment? CaptureWaveStats(CharacterData data) =>
            data.name.StartsWith("Boss_Level9_", StringComparison.Ordinal) ? null : CaptureStats(data, true);

        private static EnemyBirthStatAdjustment? CaptureStats(CharacterData data, bool wave)
        {
            if (Evidence == null) return null;
            int frame = UnityEngine.Time.frameCount;
            object before = Rng();
            CharacterStatAdjustment adjustment = AllGameManagers.Instance.GetRelicManager().GetEnemyStatAdjustmentsFromAllRelics(data);
            object after = Rng();
            if (frame != UnityEngine.Time.frameCount || JsonConvert.SerializeObject(before) != JsonConvert.SerializeObject(after))
                throw new InvalidOperationException("Covenant stat preview changed native RNG or frame.");
            if (!wave || waveAssets.Add(data.name))
                (wave ? waveStatQueries : statQueries).Add(new { AssetKey = data.name, RawAttack = data.GetAttackDamage(), RawHealth = data.GetHealth(),
                    Attack = adjustment.attackAdjustment, Health = adjustment.healthAdjustment,
                    FrameBefore = frame, FrameAfter = UnityEngine.Time.frameCount, RngBefore = before, RngAfter = after });
            return new EnemyBirthStatAdjustment(adjustment.attackAdjustment, adjustment.healthAdjustment,
                AllGameManagers.Instance.GetSaveManager().GetCovenants().Select(covenant => covenant.GetRelicDataID()).ToArray());
        }

        private static object Rng() => Enum.GetValues(typeof(RngId)).Cast<RngId>().Select(id =>
            new { Id = id.ToString(), Words = RngCalibration.Words(RandomManager.GetState(id)) }).ToArray();
    }
}

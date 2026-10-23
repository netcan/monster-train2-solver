using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace MonsterTrain2Poju.Probe
{
    // Explicit isolated activation after startup; production capture never calls Prepare.
    internal static class CovenantEnemyStatsScenario
    {
        internal static object? Evidence { get; private set; }
        private static readonly List<object> queries = new List<object>();
        private static readonly HashSet<string> observed = new HashSet<string>();
        internal static bool Enabled => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MT2_PROBE_ENEMY_STAT_COVENANTS"));

        internal static void Prepare(SaveManager save)
        {
            if (!Enabled || Evidence != null) return;
            string[] assets = Environment.GetEnvironmentVariable("MT2_PROBE_ENEMY_STAT_COVENANTS")!.Split(';');
            if (save.GetCurrentDistance() != 0 || save.GetCovenants().Count != 0 ||
                !assets.Contains("Covenant01_StrongerEnemiesMoreCards") || assets.Distinct().Count() != assets.Length ||
                assets.Any(asset => asset is not ("Covenant00_FTUE" or "Covenant01_StrongerEnemiesMoreCards")))
                throw new InvalidOperationException("Enemy-stat Covenant control requires a fresh original first battle and unique calibrated definitions.");
            CovenantData[] definitions = assets.Select(asset => save.GetAllGameData().GetAllCovenantDatas().Single(data => data.name == asset)).ToArray();
            string[] deck = save.GetDeckState().Select(card => card.GetCardDataID()).ToArray();
            object before = Rng();
            foreach (CovenantData data in definitions) ((IList<CovenantState>)save.GetCovenants()).Add(new CovenantState(data));
            object after = Rng();
            string[] deckAfter = save.GetDeckState().Select(card => card.GetCardDataID()).ToArray();
            if (!deck.SequenceEqual(deckAfter) || JsonConvert.SerializeObject(before) != JsonConvert.SerializeObject(after))
                throw new InvalidOperationException("Enemy-stat Covenant activation changed deck or RNG.");
            Evidence = new { Kind = "ControlledOriginalEnemyStatCovenantsBeforeBattle", Distance = save.GetCurrentDistance(),
                ActiveBefore = Array.Empty<string>(), ActiveAfter = save.GetCovenants().Select(owner => owner.GetRelicDataID()).ToArray(),
                Owners = definitions.Select(data => new { AssetKey = data.name, Id = data.GetID(),
                    EffectTypes = data.GetEffects().Select(effect => effect.GetEffectClassName()).ToArray() }).ToArray(),
                StartingDeckBefore = deck, StartingDeckAfter = deckAfter, StartOfRunEffectsApplied = false,
                RngBefore = before, RngAfter = after, StatQueries = queries };
        }

        internal static void Observe(CharacterData data, object evidence)
        {
            if (Evidence != null && observed.Add(data.name)) queries.Add(evidence);
        }

        private static object Rng() => Enum.GetValues(typeof(RngId)).Cast<RngId>().Select(id =>
            new { Id = id.ToString(), Words = RngCalibration.Words(RandomManager.GetState(id)) }).ToArray();
    }
}

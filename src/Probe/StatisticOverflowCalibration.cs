using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class StatisticOverflowCalibration
    {
        internal static void Capture(FullBattleTrace trace)
        {
            AllGameManagers managers = AllGameManagers.Instance!;
            CombatContext original = trace.CaptureContext();
            CardStatistics live = managers.GetCardStatistics();
            CardState[] owned = managers.GetCardManager()!.GetAllCards(new List<CardState>()).ToArray();
            SubtypeData[] subtypes = owned.Select(card => card.GetSpawnCharacterData()).Where(data => data != null)
                .SelectMany(data => data!.GetSubtypes()).Distinct().Take(2).ToArray();
            if (owned.Length < 2 || subtypes.Length == 0) throw new InvalidOperationException("Overflow calibration requires owned cards and subtypes.");
            var host = new GameObject("Poju statistic overflow calibration") { hideFlags = HideFlags.HideAndDontSave };
            host.SetActive(false);
            var native = host.AddComponent<CardStatistics>();
            var samples = new List<object>();
            try
            {
                foreach (string name in new[] { "allGameManagers", "cardManager", "heroManager", "monsterManager", "roomManager", "saveManager",
                    "relicManager", "playerManager", "combatManager" })
                    AccessTools.Field(typeof(CardStatistics), name).SetValue(native, AccessTools.Field(typeof(CardStatistics), name).GetValue(live));
                void Set(string name, object value) => AccessTools.Field(typeof(CardStatistics), name).SetValue(native, value);
                BattleStatistics Snapshot() => BattleStatisticsProbe.Capture(native, trace.CardId, owned);
                int[] seeds = { int.MaxValue, int.MaxValue - 1, int.MinValue, int.MinValue + 1, -1 };
                foreach (string type in new[] { "AnyStatusEffectStacksAdded", "TimesDrawn" })
                foreach (int seed in seeds)
                foreach (int amount in new[] { 1, 2, 9999, -1, -9999, int.MinValue })
                {
                    var entries = new Dictionary<CardState, CardStatsEntry>();
                    var tracked = (CardStatistics.TrackedValueType)Enum.Parse(typeof(CardStatistics.TrackedValueType), type);
                    foreach (CardState card in owned)
                    {
                        var entry = new CardStatsEntry();
                        foreach (CardStatistics.EntryDuration duration in new[] { CardStatistics.EntryDuration.ThisTurn, CardStatistics.EntryDuration.ThisBattle })
                        {
                            entry.IncrementValue(tracked, seed, duration);
                            if (type == "TimesDrawn") entry.IncrementValue(CardStatistics.TrackedValueType.AnyCardDrawn, seed, duration);
                        }
                        entries.Add(card, entry);
                    }
                    Set("deckStats", entries); Set("cardsPlayedThisTurn", new List<CardState>());
                    BattleStatistics before = Snapshot();
                    native.IncrementStat(owned[0], tracked, amount);
                    BattleStatistics after = Snapshot();
                    if (!JToken.DeepEquals(JToken.FromObject(before.Increment(trace.CardId(owned[0]), type, amount)), JToken.FromObject(after)))
                        throw new InvalidOperationException("Native statistic boundary differs: " + type + "/" + seed + "/" + amount);
                    samples.Add(new { Kind = "Increment", Type = type, SourceCardId = trace.CardId(owned[0]), Amount = amount,
                        Before = before, After = after });
                }
                foreach (int seed in seeds)
                {
                    Set("numMonstersSpawnedThisTurnPerFloor", new Dictionary<int, int> { [0] = seed });
                    Set("numMonstersSpawnedThisBattlePerFloor", new Dictionary<int, int> { [0] = seed });
                    Set("numMonsterSubtypesSpawnedThisTurn", subtypes.ToDictionary(subtype => subtype, _ => seed));
                    Set("numMonsterSubtypesSpawnedThisBattle", subtypes.ToDictionary(subtype => subtype, _ => seed));
                    BattleStatistics before = Snapshot();
                    foreach (string field in new[] { "numMonstersSpawnedThisTurnPerFloor", "numMonstersSpawnedThisBattlePerFloor" })
                        AccessTools.Method(typeof(CardStatistics), "IncrementSpawnDictionaryEntry").Invoke(native,
                            new[] { AccessTools.Field(typeof(CardStatistics), field).GetValue(native), (object)0 });
                    foreach (string field in new[] { "numMonsterSubtypesSpawnedThisTurn", "numMonsterSubtypesSpawnedThisBattle" })
                        AccessTools.Method(typeof(CardStatistics), "IncrementSubtypeDictionaryEntry").Invoke(native,
                            new[] { (object)subtypes.ToList(), AccessTools.Field(typeof(CardStatistics), field).GetValue(native) });
                    BattleStatistics after = Snapshot();
                    string[] keys = subtypes.Select(subtype => subtype.Key).ToArray();
                    if (!JToken.DeepEquals(JToken.FromObject(before.Spawn(0, keys)), JToken.FromObject(after)))
                        throw new InvalidOperationException("Native spawn boundary differs.");
                    samples.Add(new { Kind = "Spawn", RoomIndex = 0, Subtypes = keys, Before = before, After = after });
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
            if (!JToken.DeepEquals(JToken.FromObject(original), JToken.FromObject(trace.CaptureContext())))
                throw new InvalidOperationException("Statistic overflow calibration changed live state.");
            File.WriteAllText(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "statistic-overflow-calibration.json"),
                JsonConvert.SerializeObject(new { Schema = 1, GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                    SyntheticCounters = true, LiveContextUnchanged = true, Samples = samples }, Formatting.Indented));
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Model;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MonsterTrain2Poju.Probe
{
    internal static class MonsterDamageStatisticsCalibration
    {
        internal static void Capture(FullBattleTrace trace)
        {
            var method = AccessTools.Method(typeof(CardStatistics), "OnMonstersDamaged");
            if (method == null) throw new InvalidOperationException("Monster damage calibration requires game 2.3.1.");
            AllGameManagers managers = AllGameManagers.Instance!;
            CombatContext original = trace.CaptureContext();
            CardState[] owned = managers.GetCardManager().GetAllCards(new List<CardState>()).ToArray();
            if (owned.Length < 2 || owned.Any(card => card.GetTraitStates().Any(trait =>
                trait.GetParamTrackedValue().ToString() == "DamageDealtToMonsters" ||
                trait.GetParamTrackedValue().ToString() == "FriendlyDamageDealtToMonsters")))
                throw new InvalidOperationException("Statistic component requires ordinary owned cards without damage callbacks.");
            var host = new GameObject("Poju monster damage statistics") { hideFlags = HideFlags.HideAndDontSave };
            host.SetActive(false);
            var native = host.AddComponent<CardStatistics>();
            var previewSave = host.AddComponent<SaveManager>();
            AccessTools.Field(typeof(SaveManager), "_previewMode").SetValue(previewSave, true);
            var fields = new[] { "damageDealtToMonstersThisTurn", "damageDealtToMonstersThisBattle",
                "friendlyDamageDealtToMonstersThisTurn", "friendlyDamageDealtToMonstersThisBattle" };
            var rows = new List<object>();
            var queries = new List<object>();
            var signals = new List<string>();
            native.AddOnStatIncrementedSignal((card, type) =>
            {
                if (card != null) throw new InvalidOperationException("Global damage signal unexpectedly has a source card.");
                signals.Add(type.ToString());
            });
            void Set(string name, object value) => AccessTools.Field(typeof(CardStatistics), name).SetValue(native, value);
            BattleStatistics Snapshot() => BattleStatisticsProbe.Capture(native, trace.CardId, owned);
            void Match(BattleStatistics predicted, BattleStatistics actual, string label)
            {
                if (!JToken.DeepEquals(JToken.FromObject(predicted), JToken.FromObject(actual)))
                    throw new InvalidOperationException("Monster damage statistics differ: " + label);
            }
            try
            {
                CardStatistics live = managers.GetCardStatistics();
                foreach (string name in new[] { "allGameManagers", "cardManager", "heroManager", "monsterManager", "roomManager",
                    "saveManager", "relicManager", "playerManager", "combatManager" })
                    Set(name, AccessTools.Field(typeof(CardStatistics), name).GetValue(live));
                foreach (int seed in new[] { 0, int.MaxValue - 1, int.MinValue })
                foreach (bool preview in new[] { false, true })
                foreach (var input in new[] { (0, 0), (-1, -2), (5, 0), (0, 3), (5, 3), (-7, 4), (2, int.MaxValue) })
                {
                    Set("saveManager", preview ? previewSave : managers.GetSaveManager());
                    Set("deckStats", new Dictionary<CardState, CardStatsEntry> { [owned[0]] = new CardStatsEntry() });
                    foreach (string field in fields) Set(field, seed);
                    signals.Clear();
                    BattleStatistics before = Snapshot();
                    method.Invoke(native, new object[] { input.Item1, input.Item2 });
                    BattleStatistics actual = Snapshot();
                    Match(preview ? before : before.RecordMonsterDamage(input.Item1, input.Item2, owned.Select(trace.CardId)), actual, "increment");
                    string[] expectedSignals = preview ? Array.Empty<string>() :
                        (input.Item1 > 0 ? new[] { "DamageDealtToMonsters" } : Array.Empty<string>()).Concat(
                            input.Item2 > 0 ? new[] { "FriendlyDamageDealtToMonsters" } : Array.Empty<string>()).ToArray();
                    if (!signals.SequenceEqual(expectedSignals)) throw new InvalidOperationException("Monster damage signal order differs.");
                    rows.Add(new { Kind = "Increment", Amount = input.Item1, FriendlyAmount = input.Item2,
                        Preview = preview, Before = before, Actual = actual, Signals = signals.ToArray() });
                }
                Set("saveManager", managers.GetSaveManager());
                foreach (string field in fields) Set(field, 17);
                foreach (string type in new[] { "DamageDealtToMonsters", "FriendlyDamageDealtToMonsters" })
                foreach (CardStatistics.EntryDuration duration in Enum.GetValues(typeof(CardStatistics.EntryDuration)))
                {
                    BattleStatistics before = Snapshot();
                    int value = native.GetStatValue(new CardStatistics.StatValueData { cardState = owned[0],
                        trackedValue = (CardStatistics.TrackedValueType)Enum.Parse(typeof(CardStatistics.TrackedValueType), type),
                        entryDuration = duration, cardTypeTarget = CardStatistics.CardTypeTarget.Any });
                    queries.Add(new { Type = type, Duration = duration.ToString(), SourceCardId = trace.CardId(owned[0]),
                        Value = value, Before = before, Actual = Snapshot() });
                }
                BattleStatistics turnBefore = Snapshot();
                native.UpdateStatsForNextTurn();
                BattleStatistics turnActual = Snapshot();
                Match(turnBefore.NextTurn(original.Gold), turnActual, "next turn");
                rows.Add(new { Kind = "NextTurn", Before = turnBefore, Actual = turnActual });
                native.OnBattleEnd();
                if (fields.Any(field => (int)AccessTools.Field(typeof(CardStatistics), field).GetValue(native) != 0))
                    throw new InvalidOperationException("Native battle end retained damage counters.");
                if (!JToken.DeepEquals(JToken.FromObject(original), JToken.FromObject(trace.CaptureContext())))
                    throw new InvalidOperationException("Monster damage calibration changed live context.");
                using (var document = NativeFixtureCapture.Capture(new { Schema = 1, GameVersion = Application.version,
                    GameModuleMvid = typeof(CardStatistics).Assembly.ManifestModule.ModuleVersionId,
                    Boundary = "IsolatedNativeOnMonstersDamaged", LiveContextUnchanged = true, BattleEndCountersZero = true,
                    Rows = rows, Queries = queries }))
                using (var output = File.Create(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!,
                    "monster-damage-statistics.mt2f"))) document.Write(output);
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
        }
    }
}

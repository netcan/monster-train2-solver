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
    internal static class StatisticQueryCalibration
    {
        internal static void Capture(FullBattleTrace trace)
        {
            AllGameManagers managers = AllGameManagers.Instance!;
            SaveManager save = managers.GetSaveManager();
            string? relicError = RelicModel.Validate(RelicProbe.Capture(managers));
            if (relicError != null) throw new InvalidOperationException(relicError);
            CombatContext original = trace.CaptureContext();
            CardStatistics live = managers.GetCardStatistics();
            CardState[] owned = managers.GetCardManager()!.GetAllCards(new List<CardState>()).ToArray();
            CardState[] deck = save.GetDeckState().ToArray();
            CardState[] known = owned.Concat(deck).Distinct().ToArray();
            var ghost = new CardState();
            var variable = new CardState();
            AccessTools.Field(typeof(CardState), "cost").SetValue(variable, 2);
            AccessTools.Field(typeof(CardState), "costType").SetValue(variable, CardData.CostType.ConsumeRemainingEnergy);
            int ghostId = original.NextCardId;
            int Id(CardState card) => ReferenceEquals(card, ghost) ? ghostId : ReferenceEquals(card, variable) ? ghostId + 1 : trace.CardId(card);
            // Calibration inputs contain only these owned/permanent definitions.
            var definitions = known.Select(card => save.GetAllGameData().FindCardData(card.GetCardDataID())!).Distinct().ToArray();
            CardState[] sources = { known[0], known.First(card => card.GetCardType() == CardType.Spell) };
            SubtypeData[] subtypes = known.Select(card => card.GetSpawnCharacterData()).Where(data => data != null)
                .SelectMany(data => data!.GetSubtypes()).Distinct().Take(2).ToArray();
            string[] supportedTypes = { "TypeInDeck", "TypeInDiscardPile", "TypeInExhaustPile", "TypeInDrawPile", "TypeInEatenPile",
                "SubtypeInDeck", "SubtypeInDiscardPile", "SubtypeInExhaustPile", "SubtypeInDrawPile", "SubtypeInEatenPile",
                "HeroesKilled", "SpawnedMonsterDeaths", "TimesDiscarded", "TimesPlayed", "TimesDrawn", "TimesExhausted", "LastSacrificedMonsterStats",
                "AnyHeroKilled", "AnyMonsterDeath", "AnyDiscarded", "AnyCardPlayed", "AnyCardDrawn", "AnyExhausted", "AnyMonsterSpawned",
                "AnyMonsterSpawnedTopFloor", "MonsterSubtypePlayed", "AnyStatusEffectStacksAdded", "AnyStatusEffectStacksRemoved",
                "PlayedCost", "UnmodifiedPlayedCost", "Gold", "TurnCount", "ForgePoints", "DragonsHoardAmount", "MoonPhase",
                "PyreHeartResurrection", "LastAttackDamageDealt", "EnergyRemainingEndOfTurn", "AnyCharacter", "NumSpecificCardsInDeck" };
            var batches = new List<object>();
            var host = new GameObject("Poju statistic query calibration") { hideFlags = HideFlags.HideAndDontSave };
            host.SetActive(false); // These isolated components never register with the real providers.
            var native = host.AddComponent<CardStatistics>();
            var emptyCards = host.AddComponent<CardManager>();
            var stoppedCombat = host.AddComponent<CombatManager>();
            try
            {
                foreach (string name in new[] { "allGameManagers", "cardManager", "heroManager", "monsterManager", "roomManager", "saveManager",
                    "relicManager", "playerManager", "combatManager" })
                    AccessTools.Field(typeof(CardStatistics), name).SetValue(native, AccessTools.Field(typeof(CardStatistics), name).GetValue(live));
                foreach (string layout in new[] { "Live", "Distributed", "Empty" })
                foreach (bool withPaidCost in new[] { false, true })
                {
                    bool empty = layout == "Empty";
                    void Set(string name, object value) => AccessTools.Field(typeof(CardStatistics), name).SetValue(native, value);
                    Set("cardManager", layout == "Live" ? managers.GetCardManager()! : emptyCards);
                    Set("combatManager", empty ? stoppedCombat : managers.GetCombatManager()!);
                    var entries = new Dictionary<CardState, CardStatsEntry>();
                    foreach (CardState card in known.Concat(new[] { ghost }))
                    {
                        var entry = new CardStatsEntry();
                        if (sources.Contains(card) || card == ghost)
                        foreach (CardStatistics.EntryDuration duration in Enum.GetValues(typeof(CardStatistics.EntryDuration)))
                        foreach (string type in supportedTypes)
                            entry.IncrementValue((CardStatistics.TrackedValueType)Enum.Parse(typeof(CardStatistics.TrackedValueType), type),
                                Id(card) * 3 + (int)duration * 7 + type.Length, duration);
                        entries.Add(card, entry);
                    }
                    Set("deckStats", entries);
                    if (withPaidCost) entries[sources[0]].IncrementValue(CardStatistics.TrackedValueType.TimesPlayed,
                        int.MaxValue - entries[sources[0]].GetValue(CardStatistics.TrackedValueType.TimesPlayed, CardStatistics.EntryDuration.ThisTurn));
                    Set("playedCardCostStats", withPaidCost ? new Dictionary<CardState, int> { [sources[0]] = -2, [sources[1]] = 5, [variable] = 3 } : new Dictionary<CardState, int>());
                    Set("numMonstersSpawnedThisTurnPerFloor", new Dictionary<int, int> { [0] = 2, [1] = 4, [2] = 9 });
                    Set("numMonstersSpawnedThisBattlePerFloor", new Dictionary<int, int> { [0] = 5, [2] = 10 });
                    Set("numMonsterSubtypesSpawnedThisTurn", subtypes.ToDictionary(subtype => subtype, _ => 3));
                    Set("numMonsterSubtypesSpawnedThisBattle", subtypes.ToDictionary(subtype => subtype, _ => 11));
                    Set("numMonstersDeadThisTurn", 23); Set("numMonstersDeadThisBattle", 47);
                    Set("goldStartOfThisTurn", 19); Set("energyRemainingEndOfTurn", 6); Set("lastAttackDamageDealt", 31);
                    bool distributed = layout == "Distributed";
                    void Cards(string field, params CardState[] values) => AccessTools.Field(typeof(CardManager), field).SetValue(emptyCards, values.ToList());
                    Cards("pileHand", distributed ? new[] { known[0] } : Array.Empty<CardState>());
                    Cards("pileDeck", distributed ? new[] { known[1] }.Concat(known.Skip(8)).ToArray() : Array.Empty<CardState>());
                    Cards("pileDiscard", distributed ? new[] { known[2] } : Array.Empty<CardState>());
                    Cards("exhaustedCards", distributed ? new[] { known[4] } : Array.Empty<CardState>());
                    Cards("eatenCards", distributed ? new[] { known[5] } : Array.Empty<CardState>());
                    Cards("purgedCards", distributed ? new[] { known[6] } : Array.Empty<CardState>());
                    Cards("pileDiscardBuffer", distributed ? new[] { known[0], known[7] } : Array.Empty<CardState>());
                    AccessTools.Field(typeof(CardManager), "pileStandBy").SetValue(emptyCards, distributed ?
                        new Dictionary<CardState, RemoveFromStandByCondition> { [known[3]] = null! } : new Dictionary<CardState, RemoveFromStandByCondition>());
                    CardState[] current = layout == "Live" ? owned : emptyCards.GetAllCards(new List<CardState>()).ToArray();
                    BattleStatistics Snapshot() => BattleStatisticsProbe.Capture(native, Id, current);
                    CardToken[] Tokens(IEnumerable<CardState> list) => list.Select(card => new CardToken(Id(card), card.GetCardDataID())).ToArray();
                    CardCycleState cards = layout == "Live" ? original.Cards : new CardCycleState(Tokens(emptyCards.GetHand()), Tokens(emptyCards.GetDrawPile()), Tokens(emptyCards.GetDiscardPile()),
                        original.Cards.Rng, original.Cards.DrawModifier, original.Cards.ExternalInteractions);
                    CardPileState[] piles = layout == "Live" ? original.OtherPiles!.ToArray() : new[] {
                        new CardPileState("Standby", distributed ? Tokens(new[] { known[3] }) : Array.Empty<CardToken>(), distributed ? new[] { Id(known[3]) } : Array.Empty<int>(), Array.Empty<int>()),
                        new CardPileState("Exhausted", Tokens(emptyCards.GetExhaustedPile())), new CardPileState("Eaten", Tokens(emptyCards.GetEatenPile())),
                        new CardPileState("Purged", Tokens(emptyCards.GetPurgedPile())), new CardPileState("DiscardBuffer", Tokens(emptyCards.GetDiscardBufferPile())) };
                    var before = new CombatContext(cards, original.BattleRng, original.Gold, ghostId + 2, original.MaxHandSize,
                        original.StatusRules, Snapshot(), empty ? Array.Empty<CardInstanceState>() : original.CardInstances,
                        original.CardRegistry!.Concat(new[] { CardInstanceState.Empty(ghostId + 1, "calibration-variable") }).ToArray(),
                        original.AllScenarioBossesDead, original.NextAddedTemporaryUpgrades, piles);
                    var frame = new StatisticQueryFrame(managers.GetPlayerManager().GetEnergy(), !empty && managers.GetCombatManager()!.GetIsRunningCombat(),
                        empty ? stoppedCombat.GetTurnCount() : managers.GetCombatManager()!.GetTurnCount(), save.GetForgePoints(),
                        save.GetDragonsHoardAmount(), (int)managers.GetPlayerManager().CurrentMoonPhase, 0, save.GetGameSequence() == SaveData.GameSequence.InBattle);
                    var samples = new List<object>();
                    BattleStatistics? expectedStatistics = null;
                    string? expectedStatisticsJson = null;
                    foreach (string type in supportedTypes)
                    foreach (CardStatistics.EntryDuration duration in Enum.GetValues(typeof(CardStatistics.EntryDuration)))
                    foreach (CardStatistics.CardTypeTarget targetType in Enum.GetValues(typeof(CardStatistics.CardTypeTarget)))
                    foreach (SubtypeData? subtype in new SubtypeData?[] { null }.Concat(subtypes))
                    foreach (CardState? sourceCard in new CardState?[] { sources[0], sources[1], ghost, null }.Concat(
                        type == "PlayedCost" || type == "UnmodifiedPlayedCost" ? new[] { variable } : Array.Empty<CardState>()))
                    {
                        if (type == "MonsterSubtypePlayed" && subtype == null || (type == "PlayedCost" || type == "UnmodifiedPlayedCost") &&
                            (sourceCard == null || sourceCard == ghost)) continue;
                        // A single deliberate mismatched local type verifies that native logging does not reject the query.
                        bool local = new[] { "HeroesKilled", "SpawnedMonsterDeaths", "TimesDiscarded", "TimesPlayed", "TimesDrawn", "TimesExhausted",
                            "LastSacrificedMonsterStats", "AnyStatusEffectStacksAdded", "AnyStatusEffectStacksRemoved" }.Contains(type) ||
                            (type == "AnyMonsterSpawned" || type == "AnyMonsterSpawnedTopFloor") && duration == CardStatistics.EntryDuration.PreviousTurn;
                        bool filtered = type.StartsWith("Type", StringComparison.Ordinal) || type.StartsWith("Subtype", StringComparison.Ordinal) ||
                            new[] { "AnyHeroKilled", "AnyMonsterDeath", "AnyDiscarded", "AnyCardPlayed", "AnyCardDrawn", "AnyExhausted" }.Contains(type);
                        if (!filtered && targetType != CardStatistics.CardTypeTarget.Any && !(local && type == "TimesPlayed" && duration == CardStatistics.EntryDuration.ThisTurn && subtype == null)) continue;
                        if ((type.Contains("InDeck") || type.Contains("InDrawPile") || type.Contains("InDiscardPile") || type.Contains("InExhaustPile") || type.Contains("InEatenPile") ||
                            !local && !filtered && type != "AnyMonsterSpawned" && type != "AnyMonsterSpawnedTopFloor" && type != "MonsterSubtypePlayed" && type != "NumSpecificCardsInDeck") && duration != CardStatistics.EntryDuration.ThisTurn) continue;
                        if (local && targetType != CardStatistics.CardTypeTarget.Any && sourceCard != null && sourceCard != ghost &&
                            sourceCard.GetCardType().ToString() != targetType.ToString() && (type != "TimesPlayed" || duration != CardStatistics.EntryDuration.ThisTurn || subtype != null)) continue;
                        string[]? typeMask = targetType == CardStatistics.CardTypeTarget.Any ? null : definitions.Where(data => data.GetCardType().ToString() == targetType.ToString())
                            .Select(data => data.GetID()).ToArray();
                        string[] subtypeMask = subtype == null ? Array.Empty<string>() : definitions.Where(data => data.GetSpawnCharacterData()?.GetSubtypes().Contains(subtype) == true)
                            .Select(data => data.GetID()).ToArray();
                        CardData? specific = type == "NumSpecificCardsInDeck" && subtype == null ? definitions.First(data => data.GetID() == known[0].GetCardDataID()) : null;
                        int? rawCost = sourceCard == null || sourceCard == ghost ? (int?)null : (int)AccessTools.Field(typeof(CardState), "cost").GetValue(sourceCard);
                        var query = new CardStatisticQuery(type, duration.ToString(), typeMask, subtypeMask, subtype == null || subtype.IsNone,
                            subtype?.Key, specific?.GetID(), rawCost, sourceCard != null && sourceCard != ghost && sourceCard.IsConsumeRemainingEnergyCostType());
                        var input = new CardStatistics.StatValueData { cardState = sourceCard!, trackedValue = (CardStatistics.TrackedValueType)Enum.Parse(typeof(CardStatistics.TrackedValueType), type),
                            entryDuration = duration, cardTypeTarget = targetType, paramSubtype = subtype!, paramCardData = specific!, forPreviewText = false };
                        int actual = native.GetStatValue(input);
                        StatisticQueryResult predicted = StatisticQueryModel.Evaluate(before, query, sourceCard == null ? 0 : Id(sourceCard), frame);
                        if (!predicted.Supported || predicted.Value != actual) throw new InvalidOperationException("Native statistic query differs: " + type + "/" + duration + "/" + targetType +
                            "/" + subtype?.Key + "/" + (sourceCard == null ? 0 : Id(sourceCard)) + " expected=" + actual + " predicted=" + predicted.Value + " " + predicted.UnsupportedReason);
                        if (expectedStatistics == null) { expectedStatistics = Snapshot(); expectedStatisticsJson = JsonConvert.SerializeObject(expectedStatistics); }
                        if (JsonConvert.SerializeObject(predicted.Context!.Statistics!) != expectedStatisticsJson)
                            throw new InvalidOperationException("Native statistic query membership refresh differs.");
                        samples.Add(new { Query = query, SourceCardId = sourceCard == null ? 0 : Id(sourceCard), Actual = actual });
                    }
                    if (!JToken.DeepEquals(JToken.FromObject(Snapshot()), JToken.FromObject(expectedStatistics!)))
                        throw new InvalidOperationException("Native query batch changed nonzero counters after refresh.");
                    batches.Add(new { Layout = layout, EmptyPiles = empty, WithPaidCost = withPaidCost, Before = before, Frame = frame, AfterStatistics = expectedStatistics, Samples = samples });
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(host); }
            if (!JToken.DeepEquals(JToken.FromObject(original), JToken.FromObject(trace.CaptureContext())))
                throw new InvalidOperationException("Statistic calibration changed live battle state.");
            File.WriteAllText(Path.Combine(Environment.GetEnvironmentVariable("MT2_PROBE_DATA_DIR")!, "statistic-query-calibration.json"),
                JsonConvert.SerializeObject(new { Schema = 1, GameModuleMvid = typeof(CardState).Assembly.ManifestModule.ModuleVersionId,
                    SyntheticCounters = true, LiveContextUnchanged = true, Batches = batches }, Formatting.Indented));
        }
    }
}

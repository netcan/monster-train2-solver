using MonsterTrain2Poju.Fixtures;
using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class StatisticQueryChecks
{
    internal static void Run()
    {
        UnityRng rng = UnityRng.Seed(127);
        CardInstanceState[] registry = Enumerable.Range(1, 10).Select(id => id == 1 ?
            new CardInstanceState(id, "spell", new(new(cost: -1, xCost: 4), [], 0, []), new(new(cost: 2, xCost: -1), [], 0, []), 0, 0, 0, []) :
            CardInstanceState.Empty(id, id == 8 ? "spell" : "monster")).ToArray();
        var statistics = new BattleStatistics([new(1, "ThisTurn", "TimesPlayed", 3), new(4, "ThisTurn", "TimesPlayed", 7),
            new(9, "ThisTurn", "TimesPlayed", 60), new(4, "ThisTurn", "AnyCardPlayed", 999),
            new(4, "PreviousTurn", "AnyMonsterSpawned", 12), new(4, "PreviousTurn", "AnyMonsterSpawnedTopFloor", 17),
            new(5, "PreviousTurn", "SpawnedMonsterDeaths", 4), new(4, "ThisTurn", "AnyStatusEffectStacksAdded", 8)],
            [new(1, -2)], [], [new("0", 2), new("2", 9)], [new("1", 5)], [new("dragon", 3)], [new("dragon", 11)],
            23, 47, 6, 19, 31, [1, 4, 5, 9], [1, 4, 10]);
        CardPileState[] piles = [new("Standby", [new(4, "monster")], [4], []), new("Exhausted", [new(5, "monster")]),
            new("Eaten", [new(6, "monster")]), new("Purged", [new(7, "monster")]), new("DiscardBuffer", [new(1, "spell"), new(8, "spell")])];
        var context = new CombatContext(new([new(1, "spell")], [new(2, "monster")], [new(3, "monster")], rng, 0, []),
            rng, 85, 11, 10, statistics: statistics, cardInstances: registry.Take(8).ToArray(), cardRegistry: registry, otherPiles: piles,
            roomCapacities: [new(0, 7, 11, false, 1, [], 4, 0),
                new(1, 15, 4, false, 3, [], 5, 0), new(2, 30, 3, true, 0, [], 4, 0)]);
        string parent = JsonSerializer.Serialize(context);
        StatisticQueryResult Query(CardStatisticQuery query, int source = 4, StatisticQueryFrame? frame = null)
        {
            StatisticQueryResult result = StatisticQueryModel.Evaluate(context, query, source, frame);
            Require(result.Supported, "Statistic query rejected: " + result.UnsupportedReason);
            return result;
        }
        var any = Query(new("AnyCardPlayed"));
        Require(any.Value == 10 && !any.Context!.Statistics!.TrackedCards.Contains(9) &&
            any.Context.Statistics.Values.All(value => value.CardId != 9), "Global totals used scaling-notification counters or retained a stale member.");
        Require(Query(new("TypeInDeck", typeMatchDataIds: ["monster"])).Value == 3 &&
            Query(new("SubtypeInDeck", typeMatchDataIds: ["spell"], subtypeMatchDataIds: ["monster"])).Value == 6,
            "Type/subtype deck counts lost their distinct exclusions or subtype type-filter bypass.");
        Require(Query(new("TypeInDeck", typeMatchDataIds: ["spell"])).Value == 2 &&
            Query(new("TypeInExhaustPile", typeMatchDataIds: ["monster"])).Value == 1 &&
            Query(new("SubtypeInEatenPile", subtypeMatchDataIds: ["monster"])).Value == 1,
            "Buffer aliases, buffer-only ownership or secondary pile membership were counted incorrectly.");
        Require(Query(new("AnyMonsterSpawned", "PreviousTurn")).Value == 12 &&
            Query(new("AnyMonsterSpawnedTopFloor", "PreviousTurn")).Value == 17 &&
            Query(new("AnyMonsterDeath", "PreviousTurn", typeMatchDataIds: ["monster"])).Value == 4 &&
            Query(new("AnyMonsterDeath", typeMatchDataIds: [])).Value == 23,
            "Previous-turn fallthrough or current death totals were replaced with uniform global rules.");
        Require(Query(new("AnyMonsterSpawned")).Value == 11 && Query(new("AnyMonsterSpawnedTopFloor")).Value == 9 &&
            Query(new("MonsterSubtypePlayed", subtype: "dragon")).Value == 3 &&
            Query(new("MonsterSubtypePlayed", "PreviousTurn", subtype: "dragon")).Value == 0,
            "Spawn counters selected the wrong scope, floor or subtype.");
        Require(Query(new("TimesPlayed", typeMatchDataIds: [])).Value == 7 &&
            Query(new("AnyStatusEffectStacksAdded")).Value == 8 &&
            Query(new("AnyCardPlayed", subtypeMatchDataIds: [], subtypeIsNone: false)).Value == 0,
            "Local counters incorrectly applied type filters, became global totals or ignored subtype filtering.");
        Require(Query(new("UnmodifiedPlayedCost"), 1).Value == -2 && Query(new("PlayedCost", sourceRawCost: 2), 1).Value == 3 &&
            Query(new("PlayedCost", sourceRawCost: 2, variableCost: true), 8, new(energy: 4)).Value == 6 &&
            Query(new("PlayedCost", sourceRawCost: 2, variableCost: true), 8, new(energy: 4, activeBattle: false)).Value == 0,
            "Paid-cost signed values, intrinsic base cost, X-cost modifiers or energy fallback differ.");
        Require(Query(new("CurrentCost", sourceRawCost: 2, currentCostCardModifiersOnly: true), 1).Value == 3 &&
            !StatisticQueryModel.Evaluate(context, new("CurrentCost", sourceRawCost: 2), 1).Supported,
            "Current cost ignored ordered card modifiers or accepted unmodeled cost inputs.");
        Require(Query(new("Gold"), frame: new(runningCombat: true)).Value == 19 &&
            Query(new("Gold"), frame: new(runningCombat: false)).Value == 85,
            "Gold ignored the combat loop or the turn-start snapshot.");
        Require(Query(new("NumSpecificCardsInDeck", specificCardDataId: "monster")).Value == 2,
            "Specific-card counting used owned cards instead of the permanent deck.");
        var capacityFrame = new StatisticQueryFrame(selectedRoomIndex: 1);
        Require(Query(new("CapacityOfTargetRoom", targetTeam: "None"), frame: capacityFrame).Value == 15 &&
            Query(new("CapacityOfTargetRoom", targetTeam: "Monsters"), frame: capacityFrame).Value == 15 &&
            Query(new("CapacityOfTargetRoom", targetTeam: "Heroes, Monsters"), frame: capacityFrame).Value == 15 &&
            Query(new("CapacityOfTargetRoom", targetTeam: "Heroes"), frame: capacityFrame).Value == 4 &&
            Query(new("CapacityOfTargetRoom", targetTeam: "Heroes"), frame: capacityFrame.With(selectedRoomIndex: 0)).Value == 11,
            "Room capacity queries lost the selected room or native exact-Heroes team routing.");
        Require(!StatisticQueryModel.Evaluate(context, new("CapacityOfTargetRoom")).Supported,
            "Capacity query without a selected room produced a usable child state.");
        Require(Query(new("CorruptionInTargetRoom"), frame: capacityFrame).Value == 3 &&
            Query(new("CorruptionInTargetRoom"), frame: capacityFrame.With(selectedRoomIndex: 0)).Value == 1 &&
            !StatisticQueryModel.Evaluate(context, new("CorruptionInTargetRoom")).Supported,
            "Corruption query ignored the selected room or accepted an absent room.");
        var empty = new CombatContext(new([], [], [], rng, 0, []), rng, 85, 11, 10, statistics: statistics,
            cardInstances: [], cardRegistry: registry, otherPiles: piles.Select(CardPileModel.Clear).ToArray());
        StatisticQueryResult fallback = StatisticQueryModel.Evaluate(empty, new("TypeInDeck", typeMatchDataIds: ["monster"]));
        Require(fallback.Supported && fallback.Value == 2 && fallback.Context!.Statistics!.TrackedCards.SequenceEqual([1, 4, 10]),
            "Empty-pile queries failed to refresh from the permanent deck.");
        Require(!StatisticQueryModel.Evaluate(context, new("Gold")).Supported &&
            !StatisticQueryModel.Evaluate(context, new("CurrentCost"), 1).Supported &&
            !StatisticQueryModel.Evaluate(context, new("PlayedCost", variableCost: true), 8).Supported &&
            !StatisticQueryModel.Evaluate(context, new("TimesPlayed", "NextTurn"), 4).Supported,
            "Uncaptured dynamic inputs or unsupported queries produced usable child states.");
        Parallel.For(0, 64, _ => Require(JsonSerializer.Serialize(StatisticQueryModel.Evaluate(context, new("AnyCardPlayed"))) ==
            JsonSerializer.Serialize(any), "Parallel statistic query results diverged."));
        Require(JsonSerializer.Serialize(context) == parent, "Statistic query mutated its parent.");
        Console.WriteLine("STATISTIC-QUERY-CHECKS PASS: ownership refresh, pile/type/subtype scopes, previous-turn fallthrough, paid/variable costs, gold timing, permanent deck fallback and 64 parallel branches.");
    }

    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue fixture = document.RootElement;
        Require(fixture.GetProperty("LiveContextUnchanged").GetBoolean(), "Native statistic calibration changed live battle state.");
        int samples = 0;
        var kinds = new HashSet<string>();
        foreach (FixtureValue batch in fixture.GetProperty("Batches").EnumerateArray())
        {
            CombatContext before = batch.GetProperty("Before").Deserialize<CombatContext>()!;
            StatisticQueryFrame frame = batch.GetProperty("Frame").Deserialize<StatisticQueryFrame>()!;
            BattleStatistics actualStatistics = batch.GetProperty("AfterStatistics").Deserialize<BattleStatistics>()!;
            string parent = JsonSerializer.Serialize(before);
            foreach (FixtureValue sample in batch.GetProperty("Samples").EnumerateArray())
            {
                CardStatisticQuery query = sample.GetProperty("Query").Deserialize<CardStatisticQuery>()!;
                var result = StatisticQueryModel.Evaluate(before, query, sample.GetProperty("SourceCardId").GetInt32(), frame);
                Require(result.Supported && result.Value == sample.GetProperty("Actual").GetInt32() &&
                    JsonSerializer.Serialize(result.Context!.Statistics) == JsonSerializer.Serialize(actualStatistics),
                    "Native statistic query differs: " + query.Type + "/" + query.Duration + " " + result.UnsupportedReason);
                samples++; kinds.Add(query.Type);
            }
            Require(JsonSerializer.Serialize(before) == parent, "Native query checks mutated their captured root.");
        }
        Require(samples > 500 && kinds.Count == 42, "Native statistic calibration lacks supported query kinds.");
        var capacityQueries = fixture.GetProperty("Batches").EnumerateArray().SelectMany(batch => batch.GetProperty("Samples").EnumerateArray())
            .Where(sample => sample.GetProperty("Query").GetProperty("Type").GetString() == "CapacityOfTargetRoom").ToArray();
        Require(capacityQueries.Length >= 20 && capacityQueries.Select(sample => sample.GetProperty("Query").GetProperty("TargetTeam").GetString()).Distinct().Count() == 4,
            "Native capacity query calibration lacks selected-room and team-flag cases.");
        var currentCostQueries = fixture.GetProperty("Batches").EnumerateArray().SelectMany(batch => batch.GetProperty("Samples").EnumerateArray())
            .Where(sample => sample.GetProperty("Query").GetProperty("Type").GetString() == "CurrentCost").ToArray();
        Require(currentCostQueries.Length >= 20 && currentCostQueries.All(sample =>
            sample.GetProperty("Query").GetProperty("CurrentCostCardModifiersOnly").GetBoolean()),
            "Native current-cost calibration lacks card-modifier-only cases.");
        Console.WriteLine($"NATIVE-STATISTIC-QUERY-CHECKS PASS: {samples} native queries, {kinds.Count} kinds, selected-room capacity and current cost, three durations, masked/null/detached sources, cost fallback and refreshed ownership.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}

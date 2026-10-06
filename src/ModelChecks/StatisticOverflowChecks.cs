using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class StatisticOverflowChecks
{
    internal static void Run()
    {
        var root = new BattleStatistics([new(1, "ThisTurn", "TimesDrawn", int.MaxValue), new(1, "ThisBattle", "TimesDrawn", int.MaxValue),
            new(1, "ThisTurn", "AnyCardDrawn", int.MaxValue - 1), new(1, "ThisBattle", "AnyCardDrawn", int.MaxValue - 1),
            new(2, "ThisTurn", "AnyCardDrawn", int.MaxValue), new(2, "ThisBattle", "AnyCardDrawn", int.MaxValue)], [], [],
            [new("0", int.MaxValue)], [new("0", int.MaxValue)], [new("dragon", int.MaxValue)], [new("dragon", int.MaxValue)],
            int.MaxValue, int.MaxValue, 0, 0, 0, [1, 2]);
        string parent = JsonSerializer.Serialize(root);
        BattleStatistics result = root.Increment(1, "TimesDrawn", 2).Spawn(0, ["dragon"]).Death(true, 1);
        Require(result.Value(1, "TimesDrawn") == int.MinValue + 1 && result.Value(1, "AnyCardDrawn") == int.MinValue &&
            result.Value(2, "AnyCardDrawn") == int.MinValue && result.MonstersDeadThisTurn == int.MinValue &&
            result.SpawnedThisTurnPerFloor.Single().Value == int.MinValue && result.SubtypesSpawnedThisBattle.Single().Value == int.MinValue,
            "Per-source, global event, floor/subtype or death counters failed to preserve signed native wrapping.");
        BattleStatistics next = result.NextTurn(4);
        Require(next.Value(1, "TimesDrawn", "PreviousTurn") == int.MinValue + 1 && next.Value(1, "TimesDrawn", "ThisBattle") == int.MinValue + 1 &&
            next.Value(1, "TimesDrawn") == 0 && next.SpawnedThisBattlePerFloor.Single().Value == int.MinValue,
            "Turn rollover dropped negative counters or retained turn counters.");
        var rng = UnityRng.Seed(161);
        var owner = new CardInstanceState(1, "spell", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
            statusScalingTraits: [new(new("AnyStatusEffectStacksAdded"), 1, false, 0, ["armor"])]);
        var statistics = new BattleStatistics([new(1, "ThisTurn", "AnyStatusEffectStacksAdded", int.MaxValue - 1),
            new(1, "ThisBattle", "AnyStatusEffectStacksAdded", int.MaxValue - 1)], [], [], [], [], [], [], 0, 0, 0, 0, 0, [1], [1]);
        var context = new CombatContext(new([new(1, "spell")], [], [], rng, 0, []), rng, 0, 2, 10,
            statistics: statistics, cardInstances: [owner], otherPiles: [new("Exhausted", [])]);
        var room = new RoomCombatState(0, false, [new(10, "front", CombatTeam.Enemy, 1, 10, 10, true, false, false, []),
            new(11, "back", CombatTeam.Enemy, 1, 10, 10, true, false, false, [])], [], context);
        var effects = new CardActionEffect[] { new("AddStatus", "Room", 0, true, false, [new("armor", 0, stackable: true)]) };
        RoomCombatResult applied = CardSpellModel.Apply(room, effects, 0, 1);
        Require(applied.Supported && applied.State!.Units.Single(unit => unit.Id == 11).Statuses.Single().Stacks == 9999 &&
            applied.State.Units.Single(unit => unit.Id == 10).Statuses.Count == 0 &&
            applied.State.Context!.Statistics!.Value(1, "AnyStatusEffectStacksAdded") == int.MinValue + 9997,
            "A later target failed to read the wrapped source count, or the transition threw/returned partial state.");
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(CardSpellModel.Apply(room, effects, 0, 1)) ==
            JsonSerializer.Serialize(applied) && JsonSerializer.Serialize(root.Increment(1, "TimesDrawn", 2).Spawn(0, ["dragon"]).Death(true, 1)) ==
            JsonSerializer.Serialize(result), "Parallel boundary branches differed."));
        Require(JsonSerializer.Serialize(root) == parent && statistics.Value(1, "AnyStatusEffectStacksAdded") == int.MaxValue - 1,
            "Boundary branches mutated their parent counters.");
        Console.WriteLine("STATISTIC-OVERFLOW-CHECKS PASS: source/global counters, spawn/subtypes/deaths, duration rollover, signed status feedback and 32 parallel branches.");
    }
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); JsonElement fixture = document.RootElement;
        Require(fixture.GetProperty("LiveContextUnchanged").GetBoolean(), "Overflow calibration changed live state.");
        int increments = 0, spawns = 0, wraps = 0;
        foreach (JsonElement sample in fixture.GetProperty("Samples").EnumerateArray())
        {
            BattleStatistics before = sample.GetProperty("Before").Deserialize<BattleStatistics>()!;
            BattleStatistics after = sample.GetProperty("After").Deserialize<BattleStatistics>()!;
            string parent = JsonSerializer.Serialize(before);
            BattleStatistics predicted;
            if (sample.GetProperty("Kind").GetString() == "Increment")
            {
                predicted = before.Increment(sample.GetProperty("SourceCardId").GetInt32(), sample.GetProperty("Type").GetString()!,
                    sample.GetProperty("Amount").GetInt32()); increments++;
            }
            else
            {
                predicted = before.Spawn(sample.GetProperty("RoomIndex").GetInt32(), sample.GetProperty("Subtypes").Deserialize<string[]>()!); spawns++;
            }
            Require(JsonSerializer.Serialize(predicted) == JsonSerializer.Serialize(after) && JsonSerializer.Serialize(before) == parent,
                "Independent native overflow transition differs or mutates its parent.");
            wraps += before.Values.Any(value => value.Value > 0 && after.Value(value.CardId, value.Type, value.Duration) < 0) ||
                before.SpawnedThisTurnPerFloor.Any(value => value.Value > 0 && after.SpawnedThisTurnPerFloor.Any(next => next.Key == value.Key && next.Value < 0)) ? 1 : 0;
        }
        Require(increments == 60 && spawns == 5 && wraps > 0, "Overflow oracle lacks source/event/spawn boundaries.");
        Console.WriteLine($"NATIVE-STATISTIC-OVERFLOW-CHECKS PASS: {increments} source/global transitions, {spawns} floor/subtype transitions, {wraps} positive-to-negative wraps; complete immutable statistics.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

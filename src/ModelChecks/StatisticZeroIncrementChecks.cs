using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class StatisticZeroIncrementChecks
{
    private static readonly Dictionary<string, string> Events = new() { ["HeroesKilled"] = "AnyHeroKilled",
        ["SpawnedMonsterDeaths"] = "AnyMonsterDeath", ["TimesDiscarded"] = "AnyDiscarded",
        ["TimesPlayed"] = "AnyCardPlayed", ["TimesDrawn"] = "AnyCardDrawn", ["TimesExhausted"] = "AnyExhausted" };

    internal static void Run()
    {
        foreach (string type in Events.Keys.Concat(["AnyStatusEffectStacksAdded", "AnyStatusEffectStacksRemoved"]))
        foreach (int seed in new[] { 0, 7, int.MaxValue - 1 })
        foreach (int source in new[] { 1, 3 })
        {
            var values = new List<CardStatisticValue>();
            foreach (int id in new[] { 1, 2 })
            {
                foreach (string duration in new[] { "ThisTurn", "ThisBattle" })
                {
                    values.Add(new(id, duration, type, seed));
                    if (Events.TryGetValue(type, out string? any)) values.Add(new(id, duration, any, seed));
                }
                values.Add(new(id, "PreviousTurn", type, 9));
            }
            var root = new BattleStatistics(values, [], [], [], [], [], [], 0, 0, 0, 0, 0, [1, 2], [1, 2]);
            string parent = JsonSerializer.Serialize(root);
            BattleStatistics child = root.Increment(source, type, 0, requireTrackedCard: true);
            foreach (int id in new[] { 1, 2 })
            {
                Require(child.Value(id, type) == seed && child.Value(id, type, "ThisBattle") == seed &&
                    child.Value(id, type, "PreviousTurn") == 9, "Zero amount changed local/duration counters.");
                if (Events.TryGetValue(type, out string? any))
                {
                    int expected = unchecked(seed + (id == source ? 2 : 1));
                    Require(child.Value(id, any) == expected && child.Value(id, any, "ThisBattle") == expected,
                        "Zero amount dropped the native event or its double source count.");
                }
            }
            Require(child.TrackedCards.SequenceEqual([1, 2]) && child.DeckCards!.SequenceEqual([1, 2]) &&
                child.Value(3, type) == 0 && child.CardsPlayedThisTurn.SequenceEqual(type == "TimesPlayed" ? [source] : Array.Empty<int>()),
                "Detached zero event leaked into ownership or lost its play history.");
            string expectedChild = JsonSerializer.Serialize(child);
            Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(root.Increment(source, type, 0, requireTrackedCard: true)) == expectedChild,
                "Parallel zero events differed."));
            Require(JsonSerializer.Serialize(root) == parent, "Zero event mutated its parent.");
        }
        Console.WriteLine("STATISTIC-ZERO-INCREMENT-CHECKS PASS: six event counters, two stack counters, zero/seeded/wrapped totals, detached ownership, play history, previous duration and 32 parallel branches.");
    }

    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        JsonElement fixture = document.RootElement;
        Require(fixture.GetProperty("LiveContextUnchanged").GetBoolean(), "Native zero calibration changed live context.");
        int samples = 0, detached = 0, events = 0, wraps = 0;
        var types = new HashSet<string>();
        foreach (JsonElement sample in fixture.GetProperty("Samples").EnumerateArray())
        {
            string type = sample.GetProperty("Type").GetString()!;
            int id = sample.GetProperty("SourceCardId").GetInt32();
            Require(id > 0 && sample.GetProperty("Amount").GetInt32() == 0, "Zero oracle lacks a real source identity.");
            BattleStatistics before = sample.GetProperty("Before").Deserialize<BattleStatistics>()!;
            BattleStatistics after = sample.GetProperty("After").Deserialize<BattleStatistics>()!;
            string parent = JsonSerializer.Serialize(before);
            Require(JsonSerializer.Serialize(before.Increment(id, type, 0, requireTrackedCard: true)) == JsonSerializer.Serialize(after) &&
                JsonSerializer.Serialize(before) == parent, "Independent native zero event differs or changes its parent.");
            types.Add(type); samples++;
            detached += sample.GetProperty("DetachedSource").GetBoolean() ? 1 : 0;
            events += Events.ContainsKey(type) ? 1 : 0;
            wraps += before.Values.Any(value => value.Value > 0 && after.Value(value.CardId, value.Type, value.Duration) < 0) ? 1 : 0;
        }
        Require(samples == 48 && detached == 24 && events == 36 && types.Count == 8 && wraps > 0,
            "Native zero oracle lacks attached/detached, event/stack and signed-boundary coverage.");
        Console.WriteLine($"NATIVE-STATISTIC-ZERO-INCREMENT-CHECKS PASS: {samples} exact zero events, {detached} detached sources, {events} global event transitions and {wraps} signed wraps; complete statistics.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

using MonsterTrain2Poju.Fixtures;
using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class StatisticCacheChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(86);
        var stats = new BattleStatistics([], [], [], [], [], [], [], 0, 0, 0, 0, 0, [1], [1], [1]);
        var instance = CardInstanceState.Empty(1, "owned");
        var root = new CombatContext(new([new(1, "owned")], [], [], rng, 0, []), rng, 0, 2, 10,
            statistics: stats, cardInstances: [instance], cardRegistry: [instance], otherPiles: [], queryFrame: new(energy: 0, runningCombat: true, turn: 1));
        var generation = new CardGenerationRule("DeckPileTop", 1, [new("new", CardModifiers.Empty(), null, [])]);
        var born = CardGenerationModel.Apply(root, generation);
        Require(born.Supported && born.Context!.Statistics!.TrackedCards.SequenceEqual([1, 2]) &&
            born.Context.Statistics.StoredCards!.SequenceEqual([1]), "Generation eagerly created a native statistic cache entry.");
        string parent = JsonSerializer.Serialize(born.Context);
        CombatContext Clear(CombatContext context) => new(new([], [], [], context.Cards.Rng, 0, []), context.BattleRng,
            context.Gold, context.NextCardId, context.MaxHandSize, statistics: context.Statistics, cardInstances: [],
            cardRegistry: context.CardRegistry, otherPiles: [], queryFrame: context.QueryFrame);
        var cleared = Clear(born.Context!);
        Require(cleared.Statistics!.TrackedCards.SequenceEqual([1]) && cleared.Statistics.StoredCards!.SequenceEqual([1]) &&
            cleared.CardRegistry!.Count == 2, "Terminal ownership removal lost stored entries or retained an uncached generated card.");
        var incremented = born.Context!.Statistics!.Increment(2, "TimesDrawn");
        Require(incremented.StoredCards!.SequenceEqual([1, 2]) && incremented.Value(2, "TimesDrawn") == 1 &&
            incremented.Value(1, "AnyCardDrawn") == 1 && incremented.Value(2, "AnyCardDrawn") == 2,
            "Stat increment did not materialize owned cache entries before its source/global counters.");
        var cached = new CombatContext(born.Context.Cards, rng, 0, 3, 10, statistics: incremented,
            cardInstances: born.Context.CardInstances, cardRegistry: born.Context.CardRegistry, otherPiles: [], queryFrame: born.Context.QueryFrame);
        Require(Clear(cached).Statistics!.TrackedCards.SequenceEqual([1, 2]), "Ownership removal erased an existing native cache entry.");
        var refreshed = StatisticQueryModel.Evaluate(born.Context, new("Gold"));
        Require(refreshed.Supported && refreshed.Context!.Statistics!.StoredCards!.SequenceEqual([1, 2]) &&
            Clear(refreshed.Context).Statistics!.TrackedCards.SequenceEqual([1, 2]), "Statistic query failed to materialize actual owned entries.");
        var fallback = StatisticQueryModel.Evaluate(Clear(cached), new("Gold"));
        Require(fallback.Supported && fallback.Context!.Statistics!.StoredCards!.SequenceEqual([1]) &&
            fallback.Context.Statistics.Values.All(value => value.CardId == 1), "Empty-pile query did not refresh from the permanent deck.");
        Require(born.Context.Statistics.NextTurn(3).StoredCards!.SequenceEqual([1, 2]) &&
            born.Context.Statistics.WithLastAttackDamage(7).StoredCards!.SequenceEqual([1]) &&
            born.Context.Statistics.WithEndTurnEnergy(2).StoredCards!.SequenceEqual([1]),
            "Turn rollover or passive resource setters used the wrong cache refresh boundary.");
        var detached = born.Context.Statistics.Increment(99, "TimesDrawn", 0);
        Require(!detached.StoredCards!.Contains(99) && detached.Value(99, "TimesDrawn") == 0 &&
            detached.Value(1, "AnyCardDrawn") == 1 && detached.Value(2, "AnyCardDrawn") == 1,
            "Detached source was inserted into a captured native cache or lost the global zero event.");
        Require(born.Context.Statistics.Increment(0, "TimesDrawn").StoredCards!.SequenceEqual([1]),
            "Absent character attribution unexpectedly ran a native stat callback.");
        var legacy = new BattleStatistics([], [], [], [], [], [], [], 0, 0, 0, 0, 0, [1], [1]);
        Require(legacy.TrackCards([2]).StoredCards == null && legacy.TrackCards([2]).Increment(2, "TimesDrawn").StoredCards == null,
            "Older captures acquired guessed native cache metadata.");
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(Clear(CardGenerationModel.Apply(root, generation).Context!)) ==
            JsonSerializer.Serialize(cleared) && JsonSerializer.Serialize(StatisticQueryModel.Evaluate(born.Context, new("Gold"))) ==
            JsonSerializer.Serialize(refreshed), "Parallel cache and ownership branches diverged."));
        Require(JsonSerializer.Serialize(born.Context) == parent, "Cache refresh mutated its parent.");
        Console.WriteLine("STATISTIC-CACHE-CHECKS PASS: generated ownership/cached entry distinction, terminal clears, counter/query/turn refresh, passive setters, detached/zero sources, legacy captures and 32 immutable parallel branches.");
    }

    internal static void Native(FixtureValue fixture)
    {
        if (fixture.GetProperty("Schema").GetInt32() < 33 || fixture.GetProperty("ModifierScenario").GetString() != "pre-combat-lethal") return;
        int generations = 0, uncachedBirths = 0, terminalDrops = 0, cachedRetained = 0;
        foreach (FixtureValue record in fixture.GetProperty("CardGenerations").EnumerateArray())
        {
            var before = record.GetProperty("Before").Deserialize<CombatContext>()!;
            var actual = record.GetProperty("Actual").Deserialize<CombatContext>()!;
            var rule = record.GetProperty("Rule").Deserialize<CardGenerationRule>()!;
            var predicted = CardGenerationModel.Apply(before, rule, record.GetProperty("SourceCardId").GetInt32());
            Require(predicted.Supported && JsonSerializer.Serialize(predicted.Context) == JsonSerializer.Serialize(actual),
                "Independent native card generation differs with separate statistic-cache membership.");
            Require(before.Statistics!.StoredCards != null && actual.Statistics!.StoredCards != null,
                "Schema 33 omitted native statistic cache keys.");
            generations++;
            uncachedBirths += predicted.AddedCards.Count(card => !actual.Statistics!.StoredCards!.Contains(card.InstanceId));
        }
        foreach (FixtureValue stage in fixture.GetProperty("Stages").EnumerateArray())
        {
            var before = stage.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var actual = stage.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            if (before.Context!.CardInstances!.Count == 0 || actual.Context!.CardInstances!.Count != 0) continue;
            var result = stage.GetProperty("Kind").GetString() == "Exchange" ? RoomCombatModel.Exchange(before) : RoomCombatModel.Resolve(before);
            Require(result.Supported && ModelJson.Difference(JsonSerializer.Serialize(result.State), JsonSerializer.Serialize(actual)) == null,
                "Independent terminal room differs with native cache metadata.");
            int[] lost = before.Context.Statistics!.TrackedCards.Except(actual.Context.Statistics!.TrackedCards).ToArray();
            int[] bornUncached = before.Context.CardInstances.Where(card => !before.Context.Statistics.StoredCards!.Contains(card.InstanceId))
                .Select(card => card.InstanceId).ToArray();
            if (lost.Intersect(bornUncached).Any()) terminalDrops++;
            cachedRetained += actual.Context.Statistics.StoredCards!.Except(actual.Context.Statistics.DeckCards!).Count();
        }
        Require(generations > 0 && uncachedBirths > 0 && terminalDrops > 0 && cachedRetained > 0,
            "Native cache regression lacks uncached creation, terminal ownership loss or retained generated cache entries.");
        Console.WriteLine($"NATIVE-STATISTIC-CACHE-CHECKS PASS: {generations} complete generations, {uncachedBirths} uncached births, {terminalDrops} terminal removal boundaries and {cachedRetained} retained cached generated entries; complete contexts.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

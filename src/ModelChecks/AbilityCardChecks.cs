using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class AbilityCardChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(76);
        var owned = CardInstanceState.Empty(1, "skill");
        var stats = new BattleStatistics([], [], [], [], [], [], [], 0, 0, 0, 0, 0, [1], [1], [1]);
        var pending = new CardUpgradeModifier("pending", "pending", new(damage: 4), [], false, false, false, 0, 0, []);
        var upgrade = new CardUpgradeModifier("start", "start", new(damage: 2), [], false, true, false, 0, 0, []);
        var creation = new CardCreationRule("skill", new(new(), [upgrade], 0, []), [new(0, "CardEffectDiscardHand", 0)], []);
        var root = new CombatContext(new([new(1, "skill")], [], [], rng, 0, []), rng, 19, 2, 10, statistics: stats,
            cardInstances: [owned], cardRegistry: [owned], nextAddedTemporaryUpgrades: [pending], otherPiles: [], abilityCardCache: []);
        string parent = JsonSerializer.Serialize(root, ModelJson.Options);
        AbilityCardResult first = AbilityCardModel.Get(root, creation);
        Require(first.Supported && first.Created && first.Card!.InstanceId == 2 && first.Context!.NextCardId == 3,
            "An owned card with the same definition must not satisfy the ability cache.");
        Require(first.Context!.CardInstances!.Count == 1 && first.Context.CardRegistry!.Count == 2 &&
            first.Context.Statistics!.TrackedCards.SequenceEqual([1]) && first.Context.Statistics.StoredCards!.SequenceEqual([1]) &&
            first.Context.NextAddedTemporaryUpgrades!.Count == 1 && first.Card!.Permanent.Upgrades.Count == 1 &&
            first.Card.Temporary.Upgrades.Count == 0 && first.Card.EffectCounters![0].Value == 0,
            "Cache creation changed ownership/statistics, consumed pending upgrades or lost starting state.");
        AbilityCardResult second = AbilityCardModel.Get(first.Context, creation);
        Require(second.Supported && !second.Created && ReferenceEquals(second.Context, first.Context) &&
            ReferenceEquals(second.Card, first.Card), "Repeated unit accesses must share the same card state.");
        AbilityCardResult other = AbilityCardModel.Get(first.Context, new("other", CardModifiers.Empty(), null, []));
        Require(other.Card!.InstanceId == 3 && other.Context!.AbilityCardCache!.Count == 2 &&
            first.Context.AbilityCardCache!.Count == 1, "Distinct skill definitions must allocate isolated identities.");
        CombatContext cleared = AbilityCardModel.Clear(first.Context);
        Require(cleared.AbilityCardCache!.Count == 0 && cleared.CardRegistry!.Count == 2, "Clearing cache must retain observed references.");
        AbilityCardResult recreated = AbilityCardModel.Get(cleared, creation);
        Require(recreated.Created && recreated.Card!.InstanceId == 3 && recreated.Context!.CardRegistry!.Count == 3,
            "Cache recreation incorrectly reused an old retained reference.");
        Require(!AbilityCardModel.Get(root, new("bad", CardModifiers.Empty(), null, ["Unknown setup trait"])).Supported,
            "Unknown cache setup cannot silently succeed.");
        var malformed = new CombatContext(root.Cards, rng, 0, 2, 10, cardInstances: [owned], cardRegistry: [owned],
            abilityCardCache: [new("skill", 1)]);
        Require(AbilityCardModel.Validate(malformed) != null, "Ordinary ownership cannot alias cache membership.");
        string expected = JsonSerializer.Serialize(first.Context, ModelJson.Options);
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(AbilityCardModel.Get(root, creation).Context, ModelJson.Options) == expected,
            "Parallel skill cache allocations differ."));
        Require(JsonSerializer.Serialize(root, ModelJson.Options) == parent, "Cache transition mutated its parent.");
        Console.WriteLine("ABILITY-CARD-CHECKS PASS: shared cache identity, detached ownership, starting/counter state, clear/recreation and 32 branches.");
    }

    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("AbilityCardOperations", out var operations) || operations.GetArrayLength() == 0)
        {
            Require(!fixture.TryGetProperty("ModifierScenario", out var scenario) || scenario.GetString() != "ability-cache",
                "Requested native ability cache scenario did not execute.");
            return;
        }
        var samples = operations.EnumerateArray().ToArray();
        foreach (var sample in samples) Verify(sample);
        Require(samples.Any(sample => sample.GetProperty("Created").GetBoolean()) &&
            samples.Any(sample => !sample.GetProperty("Created").GetBoolean()), "Cache coverage missed creation or reuse.");
        var initial = fixture.GetProperty("Actions")[0].GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Train.Context!;
        Require(initial.AbilityCardCache is { Count: 0 }, "Native fixture must start before natural skill cache allocation.");
        Require(samples.Any(sample => sample.GetProperty("Created").GetBoolean() &&
            sample.GetProperty("Creation").Deserialize<CardCreationRule>()!.StartingModifiers.Upgrades.Count > 0),
            "Native cache coverage missed starting upgrades.");
        var definitions = samples.Where(sample => sample.GetProperty("Created").GetBoolean()).Select(sample =>
            sample.GetProperty("Creation").Deserialize<CardCreationRule>()!.DataId).Distinct().ToArray();
        Require(definitions.Length >= 2 && fixture.GetProperty("Spawns").EnumerateArray().Any(sample =>
            sample.GetProperty("Actual").Deserialize<EnemySpawnState>()!.Train.Rooms.SelectMany(room => room.Units)
                .Any(unit => unit.Team == CombatTeam.Enemy && unit.Ability != null && definitions.Contains(unit.Ability.DataId))),
            "Native cache coverage missed distinct definitions or enemy ability spawning.");
        var accesses = fixture.GetProperty("AbilityCardAccesses").EnumerateArray().ToArray();
        Require(accesses.GroupBy(sample => sample.GetProperty("DataId").GetString()).Any(group =>
            group.Select(sample => sample.GetProperty("UnitId").GetInt32()).Distinct().Count() >= 2 &&
            group.Select(sample => sample.GetProperty("CardId").GetInt32()).Distinct().Count() == 1),
            "Native coverage did not show multiple units sharing one cache card.");
        Parallel.For(0, 32, _ => { foreach (var sample in samples) Verify(sample); });
        Console.WriteLine($"NATIVE-ABILITY-CARD-CHECKS PASS: {samples.Length} complete native cache contexts, shared unit identities and 32 branches.");
    }
    private static void Verify(FixtureValue sample)
    {
        CombatContext before = sample.GetProperty("Before").Deserialize<CombatContext>()!;
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        var result = AbilityCardModel.Get(before, sample.GetProperty("Creation").Deserialize<CardCreationRule>()!);
        Require(result.Supported, "Native cache unsupported: " + result.UnsupportedReason);
        Require(result.Card!.InstanceId == sample.GetProperty("CardId").GetInt32() &&
            result.Created == sample.GetProperty("Created").GetBoolean(), "Native returned cache identity differs.");
        string? diff = ModelJson.Difference(JsonSerializer.Serialize(result.Context, ModelJson.Options),
            JsonSerializer.Serialize(sample.GetProperty("After").Deserialize<CombatContext>(), ModelJson.Options));
        Require(diff == null, "Native cache context differs: " + diff);
        Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Native cache changed its parent.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

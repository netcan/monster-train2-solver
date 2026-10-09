using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using System.Text.Json;

internal static class EnemySpawningChecks
{
    internal static void Run()
    {
        var context = new CombatContext(new CardCycleState([], [], [], UnityRng.Seed(1), 0, []), UnityRng.Seed(2), 0, 1, 10);
        var train = new TrainCombatState(Enumerable.Range(0, 4).Select(index => new RoomCombatState(index,
            false, [], [], context)).ToArray(), [], 2, context);
        EnemyDefinition first = Definition("first"), second = Definition("second"), treasure = Definition("treasure", false);
        var waves = new[] { new EnemyWave([new EnemyGroup([first, second])]),
            new EnemyWave([new EnemyGroup([])]), new EnemyWave([new EnemyGroup([])]) };
        var source = new EnemySpawnState(train, waves, [-1, -1, -1], 0, false, UnityRng.Seed(3),
            1, [treasure], 1, true, 1, 1, 1, []);
        var spawned = EnemySpawningModel.Spawn(source, true).State!;
        Require(spawned.Train.Rooms[0].Units.Select(unit => unit.AssetKey).SequenceEqual(["second", "first"]) &&
            spawned.Phase == 1 && spawned.TreasuresRemaining == 1,
            "The authored wave order or initial treasure restriction differed.");
        var next = new EnemySpawnState(spawned.Train, waves, spawned.SelectedGroups, spawned.Phase,
            false, spawned.Rng, spawned.NextUnitId, [treasure], 1, true, 1, 1, 1, []);
        var withTreasure = EnemySpawningModel.Spawn(next, true).State!;
        Require(withTreasure.TreasuresRemaining == 0 && withTreasure.Train.Rooms.Skip(1).Take(2)
            .SelectMany(room => room.Units).Count(unit => unit.AssetKey == "treasure") == 1,
            "Treasure was not spawned in an eligible regular floor.");
        var cache = new EnemySpawnState(train, waves, [0, -1, -1], 0, false, source.Rng,
            1, [], 0, false, 1, 1, 1, []);
        Require(EnemySpawningModel.Spawn(cache, false).State!.Rng.Equals(source.Rng),
            "A cached group consumed a new random choice.");
        var overflow = new EnemySpawnState(train, [new EnemyWave([new EnemyGroup([first, first, second])])],
            [-1], 0, false, source.Rng, 1, [], 0, false, 1, 1, 1, []);
        Require(EnemySpawningModel.Spawn(overflow, false).State!.Train.Rooms[1].Units.Count == 1,
            "Spawn slot overflow did not advance to the next floor.");
        Require(source.Train.Rooms.All(room => room.Units.Count == 0) && source.SelectedGroups.All(group => group == -1),
            "Wave selection or spawning mutated its parent.");
        string expected = JsonSerializer.Serialize(spawned);
        Parallel.For(0, 64, _ => Require(JsonSerializer.Serialize(EnemySpawningModel.Spawn(source, true).State) == expected,
            "Parallel spawning changed RNG or unit identities."));
        References();
        Console.WriteLine("SPAWN-CHECKS PASS: wave order, cached choices, treasure timing/floors, slot overflow and isolation.");
    }
    private static void References()
    {
        var rng = UnityRng.Seed(91);
        var cards = new[] {
            new CardInstanceState(1, "dead-equipment", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [], equippedUnitId: 99),
            new CardInstanceState(2, "live-equipment", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [], equippedUnitId: 7) };
        var context = new CombatContext(new CardCycleState([new(1, cards[0].DataId)], [], [], rng, 0, []), rng, 0, 3, 10,
            cardInstances: cards, cardRegistry: cards,
            otherPiles: [new("Standby", [new(2, cards[1].DataId)], equipmentConditions: [new(2, 7, true)]), new("Exhausted", [])]);
        var owner = new CombatUnit(7, "owner", CombatTeam.Player, 0, 1, 1, false, false, false, [], lastAttackerId: 99, equipmentCards: [2]);
        var train = new TrainCombatState([new(0, false, [owner], [], context), new(1, false, [], [], context)], [], 7, context);
        EnemySpawnState Root(bool canonical) => new(train, [new EnemyWave([new EnemyGroup([])])], [-1], 0, false, rng, 8, [], 0, false,
            0, 0, 1, [], canonicalDecisionReferences: canonical);
        string parent = JsonSerializer.Serialize(Root(true), ModelJson.Options);
        var canonical = EnemySpawningModel.Spawn(Root(true), false).State!;
        var legacy = EnemySpawningModel.Spawn(Root(false), false).State!;
        Require(canonical.CanonicalDecisionReferences && canonical.Train.Context!.CardRegistry![0].EquippedUnitId == 0 &&
            canonical.Train.Context.CardRegistry[1].EquippedUnitId == 7 && canonical.Train.Rooms[0].Units.Single().LastAttackerId == 0 &&
            legacy.Train.Context!.CardRegistry![0].EquippedUnitId == 99 && legacy.Train.Rooms[0].Units.Single().LastAttackerId == 99,
            "Spawning lost live equipment or retained dead references at a declared canonical boundary.");
        string expected = JsonSerializer.Serialize(canonical, ModelJson.Options);
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(EnemySpawningModel.Spawn(Root(true), false).State, ModelJson.Options) == expected,
            "Parallel spawning reference normalization differed."));
        Require(JsonSerializer.Serialize(Root(true), ModelJson.Options) == parent, "Spawning reference normalization mutated its root.");
    }
    private static EnemyDefinition Definition(string name, bool ascends = true) =>
        new(new CombatUnit(0, name, CombatTeam.Enemy, 0, 1, 1, false, false, false, []), ascends, false, []);
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }

    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("Spawns", out FixtureValue spawns)) return;
        if (spawns.GetArrayLength() == 0 && fixture.GetProperty("NativeWon").ValueKind == FixtureKind.False &&
            fixture.GetProperty("Turns").GetArrayLength() == 1 &&
            fixture.GetProperty("Turns")[0].GetProperty("ActualOutcome").GetInt32() == (int)RoomOutcome.PlayerDefeated)
        {
            var turn = fixture.GetProperty("Turns")[0];
            var before = turn.GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn;
            var after = turn.GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn;
            Require(before.Phase == after.Phase && JsonSerializer.Serialize(before.SelectedGroups) == JsonSerializer.Serialize(after.SelectedGroups) &&
                JsonSerializer.Serialize(before.Rng) == JsonSerializer.Serialize(after.Rng), "First-turn defeat unexpectedly advanced a wave or spawning RNG.");
            Console.WriteLine("NATIVE-SPAWN-CHECKS PASS: terminal first-turn defeat skips new waves and preserves phase/cache/RNG.");
            return;
        }
        int matched = 0, unsupported = 0;
        foreach (FixtureValue spawn in spawns.EnumerateArray())
        {
            EnemySpawnState before = spawn.GetProperty("Before").Deserialize<EnemySpawnState>()!;
            EnemySpawnResult predicted = EnemySpawningModel.Spawn(before, spawn.GetProperty("IncludeTreasure").GetBoolean());
            if (!predicted.Supported) { unsupported++; continue; }
            EnemySpawnState actual = spawn.GetProperty("Actual").Deserialize<EnemySpawnState>()!;
            string expectedState = Comparable(predicted.State!), actualState = Comparable(actual);
            Require(expectedState == actualState, "Native spawning differs at index " + spawn.GetProperty("Index").GetInt32() +
                ": " + ModelJson.Difference(expectedState, actualState));
            matched++;
        }
        Require(matched > 0, "No native spawning phases verified.");
        Require(unsupported == 0, "Native spawning verification skipped unsupported phases.");
        Console.WriteLine($"NATIVE-SPAWN-CHECKS PASS: {matched} matched, {unsupported} unsupported.");
    }
    private static string Comparable(EnemySpawnState state) => JsonSerializer.Serialize(new
    {
        Rooms = state.Train.Rooms.Select(room => new { room.RoomIndex, room.Units }).ToArray(),
        Movement = state.Train.Movement.OrderBy(rule => rule.UnitId).ToArray(), state.Train.Context,
        state.Phase, state.SelectedGroups, state.Rng, state.NextUnitId, state.TreasuresRemaining, state.CanonicalDecisionReferences
    });
}

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
        Console.WriteLine("SPAWN-CHECKS PASS: wave order, cached choices, treasure timing/floors, slot overflow and isolation.");
    }
    private static EnemyDefinition Definition(string name, bool ascends = true) =>
        new(new CombatUnit(0, name, CombatTeam.Enemy, 0, 1, 1, false, false, false, []), ascends, false, []);
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }

    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("Spawns", out FixtureValue spawns)) return;
        int matched = 0, unsupported = 0;
        foreach (FixtureValue spawn in spawns.EnumerateArray())
        {
            EnemySpawnState before = spawn.GetProperty("Before").Deserialize<EnemySpawnState>()!;
            EnemySpawnResult predicted = EnemySpawningModel.Spawn(before, spawn.GetProperty("IncludeTreasure").GetBoolean());
            if (!predicted.Supported) { unsupported++; continue; }
            EnemySpawnState actual = spawn.GetProperty("Actual").Deserialize<EnemySpawnState>()!;
            Require(Comparable(predicted.State!) == Comparable(actual), "Native spawning differs at index " + spawn.GetProperty("Index"));
            matched++;
        }
        Require(matched > 0, "No native spawning phases verified.");
        Console.WriteLine($"NATIVE-SPAWN-CHECKS PASS: {matched} matched, {unsupported} unsupported.");
    }
    private static string Comparable(EnemySpawnState state) => JsonSerializer.Serialize(new
    {
        Rooms = state.Train.Rooms.Select(room => new { room.RoomIndex, room.Units }).ToArray(),
        Movement = state.Train.Movement.OrderBy(rule => rule.UnitId).ToArray(), state.Train.Context,
        state.Phase, state.SelectedGroups, state.Rng, state.NextUnitId, state.TreasuresRemaining
    });
}

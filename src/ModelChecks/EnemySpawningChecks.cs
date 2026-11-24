using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using System.Security.Cryptography;
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
        StartingConduitGuard();
        Console.WriteLine("SPAWN-CHECKS PASS: wave order, cached choices, treasure timing/floors, slot overflow and isolation.");
    }
    internal static void StartingConduitGuard()
    {
        var rng = UnityRng.Seed(92);
        var unit = new CombatUnit(0, "off-train-conduit", CombatTeam.Enemy, 0, 5, 5, false, false, false,
            [new CombatStatus("conduit", 4)]);
        var definition = new EnemyDefinition(unit, true, false, []);
        var train = new TrainCombatState(Enumerable.Range(0, 4).Select(index => new RoomCombatState(index, false, [], [])).ToArray(), [], 7);
        var root = new EnemySpawnState(train, [new EnemyWave([new EnemyGroup([definition])])], [-1], 0,
            false, rng, 1, [], 0, false, 1, 1, 1, []);
        string parent = JsonSerializer.Serialize(root);
        EnemySpawnResult result = EnemySpawningModel.Spawn(root, false);
        Require(!result.Supported && result.UnsupportedReason!.Contains("off-train") && JsonSerializer.Serialize(root) == parent,
            "Native-invalid off-train starting Conduit must be rejected without changing the parent.");
        Console.WriteLine("ENEMY-CONDUIT-BIRTH-GUARD PASS: native-invalid off-train starting stacks rejected; post-entry callbacks calibrated separately.");
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
            EnemySpawnResult predicted = EnemySpawningModel.Spawn(before, spawn.GetProperty("IncludeTreasure").GetBoolean(),
                spawn.TryGetProperty("Rooms", out FixtureValue roomRules) && roomRules.ValueKind == FixtureKind.Array
                    ? roomRules.Deserialize<RoomPlayRule[]>() : null);
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

    internal static void OriginalRandomizedWaves(FixtureValue fixture, bool requireNontrivial)
    {
        Require(fixture.GetProperty("GameVersion").GetString() == "2.3.1" &&
            fixture.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d",
            "Expected the pinned randomized wave capture.");
        FixtureValue evidence = fixture.GetProperty("OriginalRandomizedWaves");
        int[] order = evidence.GetProperty("AuthoredWaveIndices").Deserialize<int[]>()!;
        int[] authored = Enumerable.Range(0, order.Length).ToArray();
        Require(order.Length >= 4 && order.Order().SequenceEqual(authored) && (!requireNontrivial || !order.SequenceEqual(authored)) &&
            order[^2] == authored[^2] && order[^1] == authored[^1] &&
            evidence.GetProperty("BonusCovenantId").GetString() == RelicRandomizedWavesModel.BonusCovenantId,
            "Expected the original permutation, authored final waves and bonus enemy Covenant.");
        bool isComponent = fixture.TryGetProperty("DeploymentSpawn", out FixtureValue deploymentValue);
        if (isComponent)
        {
            Require(fixture.GetProperty("SourceBattleSha256").GetString() is { Length: 64 } hash && hash.All(Uri.IsHexDigit),
                "Expected serialized source battle provenance for the wave component.");
        }
        else deploymentValue = fixture.GetProperty("Turns")[0].GetProperty("Before").GetProperty("Spawn");
        EnemySpawnState deployment = deploymentValue.Deserialize<EnemySpawnState>()!;
        Require(deployment.Turn == 0 && !deployment.Looping && deployment.Waves.Count == order.Length &&
            deployment.SelectedGroups.All(group => group == -1) &&
            deployment.Train.Context?.Relics?.Count(RelicRandomizedWavesModel.IsOriginal) == 1,
            "Expected an unsettled spawn cache and the active original owner at deployment.");
        var rows = fixture.GetProperty("Spawns").EnumerateArray().ToArray();
        for (int phase = 0; phase < order.Length; phase++)
        {
            Require(rows.Any(row => row.GetProperty("Before").GetProperty("Phase").GetInt32() == phase &&
                row.GetProperty("Actual").GetProperty("Phase").GetInt32() == phase + 1 &&
                row.GetProperty("Actual").GetProperty("SelectedGroups")[phase].GetInt32() >= 0),
                "Native did not select a group at projected wave phase " + phase + ".");
        }
        Native(fixture);
    }

    internal static void ExportRandomizedWaves(string inputPath, string outputPath)
    {
        using var source = File.OpenRead(inputPath);
        string hash = Convert.ToHexString(SHA256.HashData(source)).ToLowerInvariant();
        source.Position = 0;
        using var captured = FixtureDocument.Read(source);
        FixtureValue battle = captured.RootElement;
        Require(battle.GetProperty("CaptureFailures").GetInt32() == 0 && battle.GetProperty("Pending").GetInt32() == 0,
            "Expected a complete native wave capture.");
        OriginalRandomizedWaves(battle, requireNontrivial: true);

        var builder = new FixtureBuilder();
        int root = builder.Add(FixtureKind.Object, children: [
            Text("SourceBattleSha256"), Text(hash),
            Text("GameVersion"), builder.Import(battle.GetProperty("GameVersion")),
            Text("GameModuleMvid"), builder.Import(battle.GetProperty("GameModuleMvid")),
            Text("OriginalRandomizedWaves"), builder.Import(battle.GetProperty("OriginalRandomizedWaves")),
            Text("DeploymentSpawn"), builder.Import(battle.GetProperty("Turns")[0].GetProperty("Before").GetProperty("Spawn")),
            Text("Spawns"), builder.Import(battle.GetProperty("Spawns"))]);
        using FixtureDocument fixture = builder.Build(root, 0, new byte[32]);
        using var output = new FileStream(outputPath, FileMode.CreateNew);
        fixture.Write(output);

        int Text(string value) => builder.Add(FixtureKind.String, value);
    }

    internal static string Comparable(EnemySpawnState state) => JsonSerializer.Serialize(new
    {
        Rooms = state.Train.Rooms.Select(room => new { room.RoomIndex, room.Units }).ToArray(),
        Movement = state.Train.Movement.OrderBy(rule => rule.UnitId).ToArray(), Context = BattleTurnChecks.ComparableContext(state.Train.Context),
        state.Phase, state.SelectedGroups, state.Rng, state.NextUnitId, state.TreasuresRemaining, state.CanonicalDecisionReferences
    });
}

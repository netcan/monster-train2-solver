using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UnitIdentityChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(42);
        CardPileState[] piles = [new("Standby", []), new("Exhausted", []), new("Eaten", []), new("Purged", []), new("DiscardBuffer", [])];
        var context = new CombatContext(new([new(1, "unit"), new(2, "unit")], [], [], rng, 0, []),
            rng, 0, 3, 10, otherPiles: piles, nextUnitId: 10);
        var pyre = new CombatUnit(1, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, []);
        var template = new CombatUnit(0, "unit", CombatTeam.Player, 2, 5, 5, true, false, false, [], size: 1);
        var enemy = new EnemyDefinition(new(0, "enemy", CombatTeam.Enemy, 1, 4, 4, true, false, false, []), true, false, []);
        var waves = new EnemyWave[] { new([new([enemy, enemy])]) };
        var train = new TrainCombatState([new(0, false, [], [], context), new(1, false, [pyre], [], context)], [], 7, context);
        EnemySpawnState Spawn(TrainCombatState state, int next) => new(state, waves, [-1], 0, false, rng, next, [], 0, false, 0, 0, 2, []);
        var rules = new BattlePlayRules([new(0, 5, 7, true, false, false)], [new("unit", "unit", 1, "SpawnMonster", "Standby", template, [])]);
        BattleTurnState Decision(TrainCombatState state, int next) => new(Spawn(state, next), 3, 3, 5, 0, 0, "New", [new("Spawning", 42, rng)],
            state.Context!.OtherPiles!, [], rules);
        var root = Decision(train, 10);
        string parent = JsonSerializer.Serialize(root);
        string RunChain()
        {
            var first = BattleActionModel.PlayCard(root, new(1, 0, 0));
            Require(first.Supported && first.State!.Spawn.NextUnitId == 11 && first.State.Spawn.Train.Context!.NextUnitId == 11,
                "Paid spawn did not advance both unit identity counters.");
            var damaged = CardSpellModel.Apply(first.State!.Spawn.Train, 0, [new("Damage", "DropTargetCharacter", 99, false, true, [])], 10);
            Require(damaged.Supported && damaged.State!.Rooms[0].Units.Count == 0 && damaged.State.Context!.NextUnitId == 11,
                "Death reset the unit identity counter.");
            var second = BattleActionModel.PlayCard(Decision(damaged.State!, 11), new(2, 0, 0));
            Require(second.Supported && second.State!.Spawn.Train.Rooms[0].Units.Single().Id == 11,
                "Later summon reused a dead unit identity.");
            var wave = EnemySpawningModel.Spawn(second.State!.Spawn, false);
            Require(wave.Supported && wave.State!.NextUnitId == 14 && wave.State.Train.Context!.NextUnitId == 14 &&
                wave.State.Train.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Enemy).Select(unit => unit.Id).SequenceEqual([12, 13]) &&
                wave.State.Train.Rooms.All(room => room.Context!.NextUnitId == 14),
                "Enemy group did not share the monotonic player/effect identity allocation.");
            return JsonSerializer.Serialize(wave.State);
        }
        string expected = RunChain();
        Parallel.For(0, 32, _ => Require(RunChain() == expected, "Parallel unit identity allocations diverged."));
        Require(JsonSerializer.Serialize(root) == parent, "Unit allocation mutated its parent.");
        Require(!UnitIdentityModel.Allocate(context, 11).Supported && UnitIdentityModel.Validate(context, [new(10, "overlap", CombatTeam.Player, 0, 1, 1, false, false, false, [])]) != null,
            "Conflicting outer/shared or already allocated unit identity was accepted.");
        var legacy = new CombatContext(context.Cards, rng, 0, 3, 10);
        Require(!UnitIdentityModel.Allocate(legacy).Supported && UnitIdentityModel.Allocate(legacy, 10) is { Supported: true, UnitId: 10, NextUnitId: 11 } legacyAllocation &&
            legacyAllocation.Context!.NextUnitId == null && !UnitIdentityModel.Allocate(legacy, 0).Supported &&
            !UnitIdentityModel.Allocate(legacy, int.MaxValue).Supported, "Missing, exhausted or legacy identity metadata was handled incorrectly.");
        var inconsistent = Decision(train, 11);
        Require(!BattleActionModel.PlayCard(inconsistent, new(1, 0, 0)).Supported && !BattleTurnModel.EndTurn(inconsistent).Supported &&
            !EnemySpawningModel.Spawn(inconsistent.Spawn, false).Supported,
            "An action, turn or enemy wave accepted inconsistent shared identity metadata.");
        Console.WriteLine("UNIT-IDENTITY-CHECKS PASS: shared player/enemy allocation, dead identity retention, missing/conflicting/exhausted metadata, legacy behavior and 32 parallel branches.");
    }

    internal static void Native(FixtureValue fixture)
    {
        if (fixture.GetProperty("Schema").GetInt32() < 77) return;
        int boundaries = 0, births = 0;
        foreach (string collection in new[] { "Actions", "Turns" })
        foreach (var record in fixture.GetProperty(collection).EnumerateArray())
        {
            var before = record.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var after = record.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Verify(before.Spawn); Verify(after.Spawn); boundaries += 2;
            Require(after.Spawn.NextUnitId >= before.Spawn.NextUnitId, "Native unit identity allocation decreased across a decision.");
            births += after.Spawn.NextUnitId - before.Spawn.NextUnitId;
        }
        foreach (var record in fixture.GetProperty("Spawns").EnumerateArray())
        foreach (string side in new[] { "Before", "Actual" })
        { Verify(record.GetProperty(side).Deserialize<EnemySpawnState>()!); boundaries++; }
        Require(boundaries > 0 && births > 0, "Native shared identity coverage lacks decisions or unit births.");
        Console.WriteLine($"NATIVE-UNIT-IDENTITY-CHECKS PASS: {boundaries} complete spawn/decision boundaries, {births} decision-path births, consistent room/shared/outer counters and no identity reuse.");

        static void Verify(EnemySpawnState state)
        {
            Require(state.Train.Context!.NextUnitId == state.NextUnitId &&
                state.Train.Rooms.All(room => room.Context!.NextUnitId == state.NextUnitId) &&
                UnitIdentityModel.Validate(state.Train.Context, state.Train.Rooms.SelectMany(room => room.Units), state.NextUnitId) == null,
                "Native shared, room or outer unit identity state is inconsistent.");
        }
    }
    private static void Require(bool pass, string message) { if (!pass) throw new InvalidOperationException(message); }
}

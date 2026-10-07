using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RoomCapacityChecks
{
    internal static void Run()
    {
        foreach (var sample in new[] { (5, -100, 1), (1, -1, 1), (5, 100, 30), (30, 1, 30), (5, 0, 5),
            (5, int.MaxValue, 1), (0, 0, 1), (35, -1, 30), (35, 1, 35) })
            Require(RoomCapacityModel.AdjustedMaximum(sample.Item1, sample.Item2) == sample.Item3, "Native capacity boundary arithmetic differs.");
        var rng = UnityRng.Seed(439);
        var owner = new CardInstanceState(1, "capacity", CardModifiers.Empty(), CardModifiers.Empty(), 1, 0, 0, [],
            capacityScalingTraits: [new(new("UnmodifiedPlayedCost", "ThisTurn"), 2), new(new("UnmodifiedPlayedCost", "ThisTurn"), -1)]);
        var context = new CombatContext(new([new(1, "capacity"), new(2, "unit"), new(3, "unit")], [], [], rng, 0, []), rng, 0, 4, 10,
            statistics: new BattleStatistics([], [], [], [], [], [], [], 0, 0, 0, 0, 0, [1, 2, 3], [1, 2, 3]).WithPlayedCost(1, 1),
            cardInstances: [owner, CardInstanceState.Empty(2, "unit"), CardInstanceState.Empty(3, "unit")],
            otherPiles: [new("Standby", []), new("Exhausted", []), new("Purged", []), new("Eaten", [])], queryFrame: new(10, true, 2, 0, 0, 1, 0),
            roomCapacities: [new(0, 5, 5, false), new(1, 0, 0, true)]);
        var grow = new CardActionEffect("AdjustCapacity", "Room", 1, false, true, [], range: new(-10, 10, .75f));
        string parent = JsonSerializer.Serialize(context);
        var adjusted = RoomCapacityModel.Apply(context, 0, grow, 1);
        Require(adjusted.Supported && adjusted.Amount == 2 && adjusted.Context!.RoomCapacities![0].PlayerMaximum == 7 &&
            adjusted.Context.BattleRng.Equals(rng), "Ordered capacity scaling or ignored range differs: " + adjusted.UnsupportedReason);
        var enemy = RoomCapacityModel.Apply(context, 0, new("AdjustCapacity", "Room", -100, true, false, []));
        Require(enemy.Context!.RoomCapacities![0].EnemyMaximum == 1 && enemy.Context.RoomCapacities[0].PlayerMaximum == 5,
            "Enemy adjustment changed player capacity.");
        var combined = RoomCapacityModel.Apply(context, 0, new("AdjustCapacity", "Room", 2, true, true, []));
        Require(combined.Context!.RoomCapacities![0].PlayerMaximum == 7 && combined.Context.RoomCapacities[0].EnemyMaximum == 5,
            "Combined team flags did not select the native monster group.");
        var foe = new CombatUnit(10, "enemy", CombatTeam.Enemy, 1, 0, 1, true, false, false, []);
        var conditional = new CardActionEffect("AdjustCapacity", "Room", 1, false, true, [], onlyIfNoEnemies: true);
        Require(!RoomCapacityModel.Test(new(0, false, [foe], [], context), conditional) &&
            RoomCapacityModel.Test(new(0, false, [], [], context), conditional) && !RoomCapacityModel.Test(new(1, false, [], [], context), grow),
            "Capacity test ignored retained dying enemies or the Pyre room.");
        var room = new RoomCombatState(0, false, [], [], context);
        Require(CardSpellModel.Apply(room, [grow], 0, 1).State!.Context!.RoomCapacities![0].PlayerMaximum == 7 &&
            CardSpellModel.Apply(new(0, false, [], [], context, true), [grow], 0, 1).State!.Context!.RoomCapacities![0].PlayerMaximum == 5,
            "Capacity spell or preview gate differs.");
        var unit = new CombatUnit(4, "unit", CombatTeam.Player, 1, 50, 50, true, false, false, [], size: 3);
        var pyre = new CombatUnit(5, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, []);
        CardPileState[] piles = [new("Standby", []), new("Exhausted", []), new("Purged", []), new("Eaten", [])];
        var train = new TrainCombatState([new(0, false, [unit], [], context), new(1, false, [pyre], [], context)], [], 7, context);
        var spawn = new EnemySpawnState(train, [new([new([])])], [-1], 0, false, rng, 6, [], 0, false, 0, 0, 2, []);
        var rules = new BattlePlayRules([new(0, 5, 7, true, false, false), new(1, 0, 7, true, true, true)],
            [new("capacity", "capacity", 0, "Spell", "Discard", null, [], [grow]), new("unit", "unit", 0, "SpawnMonster", "Standby", unit, [])]);
        var root = new BattleTurnState(spawn, 10, 3, 5, 0, 0, "New", [], piles, [], rules);
        Require(BattleActionModel.PlayCard(root, new(2, 0)).Rejection == ActionRejection.Illegal, "Static room accepted an oversized summon.");
        var grown = BattleActionModel.PlayCard(root, new(1, 0));
        Require(grown.Supported && BattleActionModel.PlayCard(grown.State!, new(2, 0)).Supported,
            "A changed room capacity was not used by the next summon: " + grown.Reason);
        var restrictedUnit = new CombatUnit(11, "unit", CombatTeam.Player, 1, 50, 50, true, false, false, [], spawnerCardId: 2,
            size: 3, modifiers: new(1, 0, 0, 3, 2, true, false, []));
        var size = new CardUpgradeModifier("size", "size", new(size: 3), [], false, false, false, 0, 0, [], restrictSizeToRoomCapacity: true);
        var blocked = UnitModifierModel.Apply(new(0, false, [restrictedUnit], [], context), 11, size, "TemporaryUntilUnitDeath", roomCapacity: 99);
        var permitted = UnitModifierModel.Apply(new(0, false, [restrictedUnit], [], adjusted.Context), 11, size, "TemporaryUntilUnitDeath", roomCapacity: 1);
        Require(blocked.Supported && blocked.State!.Units[0].Size == 3 && permitted.Supported && permitted.State!.Units[0].Size == 6,
            "Restricted size upgrades used stale capacity arguments instead of shared live state.");
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(RoomCapacityModel.Apply(context, 0, grow, 1)) == JsonSerializer.Serialize(adjusted),
            "Parallel capacity branches differ."));
        Require(JsonSerializer.Serialize(context) == parent, "Capacity branches changed their parent.");
        Console.WriteLine("CAPACITY-CHECKS PASS: boundaries, signed wrap, ignored ranges, ordered statistic traits, exact team mapping, dying-enemy/Pyre/preview gates, live summon legality and 32 parallel branches.");
    }
    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("CapacityEffects", out var records) || records.GetArrayLength() == 0) return;
        int count = 0, bounds = 0, rejected = 0, ranges = 0, scaled = 0, unit = 0, positive = 0, negative = 0, zero = 0,
            occupied = 0, wraps = 0, scaleChanges = 0;
        foreach (var record in records.EnumerateArray())
        {
            Require(record.GetProperty("Completed").GetBoolean(), "Incomplete native capacity effect.");
            var before = record.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var after = record.GetProperty("After").Deserialize<RoomCombatState>()!;
            var effect = record.GetProperty("Effect").Deserialize<CardActionEffect>()!;
            int source = record.GetProperty("SourceCardId").GetInt32();
            CombatContext predicted = before.Context!;
            if (RoomCapacityModel.Test(before, effect))
            {
                var applied = RoomCapacityModel.Apply(predicted, before.RoomIndex, effect, source);
                Require(applied.Supported, "Native capacity effect unsupported: " + applied.UnsupportedReason); predicted = applied.Context!;
            }
            string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted), JsonSerializer.Serialize(after.Context));
            Require(difference == null, "Native capacity context differs: " + difference);
            foreach (var attempt in record.GetProperty("Attempts").EnumerateArray())
            {
                int max = attempt.GetProperty("Maximum").GetInt32(), requested = attempt.GetProperty("Requested").GetInt32();
                bool allowed = !(requested < 0 && max <= 1 || requested > 0 && max >= 30);
                int approved = allowed ? unchecked(RoomCapacityModel.AdjustedMaximum(max, requested) - max) : requested;
                Require(attempt.GetProperty("Allowed").GetBoolean() == allowed && attempt.GetProperty("Approved").GetInt32() == approved &&
                    attempt.GetProperty("Error").GetString() == (allowed ? "None" : requested < 0 ? "MinBounds" : "MaxBounds"),
                    "Native capacity clamp/approval differs.");
                bounds++; if (!allowed) rejected++;
                if (allowed && requested > 0 && unchecked(max + requested) < 0) wraps++;
            }
            var capacity = after.Context!.RoomCapacities!.Single(room => room.RoomIndex == before.RoomIndex);
            if (after.Units.Where(unit => unit.Team == CombatTeam.Player).Sum(unit => unit.Size) > capacity.PlayerMaximum ||
                after.Units.Where(unit => unit.Team == CombatTeam.Enemy).Sum(unit => unit.Size) > capacity.EnemyMaximum) occupied++;
            count++; if (effect.Range != null) ranges++; if (source == 0) unit++;
            if (source > 0 && before.Context!.CardInstances!.First(card => card.InstanceId == source).CapacityScalingTraits?.Count > 0)
            {
                scaled++;
                var attempts = record.GetProperty("Attempts");
                if (attempts.GetArrayLength() > 0 && attempts.EnumerateArray().First().GetProperty("Requested").GetInt32() != effect.Value) scaleChanges++;
            }
            if (effect.Value > 0) positive++; else if (effect.Value < 0) negative++; else zero++;
        }
        int tests = 0, refusals = 0;
        foreach (var test in fixture.GetProperty("CapacityTests").EnumerateArray())
        {
            var before = test.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var effect = test.GetProperty("Effect").Deserialize<CardActionEffect>()!;
            bool actual = test.GetProperty("Actual").GetBoolean(); Require(RoomCapacityModel.Test(before, effect) == actual, "Native capacity TestEffect differs.");
            tests++; if (!actual) refusals++;
        }
        if (fixture.GetProperty("ModifierScenario").GetString() is "room-capacity" or "room-capacity-lethal")
            Require(bounds > 0 && rejected > 0 && ranges > 0 && scaled > 0 && unit > 0 && positive > 0 && negative > 0 && zero > 0 && refusals > 0 &&
                occupied > 0 && wraps > 0 && scaleChanges > 0,
                "Native capacity mechanism coverage incomplete.");
        Console.WriteLine($"NATIVE-CAPACITY-CHECKS PASS: {count} exact contexts, {bounds} bounds/{rejected} refusals, {ranges} ignored ranges, {scaled} scaled/{unit} unit effects, {tests} native tests/{refusals} failed gates, {occupied} occupied oversize states, {wraps} wraps and {scaleChanges} changed trait quantities.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class TrainCombatChecks
{
    internal static void Run()
    {
        var enemy = new CombatUnit(1, "enemy", CombatTeam.Enemy, 4, 10, 10, true, false, false, []);
        var low = new CombatUnit(2, "low", CombatTeam.Player, 5, 20, 20, true, false, false, []);
        var high = new CombatUnit(3, "high", CombatTeam.Player, 6, 20, 20, true, false, false, []);
        var pyre = new CombatUnit(4, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false,
            [new CombatStatus("relentless", 1), new CombatStatus("sweep", 1)]);
        var fresh = new CombatUnit(5, "new-wave", CombatTeam.Enemy, 1, 10, 10, true, false, false, []);
        EnemyMovement[] supplied = [new(1, 1, true, false), new(5, 1, true, false)];
        var waves = new TrainCombatState([new(1, false, [enemy], []), new(0, false, [fresh], []),
            new(2, false, [], []), new(3, false, [pyre], [])], supplied, 7);
        var wavesMoved = TrainCombatModel.Ascend(waves);
        Require(waves.Movement.Select(rule => rule.UnitId).SequenceEqual([5, 1]) && wavesMoved.Supported &&
            wavesMoved.State!.Movement.Select(rule => rule.UnitId).SequenceEqual([5, 1]) &&
            wavesMoved.State.Rooms[1].Units.Single().Id == 5 && wavesMoved.State.Rooms[2].Units.Single().Id == 1 &&
            supplied.Select(rule => rule.UnitId).SequenceEqual([1, 5]), "New/surviving wave movement metadata lost physical order or mutated its caller.");
        var root = new TrainCombatState([
            new RoomCombatState(0, false, [enemy, low], []),
            new RoomCombatState(1, false, [high], []),
            new RoomCombatState(2, false, [], []),
            new RoomCombatState(3, false, [pyre], [])
        ], [new EnemyMovement(1, 1, true, false)], 7);
        TrainCombatResult combat = TrainCombatModel.ResolveCombat(root);
        Require(combat.Supported && combat.State!.Rooms[0].Units[0].Health == 5 &&
            combat.RoomResults.Select(result => result.State!.RoomIndex).SequenceEqual([3, 2, 1, 0]),
            "Train combat did not resolve floors from top to bottom.");
        TrainCombatResult moved = TrainCombatModel.Ascend(combat.State!);
        Require(moved.Supported && moved.State!.Rooms[0].Units.Count == 1 &&
            moved.State.Rooms[1].Units.Count == 2 && moved.State.Rooms[1].Units[0].Health == 5,
            "An ascended enemy fought again on a regular floor in the same turn.");
        TrainCombatResult second = TrainCombatModel.ResolveCombat(moved.State!);
        Require(second.State!.Rooms[1].Units.Count == 1 && second.State.Rooms[1].Units[0].Health == 16,
            "The enemy failed to fight on its new floor in the next combat phase.");
        var atTop = new TrainCombatState([
            new RoomCombatState(0, false, [], []), new RoomCombatState(1, false, [], []),
            new RoomCombatState(2, false, [enemy], []), new RoomCombatState(3, false, [pyre], [])
        ], [new EnemyMovement(1, 1, true, false)], 7);
        TrainCombatResult tower = TrainCombatModel.Ascend(atTop);
        Require(tower.State!.Rooms[3].Units.Count == 1 && tower.State.Rooms[3].Units[0].Health == 76,
            "An enemy entering the Pyre did not fight immediately, enemy first.");
        Require(root.Rooms[0].Units[0].Health == 10 && root.Rooms[3].Units[0].Health == 80,
            "Train resolution mutated its parent.");
        var rooted = new CombatUnit(1, "rooted", CombatTeam.Enemy, 4, 10, 10, true, false, false,
            [new CombatStatus("rooted", 1, removeWhenTriggered: true)]);
        var rootedTrain = new TrainCombatState([
            new RoomCombatState(0, false, [rooted], []), new RoomCombatState(1, false, [], []),
            new RoomCombatState(2, false, [], []), new RoomCombatState(3, false, [pyre], [])
        ], [new EnemyMovement(1, 1, true, false)], 7);
        TrainCombatResult stayed = TrainCombatModel.Ascend(rootedTrain);
        Require(stayed.State!.Rooms[0].Units[0].Statuses.Count == 0,
            "Rooted did not prevent movement and consume a stack.");
        Require(TrainCombatModel.Ascend(stayed.State).State!.Rooms[1].Units.Count == 1,
            "A root removed in the previous turn still prevented movement.");
        var relentless = new CombatUnit(6, "relentless", CombatTeam.Enemy, 4, 10, 10, true, false, false,
            [new CombatStatus("relentless", 1)]);
        var guard = new CombatUnit(7, "guard", CombatTeam.Player, 2, 10, 10, true, false, false,
            [new CombatStatus("undying", 1)]);
        var outerBoss = new CombatUnit(8, "outer-boss", CombatTeam.Enemy, 0, 10, 10, false, false, false, [],
            bumpRules: new UnitBumpRules(loops: false, companionBoss: false, outerBoss: true));
        var relentlessTrain = new TrainCombatState([
            new RoomCombatState(0, false, [], []),
            new RoomCombatState(1, false, [relentless, guard, outerBoss], []),
            new RoomCombatState(2, false, [], []),
            new RoomCombatState(3, false, [pyre], [])
        ], [new EnemyMovement(6, 1, true, false), new EnemyMovement(8, 1, false, false)], 7);
        TrainCombatResult relentlessAscended = TrainCombatModel.Ascend(relentlessTrain);
        Require(relentlessAscended.Supported && relentlessAscended.DestroyedRoomIndexes.SequenceEqual([1]) &&
            relentlessAscended.State!.Rooms[1].Units.Select(unit => unit.Id).SequenceEqual([outerBoss.Id]) &&
            relentlessAscended.State.Rooms[2].Units.Any(unit => unit.Id == relentless.Id) &&
            relentlessAscended.RoomResults.SelectMany(result => result.Events).Any(item => item.Kind == "Death" && item.Target == guard.Id),
            "Relentless ascension did not remove Undying, sacrifice room occupants or preserve the Outer Boss.");
        Require(relentlessTrain.Rooms[1].Units.Count == 3,
            "Relentless room destruction mutated its parent.");
        Console.WriteLine("TRAIN-CHECKS PASS: floor order, ascension, immediate Pyre combat, relentless destruction, Undying and Outer Boss gates, parent isolation.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }

    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("TrainPhases", out FixtureValue phases)) return;
        int matched = 0, unsupported = 0;
        foreach (FixtureValue phase in phases.EnumerateArray())
        {
            string kind = phase.GetProperty("Kind").GetString()!;
            TrainCombatState before = phase.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatResult result = kind == "Combat" ? TrainCombatModel.ResolveCombat(before) : TrainCombatModel.Ascend(before);
            if (!result.Supported) { unsupported++; continue; }
            TrainCombatState actual = phase.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            Require(Comparable(result.State!, kind) == Comparable(actual, kind),
                "Native train state differs at phase " + phase.GetProperty("Index"));
            matched++;
        }
        Require(matched > 0, "No native train phases were verified.");
        Require(unsupported == 0, "Native train verification skipped unsupported phases.");
        Console.WriteLine($"NATIVE-TRAIN-CHECKS PASS: {matched} matched, {unsupported} unsupported.");
    }

    private static string Comparable(TrainCombatState state, string kind)
    {
        JsonNode comparable = JsonNode.Parse(JsonSerializer.Serialize(new
        {
            Rooms = state.Rooms.Select(room => new { room.RoomIndex, room.Units }).ToArray(),
            Context = BattleTurnChecks.ComparableContext(state.Context)
        }))!;
        // DoTurn may finish the next draw before an Ascend wrapper captures its result.
        if (kind == "Ascend") (comparable["Context"]?["Statistics"] as JsonObject)?.Remove("StoredCards");
        return comparable.ToJsonString();
    }
}

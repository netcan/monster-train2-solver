using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using System.Text.Json;

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
        Console.WriteLine("TRAIN-CHECKS PASS: floor order, ascension, immediate Pyre combat, parent isolation.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }

    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("TrainPhases", out FixtureValue phases)) return;
        int matched = 0, unsupported = 0;
        foreach (FixtureValue phase in phases.EnumerateArray())
        {
            TrainCombatState before = phase.GetProperty("Before").Deserialize<TrainCombatState>()!;
            TrainCombatResult result = phase.GetProperty("Kind").GetString() == "Combat"
                ? TrainCombatModel.ResolveCombat(before) : TrainCombatModel.Ascend(before);
            if (!result.Supported) { unsupported++; continue; }
            TrainCombatState actual = phase.GetProperty("Actual").Deserialize<TrainCombatState>()!;
            Require(Comparable(result.State!) == Comparable(actual),
                "Native train state differs at phase " + phase.GetProperty("Index"));
            matched++;
        }
        Require(matched > 0, "No native train phases were verified.");
        Require(unsupported == 0, "Native train verification skipped unsupported phases.");
        Console.WriteLine($"NATIVE-TRAIN-CHECKS PASS: {matched} matched, {unsupported} unsupported.");
    }

    private static string Comparable(TrainCombatState state) => JsonSerializer.Serialize(new
        { Rooms = state.Rooms.Select(room => new { room.RoomIndex, room.Units }).ToArray(), state.Context });
}

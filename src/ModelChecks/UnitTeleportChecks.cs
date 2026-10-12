using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class UnitTeleportChecks
{
    internal static void Run()
    {
        var groups = new[]
        {
            Group(0, CombatTeam.Player, [10, 0, 0, 0]),
            Group(0, CombatTeam.Enemy, [11, 0, 0, 0]),
            Group(1, CombatTeam.Player, [0, 0, 0, 0]),
            Group(1, CombatTeam.Enemy, [0, 0, 0, 0])
        };
        var points = new BattleSpawnPoints(groups,
        [
            new UnitSpawnPointState(10, Point(0, CombatTeam.Player, 0), null),
            new UnitSpawnPointState(11, Point(0, CombatTeam.Enemy, 0), null)
        ]);
        var root = UnitAbilityChecks.Root();
        TrainCombatState train = CardSpellModel.WithContext(root.Spawn.Train,
            root.Spawn.Train.Context!.WithSpawnPoints(points));
        Require(UnitTeleportModel.CanPlay(train, 10, 0, 2), "A same-room destination after an empty point was rejected.");
        Require(!UnitTeleportModel.CanPlay(train, 10, 0, 1), "Selecting the actor's current effective slot was accepted.");
        Require(UnitTeleportModel.CanPlay(train, 10, 1, 3), "A cross-room destination was rejected.");
        Require(!UnitTeleportModel.CanPlay(train, 11, 0, 2), "An enemy actor was allowed to use player teleport destinations.");
        Require(!UnitTeleportModel.CanPlay(train, 10, 1, 4), "An out-of-range destination was accepted.");
        Require(UnitTeleportModel.TryGoalIndex(points, 10, 0, 2, out int goal) && goal == 1,
            "A free spawn point did not adjust the selected active-unit goal.");

        var world = new SpawnPointWorld(false,
        [Group(0, CombatTeam.Player, [10, 11, 0, 0])],
        [
            Occupant(10, Point(0, CombatTeam.Player, 0)),
            Occupant(11, Point(0, CombatTeam.Player, 1))
        ]);
        string parent = JsonSerializer.Serialize(world);
        SpawnPointResult moved = SpawnPointModel.Apply(world, "Swap", 0, CombatTeam.Player, index: 0, targetIndex: 2);
        Require(moved.Supported && moved.State!.Groups.Single().Occupants.SequenceEqual([0, 11, 10, 0]) &&
            moved.State.Point(10)?.Index == 2 && parent == JsonSerializer.Serialize(world),
            "Moving into an empty physical point compacted a hole or mutated the parent.");
        SpawnPointResult swapped = SpawnPointModel.Apply(world, "Swap", 0, CombatTeam.Player, index: 0, targetIndex: 1);
        Require(swapped.Supported && swapped.State!.Groups.Single().Occupants.SequenceEqual([11, 10, 0, 0]) &&
            swapped.State.Point(10)?.Index == 1 && swapped.State.Point(11)?.Index == 0,
            "Swapping occupied physical points lost their unit references.");
        Console.WriteLine("UNIT-TELEPORT-CHECKS PASS: same/cross-room legality, effective goal adjustment, physical holes and immutable swaps.");

        static SpawnPointGroupState Group(int room, CombatTeam team, int[] occupants) =>
            new(room, team, occupants.Length, occupants.Length, occupants, Enumerable.Repeat(false, occupants.Length).ToArray());
        static SpawnPointReference Point(int room, CombatTeam team, int index) => new(room, team, index);
        static SpawnPointOccupant Occupant(int id, SpawnPointReference point) =>
            new(id, 10, true, false, false, 0, false, false, point, null);
        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}

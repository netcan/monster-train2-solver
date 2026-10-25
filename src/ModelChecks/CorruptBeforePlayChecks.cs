using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CorruptBeforePlayChecks
{
    private const string Reason = "CorruptionAdded unit callbacks are not modeled.";
    internal static void Native(FixtureValue fixture)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        var last = rows[^1];
        var before = last.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var first = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 1).Units.Single(unit => unit.Id == 4);
        var second = after.Spawn.Train.Rooms.Single(room => room.RoomIndex == 1).Units.Single(unit => unit.Id == 4);
        var card = before.Spawn.Train.Context!.FindCard(3)!;
        Require(rows.Length == 4 && before.Spawn.Turn == 0 && after.Spawn.Turn == 0 &&
            !last.GetProperty("IsEndTurn").GetBoolean() &&
            card.DataId == "276fdd51-4bb8-4810-9bbf-cb663522b70f" &&
            card.Permanent.Upgrades.Any(upgrade => upgrade.Lifecycle?.AddedTraits.Any(trait =>
                trait.RuntimeType == CardCorruptStateModel.Trait) == true) &&
            first.Attack == 20 && second.Attack == 23 && first.Health == 30 && second.Health == 30 &&
            first.Status("armor") == null && second.Status("armor")?.Stacks == 3 &&
            after.Spawn.Train.Rooms.Single(room => room.RoomIndex == 1).Units.Any(unit => unit.SpawnerCardId == 3) &&
            before.Spawn.Train.Context.RoomCapacities!.Single(room => room.RoomIndex == 1).Corruption == 0 &&
            after.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 1).Corruption == 1 &&
            !first.Triggers.Single(trigger => trigger.Kind == "CorruptionAdded").HasTriggered &&
            second.Triggers.Single(trigger => trigger.Kind == "CorruptionAdded").HasTriggered == true,
            "Missing the native Corrupt-before-summon Carver upgrade boundary.");
        Console.WriteLine("CORRUPT-BEFORE-PLAY PASS:4 native inputs/full successors/RNG; upgraded Corrupt queues Carver's +3 attack before summon; immutable parents.");
    }
    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        Native(source); Native(replay);
        NativeBranchObservationChecks.Pair(source, replay);
        Console.WriteLine("CORRUPT-BEFORE-PLAY-PAIR PASS:original rejection and independent repaired forecast preserved.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

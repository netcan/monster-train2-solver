using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class SlayCorruptionCallbackChecks
{
    private const string Reason = "CorruptionAdded unit callbacks are not modeled.";
    internal static void Native(FixtureValue fixture)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        var last = rows[^1];
        var before = last.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var card = before.Spawn.Train.Context!.FindCard(10)!;
        var carver = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == 1).Units.Single(unit => unit.Id == 4);
        var changed = after.Spawn.Train.Rooms.Single(room => room.RoomIndex == 1).Units.Single(unit => unit.Id == 4);
        var action = last.GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(rows.Length == 12 && before.Spawn.Turn == 2 && after.Spawn.Turn == 2 &&
            !last.GetProperty("IsEndTurn").GetBoolean() && action.CardInstanceId == 10 && action.RoomIndex == 1 &&
            action.TargetUnitId == 6 && card.DataId == "ae114dc4-d08f-4909-a2ad-d99215661463" &&
            card.MaskDescriptor!.KillTriggers!.Any(trigger => trigger.Actions.Single() is
                { Type: "AdjustCorruption", Target: "Room", CorruptionAdd: true, Value: 2 }) &&
            carver.AssetKey == "MonsterShardsoulCarver" && carver.Attack == 29 && changed.Attack == 38 &&
            carver.Health == 30 && changed.Health == 30 && carver.Status("armor")?.Stacks == 9 &&
            changed.Status("armor")?.Stacks == 18 &&
            before.Spawn.Train.Context.RoomCapacities!.Single(room => room.RoomIndex == 1).Corruption == 3 &&
            after.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 1).Corruption == 5 &&
            after.Spawn.Train.Rooms.SelectMany(room => room.Units).All(unit => unit.Id != 6),
            "Missing the native restricted Corrupt damage/Slay/Carver boundary.");
        Console.WriteLine("SLAY-CORRUPTION-CALLBACK PASS:12 real inputs/full successors/forecasts/RNG; Carver29->38 attack and9->18 armor; room3->5 corruption; retained lethal victim; immutable parents.");
    }
    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        Native(source); Native(replay);
        NativeBranchObservationChecks.Pair(source, replay);
        Console.WriteLine("SLAY-CORRUPTION-CALLBACK-PAIR PASS: original rejection and independent repaired forecast preserved.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

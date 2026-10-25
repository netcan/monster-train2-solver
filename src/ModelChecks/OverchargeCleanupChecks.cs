using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OverchargeCleanupChecks
{
    private const string Reason = "Rolling native state differs after decision 33: $.Context.RoomCapacities[2].Corruption";
    internal static void Native(FixtureValue fixture)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason, HistoricalMismatch, RoomOutcome.BattleWon);
        var last = rows[^1];
        var before = last.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        RoomCapacityState Room(BattleTurnState state) => state.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 2);
        Require(rows.Length == 33 && before.Spawn.Turn == 8 && last.GetProperty("IsEndTurn").GetBoolean() &&
            Room(before).Corruption == 6 && Room(before).MaxCorruption == 4 && Room(after).Corruption == 4 &&
            after.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health == 49,
            "Missing the natural overcharged room completion and Pyre49 terminal boundary.");
        Console.WriteLine("OVERCHARGE-CLEANUP PASS:33 native inputs/full successors/RNG, room2 overcharge6 to4 before Pyre49 victory; component observation.");
    }
    private static void HistoricalMismatch(BattleTurnState old, BattleTurnState actual)
    {
        JsonNode projected = JsonNode.Parse(NativeBranchObservationChecks.Key(old, true))!;
        JsonNode observed = JsonNode.Parse(NativeBranchObservationChecks.Key(actual, true))!;
        JsonNode capacity = projected["Context"]!["RoomCapacities"]![2]!;
        Require(capacity["Corruption"]!.GetValue<int>() == 6 &&
            observed["Context"]!["RoomCapacities"]![2]!["Corruption"]!.GetValue<int>() == 4,
            "Preserve the original overcharge mismatch.");
        capacity["Corruption"] = 4;
        Require(JsonNode.DeepEquals(projected, observed), "Historical mismatch extends beyond room2 corruption.");
    }
    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        Native(source); Native(replay);
        NativeBranchObservationChecks.Pair(source, replay, originalPredictionPresent: true);
        Console.WriteLine("OVERCHARGE-CLEANUP-PAIR PASS:original mismatch and independent repaired terminal forecast preserved.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

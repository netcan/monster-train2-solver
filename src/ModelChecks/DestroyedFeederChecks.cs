using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class DestroyedFeederChecks
{
    private const string Reason = "Rolling native state differs after decision 12: $.Rooms[2].Units[0].LastFeederId";
    internal static void Native(FixtureValue fixture)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason, HistoricalMismatch);
        var last = rows[^1];
        var before = last.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(rows.Length == 12 && last.GetProperty("IsEndTurn").GetBoolean() &&
            before.Spawn.Turn == 2 && actual.Spawn.Turn == 3 && before.CanonicalDecisionReferences &&
            before.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == 6).LastFeederId == 5 &&
            before.Spawn.Train.Rooms[1].Units.Any(unit => unit.Id == 5 && unit.Health == 50) &&
            actual.Spawn.Train.Rooms[2].Units.Single(unit => unit.Id == 6).LastFeederId == 0 &&
            actual.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == 9).LastFeederId == 10 &&
            actual.Spawn.Train.Rooms[1].Units.Any(unit => unit.Id == 10 && unit.Health > 0) &&
            !actual.Spawn.Train.Rooms.Any(room => room.Units.Any(unit => unit.Id == 5)),
            "Missing the destroyed feeder reference and living-feeder control.");
        Console.WriteLine("DESTROYED-FEEDER PASS:12 native inputs/full successors/RNG; surviving food drops destroyed feeder5 at frame completion, living feeder10 preserved; immutable parents.");
    }
    private static void HistoricalMismatch(BattleTurnState old, BattleTurnState actual)
    {
        JsonNode projected = JsonNode.Parse(NativeBranchObservationChecks.Key(old))!;
        JsonNode observed = JsonNode.Parse(NativeBranchObservationChecks.Key(actual))!;
        foreach (JsonNode unit in new[] {projected["Rooms"]![2]!["Units"]![0]!,
            projected["Context"]!["Enchantments"]!["Rooms"]![2]!["Units"]![0]!})
        {
            Require(unit["Id"]!.GetValue<int>() == 6 && unit["LastFeederId"]!.GetValue<int>() == 5,
                "Preserve the original dangling feeder5.");
            unit["LastFeederId"] = 0;
        }
        Require(JsonNode.DeepEquals(projected, observed), "Historical mismatch extends beyond food6's feeder reference caches.");
    }
    internal static void Pair(FixtureValue source, FixtureValue replay, FixtureValue raw)
    {
        Native(source); Native(replay);
        NativeBranchObservationChecks.Pair(source, replay, originalPredictionPresent: true);
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(raw.GetProperty(counter).GetInt32() == 0, "Raw feeder component has nonzero " + counter);
        var stage = raw.GetProperty("Stages").EnumerateArray().Single(row =>
            row.GetProperty("Kind").GetString() == "Room" && row.GetProperty("Turn").GetInt32() == 2 &&
            row.GetProperty("Before").GetProperty("RoomIndex").GetInt32() == 1);
        var before = stage.GetProperty("Before").Deserialize<RoomCombatState>()!;
        var actual = stage.GetProperty("Actual").Deserialize<RoomCombatState>()!;
        string Key(RoomCombatState state) => JsonSerializer.Serialize(new { state.RoomIndex, state.Deployment,
            state.Units, Context = BattleTurnChecks.ComparableContext(state.Context), state.Preview,
            state.ActiveCompanionBossIds }, ModelJson.Options);
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        var result = RoomCombatModel.Resolve(before);
        Require(result.Supported && Key(result.State!) == Key(actual) &&
            actual.Units.Single(unit => unit.Id == 6).LastFeederId == 5 &&
            !actual.Units.Any(unit => unit.Id == 5), "Raw room must preserve feeder5 until frame destruction.");
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Raw feeder replay mutated its parent.");
        Console.WriteLine("DESTROYED-FEEDER-PAIR PASS:original stale reference, repaired native forecast and raw pre-destruction reference timing preserved.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

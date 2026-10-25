using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UnstableRevivalChecks
{
    private const string Reason = "Unstable explosion is missing during Undying revival.";
    internal static void Native(FixtureValue fixture)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason, Historical);
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var unstable = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 49);
        Require(rows.Length == 31 && rows[^1].GetProperty("IsEndTurn").GetBoolean() &&
            unstable.AssetKey == "MonsterLowRankedAssistant" && unstable.Status("unstable")?.Stacks == 4 &&
            unstable.Status("undying")?.Stacks == 1 && before.Spawn.Turn == 4 && actual.Spawn.Turn == 5 &&
            Unit(actual, 45).Health == 28 && Unit(actual, 8).Health == 343,
            "Missing the revived assistant's four-point explosion against both native enemies.");
        Console.WriteLine("UNSTABLE-REVIVAL PASS:31 native inputs/full successors/RNG; Undying death signal includes Unstable explosion; immutable parents.");
    }
    private static CombatUnit Unit(BattleTurnState state, int id) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
    private static void Historical(BattleTurnState predicted, BattleTurnState actual)
    {
        Require(Unit(predicted, 45).Health - Unit(actual, 45).Health == 4 && Unit(predicted, 8).Health - Unit(actual, 8).Health == 4,
            "Wrong historical missing Unstable revival explosion.");
    }
    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        Native(source); Native(replay);
        NativeBranchObservationChecks.Pair(source, replay, originalPredictionPresent: true);
        Console.WriteLine("UNSTABLE-REVIVAL-PAIR PASS:original health mismatch and independent repaired forecast preserved.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

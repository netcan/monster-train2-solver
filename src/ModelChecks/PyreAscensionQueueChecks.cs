using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PyreAscensionQueueChecks
{
    private const string Reason = "Native floor-transition relic damage after Pyre death.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        Check(source, true);
        if (replay != null)
        {
            Check(replay.Value, false);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true);
        }
        Console.WriteLine("PYRE-ASCENSION-QUEUE PASS: 29 full native inputs/state/forecasts/RNG; Pyre death stops lower-floor movement damage after physical reservations;" +
            (replay == null ? " independent replay pending." : " independent corrected native pair."));
    }
    private static void Check(FixtureValue fixture, bool historical)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason, historical ? HistoricalMismatch : null,
            boundaryOutcome: RoomOutcome.PlayerDefeated);
        Require(rows.Length == 29 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing complete terminal EndTurn prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(before.Spawn.Turn == 5 && Actor(before, 1).Health == 30 &&
            after.Spawn.Train.Context!.EnergyState!.PyreAlive == false &&
            after.Spawn.Train.Rooms.SelectMany(room => room.Units).All(unit => !unit.IsPyre) &&
            Actor(before, 21).Health == 325 && Actor(before, 22).Health == 110 &&
            Actor(after, 21).Health == 311 && Actor(after, 22).Health == 103 &&
            new[] { 21, 22 }.All(id => Room(before, id) == 0 && Room(after, id) == 1),
            "Missing native physical movement without lower-floor damage after Pyre death.");
    }
    private static void HistoricalMismatch(BattleTurnState historical, BattleTurnState actual) =>
        Require(Actor(historical, 21).Health == 308 && Actor(actual, 21).Health == 311 &&
            Actor(historical, 22).Health == 100 && Actor(actual, 22).Health == 103,
            "Historical extra floor-transition damage was replaced.");
    private static CombatUnit Actor(BattleTurnState state, int id) => state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
    private static int Room(BattleTurnState state, int id) => state.Spawn.Train.Rooms.Single(room => room.Units.Any(unit => unit.Id == id)).RoomIndex;
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

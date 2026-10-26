using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TerminalCoroutinePlaneChecks
{
    internal static void Pair(FixtureValue counterexample, FixtureValue source, FixtureValue replay)
    {
        Require(counterexample.GetProperty("NativeWon").GetBoolean() && counterexample.GetProperty("Mismatches").GetInt32() == 2 &&
            new[] { "CaptureFailures", "Unsupported", "Pending" }.All(key => counterexample.GetProperty(key).GetInt32() == 0),
            "Missing the original two intermediate-stage capture mismatches.");
        RollingAdviceChecks.ObservedSuccessors(counterexample);
        var oldStage = counterexample.GetProperty("Stages").EnumerateArray().Single(row => row.GetProperty("Index").GetInt32() == 37);
        var oldExpected = oldStage.GetProperty("Predicted").GetProperty("State").Deserialize<RoomCombatState>()!;
        var oldActual = oldStage.GetProperty("Actual").Deserialize<RoomCombatState>()!;
        Require(oldStage.GetProperty("Difference").GetString() == "Room context differs: $.Context.SpawnPoints.Units[3].LastKnown" &&
            Point(oldExpected.Context!.SpawnPoints!).LastKnown is { RoomIndex: 0, Index: 1 } &&
            Point(oldActual.Context!.SpawnPoints!).LastKnown == null &&
            counterexample.GetProperty("DecisionSpawnPoints").EnumerateArray().Any(sample => sample.GetProperty("Terminal").GetBoolean() &&
                Point(sample.GetProperty("Raw").Deserialize<BattleSpawnPoints>()!).LastKnown is { RoomIndex: 0, Index: 1 } &&
                Point(sample.GetProperty("Canonical").Deserialize<BattleSpawnPoints>()!).LastKnown == null),
            "The native raw plane must prove that terminal coroutine capture normalized the pointer too early.");
        Require(counterexample.GetProperty("NativeRunInput").ContentEquals(source.GetProperty("NativeRunInput")),
            "Changed the native campaign checkpoint.");
        foreach (var fixture in new[] { source, replay })
        {
            var stage = fixture.GetProperty("Stages").EnumerateArray().Single(row => row.GetProperty("Index").GetInt32() == 37);
            var actual = stage.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            Require(stage.GetProperty("Difference").ValueKind == FixtureKind.Null &&
                Point(actual.Context!.SpawnPoints!).LastKnown is { RoomIndex: 0, Index: 1 } &&
                actual.Context.Enchantments!.RetainedUnits.All(actor => actor.Unit.Id != 4),
                "A terminal room iterator must preserve its native owner pointer while retiring the no-longer-owned corpse.");
            var samples = fixture.GetProperty("DecisionSpawnPoints").EnumerateArray().Where(sample =>
                sample.GetProperty("Terminal").GetBoolean() && !sample.GetProperty("CanonicalDecision").GetBoolean()).ToArray();
            Require(samples.Any(sample => Point(sample.GetProperty("Raw").Deserialize<BattleSpawnPoints>()!).LastKnown != null &&
                Point(sample.GetProperty("Canonical").Deserialize<BattleSpawnPoints>()!).LastKnown != null),
                "No independent raw coroutine plane was captured.");
            var final = fixture.GetProperty("RollingDecisions").EnumerateArray().Last().GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Require(Point(final.Spawn.Train.Context!.SpawnPoints!).LastKnown == null &&
                final.Spawn.Train.Context.Enchantments!.RetainedUnits.All(actor => actor.Unit.Id != 4),
                "The later stable decision must still retire the corpse and its owner pointers.");
        }
        RandomCampaignChecks.Pair(source, replay, 934692550, 0, 1, "Underlegion", "LunaCoven", 10000);
        Console.WriteLine("TERMINAL-COROUTINE-PLANES PASS: original two capture mismatches preserved, native raw/decision ownership separated, " +
            "independent whole-battle wins and complete room/train/decision states/forecasts/RNG.");
    }
    private static UnitSpawnPointState Point(BattleSpawnPoints points) => points.Units.Single(unit => unit.UnitId == 4);
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

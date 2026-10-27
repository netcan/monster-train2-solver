using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RelentlessCompanionBumpChecks
{
    internal const string Reason = "Relentless companion boss Hexcalation differs from native forced movement.";

    internal static void Counterexample(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Mismatches").GetInt32() == 1 &&
            new[] { "CaptureFailures", "Unsupported", "Pending" }.All(key => fixture.GetProperty(key).GetInt32() == 0),
            "Preserve the sole original companion/outer-boss movement mismatch.");
        var rows = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(rows.Length == 63, "Missing62-input native prefix and63rd Hexcalation.");
        var row = rows[^1];
        var before = row.GetProperty("Root").Deserialize<BattleTurnState>()!;
        var historical = row.GetProperty("Expected").Deserialize<BattleTurnState>()!;
        var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
        Historical(historical, actual); Witness(before, action, actual);
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        string expected = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == expected,
                "Blocked companion movement differs from complete native successor/forecast/RNG.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Blocked companion movement mutated its parent.");
        var automatic = rows[52];
        var automaticBefore = automatic.GetProperty("Root").Deserialize<BattleTurnState>()!;
        var automaticActual = automatic.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(automatic.GetProperty("IsEndTurn").GetBoolean() && automaticBefore.Spawn.Turn == 7 &&
            automaticActual.Spawn.Turn == 8 && automaticActual.Spawn.Train.Rooms[0].Units.Any(unit => unit.Id == 11),
            "Lost native companion/controller movement before the paid-card boundary.");
        string automaticParent = JsonSerializer.Serialize(automaticBefore, ModelJson.Options);
        string automaticExpected = NativeBranchObservationChecks.Key(automaticActual);
        Parallel.For(0, 4, index =>
        {
            var result = BattlePlannerChecks.Replay(automaticBefore, [new BattleSearchStep(null, true)], out var outcome);
            Require(outcome == RoomOutcome.Exchanged && NativeBranchObservationChecks.Key(result) == automaticExpected,
                "Card movement restriction incorrectly blocked automatic boss movement.");
        });
        Require(automaticParent == JsonSerializer.Serialize(automaticBefore, ModelJson.Options), "Automatic boss movement mutated its parent.");
        Console.WriteLine("RELENTLESS-COMPANION-BUMP COUNTEREXAMPLE PASS:53rd automatic movement and63rd blocked card, full successor/forecast/RNG and eight immutable branches.");
    }

    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason, Historical);
        Require(rows.Length == 63, "Incomplete63-input companion movement observation.");
        Witness(rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!,
            rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!,
            rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!);
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true);
        }
        Console.WriteLine("RELENTLESS-COMPANION-BUMP NATIVE PASS:63 complete inputs/forecasts/RNG; independentPair=" + replay.HasValue);
    }

    private static void Witness(BattleTurnState before, PlayCardAction action, BattleTurnState actual)
    {
        var boss = before.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == 11);
        Require(before.Spawn.Turn == 9 && action is { CardInstanceId: 13, RoomIndex: 1, TargetUnitId: 11 } &&
            before.Spawn.Train.Context!.FindCard(13)!.DataId == "d67c52be-d7bb-4291-a716-31408e4ced60" &&
            boss.BumpRules is { OuterBoss: true, CompanionBoss: true, PairedBossUnitId: 9 } &&
            boss.Status("relentless") != null && actual.Spawn.Train.Rooms[1].Units.Any(unit => unit.Id == 11) &&
            actual.Spawn.Train.Rooms[2].Units.All(unit => unit.Id != 11), "Lost original paired relentless companion/outer-boss refusal.");
    }

    private static void Historical(BattleTurnState historical, BattleTurnState actual) => Require(
        historical.Spawn.Train.Rooms[2].Units.Any(unit => unit.Id == 11) &&
        actual.Spawn.Train.Rooms[1].Units.Any(unit => unit.Id == 11), "Lost original invalid floor1-to2 boss move.");
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

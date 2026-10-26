using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TerminalCompanionSlotChecks
{
    internal const string Reason = "Observed terminal companion Pyre outside slot persists after native victory.";
    internal static void Native(string path, bool historical = true)
    {
        using var document = ModelJson.ReadFixture(path);
        var fixture = document.RootElement;
        Require(fixture.GetProperty("NativeWon").GetBoolean() && fixture.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            fixture.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop", "Missing native settled victory.");
        foreach (string counter in new[] { "CaptureFailures", "Unsupported", "Pending" })
            Require(fixture.GetProperty(counter).GetInt32() == 0, "Incomplete companion evidence: " + counter);
        Require(fixture.GetProperty("Mismatches").GetInt32() == (historical ? 3 : 0), "Unexpected historical/clean mismatch count.");
        var rows = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(rows.Length == 59 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Wrong original winning route.");
        var before = rows[^1].GetProperty("Root").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var expected = rows[^1].GetProperty("Expected").Deserialize<BattleTurnState>()!;
        Require(before.Spawn.Turn == 10 && actual.Spawn.Turn == 11 &&
            before.Spawn.Train.Rooms[2].Units.Single(unit => unit.Id == 10) is
                { AssetKey: "Boss_Level6_AngelDuo_Entropic_Variant3_HarvestHealth", Health: 282 } &&
            actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health == 4,
            "Missing the original paired Boss/Pyre boundary.");
        var points = actual.Spawn.Train.Context!.SpawnPoints!;
        Require(points.Group(3, CombatTeam.Enemy)!.Outside[7] && points.Group(3, CombatTeam.Enemy)!.Occupants[7] == 0 &&
            points.Units.Single(unit => unit.UnitId == 10) is { Current: null, LastKnown: null }, "Native companion anchor was not retired.");
        if (historical)
        {
            var old = expected.Spawn.Train.Context!.SpawnPoints!.Group(3, CombatTeam.Enemy)!;
            Require(old.Outside[7] && old.Occupants[7] == 10, "Preserve the original stale outside-slot forecast.");
            var projected = JsonSerializer.SerializeToNode(expected, ModelJson.Options)!;
            projected["Spawn"]!["Train"]!["Context"]!["SpawnPoints"]!["Groups"]![6]!["Occupants"]![7] = 0;
            Require(Key(projected.Deserialize<BattleTurnState>(ModelJson.Options)!, true) == Key(actual, true),
                "Historical terminal mismatch includes another field.");
        }
        else Require(Key(expected, true) == Key(actual, true), "Clean forecast differs from native victory.");
        RollingAdviceChecks.ObservedSuccessors(fixture);
        RoomCombatChecks.NativeRoomOnly(path);
        TrainCombatChecks.Native(fixture);
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && result.Outcome == RoomOutcome.BattleWon && Key(result.State!, true) == Key(actual, true) &&
                JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Terminal anchor cleanup changed full state/RNG or parent.");
        });
        Console.WriteLine("TERMINAL-COMPANION-SLOT PASS:59 native inputs/full room/train states/RNG, dead outside anchor retired, Pyre4 and four immutable terminal branches; historical=" + historical);
    }

    internal static void Pair(string sourcePath, string replayPath)
    {
        Require(Path.GetFullPath(sourcePath) != Path.GetFullPath(replayPath), "Independent archives required.");
        Native(sourcePath); Native(replayPath, historical: false);
        using var source = ModelJson.ReadFixture(sourcePath);
        using var replay = ModelJson.ReadFixture(replayPath);
        var first = source.RootElement.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        var second = replay.RootElement.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        for (int i = 0; i < first.Length; i++)
        {
            foreach (string field in new[] { "Action", "IsEndTurn" })
                Require(first[i].GetProperty(field).ContentEquals(second[i].GetProperty(field)), "Independent input changed at " + i);
            foreach (string field in new[] { "Root", "Actual" })
                Require(Key(first[i].GetProperty(field).Deserialize<BattleTurnState>()!, i == first.Length - 1 && field == "Actual") ==
                    Key(second[i].GetProperty(field).Deserialize<BattleTurnState>()!, i == first.Length - 1 && field == "Actual"),
                    "Independent native full state/RNG changed at " + i);
        }
        Console.WriteLine("TERMINAL-COMPANION-SLOT-PAIR PASS:59 identical actual native inputs/successors; original stale-slot forecast and clean independent victory preserved.");
    }

    internal static void Observation(FixtureValue fixture)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason, (historical, actual) =>
        {
            Require(historical.Spawn.Train.Context!.SpawnPoints!.Group(3, CombatTeam.Enemy)!.Occupants[7] == 10 &&
                actual.Spawn.Train.Context!.SpawnPoints!.Group(3, CombatTeam.Enemy)!.Occupants[7] == 0,
                "Missing the isolated original outside-slot counterexample.");
            var projected = JsonSerializer.SerializeToNode(historical, ModelJson.Options)!;
            projected["Spawn"]!["Train"]!["Context"]!["SpawnPoints"]!["Groups"]![6]!["Occupants"]![7] = 0;
            Require(Key(projected.Deserialize<BattleTurnState>(ModelJson.Options)!, true) == Key(actual, true),
                "Isolated historical discrepancy includes another field.");
        }, boundaryOutcome: RoomOutcome.BattleWon);
        Require(rows.Length == 59, "Incomplete isolated terminal native chain.");
        Console.WriteLine("TERMINAL-COMPANION-SLOT-OBSERVATION PASS:59 original native inputs/full state/RNG; isolated stale outside-slot forecast preserved.");
    }
    private static string Key(BattleTurnState state, bool terminal = false) => NativeBranchObservationChecks.Key(state, terminal);
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

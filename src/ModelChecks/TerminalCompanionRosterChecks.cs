using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TerminalCompanionRosterChecks
{
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue fixture = document.RootElement;
        Require(fixture.GetProperty("NativeWon").GetBoolean() &&
            fixture.GetProperty("Mismatches").GetInt32() == 2 &&
            fixture.GetProperty("CaptureFailures").GetInt32() == 0 &&
            fixture.GetProperty("Unsupported").GetInt32() == 0 && fixture.GetProperty("Pending").GetInt32() == 0 &&
            fixture.GetProperty("Stages").GetArrayLength() == 91 && fixture.GetProperty("TrainPhases").GetArrayLength() == 22,
            "Preserve the original terminal room/turn counterexample.");
        FixtureValue[] rows = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(rows.Length == 35 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Wrong native winning route.");
        BattleTurnState before = rows[^1].GetProperty("Root").Deserialize<BattleTurnState>()!;
        BattleTurnState old = rows[^1].GetProperty("Expected").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit historical = old.Spawn.Train.Context!.Enchantments!.RetainedUnits.Single(actor => actor.Unit.Id == 6).Unit;
        CombatUnit native = actual.Spawn.Train.Context!.Enchantments!.RetainedUnits.Single(actor => actor.Unit.Id == 6).Unit;
        Require(before.Spawn.Turn == 10 && actual.Spawn.Turn == 11 &&
            historical.Health == 0 && !historical.EndsBattleOnDeath && historical.BumpRules!.PairedBossUnitId == 9 &&
            native.Health == 0 && native.EndsBattleOnDeath && native.BumpRules!.PairedBossUnitId == 0 &&
            actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health == 30,
            "Missing the dead companion's dynamic terminal pairing.");
        var repaired = JsonSerializer.SerializeToNode(old, ModelJson.Options)!;
        var retained = repaired["Spawn"]!["Train"]!["Context"]!["Enchantments"]!["RetainedUnits"]!.AsArray();
        var unit = retained.Single(actor => actor!["Unit"]!["Id"]!.GetValue<int>() == 6)!["Unit"]!;
        unit["EndsBattleOnDeath"] = true;
        unit["BumpRules"]!["PairedBossUnitId"] = 0;
        Require(Key(repaired.Deserialize<BattleTurnState>(ModelJson.Options)!) == Key(actual),
            "The historical terminal discrepancy includes another field.");
        RollingAdviceChecks.ObservedSuccessors(fixture);
        RoomCombatChecks.NativeRoomOnly(path);
        TrainCombatChecks.Native(fixture);
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 8, _ =>
        {
            BattleTurnResult result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && result.Outcome == RoomOutcome.BattleWon && Key(result.State!) == Key(actual),
                "Concurrent terminal companion replay differs from the native win.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Terminal companion cleanup mutated its parent.");
        Console.WriteLine("TERMINAL-COMPANION-ROSTER PASS:35 native inputs,91 room stages,22 train phases/full state/RNG; original two-field counterexample preserved; eight immutable winning branches; clean native pair required separately.");
    }
    private static string Key(BattleTurnState state) => BattleTurnChecks.Comparable(state, terminal: true);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

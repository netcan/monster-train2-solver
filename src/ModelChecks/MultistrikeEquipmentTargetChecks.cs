using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MultistrikeEquipmentTargetChecks
{
    private const string AlternativeReason = "Resin Blades Multistrike target preference requires native alternative verification.";
    internal static void Alternative(FixtureValue source)
    {
        var rows = NativeBranchObservationChecks.Replay(source, AlternativeReason);
        Require(rows.Length == 54, "Missing53 unchanged native prefix inputs and the alternative equipment target.");
        var row = rows[^1];
        var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
        var host = actual.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == 4);
        var chosen = BattleTacticalPolicy.Choose(before, out _, 2);
        Require(before.Spawn.Turn == 8 && action is { CardInstanceId: 36, RoomIndex: 1, TargetUnitId: 4 } &&
            host.Attack == 24 && host.Attacks == 2 && chosen?.Action.CardInstanceId == 36 &&
            chosen.Action.TargetUnitId == 4 && chosen.Action.RoomIndex == 1,
            "The native24x2 alternative and tactical preference disagree.");
        Console.WriteLine("MULTISTRIKE-EQUIPMENT-ALTERNATIVE NATIVE PASS:54 unchanged complete inputs/forecasts/RNG, actual24x2 versus recorded8x2; component strategy evidence, not a battle win or independent pair.");
    }

    internal static void Native(FixtureValue source)
    {
        Require(!source.GetProperty("NativeWon").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(key => source.GetProperty(key).GetInt32() == 0),
            "Missing accepted native strategy counterexample.");
        var row = source.GetProperty("RollingDecisions").EnumerateArray().ElementAt(53);
        var before = row.GetProperty("Root").Deserialize<BattleTurnState>()!;
        var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(before.Spawn.Turn == 8 && action is { CardInstanceId: 36, TargetUnitId: 8 } &&
            before.Spawn.Train.Context!.FindCard(36)!.DataId == "1d2e6259-e3ef-4a6a-8894-2c12db9fd4a9",
            "Lost native turn8 Resin Blades on the8ATK steward.");
        var oldHost = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 8);
        Require(oldHost.Attack == 8 && oldHost.Attacks == 2, "Lost native8x2 equipment result.");
        using var keys = new BattleStateKeys();
        string parent = keys.Key(before);
        var recorded = BattleActionModel.PlayCard(before, action);
        Require(recorded.Supported && NativeBranchObservationChecks.Key(recorded.State!) == NativeBranchObservationChecks.Key(actual),
            "Recorded Multistrike equipment differs from native successor/forecast/RNG.");
        var selected = BattleTacticalPolicy.Choose(before, out _, 2);
        Console.WriteLine("MULTISTRIKE-EQUIPMENT-CHOICE card=" + selected?.Action.CardInstanceId + " target=" + selected?.Action.TargetUnitId);
        Require(selected?.Action is { CardInstanceId: 36, TargetUnitId: 4 }, "Pure Multistrike still ignores the24ATK legal host.");
        var newHost = selected.Result.State!.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 4);
        Require(newHost.Attack == 24 && newHost.Attacks == 2, "Preferred legal host lost modeled24x2.");
        Parallel.For(0, 4, index =>
        {
            var choice = BattleTacticalPolicy.Choose(before, out _, 2);
            Require(choice?.Action is { CardInstanceId: 36, TargetUnitId: 4 },
                "Parallel Multistrike ranking changed its native parent or target.");
        });
        Require(keys.Key(before) == parent, "Multistrike ranking mutated its native parent.");
        Console.WriteLine("MULTISTRIKE-EQUIPMENT-TARGET PASS: native8x2 counterexample/full successor/RNG, legal24x2 preference and four immutable choices; strategy evidence, native alternative still required.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

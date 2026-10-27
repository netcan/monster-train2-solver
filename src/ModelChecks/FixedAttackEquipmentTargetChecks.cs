using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class FixedAttackEquipmentTargetChecks
{
    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("NativeWon").GetBoolean(), "Missing the native winning equipment route.");
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(fixture.GetProperty(counter).GetInt32() == 0, "Unaccepted equipment route: " + counter);
        var row = fixture.GetProperty("RollingDecisions").EnumerateArray().ElementAt(14);
        var before = row.GetProperty("Root").Deserialize<BattleTurnState>()!;
        var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
        var units = before.Spawn.Train.Rooms.SelectMany(room => room.Units).ToArray();
        var host = units.Single(unit => unit.Id == action.TargetUnitId);
        var champion = units.Single(unit => unit.Modifiers?.IsChampion == true);
        Require(before.Spawn.Turn == 2 && action.CardInstanceId == 23 && host.Id == 11 &&
            host.Attack == 6 && host.Attacks == 2 && champion.Attack == 45 &&
            before.Spawn.Train.Context!.FindCard(23)!.DataId == "17fa5800-9c48-41f5-be97-86db8d4c5f9e",
            "Missing the natural fixed-attack/multistrike equipment decision.");
        var equipped = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == host.Id);
        Require(equipped.Attack == 30 && equipped.Attacks == 2 && equipped.Status("sweep")?.Stacks == 1 &&
            equipped.EquipmentCards!.Contains(23), "The native equipment successor lost its fixed attack or sweep.");
        using var keys = new BattleStateKeys();
        string parent = keys.Key(before);
        var recorded = BattleActionModel.PlayCard(before, action);
        Require(recorded.Supported && BattleTurnChecks.Comparable(recorded.State!) == BattleTurnChecks.Comparable(actual),
            "Recorded equipment effects differ from the complete native successor/RNG.");
        var selected = BattleTacticalPolicy.Choose(before, out var omitted, 0);
        Console.WriteLine($"FIXED-ATTACK-EQUIPMENT-CHOICE card={selected?.Action.CardInstanceId} target={selected?.Action.TargetUnitId}");
        Require(selected?.Action.CardInstanceId == 23 && selected.Action.TargetUnitId == host.Id,
            "The tactical seed still lowers the champion from45 to30 instead of raising the two-strike host from6 to30.");
        Parallel.For(0, 4, index =>
        {
            var choice = BattleTacticalPolicy.Choose(before, out _, 0);
            Require(choice?.Action.CardInstanceId == 23 && choice.Action.TargetUnitId == host.Id,
                "Parallel equipment choices disagree.");
        });
        Require(keys.Key(before) == parent, "Equipment target ranking mutated the native parent.");
        Console.WriteLine("FIXED-ATTACK-EQUIPMENT-TARGET PASS: native6x2 to30x2/sweep, full successor/RNG, useful target over45ATK champion and four immutable parallel choices; strategy evidence, not a new default-budget victory.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

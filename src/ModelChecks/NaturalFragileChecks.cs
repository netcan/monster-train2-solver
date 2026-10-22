using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class NaturalFragileChecks
{
    internal static void Native(FixtureValue fixture)
    {
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(fixture.GetProperty(counter).GetInt32() == 0, "Natural Fragile capture has nonzero " + counter);
        Require(fixture.GetProperty("BattleDistance").GetInt32() == 7 &&
            fixture.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            string.IsNullOrEmpty(fixture.GetProperty("ModifierScenario").GetString()), "Expected a settled, unmodified natural final encounter.");
        var turns = fixture.GetProperty("Turns").EnumerateArray().ToArray();
        var actions = fixture.GetProperty("Actions");
        var root = (actions.GetArrayLength() > 0 ? actions[0] : turns[0]).GetProperty("Before").Deserialize<BattleTurnState>()!;
        Require(root.Spawn.Turn == 0 && root.Spawn.QueuedOuterBoss?.Boss.AssetKey == "Boss_Level8_Fragile" &&
            root.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health == 62,
            "Missing the natural Fragile deployment root and unchanged Pyre62.");
        if (actions.GetArrayLength() == 0)
        {
            Require(turns.Length == 5 && !fixture.GetProperty("NativeWon").GetBoolean(), "Unexpected no-card baseline.");
            BattleTurnChecks.Native(fixture);
        }
        else
        {
            Require(actions.GetArrayLength() == 39 && turns.Length == 7 && !fixture.GetProperty("NativeWon").GetBoolean(),
                "Unexpected natural triggered-status calibration route.");
            RollingAdviceChecks.Native(fixture);
            var boundary = turns.Single(turn => turn.GetProperty("Before").Deserialize<BattleTurnState>()!.Spawn.Turn == 3);
            var before = boundary.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var after = boundary.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            CombatUnit pyre = after.Spawn.Train.Rooms[3].Units.Single(unit => unit.IsPyre);
            Require(pyre.Health > 0 && after.Spawn.Train.Context!.Enchantments!.Rooms[3].Units
                .Single(unit => unit.IsPyre).Health == pyre.Health &&
                after.Spawn.Train.Context.Enchantments.Rooms.Take(3).All(room => room.Units.All(unit => !unit.IsPyre)),
                "An unrelated remote Pyre was moved or lost during local triggered-status synchronization.");
            string immutable = BattlePlannerChecks.Key(before);
            var first = BattleTurnModel.EndTurn(before);
            var second = BattleTurnModel.EndTurn(before);
            Require(first.Supported && second.Supported && BattlePlannerChecks.Key(before) == immutable &&
                BattleTurnChecks.Comparable(first.State!) == BattleTurnChecks.Comparable(after) &&
                BattleTurnChecks.Comparable(second.State!) == BattleTurnChecks.Comparable(after),
                "The affected native boundary failed independent immutable replay.");
        }
        Console.WriteLine($"NATURAL-FRAGILE PASS plays={actions.GetArrayLength()} turns={turns.Length}: unchanged natural root, complete state/RNG and physical Pyre retained; victory acceptance remains separate.");
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

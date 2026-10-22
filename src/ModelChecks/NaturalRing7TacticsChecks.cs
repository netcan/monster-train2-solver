using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class NaturalRing7TacticsChecks
{
    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("BattleDistance").GetInt32() == 6 && fixture.GetProperty("NativeWon").GetBoolean(),
            "Expected the natural seventh-battle victory.");
        RollingAdviceChecks.Native(fixture);
        var rows = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        var states = rows.Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!).ToArray();
        Require(rows.Length == 67 && fixture.GetProperty("Actions").GetArrayLength() == 57 &&
            fixture.GetProperty("Turns").GetArrayLength() == 10 &&
            states.Any(state => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Any(unit => unit.AssetKey == "Boss_Level7_Rax" && unit.IsBoss == true)) &&
            states[^1].Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health == 46,
            "Missing complete natural Rax victory or its Pyre health witness.");
        Console.WriteLine("NATURAL-RING7-TACTICS PASS: 57 plays/10 EndTurns, original Rax defeated, Pyre46, all frozen inputs and complete state/RNG matched.");
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

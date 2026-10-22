using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class NativeCampaignInputChecks
{
    internal static void Native(FixtureValue fixture, int distance, int seed, int pyre, string scenario)
    {
        FixtureValue input = fixture.GetProperty("NativeRunInput");
        Require(input.GetProperty("Schema").GetInt32() == 1 &&
            input.GetProperty("Seed").GetInt32() == seed && input.GetProperty("Distance").GetInt32() == distance &&
            input.GetProperty("Scenario").GetString() == scenario &&
            !input.GetProperty("WasDevCheatUsed").GetBoolean() &&
            input.GetProperty("BattlesWon").GetInt32() == distance &&
            input.GetProperty("PyreHealth").GetInt32() == pyre,
            "Native campaign input changed seed, encounter, victory history, cheat flag or initial Pyre.");
        FixtureValue[] battles = input.GetProperty("CompletedBattles").EnumerateArray().ToArray();
        Require(battles.Length == distance && battles.OrderBy(row => row.GetProperty("battleIndex").GetInt32())
                .Select(row => row.GetProperty("battleIndex").GetInt32()).SequenceEqual(Enumerable.Range(0, distance)) &&
            battles.All(row => row.GetProperty("loopIndex").GetInt32() == 0 && row.GetProperty("Won").GetBoolean() &&
                row.GetProperty("bossHPRemaining").GetSingle() == 0 && row.GetProperty("turnsLasted").GetInt32() > 0),
            "Native campaign input must retain every preceding original battle victory.");
        BattleTurnState root = fixture.GetProperty("RollingDecisions")[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
        Require(fixture.GetProperty("BattleDistance").GetInt32() == distance && root.Spawn.Turn == 0 &&
            root.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health == pyre,
            "Captured deployment does not match the initial native campaign input.");
        Console.WriteLine($"NATIVE-CAMPAIGN-INPUT PASS seed={seed} distance={distance} wins={battles.Length} Pyre={pyre} scenario={scenario}.");
    }

    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}

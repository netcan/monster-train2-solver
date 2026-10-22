using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class NaturalRing7TacticsChecks
{
    internal static void PlayerPair(FixtureValue source, FixtureValue replay)
    {
        foreach (var fixture in new[] { source, replay })
        {
            var player = fixture.GetProperty("PlayerAdvisor");
            Require(fixture.GetProperty("BattleDistance").GetInt32() == 6 && fixture.GetProperty("NativeWon").GetBoolean() &&
                player.GetProperty("Assembly").GetString() == "MonsterTrain2Poju.Advisor" &&
                player.GetProperty("Overlay").GetProperty("PurityFailures").GetInt32() == 0 &&
                player.GetProperty("Overlay").GetProperty("Failures").GetInt32() == 0,
                "Missing actual, pure player capture or natural seventh-battle victory.");
        }
        RollingAdviceChecks.FrozenPair(source, replay);
        BattleAdviceChecks.Native(source, requireBoss: false);
        var rows = source.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        int firstWin = Array.FindIndex(rows, row => row.GetProperty("PlannedOutcome").Deserialize<RoomOutcome>() == RoomOutcome.BattleWon);
        Require(rows.Length == 60 && source.GetProperty("Actions").GetArrayLength() == 50 &&
            source.GetProperty("Turns").GetArrayLength() == 10 && firstWin == 1 &&
            !rows[0].GetProperty("IsEndTurn").GetBoolean() &&
            rows[0].GetProperty("PlannedOutcome").Deserialize<RoomOutcome>() == RoomOutcome.PlayerDefeated,
            "Missing bounded deployment fallback followed by the complete compact winning route.");
        int retained = 0, freshWins = 0;
        for (int index = 0; index < rows.Length; index++)
        {
            bool reused = rows[index].GetProperty("ReusedWinningContinuation").GetBoolean();
            Require(!reused || index > firstWin, "A winning continuation was reused before a verified victory.");
            if (reused) retained++;
            if (index >= firstWin)
            {
                Require(rows[index].GetProperty("PlannedOutcome").Deserialize<RoomOutcome>() == RoomOutcome.BattleWon,
                    "A subsequent player publication lost its complete winning forecast.");
                if (!reused) freshWins++;
            }
        }
        Require(retained == 57 && freshWins == 2,
            "Missing retained winning advice or the later successful replan.");
        var states = rows.Select(row => row.GetProperty("Actual").Deserialize<BattleTurnState>()!).ToArray();
        Require(states.Any(state => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Any(unit => unit.AssetKey == "Boss_Level7_Rax" && unit.IsBoss == true)) &&
            states[^1].Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health == 62,
            "Missing original Rax or preserved Pyre62.");
        Console.WriteLine($"PLAYER-NATURAL-RING7 PASS inputs={rows.Length} retainedWins={retained}: verified deployment fallback, natural Rax victory/Pyre62 and independent complete native replay.");
    }

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

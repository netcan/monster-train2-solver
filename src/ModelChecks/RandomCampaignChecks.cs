using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RandomCampaignChecks
{
    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }

    private static void Input(FixtureValue fixture, int seed, int rank, int distance, string primary, string secondary)
    {
        Require(seed > 0 && rank is >= 0 and <= 10 && distance is >= 0 and <= 7 && primary != secondary,
            "Invalid declared random campaign configuration.");
        FixtureValue input = fixture.GetProperty("NativeRunInput");
        Require(input.GetProperty("AscensionLevel").GetInt32() == rank && input.GetProperty("StartingLevel").GetInt32() == rank &&
            input.GetProperty("PrimaryClan").GetString() == primary && input.GetProperty("SecondaryClan").GetString() == secondary &&
            !string.IsNullOrEmpty(input.GetProperty("PrimaryClanId").GetString()) &&
            input.GetProperty("PrimaryClanId").GetString() != input.GetProperty("SecondaryClanId").GetString(),
            "Random campaign changed its selected native clans or difficulty.");
        Require(fixture.GetProperty("StartingCovenantControl").ValueKind == FixtureKind.Null &&
            fixture.GetProperty("CovenantEnemyStatsControl").ValueKind == FixtureKind.Null &&
            fixture.GetProperty("OriginalOuterBossRoute").ValueKind == FixtureKind.Null,
            "Random campaign must preserve natural encounters and startup without authored battle controls.");
        BattleTurnState initial = fixture.GetProperty("RollingDecisions")[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
        int pyre = initial.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health;
        NativeCampaignInputChecks.Native(fixture, distance, seed, pyre, input.GetProperty("Scenario").GetString()!);
        FixtureValue settings = fixture.GetProperty("PlayerAdvisor").GetProperty("Overlay");
        Require(settings.GetProperty("Horizon").GetInt32() == 3 && settings.GetProperty("WholeBattle").GetBoolean() &&
            settings.GetProperty("MaxSteps").GetInt32() == 256 && settings.GetProperty("SearchWorkers").GetInt32() == 2 &&
            settings.GetProperty("TimeLimitMs").GetInt32() == 3000 &&
            settings.GetProperty("PreferredDeploymentRoom").GetInt32() == 2 && !settings.GetProperty("TacticalSeed").GetBoolean(),
            "Random campaign must exercise shipped whole-battle/two-worker/3000ms defaults.");
    }

    internal static void Source(FixtureValue fixture, int seed, int rank, int distance, string primary, string secondary)
    {
        Input(fixture, seed, rank, distance, primary, secondary);
        RollingAdviceChecks.Native(fixture);
        BattleAdviceChecks.Native(fixture, requireBoss: false);
        TrainCombatChecks.Native(fixture);
        Console.WriteLine($"RANDOM-CAMPAIGN-SOURCE PASS {primary}/{secondary} seed={seed} rank={rank} ring={distance + 1} won={fixture.GetProperty("NativeWon").GetBoolean()}.");
    }

    internal static void Pair(FixtureValue source, FixtureValue replay, int seed, int rank, int distance, string primary, string secondary)
    {
        Input(source, seed, rank, distance, primary, secondary);
        Input(replay, seed, rank, distance, primary, secondary);
        Require(source.GetProperty("NativeWon").GetBoolean() && replay.GetProperty("NativeWon").GetBoolean(),
            "Accepted random campaign pairs require two native victories.");
        Require(source.GetProperty("NativeRunInput").ContentEquals(replay.GetProperty("NativeRunInput")),
            "Independent random campaign replay changed the native campaign input.");
        RollingAdviceChecks.FrozenPair(source, replay);
        BattleAdviceChecks.Native(source, requireBoss: false);
        TrainCombatChecks.Native(source);
        TrainCombatChecks.Native(replay);
        Require(replay.GetProperty("PlayerAdvisor").GetProperty("Overlay").GetProperty("Published").GetInt32() == 0,
            "Frozen campaign replay must execute the recorded advice without a new search.");
        Console.WriteLine($"RANDOM-CAMPAIGN-PAIR PASS {primary}/{secondary} seed={seed} rank={rank} ring={distance + 1}: two native wins, full state/forecast/RNG identity.");
    }
}

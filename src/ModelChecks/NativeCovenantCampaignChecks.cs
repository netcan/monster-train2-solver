using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class NativeCovenantCampaignChecks
{
    internal static void Pair(FixtureValue source, FixtureValue replay, int level, int distance)
    {
        Require(level is >= 1 and <= 10 && distance is >= 1 and <= 7,
            "Natural cumulative campaign acceptance requires a declared rank and progressed ordinary battle.");
        foreach (FixtureValue fixture in new[] { source, replay })
        {
            Require(fixture.GetProperty("NativeWon").GetBoolean() &&
                fixture.GetProperty("PlayerAdvisor").GetProperty("Assembly").GetString() == "MonsterTrain2Poju.Advisor" &&
                fixture.GetProperty("StartingCovenantControl").ValueKind == FixtureKind.Null &&
                fixture.GetProperty("CovenantEnemyStatsControl").ValueKind == FixtureKind.Null &&
                fixture.GetProperty("OriginalOuterBossRoute").ValueKind == FixtureKind.Null,
                "Natural campaign evidence requires actual player victories from a resumed, unmodified native checkpoint.");
            Require(fixture.GetProperty("TerminalEffectsSettled").GetBoolean() &&
                fixture.GetProperty("TerminalCaptureBoundary").GetString() == "AfterStopCombatLoop",
                "Natural campaign terminal effects did not settle at the native boundary.");
            FixtureValue input = fixture.GetProperty("NativeRunInput");
            if (input.TryGetProperty("AscensionLevel", out FixtureValue actualLevel))
                Require(actualLevel.GetInt32() == level && input.GetProperty("StartingLevel").GetInt32() == level,
                    "Native campaign observation changed its difficulty or starting conditions.");
            BattleTurnState initial = fixture.GetProperty("RollingDecisions")[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
            int pyre = initial.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health;
            NativeCampaignInputChecks.Native(fixture, distance, 424242, pyre, input.GetProperty("Scenario").GetString()!);
            foreach (FixtureValue row in fixture.GetProperty("RollingDecisions").EnumerateArray())
            foreach (string field in new[] { "Root", "Actual" })
            {
                BattleTurnState state = row.GetProperty(field).Deserialize<BattleTurnState>()!;
                CombatRelicState[] owners = state.Spawn.Train.Context!.Relics!.Where(owner => owner.IsCovenant == true).ToArray();
                Require(owners.Length == level && TrueFinalBossCovenantModel.CalibratedOrdinaryWaveOwners(owners) &&
                    owners.Select((owner, index) => owner.AssetKey!.StartsWith($"Covenant{index + 1:D2}_", StringComparison.Ordinal)).All(match => match),
                    "Natural campaign lost an original cumulative owner or changed its calibrated effect signature/order.");
            }
            TrainCombatChecks.Native(fixture);
        }
        // The first source predates these additive read-only observations. Full
        // root/RNG identity below ties it to the newly observed native resume.
        FixtureValue resumed = replay.GetProperty("NativeRunInput");
        Require(resumed.GetProperty("AscensionLevel").GetInt32() == level &&
            resumed.GetProperty("StartingLevel").GetInt32() == level,
            "Native resume changed its actual difficulty or original starting conditions.");
        foreach (string field in new[] { "Seed", "Distance", "Scenario", "WasDevCheatUsed", "BattlesWon", "Gold", "PyreHealth", "CompletedBattles" })
            Require(source.GetProperty("NativeRunInput").GetProperty(field).ContentEquals(resumed.GetProperty(field)),
                "Fresh natural campaign replay changed the original native input: " + field);
        RollingAdviceChecks.FrozenPair(source, replay);
        BattleAdviceChecks.Native(source, requireBoss: false);
        Require(source.GetProperty("Actions").GetArrayLength() > 0 &&
            source.GetProperty("RollingDecisions").EnumerateArray().Any(row => row.GetProperty("Nodes").GetInt32() > 0),
            "Natural campaign source must execute card plays and actual player search.");
        Console.WriteLine($"PLAYER-NATIVE-COVENANT-CAMPAIGN PASS rank={level} ring={distance + 1}: original cumulative owners, prior native victories, player UI, full forecasts/state/RNG and independent native winning replay.");
    }

    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}

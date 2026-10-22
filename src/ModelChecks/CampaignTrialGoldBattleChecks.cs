using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CampaignTrialGoldBattleChecks
{
    internal static void PlayerPair(FixtureValue source, FixtureValue replay)
    {
        foreach (FixtureValue fixture in new[] { source, replay })
        {
            Require(fixture.GetProperty("NativeWon").GetBoolean() && fixture.GetProperty("BattleDistance").GetInt32() == 6 &&
                fixture.GetProperty("PlayerAdvisor").GetProperty("Assembly").GetString() == "MonsterTrain2Poju.Advisor" &&
                fixture.GetProperty("OriginalOuterBossRoute").ValueKind == FixtureKind.Null,
                "Trial-gold battle acceptance needs the actual player assembly and two natural Ring7 victories.");
            foreach (FixtureValue row in fixture.GetProperty("RollingDecisions").EnumerateArray())
            foreach (string field in new[] { "Root", "Actual" })
            {
                BattleTurnState state = row.GetProperty(field).Deserialize<BattleTurnState>()!;
                CombatRelicState owner = state.Spawn.Train.Context!.Relics!.Single(relic =>
                    relic.DataId == "07bdefdb-03b8-4159-b74b-be85f68c4c56");
                Require(owner.AssetKey == "ApplyUnitUpgradeWhenRich" &&
                    owner.EffectTypes.SequenceEqual(new[] { "RelicEffectModifyTrialGoldRewards" }),
                    "Natural trial-gold owner or its complete captured effect list changed.");
            }
        }
        RollingAdviceChecks.FrozenPair(source, replay);
        BattleAdviceChecks.Native(source, requireBoss: false);
        // The search process predates the additive native-input capture field;
        // the fresh replay proves its full root/RNG identity and native history.
        NativeCampaignInputChecks.Native(replay, 6, 42, 120, "Level7BattleHordeSilence");
        FixtureValue[] rows = source.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(rows.Length == 50 && source.GetProperty("Actions").GetArrayLength() == 42 &&
            source.GetProperty("Turns").GetArrayLength() == 8 && rows[0].GetProperty("Nodes").GetInt32() > 0 &&
            rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Rooms
                .SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health == 84,
            "Missing actual initial search and complete rolling battle.");
        for (int index = 0; index < rows.Length; index++)
        {
            Require(rows[index].GetProperty("PlannedOutcome").Deserialize<RoomOutcome>() == RoomOutcome.BattleWon &&
                rows[index].GetProperty("ReusedWinningContinuation").GetBoolean() == (index > 0),
                "Trial-gold player advice lost its verified winning continuation.");
            if (index == 0) continue;
            FixtureValue[] prior = rows[index - 1].GetProperty("PlanSteps").EnumerateArray().Skip(1).ToArray();
            FixtureValue[] current = rows[index].GetProperty("PlanSteps").EnumerateArray().ToArray();
            Require(rows[index].GetProperty("Nodes").GetInt32() == 0 &&
                rows[index].GetProperty("Timing").GetProperty("SearchMs").GetDouble() == 0 &&
                prior.Length == current.Length && prior.Zip(current).All(pair => pair.First.ContentEquals(pair.Second)) &&
                rows[0].GetProperty("Planned").ContentEquals(rows[index].GetProperty("Planned")) &&
                rows[0].GetProperty("Score").ContentEquals(rows[index].GetProperty("Score")),
                "Retained trial-gold advice changed its exact winning suffix, final state or score.");
        }
        Console.WriteLine($"PLAYER-CAMPAIGN-TRIAL-GOLD PASS inputs={rows.Length}: original acquired trial-gold owner, native Ring7 victory, pure player publications and independent full-state/RNG replay.");
    }

    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}

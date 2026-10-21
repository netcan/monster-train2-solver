using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PlayerOuterBossChecks
{
    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        foreach (FixtureValue fixture in new[] { source, replay })
            Require(fixture.GetProperty("PlayerAdvisor").GetProperty("Assembly").GetString() == "MonsterTrain2Poju.Advisor",
                "Outer-boss acceptance must load the separate player assembly in both processes.");
        bool rolling = source.GetProperty("RollingDecisions").GetArrayLength() > 0;
        var states = new List<BattleTurnState>();
        if (rolling)
        {
            RollingAdviceChecks.FrozenPair(source, replay);
            foreach (FixtureValue record in source.GetProperty("RollingDecisions").EnumerateArray())
                foreach (string field in new[] { "Root", "Actual" })
                    states.Add(record.GetProperty(field).Deserialize<BattleTurnState>()!);
        }
        else
        {
            BattlePlannerChecks.Pair(source, replay);
            FixtureValue plan = source.GetProperty("PlannerRuns").EnumerateArray().Single();
            Require(plan.GetProperty("Mode").GetString() == "search", "The source must consume a real player publication.");
            foreach (FixtureValue fixture in new[] { source, replay })
            {
                foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
                    Require(fixture.GetProperty(counter).GetInt32() == 0, "Player boss acceptance has nonzero " + counter);
                Require(fixture.GetProperty("TerminalEffectsSettled").GetBoolean() &&
                    string.IsNullOrEmpty(fixture.GetProperty("ModifierScenario").GetString()), "Player boss encounter was modified or unsettled.");
                FixtureValue window = fixture.GetProperty("PlannerRuns").EnumerateArray().Single();
                Require(window.GetProperty("Root").Deserialize<BattleTurnState>()!.Spawn.Turn == 0 &&
                    window.GetProperty("Steps").GetArrayLength() == fixture.GetProperty("Actions").GetArrayLength() + fixture.GetProperty("Turns").GetArrayLength() &&
                    window.GetProperty("Outcomes").EnumerateArray().Last().Deserialize<RoomOutcome>() == RoomOutcome.BattleWon,
                    "Player forecast must cover every native input from deployment through victory.");
            }
            states.Add(plan.GetProperty("Root").Deserialize<BattleTurnState>()!);
            states.AddRange(plan.GetProperty("Actual").EnumerateArray().Select(value => value.Deserialize<BattleTurnState>()!));
        }
        BattleAdviceChecks.Native(source);
        Require(source.GetProperty("NativeWon").GetBoolean() && replay.GetProperty("NativeWon").GetBoolean(),
            "Player outer-boss acceptance requires two native victories.");
        var phases = new HashSet<string>();
        var initialCards = states[0].Spawn.Train.Context!.CardInstances!.Select(card => card.InstanceId).ToHashSet();
        bool queued = false, generatedSourceTraits = false, abilitySourceTraits = false;
        int live = 0;
        foreach (BattleTurnState state in states)
        {
            queued |= state.Spawn.Turn == 0 && state.Spawn.QueuedOuterBoss?.Boss.AssetKey == "Boss_Level8_AllyBuffer";
            if (state.Spawn.OuterBoss is OuterBossProgress boss)
            {
                Require(boss.AssetKey == "Boss_Level8_AllyBuffer", "Uncalibrated boss entered the player acceptance scope.");
                phases.Add(boss.Phase); live++;
            }
            generatedSourceTraits |= state.Spawn.Train.Context!.CardInstances!.Any(card =>
                !initialCards.Contains(card.InstanceId) && card.MaskDescriptor?.SourceTraits != null &&
                state.PlayRules!.Cards.Any(rule => rule.DataId == card.DataId && rule.AssetKey == "TFB_EntropyScourge"));
            abilitySourceTraits |= state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Any(unit => unit.Ability?.CardCreation?.MaskDescriptor?.SourceTraits != null);
        }
        int activations = source.GetProperty("Actions").EnumerateArray()
            .Count(action => action.GetProperty("Action").GetProperty("ActivatorUnitId").GetInt32() > 0);
        Require(queued, "Missing native queued boss at deployment.");
        Require(live > 0 && phases.SetEquals(new[] { "Casting", "Relentless" }), "Missing native boss phases.");
        Require(generatedSourceTraits, "Missing new native Entropy Scourge source traits.");
        Require(abilitySourceTraits && activations > 0, "Missing native ability source traits or activations.");
        Console.WriteLine($"PLAYER-OUTER-BOSS PASS mode={(rolling ? "rolling" : "complete-window")} queuedBirth=true phases=2 liveSnapshots={live} generatedScourgeSourceTraits=true abilityActivations={activations}");
    }

    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}

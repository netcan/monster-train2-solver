using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PlayerOuterBossChecks
{
    internal static void WinningPair(FixtureValue source, FixtureValue replay, string expectedBoss = "Boss_Level8_AllyBuffer")
    {
        Pair(source, replay, expectedBoss);
        FixtureValue[] records = source.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        FixtureValue[] frozen = replay.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(records.Length > 2, "Missing winning rolling publications.");
        using var keys = new BattleStateKeys(ignorePreviewCopyAllocator: true);
        for (int index = 0; index < records.Length; index++)
        {
            FixtureValue record = records[index];
            bool retained = record.GetProperty("ReusedWinningContinuation").GetBoolean();
            Require(record.GetProperty("PlannedOutcome").Deserialize<RoomOutcome>() == RoomOutcome.BattleWon &&
                retained == (index > 0) &&
                record.GetProperty("Timing").GetProperty("ReusedWinningContinuation").GetBoolean() == retained,
                "Rolling advice lost the verified win or mislabeled its source.");
            Require(frozen[index].GetProperty("ReusedWinningContinuation").GetBoolean() == retained,
                "Frozen replay changed the historical publication source.");
            if (!retained)
            {
                Require(record.GetProperty("Nodes").GetInt32() > 0 &&
                    record.GetProperty("Timing").GetProperty("SearchMs").GetDouble() > 0,
                    "The initial winning publication did not search.");
                continue;
            }
            Require(record.GetProperty("Nodes").GetInt32() == 0 &&
                record.GetProperty("Timing").GetProperty("SearchMs").GetDouble() == 0 &&
                record.GetProperty("PolicyBaselineScore").ValueKind == FixtureKind.Null,
                "A retained publication claimed new search work or a root-specific baseline.");
            Require(keys.Key(records[index - 1].GetProperty("Expected").Deserialize<BattleTurnState>()!) ==
                keys.Key(record.GetProperty("Root").Deserialize<BattleTurnState>()!),
                "Retained advice changed full battle state or RNG beyond the unused preview allocator.");
            FixtureValue[] prior = records[index - 1].GetProperty("PlanSteps").EnumerateArray().Skip(1).ToArray();
            FixtureValue[] current = record.GetProperty("PlanSteps").EnumerateArray().ToArray();
            Require(prior.Length == current.Length && prior.Zip(current).All(pair => pair.First.ContentEquals(pair.Second)) &&
                records[0].GetProperty("Planned").ContentEquals(record.GetProperty("Planned")) &&
                records[0].GetProperty("Score").ContentEquals(record.GetProperty("Score")),
                "Retained advice changed its proven suffix, terminal state or score.");
        }
        Console.WriteLine($"PLAYER-BOSS-WINNING-CONTINUATION PASS decisions={records.Length} retained={records.Length - 1}: every publication wins; full roots/RNG, exact suffixes and fresh native replay verified.");
    }

    internal static void Pair(FixtureValue source, FixtureValue replay, string expectedBoss = "Boss_Level8_AllyBuffer")
        => CheckPair(source, replay, expectedBoss, requireVictory: true);

    internal static void FragileBoundedPair(FixtureValue source, FixtureValue replay)
    {
        CheckPair(source, replay, "Boss_Level8_Fragile", requireVictory: false);
        FixtureValue[] records = source.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(records.Length > 2 && records.All(record => !record.GetProperty("ReusedWinningContinuation").GetBoolean()),
            "Bounded Fragile control must publish independently after each action.");
        BattleTurnState root = records[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
        Require(source.GetProperty("BattleDistance").GetInt32() == 7 && replay.GetProperty("BattleDistance").GetInt32() == 7 &&
            root.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health == 62,
            "Bounded Fragile control must use the unchanged natural Ring8 deployment checkpoint.");
        foreach (FixtureValue fixture in new[] { source, replay })
        {
            FixtureValue overlay = fixture.GetProperty("PlayerAdvisor").GetProperty("Overlay");
            Require(overlay.GetProperty("Horizon").GetInt32() == 3 && overlay.GetProperty("TimeLimitMs").GetInt32() == 3000 &&
                overlay.GetProperty("TacticalSeed").GetBoolean() && overlay.GetProperty("PurityFailures").GetInt32() == 0 &&
                overlay.GetProperty("Failures").GetInt32() == 0, "Wrong bounded search configuration or impure player capture.");
        }
        Require(records.Any(record => record.GetProperty("PlannedOutcome").Deserialize<RoomOutcome>() == RoomOutcome.Exchanged) &&
            records.All(record => record.GetProperty("PlannedOutcome").Deserialize<RoomOutcome>() != RoomOutcome.BattleWon) &&
            records.Last().GetProperty("Outcome").Deserialize<RoomOutcome>() == RoomOutcome.PlayerDefeated &&
            !source.GetProperty("NativeWon").GetBoolean() && !replay.GetProperty("NativeWon").GetBoolean(),
            "Bounded Fragile loss control must not be labeled a winning route.");
        Require(records.Any(record => record.GetProperty("Nodes").GetInt32() > 0) &&
            records.All(record => record.GetProperty("Timing").GetProperty("SearchMs").GetDouble() > 0),
            "Bounded source publications must perform real search.");
        Require(records.Any(record => record.GetProperty("UsedShorterHorizon").GetBoolean()),
            "The native player route must exercise an explicitly shorter verified window.");
        CombatUnit champion = records[0].GetProperty("Actual").Deserialize<BattleTurnState>()!
            .Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.AssetKey == "MonsterBanishedChampion1");
        Require(champion.Triggers.First().Kind == "OnEquipmentAdded" && champion.Triggers.First().StateId == 4 &&
            champion.Triggers.Skip(1).Select(trigger => trigger.StateId).SequenceEqual(new int?[] { 0, 1, 2, 3 }),
            "Prepended relic callback must preserve earlier ability trigger identities in player capture.");
        Console.WriteLine($"PLAYER-FRAGILE-BOUNDED PASS decisions={records.Length}: legal forecasts, complete native loss and independent replay; no winning acceptance claimed.");
    }

    private static void CheckPair(FixtureValue source, FixtureValue replay, string expectedBoss, bool requireVictory)
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
        Require(!requireVictory || (source.GetProperty("NativeWon").GetBoolean() && replay.GetProperty("NativeWon").GetBoolean()),
            "Player outer-boss acceptance requires two native victories.");
        var phases = new HashSet<string>();
        var initialCards = states[0].Spawn.Train.Context!.CardInstances!.Select(card => card.InstanceId).ToHashSet();
        bool queued = false, generatedSourceTraits = false, abilitySourceTraits = false;
        int live = 0;
        foreach (BattleTurnState state in states)
        {
            queued |= state.Spawn.Turn == 0 && state.Spawn.QueuedOuterBoss?.Boss.AssetKey == expectedBoss;
            if (state.Spawn.OuterBoss is OuterBossProgress boss)
            {
                Require(boss.AssetKey == expectedBoss, "Uncalibrated boss entered the player acceptance scope.");
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
        if (expectedBoss == "Boss_Level8_AllyBuffer")
        {
            Require(generatedSourceTraits, "Missing new native Entropy Scourge source traits.");
            Require(abilitySourceTraits && activations > 0, "Missing native ability source traits or activations.");
        }
        else if (expectedBoss == "Boss_Level8_Fragile")
        {
            Require(states.Any(state => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Any(unit => unit.Team == CombatTeam.Enemy && unit.Status("buff")?.Stacks == 3)),
                "Missing the original Fragile aura in the live player encounter.");
        }
        else throw new InvalidOperationException("Uncalibrated player boss acceptance definition.");
        Console.WriteLine($"PLAYER-OUTER-BOSS PASS boss={expectedBoss} mode={(rolling ? "rolling" : "complete-window")} requireVictory={requireVictory} queuedBirth=true phases=2 liveSnapshots={live} generatedScourgeSourceTraits={generatedSourceTraits} abilityActivations={activations}");
    }

    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}

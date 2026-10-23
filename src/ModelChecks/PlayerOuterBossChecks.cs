using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PlayerOuterBossChecks
{
    internal static void FragileDefaultModel(FixtureValue fixture)
    {
        NativeCampaignInputChecks.Native(fixture, 7, 424244, 62, "Level8FinalBossMeleeWeakness");
        BattleTurnState root = fixture.GetProperty("RollingDecisions")[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
        using var keys = new BattleStateKeys();
        string parent = keys.Key(root);
        BattlePlanResult limited = BattlePlanner.Plan(root, keys.Key,
            new BattlePlannerOptions(16, 3, 8, timeLimit: TimeSpan.FromSeconds(30), allowShorterHorizon: true));
        Require(limited.ReachedNodeLimit && limited.ExpandedNodes == 16 && limited.CompletedHorizon && keys.Key(root) == parent,
            "Adaptive deployment escaped the hard node budget or lost its complete fallback.");
        string champion = root.PlayRules!.Cards.Single(rule => rule.SpawnUnit?.Modifiers?.IsChampion == true).DataId;
        int championId = root.Spawn.Train.Context!.Cards.Hand.Single(card => card.DataId == champion).InstanceId;
        Require(limited.Steps.TakeWhile(step => !step.IsEndTurn).Any(step =>
            step.Action?.CardInstanceId == championId && step.Action.RoomIndex == 0),
            "The native sixteen-node cutoff must retain the safer opening champion placement.");
        BattleAdviceSession.Verify(root, limited);
        Require(BattleHeuristicChecks.RollingForecast(fixture, 256, preferredDeploymentRoom: 2, retainCurrentTurn: true) == RoomOutcome.BattleWon,
            "The interactive default must win the captured natural Fragile root with complete state/RNG replay.");
        Console.WriteLine("FRAGILE-DEFAULT-MODEL PASS: three turns/3000ms, configured top preference, extra tactical seed off; adaptive opening and full route verified; native acceptance remains separate.");
    }

    internal static void FragileShortBudgetModel(FixtureValue fixture)
    {
        BattleTurnState root = fixture.GetProperty("RollingDecisions")[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
        Require(root.Spawn.Turn == 0 && root.Spawn.QueuedOuterBoss?.Boss.AssetKey == "Boss_Level8_Fragile" &&
            root.Spawn.QueuedOuterBoss.Actor.Unit.MaxHealth == 3000 && root.Spawn.QueuedOuterBoss.Actor.Unit.Attack == 30 &&
            root.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health == 62,
            "Short-budget Fragile model acceptance requires the original natural deployment.");
        Require(BattleHeuristicChecks.RollingForecast(fixture, 256, preferredDeploymentRoom: 0, retainCurrentTurn: true) == RoomOutcome.BattleWon,
            "The three-turn/3000ms bottom-floor Fragile route must win, not just replay a loss correctly.");
        Console.WriteLine("FRAGILE-SHORT-BUDGET-MODEL PASS: three turns/3000ms, bottom preference, extra tactical seed off; every forecast and full route replayed; native acceptance remains separate.");
    }

    internal static void FragileShortBudgetPair(FixtureValue source, FixtureValue replay)
        => FragileBudgetPair(source, replay, 0);

    internal static void FragileDefaultPair(FixtureValue source, FixtureValue replay)
        => FragileBudgetPair(source, replay, 2);

    private static void FragileBudgetPair(FixtureValue source, FixtureValue replay, int preferredRoom)
    {
        foreach (FixtureValue fixture in new[] { source, replay })
        {
            FixtureValue overlay = fixture.GetProperty("PlayerAdvisor").GetProperty("Overlay");
            Require(overlay.GetProperty("Horizon").GetInt32() == 3 && overlay.GetProperty("TimeLimitMs").GetInt32() == 3000 &&
                overlay.GetProperty("PreferredDeploymentRoom").GetInt32() == preferredRoom && !overlay.GetProperty("TacticalSeed").GetBoolean(),
                "Short-budget Fragile acceptance requires three turns/3000ms, its declared floor preference and no extra tactical seed.");
            Require(fixture.GetProperty("OriginalOuterBossRoute").ValueKind == FixtureKind.Null &&
                fixture.GetProperty("StartingCovenantControl").ValueKind == FixtureKind.Null,
                "Short-budget Fragile must use the unchanged natural checkpoint.");
            NativeCampaignInputChecks.Native(fixture, 7, 424244, 62, "Level8FinalBossMeleeWeakness");
        }
        Require(source.GetProperty("NativeRunInput").ContentEquals(replay.GetProperty("NativeRunInput")),
            "Independent short-budget Fragile replay changed its original natural campaign input.");
        CheckPair(source, replay, "Boss_Level8_Fragile", requireVictory: true);
        foreach (FixtureValue fixture in new[] { source, replay }) TrainCombatChecks.Native(fixture);
        OrdinaryAdviceChecks.NativeReuse(source, replay);
        Console.WriteLine($"PLAYER-FRAGILE-{(preferredRoom == 2 ? "DEFAULT" : "SHORT-BUDGET")} PASS: actual three-turn/3000ms player victories, configured preference={preferredRoom}, original seven-win checkpoint, complete forecasts/state/RNG and independent native replay.");
    }

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

    internal static void FragileWinningPair(FixtureValue source, FixtureValue replay)
    {
        Require(source.GetProperty("NativeWon").GetBoolean() && replay.GetProperty("NativeWon").GetBoolean(),
            "Natural Fragile winning acceptance requires two native victories.");
        WinningPair(source, replay, "Boss_Level8_Fragile");
        var root = source.GetProperty("RollingDecisions")[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
        Require(source.GetProperty("BattleDistance").GetInt32() == 7 && replay.GetProperty("BattleDistance").GetInt32() == 7 &&
            root.Spawn.Turn == 0 && root.Spawn.QueuedOuterBoss?.Actor.Unit.MaxHealth == 3000 &&
            root.Spawn.QueuedOuterBoss.Actor.Unit.Attack == 30 &&
            root.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health == 62,
            "Winning Fragile route must retain the original natural Ring8 deployment.");
        foreach (FixtureValue fixture in new[] { source, replay })
        {
            Require(fixture.GetProperty("OriginalOuterBossRoute").ValueKind == FixtureKind.Null,
                "A selected outer-boss scenario is not the natural checkpoint.");
            FixtureValue overlay = fixture.GetProperty("PlayerAdvisor").GetProperty("Overlay");
            Require(overlay.GetProperty("Horizon").GetInt32() == 12 && overlay.GetProperty("TimeLimitMs").GetInt32() == 30000 &&
                overlay.GetProperty("PreferredDeploymentRoom").GetInt32() == 0 &&
                overlay.GetProperty("TacticalSeed").GetBoolean(), "Unexpected winning Fragile search configuration.");
        }
    }

    internal static void CorrupterWinningPair(FixtureValue source, FixtureValue replay)
    {
        CheckPair(source, replay, "Boss_Level8_Corrupter", requireVictory: true, requireRelentless: false);
        foreach (FixtureValue fixture in new[] { source, replay })
        {
            NativeCampaignInputChecks.Native(fixture, 7, 42, 84, "Level8FinalBossCorrupter");
            TrainCombatChecks.Native(fixture);
            Require(fixture.GetProperty("OriginalOuterBossRoute").ValueKind == FixtureKind.Null,
                "A selected outer-boss scenario is not the natural Corrupter checkpoint.");
            FixtureValue overlay = fixture.GetProperty("PlayerAdvisor").GetProperty("Overlay");
            Require(overlay.GetProperty("Horizon").GetInt32() == 12 && overlay.GetProperty("TimeLimitMs").GetInt32() == 30000 &&
                overlay.GetProperty("PreferredDeploymentRoom").GetInt32() == 1 && overlay.GetProperty("TacticalSeed").GetBoolean(),
                "Unexpected natural Corrupter search configuration.");
        }
        Require(source.GetProperty("NativeRunInput").ContentEquals(replay.GetProperty("NativeRunInput")),
            "Fresh Corrupter replay changed the original native seven-win campaign input.");
        FixtureValue[] records = source.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(records[0].GetProperty("Root").Deserialize<BattleTurnState>()!.Spawn.Train.Context!.Relics!
            .Any(relic => relic.AssetKey == "DamageOnFloorTransition" && relic.FloorTransitionDamageRules?.Count == 2),
            "Natural Corrupter regression must retain its acquired floor-transition damage relic.");
        Require(records.Any(record => record.GetProperty("Root").Deserialize<BattleTurnState>()!.Spawn.Train.Rooms
            .SelectMany(room => room.Units).Any(unit => unit.AssetKey == "MonsterFirebrand" && unit.Status("duality")?.Stacks > 0)),
            "Natural Corrupter regression must exercise the original Firebrand with Duality.");
        CombatUnit original = records[0].GetProperty("Root").Deserialize<BattleTurnState>()!.Spawn.QueuedOuterBoss!.Actor.Unit;
        Require(original.BaseAttack == 20 && original.Health == 2800 && original.MaxHealth == 2800 && original.Triggers.Count == 2 &&
            original.Triggers[0].Kind == "OnAttacking" && original.Triggers[0].Effects.Single().Type == "CardEffectMultiplyStatusEffect" &&
            original.Triggers[1].Kind == "OnTurnBegin",
            "Natural Corrupter must retain its original full-health definition and corruption callbacks.");
        Require(records.Any(record => record.GetProperty("Nodes").GetInt32() > 0 &&
            record.GetProperty("Timing").GetProperty("SearchMs").GetDouble() > 0),
            "Natural Corrupter source did not perform actual player search.");
        Console.WriteLine($"PLAYER-CORRUPTER-WINNING PASS decisions={records.Length}: original full-health natural Boss, seven native prior wins, actual player UI and independent winning full-state/RNG replay.");
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

    internal static void CorrupterBoundedPair(FixtureValue source, FixtureValue replay)
    {
        CheckPair(source, replay, "Boss_Level8_Corrupter", requireVictory: false, requireRelentless: false);
        foreach (FixtureValue fixture in new[] { source, replay })
        {
            FixtureValue control = fixture.GetProperty("OriginalOuterBossRoute");
            FixtureValue overlay = fixture.GetProperty("PlayerAdvisor").GetProperty("Overlay");
            Require(control.GetProperty("Kind").GetString() == "SelectOriginalOuterBossScenario" &&
                control.GetProperty("Boss").GetString() == "Boss_Level8_Corrupter" && control.GetProperty("Distance").GetInt32() == 7 &&
                control.GetProperty("ScenarioAssetsUnchanged").GetBoolean() &&
                control.GetProperty("RngBefore").ContentEquals(control.GetProperty("RngAfter")) &&
                overlay.GetProperty("Horizon").GetInt32() == 3 && overlay.GetProperty("TimeLimitMs").GetInt32() == 3000 &&
                overlay.GetProperty("TacticalSeed").GetBoolean(), "Missing explicit original scene control or bounded player configuration.");
        }
        FixtureValue[] records = source.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        BattleTurnState deployment = records[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
        CombatUnit original = deployment.Spawn.QueuedOuterBoss!.Actor.Unit;
        Require(original.BaseAttack == 20 && original.MaxHealth == 2800 && original.Triggers.Count == 2 &&
            original.Triggers[0].Kind == "OnAttacking" && original.Triggers[0].Effects.Single().Type == "CardEffectMultiplyStatusEffect" &&
            original.Triggers[1].Kind == "OnTurnBegin" && deployment.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health == 80,
            "Original Corrupter definition or starting Pyre changed.");
        Require(records.Any(record => record.GetProperty("Nodes").GetInt32() > 0 &&
            record.GetProperty("Timing").GetProperty("SearchMs").GetDouble() > 0), "Player source must perform real search.");
        Console.WriteLine($"PLAYER-CORRUPTER-BOUNDED PASS decisions={records.Length}: original scene selection, bounded player publication/UI and independent complete native replay; natural progression/winning acceptance remains separate.");
    }

    internal static void TrialBoundedPair(string variant, FixtureValue source, FixtureValue replay)
    {
        string boss = OuterBossTrialChecks.Boss(variant);
        CheckPair(source, replay, boss, requireVictory: false, requireRelentless: false);
        foreach (FixtureValue fixture in new[] { source, replay })
        {
            OuterBossTrialChecks.Control(fixture, variant);
            FixtureValue overlay = fixture.GetProperty("PlayerAdvisor").GetProperty("Overlay");
            Require(overlay.GetProperty("Horizon").GetInt32() == 3 && overlay.GetProperty("TimeLimitMs").GetInt32() == 3000 &&
                overlay.GetProperty("TacticalSeed").GetBoolean(), "Wrong bounded trial player configuration.");
        }
        FixtureValue[] records = source.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        CombatUnit original = records[0].GetProperty("Root").Deserialize<BattleTurnState>()!.Spawn.QueuedOuterBoss!.Actor.Unit;
        Require(original.AssetKey == boss && original.BaseAttack == (variant == "AllyBuffer" ? 35 : variant == "Fragile" ? 30 : 20) &&
            original.MaxHealth == (variant == "AllyBuffer" ? 3800 : variant == "Fragile" ? 3000 : 2800), "Original player trial stats changed.");
        Require(variant == "Corrupter" ? original.Triggers.Single(trigger => trigger.Kind == "OnTurnBegin").Effects.Single().Action!.Statuses.Single().Stacks == 4 :
            original.Triggers.SelectMany(trigger => trigger.Effects).Single(effect => effect.Enchantment != null).Enchantment!.StatusPool.Single().Stacks ==
                (variant == "AllyBuffer" ? 10 : 6), "Original strengthened trial payload changed.");
        Require(records.Any(record => record.GetProperty("Nodes").GetInt32() > 0 &&
            record.GetProperty("Timing").GetProperty("SearchMs").GetDouble() > 0), "Player trial source did not perform real search.");
        Console.WriteLine($"PLAYER-TRIAL-BOUNDED PASS variant={variant} decisions={records.Length}: original trial sin/substitution, bounded actual-player publications/UI, complete forecasts and independent native replay; natural progression/winning acceptance remains separate.");
    }

    private static void CheckPair(FixtureValue source, FixtureValue replay, string expectedBoss, bool requireVictory, bool requireRelentless = true)
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
        Require(live > 0 && (requireRelentless ? phases.SetEquals(new[] { "Casting", "Relentless" }) :
            phases.Contains("Casting") && phases.IsSubsetOf(new[] { "Casting", "Relentless" })), "Missing native boss phases.");
        if (expectedBoss == "Boss_Level8_AllyBuffer")
        {
            Require(generatedSourceTraits, "Missing new native Entropy Scourge source traits.");
            Require(abilitySourceTraits && activations > 0, "Missing native ability source traits or activations.");
        }
        else if (expectedBoss == "Boss_Level8_AllyBuffer_TitanTrial")
        {
            Require(generatedSourceTraits && states.Any(state => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Any(unit => unit.Team == CombatTeam.Player && unit.Status("debuff")?.Stacks == 10)),
                "Missing original trial Scourge source traits or moving debuff10 aura.");
        }
        else if (expectedBoss is "Boss_Level8_Fragile" or "Boss_Level8_Fragile_TitanTrial")
        {
            Require(states.Any(state => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Any(unit => unit.Team == CombatTeam.Enemy && unit.Status("buff")?.Stacks ==
                    (expectedBoss == "Boss_Level8_Fragile_TitanTrial" ? 6 : 3))),
                "Missing the original Fragile aura in the live player encounter.");
        }
        else if (expectedBoss is "Boss_Level8_Corrupter" or "Boss_Level8_Corrupter_TitanTrial")
        {
            Require(states.Any(state => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Any(unit => unit.Team == CombatTeam.Player && unit.Status("corruption")?.Stacks > 0)),
                "Missing original Corrupter corruption on a live player unit.");
        }
        else throw new InvalidOperationException("Uncalibrated player boss acceptance definition.");
        Console.WriteLine($"PLAYER-OUTER-BOSS PASS boss={expectedBoss} mode={(rolling ? "rolling" : "complete-window")} requireVictory={requireVictory} queuedBirth=true phases={phases.Count} liveSnapshots={live} generatedScourgeSourceTraits={generatedSourceTraits} abilityActivations={activations}");
    }

    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}

using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using System.Text.Json;

internal static class BattleActionChecks
{
    internal static void Run()
    {
        SelfPurgeUpgradeDestination();
        StandbySlots();
        MagebladeStartingStatusSummon();
        var big = new CombatUnit(0, "big", CombatTeam.Player, 8, 25, 25, true, false, false, [], size: 3);
        var small = new CombatUnit(0, "small", CombatTeam.Player, 12, 10, 10, true, false, false, [], size: 2);
        var rules = new BattlePlayRules([
            new(0, 5, 7, true, false, false), new(1, 5, 7, true, false, false),
            new(2, 5, 7, false, false, false), new(3, 0, 7, true, true, true)
        ], [new("big", "big", 1, "SpawnMonster", "Standby", big, []),
            new("small", "small", 1, "SpawnMonster", "Standby", small, []),
            new("junk", "junk", 1, "Null", "Purged", null, []),
            new("future", "future", 0, "MultipleEffects", "Discard", null, ["Unimplemented effect"])]);
        var rng = UnityRng.Seed(42);
        var cards = new CardCycleState([new(1, "big"), new(2, "small"), new(3, "big"), new(4, "junk"), new(5, "future")],
            [], [], rng, 0, []);
        var pending = new CardUpgradeModifier("pending", "pending", new(damage: 4), [], false, false, false, 0, 0, []);
        var context = new CombatContext(cards, rng, 0, 6, 10, nextAddedTemporaryUpgrades: [pending]);
        var pyre = new CombatUnit(1, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, []);
        var train = new TrainCombatState(Enumerable.Range(0, 4).Select(index => new RoomCombatState(index, false,
            index == 3 ? new[] { pyre } : [], [], context)).ToArray(), [], 7, context);
        var spawn = new EnemySpawnState(train, [new EnemyWave([new EnemyGroup([])])], [-1], 0, false, rng, 2, [], 0, false, 2, 1, 3, []);
        var root = new BattleTurnState(spawn, 3, 3, 5, 0, 0, "New", [new("Spawning", 42, rng)],
            [new("Standby", []), new("Exhausted", []), new("Purged", [])], [], rules, selectedRoom: 0);
        string parent = JsonSerializer.Serialize(root);
        BattleTurnResult selected = BattleTurnModel.SelectRoom(root, 1);
        Require(selected.Supported && selected.State!.SelectedRoom == 1 && root.SelectedRoom == 0 &&
            BattleTurnModel.SelectRoom(selected.State, 1).State == selected.State,
            "Room selection did not produce an immutable, idempotent transition.");
        var framedContext = context.WithQueryFrame(new StatisticQueryFrame(energy: 3, runningCombat: true,
            turn: spawn.Turn, forgePoints: 0, dragonsHoard: 0, moonPhase: 1,
            pyreResurrectionCount: 0, selectedRoomIndex: 0));
        var framedTrain = new TrainCombatState(train.Rooms.Select(room => new RoomCombatState(room.RoomIndex,
            room.Deployment, room.Units, room.ExternalInteractions, framedContext, room.Preview,
            room.ActiveCompanionBossIds)).ToArray(), train.Movement, train.EnemySlotsPerRoom, framedContext);
        var framedSpawn = new EnemySpawnState(framedTrain, spawn.Waves, spawn.SelectedGroups, spawn.Phase,
            spawn.Looping, spawn.Rng, spawn.NextUnitId, spawn.Treasures, spawn.TreasuresRemaining,
            spawn.TreasureEnabled, spawn.FirstTreasureTurn, spawn.FirstTreasureRoom, spawn.Turn,
            spawn.ExternalInteractions);
        var framedRoot = new BattleTurnState(framedSpawn, root.Energy, root.EnergyPerTurn, root.DrawPerTurn,
            root.ForgePoints, root.DragonsHoard, root.MoonPhase, root.RngStreams, root.OtherPiles,
            root.ExternalInteractions, root.PlayRules, selectedRoom: root.SelectedRoom);
        BattleTurnResult framedSelection = BattleTurnModel.SelectRoom(framedRoot, 1);
        BattleActionResult framedCrossRoomPlay = BattleActionModel.PlayCard(framedRoot, new(3, 1));
        BattleActionResult framedSelectedRoomPlay = BattleActionModel.PlayCard(framedSelection.State!, new(3, 1));
        Require(framedSelection.Supported && framedSelection.State!.Spawn.Train.Context!.QueryFrame!.SelectedRoomIndex == 1 &&
            framedCrossRoomPlay.Supported && framedCrossRoomPlay.State!.SelectedRoom == 1 &&
            framedCrossRoomPlay.State.Spawn.Train.Context!.QueryFrame!.SelectedRoomIndex == 1 &&
            framedSelectedRoomPlay.Supported && framedSelectedRoomPlay.State!.SelectedRoom == 1 &&
            framedSelectedRoomPlay.State.Spawn.Train.Context!.QueryFrame!.SelectedRoomIndex == 1,
            "Cross-room card play did not select its target floor or explicit room selection was not retained.");
        BattleActionResult first = BattleActionModel.PlayCard(root, new(1, 0, 0));
        Require(first.Supported, first.Reason ?? "First summon rejected");
        Require(first.State!.Spawn.Train.Context!.NextAddedTemporaryUpgrades!.Count == 0 &&
            context.NextAddedTemporaryUpgrades!.Count == 1, "A non-generating play retained one-shot generation upgrades or changed its parent.");
        BattleActionResult second = BattleActionModel.PlayCard(first.State!, new(2, 0, 0));
        Require(second.Supported, second.Reason ?? "Second summon rejected");
        CombatUnit[] players = second.State!.Spawn.Train.Rooms[0].Units.ToArray();
        Require(players.Select(unit => unit.Id).SequenceEqual(new[] { 3, 2 }) && players.Sum(unit => unit.Size) == 5 &&
            players[0].SpawnerCardId == 2 && players[1].SpawnerCardId == 1, "Selected-position insertion or unit identity differed.");
        Require(second.State.Energy == 1 && second.State.OtherPiles.Single(pile => pile.Name == "Standby").Cards
            .Select(card => card.InstanceId).SequenceEqual(new[] { 1, 2 }), "Unit cost or standby card routing differed.");
        Require(BattleActionModel.PlayCard(second.State, new(3, 0)).Rejection == ActionRejection.Illegal,
            "A summon exceeded capacity.");
        BattleActionResult otherRoom = BattleActionModel.PlayCard(second.State, new(3, 1));
        Require(otherRoom.Supported && otherRoom.State!.Spawn.Train.Rooms[1].Units.Single().Size == 3 &&
            otherRoom.State.SelectedRoom == 1 && second.State.SelectedRoom == 0,
            "An arbitrary cross-room summon failed to select its target or changed its parent.");
        var legacyRoot = new BattleTurnState(spawn, 3, 3, 5, 0, 0, "New", root.RngStreams,
            root.OtherPiles, [], rules);
        BattleActionResult legacyRoom = BattleActionModel.PlayCard(legacyRoot, new(3, 1));
        Require(legacyRoom.Supported && legacyRoom.State!.SelectedRoom == -1,
            "An old capture with unknown room selection was silently assigned a selection.");
        BattleActionResult purge = BattleActionModel.PlayCard(second.State, new(4, 0));
        Require(purge.Supported && purge.State!.OtherPiles.Single(pile => pile.Name == "Purged").Cards.Single().InstanceId == 4 &&
            purge.State.Spawn.Train.Context!.Cards.Discard.Count == 0, "Self-purge routing differed.");
        Require(BattleActionModel.PlayCard(root, new(5, 0)).Rejection == ActionRejection.Unsupported,
            "An unimplemented card returned a partial search state.");
        Require(BattleActionModel.PlayCard(root, new(1, 3)).Rejection == ActionRejection.Illegal &&
            BattleActionModel.PlayCard(root, new(1, 2)).Rejection == ActionRejection.Illegal &&
            BattleActionModel.PlayCard(root, new(1, 0, 1)).Rejection == ActionRejection.Illegal,
            "An invalid target/position was accepted.");
        Require(BattleActionModel.PlayCard(otherRoom.State!, new(4, 0)).Rejection == ActionRejection.Illegal,
            "An unaffordable card was played.");
        BattleActionEnumeration frontier = BattleActionModel.EnumerateActions(root);
        Require(frontier.SupportedActions.All(action => action.CardInstanceId != 5) &&
            frontier.UnsupportedAttempts.Any(attempt => attempt.Action?.CardInstanceId == 5 &&
                attempt.Reason == "Unimplemented effect"),
            "The action frontier hid an unimplemented hand card instead of reporting the omitted candidate.");
        SupportedActionCandidate cachedFirst = frontier.SupportedCandidates.Single(candidate =>
            candidate.Action.CardInstanceId == 1 && candidate.Action.RoomIndex == 0 && candidate.Action.PlayerPosition == 0);
        Require(cachedFirst.Result.Supported && BattleTurnChecks.Comparable(cachedFirst.Result.State) == BattleTurnChecks.Comparable(first.State),
            "The action frontier did not retain the exact immutable successor for a supported play.");
        double SearchScore(BattleTurnState state, RoomOutcome outcome) => outcome == RoomOutcome.BattleWon ? -1000000 :
            outcome == RoomOutcome.PlayerDefeated ? 1000000 : state.Energy;
        var serialSearch = BattleSearchModel.Minimize(root, SearchScore, new BattleSearchOptions(64, 1, 1));
        var parallelSearch = BattleSearchModel.Minimize(root, SearchScore, new BattleSearchOptions(64, 1, 4));
        string SearchPath(BattleSearchResult result) => string.Join("|", result.Steps.Select(step => step.IsEndTurn ? "EndTurn" :
            step.Action!.CardInstanceId + ":" + step.Action.RoomIndex + ":" + step.Action.PlayerPosition + ":" +
            step.Action.TargetUnitId + ":" + step.Action.ActivatorUnitId));
        Require(serialSearch.BestState != null && serialSearch.Score.HasValue,
            "Bounded search failed to return a best-known state.");
        BattleTurnState replayedSearch = root;
        foreach (BattleSearchStep step in serialSearch.Steps)
        {
            if (step.IsEndTurn)
            {
                BattleTurnResult turn = BattleTurnModel.EndTurn(replayedSearch);
                Require(turn.Supported, turn.UnsupportedReason ?? "A searched EndTurn step failed on replay.");
                replayedSearch = turn.State!;
            }
            else
            {
                BattleActionResult action = BattleActionModel.PlayCard(replayedSearch, step.Action!);
                Require(action.Supported, action.Reason ?? "A searched card action failed on replay.");
                replayedSearch = action.State!;
            }
        }
        Require(serialSearch.BestState != null && serialSearch.Score.HasValue && serialSearch.HasUnsupportedAttempts &&
            serialSearch.DecisionStatesVisited <= 64 && serialSearch.Score == parallelSearch.Score &&
            SearchPath(serialSearch) == SearchPath(parallelSearch) &&
            BattleTurnChecks.Comparable(replayedSearch) == BattleTurnChecks.Comparable(serialSearch.BestState!) &&
            JsonSerializer.Serialize(root) == parent,
            "Bounded parallel search lost unsupported-action diagnostics, exceeded its node budget, changed ordering or mutated its root.");
        var prunedSearch = BattleSearchModel.Minimize(root, (_, _) => 0, new BattleSearchOptions(128, 0, 1), _ => 0);
        double AttackScore(BattleTurnState state, RoomOutcome outcome)
        {
            CombatUnit[] units = state.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Where(unit => unit.Team == CombatTeam.Player && !unit.IsPyre).ToArray();
            int totalAttack = units.Sum(unit => unit.Attack);
            return -totalAttack + (units.FirstOrDefault()?.AssetKey == "big" ? 0.01 : 0);
        }
        BattleSearchResult prioritizedSearch = BattleSearchModel.Minimize(root, AttackScore,
            new BattleSearchOptions(2, 0, 1));
        var cancelledSearch = BattleSearchModel.Minimize(root, SearchScore, new BattleSearchOptions(8, 1, 2),
            cancellationToken: new System.Threading.CancellationToken(true));
        Require(prunedSearch.PrunedBranches > 0 && prunedSearch.DecisionStatesVisited == 1 && prunedSearch.ReachedTurnLimit &&
            prioritizedSearch.Steps.Count == 2 && prioritizedSearch.Steps[0].Action?.CardInstanceId == 2 &&
            prioritizedSearch.Score < 0 && cancelledSearch.Cancelled &&
            cancelledSearch.BestState != null && cancelledSearch.DecisionStatesVisited == 1,
            "Search failed to apply an admissible lower bound, expose the turn horizon or preserve cancellation status: pruned=" +
            prunedSearch.PrunedBranches + ", nodes=" + prunedSearch.DecisionStatesVisited + ", rankedPath=" +
            string.Join("|", prioritizedSearch.Steps.Select(step => step.IsEndTurn ? "EndTurn" : step.Action!.CardInstanceId.ToString())) +
            ", score=" + prioritizedSearch.Score + ", turnLimit=" + prunedSearch.ReachedTurnLimit + ", cancelled=" + cancelledSearch.Cancelled +
            ", best=" + (cancelledSearch.BestState != null) + ", nodes=" + cancelledSearch.DecisionStatesVisited + ".");
        string expected = BattleTurnChecks.Comparable(second.State);
        Parallel.For(0, 32, _ =>
        {
            BattleTurnState child = BattleActionModel.PlayCard(BattleActionModel.PlayCard(root, new(1, 0, 0)).State!, new(2, 0, 0)).State!;
            Require(BattleTurnChecks.Comparable(child) == expected, "Parallel action branches diverged.");
        });
        Require(JsonSerializer.Serialize(root) == parent, "Card plays mutated their parent.");
        using var unsupportedFixture = MonsterTrain2Poju.Capture.NativeFixtureCapture.Capture(new
        {
            Policy = "units-and-junk",
            Actions = new[] { new { Index = 0, Before = root, Actual = root, Action = new PlayCardAction(5, 0) } }
        });
        bool skippedActionRejected = false;
        try { Native(unsupportedFixture.RootElement); }
        catch (InvalidOperationException error) when (error.Message == "Native action verification skipped unsupported actions.")
        { skippedActionRejected = true; }
        Require(skippedActionRejected, "An unsupported native action silently skipped its continuous policy verification.");
        Console.WriteLine("ACTION-CHECKS PASS: capacity, position, card identity, cost, purge, explicit rejection and parallel isolation.");
    }

    private static void SelfPurgeUpgradeDestination()
    {
        var definition = new CardMaskDefinition("purgeable", "Spell", "Common", false, false, false,
            [], [], [], [], null, false, 0, 0, false, false, false);
        var descriptor = new CardMaskDescriptor(definition, 0, [], []);
        var trait = new CardTraitValue("CardTraitSelfPurge", "CardTraitSelfPurge", 0, 0, true, 0);
        var lifecycle = new CardLifecycleUpgrade(new CardMaskUpgrade("relic-self-purge",
            new CardStatModifier(), [], false, false, false, false), "RelicSelfPurge", false, false,
            [trait], [], false, [], [], []);
        var upgrade = new CardUpgradeModifier("relic-self-purge", "RelicSelfPurge", new CardStatModifier(), [],
            false, true, false, 0, 0, [], maskMetadata: new CardUpgradeMaskMetadata([], false, false, false, false),
            lifecycle: lifecycle);
        var card = new CardInstanceState(6, "purgeable", CardModifiers.Empty(),
            new CardModifiers(new CardStatModifier(), [upgrade], 0, []), 0, 0, 0, [], maskDescriptor: descriptor);
        var rule = new CardPlayRule("purgeable", "Purgeable", 0, "Null", "Discard", null, [], cardType: "Spell");
        CardPlayRule resolved = CardModifierModel.Resolve(rule, card);
        Require(resolved.Destination == "Purged",
            "A branch-added SelfPurge trait did not change the played card destination.");
        Require(CardModifierModel.Resolve(rule, new CardInstanceState(7, "purgeable", CardModifiers.Empty(),
            CardModifiers.Empty(), 0, 0, 0, [])).Destination == "Discard",
            "A legacy card without branch trait metadata changed its captured destination.");
    }

    private static void StandbySlots()
    {
        var root = new CardPileState("Standby", [new(1, "one"), new(2, "two"), new(3, "three")], [1, 2, 3], []);
        string parent = JsonSerializer.Serialize(root);
        var holes = CardPileModel.Remove(CardPileModel.Remove(root, 1), 3);
        var first = CardPileModel.Add(holes, new(4, "four"));
        var next = CardPileModel.Add(first, new(5, "five"));
        Require(CardPileModel.Validate(next) == null && first.Cards.Select(card => card.InstanceId).SequenceEqual(new[] { 2, 4 }) &&
            next.Cards.Select(card => card.InstanceId).SequenceEqual(new[] { 5, 2, 4 }), "Standby insertions did not reuse free slots in native LIFO order.");
        var differentHistory = CardPileModel.Remove(CardPileModel.Remove(root, 3), 1);
        Require(holes.Cards.Select(card => card.InstanceId).SequenceEqual(differentHistory.Cards.Select(card => card.InstanceId)) &&
            JsonSerializer.Serialize(holes) != JsonSerializer.Serialize(differentHistory) &&
            CardPileModel.Add(differentHistory, new(4, "four")).Cards[0].InstanceId == 4,
            "Same visible cards erased distinct future standby ordering.");
        Require(CardPileModel.Validate(new("Standby", [new(2, "two")], [0, 2, 0], [0, 0])) != null &&
            CardPileModel.Validate(new("Standby", [new(2, "two")], [0, 3], [0])) != null,
            "Malformed standby allocation metadata was accepted.");
        Require(CardPileModel.Clear(holes).EntrySlots!.Count == 0 && CardPileModel.Clear(holes).FreeSlots!.Count == 0,
            "Terminal dictionary clear retained allocation history.");
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(CardPileModel.Add(holes, new(4, "four"))) == JsonSerializer.Serialize(first),
            "Parallel standby insertions differ."));
        Require(JsonSerializer.Serialize(root) == parent, "Standby routing mutated a parent.");
        Console.WriteLine("STANDBY-SLOT-CHECKS PASS: holes, LIFO reuse, distinct futures, malformed metadata, terminal clear and parallel isolation.");
    }

    internal static void MagebladeStartingStatusSummon()
    {
        var rng = UnityRng.Seed(42);
        var status = new CombatStatus("mageblade", 2, 1);
        var template = new CombatUnit(0, "mageblade-unit", CombatTeam.Player, 3, 80, 80, true, false, false,
            [status], size: 1, modifiers: new UnitModifiers(3, 0, 0, 1, 1, true, false, []), statusRegistry: [status]);
        var target = new CombatUnit(2, "mageblade-target", CombatTeam.Enemy, 0, 100, 100, false, false, false, [], size: 1);
        var pyre = new CombatUnit(1, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, []);
        var rules = new BattlePlayRules(
            [new(0, 5, 7, true, false, false), new(1, 5, 7, true, false, false),
                new(2, 5, 7, true, false, false), new(3, 0, 7, true, true, true)],
            [new("mageblade-card", "MagebladeCard", 0, "SpawnMonster", "Standby", template, [])]);
        var cards = new CardCycleState([new(1, "mageblade-card")], [], [], rng, 0, []);
        var context = new CombatContext(cards, rng, 0, 2, 10,
            magicPower: [new RoomMagicPower(0, CombatTeam.Player, 15)], nextUnitId: 3);
        var train = new TrainCombatState(Enumerable.Range(0, 4).Select(index => new RoomCombatState(index, false,
            index == 0 ? [target] : index == 3 ? [pyre] : [], [], context)).ToArray(), [], 7, context);
        var spawn = new EnemySpawnState(train, [new EnemyWave([new EnemyGroup([])])], [-1], 0, false,
            rng, 3, [], 0, false, 2, 1, 3, []);
        var decision = new BattleTurnState(spawn, 3, 3, 5, 0, 0, "New", [new("Spawning", 42, rng)],
            [new("Standby", []), new("Exhausted", []), new("Purged", [])], [], rules, selectedRoom: 0);

        Require(BattleActionModel.EnumerateSupportedPlays(decision).Any(action => action.CardInstanceId == 1 && action.RoomIndex == 0),
            "A starting Mageblade unit was filtered out before its birth callbacks could initialize the cache.");
        BattleActionResult played = BattleActionModel.PlayCard(decision, new(1, 0));
        Require(played.Supported, played.Reason ?? "A starting Mageblade unit could not be summoned.");
        CombatUnit born = played.State!.Spawn.Train.Rooms[0].Units.Single(unit => unit.Team == CombatTeam.Player);
        Require(born.Status("mageblade")?.Stacks == 2 && born.Modifiers?.MagebladeDamagePerMagicPower == 2,
            "Birth callbacks did not rebuild the Mageblade coefficient from the starting stacks: status=" +
                (born.Status("mageblade")?.Stacks.ToString() ?? "missing") + ", cache=" +
                (born.Modifiers?.MagebladeDamagePerMagicPower?.ToString() ?? "missing") + ".");

        RoomCombatResult exchange = RoomCombatModel.Exchange(played.State.Spawn.Train.Rooms[0]);
        Require(exchange.Supported && exchange.State!.Units.Single(unit => unit.Id == target.Id).Health == 67,
            exchange.UnsupportedReason ?? "The born Mageblade unit did not apply its captured room-power attack bonus.");
        Console.WriteLine("MAGEBLADE-BIRTH-CHECKS PASS: legal summon, callback cache=2 and captured power 15 produced 33 attack damage.");
    }

    internal static void Native(FixtureValue fixture)
    {
        if (fixture.TryGetProperty("ModifierScenario", out FixtureValue attachmentScenario) &&
            attachmentScenario.GetString() == "room-capacity-attachment")
        {
            FixtureValue evidence = fixture.GetProperty("RoomCapacityAttachmentCoverage");
            Require(evidence.GetProperty("Prepared").GetBoolean() && evidence.GetProperty("Selected").GetBoolean() &&
                evidence.GetProperty("Completed").GetBoolean() && evidence.GetProperty("Error").ValueKind == FixtureKind.Null &&
                evidence.GetProperty("AssetKey").GetString() == "IncreaseRoomCapacity" &&
                evidence.GetProperty("AfterCapacity").GetInt32() == evidence.GetProperty("BeforeCapacity").GetInt32() + 4 &&
                evidence.GetProperty("ActiveAttachmentCount").GetInt32() == 1 &&
                evidence.GetProperty("AttachedCardDataId").GetString() == evidence.GetProperty("CardDataId").GetString(),
                "Native IncreaseRoomCapacity did not remain attached with its +4 capacity effect.");
        }
        if (!fixture.TryGetProperty("Actions", out FixtureValue actions) || actions.GetArrayLength() == 0) return;
        bool compareRoomEnabled = BattleTurnChecks.ShouldCompareRoomEnabled(fixture);
        int supported = 0, unsupported = 0;
        foreach (FixtureValue entry in actions.EnumerateArray())
        {
            BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
            PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
            BattleActionResult result = BattleActionModel.PlayCard(before, action);
            if (!result.Supported)
            { unsupported++; Console.WriteLine("NATIVE-ACTION-UNSUPPORTED index=" + entry.GetProperty("Index") + ": " + result.Reason); continue; }
            BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            if (entry.TryGetProperty("ActualOutcome", out FixtureValue actionOutcome))
            {
                Require(result.Outcome == (RoomOutcome)actionOutcome.GetInt32(), "Native card action outcome differs.");
                if (actionOutcome.GetInt32() == (int)RoomOutcome.BattleWon && fixture.GetProperty("Schema").GetInt32() >= 12)
                {
                    if (action.ActivatorUnitId > 0) UnitAbilityChecks.NativeTerminal(entry);
                    else if (before.PlayRules!.Cards.Single(rule => rule.DataId == before.Spawn.Train.Context!.FindCard(action.CardInstanceId)!.DataId).Effect == "SpawnMonster")
                        LethalRallyChecks.NativeTerminal(entry);
                    else TerminalSpellChecks.Native(entry);
                }
            }
            bool terminalOutcome = result.Outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
            string? difference = ModelJson.Difference(BattleTurnChecks.Comparable(result.State!, terminalOutcome, compareRoomEnabled),
                BattleTurnChecks.Comparable(actual, terminalOutcome, compareRoomEnabled));
            if (difference != null && result.State!.Spawn.Train.Context?.Statistics is BattleStatistics modeledStatistics &&
                actual.Spawn.Train.Context?.Statistics is BattleStatistics actualStatistics)
            {
                var modeledKeys = modeledStatistics.Values.Select(value => (value.CardId, value.Duration, value.Type)).ToHashSet();
                foreach (CardStatisticValue missing in actualStatistics.Values.Where(value => !modeledKeys.Contains((value.CardId, value.Duration, value.Type))))
                    Console.WriteLine($"ACTION-STAT-MISSING index={entry.GetProperty("Index").GetInt32()} card={missing.CardId} duration={missing.Duration} type={missing.Type} actual={missing.Value}");
            }
            Require(difference == null, "Native card action differs at index " + entry.GetProperty("Index").GetInt32() + ": " + difference);
            supported++;
        }
        Require(unsupported == 0, "Native action verification skipped unsupported actions.");
        Console.WriteLine($"NATIVE-ACTION-CHECKS PASS: {supported} matched, {unsupported} unsupported.");
        if (fixture.TryGetProperty("ModifierScenario", out FixtureValue unitUpgradeTriggerScenario) &&
            unitUpgradeTriggerScenario.GetString() == "original-unit-upgrade-trigger-relic")
            OriginalUnitUpgradeTriggerRelicChecks.Native(fixture);
        UnitTeleportChecks.Native(fixture);
        if (fixture.TryGetProperty("ModifierScenario", out FixtureValue relicScenario) &&
            relicScenario.GetString() == "relic-card-self-purge-upgrades")
        {
            int selfPurged = 0;
            foreach (FixtureValue entry in actions.EnumerateArray())
            {
                BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
                PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
                CardInstanceState? card = before.Spawn.Train.Context!.FindCard(action.CardInstanceId);
                if (card?.Temporary.Upgrades.SelectMany(upgrade => upgrade.Lifecycle?.AddedTraits ?? Array.Empty<CardTraitValue>())
                    .Any(trait => trait.RuntimeType == "CardTraitSelfPurge") != true) continue;
                BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
                bool reachedPurgedPile = actual.OtherPiles.Single(pile => pile.Name == "Purged").Cards.Any(item => item.InstanceId == action.CardInstanceId);
                IReadOnlyList<int>? deckCards = actual.Spawn.Train.Context!.Statistics?.DeckCards;
                Require(reachedPurgedPile && deckCards != null && !deckCards.Contains(action.CardInstanceId),
                    "A played relic SelfPurge card remained in the native deck or missed the Purged pile.");
                selfPurged++;
            }
            Require(selfPurged > 0, "The native relic SelfPurge scenario did not play a card carrying its temporary trait.");
            Console.WriteLine($"NATIVE-SELF-PURGE-CHECKS PASS: {selfPurged} relic-upgraded spells moved to Purged and left deck membership.");
        }
        if (fixture.TryGetProperty("ModifierScenario", out FixtureValue roomScenario) && roomScenario.GetString() == "room-spells")
            RoomSpellChecks.Native(actions);
        if (fixture.TryGetProperty("ModifierScenario", out FixtureValue damagePerHpScenario) &&
            damagePerHpScenario.GetString() is "damage-per-hp" or "damage-per-hp-armor" or "damage-per-hp-last-attacker" or "damage-per-hp-ability")
            DamagePerHpChecks.Native(fixture);
        DamageOverflowChecks.Native(fixture);
        if (fixture.TryGetProperty("ModifierScenario", out FixtureValue randomScenario) && randomScenario.GetString() == "random-spells")
            RandomSpellChecks.Native(actions);
        if (fixture.TryGetProperty("ModifierScenario", out FixtureValue randomStatusScenario) && randomStatusScenario.GetString() == "random-status")
            RandomStatusChecks.Native(actions);
        if (fixture.TryGetProperty("ModifierScenario", out FixtureValue crossRoomScenario) && crossRoomScenario.GetString() is "cross-room-spells" or "cross-room-targets")
            CrossRoomSpellChecks.Native(fixture);
        if (fixture.TryGetProperty("ModifierScenario", out FixtureValue attackScenario) && attackScenario.GetString() == "attack-buffs")
            UnitAttackChecks.Native(actions);
        if (fixture.TryGetProperty("ModifierScenario", out FixtureValue healthScenario) && healthScenario.GetString() is "max-health-spells" or "max-health-lethal")
            UnitHealthChecks.Native(fixture);
        if (fixture.TryGetProperty("ModifierScenario", out FixtureValue rangeScenario) && rangeScenario.GetString() is "numeric-ranges" or "numeric-ranges-lethal")
            NumericRangeChecks.Native(fixture);
        if (fixture.TryGetProperty("ModifierScenario", out FixtureValue filterScenario) && filterScenario.GetString() == "target-filters")
            TargetFilterChecks.Native(fixture);
        if (fixture.TryGetProperty("ModifierScenario", out FixtureValue drawScenario) && drawScenario.GetString() == "drawing")
            DrawSpellChecks.Native(fixture);
        if (fixture.TryGetProperty("ModifierScenario", out FixtureValue removalScenario) && removalScenario.GetString() is "hand-removal" or "hand-removal-lethal")
            HandRemovalChecks.Native(fixture);
        if (fixture.TryGetProperty("ModifierScenario", out FixtureValue randomDiscardScenario) && randomDiscardScenario.GetString() == "random-discard")
            RandomDiscardChecks.Native(fixture);
        if (fixture.TryGetProperty("ModifierScenario", out FixtureValue generationScenario) &&
            generationScenario.GetString() is "generation" or "generation-lethal" or "generation-permanent-consume")
            CardGenerationChecks.Native(fixture);
        if (fixture.TryGetProperty("ModifierScenario", out FixtureValue healingScenario) && healingScenario.GetString() is "healing" or "healing-triggers")
        {
            int healPlays = 0, restored = 0;
            var statuses = new HashSet<string>();
            foreach (FixtureValue entry in actions.EnumerateArray())
            {
                BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
                BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
                PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
                foreach (CombatStatus status in before.Spawn.Train.Rooms.SelectMany(room => room.Units).SelectMany(unit => unit.Statuses))
                    statuses.Add(status.Id);
                string dataId = before.Spawn.Train.Context!.Cards.Hand.Single(card => card.InstanceId == action.CardInstanceId).DataId;
                if (!before.PlayRules!.Cards.Single(card => card.DataId == dataId).Effects.Any(effect => effect.Type == "Heal")) continue;
                healPlays++;
                CombatUnit? target = before.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.Id == action.TargetUnitId);
                CombatUnit? after = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.Id == action.TargetUnitId);
                if (target != null && after != null && after.Health > target.Health) restored++;
            }
            Require(healPlays > 0 && restored > 0 && new[] { "heal multiplier", "heal immunity", "regen", "lifesteal" }.All(statuses.Contains),
                "The healing oracle did not restore health or encounter all intended status rules.");
            Console.WriteLine($"NATIVE-HEALING-COVERAGE PASS: {healPlays} healing spell plays, {restored} restored targets, multiplier/immunity/regen/lifesteal present.");
            if (healingScenario.GetString() == "healing-triggers")
            {
                var actualStates = actions.EnumerateArray().Select(entry => entry.GetProperty("Actual").Deserialize<BattleTurnState>()!).ToArray();
                var units = actualStates.SelectMany(state => state.Spawn.Train.Rooms).SelectMany(room => room.Units).ToArray();
                Require(units.Any(unit => unit.Triggers.Any(trigger => trigger.Kind == "OnHeal" && trigger.Once && trigger.HasTriggered)),
                    "The native OnHeal fixture never consumed its once-only trigger.");
                Require(units.Any(unit => unit.Statuses.Any(status => status.Id == "silenced") &&
                    unit.Triggers.Any(trigger => trigger.Kind == "OnHeal" && trigger.IgnoreSilence && trigger.HasTriggered) &&
                    unit.Triggers.Where(trigger => trigger.Kind == "OnHeal" && !trigger.IgnoreSilence).All(trigger => !trigger.HasTriggered)),
                    "The native OnHeal fixture did not exercise ignored silence.");
                bool immuneRewards = actions.EnumerateArray().Any(entry =>
                {
                    BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
                    BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
                    int id = entry.GetProperty("Action").GetProperty("TargetUnitId").GetInt32();
                    CombatUnit? target = before.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.Id == id);
                    return target?.Statuses.Any(status => status.Id == "heal immunity") == true &&
                        actual.Spawn.Train.Context!.Gold - before.Spawn.Train.Context!.Gold == 15;
                });
                Require(immuneRewards, "Three blocked heals did not produce three native minimum rewards.");
                Console.WriteLine("NATIVE-ONHEAL-COVERAGE PASS: three blocked heal rewards, once-only state, silence and ignored silence.");
            }
        }
        if (fixture.TryGetProperty("ModifierScenario", out FixtureValue scenario) &&
            (scenario.GetString() is "dynamic-upgrades" or "sacrifice-upgrades" or "hand-upgrades" or "targeted-hand-upgrades" or "permanent-consume-hand-upgrades" or "permanent-consume-over-temporary"))
        {
            var modifiedActions = actions.EnumerateArray().Where(entry =>
            {
                BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
                int id = entry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
                string dataId = before.Spawn.Train.Context!.Cards.Hand.Single(card => card.InstanceId == id).DataId;
                return before.PlayRules!.Cards.Single(card => card.DataId == dataId).Effects.Any(effect => effect.Type is "UnitUpgrade" or "HandUpgrade");
            }).ToArray();
            Require(modifiedActions.Length > 0, "The dynamic upgrade fixture never played its modified spell.");
            bool observed = modifiedActions.Any(entry =>
            {
                BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
                if (scenario.GetString() is "hand-upgrades" or "targeted-hand-upgrades" or "permanent-consume-hand-upgrades" or "permanent-consume-over-temporary")
                    return actual.Spawn.Train.Context!.CardInstances!.Any(card => card.Permanent.Upgrades
                        .Any(upgrade => upgrade.AssetKey.StartsWith("PojuHand", StringComparison.Ordinal)));
                return scenario.GetString() == "sacrifice-upgrades" ? !actual.Spawn.Train.Rooms.SelectMany(room => room.Units)
                    .Any(unit => unit.Id == entry.GetProperty("Action").GetProperty("TargetUnitId").GetInt32()) :
                    actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Modifiers?.Upgrades
                        .Any(upgrade => upgrade.AssetKey.StartsWith("PojuProbe", StringComparison.Ordinal)) == true);
            });
            Require(observed, "The dynamic upgrade fixture never changed the native unit or killed its target.");
            Console.WriteLine($"NATIVE-UPGRADE-COVERAGE PASS: {modifiedActions.Length} modified spell plays, observed native {scenario.GetString()} effects.");
        }
        if (fixture.TryGetProperty("ModifierScenario", out FixtureValue permanentConsumeScenario) &&
            permanentConsumeScenario.GetString() is "permanent-consume-hand-upgrades" or "permanent-consume-over-temporary")
        {
            int exhausted = 0;
            foreach (FixtureValue entry in actions.EnumerateArray())
            {
                BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
                int cardId = entry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
                CardInstanceState? card = actual.Spawn.Train.Context!.FindCard(cardId);
                bool overTemporary = permanentConsumeScenario.GetString() == "permanent-consume-over-temporary";
                bool consumed = card?.Permanent.Upgrades.Any(upgrade => upgrade.AssetKey == "PojuHandPermanentConsume") == true &&
                    (!overTemporary || (card!.Temporary.Upgrades.Any(upgrade => upgrade.AssetKey == "PojuHandTemporaryConsume") &&
                        card.MaskDescriptor!.BaseTraits.All(trait => trait.RuntimeType != "CardTraitExhaustState") &&
                        card.ExhaustState!.BaseTraits.Count == 0 && card.ExhaustState.UpgradeTrait?.IsExhausted == true)) &&
                    (overTemporary || card!.ExhaustState?.BaseTraits.Any(trait => trait.IsExhausted) == true) &&
                    actual.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards.Any(token => token.InstanceId == cardId);
                if (consumed) exhausted++;
            }
            Require(exhausted > 0, "The native hand-installed permanent Consume card was never played and exhausted.");
            Console.WriteLine($"NATIVE-PERMANENT-CONSUME PASS: {exhausted} permanently upgraded cards were played into Exhausted ({permanentConsumeScenario.GetString()}).");
        }
        if (!fixture.TryGetProperty("Policy", out FixtureValue policy) ||
            policy.GetString() is not ("units-and-junk" or "units-spells-and-junk")) return;
        if (!compareRoomEnabled)
        {
            Console.WriteLine("NATIVE-POLICY-CHAIN-CHECKS SKIP: legacy EndTurn room Enabled snapshots conflict with the current model.");
            return;
        }
        Func<BattleTurnState, PlayCardAction?> chooser = policy.GetString() == "units-spells-and-junk"
            ? BattleActionModel.ChooseUnitSpellAndJunkPlay : BattleActionModel.ChooseUnitAndJunkPlay;
        if (fixture.TryGetProperty("ModifierScenario", out var chooseDiscardPolicy) &&
            chooseDiscardPolicy.GetString() is "card-effect-choose-discard" or "card-effect-choose-discard-others" or "card-effect-choose-discard-others-policy" or "card-effect-choose-discard-refine")
        {
            int scenarioCardId = actions[0].GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
            chooser = source => BattleActionModel.EnumerateSupportedPlays(source)
                .FirstOrDefault(action => action.CardInstanceId != scenarioCardId);
        }
        if (fixture.TryGetProperty("ModifierScenario", out var keepOneSmeltPolicy) &&
            keepOneSmeltPolicy.GetString() is "card-effect-choose-discard-others" or "card-effect-choose-discard-refine")
            chooser = _ => null;
        if (fixture.TryGetProperty("ModifierScenario", out var noPlayerUnitPolicy) &&
            noPlayerUnitPolicy.GetString() == "conditional-no-player-status")
            chooser = ConditionalNoPlayerUnitChecks.Choose;
        if (fixture.TryGetProperty("ModifierScenario", out var conditionalPyrePolicy) &&
            conditionalPyrePolicy.GetString() == "conditional-pyre-damage")
            chooser = ConditionalPyreDamageChecks.Choose;
        if (fixture.TryGetProperty("ModifierScenario", out var selfAttackStatusPolicy) &&
            selfAttackStatusPolicy.GetString() == "self-attack-status")
            chooser = SelfAttackStatusChecks.Choose;
        if (fixture.TryGetProperty("ModifierScenario", out var damagePerHpPolicy) &&
            damagePerHpPolicy.GetString() is "damage-per-hp" or "damage-per-hp-armor" or "damage-per-hp-last-attacker")
            chooser = DamagePerHpChecks.Choose;
        if (fixture.TryGetProperty("ModifierScenario", out var damageOverflowPolicy) && damageOverflowPolicy.GetString() == "damage-overflow")
            chooser = DamageOverflowChecks.Choose;
        if (fixture.TryGetProperty("ModifierScenario", out var scenarioPolicy) && scenarioPolicy.GetString() == "rally-lethal")
            chooser = BattleActionModel.ChooseBossRoomSummonThenCards;
        if (fixture.TryGetProperty("ModifierScenario", out var sacrificeScenario) && sacrificeScenario.GetString() == "sacrifice-effects")
            chooser = ChooseSacrificeEffectsThenCards;
        if (fixture.TryGetProperty("ModifierScenario", out var typedDrawScenario) && typedDrawScenario.GetString() == "typed-draw")
            chooser = ChooseTypedDrawThenCards;
        if (fixture.TryGetProperty("ModifierScenario", out var incantPolicy) && incantPolicy.GetString() is "incant" or "incant-thresholds" or "incant-relic" or "incant-relic-combined")
            chooser = BattleActionModel.ChooseEmptySpellThenCards;
        if (fixture.TryGetProperty("ModifierScenario", out var originalUnitRelicPolicy) && originalUnitRelicPolicy.GetString() == "original-unit-relic-batch")
            chooser = OriginalUnitRelicBatchChecks.Choose;
        if (fixture.TryGetProperty("ModifierScenario", out var originalStingRelicPolicy) && originalStingRelicPolicy.GetString() == "original-sting-relics")
            chooser = OriginalStingRelicBatchChecks.Choose;
        if (fixture.TryGetProperty("ModifierScenario", out var originalDeployableRelicPolicy) && originalDeployableRelicPolicy.GetString() == "original-deployable-relic")
            chooser = OriginalDeployableRelicChecks.Choose;
        if (fixture.TryGetProperty("ModifierScenario", out var originalTriggerRelicPolicy) && originalTriggerRelicPolicy.GetString() == "original-trigger-relic-batch")
            chooser = OriginalTriggerRelicBatchChecks.Choose;
        if (fixture.TryGetProperty("ModifierScenario", out var originalLifecycleRelicPolicy) && originalLifecycleRelicPolicy.GetString() == "original-lifecycle-relic-batch")
            chooser = OriginalLifecycleRelicBatchChecks.Choose;
        if (fixture.TryGetProperty("ModifierScenario", out var originalDecayRelicPolicy) && originalDecayRelicPolicy.GetString() == "original-decay-relic-batch")
            chooser = OriginalDecayRelicBatchChecks.Choose;
        if (fixture.TryGetProperty("ModifierScenario", out var originalResourceRelicPolicy) && originalResourceRelicPolicy.GetString() == "original-resource-relic-batch")
            chooser = OriginalResourceRelicBatchChecks.Choose;
        if (fixture.TryGetProperty("ModifierScenario", out var originalUnitUpgradeTriggerPolicy) &&
            originalUnitUpgradeTriggerPolicy.GetString() == "original-unit-upgrade-trigger-relic")
            chooser = OriginalUnitUpgradeTriggerRelicChecks.Choose;
        if (fixture.TryGetProperty("ModifierScenario", out var endlessReturnPolicy) && endlessReturnPolicy.GetString() == "original-endless-return")
        {
            int targetCardId = fixture.GetProperty("EndlessReturnCardId").GetInt32();
            chooser = source => EndlessReturnChecks.Choose(source, targetCardId);
        }
        else if (fixture.TryGetProperty("ModifierScenario", out var pyreResourceScenario) && pyreResourceScenario.GetString() == "pyre-resource-refresh")
            chooser = PyreResourceRefreshChecks.Choose;
        else if (fixture.TryGetProperty("ModifierScenario", out var capacityRelicScenario) && capacityRelicScenario.GetString() == "original-floor-capacity-relics")
            chooser = OriginalFloorCapacityRelicChecks.Choose;
        if (fixture.TryGetProperty("ModifierScenario", out var attachmentPolicy) && attachmentPolicy.GetString() == "room-capacity-attachment")
        {
            string cardDataId = fixture.GetProperty("RoomCapacityAttachmentCoverage").GetProperty("CardDataId").GetString()!;
            chooser = source => ChooseRoomCapacityAttachmentThenCards(source, cardDataId);
        }
        if (fixture.TryGetProperty("ModifierScenario", out var summonScenario) && summonScenario.GetString()?.StartsWith("multi-summon", StringComparison.Ordinal) == true)
            chooser = BattleActionModel.ChooseMultiSummonThenCards;
        if (fixture.TryGetProperty("ModifierScenario", out var abilityScenario) &&
            abilityScenario.GetString() is "ability-incant" or "ability-activation" or "ability-activation-x" or "ability-activation-lethal" or "damage-per-hp-ability" or "ability-teleport" or "ability-effects" or "equipment-abilities" or "spawn-or-status")
            chooser = UnitAbilityModel.ChooseAbilityThenCards;
        if (fixture.TryGetProperty("ModifierScenario", out var damagePerHpAbilityScenario) && damagePerHpAbilityScenario.GetString() == "damage-per-hp-ability")
            chooser = DamagePerHpChecks.ChooseAbility;
        if (fixture.TryGetProperty("ModifierScenario", out var teleportScenario) && teleportScenario.GetString() == "ability-teleport")
            chooser = UnitAbilityModel.ChooseTeleportCalibrationAction;
        if (fixture.TryGetProperty("ModifierScenario", out var magebladeScenario) &&
            magebladeScenario.GetString() is "mageblade" or "original-mageblade-magnitude")
            chooser = ChooseMagebladeThenCards;
        if (fixture.TryGetProperty("ModifierScenario", out var equipmentBuffScenario) && equipmentBuffScenario.GetString() == "equipment-buff")
            chooser = ChooseEquipmentBuffThenCards;
        if (fixture.TryGetProperty("ModifierScenario", out var unplayedTriggerScenario) &&
            unplayedTriggerScenario.GetString() is "unplayed-card-trigger" or "unplayed-card-effects")
        {
            Func<BattleTurnState, PlayCardAction?> ordinaryChooser = chooser;
            chooser = source =>
            {
                PlayCardAction? selected = ordinaryChooser(source);
                CardInstanceState? selectedCard = selected == null ? null : source.Spawn.Train.Context?.FindCard(selected.CardInstanceId);
                string[] unplayedTriggerIds = source.PlayRules?.Cards.Where(rule => rule.OnUnplayedNegativeEffects.Count > 0)
                    .Select(rule => rule.DataId).Distinct(StringComparer.Ordinal).ToArray() ?? Array.Empty<string>();
                if (selectedCard == null || !unplayedTriggerIds.Contains(selectedCard.DataId, StringComparer.Ordinal)) return selected;
                return BattleActionModel.EnumerateSupportedPlays(source).FirstOrDefault(action =>
                {
                    string? candidateId = source.Spawn.Train.Context?.FindCard(action.CardInstanceId)?.DataId;
                    return candidateId != null && !unplayedTriggerIds.Contains(candidateId, StringComparer.Ordinal);
                });
            };
        }
        FixtureValue turns = fixture.GetProperty("Turns");
        // Scenario actions are checked individually above. Start the independent policy
        // after the last one, including when a scenario injects actions mid-turn.
        FixtureValue[] allActions = actions.EnumerateArray().ToArray();
        int lastScenarioAction = allActions.Where(entry =>
                entry.TryGetProperty("ScenarioAction", out var authored) && authored.GetBoolean())
            .Select(entry => entry.GetProperty("Index").GetInt32()).DefaultIfEmpty(-1).Max();
        FixtureValue[] policyActions = allActions.Where(entry =>
            entry.GetProperty("Index").GetInt32() > lastScenarioAction &&
            (!entry.TryGetProperty("ScenarioAction", out var authored) || !authored.GetBoolean())).ToArray();
        if (fixture.TryGetProperty("ModifierScenario", out var keepOneOnly) &&
            keepOneOnly.GetString() is "card-effect-choose-discard-others" or "card-effect-choose-discard-refine")
        {
            Require(allActions.Length == 1 && policyActions.Length == 0 && turns.GetArrayLength() > 0,
                "The keep-one smelt fixture unexpectedly contains later card plays or no completed turns.");
            Console.WriteLine("NATIVE-POLICY-CHAIN-CHECKS PASS: keep-one smelt followed by natural EndTurns only.");
            return;
        }
        Require(policyActions.Length > 0, "The fixture has no subsequent native policy actions.");
        BattleTurnState root = policyActions[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        FixtureValue[] policyTurns = turns.EnumerateArray().ToArray();
        bool hasScenarioPrelude = actions.EnumerateArray().Any(entry =>
            entry.TryGetProperty("ScenarioAction", out var authored) && authored.GetBoolean());
        if (hasScenarioPrelude)
        {
            int startTurn = root.Spawn.Turn;
            policyTurns = policyTurns.Where(entry => entry.GetProperty("Before").GetProperty("Spawn").GetProperty("Turn").GetInt32() >= startTurn).ToArray();
        }
        // A prepared hand can have no ordinary play on its first turn. That EndTurn
        // is part of the policy, so start before it rather than at a later first play.
        else if (policyTurns.Length > 0)
        {
            BattleTurnState firstDecision = policyTurns[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
            if (firstDecision.Spawn.Turn < root.Spawn.Turn) root = firstDecision;
        }
        string parent = JsonSerializer.Serialize(root);
        BattleTurnState terminal = RunPolicy(root, policyActions, policyTurns, chooser, compareRoomEnabled);
        string expected = BattleTurnChecks.Comparable(terminal, compareRoomEnabled: compareRoomEnabled);
        Parallel.For(0, 16, _ => Require(BattleTurnChecks.Comparable(
            RunPolicy(root, policyActions, policyTurns, chooser, compareRoomEnabled), compareRoomEnabled: compareRoomEnabled) == expected,
            "Full policy parallel branches diverged."));
        Require(JsonSerializer.Serialize(root) == parent, "Full policy simulation mutated its root.");
        // Start independently after an actual mid-battle card action. Recompute the entire suffix.
        FixtureValue mid = policyActions.FirstOrDefault(entry => entry.GetProperty("Actual").GetProperty("Spawn").GetProperty("Turn").GetInt32() > 0);
        // A battle may finish on its first EndTurn. Its later policy root is
        // still independently checkable after the final ordinary card play.
        if (mid.ValueKind == FixtureKind.Undefined) mid = policyActions[^1];
        BattleTurnState midRoot = mid.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        BattleSimulationResult suffix = BattleSimulator.Resolve(midRoot, chooser);
        Require(suffix.Supported && BattleTurnChecks.Comparable(suffix.State!, compareRoomEnabled: compareRoomEnabled) == expected,
            "Mid-battle policy simulation diverged: " + suffix.UnsupportedReason);
        int firstTurn = midRoot.Spawn.Turn;
        FixtureValue[] actualSuffixTurns = policyTurns.Where(entry => entry.GetProperty("Before")
            .GetProperty("Spawn").GetProperty("Turn").GetInt32() >= firstTurn).ToArray();
        Require(suffix.Turns.Count == actualSuffixTurns.Length, "Mid-battle suffix EndTurn count differs.");
        for (int index = 0; index < suffix.Turns.Count; index++)
        {
            BattleTurnState actual = actualSuffixTurns[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            bool terminalTurn = suffix.Turns[index].Outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
            Require(BattleTurnChecks.Comparable(suffix.Turns[index].State!, terminalTurn, compareRoomEnabled) ==
                BattleTurnChecks.Comparable(actual, terminalTurn, compareRoomEnabled),
                "Mid-battle suffix decision differs after EndTurn " + index);
        }
        int hp = terminal.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.IsPyre)?.Health ?? 0;
        Console.WriteLine($"NATIVE-POLICY-CHAIN-CHECKS PASS: {policyActions.Length} policy card plays, {policyTurns.Length} EndTurns, final Pyre {hp}, mid-battle root and 16 parallel branches.");
    }

    private static PlayCardAction? ChooseTypedDrawThenCards(BattleTurnState source)
    {
        foreach (string asset in new[] { "Channelsong", "GiftsForAGuard", "JustCause" })
        foreach (CardToken card in source.Spawn.Train.Context!.Cards.Hand.Where(card =>
            source.PlayRules!.Cards.Any(rule => rule.DataId == card.DataId && rule.AssetKey == asset)))
        {
            var action = new PlayCardAction(card.InstanceId, 0);
            if (BattleActionModel.PlayCard(source, action).Supported) return action;
        }
        return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
    }

    private static PlayCardAction? ChooseRoomCapacityAttachmentThenCards(BattleTurnState source, string cardDataId)
    {
        if (source.Spawn.Train.Context?.RoomCapacities?.Any(room =>
            room.Attachments?.Any(item => item.DataId == cardDataId) == true) == true) return null;
        if (source.PlayRules != null && source.Spawn.Train.Context != null)
        {
            CardToken? card = source.Spawn.Train.Context.Cards.Hand.FirstOrDefault(item => item.DataId == cardDataId);
            if (card != null)
            foreach (RoomCombatState room in source.Spawn.Train.Rooms.Where(item =>
                source.PlayRules.Rooms.Single(rule => rule.RoomIndex == item.RoomIndex).IsPyre == false).OrderBy(item => item.RoomIndex))
            {
                var action = new PlayCardAction(card.InstanceId, room.RoomIndex);
                if (BattleActionModel.PlayCard(source, action).Supported) return action;
            }
        }
        return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
    }

    private static PlayCardAction? ChooseMagebladeThenCards(BattleTurnState source)
    {
        CombatContext? context = source.Spawn.Train.Context;
        if (context?.MagicPower != null && source.PlayRules != null)
        {
            HashSet<int> empoweredRooms = context.MagicPower.Where(power => power.Team == CombatTeam.Player && power.Value > 0)
                .Select(power => power.RoomIndex).ToHashSet();
            IReadOnlyList<PlayCardAction> plays = BattleActionModel.EnumerateSupportedPlays(source);
            foreach (CardToken card in context.Cards.Hand.Where(item => source.PlayRules.Cards.Any(rule => rule.DataId == item.DataId &&
                (rule.SpawnUnit?.Statuses.Any(status => status.Id == "mageblade" && status.Stacks > 0) == true ||
                 rule.SpawnUnit?.StatusRegistry?.Any(status => status.Id == "mageblade" && status.Stacks > 0) == true))))
            foreach (PlayCardAction play in plays.Where(item => item.CardInstanceId == card.InstanceId && empoweredRooms.Contains(item.RoomIndex)))
                return play;
        }
        return BattleActionModel.ChooseUnitAndJunkPlay(source);
    }

    private static PlayCardAction? ChooseEquipmentBuffThenCards(BattleTurnState source)
    {
        PlayCardAction? selected = BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
        if (selected == null || source.PlayRules == null || source.Spawn.Train.Context == null) return selected;
        CardToken? card = source.Spawn.Train.Context.Cards.Hand.FirstOrDefault(item => item.InstanceId == selected.CardInstanceId);
        CardPlayRule? rule = card == null ? null : source.PlayRules.Cards.FirstOrDefault(item => item.DataId == card.DataId);
        if (rule?.Effects.Any(effect => effect.Type == "BuffEquipment") != true) return selected;
        foreach (RoomCombatState room in source.Spawn.Train.Rooms.OrderBy(item => item.RoomIndex))
        foreach (CombatUnit target in room.Units.Where(unit => unit.Team == CombatTeam.Player && unit.EquipmentCards?.Count >= 2)
            .OrderByDescending(unit => unit.EquipmentCards!.Count))
        {
            var action = new PlayCardAction(selected.CardInstanceId, room.RoomIndex, targetUnitId: target.Id);
            if (BattleActionModel.PlayCard(source, action).Supported) return action;
        }
        return selected;
    }

    private static PlayCardAction? ChooseSacrificeEffectsThenCards(BattleTurnState source)
    {
        const string sacrificeCardId = "17f98264-cbc5-46c4-8fd8-8f86c413e228";
        CardToken? sacrifice = source.Spawn.Train.Context!.Cards.Hand.FirstOrDefault(card => card.DataId == sacrificeCardId);
        if (sacrifice != null)
        {
            RoomCombatState room = source.Spawn.Train.Rooms.Single(item => item.RoomIndex == 0);
            CombatUnit? target = room.Units.LastOrDefault(unit => unit.Team == CombatTeam.Player && !unit.IsPyre);
            if (target != null)
            {
                var action = new PlayCardAction(sacrifice.InstanceId, 0, targetUnitId: target.Id);
                if (BattleActionModel.PlayCard(source, action).Supported) return action;
            }
        }
        return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
    }

    private static BattleTurnState RunPolicy(BattleTurnState root, FixtureValue[] actions, FixtureValue[] turns,
        Func<BattleTurnState, PlayCardAction?> chooser, bool compareRoomEnabled)
    {
        // Finish first, then consult the oracle. Neither recorded actions nor the terminal turn count
        // drives the independent simulation; the policy consumes only its current model state.
        BattleSimulationResult result = BattleSimulator.Resolve(root, chooser);
        Require(result.Supported, "Independent card policy failed: " + result.UnsupportedReason);
        if (result.Actions.Count != actions.Length || result.Turns.Count != turns.Length)
        {
            string modeledSequence = string.Join(";", result.Actions.Select(item =>
                $"{item.Action.CardInstanceId}@{item.Action.RoomIndex}:{item.Action.TargetUnitId}"));
            string recordedSequence = string.Join(";", actions.Select(entry =>
            {
                PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
                return $"{action.CardInstanceId}@{action.RoomIndex}:{action.TargetUnitId}";
            }));
            throw new InvalidOperationException(
                $"Independent policy action/turn count differed: {result.Actions.Count}/{actions.Length} actions, " +
                $"{result.Turns.Count}/{turns.Length} EndTurns. Modeled=[{modeledSequence}] Recorded=[{recordedSequence}]");
        }
        for (int index = 0; index < result.Actions.Count; index++)
        {
            BattleSimulationAction modeled = result.Actions[index];
            PlayCardAction actualAction = actions[index].GetProperty("Action").Deserialize<PlayCardAction>()!;
            BattleTurnState actual = actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            string modeledAction = JsonSerializer.Serialize(modeled.Action), nativeAction = JsonSerializer.Serialize(actualAction);
            bool terminalAction = modeled.Result.Outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
            string modeledState = BattleTurnChecks.Comparable(modeled.Result.State!, terminalAction, compareRoomEnabled);
            string nativeState = BattleTurnChecks.Comparable(actual, terminalAction, compareRoomEnabled);
            if (modeledAction != nativeAction || modeledState != nativeState)
                throw new InvalidOperationException("Independent policy diverged after card action " + index + ": " +
                    (ModelJson.Difference(modeledAction, nativeAction) ?? ModelJson.Difference(modeledState, nativeState)));
            if (actions[index].TryGetProperty("ActualOutcome", out FixtureValue outcome))
                Require(modeled.Result.Outcome == (RoomOutcome)outcome.GetInt32(), "Independent policy action outcome differs.");
        }
        for (int index = 0; index < result.Turns.Count; index++)
        {
            BattleTurnState actual = turns[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            bool terminalTurn = result.Turns[index].Outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
            Require(result.Turns[index].Outcome == (RoomOutcome)turns[index].GetProperty("ActualOutcome").GetInt32() &&
                BattleTurnChecks.Comparable(result.Turns[index].State!, terminalTurn, compareRoomEnabled) ==
                    BattleTurnChecks.Comparable(actual, terminalTurn, compareRoomEnabled),
                "Independent policy diverged after EndTurn " + index);
        }
        return result.State!;
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}

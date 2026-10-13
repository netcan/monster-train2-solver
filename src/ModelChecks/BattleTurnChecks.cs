using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using System.Text.Json;

internal static class BattleTurnChecks
{
    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("Turns", out FixtureValue turns) || turns.GetArrayLength() == 0) return;
        bool compareRoomEnabled = ShouldCompareRoomEnabled(fixture);
        int matched = 0, unsupported = 0;
        foreach (FixtureValue turn in turns.EnumerateArray())
        {
            BattleTurnState before = turn.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnResult result = BattleTurnModel.EndTurn(before);
            if (!result.Supported) { unsupported++; continue; }
            BattleTurnState actual = turn.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            bool isTerminalOutcome = result.Outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
            string predictedState = Comparable(result.State!, isTerminalOutcome, compareRoomEnabled);
            string actualState = Comparable(actual, isTerminalOutcome, compareRoomEnabled);
            Require(result.Outcome == (RoomOutcome)turn.GetProperty("ActualOutcome").GetInt32() && predictedState == actualState,
                "Native EndTurn differs at index " + turn.GetProperty("Index").GetInt32() + ": " +
                (predictedState == actualState ? "terminal outcome" : ModelJson.Difference(predictedState, actualState)));
            matched++;
        }
        Require(unsupported == 0, "Native EndTurn verification skipped unsupported turns.");
        Console.WriteLine($"NATIVE-TURN-CHECKS PASS: {matched} matched, {unsupported} unsupported.");
        if (fixture.TryGetProperty("Policy", out FixtureValue policy) &&
            policy.GetString() is "units-and-junk" or "units-spells-and-junk") return;
        BattleTurnState root = turns[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        string parent = JsonSerializer.Serialize(root);
        BattleTurnState terminal = RunIndependentChain(root, turns, compareRoomEnabled);
        Require(JsonSerializer.Serialize(root) == parent, "Full battle simulation mutated its root.");
        string expected = Comparable(terminal, compareRoomEnabled: compareRoomEnabled);
        Parallel.For(0, 16, _ => Require(Comparable(RunIndependentChain(root, turns, compareRoomEnabled),
            compareRoomEnabled: compareRoomEnabled) == expected,
            "Full battle parallel branches diverged."));
        int hp = terminal.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.IsPyre)?.Health ?? 0;
        Console.WriteLine($"NATIVE-FULL-CHAIN-CHECKS PASS: {turns.GetArrayLength()} EndTurns, final Pyre {hp}, independent root and 16 parallel branches.");
    }

    internal static void UnplayedCardTrigger(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("Turns", out FixtureValue turns) || turns.GetArrayLength() == 0)
            throw new InvalidOperationException("Unplayed-card trigger check requires a battle turn snapshot.");
        BattleTurnState root = turns[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        if (root.PlayRules == null || root.Spawn.Train.Context == null)
            throw new InvalidOperationException("Unplayed-card trigger check requires captured rules and context.");
        string parent = JsonSerializer.Serialize(root);
        BattleTurnResult baseline = BattleTurnModel.EndTurn(root);
        Require(baseline.Supported, "Baseline EndTurn failed: " + baseline.UnsupportedReason);
        int baselinePyreHealth = PyreHealth(baseline.State!);
        BattleTurnResult single = BattleTurnModel.EndTurn(AddUnplayedCards(root, ("test-unplayed-2", 2, 1)));
        Require(single.Supported, "Single unplayed Pyre-damage card failed: " + single.UnsupportedReason);
        Require(PyreHealth(single.State!) == baselinePyreHealth - 2,
            "One unplayed negative trigger did not deal its captured Pyre damage exactly once.");
        BattleTurnResult multiple = BattleTurnModel.EndTurn(AddUnplayedCards(root,
            ("test-unplayed-2", 2, 1), ("test-unplayed-3", 3, 0)));
        Require(multiple.Supported, "Multiple unplayed Pyre-damage cards failed: " + multiple.UnsupportedReason);
        Require(PyreHealth(multiple.State!) == baselinePyreHealth - 5,
            "Multiple unplayed negative triggers did not accumulate their Pyre damage.");
        Require(JsonSerializer.Serialize(root) == parent, "Unplayed trigger resolution mutated its parent decision state.");
        Console.WriteLine("UNPLAYED-CARD-TRIGGER-CHECKS PASS: fixed Pyre damage, multiple hand triggers and parent isolation.");

        static int PyreHealth(BattleTurnState state) => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.IsPyre).Health;

        static BattleTurnState AddUnplayedCards(BattleTurnState source, params (string DataId, int Damage, int Order)[] specs)
        {
            CombatContext context = source.Spawn.Train.Context!;
            var tokens = new List<CardToken>(context.Cards.Hand);
            var instances = new List<CardInstanceState>();
            var rules = new List<CardPlayRule>();
            int nextId = context.NextCardId;
            foreach ((string dataId, int damage, int order) in specs)
            {
                tokens.Add(new CardToken(nextId, dataId));
                instances.Add(CardInstanceState.Empty(nextId, dataId));
                rules.Add(new CardPlayRule(dataId, dataId, 0, "Null", "Discard", null, Array.Empty<string>(),
                    onUnplayedNegativeEffects: new[]
                    {
                        new CardActionEffect("Damage", "Pyre", damage, false, true, Array.Empty<CombatStatus>())
                    }, unplayedNegativeOrder: order));
                nextId++;
            }
            var cards = new CardCycleState(tokens, context.Cards.Draw, context.Cards.Discard, context.Cards.Rng,
                context.Cards.DrawModifier, context.Cards.ExternalInteractions, context.Cards.BonusDraw,
                context.Cards.NextDrawUpgrades, context.Cards.NextUpgradeReferenceId);
            CardInstanceState[]? registered = context.CardInstances?.Concat(instances).ToArray();
            CardInstanceState[]? registry = context.CardRegistry?.Concat(instances).ToArray();
            if (registered == null && registry == null) registry = instances.ToArray();
            context = context.WithCards(cards).WithNextCardId(nextId).WithCardInstances(registered).WithCardRegistry(registry);
            TrainCombatState train = CardSpellModel.WithContext(source.Spawn.Train, context);
            EnemySpawnState old = source.Spawn;
            var spawn = new EnemySpawnState(train, old.Waves, old.SelectedGroups, old.Phase, old.Looping, old.Rng,
                train.Context?.NextUnitId ?? old.NextUnitId, old.Treasures, old.TreasuresRemaining, old.TreasureEnabled,
                old.FirstTreasureTurn, old.FirstTreasureRoom, old.Turn, old.ExternalInteractions,
                old.CanonicalDecisionReferences, old.PendingDestroyedUnitIds);
            BattlePlayRules oldRules = source.PlayRules!;
            var playRules = new BattlePlayRules(oldRules.Rooms, oldRules.Cards.Concat(rules).ToArray(), oldRules.StatusRules,
                oldRules.UnitCopyCatalog);
            return new BattleTurnState(spawn, source.Energy, source.EnergyPerTurn, source.DrawPerTurn,
                source.ForgePoints, source.DragonsHoard, source.MoonPhase, source.RngStreams, source.OtherPiles,
                source.ExternalInteractions, playRules, source.BattlePreviewEnabled, source.UiRngIsolated,
                source.CanonicalDecisionReferences, source.CanonicalPhysicalReferences, source.SelectedRoom,
                source.MoonPhaseTrackingActive);
        }
    }

    internal static bool HasRoomEnabledSnapshotConflict(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("Turns", out FixtureValue turns) || turns.ValueKind != FixtureKind.Array ||
            turns.GetArrayLength() == 0) return false;
        foreach (FixtureValue turn in turns.EnumerateArray())
        {
            if (!turn.TryGetProperty("Actual", out FixtureValue actualSnapshot) || actualSnapshot.ValueKind == FixtureKind.Null)
                continue;
            BattleTurnState before = turn.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnResult predicted = BattleTurnModel.EndTurn(before);
            if (!predicted.Supported || predicted.State!.PlayRules == null) continue;
            BattleTurnState actual = actualSnapshot.Deserialize<BattleTurnState>()!;
            if (actual.PlayRules == null) continue;
            foreach (RoomPlayRule room in predicted.State.PlayRules.Rooms)
            {
                RoomPlayRule? actualRoom = actual.PlayRules.Rooms.FirstOrDefault(item => item.RoomIndex == room.RoomIndex);
                if (actualRoom != null && actualRoom.Enabled != room.Enabled) return true;
            }
        }
        return false;
    }

    internal static bool ShouldCompareRoomEnabled(FixtureValue fixture) =>
        fixture.TryGetProperty("DecisionPlayRulesSnapshotsFresh", out FixtureValue freshRules) && freshRules.GetBoolean() ||
        !HasRoomEnabledSnapshotConflict(fixture);

    private static BattleTurnState RunIndependentChain(BattleTurnState root, FixtureValue oracle, bool compareRoomEnabled)
    {
        // All following states come from the model. Native Before/Predicted states are never injected.
        BattleSimulationResult simulation = BattleSimulator.ResolveNoMoreCards(root);
        Require(simulation.Supported && simulation.Turns.Count == oracle.GetArrayLength(),
            "Independent full simulation did not reach the expected terminal turn: " + simulation.UnsupportedReason);
        for (int index = 0; index < simulation.Turns.Count; index++)
        {
            BattleTurnResult result = simulation.Turns[index];
            Require(result.Supported, "Independent full chain rejected turn " + index + ": " + result.UnsupportedReason);
            FixtureValue actualTurn = oracle[index];
            BattleTurnState actual = actualTurn.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            bool terminalTurn = result.Outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
            Require(result.Outcome == (RoomOutcome)actualTurn.GetProperty("ActualOutcome").GetInt32() &&
                Comparable(result.State!, terminalTurn, compareRoomEnabled) == Comparable(actual, terminalTurn, compareRoomEnabled),
                "Independent full chain diverged at turn " + index);
            bool terminal = result.Outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated;
            Require(terminal == (index == oracle.GetArrayLength() - 1), "The independent chain terminated at the wrong turn.");
        }
        return simulation.State!;
    }
    internal static string Comparable(BattleTurnState state, bool terminal = false, bool compareRoomEnabled = true)
    {
        // Room selection and this allocator belong to UI previews, not battle decisions.
        CombatContext? context = ComparableContext(state.Spawn.Train.Context);
        return JsonSerializer.Serialize(new
        {
            Rooms = state.Spawn.Train.Rooms.Select(room => new { room.RoomIndex, room.Deployment, room.Units }).ToArray(),
            Movement = state.Spawn.Train.Movement.OrderBy(rule => rule.UnitId).ToArray(), Context = context,
            state.Spawn.Phase, state.Spawn.SelectedGroups, state.Spawn.Rng, state.Spawn.NextUnitId,
            state.Spawn.TreasuresRemaining, state.Spawn.Turn, state.Energy, state.ForgePoints, state.DragonsHoard,
            state.EnergyPerTurn, state.DrawPerTurn,
            state.MoonPhase, state.MoonPhaseTrackingActive, state.RngStreams, state.OtherPiles,
            PlayRules = terminal ? null : ComparablePlayRules(state.PlayRules, compareRoomEnabled),
            state.BattlePreviewEnabled, state.UiRngIsolated,
            state.CanonicalPhysicalReferences
        });
    }
    private static object? ComparablePlayRules(BattlePlayRules? rules, bool compareRoomEnabled)
    {
        if (rules == null) return null;
        return new
        {
            Rooms = rules.Rooms.Select(room => new
            {
                room.RoomIndex,
                room.PlayerCapacity,
                room.PlayerSlots,
                Enabled = compareRoomEnabled ? (bool?)room.Enabled : null,
                room.SummonBlocked,
                room.IsPyre,
                room.EnemyCapacity
            }),
            rules.Cards,
            rules.StatusRules,
            rules.UnitCopyCatalog
        };
    }
    internal static CombatContext? ComparableContext(CombatContext? context)
    {
        if (context?.QueryFrame is StatisticQueryFrame frame &&
            (frame.SelectedRoomIndex.HasValue || frame.MoonPhaseTrackingActive.HasValue))
            context = context.WithQueryFrame(new StatisticQueryFrame(frame.Energy, frame.RunningCombat, frame.Turn,
                frame.ForgePoints, frame.DragonsHoard, frame.MoonPhase, frame.PyreResurrectionCount, frame.ActiveBattle));
        if (context?.SpawnPoints?.NextPreviewCopyId.HasValue == true)
        {
            BattleSpawnPoints points = context.SpawnPoints;
            context = context.WithSpawnPoints(new BattleSpawnPoints(points.Groups, points.Units));
        }
        return context;
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}

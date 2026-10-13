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
        var originalCards = new[]
        {
            (Id: "186180e9-94e9-47d0-b69c-865b9a6d27a4", Damage: 1),
            (Id: "2785cc8e-d0e8-46c0-84ab-9c1d602525d4", Damage: 3),
            (Id: "3b603b7f-300c-4022-9fc2-275448446e58", Damage: 3),
            (Id: "913694e9-bace-4480-8691-24f699ae583e", Damage: 3),
            (Id: "fbf74ec4-2781-4b99-b6f7-b7d75236da60", Damage: 5)
        };
        foreach (var original in originalCards)
        {
            Require(root.Spawn.Train.Context.Cards.Hand.Any(card => card.DataId == original.Id),
                "An original unplayed Pyre-damage card is missing from the captured EndTurn hand: " + original.Id);
            CardPlayRule? rule = root.PlayRules.Cards.SingleOrDefault(card => card.DataId == original.Id);
            Require(rule?.OnUnplayedNegativeEffects.Count == 1 && rule.OnUnplayedNegativeEffects[0].Type == "Damage" &&
                rule.OnUnplayedNegativeEffects[0].Target == "Pyre" && rule.OnUnplayedNegativeEffects[0].Value == original.Damage &&
                rule.OnUnplayedNegativeEffects[0].Range == null,
                "The captured original unplayed Pyre-damage definition changed: " + original.Id);
        }
        string parent = JsonSerializer.Serialize(root);
        BattleTurnResult baseline = BattleTurnModel.EndTurn(root);
        Require(baseline.Supported, "Baseline EndTurn failed: " + baseline.UnsupportedReason);
        int baselinePyreHealth = PyreHealth(baseline.State!);
        Require(PyreHealth(root) - baselinePyreHealth == 15,
            "The five original unplayed Pyre-damage cards did not apply their 1+3+3+3+5 damage.");
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

    internal static void UnplayedCardEffects(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("Turns", out FixtureValue turns) || turns.GetArrayLength() == 0)
            throw new InvalidOperationException("Unplayed-card effects check requires a battle turn snapshot.");
        BattleTurnState root = turns[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        if (root.PlayRules == null || root.Spawn.Train.Context == null)
            throw new InvalidOperationException("Unplayed-card effects check requires captured rules and context.");

        const string titanCurseId = "17fef621-2bb7-4847-b41d-52a89795c784";
        const string soulSaviorId = "4d1f27af-3122-4a08-87e3-5ef5a14ced94";
        CardPlayRule titanRule = root.PlayRules.Cards.Single(rule => rule.DataId == titanCurseId);
        CardPlayRule soulRule = root.PlayRules.Cards.Single(rule => rule.DataId == soulSaviorId);
        Require(root.Spawn.Train.Context.Cards.Hand.Any(card => card.DataId == titanCurseId) &&
            root.Spawn.Train.Context.Cards.Hand.Any(card => card.DataId == soulSaviorId),
            "The two original unplayed-effect cards are missing from the deployment hand.");
        Require(titanRule.OnUnplayedNegativeEffects.Count == 1 &&
            titanRule.OnUnplayedNegativeEffects[0].Type == "UpgradeDeckSpells" &&
            titanRule.OnUnplayedNegativeEffects[0].Upgrade != null &&
            titanRule.OnUnplayedNegativeEffects[0].Lifetime == "TemporaryUntilEndOfBattle",
            "TitanCurse's deck-spell upgrade definition was not captured.");
        CardActionEffect copyEffect = soulRule.OnUnplayedNegativeEffects.Single();
        Require(copyEffect.Type == "GenerateUpgradedCopy" && copyEffect.Generation?.RequireCopyableSource == true &&
            copyEffect.Generation.DeterministicPoolSelection && copyEffect.Generation.Destination == "DiscardPile",
            "Scourge_SoulSavior's copy definition was not captured.");

        CombatContext beforeContext = root.Spawn.Train.Context;
        CardInstanceState soul = beforeContext.FindCard(beforeContext.Cards.Hand.Single(card => card.DataId == soulSaviorId).InstanceId)!;
        Require(soul.CanBeCopied == true, "Scourge_SoulSavior's native copyability state was not captured as true.");
        CardToken[] cardsBefore = AllCards(beforeContext).GroupBy(card => card.InstanceId).Select(group => group.First()).ToArray();
        var spellTargets = cardsBefore.Select(token => new
            {
                Token = token,
                Rule = root.PlayRules.Cards.FirstOrDefault(rule => rule.DataId == token.DataId),
                Instance = beforeContext.FindCard(token.InstanceId)
            })
            .Where(item => item.Rule?.CardType == "Spell" && item.Instance != null).ToArray();
        Require(spellTargets.Length > 0, "The native deployment snapshot has no modeled spell in its card piles.");

        CardActionEffect deckEffect = titanRule.OnUnplayedNegativeEffects[0];
        var eligibleSpells = spellTargets.Where(item =>
        {
            if (deckEffect.CardFilter == null && deckEffect.UpgradeFilters.Count == 0) return true;
            if (item.Instance!.MaskDescriptor == null) return false;
            CardUpgradeMaskCard view = CardBranchMaskModel.Resolve(item.Instance);
            return (deckEffect.CardFilter == null || CardUpgradeMaskModel.FilterCard(deckEffect.CardFilter, view)) &&
                deckEffect.UpgradeFilters.All(filter => CardUpgradeMaskModel.FilterCard(filter, view));
        }).ToArray();
        Require(eligibleSpells.Length > 0, "No captured spell passes TitanCurse's native upgrade filters.");

        BattleTurnResult result = BattleTurnModel.EndTurn(root);
        Require(result.Supported, "Native EndTurn with unplayed effects failed: " + result.UnsupportedReason);
        CombatContext afterContext = result.State!.Spawn.Train.Context!;
        Require(afterContext.CardRegistry?.Count == beforeContext.CardRegistry?.Count + 1 &&
            AllCards(afterContext).Count(card => card.DataId == soulSaviorId) == AllCards(beforeContext).Count(card => card.DataId == soulSaviorId) + 1,
            "Scourge_SoulSavior did not create exactly one copy of its copyable source card.");
        bool upgradedSpell = eligibleSpells.Any(item =>
        {
            CardInstanceState? after = afterContext.FindCard(item.Token.InstanceId);
            if (after == null) return false;
            string dataId = deckEffect.Upgrade!.DataId;
            string assetKey = deckEffect.Upgrade.AssetKey;
            return after.Temporary.Upgrades.Any(upgrade => upgrade.DataId == dataId && upgrade.AssetKey == assetKey);
        });
        Require(upgradedSpell, "TitanCurse did not add its captured temporary upgrade to an eligible deck spell.");
        Console.WriteLine("UNPLAYED-CARD-EFFECTS-CHECKS PASS: TitanCurse upgrades eligible spells and Soul Savior creates one deterministic copy.");

        static IEnumerable<CardToken> AllCards(CombatContext context)
        {
            foreach (CardToken card in context.Cards.Hand) yield return card;
            foreach (CardToken card in context.Cards.Discard) yield return card;
            foreach (CardToken card in context.Cards.Draw) yield return card;
            foreach (CardToken card in context.OtherPiles!.SelectMany(pile => pile.Cards)) yield return card;
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

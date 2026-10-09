using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using System.Text.Json;

internal static class BattleActionChecks
{
    internal static void Run()
    {
        StandbySlots();
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
            [new("Standby", []), new("Exhausted", []), new("Purged", [])], [], rules);
        string parent = JsonSerializer.Serialize(root);
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
        Require(otherRoom.Supported && otherRoom.State!.Spawn.Train.Rooms[1].Units.Single().Size == 3,
            "An arbitrary decision-turn summon into another room failed.");
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
        Require(BattleActionModel.EnumerateSupportedPlays(root).All(action => action.CardInstanceId != 5),
            "Unsupported effects entered the action enumerator.");
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

    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("Actions", out FixtureValue actions) || actions.GetArrayLength() == 0) return;
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
            string? difference = ModelJson.Difference(BattleTurnChecks.Comparable(result.State!), BattleTurnChecks.Comparable(actual));
            Require(difference == null, "Native card action differs at index " + entry.GetProperty("Index").GetInt32() + ": " + difference);
            supported++;
        }
        Require(unsupported == 0, "Native action verification skipped unsupported actions.");
        Console.WriteLine($"NATIVE-ACTION-CHECKS PASS: {supported} matched, {unsupported} unsupported.");
        if (fixture.TryGetProperty("ModifierScenario", out FixtureValue roomScenario) && roomScenario.GetString() == "room-spells")
            RoomSpellChecks.Native(actions);
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
        if (fixture.TryGetProperty("ModifierScenario", out FixtureValue generationScenario) && generationScenario.GetString() is "generation" or "generation-lethal")
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
            (scenario.GetString() is "dynamic-upgrades" or "sacrifice-upgrades" or "hand-upgrades" or "targeted-hand-upgrades"))
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
                if (scenario.GetString() is "hand-upgrades" or "targeted-hand-upgrades")
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
        if (!fixture.TryGetProperty("Policy", out FixtureValue policy) ||
            policy.GetString() is not ("units-and-junk" or "units-spells-and-junk")) return;
        Func<BattleTurnState, PlayCardAction?> chooser = policy.GetString() == "units-spells-and-junk"
            ? BattleActionModel.ChooseUnitSpellAndJunkPlay : BattleActionModel.ChooseUnitAndJunkPlay;
        if (fixture.TryGetProperty("ModifierScenario", out var scenarioPolicy) && scenarioPolicy.GetString() == "rally-lethal")
            chooser = BattleActionModel.ChooseBossRoomSummonThenCards;
        if (fixture.TryGetProperty("ModifierScenario", out var incantPolicy) && incantPolicy.GetString() is "incant" or "incant-thresholds")
            chooser = BattleActionModel.ChooseEmptySpellThenCards;
        if (fixture.TryGetProperty("ModifierScenario", out var summonScenario) && summonScenario.GetString()?.StartsWith("multi-summon", StringComparison.Ordinal) == true)
            chooser = BattleActionModel.ChooseMultiSummonThenCards;
        if (fixture.TryGetProperty("ModifierScenario", out var abilityScenario) &&
            abilityScenario.GetString() is "ability-activation" or "ability-activation-x" or "ability-activation-lethal" or "ability-effects" or "equipment-abilities")
            chooser = UnitAbilityModel.ChooseAbilityThenCards;
        FixtureValue turns = fixture.GetProperty("Turns");
        // Authored setup plays are checked individually above. The independent policy starts
        // after that prelude and must reproduce every ordinary policy play and EndTurn.
        FixtureValue[] policyActions = actions.EnumerateArray().Where(entry =>
            !entry.TryGetProperty("ScenarioAction", out var authored) || !authored.GetBoolean()).ToArray();
        Require(policyActions.Length > 0, "The fixture has no subsequent native policy actions.");
        BattleTurnState root = policyActions[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        // A prepared hand can have no ordinary play on its first turn. That EndTurn
        // is part of the policy, so start before it rather than at a later first play.
        if (turns.GetArrayLength() > 0)
        {
            BattleTurnState firstDecision = turns[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
            if (firstDecision.Spawn.Turn < root.Spawn.Turn) root = firstDecision;
        }
        string parent = JsonSerializer.Serialize(root);
        BattleTurnState terminal = RunPolicy(root, policyActions, turns, chooser);
        string expected = BattleTurnChecks.Comparable(terminal);
        Parallel.For(0, 16, _ => Require(BattleTurnChecks.Comparable(RunPolicy(root, policyActions, turns, chooser)) == expected,
            "Full policy parallel branches diverged."));
        Require(JsonSerializer.Serialize(root) == parent, "Full policy simulation mutated its root.");
        // Start independently after an actual mid-battle card action. Recompute the entire suffix.
        FixtureValue mid = policyActions.FirstOrDefault(entry => entry.GetProperty("Actual").GetProperty("Spawn").GetProperty("Turn").GetInt32() > 0);
        // A battle may finish on its first EndTurn. Its later policy root is
        // still independently checkable after the final ordinary card play.
        if (mid.ValueKind == FixtureKind.Undefined) mid = policyActions[^1];
        BattleTurnState midRoot = mid.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        BattleSimulationResult suffix = BattleSimulator.Resolve(midRoot, chooser);
        Require(suffix.Supported && BattleTurnChecks.Comparable(suffix.State!) == expected,
            "Mid-battle policy simulation diverged: " + suffix.UnsupportedReason);
        int firstTurn = midRoot.Spawn.Turn;
        FixtureValue[] actualSuffixTurns = turns.EnumerateArray().Where(entry => entry.GetProperty("Before")
            .GetProperty("Spawn").GetProperty("Turn").GetInt32() >= firstTurn).ToArray();
        Require(suffix.Turns.Count == actualSuffixTurns.Length, "Mid-battle suffix EndTurn count differs.");
        for (int index = 0; index < suffix.Turns.Count; index++)
        {
            BattleTurnState actual = actualSuffixTurns[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Require(BattleTurnChecks.Comparable(suffix.Turns[index].State!) == BattleTurnChecks.Comparable(actual),
                "Mid-battle suffix decision differs after EndTurn " + index);
        }
        int hp = terminal.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.IsPyre)?.Health ?? 0;
        Console.WriteLine($"NATIVE-POLICY-CHAIN-CHECKS PASS: {policyActions.Length} policy card plays, {turns.GetArrayLength()} EndTurns, final Pyre {hp}, mid-battle root and 16 parallel branches.");
    }

    private static BattleTurnState RunPolicy(BattleTurnState root, FixtureValue[] actions, FixtureValue turns, Func<BattleTurnState, PlayCardAction?> chooser)
    {
        // Finish first, then consult the oracle. Neither recorded actions nor the terminal turn count
        // drives the independent simulation; the policy consumes only its current model state.
        BattleSimulationResult result = BattleSimulator.Resolve(root, chooser);
        Require(result.Supported, "Independent card policy failed: " + result.UnsupportedReason);
        Require(result.Actions.Count == actions.Length && result.Turns.Count == turns.GetArrayLength(),
            $"Independent policy action/turn count differed: {result.Actions.Count}/{actions.Length} actions, " +
            $"{result.Turns.Count}/{turns.GetArrayLength()} EndTurns.");
        for (int index = 0; index < result.Actions.Count; index++)
        {
            BattleSimulationAction modeled = result.Actions[index];
            PlayCardAction actualAction = actions[index].GetProperty("Action").Deserialize<PlayCardAction>()!;
            BattleTurnState actual = actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            string modeledAction = JsonSerializer.Serialize(modeled.Action), nativeAction = JsonSerializer.Serialize(actualAction);
            string modeledState = BattleTurnChecks.Comparable(modeled.Result.State!), nativeState = BattleTurnChecks.Comparable(actual);
            if (modeledAction != nativeAction || modeledState != nativeState)
                throw new InvalidOperationException("Independent policy diverged after card action " + index + ": " +
                    (ModelJson.Difference(modeledAction, nativeAction) ?? ModelJson.Difference(modeledState, nativeState)));
            if (actions[index].TryGetProperty("ActualOutcome", out FixtureValue outcome))
                Require(modeled.Result.Outcome == (RoomOutcome)outcome.GetInt32(), "Independent policy action outcome differs.");
        }
        for (int index = 0; index < result.Turns.Count; index++)
        {
            BattleTurnState actual = turns[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Require(result.Turns[index].Outcome == (RoomOutcome)turns[index].GetProperty("ActualOutcome").GetInt32() &&
                BattleTurnChecks.Comparable(result.Turns[index].State!) == BattleTurnChecks.Comparable(actual),
                "Independent policy diverged after EndTurn " + index);
        }
        return result.State!;
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}

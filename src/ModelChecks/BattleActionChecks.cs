using MonsterTrain2Poju.Model;
using System.Text.Json;

internal static class BattleActionChecks
{
    internal static void Run()
    {
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
        var context = new CombatContext(cards, rng, 0, 6, 10);
        var pyre = new CombatUnit(1, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, []);
        var train = new TrainCombatState(Enumerable.Range(0, 4).Select(index => new RoomCombatState(index, false,
            index == 3 ? new[] { pyre } : [], [], context)).ToArray(), [], 7, context);
        var spawn = new EnemySpawnState(train, [new EnemyWave([new EnemyGroup([])])], [-1], 0, false, rng, 2, [], 0, false, 2, 1, 3, []);
        var root = new BattleTurnState(spawn, 3, 3, 5, 0, 0, "New", [new("Spawning", 42, rng)],
            [new("Standby", []), new("Exhausted", []), new("Purged", [])], [], rules);
        string parent = JsonSerializer.Serialize(root);
        BattleActionResult first = BattleActionModel.PlayCard(root, new(1, 0, 0));
        Require(first.Supported, first.Reason ?? "First summon rejected");
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
        Console.WriteLine("ACTION-CHECKS PASS: capacity, position, card identity, cost, purge, explicit rejection and parallel isolation.");
    }

    internal static void Native(JsonElement fixture)
    {
        if (!fixture.TryGetProperty("Actions", out JsonElement actions) || actions.GetArrayLength() == 0) return;
        int supported = 0, unsupported = 0;
        foreach (JsonElement entry in actions.EnumerateArray())
        {
            BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>(ModelJson.Options)!;
            PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
            BattleActionResult result = BattleActionModel.PlayCard(before, action);
            if (!result.Supported) { unsupported++; continue; }
            BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>(ModelJson.Options)!;
            if (entry.TryGetProperty("ActualOutcome", out JsonElement actionOutcome))
            {
                Require(result.Outcome == (RoomOutcome)actionOutcome.GetInt32(), "Native card action outcome differs.");
                if (actionOutcome.GetInt32() == (int)RoomOutcome.BattleWon && fixture.GetProperty("Schema").GetInt32() >= 12)
                    TerminalSpellChecks.Native(entry);
            }
            string? difference = ModelJson.Difference(BattleTurnChecks.Comparable(result.State!), BattleTurnChecks.Comparable(actual));
            Require(difference == null, "Native card action differs at index " + entry.GetProperty("Index") + ": " + difference);
            supported++;
        }
        Console.WriteLine($"NATIVE-ACTION-CHECKS PASS: {supported} matched, {unsupported} unsupported.");
        if (fixture.TryGetProperty("ModifierScenario", out JsonElement roomScenario) && roomScenario.GetString() == "room-spells")
            RoomSpellChecks.Native(actions);
        if (fixture.TryGetProperty("ModifierScenario", out JsonElement healingScenario) && healingScenario.GetString() is "healing" or "healing-triggers")
        {
            int healPlays = 0, restored = 0;
            var statuses = new HashSet<string>();
            foreach (JsonElement entry in actions.EnumerateArray())
            {
                BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>(ModelJson.Options)!;
                BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>(ModelJson.Options)!;
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
                var actualStates = actions.EnumerateArray().Select(entry => entry.GetProperty("Actual").Deserialize<BattleTurnState>(ModelJson.Options)!).ToArray();
                var units = actualStates.SelectMany(state => state.Spawn.Train.Rooms).SelectMany(room => room.Units).ToArray();
                Require(units.Any(unit => unit.Triggers.Any(trigger => trigger.Kind == "OnHeal" && trigger.Once && trigger.HasTriggered)),
                    "The native OnHeal fixture never consumed its once-only trigger.");
                Require(units.Any(unit => unit.Statuses.Any(status => status.Id == "silenced") &&
                    unit.Triggers.Any(trigger => trigger.Kind == "OnHeal" && trigger.IgnoreSilence && trigger.HasTriggered) &&
                    unit.Triggers.Where(trigger => trigger.Kind == "OnHeal" && !trigger.IgnoreSilence).All(trigger => !trigger.HasTriggered)),
                    "The native OnHeal fixture did not exercise ignored silence.");
                bool immuneRewards = actions.EnumerateArray().Any(entry =>
                {
                    BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>(ModelJson.Options)!;
                    BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>(ModelJson.Options)!;
                    int id = entry.GetProperty("Action").GetProperty("TargetUnitId").GetInt32();
                    CombatUnit? target = before.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.Id == id);
                    return target?.Statuses.Any(status => status.Id == "heal immunity") == true &&
                        actual.Spawn.Train.Context!.Gold - before.Spawn.Train.Context!.Gold == 15;
                });
                Require(immuneRewards, "Three blocked heals did not produce three native minimum rewards.");
                Console.WriteLine("NATIVE-ONHEAL-COVERAGE PASS: three blocked heal rewards, once-only state, silence and ignored silence.");
            }
        }
        if (fixture.TryGetProperty("ModifierScenario", out JsonElement scenario) &&
            (scenario.GetString() is "dynamic-upgrades" or "sacrifice-upgrades" or "hand-upgrades" or "targeted-hand-upgrades"))
        {
            var modifiedActions = actions.EnumerateArray().Where(entry =>
            {
                BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>(ModelJson.Options)!;
                int id = entry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
                string dataId = before.Spawn.Train.Context!.Cards.Hand.Single(card => card.InstanceId == id).DataId;
                return before.PlayRules!.Cards.Single(card => card.DataId == dataId).Effects.Any(effect => effect.Type is "UnitUpgrade" or "HandUpgrade");
            }).ToArray();
            Require(modifiedActions.Length > 0, "The dynamic upgrade fixture never played its modified spell.");
            bool observed = modifiedActions.Any(entry =>
            {
                BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>(ModelJson.Options)!;
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
        if (unsupported > 0 || !fixture.TryGetProperty("Policy", out JsonElement policy) ||
            policy.GetString() is not ("units-and-junk" or "units-spells-and-junk")) return;
        Func<BattleTurnState, PlayCardAction?> chooser = policy.GetString() == "units-spells-and-junk"
            ? BattleActionModel.ChooseUnitSpellAndJunkPlay : BattleActionModel.ChooseUnitAndJunkPlay;
        JsonElement turns = fixture.GetProperty("Turns");
        BattleTurnState root = actions[0].GetProperty("Before").Deserialize<BattleTurnState>(ModelJson.Options)!;
        string parent = JsonSerializer.Serialize(root);
        BattleTurnState terminal = RunPolicy(root, actions, turns, chooser);
        string expected = BattleTurnChecks.Comparable(terminal);
        Parallel.For(0, 16, _ => Require(BattleTurnChecks.Comparable(RunPolicy(root, actions, turns, chooser)) == expected,
            "Full policy parallel branches diverged."));
        Require(JsonSerializer.Serialize(root) == parent, "Full policy simulation mutated its root.");
        // Start independently after an actual mid-battle card action. Recompute the entire suffix.
        JsonElement mid = actions.EnumerateArray().First(entry => entry.GetProperty("Actual").GetProperty("Spawn").GetProperty("Turn").GetInt32() > 0);
        BattleTurnState midRoot = mid.GetProperty("Actual").Deserialize<BattleTurnState>(ModelJson.Options)!;
        BattleSimulationResult suffix = BattleSimulator.Resolve(midRoot, chooser);
        Require(suffix.Supported && BattleTurnChecks.Comparable(suffix.State!) == expected,
            "Mid-battle policy simulation diverged: " + suffix.UnsupportedReason);
        int firstTurn = midRoot.Spawn.Turn;
        JsonElement[] actualSuffixTurns = turns.EnumerateArray().Where(entry => entry.GetProperty("Before")
            .GetProperty("Spawn").GetProperty("Turn").GetInt32() >= firstTurn).ToArray();
        Require(suffix.Turns.Count == actualSuffixTurns.Length, "Mid-battle suffix EndTurn count differs.");
        for (int index = 0; index < suffix.Turns.Count; index++)
        {
            BattleTurnState actual = actualSuffixTurns[index].GetProperty("Actual").Deserialize<BattleTurnState>(ModelJson.Options)!;
            Require(BattleTurnChecks.Comparable(suffix.Turns[index].State!) == BattleTurnChecks.Comparable(actual),
                "Mid-battle suffix decision differs after EndTurn " + index);
        }
        int hp = terminal.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.IsPyre)?.Health ?? 0;
        Console.WriteLine($"NATIVE-POLICY-CHAIN-CHECKS PASS: {actions.GetArrayLength()} card plays, {turns.GetArrayLength()} EndTurns, final Pyre {hp}, mid-battle root and 16 parallel branches.");
    }

    private static BattleTurnState RunPolicy(BattleTurnState root, JsonElement actions, JsonElement turns, Func<BattleTurnState, PlayCardAction?> chooser)
    {
        // Finish first, then consult the oracle. Neither recorded actions nor the terminal turn count
        // drives the independent simulation; the policy consumes only its current model state.
        BattleSimulationResult result = BattleSimulator.Resolve(root, chooser);
        Require(result.Supported, "Independent card policy failed: " + result.UnsupportedReason);
        Require(result.Actions.Count == actions.GetArrayLength() && result.Turns.Count == turns.GetArrayLength(),
            "Independent policy action/turn count differed.");
        for (int index = 0; index < result.Actions.Count; index++)
        {
            BattleSimulationAction modeled = result.Actions[index];
            PlayCardAction actualAction = actions[index].GetProperty("Action").Deserialize<PlayCardAction>()!;
            BattleTurnState actual = actions[index].GetProperty("Actual").Deserialize<BattleTurnState>(ModelJson.Options)!;
            Require(JsonSerializer.Serialize(modeled.Action) == JsonSerializer.Serialize(actualAction) &&
                BattleTurnChecks.Comparable(modeled.Result.State!) == BattleTurnChecks.Comparable(actual),
                "Independent policy diverged after card action " + index);
            if (actions[index].TryGetProperty("ActualOutcome", out JsonElement outcome))
                Require(modeled.Result.Outcome == (RoomOutcome)outcome.GetInt32(), "Independent policy action outcome differs.");
        }
        for (int index = 0; index < result.Turns.Count; index++)
        {
            BattleTurnState actual = turns[index].GetProperty("Actual").Deserialize<BattleTurnState>(ModelJson.Options)!;
            Require(result.Turns[index].Outcome == (RoomOutcome)turns[index].GetProperty("ActualOutcome").GetInt32() &&
                BattleTurnChecks.Comparable(result.Turns[index].State!) == BattleTurnChecks.Comparable(actual),
                "Independent policy diverged after EndTurn " + index);
        }
        return result.State!;
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}

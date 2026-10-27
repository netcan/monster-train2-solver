using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class BurnoutHealTargetChecks
{
    internal const string AlternativeReason = "Native Purifying Cleanse seed must preserve its expiring damage dealer.";
    internal static void Native(FixtureValue source)
    {
        var row = source.GetProperty("RollingDecisions").EnumerateArray().ElementAt(21);
        var before = row.GetProperty("Root").Deserialize<BattleTurnState>()!;
        var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
        var card = before.Spawn.Train.Context!.FindCard(8)!;
        var hero = before.Spawn.Train.Rooms[2].Units.Single(unit => unit.Id == 2);
        var lady = before.Spawn.Train.Rooms[2].Units.Single(unit => unit.Id == 3);
        Require(before.Spawn.Turn == 3 && action is { CardInstanceId: 8, RoomIndex: 2, TargetUnitId: 2 } &&
            card.DataId == "d4366b7c-588c-49aa-b2c6-2aeb30b0e01b" && hero.Health == hero.MaxHealth &&
            lady.Health == 33 && lady.MaxHealth == 65 && lady.Attack == 39 && lady.Status("ephemeral")?.Stacks == 1,
            "Missing native full-health champion cast beside the expiring39ATK Lady.");
        var recorded = BattleActionModel.PlayCard(before, action);
        Require(recorded.Supported && NativeBranchObservationChecks.Key(recorded.State!) == NativeBranchObservationChecks.Key(actual),
            "Recorded low-value Burnout healing lost its complete native successor/RNG.");
        using var keys = new BattleStateKeys();
        string parent = keys.Key(before);
        foreach (bool tactical in new[] { false, true })
        {
            var choice = Choose(before, tactical);
            Console.WriteLine($"BURNOUT-HEAL-CHOICE tactical={tactical} card={choice?.Action.CardInstanceId} target={choice?.Action.TargetUnitId}");
            Require(choice?.Action is { CardInstanceId: 8, RoomIndex: 2, TargetUnitId: 3 }, "Burnout heal still ignores the expiring damage dealer.");
            var target = choice.Result.State!.Spawn.Train.Rooms[2].Units.Single(unit => unit.Id == 3);
            Require(target.Health == 56 && target.Status("ephemeral")?.Stacks == 10 &&
                BattleActionModel.EnumerateActions(before, 8, includeAbilities: false).SupportedActions.Any(candidate => candidate.TargetUnitId == 2),
                "Preferred heal23/Burnout+9 differs or legal champion cast was pruned.");
            Parallel.For(0, 4, _ => Require(Choose(before, tactical)?.Action.TargetUnitId == 3, "Parallel Burnout-heal target changed."));
        }
        Require(keys.Key(before) == parent, "Burnout healing selection changed its original native root.");
        Console.WriteLine("BURNOUT-HEAL-TARGET PASS: native full-health-champion counterexample/full successor/RNG; both seeds heal33->56 and extend Burnout1->10 on39ATK ally, eight immutable choices and legal champion branch retained.");
        Controls();
    }
    internal static void Alternative(FixtureValue fixture)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, AlternativeReason);
        Require(rows.Length == 22, "Missing21 unchanged natural inputs and preferred healing target.");
        var last = rows[^1];
        var action = last.GetProperty("Action").Deserialize<PlayCardAction>()!;
        var after = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var lady = after.Spawn.Train.Rooms[2].Units.Single(unit => unit.Id == 3);
        Require(action is { CardInstanceId: 8, RoomIndex: 2, TargetUnitId: 3 } && lady.Health == 56 &&
            lady.Attack == 39 && lady.Status("ephemeral")?.Stacks == 10, "Native preferred healing target differs.");
        Console.WriteLine("BURNOUT-HEAL-ALTERNATIVE NATIVE PASS:22 complete inputs/forecasts/RNG, actual39ATK ally healed33->56/Burnout1->10; component strategy witness, not a battle win.");
    }
    private static SupportedActionCandidate? Choose(BattleTurnState source, bool tactical) => tactical
        ? BattleTacticalPolicy.Choose(source, out _) : BattleHeuristicPolicy.Choose(source, out _);

    private static void Controls()
    {
        foreach (int scenario in Enumerable.Range(0, 5))
        {
            var rng = UnityRng.Seed(424246);
            var context = new CombatContext(new CardCycleState([new(1, "heal-burnout")], [], [], rng, 0, []), rng, 0, 2, 10);
            var first = new CombatUnit(2, "first", CombatTeam.Player, 10, scenario is 1 or 4 ? 10 : 30, 30, true, false, false,
                scenario is 2 or 3 ? [new("ephemeral", 1)] : [], modifiers: new UnitModifiers(10, 0, 0, 1, 1, scenario != 4, false, []));
            var second = new CombatUnit(3, "second", CombatTeam.Player, 39, scenario is 2 or 3 ? 10 : 30, 30, true, false, false,
                scenario is 2 or 3 ? [new("ephemeral", scenario == 2 ? 9 : 1)] : [], modifiers: new UnitModifiers(39, 0, 0, 1, 1, true, false, []));
            var rooms = Enumerable.Range(0, 4).Select(index => new RoomCombatState(index, false,
                index == 0 ? [first, second] : index == 3 ?
                    [new CombatUnit(4, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, [])] : [], [], context)).ToArray();
            var train = new TrainCombatState(rooms, [], 5, context);
            var spawn = new EnemySpawnState(train, [new EnemyWave([new EnemyGroup([])])], [-1], 0,
                false, rng, 6, [], 0, false, 2, 1, 1, []);
            var rule = new CardPlayRule("heal-burnout", "heal-burnout", 0, "Spell", "Discard", null, [],
                [new("Heal", "DropTargetCharacter", 15, false, true, []),
                new("AddStatus", "LastTargetedCharacters", 0, true, true, [new("ephemeral", 3)])], cardType: "Spell", isAnyAbility: false);
            var root = new BattleTurnState(spawn, 3, 3, 5, 0, 0, "New", [],
                [new("Standby", []), new("Exhausted", []), new("Purged", [])], [],
                new BattlePlayRules(Enumerable.Range(0, 4).Select(index => new RoomPlayRule(index,
                    index == 3 ? 0 : 5, 7, true, index == 3, index == 3)).ToArray(), [rule]));
            using var keys = new BattleStateKeys();
            string parent = keys.Key(root);
            foreach (bool tactical in new[] { false, true })
            {
                var choice = Choose(root, tactical);
                int expected = scenario is 0 or 4 ? 0 : scenario == 3 ? 3 : 2;
                Require(expected == 0 ? choice == null : choice?.Action.TargetUnitId == expected,
                    $"Burnout heal must defer useless casts, heal injury, preserve immediate expiry and prefer damage on equal clocks; scenario={scenario}, target={choice?.Action.TargetUnitId}.");
                if (choice != null)
                {
                    var direct = BattleActionModel.PlayCard(root, choice.Action);
                    Require(direct.Supported && keys.Key(direct.State!) == keys.Key(choice.Result.State!), "Burnout seed successor differs.");
                }
                var legal = BattleActionModel.EnumerateActions(root, 1, includeAbilities: false);
                Require(legal.SupportedActions.Any(action => action.TargetUnitId == 2) &&
                    legal.SupportedActions.Any(action => action.TargetUnitId == 3) && keys.Key(root) == parent,
                    $"Burnout seed pruned legal targets or changed its original root: scenario={scenario}, legal={string.Join(',', legal.SupportedActions.Select(action => action.TargetUnitId))}, unsupported={string.Join(';', legal.UnsupportedAttempts.Select(attempt => attempt.Reason))}.");
            }
        }
        Console.WriteLine("BURNOUT-HEAL-CONTROLS PASS: both seeds defer full-health non-Burnout and unhealable casts, heal ordinary injury, extend urgent full-health Burnout, prefer damage at equal expiry; full successors, all legal targets and immutable roots retained.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

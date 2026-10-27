using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TargetedDamageChecks
{
    internal const string Reason = "Native damage seed must preserve useful upgraded spell damage and visible weakness.";

    internal static void Native(FixtureValue fixture)
    {
        var row = fixture.GetProperty("RollingDecisions").EnumerateArray().ElementAt(24);
        var before = row.GetProperty("Root").Deserialize<BattleTurnState>()!;
        var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
        var boss = before.Spawn.Train.Rooms[2].Units.Single(unit => unit.Id == 10);
        Require(action is { CardInstanceId: 18, RoomIndex: 0, TargetUnitId: 23 } &&
            before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 23).Health == 3 &&
            boss.Health == 1706 && boss.Status("spell weakness")?.Stacks == 2,
            "Missing native upgraded spell overkill beside visible boss weakness.");
        var recorded = BattleActionModel.PlayCard(before, action);
        Require(recorded.Supported && Key(recorded.State!) == Key(actual), "Original overkill lost its full native successor/RNG.");
        var targeted = new TargetedDamagePolicy(before);
        var rawRule = before.PlayRules!.Cards.Single(rule => rule.DataId == "8de281af-ab9a-4d3d-ac5f-89cd43ff6c84");
        var upgraded = CardModifierModel.Resolve(rawRule, before.Spawn.Train.Context!.FindCard(18)!);
        Require(targeted.TargetValue(new(18, 2, targetUnitId: 10), upgraded, boss) == 1090 &&
            targeted.TargetValue(action, upgraded, before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 23)) == 1027,
            "Visible room Conduit9 and weakness2 must rank90 damage above3 damage/7ATK removal.");
        Require(targeted.TargetValue(new(11, 2, targetUnitId: 10),
            CardModifierModel.Resolve(rawRule, before.Spawn.Train.Context.FindCard(11)!), boss) == null,
            "Temporary Conduit must not move the cheap ping out of its finishing role.");
        using var keys = new BattleStateKeys();
        string parent = keys.Key(before);
        foreach (bool tactical in new[] { false, true })
        {
            BattleTurnState branch = before;
            SupportedActionCandidate? choice = null;
            for (int prefix = 0; prefix < 18; prefix++)
            {
                choice = Choose(branch, tactical);
                if (choice == null) break;
                var card = branch.Spawn.Train.Context!.FindCard(choice.Action.CardInstanceId);
                var definition = branch.PlayRules!.Cards.FirstOrDefault(rule => rule.DataId == card?.DataId);
                int damage = definition?.Effects.FirstOrDefault(effect => effect.Type == "Damage")?.Value ?? 0;
                if (card != null) damage = CardModifierModel.UpgradedStat(damage, "Damage", true, card.Permanent);
                if (choice.Action.ActivatorUnitId == 0 && damage > 3) break;
                branch = choice.Result.State!;
            }
            Console.WriteLine($"TARGETED-DAMAGE-CHOICE tactical={tactical} card={choice?.Action.CardInstanceId} room={choice?.Action.RoomIndex} target={choice?.Action.TargetUnitId}");
            Require(choice?.Action is { } &&
                branch.Spawn.Train.Context!.CardInstances!.Single(card => card.InstanceId == choice.Action.CardInstanceId).DataId ==
                    "8de281af-ab9a-4d3d-ac5f-89cd43ff6c84", "Damage seed still wastes the upgraded spell on3HP.");
            var victim = branch.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == choice.Action.TargetUnitId);
            var remaining = choice.Result.State!.Spawn.Train.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == victim.Id);
            Require(victim.Health - Math.Max(0, remaining?.Health ?? 0) > 3,
                "Preferred targeted spell must preserve useful damage after legal seed abilities.");
            Parallel.For(0, 4, _ => Require(Choose(branch, tactical)?.Action is { } concurrent &&
                concurrent.TargetUnitId == choice.Action.TargetUnitId && concurrent.CardInstanceId == choice.Action.CardInstanceId,
                "Concurrent targeted damage choice changed."));
        }
        Require(BattleActionModel.EnumerateActions(before, 18, includeAbilities: false).SupportedActions.Any(candidate =>
            candidate.RoomIndex == 0 && candidate.TargetUnitId == 23) && keys.Key(before) == parent,
            "Damage ranking pruned the legal small-enemy branch or changed its native parent.");
        Console.WriteLine("TARGETED-DAMAGE-TARGET PASS: native3HP overkill/full successor/RNG, both seeds preserve useful spell damage after their legal abilities, eight immutable concurrent choices and original legal target retained.");
        Controls();
    }

    internal static void Alternative(FixtureValue fixture, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        Require(rows.Length == 25, "Missing24 unchanged natural inputs before alternative damage.");
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(action is { CardInstanceId: 18, RoomIndex: 2, TargetUnitId: 10 } &&
            before.Spawn.Train.Rooms[2].Units.Single(unit => unit.Id == 10).Health == 1706 &&
            after.Spawn.Train.Rooms[2].Units.Single(unit => unit.Id == 10).Health == 1616 &&
            after.Spawn.Train.Rooms[2].Units.Single(unit => unit.Id == 10).Status("spell weakness") == null &&
            after.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 23).Health == 3,
            "Native targeted damage must consume2 weakness for90 damage and preserve the3HP enemy.");
        using var keys = new BattleStateKeys();
        string parent = keys.Key(before);
        Parallel.For(0, 4, _ => Require(Key(BattleActionModel.PlayCard(before, action).State!) == Key(after),
            "Alternative damage differs in an immutable branch."));
        Require(keys.Key(before) == parent, "Alternative damage changed its native parent.");
        if (replay.HasValue)
        {
            NativeBranchObservationChecks.Replay(replay.Value, Reason);
            NativeBranchObservationChecks.Pair(fixture, replay.Value,
                originalPredictionPresent: true, originalPredictionMatched: true);
        }
        Console.WriteLine("TARGETED-DAMAGE-ALTERNATIVE NATIVE PASS:25 complete successors/forecasts/RNG,90 damage/2 weakness consumed, four immutable branches; component witness, not a battle win.");
    }

    private static SupportedActionCandidate? Choose(BattleTurnState source, bool tactical) => tactical
        ? BattleTacticalPolicy.Choose(source, out _) : BattleHeuristicPolicy.Choose(source, out _);

    private static void Controls()
    {
        foreach (int scenario in Enumerable.Range(0, 5))
        {
            var rng = UnityRng.Seed(424247);
            var context = new CombatContext(new CardCycleState([new(1, "damage")], [], [], rng, 0, []), rng, 0, 2, 10);
            var small = new CombatUnit(2, "small", CombatTeam.Enemy, scenario == 1 ? 7 : 1, 3, 3, true, false, false,
                scenario == 4 ? [new("damage shield", 1)] : []);
            var large = new CombatUnit(3, "large", CombatTeam.Enemy, 1, 100, 100, true, false, false,
                scenario is 2 or 4 ? [new("damage shield", 1)] : scenario == 3 ? [new("armor", 50, 1)] : []);
            var rooms = Enumerable.Range(0, 4).Select(index => new RoomCombatState(index, false,
                index == 0 ? [small, large] : index == 3 ?
                    [new CombatUnit(4, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, [])] : [], [], context)).ToArray();
            var train = new TrainCombatState(rooms, [], 5, context);
            var spawn = new EnemySpawnState(train, [new EnemyWave([new EnemyGroup([])])], [-1], 0,
                false, rng, 6, [], 0, false, 2, 1, 1, []);
            var rule = new CardPlayRule("damage", "damage", 0, "Spell", "Discard", null, [],
                [new("Damage", "DropTargetCharacter", scenario == 1 ? 3 : 30, true, false, [])], cardType: "Spell");
            var root = new BattleTurnState(spawn, 3, 3, 5, 0, 0, "New", [],
                [new("Standby", []), new("Exhausted", []), new("Purged", [])], [],
                new BattlePlayRules(Enumerable.Range(0, 4).Select(index => new RoomPlayRule(index,
                    index == 3 ? 0 : 5, 7, true, index == 3, index == 3)).ToArray(), [rule]));
            using var keys = new BattleStateKeys();
            string parent = keys.Key(root);
            foreach (bool tactical in new[] { false, true })
            {
                var choice = Choose(root, tactical);
                int expected = scenario is 1 or 2 or 4 ? 2 : 3;
                Require(choice?.Action.TargetUnitId == expected, $"Damage control{scenario} picked{choice?.Action.TargetUnitId}.");
                var direct = BattleActionModel.PlayCard(root, choice.Action);
                Require(direct.Supported && keys.Key(direct.State!) == keys.Key(choice.Result.State!) && keys.Key(root) == parent,
                    "Damage estimate changed actual effects, RNG or immutable parent.");
                var legal = BattleActionModel.EnumerateActions(root, 1, includeAbilities: false);
                Require(legal.SupportedActions.Any(action => action.TargetUnitId == 2) &&
                    legal.SupportedActions.Any(action => action.TargetUnitId == 3), "Damage estimate pruned a legal target.");
            }
        }
        Console.WriteLine("TARGETED-DAMAGE-CONTROLS PASS: ten choices preserve high-damage use, low-damage threat kills, real injury over shields, armor removal and all-shield fallback; full effects/RNG/immutable roots and both legal targets retained.");
    }
    private static string Key(BattleTurnState state) => NativeBranchObservationChecks.Key(state);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

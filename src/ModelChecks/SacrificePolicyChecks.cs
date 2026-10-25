using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class SacrificePolicyChecks
{
    internal static void Native(FixtureValue fixture)
    {
        var rows = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        var row = rows.Single(item => !item.GetProperty("IsEndTurn").GetBoolean() &&
            item.GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == 14 &&
            item.GetProperty("Action").GetProperty("TargetUnitId").GetInt32() == 2);
        var root = row.GetProperty("Root").Deserialize<BattleTurnState>()!;
        var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(rows.Length == 49 && !fixture.GetProperty("NativeWon").GetBoolean() && root.Spawn.Turn == 5 &&
            root.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 2).Modifiers?.IsChampion == true &&
            actual.Spawn.Train.Rooms.SelectMany(room => room.Units).All(unit => unit.Id != 2),
            "Original native champion-sacrifice counterexample changed.");
        using var keys = new BattleStateKeys();
        string parent = keys.Key(root);
        foreach (bool tactical in new[] { false, true })
        {
            var choice = tactical ? BattleTacticalPolicy.Choose(root, out _) : BattleHeuristicPolicy.Choose(root, out _);
            Require(choice?.Action.CardInstanceId == 14 && choice.Action.TargetUnitId != 2 &&
                choice.Result.State!.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == 2 && unit.Health > 0),
                "Resource sacrifice seed killed its champion or skipped the usable draw.");
            var direct = BattleActionModel.PlayCard(root, choice!.Action);
            Require(direct.Supported && keys.Key(direct.State!) == keys.Key(choice.Result.State!) && keys.Key(root) == parent,
                "Sacrifice candidate replay/state/RNG or immutable parent differs.");
        }
        Require(BattleActionModel.EnumerateActions(root, 14, includeAbilities: false).SupportedActions.Any(action => action.TargetUnitId == 2),
            "Legal champion sacrifice disappeared from full search.");
        Controls();
        Console.WriteLine("SACRIFICE-POLICY PASS: original49-input native defeat retained; both seeds preserve champion at turn5, direct full-state/RNG replay and legal champion sacrifice; four main/on-cast weak-target/absent-target controls.");
    }

    private static void Controls()
    {
        foreach (bool onCast in new[] { false, true })
        foreach (bool weakPresent in new[] { false, true })
        {
            var rng = UnityRng.Seed(424244);
            var context = new CombatContext(new CardCycleState([new(1, "sacrifice")], [], [], rng, 0, []), rng, 0, 2, 10);
            var champion = new CombatUnit(2, "champion", CombatTeam.Player, 100, 40, 40, true, false, false, [],
                modifiers: new UnitModifiers(100, 0, 0, 1, 1, true, false, [], isChampion: true));
            var weak = new CombatUnit(3, "weak", CombatTeam.Player, 1, 1, 1, true, false, false, []);
            var strong = new CombatUnit(4, "strong", CombatTeam.Player, 50, 30, 30, true, false, false, []);
            var pyre = new CombatUnit(5, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, []);
            var rooms = Enumerable.Range(0, 4).Select(index => new RoomCombatState(index, false,
                index == 0 ? weakPresent ? [champion, strong, weak] : [champion] : index == 3 ? [pyre] : [], [], context)).ToArray();
            var train = new TrainCombatState(rooms, [], 6, context);
            var spawn = new EnemySpawnState(train, [new EnemyWave([new EnemyGroup([])])], [-1], 1,
                false, rng, 6, [], 0, false, 2, 1, 1, []);
            CardActionEffect[] effects = [new("Sacrifice", "DropTargetCharacter", 0, allowEnemy: false, allowPlayer: true, [], doNotFullyRemove: false)];
            var rule = new CardPlayRule("sacrifice", "sacrifice", 0, "Spell", "Discard", null, [],
                effects: onCast ? [new("Null", "DropTargetCharacter", 0, false, true, [])] : effects,
                onCastEffects: onCast ? effects : [], cardType: "Spell");
            var rules = new BattlePlayRules(Enumerable.Range(0, 4).Select(index =>
                new RoomPlayRule(index, index == 3 ? 0 : 5, 7, true, index == 3, index == 3)).ToArray(), [rule]);
            var root = new BattleTurnState(spawn, 3, 3, 5, 0, 0, "New", [],
                [new("Standby", []), new("Exhausted", []), new("Purged", [])], [], rules);
            using var keys = new BattleStateKeys();
            string parent = keys.Key(root);
            foreach (bool tactical in new[] { false, true })
            {
                var choice = tactical ? BattleTacticalPolicy.Choose(root, out _) : BattleHeuristicPolicy.Choose(root, out _);
                Require(weakPresent ? choice?.Action.TargetUnitId == 3 : choice == null,
                    "Sacrifice seed must choose weak unit or defer when only the champion remains.");
                var legal = BattleActionModel.EnumerateActions(root);
                var direct = BattleActionModel.PlayCard(root, new PlayCardAction(1, 0, targetUnitId: 2));
                Require(keys.Key(root) == parent && legal.SupportedActions.Any(action => action.TargetUnitId == 2),
                    $"Sacrifice seed changed parent or pruned legal full-search action: onCast={onCast}, weak={weakPresent}, tactical={tactical}, direct={direct.Reason}, omitted={string.Join(";", legal.UnsupportedAttempts.Select(item => item.Reason))}.");
            }
        }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

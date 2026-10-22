using MonsterTrain2Poju.Model;

internal static class TacticalTargetChecks
{
    internal static void Run()
    {
        foreach (bool negative in new[] { true, false })
        foreach (bool onCast in new[] { false, true })
        {
            var status = new CombatStatus(negative ? "melee weakness" : "armor", 2,
                displayCategory: negative ? "Negative" : "Positive");
            CardActionEffect[] effects = [new("AddStatus", "DropTargetCharacter", 0, true, true, [status])];
            var rule = new CardPlayRule("status", "status", 0, "Spell", "Discard", null, [],
                effects: onCast ? [new("Null", "DropTargetCharacter", 0, true, true, [])] : effects,
                onCastEffects: onCast ? effects : [], cardType: "Spell");
            var rng = UnityRng.Seed(424244);
            var context = new CombatContext(new CardCycleState([new(1, "status")], [], [], rng, 0, []), rng, 0, 2, 10);
            var enemy = new CombatUnit(1, "enemy", CombatTeam.Enemy, 8, 20, 20, true, false, false, []);
            var ally = new CombatUnit(2, "ally", CombatTeam.Player, 30, 40, 40, true, false, false, []);
            var pyre = new CombatUnit(3, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, []);
            var trash = new CombatUnit(4, "trash", CombatTeam.Enemy, 1, 1, 1, true, false, false, []);
            var boss = new CombatUnit(5, "boss", CombatTeam.Enemy, 25, 1100, 1100, true, false, true, [], isBoss: true);
            var rooms = Enumerable.Range(0, 4).Select(index => new RoomCombatState(index, false,
                index == 0 ? [enemy, trash, boss, ally] : index == 3 ? [pyre] : [], [], context)).ToArray();
            var train = new TrainCombatState(rooms, [], 7, context);
            var spawn = new EnemySpawnState(train, [new EnemyWave([new EnemyGroup([])])], [-1], 0,
                false, rng, 6, [], 0, false, 2, 1, 1, []);
            var rules = new BattlePlayRules(Enumerable.Range(0, 4).Select(index => new RoomPlayRule(index,
                index == 3 ? 0 : 5, 7, enabled: true, summonBlocked: index == 3, isPyre: index == 3)).ToArray(), [rule]);
            var root = new BattleTurnState(spawn, 3, 3, 5, 0, 0, "New", [],
                [new("Standby", []), new("Exhausted", []), new("Purged", [])], [], rules);
            using var keys = new BattleStateKeys();
            string original = keys.Key(root);
            var chosen = BattleTacticalPolicy.Choose(root, out var omitted);
            int target = negative ? boss.Id : ally.Id;
            var direct = BattleActionModel.PlayCard(root, new PlayCardAction(1, 0, targetUnitId: target));
            if (!direct.Supported) throw new InvalidOperationException("Tactical target control is invalid: " + direct.Reason);
            using var independent = new BattleStateKeys();
            if (chosen == null || chosen.Action.TargetUnitId != target || omitted.Count != 0 ||
                chosen.Result.State!.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == target).Status(status.Id)?.Stacks != 2 ||
                chosen.Result.State.Spawn.Train.Rooms[0].Units.Any(unit => unit.Id != target && unit.Status(status.Id) != null) ||
                independent.Key(root) != original)
                throw new InvalidOperationException($"Tactical status targeted the wrong team or changed its root: negative={negative}, onCast={onCast}, target={chosen?.Action.TargetUnitId}.");
        }
        Console.WriteLine("TACTICAL-TARGET PASS: main/on-cast debuffs prefer the boss over low-HP trash, buffs target allies, other actors and parent state preserved.");
    }
}

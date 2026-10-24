using MonsterTrain2Poju.Model;

internal static class TacticalTargetChecks
{
    internal static void Run()
    {
        foreach (var (negative, damage, roomDamage) in new[] { (true, false, false), (true, true, false),
            (false, false, false), (false, false, true) })
        foreach (bool onCast in new[] { false, true })
        foreach (bool preferredTeamPresent in new[] { true, false })
        {
            var status = new CombatStatus(negative ? "melee weakness" : "armor", 2,
                displayCategory: negative ? "Negative" : "Positive");
            CardActionEffect[] effects = [new(damage ? "Damage" : "AddStatus", "DropTargetCharacter", damage ? 2 : 0,
                true, true, damage ? [] : [status])];
            if (roomDamage) effects = [.. effects, new("Damage", "Room", 2, false, true, [])];
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
                index == 0 ? preferredTeamPresent ? [enemy, trash, boss, ally] : negative ? [ally] : [enemy, trash, boss]
                    : index == 3 ? [pyre] : [], [], context)).ToArray();
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
            if (!preferredTeamPresent)
            {
                // These cross-team casts are legal and must remain searchable
                // for sacrifice/Incant combinations. The ordinary seed should
                // not spend its next input debuffing an ally or buffing a boss
                // merely because its intended target is absent.
                int wrongTarget = negative ? ally.Id : boss.Id;
                var legal = BattleActionModel.EnumerateActions(root);
                if (!legal.SupportedCandidates.Any(candidate => candidate.Action.TargetUnitId == wrongTarget) ||
                    chosen != null || omitted.Count != 0 || keys.Key(root) != original)
                    throw new InvalidOperationException($"Missing tactical target must skip the card while preserving legal branches: negative={negative}, onCast={onCast}, target={chosen?.Action.TargetUnitId}.");
                continue;
            }
            int target = negative ? damage ? trash.Id : boss.Id : ally.Id;
            var direct = BattleActionModel.PlayCard(root, new PlayCardAction(1, 0, targetUnitId: target));
            if (!direct.Supported) throw new InvalidOperationException("Tactical target control is invalid: " + direct.Reason);
            using var independent = new BattleStateKeys();
            if (chosen == null || chosen.Action.TargetUnitId != target || omitted.Count != 0 ||
                independent.Key(chosen.Result.State!) != independent.Key(direct.State!) ||
                independent.Key(root) != original)
                throw new InvalidOperationException($"Tactical status targeted the wrong team or changed its root: negative={negative}, onCast={onCast}, target={chosen?.Action.TargetUnitId}.");
        }
        Console.WriteLine("TACTICAL-TARGET PASS: 16 main/on-cast status/damage/mixed-effect cases; absent targets defer the seed without pruning legal cross-team branches; immutable full-state/RNG replays.");
    }
}

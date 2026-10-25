using MonsterTrain2Poju.Model;
using MonsterTrain2Poju.Fixtures;
using System.Text.Json;

internal static class TacticalTargetChecks
{
    internal static void NativeAbilityBoundary(FixtureValue fixture)
    {
        BattleTurnState root = fixture.GetProperty("Rows").EnumerateArray().Last().GetProperty("Before").Deserialize<BattleTurnState>()!;
        using var keys = new BattleStateKeys();
        string parent = keys.Key(root);
        BattleTurnState current = root;
        SupportedActionCandidate? choice = null;
        int handPlays = 0;
        while (handPlays < 18 && (choice = BattleTacticalPolicy.Choose(current, out _))?.Action.ActivatorUnitId == 0)
        {
            current = choice.Result.State!;
            handPlays++;
        }
        CombatUnit? target = current.Spawn.Train.Rooms.SelectMany(room => room.Units).FirstOrDefault(unit => unit.Id == choice?.Action.TargetUnitId);
        if (choice?.Action.ActivatorUnitId != 2 || target?.Team != CombatTeam.Enemy || target.Id != 10 ||
            choice.Result.State!.Spawn.Train.Rooms.SelectMany(room => room.Units).All(unit => unit.Id != 2) || keys.Key(root) != parent)
            throw new InvalidOperationException($"Tactical ability must hit the low-health enemy and preserve its champion; chosen={JsonSerializer.Serialize(choice?.Action)}.");
        PlayCardAction self = new(27, 2, targetUnitId: 2, activatorUnitId: 2);
        if (!BattleActionModel.EnumerateActions(current).SupportedActions.Any(action =>
            JsonSerializer.Serialize(action) == JsonSerializer.Serialize(self)))
            throw new InvalidOperationException("The legal self-targeted ability must remain available to the full search.");
        Console.WriteLine($"TACTICAL-ABILITY-TARGET PASS: native failure root plus {handPlays} legal hand plays targets enemy10, preserves champion2, keeps the legal sacrifice branch and immutable parent; model policy control.");
    }

    internal static void Run()
    {
        FrontDamageTargets();
        AbilityTargets();
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

    private static void FrontDamageTargets()
    {
        // A large hidden backline must not make its one-HP front look better
        // than a real five-damage hit, nor should an empty preferred floor win.
        var rng = UnityRng.Seed(424244);
        var context = new CombatContext(new CardCycleState([new(1, "front")], [], [], rng, 0, []), rng, 0, 2, 10);
        var weakFront = new CombatUnit(1, "front", CombatTeam.Enemy, 1, 1, 1, true, false, false, []);
        var backline = new CombatUnit(2, "back", CombatTeam.Enemy, 1, 100, 100, true, false, false, []);
        var target = new CombatUnit(3, "target", CombatTeam.Enemy, 1, 7, 7, true, false, false, []);
        var rooms = Enumerable.Range(0, 3).Select(index => new RoomCombatState(index, false,
            index == 0 ? [weakFront, backline] : index == 1 ? [target] : [], [], context)).ToArray();
        var train = new TrainCombatState(rooms, [], 4, context);
        var spawn = new EnemySpawnState(train, [new EnemyWave([new EnemyGroup([])])], [-1], 1,
            false, rng, 4, [], 0, false, 2, 1, 1, []);
        var rule = new CardPlayRule("front", "front", 0, "Spell", "Discard", null, [],
            effects: [new("Damage", "FrontInRoom", 5, allowEnemy: true, allowPlayer: false, [])], cardType: "Spell");
        var rules = new BattlePlayRules(Enumerable.Range(0, 3).Select(index => new RoomPlayRule(index, 5, 7, enabled: true, summonBlocked: false, isPyre: false)).ToArray(), [rule]);
        var root = new BattleTurnState(spawn, 3, 3, 5, 0, 0, "New", [],
            [new("Standby", []), new("Exhausted", []), new("Purged", [])], [], rules);
        using var keys = new BattleStateKeys();
        string parent = keys.Key(root);
        foreach (bool tactical in new[] { false, true })
        {
            var choice = tactical ? BattleTacticalPolicy.Choose(root, out _) : BattleHeuristicPolicy.Choose(root, out _);
            var expected = BattleActionModel.PlayCard(root, new PlayCardAction(1, 1));
            if (choice?.Action.RoomIndex != 1 || !expected.Supported ||
                keys.Key(choice.Result.State!) != keys.Key(expected.State!) ||
                choice.Result.State!.Spawn.Train.Rooms.Single(room => room.RoomIndex == 1).Units.Single().Health != 2 ||
                keys.Key(root) != parent || !BattleActionModel.EnumerateActions(root).SupportedActions.Any(action => action.RoomIndex == 2))
                throw new InvalidOperationException($"Front damage must hit a real front, preserve the untouched backline/root and retain legal empty-room branches: tactical={tactical}, choice={JsonSerializer.Serialize(choice?.Action)}, direct={expected.Reason}.");
        }
        Console.WriteLine("TACTICAL-FRONT-DAMAGE PASS: both seeds choose five damage over a one-HP front/large backline or empty preferred floor; full successor/RNG and legal empty-room branch preserved.");
    }

    private static void AbilityTargets()
    {
        foreach (bool damage in new[] { false, true })
        foreach (bool preferredTeamLegal in new[] { false, true })
        {
            BattleTurnState original = UnitAbilityChecks.Root();
            var effects = new[] { new CardActionEffect(damage ? "Damage" : "AddStatus", "DropTargetCharacter", damage ? 1 : 0,
                damage ? preferredTeamLegal : true, damage || preferredTeamLegal,
                damage ? [] : [new CombatStatus("armor", 2, displayCategory: "Positive")]) };
            var rules = new BattlePlayRules(original.PlayRules!.Rooms.Where(rule => original.Spawn.Train.Rooms.Any(room => room.RoomIndex == rule.RoomIndex)).ToArray(),
                [new CardPlayRule("skill", "skill", 1, "Spell", "Discard", null, [],
                effects, upgradeInteractions: [], ability: new("Unit", false, true), cardType: "Spell", isAnyAbility: true)]);
            var root = new BattleTurnState(original.Spawn, original.Energy, original.EnergyPerTurn, original.DrawPerTurn,
                original.ForgePoints, original.DragonsHoard, original.MoonPhase, original.RngStreams, original.OtherPiles,
                original.ExternalInteractions, rules);
            using var keys = new BattleStateKeys();
            string parent = keys.Key(root);
            SupportedActionCandidate? choice = BattleTacticalPolicy.Choose(root, out _);
            int wanted = damage ? 11 : 10, unwanted = damage ? 10 : 11;
            if ((preferredTeamLegal ? choice?.Action.TargetUnitId != wanted : choice != null) ||
                !BattleActionModel.EnumerateActions(root).SupportedActions.Any(action => action.TargetUnitId == unwanted) || keys.Key(root) != parent)
                throw new InvalidOperationException($"Tactical ability targeted the wrong team or pruned a legal branch: damage={damage}, preferredLegal={preferredTeamLegal}, chosen={choice?.Action.TargetUnitId}.");
        }
        Console.WriteLine("TACTICAL-ABILITY-TEAMS PASS: four targeted damage/buff and absent-legal-target controls; legal cross-team activations retained, immutable parents.");
    }
}

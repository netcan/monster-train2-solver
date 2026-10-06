using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class RoomSpellChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(92);
        var context = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 1, 10, cardInstances: []);
        CombatUnit Unit(int id, CombatTeam team, int hp, bool healable = true, params CombatStatus[] statuses) =>
            new(id, "unit-" + id, team, 3, hp, 10, true, false, false, statuses, size: 1,
                modifiers: new(3, 0, 0, 1, 1, healable, false, []));
        RoomCombatState Room(params CombatUnit[] units) => new(0, false, units, [], context);
        CardActionEffect Effect(string type, string mode, int value = 0, bool enemy = true, bool player = true,
            CardEffectTests? tests = null) => new(type, mode, value, enemy, player,
                type == "AddStatus" ? [new("armor", 1, 1, removeWhenTriggered: true)] : [], tests: tests);
        var room = Room(Unit(20, CombatTeam.Player, 5), Unit(10, CombatTeam.Enemy, 1), Unit(21, CombatTeam.Player, 5),
            Unit(11, CombatTeam.Enemy, 7), Unit(12, CombatTeam.Enemy, 1, statuses: [new("untouchable", 1)]),
            Unit(22, CombatTeam.Player, 3, statuses: [new("stealth", 1)]));
        int[] Targets(string mode, bool enemy = true, bool player = true) =>
            CardTargetModel.Collect(room, Effect("Damage", mode, enemy: enemy, player: player), []).UnitIds.ToArray();
        Require(Targets("Room").SequenceEqual([10, 11, 20, 21, 22]), "Room collection order, untouchable or spell stealth differs.");
        Require(Targets("FrontInRoom").SequenceEqual([10]) && Targets("BackInRoom").SequenceEqual([22]) &&
            Targets("Weakest").SequenceEqual([10]), "Both-team front/back/weakest selection differs.");
        Require(Targets("Weakest", false, true).SequenceEqual([22]) &&
            CardTargetModel.Collect(Room(Unit(20, CombatTeam.Player, 5), Unit(21, CombatTeam.Player, 5)),
                Effect("Damage", "Weakest"), []).UnitIds.SequenceEqual([20]), "Weakest tie did not retain native first-target order.");
        var healRoom = Room(Unit(20, CombatTeam.Player, 10), Unit(21, CombatTeam.Player, 5, false));
        Require(CardTargetModel.Collect(healRoom, Effect("Heal", "RoomHealTargets"), []).UnitIds.SequenceEqual([20]),
            "Healing targets excluded a full-health unit or included an unhealable unit.");
        Require(CardTargetModel.Collect(room, Effect("AddStatus", "LastTargetedCharacters"), [12, 22, 999]).UnitIds.SequenceEqual([12, 22]),
            "Last-target collection incorrectly reapplied normal untouchable filters.");
        Require(CardTargetModel.Collect(room, Effect("Damage", "StrongestLastTargetedCharacters", enemy: false), [10, 11]).UnitIds.SequenceEqual([11]),
            "Strongest-last incorrectly applied a team filter.");
        Require(CardTargetModel.Collect(Room(), Effect("Damage", "StrongestLastTargetedCharacters"), [10, 11]).UnitIds.SequenceEqual([10]),
            "Strongest-last dropped all dead references instead of retaining the first tie.");
        Require(CardTargetModel.Collect(room, Effect("Damage", "LastTargetedCharacters"), [10], firstEffect: true).UnitIds.Count == 0,
            "First effect consumed stale last-target history.");

        CardActionEffect[] chain = [Effect("Damage", "Room", 1, player: false), Effect("Heal", "Room", 1, enemy: false),
            Effect("AddStatus", "LastTargetedCharacters")];
        string parent = JsonSerializer.Serialize(room);
        RoomCombatResult child = CardSpellModel.Apply(room, chain, 0);
        Require(child.Supported && !child.State!.Units.Any(unit => unit.Id == 10) &&
            child.State.Units.Single(unit => unit.Id == 11).Statuses.Any(status => status.Id == "armor") &&
            child.State.Units.Where(unit => unit.Team == CombatTeam.Player).All(unit => unit.Statuses.All(status => status.Id != "armor")) &&
            child.State.Units.Single(unit => unit.Id == 20).Health == 6,
            "A later room effect replaced the initial last-target group or a dead target was reused.");
        var retarget = CardSpellModel.Apply(room, [Effect("Damage", "Room", player: false),
            Effect("Damage", "DropTargetCharacter", enemy: false), Effect("AddStatus", "LastTargetedCharacters")], 21);
        Require(retarget.Supported && retarget.State!.Units.Single(unit => unit.Id == 21).Statuses.Any(status => status.Id == "armor") &&
            retarget.State.Units.Single(unit => unit.Id == 11).Statuses.Count == 0, "Drop did not replace the whole last-target group.");

        var health = new CardUpgradeModifier("group-hp", "group-hp", new(health: 1), [], false, false, false, 2, 0, []);
        var groupRoom = Room(Unit(20, CombatTeam.Player, 3), Unit(21, CombatTeam.Player, 7));
        var upgraded = CardSpellModel.Apply(groupRoom, [new("UnitUpgrade", "Room", 0, false, true, [], health, "TemporaryUntilUnitDeath"),
            Effect("Heal", "RoomHealTargets", 999, false, true)], 0);
        Require(upgraded.Supported && upgraded.State!.Units.All(unit => unit.MaxHealth == 13 && unit.Health == 13 &&
            unit.Modifiers!.Upgrades.Count == 1), "Area upgrade/healing did not update every collected unit: " + upgraded.UnsupportedReason);

        var empty = Room();
        Require(CardSpellModel.TestPlay(empty, [Effect("Damage", "Room")], 0).CanPlay &&
            CardSpellModel.TestPlay(empty, [Effect("Heal", "Room")], 0).CanPlay &&
            !CardSpellModel.TestPlay(empty, [Effect("Heal", "FrontInRoom")], 0).CanPlay,
            "Empty-room and explicit single-target cast tests differ.");
        Require(CardSpellModel.TestPlay(empty, [Effect("Heal", "FrontInRoom"), Effect("Damage", "Room")], 0).CanPlay &&
            !CardSpellModel.TestPlay(empty, [Effect("Heal", "FrontInRoom", tests: new(true, true, false, false)),
                Effect("Damage", "Room")], 0).CanPlay, "Casting did not distinguish any success from a mandatory failure.");
        Require(!CardSpellModel.TestPlay(empty, [Effect("Damage", "Room", tests: new(false, false, false, false))], 0).CanPlay &&
            CardSpellModel.TestPlay(empty, [Effect("AddStatus", "FrontInRoom")], 0).CanPlay &&
            !CardSpellModel.TestPlay(empty, [Effect("AddStatus", "FrontInRoom", tests: new(true, false, false, true))], 0).CanPlay,
            "Disabled casting tests or strict status targets differ.");
        CardActionEffect[] canceled = [Effect("Damage", "Room", 99, player: false),
            Effect("Heal", "LastTargetedCharacters", tests: new(true, false, true, false)),
            Effect("Damage", "Room", 2, enemy: false)];
        var cancelRoom = Room(Unit(10, CombatTeam.Enemy, 1), Unit(20, CombatTeam.Player, 5));
        Require(CardSpellModel.TestPlay(cancelRoom, canceled, 0).CanPlay &&
            CardSpellModel.Apply(cancelRoom, canceled, 0).State!.Units.Single().Health == 5 &&
            CardSpellModel.Apply(cancelRoom, [canceled[0], Effect("Heal", "LastTargetedCharacters"), canceled[2]], 0)
                .State!.Units.Single().Health == 3, "Runtime cancellation did not retain prior effects and skip subsequent effects.");
        Require(!CardSpellModel.Apply(room, [Effect("Damage", "Room"), Effect("Damage", "Tower")], 0).Supported &&
            !CardSpellModel.TestPlay(room, [Effect("Damage", "Room", tests: new(false, false, false, false)),
                Effect("Damage", "LastTargetedCharacters")], 0).Supported,
            "Unknown targeting or uncaptured casting history returned a search state.");
        var unknownHealability = new CombatUnit(1, "unknown", CombatTeam.Player, 3, 5, 10, true, false, false, []);
        Require(!CardSpellModel.Apply(Room(unknownHealability), [Effect("Heal", "RoomHealTargets", 1)], 0).Supported,
            "Unknown healability returned a search child.");
        // Legality reaches the action enumerator before cost, pile or statistic changes.
        var actionCards = new CardCycleState([new(1, "area"), new(2, "single-heal")], [], [], rng, 0, []);
        var actionContext = new CombatContext(actionCards, rng, 0, 3, 10);
        var pyre = new CombatUnit(1, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, []);
        var train = new TrainCombatState([new(0, false, [], [], actionContext), new(1, false, [pyre], [], actionContext)], [], 3, actionContext);
        var spawn = new EnemySpawnState(train, [new EnemyWave([new EnemyGroup([])])], [-1], 0, false, rng, 2, [], 0, false, 2, 1, 1, []);
        var rules = new BattlePlayRules([new(0, 5, 7, true, false, false), new(1, 0, 7, true, true, true)],
            [new("area", "area", 1, "Spell", "Discard", null, [], [Effect("Damage", "Room")]),
             new("single-heal", "single-heal", 1, "Spell", "Discard", null, [], [Effect("Heal", "FrontInRoom")])]);
        var actionRoot = new BattleTurnState(spawn, 3, 3, 5, 0, 0, "New", [new("Spawning", 92, rng)],
            [new("Standby", []), new("Exhausted", []), new("Purged", [])], [], rules);
        string actionParent = JsonSerializer.Serialize(actionRoot);
        var legal = BattleActionModel.PlayCard(actionRoot, new(1, 0));
        Require(legal.Supported && legal.State!.Energy == 2 && legal.State.Spawn.Train.Context!.Cards.Discard.Single().InstanceId == 1 &&
            BattleActionModel.PlayCard(actionRoot, new(2, 0)).Rejection == ActionRejection.Illegal &&
            BattleActionModel.PlayCard(actionRoot, new(1, 0, targetUnitId: 1)).Rejection == ActionRejection.Illegal &&
            BattleActionModel.EnumerateSupportedPlays(actionRoot).All(action => action.CardInstanceId != 2),
            "Area spell action legality, energy, discard or enumeration differs.");
        Require(JsonSerializer.Serialize(actionRoot) == actionParent, "An illegal spell action mutated its parent.");
        var boss = new CombatUnit(30, "boss", CombatTeam.Enemy, 7, 1, 125, true, false, true, []);
        var terminalRoom = Room(boss, Unit(20, CombatTeam.Player, 5));
        Require(CardSpellModel.Apply(terminalRoom, [Effect("Damage", "Room", 1, player: false),
            Effect("AddStatus", "LastTargetedCharacters", player: false)], 0).Outcome == RoomOutcome.BattleWon &&
            !CardSpellModel.Apply(terminalRoom, [Effect("Damage", "Room", 1, player: false),
                Effect("Damage", "Room", 2, enemy: false)], 0).Supported &&
            !CardSpellModel.Apply(Room(boss, Unit(31, CombatTeam.Enemy, 5)), [Effect("Damage", "Room", 1, player: false)], 0).Supported,
            "Uncaptured terminal coroutine timing returned a partial search state.");
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(CardSpellModel.Apply(room, chain, 0).State) ==
            JsonSerializer.Serialize(child.State), "Parallel group spell branches differ."));
        Require(JsonSerializer.Serialize(room) == parent, "Group spells mutated their parent.");
        Console.WriteLine("ROOM-SPELL-CHECKS PASS: collection order/filters/ties, sticky/drop last groups, dead references, area upgrades/healing, cast tests, cancellation, action legality and parallel isolation.");
    }

    internal static void Native(JsonElement actions)
    {
        int areaPlays = 0, multipleHealed = 0, multipleDamaged = 0, enemyFollowups = 0;
        foreach (JsonElement entry in actions.EnumerateArray())
        {
            BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>(ModelJson.Options)!;
            BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>(ModelJson.Options)!;
            PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
            string dataId = before.Spawn.Train.Context!.Cards.Hand.Single(card => card.InstanceId == action.CardInstanceId).DataId;
            CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == dataId);
            if (!rule.Effects.Any(effect => effect.Target == "RoomHealTargets")) continue;
            areaPlays++;
            Require(action.TargetUnitId == 0 && rule.Effects.All(effect => effect.Tests != null), "Native area spell lacks targetless action or cast metadata.");
            CombatUnit[] oldUnits = before.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex).Units.ToArray();
            CombatUnit[] newUnits = actual.Spawn.Train.Rooms.Single(room => room.RoomIndex == action.RoomIndex).Units.ToArray();
            int changedPlayers = oldUnits.Count(unit => unit.Team == CombatTeam.Player && newUnits.Any(next => next.Id == unit.Id &&
                next.MaxHealth == unit.MaxHealth + 4 && next.Health == next.MaxHealth));
            int damagedEnemies = oldUnits.Count(unit => unit.Team == CombatTeam.Enemy &&
                (!newUnits.Any(next => next.Id == unit.Id) || newUnits.Single(next => next.Id == unit.Id).Health < unit.Health));
            if (changedPlayers >= 2) multipleHealed++;
            if (damagedEnemies >= 2) multipleDamaged++;
            if (newUnits.Any(unit => unit.Team == CombatTeam.Enemy && unit.Statuses.Any(status => status.Id == "pyregel"))) enemyFollowups++;
            Require(newUnits.Where(unit => unit.Team == CombatTeam.Player).All(unit => unit.Statuses.All(status => status.Id != "pyregel")),
                "A later friendly-room effect incorrectly replaced the initial enemy last-target group.");
        }
        Require(areaPlays > 0 && multipleHealed > 0 && multipleDamaged > 0 && enemyFollowups > 0,
            "The room-spell oracle lacks multiple-unit healing/damage or a surviving enemy last-target follow-up.");
        Console.WriteLine($"NATIVE-ROOM-SPELL-COVERAGE PASS: {areaPlays} area plays, {multipleHealed} multi-unit heals, {multipleDamaged} multi-enemy damage plays, {enemyFollowups} surviving enemy follow-ups.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}

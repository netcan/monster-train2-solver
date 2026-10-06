using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class HandUpgradeChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(1);
        CardUpgradeModifier Make(string id, int attack, int hp, int cost, bool unique = false) =>
            new(id, id, new(damage: attack, health: hp, cost: cost), [], false, unique, false, 0, 0, []);
        var temporary = Make("temporary", 2, 3, 0);
        var unique = Make("unique", 1, 1, -1, true);
        var permanent = Make("permanent", 1, 2, 0);
        CardActionEffect Hand(CardUpgradeModifier upgrade, string lifetime = "TemporaryUntilEndOfBattle") =>
            new("HandUpgrade", "Hand", 0, false, false, [], upgrade, lifetime);
        CardActionEffect[] effects = [Hand(temporary), Hand(temporary), Hand(unique), Hand(unique), Hand(permanent, "Permanent")];
        var unit = new CombatUnit(0, "unit", CombatTeam.Player, 8, 25, 25, true, false, false, [], size: 3);
        var rules = new BattlePlayRules([new(0, 5, 5, true, false, false), new(1, 5, 5, true, false, true)], [
            new("boost", "boost", 2, "Spell", "Discard", null, [], effects, []),
            new("unit", "unit", 1, "SpawnMonster", "Standby", unit, [], upgradeInteractions: []),
            new("future", "future", 0, "UnimplementedAbility", "Discard", null, ["Unit ability"], upgradeInteractions: [])]);
        var context = new CombatContext(new CardCycleState([new(1, "boost"), new(2, "unit"), new(3, "future")],
            [new(4, "unit")], [new(5, "unit")], rng, 0, []), rng, 0, 6, 10,
            cardInstances: [CardInstanceState.Empty(1, "boost"), CardInstanceState.Empty(2, "unit"),
                CardInstanceState.Empty(3, "future"), CardInstanceState.Empty(4, "unit"), CardInstanceState.Empty(5, "unit")]);
        var room = new RoomCombatState(0, false, [], [], context);
        var pyre = new CombatUnit(1, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, []);
        var train = new TrainCombatState([room, new(1, false, [pyre], [], context)], [], 5, context);
        var spawn = new EnemySpawnState(train, [], [], 0, false, rng, 2, [], 0, false, 0, 0, 0, []);
        var root = new BattleTurnState(spawn, 2, 3, 5, 0, 0, "Full", [], [new("Standby", []), new("Exhausted", [])], [], rules);
        string parent = JsonSerializer.Serialize(root);
        var played = BattleActionModel.PlayCard(root, new(1, 0));
        Require(played.Supported, "Targetless hand upgrade rejected: " + played.Reason);
        var result = played.State!.Spawn.Train.Context!;
        var instances = result.CardInstances!;
        Require(instances.Where(card => card.InstanceId is 2 or 3).All(card => card.Permanent.Upgrades.Count == 1 && card.Temporary.Upgrades.Count == 3) &&
            instances.Where(card => card.InstanceId == 1 || card.InstanceId >= 4).All(card => card.Permanent.Upgrades.Count == 0 && card.Temporary.Upgrades.Count == 0),
            "Hand membership, playing-card exclusion or unique upgrade grouping differs.");
        Require(played.State.Energy == 0 && instances.Single(card => card.InstanceId == 1).LastPlayedCost == 2,
            "An upgrade changed the already paid cost.");
        var summoned = BattleActionModel.PlayCard(played.State, new(2, 0));
        Require(summoned.Supported && summoned.State!.Spawn.Train.Rooms[0].Units.Single().BaseAttack == 14 &&
            summoned.State.Spawn.Train.Rooms[0].Units.Single().MaxHealth == 34, "A hand upgrade did not affect the later summon.");
        Require(BattleActionModel.EnumerateSupportedPlays(root).Any(action => action.CardInstanceId == 1 && action.TargetUnitId == 0) &&
            BattleActionModel.PlayCard(root, new(1, 0, targetUnitId: 1)).Rejection == ActionRejection.Illegal,
            "A hand spell was not enumerated or accepted a unit target.");

        var enemy = new CombatUnit(1, "enemy", CombatTeam.Enemy, 1, 20, 20, true, false, false, []);
        var damageEffects = effects.Concat([new CardActionEffect("Damage", "DropTargetCharacter", 1, true, false, [])]).ToArray();
        var damageRules = new BattlePlayRules(rules.Rooms, rules.Cards.Select(rule => rule.DataId == "boost" ?
            new CardPlayRule(rule.DataId, rule.AssetKey, rule.Cost, rule.Effect, rule.Destination, null, [], damageEffects, []) : rule).ToArray());
        var damaged = CardSpellModel.Apply(new(0, false, [enemy], [], context), damageEffects, 1, 1, definitions: damageRules);
        Require(damaged.Supported && damaged.State!.Units.Single().Health == 13,
            "Later spell damage did not read the source card's new upgrades.");
        var lastEffects = damageEffects.Concat([new CardActionEffect("AddStatus", "LastTargetedCharacters", 0, true, false,
            [new("pyregel", 2, 1)])]).ToArray();
        var lastRules = new BattlePlayRules(rules.Rooms, damageRules.Cards.Select(rule => rule.DataId == "boost" ?
            new CardPlayRule(rule.DataId, rule.AssetKey, rule.Cost, rule.Effect, rule.Destination, null, [], lastEffects, []) : rule).ToArray());
        var last = CardSpellModel.Apply(new(0, false, [enemy], [], context), lastEffects, 1, 1, definitions: lastRules);
        Require(last.Supported && last.State!.Units.Single().Statuses.Single().Id == "pyregel",
            "A later drop target did not replace the native last-target collection.");
        var restrictedRules = new BattlePlayRules(rules.Rooms, rules.Cards.Select(rule => rule.DataId == "unit" ?
            new CardPlayRule(rule.DataId, rule.AssetKey, rule.Cost, rule.Effect, rule.Destination, unit, [], upgradeInteractions: ["Upgrade callback"]) : rule).ToArray());
        Require(!HandUpgradeModel.Apply(room, temporary, "Permanent", restrictedRules).Supported &&
            !HandUpgradeModel.Apply(room, new("bad", "bad", new(), [], false, false, false, 0, 0, ["Upgrade filter"]), "Permanent", rules).Supported,
            "Unknown upgrade callbacks or filters returned a partial state.");
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(BattleActionModel.PlayCard(root, new(1, 0)).State) ==
            JsonSerializer.Serialize(played.State), "Parallel hand upgrade branches diverged."));
        Require(JsonSerializer.Serialize(root) == parent, "Hand upgrades mutated their parent.");
        Console.WriteLine("HAND-UPGRADE-CHECKS PASS: hand membership, playing-card exclusion, unique groups, paid cost, live spell damage and isolation.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}

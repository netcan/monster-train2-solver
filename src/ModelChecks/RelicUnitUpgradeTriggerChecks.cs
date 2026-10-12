using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class RelicUnitUpgradeTriggerChecks
{
    internal static void Run()
    {
        var upgrade = Upgrade("funguy-upgrade", 2);
        var rules = new[]
        {
            Rule(0, "OnTroopAdded", false, RelicUnitUpgradeTriggerModel.UntilUnitDeath, upgrade),
            Rule(1, "OnTroopRemoved", true, RelicUnitUpgradeTriggerModel.UntilUnitDeath, upgrade)
        };
        var relic = Relic([RelicModel.AddRemoveTempUpgradeOnUnitTrigger, RelicModel.AddRemoveTempUpgradeOnUnitTrigger], rules);
        var actor = Unit("funguy", "funguy");
        var root = State(actor, relic);
        string parent = JsonSerializer.Serialize(root);
        var added = Fire(root, "OnTroopAdded", paramInt2: 2);
        Require(added.Supported && added.State!.Units.Single().BaseAttack == 8 &&
            added.State.Units.Single().Modifiers!.Upgrades.Count == 2 && added.State.Context!.CardInstances!.Single().Temporary.Upgrades.Count == 0,
            $"OnTroopAdded differed: supported={added.Supported}, reason={added.UnsupportedReason}, attack={added.State?.Units.SingleOrDefault()?.BaseAttack}, upgrades={added.State?.Units.SingleOrDefault()?.Modifiers?.Upgrades.Count}.");
        var removed = Fire(added.State!, "OnTroopRemoved", paramInt2: 1);
        Require(removed.Supported && removed.State!.Units.Single().BaseAttack == 6 &&
            removed.State.Units.Single().Modifiers!.Upgrades.Count == 1,
            "OnTroopRemoved did not remove one matching unit upgrade per paramInt2 entry.");

        var wrongSubtype = Fire(State(Unit("other", "other"), relic), "OnTroopAdded", paramInt2: 2);
        Require(wrongSubtype.Supported && wrongSubtype.State!.Units.Single().BaseAttack == 4,
            "A unit outside the original subtype filter received the relic upgrade.");
        var wrongTeam = Fire(State(Unit("enemy", "funguy", CombatTeam.Enemy), relic), "OnTroopAdded", paramInt2: 2);
        Require(wrongTeam.Supported && wrongTeam.State!.Units.Single().BaseAttack == 4,
            "An enemy unit passed the relic source-team filter.");

        var deathRule = Rule(0, "OnDeath", false, RelicUnitUpgradeTriggerModel.UntilUnitDeath, upgrade);
        var deathRelic = Relic([RelicModel.AddRemoveTempUpgradeOnUnitTrigger], [deathRule]);
        var liveDeath = Fire(State(Unit("funguy", "funguy"), deathRelic), "OnDeath", paramInt: 0);
        var revivedDeath = Fire(State(Unit("funguy", "funguy"), deathRelic), "OnDeath", paramInt: 1);
        Require(liveDeath.Supported && liveDeath.State!.Units.Single().BaseAttack == 4 && revivedDeath.Supported &&
            revivedDeath.State!.Units.Single().BaseAttack == 6,
            "OnDeath did not honor the native live-character paramInt exception.");

        var endBattleUpgrade = Upgrade("battle-upgrade", 1);
        var endBattleRelic = Relic([RelicModel.AddRemoveTempUpgradeOnUnitTrigger, RelicModel.AddRemoveTempUpgradeOnUnitTrigger],
        [
            Rule(0, "OnMoonShade", false, RelicUnitUpgradeTriggerModel.UntilEndOfBattle, endBattleUpgrade),
            Rule(1, "OnMoonshadeLost", true, RelicUnitUpgradeTriggerModel.UntilEndOfBattle, endBattleUpgrade)
        ]);
        var battleRoot = State(Unit("funguy", "funguy"), endBattleRelic);
        var moon = Fire(battleRoot, "OnMoonShade");
        Require(moon.Supported && moon.State!.Units.Single().BaseAttack == 5 &&
            moon.State.Context!.CardInstances!.Single().Temporary.Upgrades.Count == 1,
            "End-of-battle unit upgrade did not update its spawner card's temporary modifiers.");
        var lostMoon = Fire(moon.State!, "OnMoonshadeLost");
        Require(lostMoon.Supported && lostMoon.State!.Units.Single().BaseAttack == 4 &&
            lostMoon.State.Context!.CardInstances!.Single().Temporary.Upgrades.Count == 0,
            "Removing an end-of-battle unit upgrade did not update its spawner card.");

        var countRule = Rule(0, "OnMoonShade", false, RelicUnitUpgradeTriggerModel.UntilUnitDeath, upgrade);
        var countRelic = Relic([RelicModel.AddRemoveTempUpgradeOnUnitTrigger], [countRule]);
        var countState = new TriggerCountState([new("OnMoonShade", 1)], [], [], []);
        var counted = Fire(State(Unit("funguy", "funguy"), countRelic, countState), "OnMoonShade");
        Require(counted.Supported && counted.State!.Units.Single().BaseAttack == 8,
            $"Ordinary relic unit trigger count differed: supported={counted.Supported}, reason={counted.UnsupportedReason}, attack={counted.State?.Units.SingleOrDefault()?.BaseAttack}, upgrades={counted.State?.Units.SingleOrDefault()?.Modifiers?.Upgrades.Count}.");

        var malformed = Relic([RelicModel.AddRemoveTempUpgradeOnUnitTrigger],
            [Rule(0, "OnMoonShade", false, 9, upgrade)]);
        Require(!Fire(State(Unit("funguy", "funguy"), malformed), "OnMoonShade").Supported,
            "An unknown native unit-upgrade lifetime was guessed.");
        Require(JsonSerializer.Serialize(root) == parent, "Relic unit-upgrade triggers mutated their parent branch.");
        Console.WriteLine("RELIC-UNIT-UPGRADE-TRIGGER-CHECKS PASS: ordered add/remove, paramInt2 repetition, team/subtype/OnDeath gates, trigger counts, both lifetimes and immutable state.");
    }

    private static RelicUnitUpgradeTrigger Rule(int index, string kind, bool remove, int lifetime, CardUpgradeModifier upgrade) =>
        new(index, kind, sourcePlayer: true, sourceEnemy: false, "funguy", subtypeIsNone: false, remove, lifetime, upgrade);

    private static CombatRelicState Relic(string[] types, RelicUnitUpgradeTrigger[] rules) =>
        new("test-relic", "UnitUpgradeTriggerRelic", types, unitUpgradeTriggers: rules);

    private static CardUpgradeModifier Upgrade(string id, int damage) =>
        new(id, id, new(damage: damage), [], false, false, false, 0, 0, []);

    private static CombatUnit Unit(string asset, string subtype, CombatTeam team = CombatTeam.Player) =>
        new(1, asset, team, 4, 20, 20, true, false, false, [], [new("OnTroopAdded", false, false, true, 1, [], false,
                noCountModifiersAllowed: false), new("OnTroopRemoved", false, false, true, 1, [], false, noCountModifiersAllowed: false),
            new("OnDeath", false, false, true, 1, [], false, noCountModifiersAllowed: false)],
            spawnerCardId: 8, subtypes: [subtype], modifiers: new(4, 0, 0, 1, 1, true, false, []));

    private static RoomCombatResult Fire(RoomCombatState state, string kind, int paramInt = 0, int paramInt2 = 0) =>
        RoomCombatModel.ApplyQueuedCharacterTrigger(state,
            new RoomCombatModel.QueuedCharacterTrigger(0, state.Units.Single(), kind, paramInt: paramInt, paramInt2: paramInt2), _ => { });

    private static RoomCombatState State(CombatUnit unit, CombatRelicState relic, TriggerCountState? triggerCounts = null)
    {
        var rng = UnityRng.Seed(310);
        var card = CardInstanceState.Empty(8, "funguy-card");
        var context = new CombatContext(new([], [], [], rng, 0, []), rng, 0, 1, 10,
            relics: [relic], cardInstances: [card], triggerCounts: triggerCounts);
        return new RoomCombatState(0, false, [unit], [], context);
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}

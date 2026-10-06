using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class UnitModifierChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(1);
        var context = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 9, 10,
            statistics: BattleStatistics.Empty().TrackCards([8]), cardInstances: [CardInstanceState.Empty(8, "unit")]);
        var unit = new CombatUnit(1, "unit", CombatTeam.Player, 8, 12, 25, true, false, false, [], spawnerCardId: 8, size: 3,
            modifiers: new(8, 0, 0, 3, 2, true, false, []));
        var room = new RoomCombatState(0, false, [unit], [], context);
        var upgrade = new CardUpgradeModifier("upgrade", "upgrade", new(damage: 3, health: 7, size: 8),
            [new("armor", 5, 1, removeWhenTriggered: true)], false, false, false, 6, 2, []);
        string parent = JsonSerializer.Serialize(room);
        var applied = UnitModifierModel.Apply(room, 1, upgrade, "TemporaryUntilEndOfBattle");
        Require(applied.Supported && applied.State!.Units.Single().BaseAttack == 13 && applied.State.Units.Single().Health == 19 &&
            applied.State.Units.Single().MaxHealth == 38 && applied.State.Units.Single().Size == 6 &&
            applied.State.Units.Single().Modifiers!.RawSize == 11 &&
            applied.State.Context!.CardInstances!.Single().Temporary.Upgrades.Count == 1, "Unit and source-card upgrades differ.");
        var removed = UnitModifierModel.Apply(applied.State!, 1, upgrade, "", true);
        Require(removed.Supported && removed.State!.Units.Single().BaseAttack == 8 && removed.State.Units.Single().Health == 19 &&
            removed.State.Units.Single().MaxHealth == 25 && removed.State.Units.Single().Size == 3 &&
            removed.State.Units.Single().Modifiers!.Upgrades.Count == 0 && removed.State.Context!.CardInstances!.Single().Temporary.Upgrades.Count == 0,
            "Removal healed a damaged unit or lost the raw size behind the display clamp.");
        var untilDeath = UnitModifierModel.Apply(room, 1, upgrade, "TemporaryUntilUnitDeath");
        Require(untilDeath.Supported && untilDeath.State!.Units.Single().Modifiers!.Upgrades.Count == 1 &&
            untilDeath.State.Context!.CardInstances!.Single().Temporary.Upgrades.Count == 0, "Until-death upgrades changed the source card.");
        var permanent = UnitModifierModel.Apply(room, 1, upgrade, "Permanent");
        Require(permanent.Supported && permanent.State!.Context!.CardInstances!.Single().Permanent.Upgrades.Count == 1,
            "Permanent unit upgrade did not update the permanent source group.");
        var detachedContext = new CombatContext(context.Cards, rng, 0, 9, 10,
            statistics: context.Statistics, cardInstances: [], cardRegistry: context.CardInstances);
        var detached = new RoomCombatState(0, false, [unit], [], detachedContext);
        string detachedParent = JsonSerializer.Serialize(detached);
        var retained = UnitModifierModel.Apply(detached, 1, upgrade, "TemporaryUntilEndOfBattle");
        Require(retained.Supported && retained.State!.Context!.CardInstances!.Count == 0 &&
            retained.State.Context.CardRegistry!.Single().Temporary.Upgrades.Count == 1 &&
            JsonSerializer.Serialize(detached) == detachedParent,
            "An unowned spawner reference was lost, made owned or mutated in the parent.");
        var retainedRemoved = UnitModifierModel.Apply(retained.State!, 1, upgrade, "", true);
        Require(retainedRemoved.Supported && retainedRemoved.State!.Context!.CardInstances!.Count == 0 &&
            retainedRemoved.State.Context.CardRegistry!.Single().Temporary.Upgrades.Count == 0,
            "An unowned spawner reference did not receive upgrade removal.");
        var unique = new CardUpgradeModifier("unique", "unique", new(damage: 2, health: 3), [], false, true, true, 0, 0, []);
        var once = UnitModifierModel.Apply(room, 1, unique, "TemporaryUntilUnitDeath");
        var twice = UnitModifierModel.Apply(once.State!, 1, unique, "TemporaryUntilUnitDeath");
        Require(twice.Supported && JsonSerializer.Serialize(once.State) == JsonSerializer.Serialize(twice.State), "Unique unit upgrades stacked.");
        var cloned = new CombatUnit(1, "clone", CombatTeam.Player, 8, 12, 25, true, false, false, [], size: 3,
            modifiers: new(8, 0, 0, 3, 2, true, true, []));
        var cloneRoom = new RoomCombatState(0, false, [cloned], [], context);
        Require(JsonSerializer.Serialize(UnitModifierModel.Apply(cloneRoom, 1, unique, "Permanent").State) == JsonSerializer.Serialize(cloneRoom),
            "Clone-excluded upgrades applied to a clone.");
        var lethal = new CardUpgradeModifier("lethal", "lethal", new(health: -100), [], false, false, false, 0, 0, []);
        var restricted = new CardUpgradeModifier("restricted", "restricted", new(size: 1), [], false, false, false, 0, 0, [], true);
        var blocked = UnitModifierModel.Apply(room, 1, restricted, "Permanent", roomCapacity: 3);
        Require(blocked.Supported && JsonSerializer.Serialize(blocked.State) == parent,
            "Rejected size upgrade changed the unit or its source card.");
        var fits = UnitModifierModel.Apply(room, 1, restricted, "TemporaryUntilEndOfBattle", roomCapacity: 4);
        Require(fits.Supported && fits.State!.Units.Single().Size == 4 &&
            UnitModifierModel.Apply(fits.State, 1, restricted, "", true, roomCapacity: 4).State!.Units.Single().Size == 3,
            "A full room prevented removing a restricted size upgrade.");
        var sacrificed = UnitModifierModel.Apply(room, 1, lethal, "Permanent");
        Require(sacrificed.Supported && sacrificed.State!.Units.Count == 0 && sacrificed.State.Context!.Statistics!.MonstersDeadThisBattle == 1 &&
            sacrificed.State.Context.CardInstances!.Single().Permanent.Upgrades.Count == 0,
            "Lethal max-health loss or failed upgrade source routing differs.");
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(UnitModifierModel.Apply(room, 1, upgrade, "TemporaryUntilEndOfBattle").State) ==
            JsonSerializer.Serialize(applied.State), "Parallel unit upgrade branches diverged."));
        Require(JsonSerializer.Serialize(room) == parent, "Unit upgrades mutated their parent.");
        Console.WriteLine("UNIT-MODIFIER-CHECKS PASS: lifetimes, source cards, raw size, unhealed health, removal, unique/clone rules, lethal loss and parallel isolation.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}

using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class HealingChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(17);
        var context = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 1, 10, cardInstances: []);
        var multiplier = new CombatStatus("heal multiplier", 7, 2);
        var immunity = new CombatStatus("heal immunity", 1);
        CombatUnit Unit(int hp = 5, bool healable = true, params CombatStatus[] statuses) =>
            new(1, "healer", CombatTeam.Player, 3, hp, 20, true, false, false, statuses, size: 1,
                modifiers: new(3, 0, 0, 1, 1, healable, false, []));
        RoomCombatState Room(CombatUnit unit) => new(0, false, [unit], [], context);
        int Heal(CombatUnit unit, int amount) => RoomCombatModel.ApplyCardHeal(Room(unit), 1, amount).State!.Units.Single().Health;
        Require(Heal(Unit(), 99) == 20 && Heal(Unit(), 0) == 5, "Healing cap or zero amount differs.");
        Require(Heal(Unit(statuses: [multiplier]), 3) == 11, "Healing used multiplier stacks instead of its parameter.");
        Require(Heal(Unit(statuses: [multiplier, immunity]), 3) == 5, "Healing immunity failed.");
        Require(Heal(Unit(healable: false), 3) == 5, "An unhealable unit was healed.");
        Require(HealingModel.HealedHealth(5, 20, -3, true, []) == 5 &&
            HealingModel.HealedHealth(0, 20, 9, true, []) == 0, "Negative healing damaged a unit or resurrected it.");
        var missing = new CombatUnit(1, "unknown", CombatTeam.Player, 1, 5, 20, true, false, false, []);
        Require(!RoomCombatModel.ApplyCardHeal(Room(missing), 1, 1).Supported &&
            !RoomCombatModel.ApplyCardHeal(Room(Unit()), 2, 1).Supported, "Unknown healability or target returned a child.");

        var low = new CardModifiers(new(heal: -5), [], 0, []);
        var high = new CardModifiers(new(heal: 8), [], 0, []);
        var instance = new CardInstanceState(2, "heal", low, high, 0, 0, 0, []);
        var rule = new CardPlayRule("heal", "heal", 1, "Spell", "Discard", null, [],
            [new("Heal", "DropTargetCharacter", 3, false, true, [])]);
        var resolved = CardModifierModel.Resolve(rule, instance);
        Require(resolved.Effects.Single().Value == 8 &&
            CardSpellModel.Apply(Room(Unit()), resolved.Effects, 1).State!.Units.Single().Health == 13,
            "Spell healing did not clamp permanent and temporary groups separately.");

        var health = new CardUpgradeModifier("hp", "hp", new(health: 3), [], false, false, false, 0, 0, []);
        Require(UnitModifierModel.Apply(Room(Unit(statuses: [multiplier, immunity])), 1, health,
            "TemporaryUntilUnitDeath").State!.Units.Single().Health == 11,
            "Max-health healing did not bypass immunity or apply the multiplier.");
        Require(UnitModifierModel.Apply(Room(Unit(healable: false)), 1, health,
            "TemporaryUntilUnitDeath").State!.Units.Single().Health == 5,
            "Max-health healing ignored healability.");
        var enemy = new CombatUnit(2, "enemy", CombatTeam.Enemy, 2, 20, 20, true, false, false, []);
        int CombatHeal(CombatUnit player) => RoomCombatModel.Resolve(new(0, false, [enemy, player], [], context))
            .State!.Units.Single(unit => unit.Id == 1).Health;
        var regen = new CombatStatus("regen", 2, 1, removeStackAtEnd: true);
        var lifesteal = new CombatStatus("lifesteal", 2, removeWhenTriggered: true);
        Require(CombatHeal(Unit(statuses: [regen, lifesteal, multiplier])) == 13 &&
            CombatHeal(Unit(statuses: [regen, lifesteal, immunity])) == 3 &&
            CombatHeal(Unit(healable: false, statuses: [regen, lifesteal])) == 3,
            "Regen or lifesteal ignored healing modifiers or healability.");
        var root = Room(Unit(statuses: [multiplier]));
        string parent = JsonSerializer.Serialize(root);
        string expected = JsonSerializer.Serialize(RoomCombatModel.ApplyCardHeal(root, 1, 3).State);
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(RoomCombatModel.ApplyCardHeal(root, 1, 3).State) == expected,
            "Parallel healing branches differ."));
        Require(JsonSerializer.Serialize(root) == parent, "Healing mutated the parent.");
        Console.WriteLine("HEALING-CHECKS PASS: caps, multiplier parameter, immunity, healability, spell group clamps, max-health healing, regen/lifesteal and parallel isolation.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}

using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class FuelChecks
{
    internal static void Run()
    {
        CombatStatus Spark(int count) => new("spark", count, removeWhenTriggered: true, stackable: true);
        RoomCombatState Root(int fuel, bool inert = true, bool dazed = false, bool victim = true, bool canAttack = true, bool refuel = false)
        {
            var statuses = new List<CombatStatus> { new("multistrike", 1, 2) };
            if (inert) statuses.Add(new("inert", 1));
            if (fuel > 0) statuses.Add(Spark(fuel));
            if (dazed) statuses.Add(new("dazed", 1, removeWhenTriggered: true));
            var effect = new CombatEffect("CardEffectAddStatusEffect", 0, 0, "", 0, [], false,
                action: new("AddStatus", "Self", 0, false, false, [Spark(1)]));
            var actor = new CombatUnit(1, "fuel-actor", CombatTeam.Player, 25, 100, 100, canAttack, false, false, statuses,
                refuel ? [new("OnTurnBegin", false, false, false, 1, [effect], false)] : [], isBoss: false, lastAttackerId: 0);
            var units = new List<CombatUnit> { actor };
            if (victim) units.Add(new(2, "victim", CombatTeam.Enemy, 0, 1000, 1000, false, false, false, [], [], isBoss: false, lastAttackerId: 0));
            var rng = UnityRng.Seed(117);
            return new(0, false, units, [], new(new([], [], [], rng, 0, []), rng, 0, 1, 10));
        }
        (int Fuel, bool Inert, bool Dazed, bool Victim, bool Attack, bool Force, int Damage, int Remaining)[] cases = [
            (2, true, false, true, true, false, 50, 1),
            (1, true, false, true, true, false, 50, 0), (0, true, false, true, true, false, 0, 0),
            (0, false, false, true, true, false, 50, 0), (0, true, false, true, true, true, 50, 0),
            (2, true, true, true, true, false, 0, 1), (2, true, false, false, true, false, 0, 1),
            (2, true, false, true, false, false, 0, 1), (1, true, true, true, true, true, 50, 0)
        ];
        foreach (var test in cases)
        {
            var root = Root(test.Fuel, test.Inert, test.Dazed, test.Victim, test.Attack);
            string parent = JsonSerializer.Serialize(root);
            RoomCombatResult Run() => RoomCombatModel.ApplyUnitTurn(root, 1, ignoreAttackPrevention: test.Force);
            var result = Run();
            Require(result.Supported && (result.State!.Units.Single(unit => unit.Id == 1).Status("spark")?.Stacks ?? 0) == test.Remaining &&
                (!test.Victim || result.State.Units.Single(unit => unit.Id == 2).Health == 1000 - test.Damage) &&
                result.State.Context!.BattleRng.Equals(root.Context!.BattleRng), result.UnsupportedReason ?? $"Fuel case {test}: admission, one-stack consumption or whole-turn Multistrike differs.");
            string expected = JsonSerializer.Serialize(result.State);
            Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(Run().State) == expected, "Fuel branches diverged."));
            Require(JsonSerializer.Serialize(root) == parent, "Fuel consumed its parent state.");
        }
        var empty = Root(0, refuel: true);
        var first = RoomCombatModel.ApplyUnitTurn(empty, 1);
        Require(first.Supported && first.State!.Units.Single(unit => unit.Id == 1).Status("spark")?.Stacks == 1 && first.State.Units.Single(unit => unit.Id == 2).Health == 1000,
            first.UnsupportedReason ?? "OnTurnBegin refueling retroactively changed admission.");
        var second = RoomCombatModel.ApplyUnitTurn(first.State!, 1);
        Require(second.Supported && second.State!.Units.Single(unit => unit.Id == 1).Status("spark")?.Stacks == 1 && second.State.Units.Single(unit => unit.Id == 2).Health == 950,
            second.UnsupportedReason ?? "Last fuel stack did not permit the next refueled Multistrike turn.");
        Console.WriteLine("FUEL PASS: last-stack Multistrike, empty fuel, forced bypass, Dazed, absent targets, attack-disabled consumption, trigger ordering, unchanged RNG and 32 immutable branches each.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

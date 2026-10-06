using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class TrainSpellChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(92);
        var context = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 1, 10, cardInstances: []);
        CombatUnit Unit(int id, CombatTeam team, int health) => new(id, "unit" + id, team, 3, health, 10, true, false, false, [],
            modifiers: new(3, 0, 0, 1, 1, true, false, []));
        var root = new TrainCombatState([new(0, false, [Unit(1, CombatTeam.Enemy, 2), Unit(2, CombatTeam.Player, 5)], [], context),
            new(1, false, [Unit(3, CombatTeam.Player, 8)], [], context),
            new(2, false, [new(4, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, [])], [], context)],
            [new(1, 1, true, false)], 5, context);
        CardActionEffect[] effects = [new("Damage", "Room", 2, true, false, []),
            new("Heal", "RandomInRoom", 0, false, true, [])];
        string parent = JsonSerializer.Serialize(root);
        var child = CardSpellModel.Apply(root, 0, effects, 0);
        Require(child.Supported && child.State!.Rooms[0].Units.Single().Id == 2 && child.State.Movement.Count == 0 &&
            child.State.Rooms[1].Units.Single().Health == 8 && child.State.Rooms[2].Units.Single().Health == 80 &&
            child.State.Context!.BattleRng.Equals(rng.Next()) &&
            child.State.Rooms.All(room => ReferenceEquals(room.Context, child.State.Context)),
            "Train spells lost another room, retained dead movement or failed to share the final RNG context: " + child.UnsupportedReason);
        Require(CardTargetModel.Collect(root, 0, new("Heal", "LastTargetedCharacters", 1, false, true, []), [3])
            .UnitIds.SequenceEqual([3]), "Train last-target references were confined to the selected room.");
        var invalid = new TrainCombatState([root.Rooms[0]], [], 5, context);
        Require(!CardSpellModel.Apply(invalid, 0, effects, 0).Supported &&
            !CardSpellModel.Apply(root, 99, effects, 0).Supported,
            "Incomplete train input or missing selected room returned a search child.");
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(CardSpellModel.Apply(root, 0, effects, 0).State) ==
            JsonSerializer.Serialize(child.State), "Parallel train spell branches differ."));
        Require(JsonSerializer.Serialize(root) == parent, "Train spell execution changed its parent.");
        Console.WriteLine("TRAIN-SPELL-CHECKS PASS: complete room state, shared context/RNG, dead movement removal, global last references and parallel isolation.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}

using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class StatusPerOtherEffectChecks
{
    internal static void Run()
    {
        UnityRng rng = UnityRng.Seed(771);
        var context = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 1, 10,
            statusRules: [new("armor", 0), new("damage shield", 0)]);
        CombatUnit Unit(int id, CombatTeam team, int shields, int armor = 0)
        {
            var statuses = new List<CombatStatus>();
            if (shields != 0) statuses.Add(new("damage shield", shields));
            if (armor != 0) statuses.Add(new("armor", armor));
            return new(id, "unit-" + id, team, 0, 10, 10, true, false, false, statuses,
                modifiers: new(0, 0, 0, 1, 1, true, false, []));
        }
        TrainCombatState Train() => new([
            new(0, false, [Unit(1, CombatTeam.Player, 1), Unit(2, CombatTeam.Player, 3),
                Unit(3, CombatTeam.Enemy, 5)], [], context),
            new(1, false, [Unit(4, CombatTeam.Enemy, 7)], [], context)
        ], [], 5, context);
        CardActionEffect Effect(string target, int multiplier, bool targetsOnly, bool enemies = true, bool players = true,
            IReadOnlyList<CombatStatus>? statuses = null) => new("AddStatusPerOtherEffect", target, multiplier,
                enemies, players, statuses ?? [new("armor", 1), new("damage shield", 1)],
                countStatusOnlyOnTargets: targetsOnly);

        TrainCombatState root = Train();
        string parent = JsonSerializer.Serialize(root);
        TrainSpellResult wholeRoom = CardSpellModel.Apply(root, 0, [Effect("Room", 2, false)], 0);
        Require(wholeRoom.Supported && wholeRoom.State!.Rooms[0].Units.All(unit => unit.Status("armor")?.Stacks == 18) &&
            wholeRoom.State.Rooms[1].Units.Single().Status("armor") == null,
            "Whole-room counting did not sum both teams only on the selected room.");

        TrainSpellResult targetOnly = CardSpellModel.Apply(root, 0,
            [Effect("Room", 2, true, enemies: false, players: true)], 0);
        Require(targetOnly.Supported && targetOnly.State!.Rooms[0].Units.Where(unit => unit.Team == CombatTeam.Player)
                .All(unit => unit.Status("armor")?.Stacks == 8) &&
            targetOnly.State.Rooms[0].Units.Single(unit => unit.Team == CombatTeam.Enemy).Status("armor") == null,
            "Target-only counting included an untargeted unit.");

        TrainSpellResult towerTargets = CardSpellModel.Apply(root, 0,
            [Effect("Tower", 1, true)], 0);
        Require(towerTargets.Supported && towerTargets.State!.Rooms.SelectMany(room => room.Units)
                .All(unit => unit.Status("armor")?.Stacks == 16),
            "Target-only counting did not include selected units across rooms.");

        CardActionEffect seed = new("AddStatus", "DropTargetCharacter", 0, false, true, [new("damage shield", 2)]);
        TrainSpellResult chained = CardSpellModel.Apply(root, 0,
            [seed, Effect("Room", 1, false)], targetId: 1);
        Require(chained.Supported && chained.State!.Rooms[0].Units.All(unit => unit.Status("armor")?.Stacks == 11),
            "The effect did not count status stacks added by a preceding effect in the same spell.");

        RoomCombatState overflowRoot = new(0, false,
            [Unit(10, CombatTeam.Player, int.MaxValue, armor: 3), Unit(11, CombatTeam.Enemy, 1)], [], context);
        RoomCombatResult overflow = CardSpellModel.Apply(overflowRoot, [Effect("Room", 2, false)], 10);
        Require(overflow.Supported && overflow.State!.Units.Single(unit => unit.Id == 10).Status("armor")?.Stacks == 3,
            "Status summation and multiplication did not retain native unchecked Int32 behavior: " +
            (overflow.Supported ? overflow.State!.Units.Single(unit => unit.Id == 10).Status("armor")?.Stacks.ToString() ?? "missing" : overflow.UnsupportedReason));

        RoomCombatResult noTargets = CardSpellModel.Apply(new RoomCombatState(0, false, [], [], context),
            [Effect("Room", 1, false)], 0);
        Require(noTargets.Supported && noTargets.State!.Units.Count == 0,
            "An empty target set was not safely skipped.");
        Require(!CardSpellModel.Apply(root, 0,
                [Effect("Room", 1, false, statuses: [new("armor", 1)])], 0).Supported,
            "A malformed effect without a counted-status definition was accepted.");

        string expected = JsonSerializer.Serialize(wholeRoom.State);
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(
            CardSpellModel.Apply(root, 0, [Effect("Room", 2, false)], 0).State) == expected,
            "Parallel per-status branches diverged."));
        Require(JsonSerializer.Serialize(root) == parent, "Per-status application mutated its parent train.");
        Console.WriteLine("STATUS-PER-OTHER-EFFECT-CHECKS PASS: room/target sums, live sequencing, overflow, empty targets and 32 branches.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}

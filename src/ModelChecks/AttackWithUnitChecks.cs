using MonsterTrain2Poju.Model;
using System.Text.Json;

internal static class AttackWithUnitChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(712);
        var context = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 1, 10, cardInstances: []);
        CombatUnit Unit(int id, string name, CombatTeam team, int attack, int health, params CombatStatus[] statuses) =>
            new(id, name, team, attack, health, health, true, false, false, statuses,
                modifiers: new(attack, 0, 0, 1, 1, true, false, []));
        CombatUnit Actor(bool dazed = false, IReadOnlyList<int>? equipment = null) =>
            new(2, "attacker", CombatTeam.Player, 5, 20, 20, true, false, false,
                dazed ? [new("dazed", 1, removeWhenTriggered: true)] : [],
                [new CombatTrigger("OnTurnBegin", true, false, false, 1, [], false)], size: 1,
                modifiers: new(5, 0, 0, 1, 1, true, false, []), equipmentCards: equipment);
        TrainCombatState Train(IReadOnlyList<CombatUnit> firstRoom, IReadOnlyList<CombatUnit>? pyreRoom = null) => new([
            new(0, false, firstRoom, [], context), new(1, false, [], [], context), new(2, false, [], [], context),
            new(3, false, pyreRoom ?? [new CombatUnit(12, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, [])], [], context)],
            firstRoom.Where(unit => unit.Team == CombatTeam.Enemy).Select(unit => new EnemyMovement(unit.Id, 1, true, false)).ToArray(), 5, context);
        CardActionEffect SelfAttack(bool requireEquipment = false) => new("AttackWithUnit", "Self", 0, false, true, [],
            requireEquipment: requireEquipment);

        TrainCombatState root = Train([Unit(1, "enemy", CombatTeam.Enemy, 1, 40), Actor()]);
        string parent = JsonSerializer.Serialize(root);
        TrainSpellResult ordinary = CardSpellModel.Apply(root, 0, [SelfAttack()], 2, selfUnitId: 2);
        Require(ordinary.Supported && ordinary.State!.Rooms[0].Units.Single(unit => unit.Id == 1).Health == 35 &&
            ordinary.State.Rooms[0].Units.Single(unit => unit.Id == 2).Triggers.Single().HasTriggered,
            "An AttackWithUnit spell did not execute the actor's turn and its ordinary turn-begin trigger.");

        TrainCombatState dazedRoot = Train([Unit(1, "enemy", CombatTeam.Enemy, 1, 40), Actor(dazed: true)]);
        TrainSpellResult dazed = CardSpellModel.Apply(dazedRoot, 0, [SelfAttack()], 2, selfUnitId: 2);
        Require(dazed.Supported && dazed.State!.Rooms[0].Units.Single(unit => unit.Id == 1).Health == 35 &&
            !dazed.State.Rooms[0].Units.Single(unit => unit.Id == 2).Triggers.Single().HasTriggered,
            "The native attack-prevention bypass failed to allow a dazed attack or incorrectly fired its turn trigger.");

        TrainCombatState unarmed = Train([Unit(1, "enemy", CombatTeam.Enemy, 1, 40), Actor(equipment: [])]);
        TrainSpellResult skipped = CardSpellModel.Apply(unarmed, 0, [SelfAttack(requireEquipment: true)], 2, selfUnitId: 2);
        Require(skipped.Supported && skipped.State!.Rooms[0].Units.Single(unit => unit.Id == 1).Health == 40,
            "An equipment-gated unit attack did not skip an unequipped actor.");
        TrainCombatState unknownEquipment = Train([Unit(1, "enemy", CombatTeam.Enemy, 1, 40), Actor()]);
        TrainSpellResult unknown = CardSpellModel.Apply(unknownEquipment, 0, [SelfAttack(requireEquipment: true)], 2, selfUnitId: 2);
        Require(!unknown.Supported && unknown.UnsupportedReason!.Contains("equipment state", StringComparison.OrdinalIgnoreCase),
            "An unknown equipment snapshot silently passed an equipment-gated unit attack.");
        TrainCombatState armed = Train([Unit(1, "enemy", CombatTeam.Enemy, 1, 40), Actor(equipment: [90])]);
        TrainSpellResult equipped = CardSpellModel.Apply(armed, 0, [SelfAttack(requireEquipment: true)], 2, selfUnitId: 2);
        Require(equipped.Supported && equipped.State!.Rooms[0].Units.Single(unit => unit.Id == 1).Health == 35,
            "An equipped actor failed the equipment-gated unit attack.");

        CombatUnit pyre = new(12, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, [new("sweep", 1)]);
        CombatUnit pyreFloorEnemy = Unit(13, "pyre-floor-enemy", CombatTeam.Enemy, 1, 100);
        TrainCombatState crossRoom = Train([Unit(3, "selected-floor-decoy", CombatTeam.Enemy, 1, 100)], [pyre, pyreFloorEnemy]);
        var pyreAttack = new CardActionEffect("AttackWithUnit", "Pyre", 0, false, false, [], useSelectedRoom: true);
        TrainSpellResult selectedRoom = CardSpellModel.Apply(crossRoom, 0, [pyreAttack], 0);
        Require(selectedRoom.Supported && selectedRoom.State!.Rooms[0].Units.Single(unit => unit.Id == 3).Health == 55 &&
            selectedRoom.State.Rooms[3].Units.Single(unit => unit.Id == 13).Health == 100 &&
            selectedRoom.State.Rooms[3].Units.Single(unit => unit.Id == 12).Health == 80 &&
            selectedRoom.State.Rooms.All(room => ReferenceEquals(room.Context, selectedRoom.State.Context)),
            "The Pyre attack used the Pyre's room instead of the card-selected room.");

        CombatUnit remoteActor = Actor();
        TrainCombatState externalRoot = new([
            new(0, false, [Unit(20, "external-target", CombatTeam.Enemy, 1, 40)], [], context),
            new(1, false, [], [], context), new(2, false, [remoteActor], [], context),
            new(3, false, [new CombatUnit(21, "pyre", CombatTeam.Player, 45, 80, 80, true, true, false, [])], [], context)
        ], [], 5, context);
        string externalParent = JsonSerializer.Serialize(externalRoot);
        var externalAttack = new CardActionEffect("AttackWithUnit", "Tower", 0, false, true, [], useSelectedRoom: true);
        TrainSpellResult external = CardSpellModel.Apply(externalRoot, 0, [externalAttack], 0);
        Require(external.Supported && external.State!.Rooms[0].Units.Single(unit => unit.Id == 20).Health == 35 &&
            external.State.Rooms[0].Units.All(unit => unit.Id != remoteActor.Id) &&
            external.State.Rooms[2].Units.Single(unit => unit.Id == remoteActor.Id).Triggers.Single().HasTriggered &&
            external.State.Rooms.All(room => ReferenceEquals(room.Context, external.State.Context)) &&
            JsonSerializer.Serialize(externalRoot) == externalParent,
            "A remote AttackWithUnit actor did not attack in the selected room and retain its state at its original room.");

        string expected = JsonSerializer.Serialize(ordinary.State);
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(CardSpellModel.Apply(root, 0, [SelfAttack()], 2, selfUnitId: 2).State) == expected,
            "Parallel AttackWithUnit branches diverged."));
        Require(JsonSerializer.Serialize(root) == parent, "AttackWithUnit mutated its parent train.");
        Console.WriteLine("ATTACK-WITH-UNIT-CHECKS PASS: unit turns, dazed bypass, equipment gate, selected rooms, remote actors and 32 branches.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}

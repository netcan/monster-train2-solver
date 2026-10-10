using MonsterTrain2Poju.Model;
using System.Text.Json;

internal static class CardSpellChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(424242);
        var armor = new CombatStatus("armor", 3, 1, removeWhenTriggered: true);
        var context = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 1, 10, [armor]);
        var pyregel = new CombatStatus("pyregel", 2, 1, removeStackAtEnd: true);
        var valor = new CombatStatus("valor", 2, 1);
        CombatUnit Make(int id, CombatTeam team, int hp, params CombatStatus[] statuses) =>
            new(id, "unit-" + id, team, 8, hp, hp, true, false, false, statuses, size: 2);
        var enemy = Make(1, CombatTeam.Enemy, 5, armor, pyregel);
        var front = Make(2, CombatTeam.Player, 20);
        var back = Make(3, CombatTeam.Player, 20);
        var room = new RoomCombatState(0, false, [enemy, front, back], [], context);
        string parent = JsonSerializer.Serialize(room);
        CardActionEffect[] damage = [new("Damage", "DropTargetCharacter", 1, true, true, []),
            new("AddStatus", "LastTargetedCharacters", 0, true, true, [pyregel])];
        RoomCombatResult hit = CardSpellModel.Apply(room, damage, 1);
        Require(hit.Supported && hit.State!.Units[0].Health == 5 &&
            !hit.State.Units[0].Statuses.Any(status => status.Id == "armor") &&
            hit.State.Units[0].Statuses.Single(status => status.Id == "pyregel").Stacks == 4,
            "Pyregel flat damage, armor consumption or subsequent status stacks differed.");
        RoomCombatResult shield = CardSpellModel.Apply(new RoomCombatState(0, false,
            [Make(1, CombatTeam.Enemy, 5, new CombatStatus("damage shield", 1, removeWhenTriggered: true)), front], [], context), damage, 1);
        Require(shield.Supported && shield.State!.Units[0].Health == 5 &&
            !shield.State.Units[0].Statuses.Any(status => status.Id == "damage shield") &&
            shield.State.Units[0].Statuses.Single(status => status.Id == "pyregel").Stacks == 2,
            "Spell shield and follow-up status handling differed.");
        var armorRule = new CombatStatus("armor", 1);
        var shieldRule = new CombatStatus("damage shield", 1, removeWhenTriggered: true);
        var mask = new CardMaskDefinition("piercing-source", "Spell", "Common", false, false, false,
            [], [], [], [], null, false, 0, 0, false, false, false);
        var piercingTrait = new CardTraitValue("CardTraitIgnoreArmor", "CardTraitIgnoreArmor", 0, 0, true, 0);
        var lifecycle = new CardLifecycleUpgrade(new CardMaskUpgrade("piercing-upgrade", new CardStatModifier(),
            [], false, false, false, false), "piercing-upgrade", false, false,
            [piercingTrait], [], false, [], [], []);
        var piercingUpgrade = new CardUpgradeModifier("piercing-upgrade", "piercing-upgrade", new CardStatModifier(),
            [], false, false, false, 0, 0, [], maskMetadata: new CardUpgradeMaskMetadata([], false, false, false, false),
            lifecycle: lifecycle);
        var descriptor = new CardMaskDescriptor(mask, 0, [], []);
        var piercingCard = new CardInstanceState(7, mask.DataId, CardModifiers.Empty(),
            new CardModifiers(new CardStatModifier(), [piercingUpgrade], 0, []), 0, 0, 0, [], maskDescriptor: descriptor);
        var ordinaryCard = CardInstanceState.Empty(8, "ordinary-source");
        CombatContext CardContext(CardInstanceState card) => new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng,
            0, 9, 10, [armorRule, shieldRule], cardInstances: [card], cardRegistry: [card]);
        RoomCombatState DamageRoom(CardInstanceState card, params CombatStatus[] statuses) => new RoomCombatState(0, false,
            [Make(9, CombatTeam.Enemy, 20, statuses)], [], CardContext(card));
        RoomCombatResult piercingArmor = RoomCombatModel.ApplyCardDamage(DamageRoom(piercingCard, new CombatStatus("armor", 5, 1)), 9, 4, 7);
        Require(piercingArmor.Supported && piercingArmor.State!.Units[0].Health == 16 &&
            piercingArmor.State.Units[0].Status("armor")?.Stacks == 5,
            "An upgrade-added IgnoreArmor trait did not pierce armor without consuming it.");
        RoomCombatResult piercingShield = RoomCombatModel.ApplyCardDamage(DamageRoom(piercingCard, new CombatStatus("damage shield", 1, removeWhenTriggered: true)), 9, 4, 7);
        Require(piercingShield.Supported && piercingShield.State!.Units[0].Health == 16 &&
            piercingShield.State.Units[0].Status("damage shield")?.Stacks == 1,
            "An upgrade-added IgnoreArmor trait did not pierce damage shield without consuming it.");
        RoomCombatResult ordinaryArmor = RoomCombatModel.ApplyCardDamage(DamageRoom(ordinaryCard, new CombatStatus("armor", 5, 1)), 9, 4, 8);
        Require(ordinaryArmor.Supported && ordinaryArmor.State!.Units[0].Health == 20,
            "A card without IgnoreArmor bypassed armor.");
        RoomCombatResult lethal = CardSpellModel.Apply(new RoomCombatState(0, false,
            [Make(1, CombatTeam.Enemy, 1), Make(4, CombatTeam.Enemy, 5), front], [], context), damage, 1);
        Require(lethal.Supported && lethal.State!.Units[0].Id == 4 && lethal.State.Units[0].Statuses.Count == 0,
            "A follow-up LastTargeted effect retargeted after lethal damage.");
        CardActionEffect[] rally = [new("FloorRearrange", "DropTargetCharacter", 0, true, true, []),
            new("AddStatus", "LastTargetedCharacters", 0, true, true, [valor])];
        RoomCombatResult moved = CardSpellModel.Apply(room, rally, 3);
        Require(moved.Supported && moved.State!.Units.Select(unit => unit.Id).SequenceEqual(new[] { 1, 3, 2 }) &&
            moved.State.Units[1].Attack == 10 && moved.State.Units[1].BaseAttack == 8 &&
            !moved.State.Units[1].Statuses.Any(status => status.Id == "armor"), "Rearrange/valor or immediate armor timing differed.");
        RoomCombatResult resolved = RoomCombatModel.Resolve(moved.State!);
        Require(resolved.Supported && resolved.State!.Units.First(unit => unit.Team == CombatTeam.Player)
            .Statuses.Single(status => status.Id == "armor").Stacks == 2, "Valor did not replenish front armor after room combat.");
        RoomCombatResult deployment = RoomCombatModel.Resolve(new RoomCombatState(0, true, moved.State!.Units, [], context));
        Require(deployment.Supported && !deployment.State!.Units.Where(unit => unit.Team == CombatTeam.Player)
            .Any(unit => unit.Statuses.Any(status => status.Id == "armor")), "Valor armor fired during deployment.");
        var immune = new CombatUnit(1, "immune", CombatTeam.Enemy, 1, 5, 5, true, false, false, [], statusImmunities: ["pyregel"]);
        RoomCombatResult immunity = CardSpellModel.Apply(new RoomCombatState(0, false, [immune], [], context), damage, 1);
        Require(immunity.Supported && immunity.State!.Units[0].Health == 4 && immunity.State.Units[0].Statuses.Count == 0,
            "Native status immunity was ignored.");
        var immobile = Make(3, CombatTeam.Player, 20, new CombatStatus("immobile", 1));
        RoomCombatResult stationary = CardSpellModel.Apply(new RoomCombatState(0, false, [front, immobile], [], context), rally, 3);
        Require(stationary.Supported && stationary.State!.Units[0].Id == 2 && stationary.State.Units[1].Attack == 10,
            "Immobile cancelled the spell's later status effect.");
        var retargetEffects = rally.Concat([new CardActionEffect("Damage", "DropTargetCharacter", 5, false, true, []),
            new CardActionEffect("AddStatus", "LastTargetedCharacters", 0, false, true, [pyregel])]).ToArray();
        var retarget = CardSpellModel.Apply(room, retargetEffects, 3);
        Require(retarget.Supported && retarget.State!.Units.Single(unit => unit.Id == 3).Health == 20 &&
            retarget.State.Units.Single(unit => unit.Id == 2).Health == 15 &&
            retarget.State.Units.Single(unit => unit.Id == 2).Statuses.Any(status => status.Id == "pyregel") &&
            !retarget.State.Units.Single(unit => unit.Id == 3).Statuses.Any(status => status.Id == "pyregel"),
            "A repeated drop effect did not reselect the original spawn point or update its last target.");
        Require(!CardSpellModel.Apply(room, [damage[0], new("Unknown", "LastTargetedCharacters", 0, true, true, [])], 1).Supported,
            "A partially implemented spell returned a search state.");
        Parallel.For(0, 32, _ => Require(JsonSerializer.Serialize(CardSpellModel.Apply(room, rally, 3).State) ==
            JsonSerializer.Serialize(moved.State), "Parallel spell branches diverged."));
        Require(JsonSerializer.Serialize(room) == parent, "Spells mutated their parent.");
        Console.WriteLine("SPELL-CHECKS PASS: armor/shield, flat damage, lethal follow-ups, valor, rearrange, immunity and isolation.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}

using MonsterTrain2Poju.Model;
using MonsterTrain2Poju.Fixtures;

internal static class TitanSkinChecks
{
    internal static void Run()
    {
        var titanSkin = new CombatStatus("titanskin", 2, 1);
        var target = Unit(1, CombatTeam.Enemy, 0, 20, false, titanSkin);
        var attacker = Unit(2, CombatTeam.Player, 5, 20, true);
        RoomCombatResult soaked = RoomCombatModel.Exchange(Room(target, attacker));
        CombatUnit soakedTarget = soaked.State!.Units.Single(unit => unit.Id == 1);
        Require(soaked.Supported && soakedTarget.Health == 17 && soakedTarget.Status("titanskin")?.Stacks == 2 &&
            soaked.Events.Single(item => item.Kind == "Attack").Amount == 3,
            "Titan Skin did not soak damage without consuming its passive stacks.");

        RoomCombatResult capped = RoomCombatModel.Exchange(Room(
            Unit(1, CombatTeam.Enemy, 0, 20, false, new CombatStatus("titanskin", 4, 1)),
            Unit(2, CombatTeam.Player, 1, 20, true)));
        Require(capped.Supported && capped.State!.Units[0].Health == 20 && capped.State.Units[0].Status("titanskin")?.Stacks == 4,
            "Titan Skin did not cap applicable stacks by incoming damage.");

        RoomCombatResult shieldFirst = RoomCombatModel.Exchange(Room(
            Unit(1, CombatTeam.Enemy, 0, 20, false, new CombatStatus("damage shield", 1, removeWhenTriggered: true), titanSkin),
            Unit(2, CombatTeam.Player, 5, 20, true, new CombatStatus("multistrike", 1, 2))));
        CombatUnit shieldedTarget = shieldFirst.State!.Units.Single(unit => unit.Id == 1);
        Require(shieldFirst.Supported && shieldedTarget.Health == 17 && shieldedTarget.Status("damage shield") == null &&
            shieldedTarget.Status("titanskin")?.Stacks == 2,
            "Damage Shield did not resolve before Titan Skin on successive attacks.");

        RoomCombatResult armorAfter = RoomCombatModel.Exchange(Room(
            Unit(1, CombatTeam.Enemy, 0, 20, false, titanSkin,
                new CombatStatus("armor", 4, 1, removeWhenTriggered: true)), attacker));
        CombatUnit armoredTarget = armorAfter.State!.Units.Single(unit => unit.Id == 1);
        Require(armorAfter.Supported && armoredTarget.Health == 20 && armoredTarget.Status("armor")?.Stacks == 1 &&
            armoredTarget.Status("titanskin")?.Stacks == 2,
            "Armor did not resolve after Titan Skin reduced incoming damage.");

        RoomCombatResult unitPiercing = RoomCombatModel.Exchange(Room(
            Unit(1, CombatTeam.Enemy, 0, 20, false, titanSkin, new CombatStatus("armor", 4, 1, removeWhenTriggered: true)),
            Unit(2, CombatTeam.Player, 5, 20, true, new CombatStatus("piercing", 1))));
        CombatUnit unitPiercedTarget = unitPiercing.State!.Units.Single(unit => unit.Id == 1);
        Require(unitPiercing.Supported && unitPiercedTarget.Health == 15 && unitPiercedTarget.Status("armor")?.Stacks == 4 &&
            unitPiercedTarget.Status("titanskin")?.Stacks == 2,
            "Unit Piercing did not bypass Titan Skin and Armor without consuming their stacks.");

        var piercingRelic = new CombatRelicState("piercing", "piercing", [RelicModel.AllDamageIsPiercing]);
        RoomCombatResult relicPiercing = RoomCombatModel.Exchange(Room(
            Unit(1, CombatTeam.Enemy, 0, 20, false, titanSkin,
                new CombatStatus("damage shield", 1, removeWhenTriggered: true),
                new CombatStatus("armor", 4, 1, removeWhenTriggered: true)), attacker, [piercingRelic]));
        CombatUnit relicPiercedTarget = relicPiercing.State!.Units.Single(unit => unit.Id == 1);
        Require(relicPiercing.Supported && relicPiercedTarget.Health == 15 && relicPiercedTarget.Status("damage shield")?.Stacks == 1 &&
            relicPiercedTarget.Status("armor")?.Stacks == 4 && relicPiercedTarget.Status("titanskin")?.Stacks == 2,
            "All Damage Is Piercing did not bypass all three defenses.");

        CombatContext magnitudeContext = Context([new CombatRelicState("magnitude", "magnitude",
            [RelicModel.ModifyStatusMagnitude], statusMagnitudes: [new(0, 1, "titanskin", 2)])]);
        RoomCombatResult modified = RoomCombatModel.Exchange(Room(target, attacker, context: magnitudeContext));
        Require(modified.Supported && modified.State!.Units.Single(unit => unit.Id == 1).Health == 20 &&
            modified.State.Units.Single(unit => unit.Id == 1).Status("titanskin")?.Stacks == 2,
            "Relic-modified Titan Skin magnitude was not applied per stack: supported=" + modified.Supported +
            ", reason=" + modified.UnsupportedReason + ", health=" + modified.State?.Units.Single(unit => unit.Id == 1).Health);

        CardMaskDefinition definition = new("ignore-armor", "Spell", "Common", false, false, false,
            [], [], [], [], null, false, 0, 0, false, false, false);
        var trait = new CardTraitValue("CardTraitIgnoreArmor", "CardTraitIgnoreArmor", 0, 0, true, 0);
        var descriptor = new CardMaskDescriptor(definition, 0, [trait], []);
        var card = new CardInstanceState(7, "ignore-armor", CardModifiers.Empty(), CardModifiers.Empty(), 0, 0, 0, [],
            maskDescriptor: descriptor);
        CombatContext cardContext = Context([], [card]);
        RoomCombatState cardRoom = Room(Unit(1, CombatTeam.Enemy, 0, 20, false, titanSkin),
            Unit(2, CombatTeam.Player, 0, 20, false), context: cardContext);
        RoomCombatResult cardPiercing = RoomCombatModel.ApplyCardDamage(cardRoom, 1, 5, 7);
        Require(cardPiercing.Supported && cardPiercing.State!.Units.Single(unit => unit.Id == 1).Health == 15 &&
            cardPiercing.State.Units.Single(unit => unit.Id == 1).Status("titanskin")?.Stacks == 2,
            "CardTraitIgnoreArmor did not bypass Titan Skin.");

        Console.WriteLine("TITAN-SKIN-CHECKS PASS: per-hit soak, persistent stacks, shield/armor order, status/card/relic piercing and modified magnitude.");
    }

    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("ModifierScenario").GetString() == "titan-skin-combat" &&
            fixture.GetProperty("NativeWon").GetBoolean() && fixture.GetProperty("CaptureFailures").GetInt32() == 0 &&
            fixture.GetProperty("Mismatches").GetInt32() == 0 && fixture.GetProperty("Unsupported").GetInt32() == 0 &&
            fixture.GetProperty("Pending").GetInt32() == 0,
            "Titan Skin native battle capture was incomplete.");

        int observed = 0;
        int damaged = 0;
        foreach (FixtureValue stage in fixture.GetProperty("Stages").EnumerateArray())
        {
            RoomCombatState before = stage.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState actual = stage.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            foreach (CombatUnit boss in before.Units.Where(unit => unit.AssetKey == "Boss_Level1_Basic" &&
                unit.Status("titanskin") != null))
            {
                CombatStatus status = boss.Status("titanskin")!;
                Require(status.Stacks == 2 && status.ParamInt == 1,
                    "Native Titan Skin started with unexpected stacks or per-stack magnitude.");
                observed++;
                CombatUnit? after = actual.Units.FirstOrDefault(unit => unit.Id == boss.Id);
                if (after == null || after.Health < boss.Health) damaged++;
                if (after != null)
                    Require(after.Status("titanskin")?.Stacks == 2,
                        "Native Titan Skin stacks were consumed by a hit.");
            }
        }
        Require(observed > 0 && damaged > 0,
            "The native battle did not capture Titan Skin on Boss_Level1_Basic taking damage.");
        Console.WriteLine($"TITAN-SKIN-NATIVE-CHECKS PASS: {observed} status stages, {damaged} damaged stages, passive stacks retained.");
    }

    private static CombatUnit Unit(int id, CombatTeam team, int attack, int health, bool canAttack,
        params CombatStatus[] statuses) => new(id, "unit-" + id, team, attack, health, health, canAttack, false, false,
            statuses, modifiers: new(attack, 0, 0, 0, 0, true, false, [], damageBuffFromStatusEffects: 0));

    private static RoomCombatState Room(CombatUnit enemy, CombatUnit player, IReadOnlyList<CombatRelicState>? relics = null,
        CombatContext? context = null) => new(0, false, [enemy, player], [], context ?? (relics == null ? null : Context(relics)));

    private static CombatContext Context(IReadOnlyList<CombatRelicState> relics,
        IReadOnlyList<CardInstanceState>? cardRegistry = null) =>
        new(new([], [], [], UnityRng.Seed(13), 0, []), UnityRng.Seed(13), 0, 1, 10,
            cardRegistry: cardRegistry, relics: relics);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

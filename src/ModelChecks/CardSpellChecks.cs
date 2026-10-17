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
        var multiply = new CardActionEffect("MultiplyStatus", "DropTargetCharacter", 100, true, true, [armor.WithStacks(2)]);
        RngDraw multiplyChance = rng.Range(0, 100);
        RoomCombatResult doubled = CardSpellModel.Apply(new RoomCombatState(0, false, [enemy], [], context), [multiply], 1);
        Require(doubled.Supported && doubled.State!.Units.Single().Status("armor")?.Stacks == 6 &&
            JsonSerializer.Serialize(doubled.State.Context!.BattleRng) == JsonSerializer.Serialize(multiplyChance.State),
            "Status multiplication did not double existing stacks or consume its per-target chance draw.");
        var certainMultiply = new CardActionEffect("MultiplyStatus", "DropTargetCharacter", 0, true, true, [armor.WithStacks(2)]);
        RoomCombatResult certainDoubled = CardSpellModel.Apply(new RoomCombatState(0, false, [enemy], [], context), [certainMultiply], 1);
        Require(certainDoubled.Supported && certainDoubled.State!.Units.Single().Status("armor")?.Stacks == 6 &&
            JsonSerializer.Serialize(certainDoubled.State.Context!.BattleRng) == JsonSerializer.Serialize(rng),
            "A zero-chance-parameter status multiplication was not unconditional or consumed RNG.");
        RoomCombatResult emptyMultiply = CardSpellModel.Apply(new RoomCombatState(0, false, [front], [], context), [multiply], 2);
        Require(emptyMultiply.Supported && emptyMultiply.State!.Units.Single().Status("armor") == null &&
            JsonSerializer.Serialize(emptyMultiply.State.Context!.BattleRng) == JsonSerializer.Serialize(multiplyChance.State),
            "Status multiplication changed a target without stacks or skipped the chance draw.");
        var positive = new CombatStatus("armor", 3, stackable: true, displayCategory: "Positive");
        var negative = new CombatStatus("pyregel", 2, stackable: true, displayCategory: "Negative");
        var positiveTarget = Make(4, CombatTeam.Player, 20, positive, negative);
        var multiplyPositive = new CardActionEffect("MultiplyAllPositiveStatusEffects", "DropTargetCharacter", 2, true, true, []);
        RoomCombatState positiveRoom = new(0, false, [positiveTarget], [], context);
        string positiveParent = JsonSerializer.Serialize(positiveRoom);
        RoomCombatResult positiveResult = CardSpellModel.Apply(positiveRoom, [multiplyPositive], 4);
        Require(positiveResult.Supported && positiveResult.State!.Units.Single().Status("armor")?.Stacks == 6 &&
            positiveResult.State.Units.Single().Status("pyregel")?.Stacks == 2 &&
            JsonSerializer.Serialize(positiveRoom) == positiveParent,
            "Positive-status multiplication changed a negative status or differed: supported=" + positiveResult.Supported + ", reason=" + positiveResult.UnsupportedReason +
            ", statuses=" + string.Join(",", positiveResult.State?.Units.Single().Statuses.Select(status => status.Id + ":" + status.Stacks) ?? Array.Empty<string>()) +
            ", parentUnchanged=" + (JsonSerializer.Serialize(positiveRoom) == positiveParent));
        var propagatableArmor = new CombatStatus("armor", 2, stackable: true, displayCategory: "Positive", propagatable: true,
            excludeHeroPropagation: false, excludeMonsterPropagation: false);
        var heroExcludedPyregel = new CombatStatus("pyregel", 3, stackable: true, displayCategory: "Negative", propagatable: true,
            excludeHeroPropagation: true, excludeMonsterPropagation: false);
        var nonPropagatableValor = new CombatStatus("valor", 1, stackable: true, displayCategory: "Positive", propagatable: false,
            excludeHeroPropagation: false, excludeMonsterPropagation: false);
        var adjustmentTarget = Make(5, CombatTeam.Enemy, 20, propagatableArmor, heroExcludedPyregel, nonPropagatableValor);
        var propagatableAdjustment = new CardActionEffect("AdjustAllStatusEffects", "DropTargetCharacter", 2, true, true, [],
            statusAdjustmentFilter: 1);
        RoomCombatResult propagated = CardSpellModel.Apply(new RoomCombatState(0, false, [adjustmentTarget], [], context),
            [propagatableAdjustment], 5);
        Require(propagated.Supported && propagated.State!.Units.Single().Status("armor")?.Stacks == 4 &&
            propagated.State.Units.Single().Status("pyregel")?.Stacks == 3 &&
            propagated.State.Units.Single().Status("valor")?.Stacks == 1,
            "Propagatable status adjustment ignored team exclusions or changed an ineligible status.");
        var uncapturedPropagation = new CardActionEffect("AdjustAllStatusEffects", "DropTargetCharacter", 2, true, true,
            [new CombatStatus("armor", 1)], statusAdjustmentFilter: 1);
        RoomCombatResult unknownPropagation = CardSpellModel.Apply(new RoomCombatState(0, false,
            [Make(7, CombatTeam.Enemy, 20, new CombatStatus("armor", 2))], [], context), [uncapturedPropagation], 7);
        Require(!unknownPropagation.Supported && unknownPropagation.UnsupportedReason!.Contains("propagation metadata", StringComparison.Ordinal),
            "Missing propagation metadata silently produced a supported status-adjustment branch.");
        var listAdjustment = new CardActionEffect("AdjustAllStatusEffects", "DropTargetCharacter", -1, true, true, [positive],
            statusAdjustmentFilter: 0);
        RoomCombatResult removedArmor = CardSpellModel.Apply(positiveRoom, [listAdjustment], 4);
        Require(removedArmor.Supported && removedArmor.State!.Units.Single().Status("armor")?.Stacks == 2 &&
            removedArmor.State.Units.Single().Status("pyregel")?.Stacks == 2,
            "Status-list adjustment did not remove only the selected status.");
        var strictStatusAdjustment = new CardActionEffect("AdjustAllStatusEffects", "DropTargetCharacter", 2, true, true, [positive],
            tests: new CardEffectTests(true, false, false, true), statusAdjustmentFilter: 0);
        RoomCombatState strictRoom = new(0, false, [Make(6, CombatTeam.Enemy, 20, negative)], [], context);
        RoomCombatResult strictNoMatch = CardSpellModel.Apply(strictRoom, [strictStatusAdjustment], 6);
        Require(strictNoMatch.Supported && strictNoMatch.State!.Units.Single().Status("pyregel")?.Stacks == 2 &&
            strictNoMatch.State.Units.Single().Status("armor") == null,
            "Require-valid-target status adjustment applied without a matching status.");
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
        CardInstanceState shiftedSpawnerCard = CardInstanceState.Empty(90, "shifted-spawner")
            .WithPlayedRoomUnits([4, 20]);
        var shiftedContext = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 91, 10,
            cardInstances: [shiftedSpawnerCard], cardRegistry: [shiftedSpawnerCard]);
        var shiftHost = new CombatUnit(4, "shift-host", CombatTeam.Player, 8, 20, 20, true, false, false, [],
            [new CombatTrigger("OnShift", false, false, true, 1, [], skipDuringDeployment: false)], spawnerCardId: 90);
        var movedForCache = new RoomCombatState(0, false,
            [Make(21, CombatTeam.Enemy, 5), Make(22, CombatTeam.Enemy, 5), shiftHost, Make(20, CombatTeam.Player, 20)], [], shiftedContext);
        RoomCombatResult shiftedCache = CardSpellModel.Apply(movedForCache,
            [new CardActionEffect("FloorRearrange", "DropTargetCharacter", 0, true, true, [])], 20);
        Require(shiftedCache.Supported && shiftedCache.State!.Context!.FindCard(90)!.PlayedRoomUnitIds!.SequenceEqual([4, 20, 21, 22]),
            "A shifted unit's OnShift callback did not refresh its spawner card's room cache.");
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
        var equipmentUpgrade = new CardUpgradeModifier("buff-equipment", "buff-equipment",
            new CardStatModifier(damage: 2, health: 3), [], false, false, false, 0, 0, []);
        var equipmentCards = new[] { CardInstanceState.Empty(40, "equipment-a"), CardInstanceState.Empty(41, "equipment-b") };
        var equipmentContext = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 0, 42, 10,
            cardInstances: equipmentCards, cardRegistry: equipmentCards);
        var equippedUnit = new CombatUnit(39, "equipped-unit", CombatTeam.Player, 5, 20, 20, true, false, false, [],
            modifiers: new UnitModifiers(5, 0, 0, 1, 2, true, false, []), isBoss: false, equipmentCards: [40, 41]);
        var equipmentRoom = new RoomCombatState(0, false, [equippedUnit], [], equipmentContext, activeCompanionBossIds: []);
        TrainCombatState equipmentTrain = new TrainCombatState([equipmentRoom], [], 1, equipmentContext);
        Require(equipmentTrain.Rooms.Single().ActiveCompanionBossIds is { Count: 0 },
            "A single-room train wrapper dropped the explicitly captured empty companion-boss store.");
        string equipmentParent = JsonSerializer.Serialize(equipmentRoom);
        var buffEquipment = new CardActionEffect("BuffEquipment", "DropTargetCharacter", 0, false, true, [],
            upgrade: equipmentUpgrade, lifetime: "TemporaryUntilUnitDeath", scaleUpgrade: true);
        RoomCombatResult firstEquipmentBuff = CardSpellModel.Apply(equipmentRoom, [buffEquipment], 39);
        RoomCombatResult preservedEquipmentSnapshot = UnitModifierModel.Apply(equipmentRoom, 39, equipmentUpgrade,
            "TemporaryUntilUnitDeath", scaleUpgrade: true, accumulateScaledUpgradeRecord: false);
        Require(preservedEquipmentSnapshot.Supported && preservedEquipmentSnapshot.State!.ActiveCompanionBossIds is { Count: 0 },
            "A direct unit upgrade dropped the explicitly captured empty companion-boss store.");
        Require(firstEquipmentBuff.Supported && firstEquipmentBuff.State!.Units.Single().Attack == 9 &&
            firstEquipmentBuff.State.ActiveCompanionBossIds is { Count: 0 } &&
            firstEquipmentBuff.State.Units.Single().Modifiers!.Upgrades.Single().Stats.Damage == 2 &&
            equipmentCards.All(card => firstEquipmentBuff.State.Context!.FindCard(card.InstanceId)!.Permanent.Upgrades
                .Single(upgrade => upgrade.DataId == equipmentUpgrade.DataId).Stats.Damage == 2),
            "BuffEquipment did not upgrade each attached card and its host: attack=" +
            (firstEquipmentBuff.State?.Units.SingleOrDefault()?.Attack.ToString() ?? "missing") + ", active=" +
            (firstEquipmentBuff.State?.ActiveCompanionBossIds == null ? "null" : firstEquipmentBuff.State.ActiveCompanionBossIds.Count.ToString()) +
            ", gear=" + string.Join(",", equipmentCards.Select(card => firstEquipmentBuff.State?.Context?.FindCard(card.InstanceId)?.Permanent.Upgrades
                .SingleOrDefault(upgrade => upgrade.DataId == equipmentUpgrade.DataId)?.Stats.Damage.ToString() ?? "missing")) +
            ", reason=" + firstEquipmentBuff.UnsupportedReason);
        RoomCombatResult scaledEquipmentBuff = CardSpellModel.Apply(firstEquipmentBuff.State!, [buffEquipment], 39);
        Require(scaledEquipmentBuff.Supported && scaledEquipmentBuff.State!.Units.Single().Attack == 13 &&
            scaledEquipmentBuff.State.Units.Single().Modifiers!.Upgrades.Single().Stats.Damage == 2 &&
            equipmentCards.All(card => scaledEquipmentBuff.State.Context!.FindCard(card.InstanceId)!.Permanent.Upgrades
                .Count(upgrade => upgrade.DataId == equipmentUpgrade.DataId) == 1 &&
                scaledEquipmentBuff.State.Context.FindCard(card.InstanceId)!.Permanent.Upgrades
                    .Single(upgrade => upgrade.DataId == equipmentUpgrade.DataId).Stats.Damage == 4),
            "Scaled BuffEquipment did not accumulate equipment and host upgrades.");
        var strongerEquipmentDescriptor = new CardMaskDescriptor(new CardMaskDefinition("strong-equipment", "Equipment", "Common",
            false, false, false, [], [], [], [], null, false, 0, 0, false, false, false), 1,
            [new("CardTraitStrongerMagicPower", "CardTraitStrongerMagicPower", 0, 0, true, 0)], []);
        CardInstanceState StrongEquipment(int id) => new(id, "strong-equipment", CardModifiers.Empty(), CardModifiers.Empty(),
            0, 0, 0, [], maskDescriptor: strongerEquipmentDescriptor);
        CardInstanceState[] strongerEquipmentCards = [StrongEquipment(40), StrongEquipment(41)];
        var strongerEquipmentContext = new CombatContext(equipmentContext.Cards, rng, 0, 42, 10,
            cardInstances: strongerEquipmentCards, cardRegistry: strongerEquipmentCards);
        var strongerEquipmentUpgrade = new CardUpgradeModifier(equipmentUpgrade.DataId, equipmentUpgrade.AssetKey,
            equipmentUpgrade.Stats, equipmentUpgrade.Statuses, false, false, false, 0, 0, [],
            maskMetadata: new CardUpgradeMaskMetadata([], false, false, false, false));
        var strongerBuffEquipment = new CardActionEffect("BuffEquipment", "DropTargetCharacter", 0, false, true, [],
            upgrade: strongerEquipmentUpgrade, lifetime: "TemporaryUntilUnitDeath", scaleUpgrade: true);
        var strongerEquipmentRoom = new RoomCombatState(0, false,
            [new CombatUnit(39, "strong-equipped-unit", CombatTeam.Player, 5, 20, 20, true, false, false, [],
                modifiers: new UnitModifiers(5, 0, 0, 1, 2, true, false, []), equipmentCards: [40, 41])], [], strongerEquipmentContext);
        RoomCombatResult firstStrongBuff = CardSpellModel.Apply(strongerEquipmentRoom, [strongerBuffEquipment], 39);
        Require(firstStrongBuff.Supported, "Strong-magic equipment buff failed: " + firstStrongBuff.UnsupportedReason);
        Require(firstStrongBuff.State!.Units.Single().Attack == 17 && strongerEquipmentCards.All(card =>
            firstStrongBuff.State.Context!.FindCard(card.InstanceId)!.Permanent.Upgrades
                .Single(upgrade => upgrade.DataId == equipmentUpgrade.DataId).Stats.Damage == 10),
            "Strong-magic BuffEquipment did not scale each first equipment upgrade while applying the raw host increment after the first item.");
        RoomCombatResult repeatedStrongBuff = CardSpellModel.Apply(firstStrongBuff.State!, [strongerBuffEquipment], 39);
        Require(repeatedStrongBuff.Supported && repeatedStrongBuff.State!.Units.Single().Attack == 21 &&
            strongerEquipmentCards.All(card => repeatedStrongBuff.State.Context!.FindCard(card.InstanceId)!.Permanent.Upgrades
                .Single(upgrade => upgrade.DataId == equipmentUpgrade.DataId).Stats.Damage == 12),
            "Scaled BuffEquipment did not apply magic power only to the first equipment upgrade: attack=" +
            (repeatedStrongBuff.State?.Units.Single().Attack.ToString() ?? "missing") + ", damage=" + string.Join(",",
                strongerEquipmentCards.Select(card => repeatedStrongBuff.State?.Context?.FindCard(card.InstanceId)?.Permanent.Upgrades
                    .SingleOrDefault(upgrade => upgrade.DataId == equipmentUpgrade.DataId)?.Stats.Damage.ToString() ?? "missing")) +
            ", reason=" + repeatedStrongBuff.UnsupportedReason);
        var existingUpgradeUnit = new CombatUnit(39, "upgraded-equipped-unit", CombatTeam.Player, 7, 20, 20, true,
            false, false, [], modifiers: new UnitModifiers(7, 2, 0, 1, 2, true, false, [equipmentUpgrade]),
            equipmentCards: [40, 41]);
        var existingUpgradeRoom = new RoomCombatState(0, false, [existingUpgradeUnit], [], equipmentContext);
        RoomCombatResult existingUpgradeBuff = CardSpellModel.Apply(existingUpgradeRoom, [buffEquipment], 39);
        Require(existingUpgradeBuff.Supported && existingUpgradeBuff.State!.Units.Single().Attack == 11 &&
            existingUpgradeBuff.State.Units.Single().Modifiers!.Upgrades.Single().Stats.Damage == 2,
            "BuffEquipment changed the host's accumulated CardUpgradeState instead of only scaling live stats.");
        var unscaledEquipmentBuff = new CardActionEffect("BuffEquipment", "DropTargetCharacter", 0, false, true, [],
            upgrade: equipmentUpgrade, lifetime: "TemporaryUntilUnitDeath");
        RoomCombatResult unscaledBuff = CardSpellModel.Apply(equipmentRoom, [unscaledEquipmentBuff], 39);
        Require(unscaledBuff.Supported && unscaledBuff.State!.Units.Single().Attack == 9 &&
            unscaledBuff.State.Units.Single().Modifiers!.Upgrades.Count(upgrade => upgrade.DataId == equipmentUpgrade.DataId) == 2,
            "Unscaled BuffEquipment did not apply one host upgrade for every equipped card.");
        Require(JsonSerializer.Serialize(equipmentRoom) == equipmentParent,
            "BuffEquipment mutated its source state.");
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
        Console.WriteLine("SPELL-CHECKS PASS: status multiplication/adjustment, propagation filters, positive-status multiplication, armor/shield, flat damage, lethal follow-ups, valor, rearrange, immunity and isolation.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}

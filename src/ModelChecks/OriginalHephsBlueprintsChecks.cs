using System;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalHephsBlueprintsChecks
{
    private const string RelicId = "5152b7d0-4f8a-4b7b-8871-06c25bb1a5af";
    private const string RelicKey = "HephsBlueprints";
    private const string Effect = "RelicEffectModifyForgeUpgrade";
    private const int ForgePoints = 3;

    internal static void Run()
    {
        UnityRng rng = UnityRng.Seed(43);
        CardUpgradeModifier defaultUpgrade = Upgrade("native-default", damage: 1, health: 2);
        CardUpgradeModifier relicUpgrade = Upgrade("hephs-synthetic", damage: 2, statuses: [new CombatStatus("armor", 2)]);
        CardUpgradeMaskRule equipmentMask = new("OnlyEquipmentNoEnemyTargetted", "Equipment");
        CombatRelicState owner = new(RelicId, RelicKey, [Effect], isCovenant: false, disallowedInPlacementPhase: false);
        ForgeUpgradeDefinition forge = new(defaultUpgrade, false,
            [new ForgeUpgradeModifier(RelicId, RelicKey, 0, relicUpgrade, [equipmentMask])]);

        CardInstanceState equipment = MaskedCard(42, "heph-equipment", "Equipment");
        CombatContext equipmentContext = Context(equipment, owner, rng);
        CardPlayRule equipmentRule = new(equipment.DataId, "HephsBlueprintsEquipment", 0, "Equipment", "Standby", null, [],
            equipment: new EquipmentDefinition([]), cardType: "Equipment", forgeEligible: true);
        var equipmentRules = new BattlePlayRules([], [equipmentRule], forgeUpgrade: forge);
        ForgeUpgradeModelResult positive = ForgeUpgradeModel.BeforeDirectCardPlay(equipmentContext, equipmentRules,
            equipmentRule, equipment, ForgePoints);
        CardInstanceState forgedEquipment = positive.Context?.FindCard(equipment.InstanceId)
            ?? throw new InvalidOperationException("HephsBlueprints forge model did not return the test equipment: " + positive.UnsupportedReason);
        CardUpgradeModifier scaledOwner = forgedEquipment.Temporary.Upgrades.Single(upgrade => upgrade.DataId == relicUpgrade.DataId);
        Require(positive.Supported && positive.Context!.QueryFrame!.ForgePoints == 0 && forgedEquipment.LastForgedAmount == ForgePoints &&
            scaledOwner.Stats.Damage == 6 && scaledOwner.Statuses.Single(status => status.Id == "armor").Stacks == 6,
            "Three forge points did not scale the Equipment-only HephsBlueprints upgrade.");
        Require(equipmentContext.QueryFrame!.ForgePoints == ForgePoints &&
            equipmentContext.FindCard(equipment.InstanceId)!.Temporary.Upgrades.Count == 0,
            "HephsBlueprints forge simulation mutated its immutable input.");

        CardInstanceState monster = MaskedCard(43, "heph-monster", "Monster");
        CombatContext monsterContext = Context(monster, owner, rng);
        CardPlayRule monsterRule = new(monster.DataId, "HephsBlueprintsMonster", 0, "SpawnMonster", "Standby", null, [],
            cardType: "Monster", forgeEligible: true);
        var monsterRules = new BattlePlayRules([], [monsterRule], forgeUpgrade: forge);
        ForgeUpgradeModelResult filterControl = ForgeUpgradeModel.BeforeDirectCardPlay(monsterContext, monsterRules,
            monsterRule, monster, ForgePoints);
        CardInstanceState controlledMonster = filterControl.Context?.FindCard(monster.InstanceId)
            ?? throw new InvalidOperationException("HephsBlueprints filter control omitted the test monster.");
        Require(filterControl.Supported && controlledMonster.LastForgedAmount == ForgePoints &&
            controlledMonster.Temporary.Upgrades.Any(upgrade => upgrade.DataId == defaultUpgrade.DataId && upgrade.Stats.Damage == 3) &&
            controlledMonster.Temporary.Upgrades.All(upgrade => upgrade.DataId != relicUpgrade.DataId),
            "The Equipment-only HephsBlueprints filter upgraded a monster or omitted its default forge control.");

        CombatContext inactive = equipmentContext.WithQueryFrame(equipmentContext.QueryFrame!.With(forgeTrackingActive: false));
        ForgeUpgradeModelResult disabled = ForgeUpgradeModel.BeforeDirectCardPlay(inactive, equipmentRules, equipmentRule, equipment, ForgePoints);
        ForgeUpgradeModelResult zeroPoints = ForgeUpgradeModel.BeforeDirectCardPlay(equipmentContext, equipmentRules, equipmentRule, equipment, 0);
        CardPlayRule ineligible = new(equipment.DataId, "HephsBlueprintsEquipment", 0, "Equipment", "Standby", null, [],
            equipment: new EquipmentDefinition([]), cardType: "Equipment", forgeEligible: false);
        ForgeUpgradeModelResult excluded = ForgeUpgradeModel.BeforeDirectCardPlay(equipmentContext, equipmentRules, ineligible, equipment, ForgePoints);
        Require(disabled.Supported && disabled.Context!.QueryFrame!.ForgePoints == ForgePoints &&
            zeroPoints.Supported && zeroPoints.Context!.QueryFrame!.ForgePoints == ForgePoints &&
            excluded.Supported && excluded.Context!.QueryFrame!.ForgePoints == ForgePoints,
            "An inactive, zero-point or ineligible forge spent points for HephsBlueprints.");
        Console.WriteLine("ORIGINAL-HEPHS-BLUEPRINTS-CHECKS PASS: Equipment effect, type filter, +2 damage/+2 armor scaled by three, monster/default-forge control, inactive/zero/ineligible guards and immutable input.");
    }

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 200 &&
            root.GetProperty("ModifierScenario").GetString() == "original-hephs-blueprints" &&
            root.GetProperty("OriginalHephsBlueprintsPrepared").GetBoolean() &&
            root.GetProperty("NativeWon").GetBoolean() && root.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                .All(name => root.GetProperty(name).GetInt32() == 0),
            "Original HephsBlueprints battle is incomplete or has native/model failures.");

        FixtureValue evidence = root.GetProperty("OriginalHephsBlueprintsDefinition");
        Require(evidence.GetProperty("OwnerId").GetString() == RelicId &&
            evidence.GetProperty("AssetKey").GetString() == RelicKey &&
            evidence.GetProperty("Effect").GetString() == Effect &&
            evidence.GetProperty("UpgradeId").GetString()?.Length > 0 &&
            evidence.GetProperty("FilterCount").GetInt32() > 0,
            "The original HephsBlueprints owner or native forge upgrade definition is absent.");

        FixtureValue[] entries = root.GetProperty("Actions").EnumerateArray()
            .Where(row => row.GetProperty("ScenarioAction").GetBoolean())
            .OrderBy(row => row.GetProperty("Index").GetInt32()).ToArray();
        Require(entries.Length == 2, "HephsBlueprints must capture a filter-control summon and one eligible equipment play.");
        foreach (FixtureValue entry in entries)
            Require(entry.GetProperty("Actual").ValueKind != FixtureKind.Null &&
                entry.GetProperty("Difference").ValueKind == FixtureKind.Null,
                "A scenario HephsBlueprints action did not match the native game.");

        BattleTurnState firstBefore = entries[0].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState firstAfter = entries[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction firstAction = entries[0].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext firstContext = firstBefore.Spawn.Train.Context!;
        CardPlayRule monsterRule = firstBefore.PlayRules!.Cards.Single(rule => rule.DataId == firstContext.Cards.Hand
            .Single(card => card.InstanceId == firstAction.CardInstanceId).DataId);
        CardInstanceState controlledCard = firstAfter.Spawn.Train.Context!.FindCard(firstAction.CardInstanceId)!;
        CombatUnit spawned = firstAfter.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.SpawnerCardId == firstAction.CardInstanceId);
        CardUpgradeModifier nativeDefault = firstBefore.PlayRules.ForgeUpgrade!.DefaultUpgrade!;
        Require(firstBefore.ForgePoints == ForgePoints && monsterRule.Effect == "SpawnMonster" &&
            monsterRule.ForgeEligible == true && controlledCard.LastForgedAmount == ForgePoints &&
            controlledCard.Temporary.Upgrades.Any(upgrade => upgrade.DataId == nativeDefault.DataId && upgrade.Stats.Damage == nativeDefault.Stats.Damage * ForgePoints) &&
            controlledCard.Temporary.Upgrades.All(upgrade => upgrade.DataId != evidence.GetProperty("UpgradeId").GetString()) && spawned.MaxHealth > 0,
            "The Equipment-only HephsBlueprints owner affected the native monster control action.");

        BattleTurnState before = entries[1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = entries[1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = entries[1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext beforeContext = before.Spawn.Train.Context!;
        CombatContext afterContext = after.Spawn.Train.Context!;
        string cardDataId = beforeContext.Cards.Hand.Single(card => card.InstanceId == action.CardInstanceId).DataId;
        CardPlayRule cardRule = before.PlayRules!.Cards.Single(rule => rule.DataId == cardDataId);
        ForgeUpgradeDefinition forge = before.PlayRules.ForgeUpgrade!;
        ForgeUpgradeModifier relicUpgrade = forge.RelicUpgrades.Single(upgrade => upgrade.RelicDataId == RelicId);
        CombatRelicState owner = beforeContext.Relics!.Single(relic => relic.DataId == RelicId);
        CardInstanceState sourceCard = beforeContext.FindCard(action.CardInstanceId)
            ?? throw new InvalidOperationException("The HephsBlueprints equipment is missing from native input state.");
        CardInstanceState forgedCard = afterContext.FindCard(action.CardInstanceId)
            ?? throw new InvalidOperationException("The HephsBlueprints forged equipment is missing from native card state.");
        CardUpgradeModifier actualRelicUpgrade = forgedCard.Temporary.Upgrades.Single(upgrade =>
            upgrade.DataId == evidence.GetProperty("UpgradeId").GetString());
        CombatUnit equipped = after.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.Id == action.TargetUnitId);
        CardUpgradeModifier actualEquippedUpgrade = equipped.Modifiers!.Upgrades.Single(upgrade =>
            upgrade.DataId.Length == 0 && upgrade.AssetKey.Length == 0 && upgrade.EquipmentSourceCardId == null);
        int expectedEquipmentDamage = forgedCard.Temporary.Upgrades.Sum(upgrade => upgrade.Stats.Damage);
        int expectedEquipmentArmor = forgedCard.Temporary.Upgrades.SelectMany(upgrade => upgrade.Statuses)
            .Where(status => status.Id == "armor").Sum(status => status.Stacks);
        ForgeUpgradeModelResult predicted = ForgeUpgradeModel.BeforeDirectCardPlay(beforeContext, before.PlayRules,
            cardRule, sourceCard, ForgePoints);
        CardUpgradeModifier predictedUpgrade = predicted.Context?.FindCard(action.CardInstanceId)?.Temporary.Upgrades
            .SingleOrDefault(upgrade => upgrade.DataId == actualRelicUpgrade.DataId)
            ?? throw new InvalidOperationException("HephsBlueprints forge prediction omitted its original upgrade.");

        Require(before.ForgePoints == ForgePoints && beforeContext.QueryFrame?.ForgePoints == ForgePoints &&
            beforeContext.QueryFrame.ForgeTrackingActive == true && beforeContext.QueryFrame.ForgeToggleActive == true &&
            cardRule.ForgeEligible == true && cardRule.Effect == "Equipment" && cardRule.CardType == "Equipment" &&
            owner.AssetKey == RelicKey && owner.EffectTypes.SequenceEqual([Effect]) &&
            relicUpgrade.RelicAssetKey == RelicKey && relicUpgrade.Upgrade.DataId == evidence.GetProperty("UpgradeId").GetString() &&
            relicUpgrade.Filters.Count == evidence.GetProperty("FilterCount").GetInt32() &&
            relicUpgrade.Filters.Single().CardType == "Equipment",
            "Native forge inputs do not match the unchanged HephsBlueprints owner and Equipment filter.");
        Require(predicted.Supported && afterContext.QueryFrame?.ForgePoints == 0 && forgedCard.LastForgedAmount == ForgePoints &&
            equipped.EquipmentCards?.Contains(action.CardInstanceId) == true && actualRelicUpgrade.Stats.Damage == 6 &&
            actualRelicUpgrade.Statuses.Single(status => status.Id == "armor").Stacks == 6 &&
            Serialize(predictedUpgrade) == Serialize(actualRelicUpgrade) &&
            equipped.EquipmentCards?.Contains(action.CardInstanceId) == true &&
            actualEquippedUpgrade.Stats.Damage == expectedEquipmentDamage &&
            actualEquippedUpgrade.Statuses.Single(status => status.Id == "armor").Stacks == expectedEquipmentArmor,
            "The shared forge model did not reproduce HephsBlueprints scaling and the installed equipment upgrade.");
        BattleActionChecks.Native(root);
        Console.WriteLine("NATIVE-HEPHS-BLUEPRINTS PASS: Equipment-only monster control, eligible three-point equipment, scaled original upgrade installed on host, matching model action.");
    }

    private static CombatContext Context(CardInstanceState card, CombatRelicState owner, UnityRng rng) =>
        new(new([new CardToken(card.InstanceId, card.DataId)], [], [], rng, 0, []), rng, 0, 43, 10,
            cardInstances: [card], cardRegistry: [card],
            queryFrame: new StatisticQueryFrame(forgePoints: ForgePoints, forgeTrackingActive: true, forgeToggleActive: true),
            relics: [owner]);

    private static CardInstanceState MaskedCard(int id, string dataId, string type)
    {
        var definition = new CardMaskDefinition(dataId, type, "Common", type == "Monster", type == "Monster",
            type == "Monster", [], [], [], [], null, false, 1, 0, false, false, false);
        var descriptor = new CardMaskDescriptor(definition, 0, [], []);
        return CardInstanceState.Empty(id, dataId).WithMaskDescriptor(descriptor);
    }

    private static CardUpgradeModifier Upgrade(string id, int damage = 0, int health = 0,
        IReadOnlyList<CombatStatus>? statuses = null)
    {
        CombatStatus[] values = (statuses ?? Array.Empty<CombatStatus>()).ToArray();
        var metadata = new CardUpgradeMaskMetadata(values.Select(status =>
            new UpgradeMaskStatus(status.Id, status.Stacks, false)).ToArray(), false, false, false, false);
        return new CardUpgradeModifier(id, id, new CardStatModifier(damage: damage, health: health), values,
            false, false, false, 0, 0, [], maskMetadata: metadata);
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

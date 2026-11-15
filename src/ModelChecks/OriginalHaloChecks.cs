using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalHaloChecks
{
    private const string Owner = "cbfec4a4-00fe-406e-b52a-e35e6456ba7d";
    private const string Modifier = "RoomStateSelfDamagePerMissingHp";
    private static void Require(bool condition, string reason) => TrueFinalBossChecks.Require(condition, reason);

    internal static void Catalog(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        var cards = root.GetProperty("Cards").EnumerateArray().ToArray();
        var logbook = cards.Where(card => card.GetProperty("AppearsInLogbook").GetBoolean()).ToArray();
        Require(root.GetProperty("GameVersion").GetString() == "2.3.1" && cards.Length == 724 && logbook.Length == 614 &&
            cards.Select(card => card.GetProperty("Id").GetString()).Distinct().Count() == 724 &&
            root.GetProperty("DecisionUnchanged").GetBoolean() &&
            root.GetProperty("FrameBefore").GetInt32() == root.GetProperty("FrameAfter").GetInt32() &&
            root.GetProperty("RngBefore").ContentEquals(root.GetProperty("RngAfter")),
            "Catalog identities, version, decision, RNG or frame changed.");
        Require(logbook.All(card => card.GetProperty("ProjectionError").ValueKind == FixtureKind.Null &&
            card.GetProperty("Rule").GetProperty("ExternalInteractions").GetArrayLength() == 0),
            "Logbook definition projection contains errors or explicit guards.");
        Require(logbook.Single(card => card.GetProperty("Id").GetString() == Owner)
            .GetProperty("Rule").GetProperty("Equipment").GetProperty("Upgrades").EnumerateArray().Single()
            .Deserialize<CardUpgradeModifier>() is { SelfAttackRules.Count: 1, ExternalInteractions.Count: 0 },
            "Unmodified catalog lost the typed Halo equipment rule.");
        Console.WriteLine("HALO-CATALOG PASS: 724 unique definitions, 614 logbook cards, zero projection errors/explicit guards; decision/RNG/frame unchanged. Definition audit only; whole-card combinations remain separate acceptance.");
    }

    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path); var root = document.RootElement;
        Require(root.GetProperty("OriginalHaloPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Incomplete original Halo battle.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero native " + key);
        var definition = root.GetProperty("OriginalHaloDefinition");
        var effect = definition.GetProperty("Effects").EnumerateArray().Single();
        var upgrade = definition.GetProperty("Upgrade").Deserialize<CardUpgradeModifier>()!;
        var nativeModifier = definition.GetProperty("Modifiers").EnumerateArray().Single();
        Require(definition.GetProperty("Id").GetString() == Owner && definition.GetProperty("Cost").GetInt32() == 1 &&
            definition.GetProperty("Traits").GetArrayLength() == 0 &&
            effect.GetProperty("Type").GetString() == "CardEffectAttachEquipment" &&
            effect.GetProperty("Target").GetString() == "DropTargetCharacter" && effect.GetProperty("Team").GetString() == "Monsters" &&
            upgrade is { DataId: "d66f5d82-9174-4212-b85c-e89b56f81be7", Stats.Health: 10, Stats.Damage: 0,
                Statuses.Count: 0, ExternalInteractions.Count: 0, SelfAttackRules.Count: 1 } &&
            upgrade.SelfAttackRules.Single() is { Type: Modifier, Coefficient: 1, Divisor: 1, StatusId.Length: 0 } &&
            nativeModifier.GetProperty("Type").GetString() == Modifier && nativeModifier.GetProperty("Int").GetInt32() == 1 &&
            nativeModifier.GetProperty("Int2").GetInt32() == 1 && !nativeModifier.GetProperty("DisallowedInDeployment").GetBoolean(),
            "Original cost, equipment targeting, +10 HP or missing-health room modifier changed.");
        int[] ids = root.GetProperty("OriginalHaloIds").Deserialize<int[]>()!;
        var actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 25 && ids.Distinct().Count() == 25 && actions.Length == 25 &&
            actions.Select(row => row.GetProperty("Action").Deserialize<PlayCardAction>()!.CardInstanceId).SequenceEqual(ids),
            "Missing ordered original equipment casts and independent controls.");
        BattleTurnState Before(int index) => actions[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState After(int index) => actions[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit Unit(BattleTurnState state, int card = 1) => state.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Single(unit => unit.SpawnerCardId == ids[card]);
        int Attack(int index, int card = 1)
        {
            var state = After(index); return RelicResourceModel.AttackDamage(Unit(state, card), state.Spawn.Train.Context);
        }
        int Rules(int index, int card = 1) => Unit(After(index), card).Modifiers!.SelfAttackRules?.Count ?? 0;
        foreach (var item in new[] { (3, 0), (5, 7), (6, 4), (8, 4), (9, 8), (10, 14), (11, 14), (12, 14), (13, 14),
            (14, 0), (15, 0), (16, 0), (18, 0), (19, 5) })
            Require(Attack(item.Item1) == item.Item2, "Host A contextual attack differs after action " + item.Item1);
        foreach (var item in new[] { (12, 0), (17, 3), (19, 3), (20, 3), (21, 7), (22, 0), (23, 0), (24, 2) })
            Require(Attack(item.Item1, 2) == item.Item2, "Host B contextual attack differs after action " + item.Item1);
        Require(Rules(3) == 1 && Rules(9) == 2 && Rules(14) == 0 && Rules(18) == 1 && Rules(17, 2) == 1 && Rules(23, 2) == 1 &&
            Unit(After(9)).EquipmentCards!.SequenceEqual(new[] { ids[3], ids[9] }) &&
            Unit(After(14)).EquipmentCards!.Count == 0 && Unit(After(14)).MaxHealth == 30 && Unit(After(14)).Health == 30 &&
            Unit(After(23), 2).EquipmentCards!.SequenceEqual(new[] { ids[23] }) &&
            Unit(Before(23), 2).EquipmentCards!.SequenceEqual(new[] { ids[17] }),
            "Stacking, remove-all, reinstallation or capacity-one oldest replacement differs.");
        Require(Unit(Before(8)).Status("armor")?.Stacks == 5 && Unit(After(8)).Status("armor") == null &&
            Unit(Before(8)).Health == Unit(After(8)).Health &&
            After(11).Spawn.Train.Rooms.Single(room => room.RoomIndex == 1).Units.Any(unit => unit.SpawnerCardId == ids[1]) &&
            After(13).Spawn.Train.Rooms.Single(room => room.RoomIndex == 0).Units.Any(unit => unit.SpawnerCardId == ids[1]) &&
            After(20).Spawn.Train.Rooms.SelectMany(room => room.Units).All(unit => unit.SpawnerCardId != ids[1]) &&
            Unit(Before(20), 2).Health == Unit(After(20), 2).Health && Unit(After(22), 2).Health == Unit(After(22), 2).MaxHealth,
            "Armor absorption, movement, death isolation or full-healing controls are missing.");
        int queries = 0, positive = 0; bool stacked = false, moved = false, fullHealth = false;
        foreach (var record in root.GetProperty("UnitSelfAttackQueries").EnumerateArray())
        {
            var room = record.GetProperty("Room").Deserialize<RoomCombatState>()!;
            foreach (var query in record.GetProperty("Attacks").EnumerateArray())
            {
                var unit = room.Units.Single(actor => actor.Id == query.GetProperty("UnitId").GetInt32());
                int actual = query.GetProperty("Attack").GetInt32();
                Require(RelicResourceModel.AttackDamage(unit, room.Context) == actual,
                    "Independent native contextual attack differs for unit " + unit.Id);
                queries++;
                int count = unit.Modifiers?.SelfAttackRules?.Count(rule => rule.Type == Modifier) ?? 0;
                if (count == 0) continue;
                fullHealth |= unit.Health == unit.MaxHealth && actual == unit.Attack;
                if (actual <= unit.Attack) continue;
                positive++; stacked |= count == 2; moved |= room.RoomIndex == 1;
            }
        }
        Require(positive > 0 && stacked && moved && fullHealth, "Missing positive, stacked, moved or full-health native getter witness.");
        Require(root.GetProperty("EquipmentOperations").GetArrayLength() == 9, "Changed attachment/removal operation coverage.");
        EquipmentChecks.Native(root);
        RoomCombatChecks.NativeRoomOnly(path); TrainCombatChecks.Native(root); RecordedBattleChecks.Native(root);
        BattleSearchChecks.FrontierDiagnostics(root); BattleSearchChecks.Native(root, requireZeroUnsupported: true);
        Console.WriteLine($"ORIGINAL-HALO PASS: original +10 HP/missing-health equipment; {queries} exact native attack getters ({positive} positive); armor/healing, two-copy stacking, movement, remove-all/reinstall, host death isolation and oldest replacement; full chain/16 branches and zero-unsupported serial/parallel search.");
    }
}

using System.Security.Cryptography;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HivemindChecks
{
    private const string Boundary = "NativeHivemindTurnSettlement";
    private const string FirstTurnBoundary = "NativeHivemindFirstTurn";
    private const string SecondTurnBoundary = "NativeHivemindSecondTurn";
    private const string Scenario = "SoulSavior_R2_BossBattle_FavoredChild_3";
    private const string Boss = "Boss_SoulSavior_R2_Indulgent_Titanskin";
    private const string Hunter = "RAny_Attacker_Hunter_Hivemind_Ver2";
    private const string Feed = "R2_Support_FeedInfested";
    private const string Heavy = "R2_Heavy_HealGorge";
    private const string Upgrade = "RoomStateHivemindModifierUpgrade";

    internal static void ExportTurnComponent(string battlePath, string outputPath, bool secondTurn = false)
    {
        using var source = ModelJson.ReadFixture(battlePath);
        FixtureValue battle = source.RootElement;
        int index = secondTurn ? 1 : 0;
        Turn(battle, index);
        if (secondTurn) SecondDraw(battle);
        else Callbacks(battle, requirePreviewFeed: true);
        FixtureValue turn = Row(battle, "Turns", index);
        var builder = new FixtureBuilder();
        var fields = new List<int>();
        foreach (string name in new[] { "Index", "Before", "Actual", "ActualOutcome" })
            fields.AddRange(new[] { Key(name), builder.Import(turn.GetProperty(name)) });
        if (turn.TryGetProperty("RelentlessDecisions", out FixtureValue decisions))
            fields.AddRange(new[] { Key("RelentlessDecisions"), builder.Import(decisions) });
        int row = builder.Add(FixtureKind.Object, children: fields.ToArray());
        using var stream = File.OpenRead(battlePath);
        var properties = new List<int> {
            Key("Schema"), builder.Add(FixtureKind.Number, "1"),
            Key("Boundary"), Key(secondTurn ? SecondTurnBoundary : FirstTurnBoundary),
            Key("SourceBattleSha256"), Key(Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant()),
            Key("GameVersion"), builder.Import(battle.GetProperty("GameVersion")),
            Key("GameModuleMvid"), builder.Import(battle.GetProperty("GameModuleMvid")),
            Key("CaptureFailures"), builder.Import(battle.GetProperty("CaptureFailures")),
            Key("Pending"), builder.Import(battle.GetProperty("Pending")),
            Key("OriginalUnrulyBattle"), builder.Import(battle.GetProperty("OriginalUnrulyBattle")),
            Key("Turns"), builder.Add(FixtureKind.Array, children: [row])
        };
        if (secondTurn)
        {
            FixtureValue draw = Row(battle, "CardCycles", 3);
            properties.Add(Key("CardCycles"));
            properties.Add(builder.Add(FixtureKind.Array, children: [builder.Import(draw)]));
        }
        int root = builder.Add(FixtureKind.Object, children: properties.ToArray());
        using FixtureDocument component = builder.Build(root, 0, new byte[32]);
        TurnComponent(component.RootElement, secondTurn);
        using var output = new FileStream(outputPath, FileMode.CreateNew);
        component.Write(output);

        int Key(string value) => builder.Add(FixtureKind.String, value);
    }

    internal static void TurnComponent(FixtureValue component, bool secondTurn)
    {
        string boundary = secondTurn ? SecondTurnBoundary : FirstTurnBoundary;
        if (Number(component, "Schema") != 1 || component.GetProperty("Boundary").GetString() != boundary ||
            component.GetProperty("SourceBattleSha256").GetString() is not { Length: 64 } hash || !hash.All(Uri.IsHexDigit) ||
            component.GetProperty("Turns").GetArrayLength() != 1)
            throw new InvalidDataException("Expected an original Hivemind turn component with source provenance.");
        Turn(component, secondTurn ? 1 : 0);
        if (secondTurn) SecondDraw(component);
        else Callbacks(component, requirePreviewFeed: true);
    }

    internal static void Export(string battlePath, string outputPath)
    {
        using var source = ModelJson.ReadFixture(battlePath);
        FixtureValue battle = source.RootElement;
        CheckBuild(battle);
        CheckScenario(battle.GetProperty("OriginalUnrulyBattle"));
        if (Number(battle, "CaptureFailures") != 0 || Number(battle, "Pending") != 0 ||
            Number(battle, "Mismatches") != 0)
            throw new InvalidDataException("The original observation contains capture failures, pending rows, or mismatches.");

        FixtureValue firstSpawn = Row(battle, "Spawns", 0);
        if (firstSpawn.GetProperty("IncludeTreasure").GetBoolean())
            throw new InvalidDataException("Expected the original deployment wave.");
        FixtureValue firstStage = Row(battle, "Stages", 14);
        FixtureValue secondStage = Row(battle, "Stages", 20);
        var builder = new FixtureBuilder();
        using var stream = File.OpenRead(battlePath);
        string hash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        int root = builder.Add(FixtureKind.Object, children: [
            Key("Schema"), builder.Add(FixtureKind.Number, "1"),
            Key("Boundary"), Key(Boundary),
            Key("SourceBattleSha256"), Key(hash),
            Key("GameVersion"), builder.Import(battle.GetProperty("GameVersion")),
            Key("GameModuleMvid"), builder.Import(battle.GetProperty("GameModuleMvid")),
            Key("Observation"), builder.Import(battle.GetProperty("OriginalUnrulyBattle")),
            Key("FirstSpawnRooms"), builder.Import(SpawnRooms(firstSpawn.GetProperty("Actual"))),
            Key("FirstDrawRooms"), builder.Import(DrawRooms(battle, 1, 1)),
            Key("FirstDrawContext"), builder.Import(Row(battle, "CardCycles", 1).GetProperty("StandbyContext")),
            Key("FirstSettledRooms"), builder.Import(SettledRooms(battle, 0)),
            Key("FirstRoom"), builder.Import(RoomStart(firstStage, 1, 0)),
            Key("SecondDrawRooms"), builder.Import(DrawRooms(battle, 3, 2)),
            Key("SecondSettledRooms"), builder.Import(SettledRooms(battle, 1)),
            Key("SecondRoom"), builder.Import(RoomStart(secondStage, 2, 1))
        ]);
        using FixtureDocument component = builder.Build(root, 0, new byte[32]);
        Native(component.RootElement);
        using var output = new FileStream(outputPath, FileMode.CreateNew);
        component.Write(output);

        int Key(string value) => builder.Add(FixtureKind.String, value);
    }

    internal static void Native(FixtureValue component)
    {
        if (Number(component, "Schema") != 1 ||
            component.GetProperty("Boundary").GetString() != Boundary ||
            component.GetProperty("SourceBattleSha256").GetString() is not { Length: 64 } hash ||
            !hash.All(Uri.IsHexDigit))
            throw new InvalidDataException("Expected an original Hivemind component with binary source provenance.");
        CheckBuild(component);
        CheckScenario(component.GetProperty("Observation"));

        FixtureValue spawned = Room(component, "FirstSpawnRooms", 0);
        FixtureValue drawing = Room(component, "FirstDrawRooms", 0);
        FixtureValue settled = Room(component, "FirstSettledRooms", 0);
        FixtureValue firstRoom = component.GetProperty("FirstRoom");
        foreach (var (key, attack) in new[] { (Hunter, 3), (Feed, 0) })
        {
            FixtureValue born = Unit(spawned, key);
            CheckUnit(born, 1, attack, null);
            CheckUnit(SameUnit(drawing, born), 1, attack, null);
            CheckUnit(SameUnit(settled, born), 1, attack + 2, 2);
            CheckUnit(SameUnit(firstRoom, born), 1, attack + 2, 2);
        }
        FixtureValue heavy = Unit(spawned, Heavy);
        CheckUnit(heavy, 0, 8, null);
        CheckUnit(SameUnit(drawing, heavy), 0, 8, null);
        CheckUnit(SameUnit(settled, heavy), 0, 8, null);
        CheckUnit(SameUnit(firstRoom, heavy), 0, 8, null);

        // The second draw is preceded by native feed and movement. Its two floors
        // distinguish the tower-wide membership from an effect scoped to the current room.
        FixtureValue secondDrawLower = Room(component, "SecondDrawRooms", 0);
        FixtureValue secondDrawUpper = Room(component, "SecondDrawRooms", 1);
        FixtureValue secondLower = Room(component, "SecondSettledRooms", 0);
        FixtureValue secondUpper = Room(component, "SecondSettledRooms", 1);
        FixtureValue secondRoom = component.GetProperty("SecondRoom");
        FixtureValue newborn = Unit(secondDrawLower, Feed);
        if (Number(newborn, "Id") == Number(Unit(spawned, Feed), "Id"))
            throw new InvalidDataException("The original second wave must add a distinct FeedInfested actor.");
        CheckUnit(newborn, 1, 0, null);
        CheckUnit(SameUnit(secondLower, newborn), 1, 6, 6);
        foreach (var (key, stacks, attack, beforeUpgrade) in new[] { (Hunter, 1, 5, 2), (Feed, 1, 2, 2), (Heavy, 3, 8, 0) })
        {
            FixtureValue previous = Unit(spawned, key);
            CheckUnit(SameUnit(secondDrawUpper, previous), stacks, attack, beforeUpgrade);
            int updatedAttack = attack - beforeUpgrade + 6;
            CheckUnit(SameUnit(secondUpper, previous), stacks, updatedAttack, 6);
            CheckUnit(SameUnit(secondRoom, previous), stacks, updatedAttack, 6);
        }
        ReplayFirstDraw(component);
        Console.WriteLine("NATIVE-HIVEMIND COMPONENT PASS: first +2 after draw entry, persistence into combat, and second +6 across two floors.");
    }

    private static void ReplayFirstDraw(FixtureValue component)
    {
        RoomCombatState[] rooms = component.GetProperty("FirstDrawRooms").Deserialize<RoomCombatState[]>()!;
        var input = new TrainCombatState(rooms, Array.Empty<EnemyMovement>(), 4,
            component.GetProperty("FirstDrawContext").Deserialize<CombatContext>()!);
        string immutable = JsonSerializer.Serialize(input, ModelJson.Options);
        TrainCombatState? replay = HivemindTurnUpgrade.ApplyFirstOrdinaryTurn(input, out string? error);
        if (replay == null || immutable != JsonSerializer.Serialize(input, ModelJson.Options))
            throw new InvalidDataException("First native Hivemind upgrade projection failed: " + error);
        RoomCombatState actual = Room(component, "FirstSettledRooms", 0).Deserialize<RoomCombatState>()!;
        foreach (CombatUnit unit in replay.Rooms[0].Units.Where(unit => unit.Status(HivemindTurnUpgrade.StatusId) != null))
        {
            CombatUnit expected = actual.Units.Single(actor => actor.Id == unit.Id);
            string? difference = ModelJson.Difference(JsonSerializer.Serialize(unit, ModelJson.Options),
                JsonSerializer.Serialize(expected, ModelJson.Options));
            if (difference != null) throw new InvalidDataException("First settled native Hivemind actor differs: " + difference);
        }
        RoomCombatState[] laterRooms = component.GetProperty("SecondDrawRooms").Deserialize<RoomCombatState[]>()!;
        if (HivemindTurnUpgrade.ApplyFirstOrdinaryTurn(new TrainCombatState(laterRooms, Array.Empty<EnemyMovement>(), 4), out _) != null)
            throw new InvalidDataException("The later native feeding transition requires its own calibration.");
    }

    internal static void Turn(FixtureValue battle, int index = 0)
    {
        CheckBuild(battle);
        CheckScenario(battle.GetProperty("OriginalUnrulyBattle"));
        FixtureValue row = Row(battle, "Turns", index);
        BattleTurnState before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
        BattleTurnResult result = BattleTurnModel.EndTurn(before, RecordedBattleChecks.Decisions(row));
        if (!result.Supported || immutable != JsonSerializer.Serialize(before, ModelJson.Options))
            throw new InvalidDataException($"Original Hivemind EndTurn {index} failed: " + result.UnsupportedReason);
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(result.State, ModelJson.Options),
            JsonSerializer.Serialize(actual, ModelJson.Options));
        if (difference != null || (int)result.Outcome != Number(row, "ActualOutcome"))
            throw new InvalidDataException($"Original Hivemind EndTurn {index} differs: " + difference);
        Console.WriteLine($"NATIVE-HIVEMIND TURN {index} PASS.");
    }

    internal static void SecondDraw(FixtureValue battle)
    {
        CheckBuild(battle);
        CheckScenario(battle.GetProperty("OriginalUnrulyBattle"));
        var rooms = DrawRooms(battle, 3, 2).Deserialize<RoomCombatState[]>()!;
        var context = Row(battle, "CardCycles", 3).GetProperty("StandbyContext").Deserialize<CombatContext>()!;
        var source = new TrainCombatState(rooms, Array.Empty<EnemyMovement>(), 4, context);
        string immutable = JsonSerializer.Serialize(source, ModelJson.Options);
        TrainCombatState? state = HivemindTurnUpgrade.ApplySecondOrdinaryTurn(source, out string? error);
        if (state == null || immutable != JsonSerializer.Serialize(source, ModelJson.Options))
            throw new InvalidDataException("Second native Hivemind draw projection failed: " + error);
        RoomCombatState[] actual = SettledRooms(battle, 1).Deserialize<RoomCombatState[]>()!;
        var owners = state.Rooms.SelectMany(room => room.Units.Where(unit => unit.Status(HivemindTurnUpgrade.StatusId) != null)
            .Select(unit => (room.RoomIndex, Unit: unit))).ToArray();
        if (owners.Length != 4 || owners.Select(owner => owner.RoomIndex).Distinct().Count() != 2)
            throw new InvalidDataException("Second native Hivemind attack refresh needs four owners on two floors.");
        foreach (var owner in owners)
        {
            CombatUnit matched = actual[owner.RoomIndex].Units.Single(unit => unit.Id == owner.Unit.Id);
            string? difference = ModelJson.Difference(JsonSerializer.Serialize(owner.Unit, ModelJson.Options),
                JsonSerializer.Serialize(matched, ModelJson.Options));
            if (difference != null)
                throw new InvalidDataException("Second settled native Hivemind actor differs: " + difference);
        }
        Console.WriteLine("NATIVE-HIVEMIND SECOND DRAW PASS: four native actors and immutable source.");
    }

    internal static void Callbacks(FixtureValue battle, bool requirePreviewFeed)
    {
        CheckBuild(battle);
        FixtureValue observation = battle.GetProperty("OriginalUnrulyBattle");
        CheckScenario(observation);
        if (Number(battle, "CaptureFailures") != 0 || Number(battle, "Pending") != 0 ||
            !observation.TryGetProperty("HivemindCallbacks", out FixtureValue originalRows))
            throw new InvalidDataException("Expected the original turn-start callback observer receipt.");
        FixtureValue[] rows = originalRows.EnumerateArray()
            .Where(row => row.GetProperty("Kind").GetString() == "TurnStart").ToArray();
        if (rows.Length != 2 || Number(rows[0], "Sequence") >= Number(rows[1], "Sequence") ||
            !rows[0].GetProperty("After").ContentEquals(rows[1].GetProperty("Before")))
            throw new InvalidDataException("Expected two ordered turn-start callbacks with a continuous native boundary.");
        foreach (FixtureValue row in rows)
        {
            if (Number(row, "Turn") != 1 || row.GetProperty("Preview").GetBoolean() ||
                !row.GetProperty("Completed").GetBoolean() || row.GetProperty("PrimaryStage").GetString() != "OnMonsterTeamTurnBegin" ||
                row.GetProperty("AdditionalStages").GetArrayLength() != 0)
                throw new InvalidDataException("The original first-turn Hivemind callback timing changed.");
            FixtureValue before = row.GetProperty("Before");
            FixtureValue after = row.GetProperty("After");
            CheckRng(before, after);
        }
        FixtureValue initial = rows[0].GetProperty("Before");
        FixtureValue intermediate = rows[0].GetProperty("After");
        FixtureValue settled = rows[1].GetProperty("After");
        CheckActor(initial, intermediate, Hunter, Number(rows[0], "UnitId"), 1, 3, 5, null, 2);
        CheckActor(intermediate, settled, Feed, Number(rows[1], "UnitId"), 1, 0, 2, null, 2);
        if (!Actor(initial, Feed).ContentEquals(Actor(intermediate, Feed)) ||
            !Actor(intermediate, Hunter).ContentEquals(Actor(settled, Hunter)) ||
            !Actor(initial, Heavy).ContentEquals(Actor(intermediate, Heavy)) ||
            !Actor(intermediate, Heavy).ContentEquals(Actor(settled, Heavy)))
            throw new InvalidDataException("The observed Hivemind callback changed a non-target actor.");
        if (requirePreviewFeed)
        {
            FixtureValue[] changes = originalRows.EnumerateArray()
                .Where(row => row.GetProperty("Kind").GetString() == "OwnerStatusChanged").ToArray();
            if (changes.Length != 2 || !changes[0].GetProperty("Preview").GetBoolean() ||
                changes[1].GetProperty("Preview").GetBoolean() || Number(changes[0], "Sequence") <= Number(rows[1], "Sequence") ||
                Number(changes[1], "Sequence") <= Number(changes[0], "Sequence"))
                throw new InvalidDataException("Expected native preview followed by physical Hivemind feed marker application.");
            foreach (FixtureValue row in changes)
            {
                FixtureValue before = row.GetProperty("Before"), after = row.GetProperty("After");
                if (Number(row, "Turn") != 1 || !row.GetProperty("Completed").GetBoolean() ||
                    Number(Actor(before, Heavy), "UnitId") != Number(Actor(initial, Heavy), "UnitId") ||
                    Number(Actor(before, Heavy), "Health") != 48 || Number(Actor(before, Feed), "Health") != 20 ||
                    Number(Actor(before, Hunter), "Attack") != 5 || Number(Actor(before, Feed), "Attack") != 2 ||
                    !Actor(before, Hunter).ContentEquals(Actor(after, Hunter)) ||
                    !Actor(before, Feed).ContentEquals(Actor(after, Feed)))
                    throw new InvalidDataException("Unexpected original Hivemind feeding callback context.");
                CheckActor(before, after, Heavy, Number(row, "UnitId"), 3, 8, 8, null, 0);
                CheckRng(before, after);
            }
        }
        Console.WriteLine(requirePreviewFeed ?
            "NATIVE-HIVEMIND CALLBACKS PASS: ordered after-draw +2, zero-damage feed marker in preview and play, unchanged RNG." :
            "NATIVE-HIVEMIND CALLBACKS PASS: two original after-draw +2 applications and unchanged gameplay RNG.");

        static FixtureValue Actor(FixtureValue snapshot, string key) => snapshot.GetProperty("Actors").EnumerateArray()
            .Single(unit => unit.GetProperty("Key").GetString() == key);

        static void CheckActor(FixtureValue before, FixtureValue after, string key,
            int unitId, int stacks, int attackBefore, int attackAfter, int? upgradeBefore, int? upgradeAfter)
        {
            FixtureValue first = Actor(before, key);
            FixtureValue last = Actor(after, key);
            if (Number(first, "UnitId") != unitId || Number(last, "UnitId") != unitId ||
                Number(first, "Room") != Number(last, "Room") || Number(first, "Health") != Number(last, "Health") ||
                Number(first, "Stacks") != stacks || Number(last, "Stacks") != stacks ||
                Number(first, "Attack") != attackBefore || Number(last, "Attack") != attackAfter ||
                !UpgradesMatch(first, upgradeBefore) || !UpgradesMatch(last, upgradeAfter))
                throw new InvalidDataException("Unexpected native Hivemind application to " + key);
        }

        static bool UpgradesMatch(FixtureValue actor, int? damage)
        {
            FixtureValue upgrades = actor.GetProperty("Upgrades");
            return damage.HasValue ? upgrades.GetArrayLength() == 1 && upgrades[0].GetInt32() == damage.Value :
                upgrades.GetArrayLength() == 0;
        }

        static void CheckRng(FixtureValue before, FixtureValue after)
        {
            foreach (string stream in new[] { "Battle", "BattleTest", "CardDraw" })
                if (before.GetProperty(stream).GetArrayLength() != 4 ||
                    !before.GetProperty(stream).ContentEquals(after.GetProperty(stream)))
                    throw new InvalidDataException("The original Hivemind callback changed " + stream + " RNG.");
        }
    }

    private static void CheckBuild(FixtureValue row)
    {
        if (row.GetProperty("GameVersion").GetString() != Game231FixtureCatalog.Version ||
            row.GetProperty("GameModuleMvid").GetString() != Game231FixtureCatalog.Module)
            throw new InvalidDataException("Expected original 2.3.1 executable identity.");
    }

    private static void CheckScenario(FixtureValue row)
    {
        if (row.GetProperty("Scenario").GetString() != Scenario || row.GetProperty("BossKey").GetString() != Boss ||
            !row.GetProperty("RailforgedInstalled").GetBoolean() || !row.GetProperty("ControlledTestScenario").GetBoolean())
            throw new InvalidDataException("Expected the original installed Railforged controlled scenario.");
    }

    private static void CheckUnit(FixtureValue unit, int stacks, int attack, int? upgradeDamage)
    {
        int count = unit.GetProperty("Statuses").EnumerateArray()
            .Where(status => status.GetProperty("Id").GetString() == "hivemind")
            .Sum(status => Number(status, "Stacks"));
        var upgrades = unit.GetProperty("Modifiers").GetProperty("Upgrades").EnumerateArray()
            .Where(upgrade => upgrade.GetProperty("DataId").GetString() == Upgrade).ToArray();
        if (Number(unit, "Team") != (int)CombatTeam.Enemy || Number(unit, "Health") <= 0 ||
            count != stacks || Number(unit, "Attack") != attack || upgrades.Length != (upgradeDamage.HasValue ? 1 : 0))
            throw new InvalidDataException("Unexpected original Hivemind actor state: " + unit.GetProperty("AssetKey").GetString());
        if (!upgradeDamage.HasValue) return;
        FixtureValue upgrade = upgrades[0];
        FixtureValue stats = upgrade.GetProperty("Stats");
        if (!upgrade.GetProperty("Unique").GetBoolean() || !upgrade.GetProperty("Lifecycle").GetProperty("Unique").GetBoolean() ||
            Number(stats, "Damage") != upgradeDamage ||
            stats.EnumerateObject().Any(field => field.Name != "Damage" && field.Value.GetInt32() != 0) ||
            upgrade.GetProperty("Statuses").GetArrayLength() != 0)
            throw new InvalidDataException("The original Hivemind upgrade must be one unique attack-only upgrade.");
    }

    private static FixtureValue Row(FixtureValue battle, string key, int index) =>
        battle.GetProperty(key).EnumerateArray().Single(row => Number(row, "Index") == index);

    private static FixtureValue Room(FixtureValue component, string key, int index) =>
        component.GetProperty(key).EnumerateArray().Single(room => Number(room, "RoomIndex") == index);

    private static FixtureValue Unit(FixtureValue room, string key) =>
        room.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("AssetKey").GetString() == key);

    private static FixtureValue SameUnit(FixtureValue room, FixtureValue original) =>
        room.GetProperty("Units").EnumerateArray().Single(unit => Number(unit, "Id") == Number(original, "Id") &&
            unit.GetProperty("AssetKey").ContentEquals(original.GetProperty("AssetKey")));

    private static FixtureValue SpawnRooms(FixtureValue spawn) => spawn.GetProperty("Train").GetProperty("Rooms");
    private static FixtureValue SettledRooms(FixtureValue battle, int index) =>
        SpawnRooms(Row(battle, "Turns", index).GetProperty("Actual").GetProperty("Spawn"));

    private static FixtureValue DrawRooms(FixtureValue battle, int index, int turn)
    {
        FixtureValue row = Row(battle, "CardCycles", index);
        if (row.GetProperty("Kind").GetString() != "Draw" || Number(row, "Turn") != turn)
            throw new InvalidDataException("Expected the original normal draw boundary.");
        return row.GetProperty("StandbyContext").GetProperty("Enchantments").GetProperty("Rooms");
    }

    private static FixtureValue RoomStart(FixtureValue row, int turn, int room)
    {
        FixtureValue before = row.GetProperty("Before");
        if (row.GetProperty("Kind").GetString() != "Room" || Number(row, "Turn") != turn || Number(before, "RoomIndex") != room)
            throw new InvalidDataException("Expected the original settled room combat boundary.");
        return before;
    }

    private static int Number(FixtureValue row, string key) => row.GetProperty(key).GetInt32();
}

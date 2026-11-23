using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class LifemotherInfestedChecks
{
    private const string Scenario = "SoulSavior_R5_BossBattle_Lifemother_Infested";
    private const string BossKey = "Boss_SoulSavior_Final_Lifemother_Infested";
    private const string Boundary = "NativeLifemotherInfestedStatusAddition";
    private const string OpeningBoundary = "NativeLifemotherInfestedOpening";
    private const string FirstTurnBoundary = "NativeLifemotherInfestedFirstTurn";
    private const string FirstCombatBoundary = "NativeLifemotherInfestedFirstCombat";
    private const string LaterCombatBoundary = "NativeLifemotherInfestedLaterCombat";
    private const string DrawBoundary = "NativeLifemotherInfestedLaterDraws";
    private const string SpawnBoundary = "NativeLifemotherInfestedLaterSpawns";

    private static readonly (int Index, int Turn, int Floor, bool Exchange)[] FirstCombatStages =
        [(10, 1, 2, false), (11, 1, 2, true), (14, 1, 0, false), (15, 1, 0, true)];
    private static readonly (int Index, int Turn, int Floor, bool Exchange)[] LaterCombatStages =
        [(18, 2, 2, false), (19, 2, 1, false), (20, 2, 1, true), (21, 2, 0, false), (22, 2, 0, true),
         (25, 3, 2, false), (26, 3, 2, true), (27, 3, 1, false), (28, 3, 1, true), (29, 3, 0, false), (30, 3, 0, true)];

    internal static void ExportFirstCombat(string sourcePath, string outputPath, bool later = false)
    {
        using FixtureDocument source = ModelJson.ReadFixture(sourcePath);
        FixtureValue battle = source.RootElement;
        FixtureValue observation = battle.GetProperty("OriginalUnrulyBattle");
        Require(battle.GetProperty("TerminalEffectsSettled").GetBoolean() && battle.GetProperty("CaptureFailures").GetInt32() == 0 &&
            battle.GetProperty("Pending").GetInt32() == 0 && observation.GetProperty("ControlledTestScenario").GetBoolean() &&
            observation.GetProperty("RailforgedInstalled").GetBoolean(), "Expected the settled original Infested battle.");
        FixtureValue sourceStages = battle.GetProperty("Stages");
        var builder = new FixtureBuilder();
        var cases = later ? LaterCombatStages : FirstCombatStages;
        int root = builder.Add(FixtureKind.Object, children:
        [
            Key("Boundary"), Key(later ? LaterCombatBoundary : FirstCombatBoundary), Key("SourceBattleSha256"), Key(Hash(sourcePath).ToLowerInvariant()),
            Key("GameVersion"), builder.Import(battle.GetProperty("GameVersion")),
            Key("GameModuleMvid"), builder.Import(battle.GetProperty("GameModuleMvid")),
            Key("Scenario"), builder.Import(observation.GetProperty("Scenario")),
            Key("Stages"), builder.Add(FixtureKind.Array, children: cases.Select(expected => builder.Add(FixtureKind.Object,
                children: new[] { "Index", "Turn", "Kind", "Before", "Actual", "PendingPhysicalDeaths",
                    "ExternalTargets", "ExternalPyre" }.SelectMany(name => new[] { Key(name), builder.Import(sourceStages[expected.Index].GetProperty(name)) })
                    .ToArray())).ToArray())
        ]);
        using FixtureDocument component = builder.Build(root, 0, new byte[32]);
        FirstCombat(component.RootElement, later);
        using var output = new FileStream(outputPath, FileMode.CreateNew);
        component.Write(output);

        int Key(string value) => builder.Add(FixtureKind.String, value);
    }

    internal static void FirstCombat(FixtureValue component, bool later = false)
    {
        FixtureValue[] stages = component.GetProperty("Stages").EnumerateArray().ToArray();
        var cases = later ? LaterCombatStages : FirstCombatStages;
        Require(component.GetProperty("Boundary").GetString() == (later ? LaterCombatBoundary : FirstCombatBoundary) &&
            component.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            component.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            component.GetProperty("Scenario").GetString() == Scenario &&
            component.GetProperty("SourceBattleSha256").GetString() is { Length: 64 } hash && hash.All(Uri.IsHexDigit) &&
            stages.Select(stage => stage.GetProperty("Index").GetInt32()).SequenceEqual(cases.Select(expected => expected.Index)),
            "Expected the original physical combat stages with source provenance.");
        foreach (var (stage, expected) in stages.Zip(cases))
        {
            var (index, turn, floor, exchange) = expected;
            var before = stage.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var actual = stage.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            Require(stage.GetProperty("Turn").GetInt32() == turn && stage.GetProperty("Kind").GetString() == (exchange ? "Exchange" : "Room") &&
                !before.Preview && !actual.Preview && before.RoomIndex == floor && actual.RoomIndex == floor &&
                stage.GetProperty("ExternalTargets").GetArrayLength() == 0 && stage.GetProperty("ExternalPyre").ValueKind == FixtureKind.Null,
                "Changed original physical combat boundary at stage " + index);
            int[] physical = stage.GetProperty("PendingPhysicalDeaths").Deserialize<int[]>()!;
            string immutable = Serialize(before);
            RoomCombatResult replay = exchange ? RoomCombatModel.Exchange(before, pendingPhysicalDeathIds: physical) :
                RoomCombatModel.Resolve(before);
            Require(replay.Supported && Serialize(before) == immutable, "Physical combat unsupported at stage " + index + ": " + replay.UnsupportedReason);
            string? difference = ModelJson.Difference(Comparable(replay.State!), Comparable(actual));
            Require(difference == null, "Physical combat differs at stage " + index + ": " + difference);
        }
        Require(!OuterBossDefinitions.Supports(BossKey), "The scoped combat evidence may not admit the full boss battle.");
        Console.WriteLine($"NATIVE-LIFEMOTHER-INFESTED {(later ? "LATER" : "FIRST")}-COMBAT PASS: {stages.Length} physical room/exchange stages, complete actor state and RNG.");

        static string Comparable(RoomCombatState room) => Serialize(room.WithContext(BattleTurnChecks.ComparableContext(room.Context)));
    }

    internal static void ExportSpawns(string sourcePath, string outputPath)
    {
        using FixtureDocument source = ModelJson.ReadFixture(sourcePath);
        FixtureValue battle = source.RootElement;
        FixtureValue observation = battle.GetProperty("OriginalUnrulyBattle");
        Require(observation.GetProperty("Scenario").GetString() == Scenario &&
            observation.GetProperty("BossKey").GetString() == BossKey &&
            observation.GetProperty("RailforgedInstalled").GetBoolean() &&
            observation.GetProperty("ControlledTestScenario").GetBoolean() &&
            battle.GetProperty("CaptureFailures").GetInt32() == 0 && battle.GetProperty("Pending").GetInt32() == 0,
            "Expected completed original Infested wave observations.");
        using var input = File.OpenRead(sourcePath);
        var builder = new FixtureBuilder();
        int[] spawns = battle.GetProperty("Spawns").EnumerateArray().Skip(1).Select(builder.Import).ToArray();
        int root = builder.Add(FixtureKind.Object, children: [
            Key("Boundary"), Key(SpawnBoundary),
            Key("SourceBattleSha256"), Key(Convert.ToHexString(SHA256.HashData(input)).ToLowerInvariant()),
            Key("GameVersion"), builder.Import(battle.GetProperty("GameVersion")),
            Key("GameModuleMvid"), builder.Import(battle.GetProperty("GameModuleMvid")),
            Key("Spawns"), builder.Add(FixtureKind.Array, children: spawns)]);
        using FixtureDocument fixture = builder.Build(root, 0, new byte[32]);
        Spawns(fixture.RootElement);
        using var output = new FileStream(outputPath, FileMode.CreateNew);
        fixture.Write(output);

        int Key(string name) => builder.Add(FixtureKind.String, name);
    }

    internal static void Spawns(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Boundary").GetString() == SpawnBoundary &&
            fixture.GetProperty("SourceBattleSha256").GetString() is { Length: 64 } hash && hash.All(Uri.IsHexDigit) &&
            fixture.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            fixture.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module, "Expected original Infested wave provenance.");
        FixtureValue[] rows = fixture.GetProperty("Spawns").EnumerateArray().ToArray();
        Require(rows.Length == 4 &&
            rows.Select(row => row.GetProperty("Before").GetProperty("Phase").GetInt32()).SequenceEqual([1, 1, 2, 2]) &&
            rows.Select(row => row.GetProperty("IncludeTreasure").GetBoolean()).SequenceEqual([true, false, true, false]),
            "Expected the second and third installed waves at both entry points.");
        EnemySpawningChecks.Native(fixture);
    }

    internal static void ExportDraws(string sourcePath, string outputPath)
    {
        using FixtureDocument source = ModelJson.ReadFixture(sourcePath);
        FixtureValue battle = source.RootElement;
        Require(battle.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            battle.GetProperty("CaptureFailures").GetInt32() == 0 && battle.GetProperty("Pending").GetInt32() == 0,
            "Expected completed original Infested draw captures.");
        using var input = File.OpenRead(sourcePath);
        var builder = new FixtureBuilder();
        var rows = new List<int>();
        foreach (int turn in new[] { 2, 3 })
        {
            FixtureValue cycle = battle.GetProperty("CardCycles")[2 * turn - 1];
            Require(cycle.GetProperty("Kind").GetString() == "Draw" && cycle.GetProperty("Turn").GetInt32() == turn,
                "Expected original Infested draw " + turn);
            FixtureValue before = cycle.GetProperty("StandbyContext");
            FixtureValue after = battle.GetProperty("Turns")[turn - 1].GetProperty("Actual").GetProperty("Spawn")
                .GetProperty("Train").GetProperty("Rooms");
            rows.Add(builder.Add(FixtureKind.Object, children:
                [Key("Before"), builder.Import(before), Key("ActualRooms"), builder.Import(after)]));
        }
        int root = builder.Add(FixtureKind.Object, children:
            [Key("Boundary"), Key(DrawBoundary),
             Key("SourceBattleSha256"), Key(Convert.ToHexString(SHA256.HashData(input)).ToLowerInvariant()),
             Key("GameVersion"), builder.Import(battle.GetProperty("GameVersion")),
             Key("GameModuleMvid"), builder.Import(battle.GetProperty("GameModuleMvid")),
             Key("Observation"), builder.Import(battle.GetProperty("OriginalUnrulyBattle")),
             Key("Draws"), builder.Add(FixtureKind.Array, children: rows.ToArray())]);
        using FixtureDocument fixture = builder.Build(root, 0, new byte[32]);
        Draws(fixture.RootElement);
        using var output = new FileStream(outputPath, FileMode.CreateNew);
        fixture.Write(output);

        int Key(string name) => builder.Add(FixtureKind.String, name);
    }

    internal static void Draws(FixtureValue source)
    {
        FixtureValue observation = source.GetProperty("Observation");
        Require(source.GetProperty("Boundary").GetString() == DrawBoundary &&
            source.GetProperty("SourceBattleSha256").GetString() is { Length: 64 } hash && hash.All(Uri.IsHexDigit) &&
            source.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            source.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            observation.GetProperty("Scenario").GetString() == Scenario &&
            observation.GetProperty("BossKey").GetString() == BossKey, "Expected original Infested draws.");
        foreach (int turn in new[] { 2, 3 })
        {
            FixtureValue capture = source.GetProperty("Draws")[turn - 2];
            CombatContext context = capture.GetProperty("Before").Deserialize<CombatContext>()!;
            EnchantmentWorld world = context.Enchantments!;
            var before = new TrainCombatState(world.Rooms, world.Movement, world.EnemySlotsPerRoom, context);
            string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
            TrainCombatState? result = HivemindTurnUpgrade.ApplyInfestedDraw(before, out string? error);
            Require(result != null, "Original Infested draw " + turn + " unsupported: " + error);
            RoomCombatState[] actual = capture.GetProperty("ActualRooms").Deserialize<RoomCombatState[]>()!;
            int owners = 0;
            foreach (RoomCombatState room in result!.Rooms)
            {
                foreach (CombatUnit unit in room.Units.Where(unit => unit.Status("hivemind") != null))
                {
                    CombatUnit expected = actual[room.RoomIndex].Units.Single(actor => actor.Id == unit.Id);
                    string? difference = ModelJson.Difference(JsonSerializer.Serialize(unit, ModelJson.Options),
                        JsonSerializer.Serialize(expected, ModelJson.Options));
                    Require(difference == null, "Original Infested draw " + turn + " unit " + unit.Id + ": " + difference);
                    Require(room.ExternalInteractions.Contains(HivemindTurnUpgrade.Marker(unit)) &&
                        actual[room.RoomIndex].ExternalInteractions.Contains(HivemindTurnUpgrade.Marker(unit)),
                        "Original Infested draw rested without the expected room marker.");
                    owners++;
                }
            }
            Require(owners == (turn == 2 ? 7 : 9) && context.BattleRng.Equals(result.Context!.BattleRng) &&
                world.TestRng.Equals(result.Context.Enchantments!.TestRng) &&
                context.Cards.Rng.Equals(result.Context.Cards.Rng) &&
                immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Original Infested draw changed RNG or its parent.");
            Console.WriteLine($"NATIVE-INFESTED DRAW {turn} PASS: {owners} exact actors and room markers.");
        }
    }

    internal static void FirstTurn(string battlePath, string birthPath)
    {
        using var battleDocument = ModelJson.ReadFixture(battlePath);
        using var birthDocument = ModelJson.ReadFixture(birthPath);
        FixtureValue battle = battleDocument.RootElement;
        VerifyFirstTurn(battle, battle.GetProperty("OriginalUnrulyBattle"), battle.GetProperty("Turns")[0],
            birthDocument.RootElement, allowHistoricalControllerOmission:
            Hash(battlePath) == "041DB0E09B2F23E140E8E824F10F3EB5DFE4BD5F4A5E7016EF58B7F56B560D97" &&
            Hash(birthPath) == "A36EEA0FA27C91D4667F6A2076CE083487CE5BCA6AAEDFA63C804C5742FA9BFD");
    }

    internal static void FirstTurnComponent(FixtureValue component)
    {
        Require(component.GetProperty("Boundary").GetString() == FirstTurnBoundary &&
            HasHash("SourceBattleSha256") && HasHash("SourceBirthSha256"),
            "Expected original Infested first-turn source provenance.");
        VerifyFirstTurn(component, component.GetProperty("Observation"), component.GetProperty("Turn"),
            component.GetProperty("Birth"));

        bool HasHash(string key) => component.GetProperty(key).GetString() is { Length: 64 } hash && hash.All(Uri.IsHexDigit);
    }

    internal static void ExportFirstTurn(string battlePath, string birthPath, string outputPath)
    {
        using var battleDocument = ModelJson.ReadFixture(battlePath);
        using var birthDocument = ModelJson.ReadFixture(birthPath);
        FixtureValue battle = battleDocument.RootElement;
        FixtureValue observation = battle.GetProperty("OriginalUnrulyBattle");
        Require(battle.GetProperty("TerminalEffectsSettled").GetBoolean() && battle.GetProperty("CaptureFailures").GetInt32() == 0 &&
            battle.GetProperty("Pending").GetInt32() == 0, "Expected a settled native source.");
        var builder = new FixtureBuilder();
        int root = builder.Add(FixtureKind.Object, children:
        [
            Key("Boundary"), Key(FirstTurnBoundary),
            Key("SourceBattleSha256"), Key(Hash(battlePath).ToLowerInvariant()),
            Key("SourceBirthSha256"), Key(Hash(birthPath).ToLowerInvariant()),
            Key("GameVersion"), builder.Import(battle.GetProperty("GameVersion")),
            Key("GameModuleMvid"), builder.Import(battle.GetProperty("GameModuleMvid")),
            Key("Observation"), builder.Add(FixtureKind.Object, children: new[]
            {
                "Scenario", "BossKey", "ControlledTestScenario", "RailforgedInstalled"
            }.SelectMany(name => new[] { Key(name), builder.Import(observation.GetProperty(name)) }).ToArray()),
            Key("Turn"), builder.Import(battle.GetProperty("Turns")[0]),
            Key("Birth"), builder.Import(birthDocument.RootElement)
        ]);
        using FixtureDocument component = builder.Build(root, 0, new byte[32]);
        FirstTurnComponent(component.RootElement);
        using var output = new FileStream(outputPath, FileMode.CreateNew);
        component.Write(output);

        int Key(string value) => builder.Add(FixtureKind.String, value);
    }

    private static void VerifyFirstTurn(FixtureValue battle, FixtureValue observation, FixtureValue observedTurn, FixtureValue birth,
        bool allowHistoricalControllerOmission = false)
    {
        Require(battle.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            battle.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            observation.GetProperty("Scenario").GetString() == Scenario && observation.GetProperty("BossKey").GetString() == BossKey &&
            observation.GetProperty("ControlledTestScenario").GetBoolean() && observation.GetProperty("RailforgedInstalled").GetBoolean(),
            "Expected the installed original Infested encounter.");
        OuterBossBirthChecks.Native(birth, Game231FixtureCatalog.Module);
        var before = observedTurn.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = observedTurn.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var definition = birth.GetProperty("Births")[0].GetProperty("Instantiation").GetProperty("Definition")
            .Deserialize<OuterBossSpawnDefinition>()!;
        Require(observedTurn.GetProperty("Index").GetInt32() == 0 && before.Spawn.Turn == 0 && actual.Spawn.Turn == 1 &&
            before.BattlePreviewEnabled && actual.BattlePreviewEnabled && definition.Actor.Unit.AssetKey == BossKey,
            "Expected the first original Infested EndTurn with battle preview enabled.");

        EnemySpawnState spawn = before.Spawn;
        bool missingSettledController = actual.Spawn.OuterBoss == null;
        Require(!missingSettledController || allowHistoricalControllerOmission,
            "Missing settled controller is allowed only for the pinned historical capture.");
        BattleTurnState input = before;
        if (spawn.QueuedOuterBoss == null)
        {
            // The native queue predates the capture's supported scheduler. Its exact
            // definition is independently present before the first native birth.
            Require(spawn.ExternalInteractions.SequenceEqual(["Outer or queued final boss spawning"]),
                "Expected the original queue marker for the independent birth receipt.");
            var queued = new EnemySpawnState(spawn.Train, spawn.Waves, spawn.SelectedGroups, spawn.Phase, spawn.Looping, spawn.Rng,
                spawn.NextUnitId, spawn.Treasures, spawn.TreasuresRemaining, spawn.TreasureEnabled, spawn.FirstTreasureTurn,
                spawn.FirstTreasureRoom, spawn.Turn, [], spawn.CanonicalDecisionReferences, spawn.PendingDestroyedUnitIds,
                queuedOuterBoss: definition);
            input = new BattleTurnState(queued, before.Energy, before.EnergyPerTurn, before.DrawPerTurn, before.ForgePoints,
                before.DragonsHoard, before.MoonPhase, before.RngStreams, before.OtherPiles, before.ExternalInteractions,
                before.PlayRules, before.BattlePreviewEnabled, before.UiRngIsolated, before.CanonicalDecisionReferences,
                before.CanonicalPhysicalReferences, before.SelectedRoom, before.MoonPhaseTrackingActive);
        }
        else
            Require(Serialize(spawn.QueuedOuterBoss) == Serialize(definition),
                "The queued original boss definition differs from its independent birth receipt.");

        string immutable = Serialize(input);
        BattleTurnResult result = BattleTurnModel.EndTurn(input, RecordedBattleChecks.Decisions(observedTurn));
        Require(result.Supported && result.Outcome == (RoomOutcome)observedTurn.GetProperty("ActualOutcome").GetInt32() &&
            result.State!.Spawn.OuterBoss?.AssetKey == BossKey && Serialize(input) == immutable &&
            !OuterBossDefinitions.Supports(BossKey), "Original Infested first turn failed: " + result.UnsupportedReason);
        string expected = BattleTurnChecks.Comparable(actual);
        string predicted = BattleTurnChecks.Comparable(result.State!);
        if (missingSettledController)
        {
            JsonObject projected = JsonNode.Parse(predicted)!.AsObject();
            Require(projected.Remove("OuterBoss"), "Expected a modeled controller outside the old observation.");
            predicted = projected.ToJsonString();
        }
        string? difference = ModelJson.Difference(predicted, expected);
        Require(difference == null, "Original Infested first settled state or RNG differs: " + difference);
        Console.WriteLine("NATIVE-LIFEMOTHER-INFESTED FIRST-TURN PASS: full captured state, RNG and preview; " +
            (missingSettledController ? "settled controller absent from pinned historical capture." : "exact settled controller."));
    }

    private static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    internal static void Opening(FixtureValue component)
    {
        FixtureValue observation = component.GetProperty("Observation");
        FixtureValue birth = component.GetProperty("Birth");
        Require(component.GetProperty("Boundary").GetString() == OpeningBoundary &&
            component.GetProperty("SourceBattleSha256").GetString() is { Length: 64 } battleHash && battleHash.All(Uri.IsHexDigit) &&
            component.GetProperty("SourceBirthSha256").GetString() is { Length: 64 } birthHash && birthHash.All(Uri.IsHexDigit) &&
            component.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            component.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            observation.GetProperty("Scenario").GetString() == Scenario && observation.GetProperty("BossKey").GetString() == BossKey &&
            observation.GetProperty("ControlledTestScenario").GetBoolean() && observation.GetProperty("RailforgedInstalled").GetBoolean(),
            "Expected the installed original Infested opening.");
        Require(birth.GetProperty("Births")[0].GetProperty("Instantiation").GetProperty("Definition").GetProperty("Actor")
            .GetProperty("Unit").GetProperty("AssetKey").GetString() == BossKey, "Expected the original Infested birth receipt.");
        OuterBossBirthChecks.Native(birth, Game231FixtureCatalog.Module);

        CombatContext context = component.GetProperty("FirstDrawContext").Deserialize<CombatContext>()!;
        Require(context.QueryFrame?.Turn == 1, "Expected the native first draw.");
        EnchantmentWorld world = context.Enchantments ?? throw new InvalidDataException("Missing native first draw world.");
        var input = new TrainCombatState(world.Rooms, world.Movement, world.EnemySlotsPerRoom, context);
        CombatUnit boss = input.Rooms.SelectMany(room => room.Units).Single(unit => unit.AssetKey == BossKey);
        CombatTrigger death = boss.Triggers[0];
        CombatTrigger altered = death.WithEffects([death.Effects[0].WithActionValue(1)]);
        CombatUnit alteredBoss = boss.WithTriggers([altered, boss.Triggers[1]]);
        Require(OuterBossDefinitions.HasDormantLifemotherInfestedDeathGrant(boss, death, context) &&
            !OuterBossDefinitions.HasDormantLifemotherInfestedDeathGrant(alteredBoss, altered, context) &&
            !OuterBossDefinitions.HasDormantLifemotherInfestedDeathGrant(boss, death,
                context.WithQueryFrame(context.QueryFrame!.With(turn: 2))) &&
            TriggeredStatusModel.Validate(death.Effects[0]) != null,
            "Expected exact dormant death payload admission only during the original opening.");
        CombatUnit[] owners = input.Rooms.SelectMany(room => room.Units).Where(unit => unit.Status("hivemind") != null).ToArray();
        Require(owners.Length == 3 && HivemindTurnUpgrade.HasOriginalFirstWave(owners) &&
            !HivemindTurnUpgrade.HasOriginalFirstWave(owners.Take(2)), "Expected the exact native three-owner group.");
        string immutable = JsonSerializer.Serialize(input, ModelJson.Options);
        TrainCombatState? replayed = HivemindTurnUpgrade.ApplyFirstOrdinaryTurn(input, out string? error);
        Require(replayed != null && context.BattleRng.Equals(replayed.Context!.BattleRng), "Opening +3 projection failed: " + error);
        var nativeOwners = component.GetProperty("SettledOwners").Deserialize<CombatUnit[]>()!;
        Require(nativeOwners.Length == owners.Length, "Expected the same three native settled wave actors.");
        foreach (CombatUnit owner in owners)
        {
            CombatUnit replay = replayed!.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == owner.Id);
            CombatUnit expected = nativeOwners.Single(unit => unit.Id == owner.Id);
            Require(replay.Attack == owner.Attack + 3 &&
                JsonSerializer.Serialize(replay, ModelJson.Options) == JsonSerializer.Serialize(expected, ModelJson.Options),
                "Native first-wave Hivemind owner differs at unit " + owner.Id);
        }
        Require(immutable == JsonSerializer.Serialize(input, ModelJson.Options) && !OuterBossDefinitions.Supports(BossKey),
            "Infested opening changed its input or bypassed the whole-battle guard.");
        Console.WriteLine("NATIVE-LIFEMOTHER-INFESTED OPENING PASS: exact original birth and three native +3 Hivemind owners.");
    }

    internal static void Export(string inputPath, string outputPath)
    {
        using var battleDocument = ModelJson.ReadFixture(inputPath);
        FixtureValue battle = battleDocument.RootElement;
        Require(battle.GetProperty("TerminalEffectsSettled").GetBoolean() &&
            battle.GetProperty("CaptureFailures").GetInt32() == 0 && battle.GetProperty("Pending").GetInt32() == 0,
            "Expected a completed original 2.3.1 native capture.");
        using var stream = File.OpenRead(inputPath);
        string hash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        var builder = new FixtureBuilder();
        int root = builder.Add(FixtureKind.Object, children: [
            Key("Boundary"), Key(Boundary), Key("SourceBattleSha256"), Key(hash),
            Key("GameVersion"), builder.Import(battle.GetProperty("GameVersion")),
            Key("GameModuleMvid"), builder.Import(battle.GetProperty("GameModuleMvid")),
            Key("Observation"), builder.Import(battle.GetProperty("OriginalUnrulyBattle")),
            Key("TriggeredStatuses"), builder.Import(battle.GetProperty("TriggeredStatuses"))
        ]);
        using FixtureDocument component = builder.Build(root, 0, new byte[32]);
        Native(component.RootElement);
        using var output = new FileStream(outputPath, FileMode.CreateNew);
        component.Write(output);
        int Key(string value) => builder.Add(FixtureKind.String, value);
    }

    internal static void Native(FixtureValue component)
    {
        FixtureValue observation = component.GetProperty("Observation");
        string? hash = component.GetProperty("SourceBattleSha256").GetString();
        Require(component.GetProperty("Boundary").GetString() == Boundary && hash?.Length == 64 && hash.All(Uri.IsHexDigit) &&
            component.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            component.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            observation.GetProperty("Scenario").GetString() == Scenario && observation.GetProperty("BossKey").GetString() == BossKey &&
            observation.GetProperty("ControlledTestScenario").GetBoolean() && observation.GetProperty("RailforgedInstalled").GetBoolean(),
            "Expected a pinned original Lifemother Infested component.");

        var rows = component.GetProperty("TriggeredStatuses").EnumerateArray().ToArray();
        Require(rows.Length == 3, "Expected three original Hivemind additions.");
        int actorId = rows[0].GetProperty("ActorId").GetInt32();
        for (int index = 0; index < rows.Length; index++)
        {
            var row = rows[index];
            var effect = row.GetProperty("Effect").Deserialize<CombatEffect>()!;
            var before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var actual = row.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            var actor = before.Units.Single(unit => unit.Id == actorId);
            var observed = row.GetProperty("ActualUnits").Deserialize<CombatUnit[]>()!.Single(unit => unit.Id == actorId);
            Require(row.GetProperty("Sequence").GetInt32() == index + 1 && row.GetProperty("ActorId").GetInt32() == actorId &&
                row.GetProperty("Completed").GetBoolean() && row.GetProperty("Kind").GetString() == "OnAnyUnitDeathOnFloor" &&
                row.GetProperty("SourceCardId").GetInt32() == 0 && row.GetProperty("Targets").Deserialize<int[]>()!.SequenceEqual([actorId]) &&
                actor is { AssetKey: BossKey, Team: CombatTeam.Enemy } &&
                (actor.Status("hivemind")?.Stacks ?? 0) == index * 2 && observed.Status("hivemind")?.Stacks == (index + 1) * 2 &&
                actor.Attack == observed.Attack && effect is { Type: "CardEffectAddStatusEffect", Action: {
                    Type: "AddStatus", Target: "Self", AllowEnemy: false, AllowPlayer: true, Value: 0, Range: null,
                    Statuses: [{ Id: "hivemind", Stacks: 2 }] } }, "Changed original Hivemind addition at step " + (index + 1));

            string immutable = Serialize(before);
            RoomCombatResult applied = StatusApplicationModel.ApplyRetained(before, actorId, effect.Action!.Statuses[0], 0);
            Require(applied.Supported, "Could not apply captured Hivemind: " + applied.UnsupportedReason);
            var predicted = applied.State!.Units.Single(unit => unit.Id == actorId);
            string? difference = ModelJson.Difference(Serialize(predicted), Serialize(observed));
            Require(difference == null && Serialize(before) == immutable &&
                applied.State.Context!.BattleRng.Equals(actual.Context!.BattleRng) &&
                applied.State.Context.Enchantments!.TestRng.Equals(actual.Context.Enchantments!.TestRng) &&
                applied.State.Context.Cards.Rng.Equals(actual.Context.Cards.Rng),
                "Hivemind actor or RNG differs: " + difference);
        }
        Console.WriteLine("NATIVE-LIFEMOTHER-INFESTED PASS: 3 original status additions, full actor state and all three RNG streams.");
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);

    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidDataException(message);
    }
}

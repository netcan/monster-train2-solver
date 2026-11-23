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

    internal static void FirstTurn(string battlePath, string birthPath)
    {
        using var battleDocument = ModelJson.ReadFixture(battlePath);
        using var birthDocument = ModelJson.ReadFixture(birthPath);
        FixtureValue battle = battleDocument.RootElement;
        FixtureValue birth = birthDocument.RootElement;
        FixtureValue observation = battle.GetProperty("OriginalUnrulyBattle");
        Require(battle.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            battle.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            observation.GetProperty("Scenario").GetString() == Scenario && observation.GetProperty("BossKey").GetString() == BossKey &&
            observation.GetProperty("ControlledTestScenario").GetBoolean() && observation.GetProperty("RailforgedInstalled").GetBoolean(),
            "Expected the installed original Infested encounter.");
        OuterBossBirthChecks.Native(birth, Game231FixtureCatalog.Module);
        var observedTurn = battle.GetProperty("Turns")[0];
        var before = observedTurn.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = observedTurn.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var definition = birth.GetProperty("Births")[0].GetProperty("Instantiation").GetProperty("Definition")
            .Deserialize<OuterBossSpawnDefinition>()!;
        Require(observedTurn.GetProperty("Index").GetInt32() == 0 && before.Spawn.Turn == 0 && actual.Spawn.Turn == 1 &&
            before.BattlePreviewEnabled && actual.BattlePreviewEnabled && definition.Actor.Unit.AssetKey == BossKey,
            "Expected the first original Infested EndTurn with battle preview enabled.");

        EnemySpawnState spawn = before.Spawn;
        bool legacy = spawn.QueuedOuterBoss == null;
        BattleTurnState input = before;
        if (legacy)
        {
            Require(Hash(battlePath) == "041DB0E09B2F23E140E8E824F10F3EB5DFE4BD5F4A5E7016EF58B7F56B560D97" &&
                Hash(birthPath) == "A36EEA0FA27C91D4667F6A2076CE083487CE5BCA6AAEDFA63C804C5742FA9BFD" &&
                spawn.ExternalInteractions.SequenceEqual(["Outer or queued final boss spawning"]) && actual.Spawn.OuterBoss == null,
                "Missing native controller is allowed only for the pinned historical capture.");
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
            Require(spawn.QueuedOuterBoss!.Actor.Unit.AssetKey == BossKey && actual.Spawn.OuterBoss?.AssetKey == BossKey,
                "Expected both queued and settled original controller snapshots.");

        string immutable = Serialize(input);
        BattleTurnResult result = BattleTurnModel.EndTurn(input, RecordedBattleChecks.Decisions(observedTurn));
        Require(result.Supported && result.Outcome == (RoomOutcome)observedTurn.GetProperty("ActualOutcome").GetInt32() &&
            result.State!.Spawn.OuterBoss?.AssetKey == BossKey && Serialize(input) == immutable &&
            !OuterBossDefinitions.Supports(BossKey), "Original Infested first turn failed: " + result.UnsupportedReason);
        string expected = BattleTurnChecks.Comparable(actual);
        string predicted = BattleTurnChecks.Comparable(result.State!);
        if (legacy)
        {
            JsonObject projected = JsonNode.Parse(predicted)!.AsObject();
            Require(projected.Remove("OuterBoss"), "Expected a modeled controller outside the old observation.");
            predicted = projected.ToJsonString();
        }
        string? difference = ModelJson.Difference(predicted, expected);
        Require(difference == null, "Original Infested first settled state or RNG differs: " + difference);
        Console.WriteLine("NATIVE-LIFEMOTHER-INFESTED FIRST-TURN PASS: full captured state, RNG and preview; " +
            (legacy ? "settled controller absent from pinned historical capture." : "exact settled controller."));

        static string Hash(string path)
        {
            using var stream = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(stream));
        }
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

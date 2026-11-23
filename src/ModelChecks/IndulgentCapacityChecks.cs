using System.Security.Cryptography;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class IndulgentCapacityChecks
{
    private const string Scenario = "SoulSavior_R2_BossBattle_FavoredChild_2";
    private const string Boundary = "NativeIndulgentCapacityAndOnFeed";

    internal static void Export(string battlePath, string outputPath)
    {
        using var input = File.OpenRead(battlePath);
        string sha = Convert.ToHexString(SHA256.HashData(input)).ToLowerInvariant();
        input.Position = 0;
        using var document = FixtureDocument.Read(input);
        FixtureValue battle = document.RootElement;
        Require(battle.GetProperty("TerminalEffectsSettled").GetBoolean() && battle.GetProperty("CaptureFailures").GetInt32() == 0 &&
            battle.GetProperty("Pending").GetInt32() == 0, "Expected a completed original native capture.");
        FixtureValue capacities = battle.GetProperty("CapacityEffects");
        var stages = battle.GetProperty("Stages").EnumerateArray().Where(stage =>
            stage.GetProperty("Kind").GetString() == "Room" && capacities.EnumerateArray().Any(row =>
                stage.GetProperty("Turn").ContentEquals(row.GetProperty("Before").GetProperty("Context").GetProperty("QueryFrame").GetProperty("Turn")) &&
                stage.GetProperty("Before").GetProperty("RoomIndex").ContentEquals(row.GetProperty("Before").GetProperty("RoomIndex")))).ToArray();
        var builder = new FixtureBuilder();
        int root = builder.Add(FixtureKind.Object, children: [
            Key("Schema"), builder.Add(FixtureKind.Number, "1"), Key("Boundary"), Key(Boundary),
            Key("SourceBattleSha256"), Key(sha), Key("SourceUnsupported"), builder.Import(battle.GetProperty("Unsupported")),
            Key("GameVersion"), builder.Import(battle.GetProperty("GameVersion")),
            Key("GameModuleMvid"), builder.Import(battle.GetProperty("GameModuleMvid")),
            Key("Observation"), builder.Import(battle.GetProperty("OriginalUnrulyBattle")),
            Key("ModifierScenario"), builder.Import(battle.GetProperty("ModifierScenario")),
            Key("CapacityEffects"), builder.Import(capacities),
            Key("CapacityTests"), builder.Import(battle.GetProperty("CapacityTests")),
            Key("Stages"), builder.Add(FixtureKind.Array, children: stages.Select(builder.Import).ToArray())
        ]);
        using FixtureDocument component = builder.Build(root, 0, new byte[32]);
        Native(component.RootElement);
        using var output = new FileStream(outputPath, FileMode.CreateNew);
        component.Write(output);
        int Key(string value) => builder.Add(FixtureKind.String, value);
    }

    internal static void Native(FixtureValue component)
    {
        string? sha = component.GetProperty("SourceBattleSha256").GetString();
        FixtureValue observation = component.GetProperty("Observation");
        Require(component.GetProperty("Schema").GetInt32() == 1 && component.GetProperty("Boundary").GetString() == Boundary &&
            sha?.Length == 64 && sha.All(Uri.IsHexDigit) && component.GetProperty("SourceUnsupported").GetInt32() >= 0 &&
            component.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            component.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            observation.GetProperty("Scenario").GetString() == Scenario &&
            observation.GetProperty("BossKey").GetString() == OuterBossDefinitions.IndulgentReduceCapacity &&
            observation.GetProperty("ControlledTestScenario").GetBoolean() && observation.GetProperty("RailforgedInstalled").GetBoolean(),
            "Expected original 2.3.1 Indulgent capacity observations with binary source provenance.");
        FixtureValue[] rows = component.GetProperty("CapacityEffects").EnumerateArray().ToArray();
        Require(rows.Length >= 3, "Expected three distinct original capacity reductions.");
        RoomCapacityChecks.Native(component);

        foreach (FixtureValue row in rows)
        {
            var before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var after = row.GetProperty("After").Deserialize<RoomCombatState>()!;
            CombatUnit actor = Boss(before);
            CombatTrigger postCombat = actor.Triggers.Single(trigger => trigger.Kind == "PostCombat");
            int turn = before.Context?.QueryFrame?.Turn ?? throw new InvalidDataException("Missing native capacity turn.");
            FixtureValue stage = component.GetProperty("Stages").EnumerateArray().Single(record =>
                record.GetProperty("Turn").GetInt32() == turn && record.GetProperty("Before").GetProperty("RoomIndex").GetInt32() == before.RoomIndex);
            CombatUnit settled = Boss(stage.GetProperty("Actual").Deserialize<RoomCombatState>()!);
            Require(OuterBossDefinitions.IsIndulgentCapacityFeed(actor, postCombat) &&
                row.GetProperty("SourceCardId").GetInt32() == 0 &&
                row.GetProperty("Effect").Deserialize<CardActionEffect>() is { Type: "AdjustCapacity", Target: "Room", Value: -1,
                    Range: null, AllowPlayer: true, AllowEnemy: true } &&
                Boss(after).Id == actor.Id && settled.Id == actor.Id && Rage(Boss(after)) == Rage(actor) && Rage(settled) == Rage(actor) + 3,
                "Capacity and ordered original self OnFeed Rage boundaries changed.");
        }

        ReplayFirstPair(rows[0]);
        Console.WriteLine($"NATIVE-INDULGENT-CAPACITY PASS: {rows.Length} exact reductions and following self OnFeed +3 Rage; " +
            "first native PostCombat pair replays through the existing trigger queue.");
    }

    private static void ReplayFirstPair(FixtureValue row)
    {
        var before = row.GetProperty("Before").Deserialize<RoomCombatState>()!;
        var afterCapacity = row.GetProperty("After").Deserialize<RoomCombatState>()!;
        CombatUnit actor = Boss(before);
        Require(before.ExternalInteractions.SequenceEqual(new[] { "Outer train boss" }),
            "Expected the original room with only its separately guarded outer boss.");
        // The phase witness replays just this actor's existing callbacks, from the
        // pre-effect capture. The outer battle itself still requires admission.
        var scope = new RoomCombatState(before.RoomIndex, before.Deployment, before.Units, [], before.Context, before.Preview, before.ActiveCompanionBossIds);
        string immutable = JsonSerializer.Serialize(scope, ModelJson.Options);
        var queue = new List<RoomCombatModel.QueuedCharacterTrigger>();
        RoomCombatResult capacity = RoomCombatModel.ApplyCharacterPhase(scope, actor.Id, "PostCombat", queue.Add);
        Require(capacity.Supported, "Original PostCombat pair failed validation: " + capacity.UnsupportedReason);
        Require(queue.Count == 1 && queue[0].Kind == "OnFeed" && queue[0].Unit.Id == actor.Id &&
            Rage(Boss(capacity.State!)) == Rage(actor) && Same(capacity.State!.Context!.RoomCapacities, afterCapacity.Context!.RoomCapacities),
            "Original room capacity must settle before queuing exactly one self OnFeed.");
        RoomCombatResult fed = RoomCombatModel.ApplyQueuedCharacterTrigger(capacity.State!, queue[0], queue.Add);
        Require(fed.Supported && Rage(Boss(fed.State!)) == Rage(actor) + 3 &&
            fed.State!.Context!.BattleRng.Equals(afterCapacity.Context!.BattleRng) &&
            Same(fed.State.Context.RoomCapacities, afterCapacity.Context.RoomCapacities) &&
            JsonSerializer.Serialize(scope, ModelJson.Options) == immutable && !OuterBossDefinitions.Supports(actor.AssetKey),
            "Original self OnFeed must add exactly three Rage without extra capacity changes or advancing RNG.");

        CombatTrigger postCombat = actor.Triggers.Single(trigger => trigger.Kind == "PostCombat");
        CombatUnit altered = actor.WithTriggers(actor.Triggers.Select(trigger => trigger == postCombat
            ? trigger.WithEffects([postCombat.Effects[0].WithActionValue(-2), postCombat.Effects[1]]) : trigger).ToArray());
        Require(!RoomCombatModel.ApplyCharacterPhase(scope.WithUnits(scope.Units.Select(unit => unit.Id == actor.Id ? altered : unit).ToArray()),
            actor.Id, "PostCombat", _ => { }).Supported, "An altered capacity payload admitted the original replay exception.");
    }

    private static bool Same<T>(T left, T right) => JsonSerializer.Serialize(left, ModelJson.Options) == JsonSerializer.Serialize(right, ModelJson.Options);
    private static CombatUnit Boss(RoomCombatState room) => room.Units.Single(unit => unit.AssetKey == OuterBossDefinitions.IndulgentReduceCapacity);
    private static int Rage(CombatUnit unit) => unit.Status("buff")?.Stacks ?? 0;

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }
}

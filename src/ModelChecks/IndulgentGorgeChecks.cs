using System.Security.Cryptography;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class IndulgentGorgeChecks
{
    private const string Scenario = "SoulSavior_R2_BossBattle_FavoredChild_1";
    private const string Boundary = "NativeIndulgentGorgeOnSlay";

    internal static void Export(string battlePath, string outputPath)
    {
        using var input = File.OpenRead(battlePath);
        string sha = Convert.ToHexString(SHA256.HashData(input)).ToLowerInvariant();
        input.Position = 0;
        using var document = FixtureDocument.Read(input);
        FixtureValue battle = document.RootElement;
        Require(battle.GetProperty("TerminalEffectsSettled").GetBoolean() && battle.GetProperty("CaptureFailures").GetInt32() == 0 &&
            battle.GetProperty("Pending").GetInt32() == 0, "Expected the settled original GorgeOnSlay capture.");
        var builder = new FixtureBuilder();
        int root = builder.Add(FixtureKind.Object, children: [
            Key("Schema"), builder.Add(FixtureKind.Number, "1"), Key("Boundary"), Key(Boundary),
            Key("SourceBattleSha256"), Key(sha), Key("SourceUnsupported"), builder.Import(battle.GetProperty("Unsupported")),
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
        string? sha = component.GetProperty("SourceBattleSha256").GetString();
        Require(component.GetProperty("Schema").GetInt32() == 1 && component.GetProperty("Boundary").GetString() == Boundary &&
            sha?.Length == 64 && sha.All(Uri.IsHexDigit) && component.GetProperty("SourceUnsupported").GetInt32() >= 0 &&
            component.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            component.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            observation.GetProperty("Scenario").GetString() == Scenario &&
            observation.GetProperty("BossKey").GetString() == OuterBossDefinitions.IndulgentGorgeOnSlay &&
            observation.GetProperty("ControlledTestScenario").GetBoolean() && observation.GetProperty("RailforgedInstalled").GetBoolean(),
            "Expected original 2.3.1 GorgeOnSlay observations with binary source provenance.");

        var used = new HashSet<int>();
        foreach (FixtureValue replay in observation.GetProperty("GorgeOnSlayReplays").EnumerateArray())
        {
            int sequence = replay.GetProperty("GrantSequence").GetInt32();
            Require(replay.GetProperty("Completed").GetBoolean() && replay.GetProperty("Trigger").GetString() == "OnKill" &&
                replay.GetProperty("Replay").GetString() == "OnFeed" && replay.GetProperty("QueuedOnFeed").GetInt32() == 1 &&
                sequence > 0 && used.Add(sequence), "Missing unique original OnKill enqueue and linked OnFeed grant.");
            FixtureValue grant = component.GetProperty("TriggeredStatuses").EnumerateArray().Single(row =>
                row.GetProperty("Sequence").GetInt32() == sequence);
            Replay(grant, replay.GetProperty("Turn").GetInt32(), replay.GetProperty("BossId").GetInt32());
        }
        Require(used.Count > 0, "No original GorgeOnSlay replay with a positive Rage grant was recorded.");
        Console.WriteLine($"NATIVE-INDULGENT-GORGE PASS: {used.Count} OnKill enqueue and self OnFeed grants, exact actor, context and RNG.");
    }

    private static void Replay(FixtureValue grant, int turn, int bossId)
    {
        var before = grant.GetProperty("Before").Deserialize<RoomCombatState>()!;
        var actual = grant.GetProperty("Actual").Deserialize<RoomCombatState>()!;
        CombatUnit actor = before.Units.Single(unit => unit.Id == bossId);
        CombatUnit after = grant.GetProperty("ActualUnits").Deserialize<CombatUnit[]>()!.Single();
        CombatTrigger kill = actor.Triggers.Single(trigger => trigger.Kind == "OnKill");
        CombatEffect feed = grant.GetProperty("Effect").Deserialize<CombatEffect>()!;
        Require(before.Context?.QueryFrame?.Turn == turn && grant.GetProperty("Completed").GetBoolean() &&
            grant.GetProperty("ActorId").GetInt32() == bossId && grant.GetProperty("Kind").GetString() == "OnFeed" &&
            grant.GetProperty("SourceCardId").GetInt32() == 0 && grant.GetProperty("Interactions").GetArrayLength() == 0 &&
            grant.GetProperty("Targets").Deserialize<int[]>()!.SequenceEqual(new[] { bossId }) && after.Id == bossId &&
            after.Status("buff")?.Stacks == (actor.Status("buff")?.Stacks ?? 0) + 3 &&
            feed is { Type: "CardEffectAddStatusEffect", Action: { Target: "Self", Statuses: [{ Id: "buff", Stacks: 3 }] } } &&
            OuterBossDefinitions.IsIndulgentSlayFeed(actor, kill) && !OuterBossDefinitions.Supports(actor.AssetKey),
            "Original boss, native self OnFeed payload or Rage delta changed.");

        // The standalone capture also carries the outer-boss admission label.
        // Preserve the native world's own room flags when replaying this one effect.
        var nativeFlags = before.Context!.Enchantments!.Rooms.Single(room => room.RoomIndex == before.RoomIndex).ExternalInteractions;
        Require(before.ExternalInteractions.Where(value => value != "Outer train boss").SequenceEqual(nativeFlags),
            "Standalone and world room flags differ beyond the outer-boss admission label.");
        var scope = new RoomCombatState(before.RoomIndex, before.Deployment, before.Units, nativeFlags, before.Context,
            before.Preview, before.ActiveCompanionBossIds);
        string immutable = Serialize(scope);
        var queue = new List<RoomCombatModel.QueuedCharacterTrigger>();
        RoomCombatResult replayed = RoomCombatModel.ApplyQueuedCharacterTrigger(scope, new(scope.RoomIndex, actor, "OnKill"), queue.Add);
        Require(replayed.Supported && queue is [var child] && child.Kind == "OnFeed" && child.Unit.Id == bossId &&
            child.RoomIndex == scope.RoomIndex && child.DyingCharacter == null && child.OverrideTarget == null,
            "Existing dispatcher did not enqueue exactly one original self OnFeed: " + replayed.UnsupportedReason);
        RoomCombatResult fed = RoomCombatModel.ApplyQueuedCharacterTrigger(replayed.State!, queue[0], queue.Add);
        Require(fed.Supported, "Original self OnFeed could not run: " + fed.UnsupportedReason);
        string? difference = ModelJson.Difference(Serialize(fed.State!.Context), Serialize(actual.Context));
        Require(difference == null, "Original self OnFeed context and RNG differ at " + difference);
        Require(Serialize(fed.State.Units.Single(unit => unit.Id == bossId)) == Serialize(after) && Serialize(scope) == immutable,
            "Original self OnFeed actor or immutable input differs.");

        CombatTrigger changed = kill.WithEffects([kill.Effects[0].WithActionValue(-1)]);
        CombatUnit altered = actor.WithTriggers(actor.Triggers.Select(trigger => trigger == kill ? changed : trigger).ToArray());
        Require(!OuterBossDefinitions.IsIndulgentSlayFeed(altered, changed), "An altered replay payload passed the original boss guard.");
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, ModelJson.Options);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }
}

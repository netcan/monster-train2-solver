using System.Security.Cryptography;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RailforgedKillBumpChecks
{
    private const string Boundary = "OriginalRailforgedKillBumpExchanges";
    private const string Scenario = "SoulSavior_R5_BossBattle_Lifemother_Reanimate";
    private const string Boss = "Boss_SoulSavior_Final_Lifemother_HealReanimate";
    private const string SourceHash = "5482755f420e8942f693172f3f1b5a70aff87cab62f17bb18a4a83ddcde24b50";
    private const int SourceLength = 49542;

    internal static void Native(string path)
    {
        using var source = File.OpenRead(path);
        using var document = FixtureDocument.Read(source);
        var root = document.RootElement;
        if (!root.TryGetProperty("Boundary", out _))
        {
            var (bottom, upper, rules) = ReadSource(root, source);
            Check(bottom, upper, rules);
            return;
        }
        var (componentBottom, componentUpper, componentRules) = ReadComponent(root);
        Check(componentBottom, componentUpper, componentRules);
    }

    internal static void Extract(string sourcePath, string outputPath)
    {
        using var source = File.OpenRead(sourcePath);
        using var document = FixtureDocument.Read(source);
        var root = document.RootElement;
        var (bottom, upper, rules) = ReadSource(root, source);
        var builder = new FixtureBuilder();
        int Text(string value) => builder.Add(FixtureKind.String, value);
        int Number(string value) => builder.Add(FixtureKind.Number, value);
        int componentRoot = builder.Add(FixtureKind.Object, children:
        [
            Text("Schema"), Number("1"),
            Text("Boundary"), Text(Boundary),
            Text("SourceSHA256"), Text(SourceHash),
            Text("SourceLength"), Number(SourceLength.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            Text("GameVersion"), builder.Import(root.GetProperty("GameVersion")),
            Text("GameModuleMvid"), builder.Import(root.GetProperty("GameModuleMvid")),
            Text("OriginalUnrulyBattle"), builder.Import(root.GetProperty("OriginalUnrulyBattle")),
            Text("CaptureFailures"), builder.Import(root.GetProperty("CaptureFailures")),
            Text("Pending"), builder.Import(root.GetProperty("Pending")),
            Text("SourceUnsupported"), builder.Import(root.GetProperty("Unsupported")),
            Text("Stages"), builder.Add(FixtureKind.Array, children: [builder.Import(bottom), builder.Import(upper)]),
            Text("CrossFloorRoomRules"), builder.Import(rules)
        ]);
        using var component = builder.Build(componentRoot, 0, new byte[32]);
        var (componentBottom, componentUpper, componentRules) = ReadComponent(component.RootElement);
        Require(componentBottom.ContentEquals(bottom) && componentUpper.ContentEquals(upper) && componentRules.ContentEquals(rules),
            "Retained component values differ from the native source.");
        Check(componentBottom, componentUpper, componentRules);
        using var output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write);
        component.Write(output);
    }

    private static (FixtureValue Bottom, FixtureValue Upper, FixtureValue Rules) ReadSource(FixtureValue root, Stream source)
    {
        source.Position = 0;
        Require(source.Length == SourceLength && Convert.ToHexString(SHA256.HashData(source)).Equals(SourceHash, StringComparison.OrdinalIgnoreCase),
            "Expected the pinned raw Railforged source archive SHA-256.");
        CheckNativeIdentity(root);
        var stages = root.GetProperty("Stages").EnumerateArray();
        return (stages.Single(row => row.GetProperty("Index").GetInt32() == 15),
            stages.Single(row => row.GetProperty("Index").GetInt32() == 20),
            root.GetProperty("Turns").EnumerateArray().Single(row => row.GetProperty("Index").GetInt32() == 2)
                .GetProperty("Before").GetProperty("PlayRules").GetProperty("Rooms"));
    }

    private static (FixtureValue Bottom, FixtureValue Upper, FixtureValue Rules) ReadComponent(FixtureValue root)
    {
        CheckNativeIdentity(root);
        var stages = root.GetProperty("Stages");
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("Boundary").GetString() == Boundary &&
            root.GetProperty("SourceSHA256").GetString() == SourceHash && root.GetProperty("SourceLength").GetInt32() == SourceLength &&
            root.GetProperty("SourceUnsupported").GetInt32() == 25 && stages.GetArrayLength() == 2,
            "Expected two exchanges with pinned raw Railforged archive provenance.");
        return (stages[0], stages[1], root.GetProperty("CrossFloorRoomRules"));
    }

    private static void CheckNativeIdentity(FixtureValue root)
    {
        var original = root.GetProperty("OriginalUnrulyBattle");
        Require(root.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            root.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            original.GetProperty("Scenario").GetString() == Scenario && original.GetProperty("BossKey").GetString() == Boss &&
            original.GetProperty("RailforgedInstalled").GetBoolean() && original.GetProperty("ControlledTestScenario").GetBoolean() &&
            root.GetProperty("CaptureFailures").GetInt32() == 0 && root.GetProperty("Pending").GetInt32() == 0,
            "Expected the pinned original Railforged TestScenario receipt.");
    }

    private static void Check(FixtureValue stage, FixtureValue upper, FixtureValue capturedRoomRules)
    {
        Require(MatchesExchange(stage, 15, 1) && MatchesExchange(upper, 20, 2), "Expected original exchanges 15 and 20 in order.");
        RoomCombatState before = stage.GetProperty("Before").Deserialize<RoomCombatState>()!;
        RoomCombatState native = stage.GetProperty("Actual").Deserialize<RoomCombatState>()!;
        CombatUnit actor = before.Units.Single(unit => unit.AssetKey == "R1_Attacker_AscendGainAtk_Ver2");
        CombatUnit after = native.Units.Single(unit => unit.Id == actor.Id);
        Require(before.RoomIndex == 0 && native.RoomIndex == 0 && actor.Status("armor")?.Stacks == 20 &&
            after.Status("armor")?.Stacks == 30 && actor.Status("burst") == null && after.Status("burst") == null,
            "Expected the native bottom-floor kill to retain the same actor and grant exactly ten Armor.");
        string original = JsonSerializer.Serialize(before, ModelJson.Options);
        RoomCombatResult projected = RoomCombatModel.Exchange(before,
            pendingPhysicalDeathIds: stage.GetProperty("PendingPhysicalDeaths").Deserialize<int[]>());
        Require(projected.Supported, projected.UnsupportedReason ?? "Unsupported exchange.");
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(projected.State, ModelJson.Options),
            JsonSerializer.Serialize(native, ModelJson.Options));
        Require(difference == null && JsonSerializer.Serialize(before, ModelJson.Options) == original,
            "Native bottom-floor exchange differs or mutated input: " + difference);
        Console.WriteLine("RAILFORGED-BOTTOM-KILL-BUMP PASS: full room, physical actor, Armor and RNG match native.");

        before = upper.GetProperty("Before").Deserialize<RoomCombatState>()!;
        native = upper.GetProperty("Actual").Deserialize<RoomCombatState>()!;
        actor = before.Units.Single(unit => unit.Id == actor.Id);
        after = native.Context!.Enchantments!.Rooms.Single(room => room.RoomIndex == 0).Units.Single(unit => unit.Id == actor.Id);
        Require(before.RoomIndex == 1 && native.Units.All(unit => unit.Id != actor.Id) &&
            actor.Status("armor")?.Stacks == 30 && after.Status("armor")?.Stacks == 40,
            "Expected the original kill to move its attacker to the lower room before adding Armor.");
        RoomPlayRule[] roomRules = capturedRoomRules.Deserialize<RoomPlayRule[]>()!;
        original = JsonSerializer.Serialize(before, ModelJson.Options);
        projected = RoomCombatModel.Exchange(before, roomRules: roomRules,
            pendingPhysicalDeathIds: upper.GetProperty("PendingPhysicalDeaths").Deserialize<int[]>());
        Require(projected.Supported, projected.UnsupportedReason ?? "Unsupported cross-floor exchange.");
        difference = ModelJson.Difference(JsonSerializer.Serialize(projected.State, ModelJson.Options),
            JsonSerializer.Serialize(native, ModelJson.Options));
        Require(difference == null && JsonSerializer.Serialize(before, ModelJson.Options) == original,
            "Native cross-floor exchange differs or mutated input: " + difference);
        Console.WriteLine("RAILFORGED-CROSS-FLOOR-KILL-BUMP PASS: full room, remote actor, physical placement, Armor and RNG match native.");
    }

    private static bool MatchesExchange(FixtureValue row, int index, int turn) => row.GetProperty("Index").GetInt32() == index &&
        row.GetProperty("Turn").GetInt32() == turn && row.GetProperty("Kind").GetString() == "Exchange";

    private static void Require(bool condition, string error)
    {
        if (!condition) throw new InvalidOperationException(error);
    }
}

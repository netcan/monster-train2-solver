using System.Security.Cryptography;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class IndulgentTitanskinChecks
{
    internal static void Export(string battlePath, string birthPath, string outputPath)
    {
        using var battleSource = ModelJson.ReadFixture(battlePath);
        using var birthSource = ModelJson.ReadFixture(birthPath);
        FixtureValue battle = battleSource.RootElement;
        FixtureValue birth = birthSource.RootElement;
        if (battle.GetProperty("GameVersion").GetString() != Game231FixtureCatalog.Version ||
            battle.GetProperty("GameModuleMvid").GetString() != Game231FixtureCatalog.Module ||
            battle.GetProperty("CaptureFailures").GetInt32() != 0 || battle.GetProperty("Pending").GetInt32() != 0)
            throw new InvalidDataException("Expected a complete original 2.3.1 native capture.");
        FixtureValue observation = battle.GetProperty("OriginalUnrulyBattle");
        FixtureValue hit = observation.GetProperty("TitanSkinAbsorptions").EnumerateArray().Single();
        int id = hit.GetProperty("UnitId").GetInt32();
        var stages = battle.GetProperty("Stages").EnumerateArray().Where(stage => stage.GetProperty("Turn").ContentEquals(hit.GetProperty("Turn")) &&
            stage.GetProperty("Before").GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == id)).ToArray();
        var builder = new FixtureBuilder();
        int root = builder.Add(FixtureKind.Object, children: [
            Key("Schema"), builder.Add(FixtureKind.Number, "1"),
            Key("Boundary"), Key("NativeIndulgentTitanskinBirthAndDamage"),
            Key("GameVersion"), builder.Import(battle.GetProperty("GameVersion")),
            Key("GameModuleMvid"), builder.Import(battle.GetProperty("GameModuleMvid")),
            Key("SourceBattleSha256"), Key(Hash(battlePath)),
            Key("SourceBirthSha256"), Key(Hash(birthPath)),
            Key("Observation"), builder.Import(observation),
            Key("Birth"), builder.Import(birth),
            Key("Stages"), builder.Add(FixtureKind.Array, children: stages.Select(builder.Import).ToArray())
        ]);
        using FixtureDocument component = builder.Build(root, 0, new byte[32]);
        Native(component.RootElement);
        using var output = new FileStream(outputPath, FileMode.CreateNew);
        component.Write(output);
        int Key(string key) => builder.Add(FixtureKind.String, key);
        static string Hash(string path)
        {
            using var file = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(file)).ToLowerInvariant();
        }
    }

    internal static void Native(FixtureValue component)
    {
        if (component.GetProperty("Schema").GetInt32() != 1 ||
            component.GetProperty("Boundary").GetString() != "NativeIndulgentTitanskinBirthAndDamage" ||
            component.GetProperty("GameVersion").GetString() != Game231FixtureCatalog.Version ||
            component.GetProperty("GameModuleMvid").GetString() != Game231FixtureCatalog.Module ||
            !IsSha(component.GetProperty("SourceBattleSha256").GetString()) ||
            !IsSha(component.GetProperty("SourceBirthSha256").GetString()))
            throw new InvalidDataException("Expected an original Indulgent Titanskin component with binary source provenance.");
        FixtureValue observation = component.GetProperty("Observation");
        FixtureValue birth = component.GetProperty("Birth");
        if (observation.GetProperty("Scenario").GetString() != "SoulSavior_R2_BossBattle_FavoredChild_3" ||
            observation.GetProperty("BossKey").GetString() != OuterBossDefinitions.IndulgentTitanskin ||
            !observation.GetProperty("ControlledTestScenario").GetBoolean() || !observation.GetProperty("RailforgedInstalled").GetBoolean())
            throw new InvalidDataException("Expected the installed original Indulgent Titanskin controlled scenario.");
        OuterBossBirthChecks.Native(birth, Game231FixtureCatalog.Module);
        var boss = birth.GetProperty("Births")[0].GetProperty("Actual").Deserialize<OuterBossPhaseState>()!.Boss;
        if (boss.AssetKey != OuterBossDefinitions.IndulgentTitanskin)
            throw new InvalidDataException("The original Indulgent birth does not identify the battle boss.");
        FixtureValue hit = observation.GetProperty("TitanSkinAbsorptions").EnumerateArray().Single();
        if (hit.GetProperty("UnitId").GetInt32() != boss.UnitId || hit.GetProperty("HealthDamage").GetInt32() != 5 ||
            hit.GetProperty("BlockedDamage").GetInt32() != 0 || hit.GetProperty("Absorbed").GetInt32() != 3)
            throw new InvalidDataException("Expected the original applied five-health hit with three Titanskin absorption.");
        var matched = new HashSet<string>(StringComparer.Ordinal);
        foreach (FixtureValue stage in component.GetProperty("Stages").EnumerateArray())
        {
            if (stage.GetProperty("Turn").GetInt32() != hit.GetProperty("Turn").GetInt32()) continue;
            var before = stage.GetProperty("Before").Deserialize<RoomCombatState>()!;
            var actual = stage.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            CombatUnit? source = before.Units.SingleOrDefault(unit => unit.Id == boss.UnitId);
            CombatUnit? target = actual.Units.SingleOrDefault(unit => unit.Id == boss.UnitId);
            if (source is not { Health: 1000 } || target is not { Health: 995 }) continue;
            string kind = stage.GetProperty("Kind").GetString()!;
            if (kind is not ("Room" or "Exchange") || !matched.Add(kind) ||
                source.Status("titanskin")?.Stacks != 3 || target.Status("titanskin")?.Stacks != 3 ||
                stage.GetProperty("ExternalTargets").GetArrayLength() != 0 ||
                stage.GetProperty("ExternalPyre").ValueKind != FixtureKind.Null)
                throw new InvalidDataException("Unexpected original Titanskin room boundary.");
            string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
            RoomCombatResult predicted = kind == "Exchange"
                ? RoomCombatModel.Exchange(before,
                    pendingPhysicalDeathIds: stage.GetProperty("PendingPhysicalDeaths").Deserialize<int[]>())
                : RoomCombatModel.Resolve(before, relentlessDecisions: RecordedBattleChecks.Decisions(stage));
            if (!predicted.Supported || immutable != JsonSerializer.Serialize(before, ModelJson.Options))
                throw new InvalidDataException("Original Titanskin room replay failed: " + predicted.UnsupportedReason);
            string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted.State, ModelJson.Options),
                JsonSerializer.Serialize(actual, ModelJson.Options));
            if (difference != null) throw new InvalidDataException("Original Titanskin room replay differs: " + difference);
        }
        if (matched.Count != 2) throw new InvalidDataException("Missing the original Titanskin room and exchange pair.");
        Console.WriteLine("NATIVE-INDULGENT-TITANSKIN PASS: exact native birth and both actual damage boundaries.");

        static bool IsSha(string? value) => value?.Length == 64 && value.All(Uri.IsHexDigit);
    }
}

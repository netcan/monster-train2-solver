using System.Security.Cryptography;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MoreTreasureUnitsChecks
{
    private const string Boundary = "OriginalMoreTreasureUnitsSpawning";
    private const string SourceHash = "2f7b381ce8dc7fea1f5ceac98ca19f2e4a44c95d171b439609bc934d6b32d5c1";

    internal static void Native(string referencePath, string? modifiedPath)
    {
        using var stock = ModelJson.ReadFixture(referencePath);
        EnemySpawnState baseline = Initial(stock.RootElement, 1, modified: false);
        SpawnProof(stock.RootElement, 1);
        if (modifiedPath == null)
        {
            Console.WriteLine("NATIVE-MORE-TREASURE-UNITS CONTROL PASS: one original eligible collector.");
            return;
        }

        using var modified = ModelJson.ReadFixture(modifiedPath);
        EnemySpawnState candidate = Initial(modified.RootElement, 2, modified: true);
        Require(SameCheckpoint(stock.RootElement.GetProperty("NativeRunInput"), modified.RootElement.GetProperty("NativeRunInput")),
            "The collector gain must use the same original native campaign input as the control.");
        Require(baseline.Rng.Equals(candidate.Rng) &&
            baseline.Treasures.Select(item => item.Unit.AssetKey).SequenceEqual(candidate.Treasures.Select(item => item.Unit.AssetKey)),
            "Collector count adjustment changed the original spawning stream or pool.");
        SpawnProof(modified.RootElement, 2);
        Console.WriteLine("NATIVE-MORE-TREASURE-UNITS PASS: deployment count 1→2, two original births and exact spawning replay.");
    }

    internal static void Export(string referencePath, string sourcePath, string outputPath)
    {
        string hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(sourcePath))).ToLowerInvariant();
        Require(hash == SourceHash, "Expected the pinned native MoreTreasureUnits source receipt.");
        Native(referencePath, sourcePath);
        using var source = ModelJson.ReadFixture(sourcePath);
        FixtureValue root = source.RootElement;
        FixtureValue[] births = root.GetProperty("Spawns").EnumerateArray().Where(row =>
            row.GetProperty("Before").GetProperty("TreasuresRemaining").GetInt32() >
            row.GetProperty("Actual").GetProperty("TreasuresRemaining").GetInt32()).ToArray();
        using var component = NativeFixtureCapture.Capture(new
        {
            Schema = 1, Boundary, SourceArchive = Path.GetFileName(sourcePath), SourceSHA256 = hash,
            GameVersion = root.GetProperty("GameVersion"), GameModuleMvid = root.GetProperty("GameModuleMvid"),
            NativeRunInput = root.GetProperty("NativeRunInput"), Deployment = Deployment(root), Spawns = births
        });
        using var stream = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write);
        component.Write(stream);
    }

    private static FixtureValue Deployment(FixtureValue root) =>
        root.GetProperty("Turns")[0].GetProperty("Before").GetProperty("Spawn");

    private static bool SameCheckpoint(FixtureValue stock, FixtureValue modified)
    {
        if (stock.ContentEquals(modified)) return true;
        if (stock.TryGetProperty("RunType", out _) || !modified.TryGetProperty("RunType", out _)) return false;

        FixtureProperty[] legacy = stock.EnumerateObject().ToArray();
        FixtureProperty[] current = modified.EnumerateObject().ToArray();
        return current.Length == legacy.Length + 1 && current.Count(field => field.Name == "RunType") == 1 &&
            legacy.Zip(current.Where(field => field.Name != "RunType"),
                (before, after) => before.Name == after.Name && before.Value.ContentEquals(after.Value)).All(equal => equal);
    }

    private static EnemySpawnState Initial(FixtureValue fixture, int count, bool modified)
    {
        FixtureValue run = fixture.GetProperty("NativeRunInput");
        Require(fixture.GetProperty("GameVersion").GetString() == "2.3.1" &&
            fixture.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            run.GetProperty("Schema").GetInt32() == 1 &&
            (!run.TryGetProperty("RunType", out FixtureValue runType) || runType.ValueKind == FixtureKind.String && runType.GetString() == "Class") &&
            run.GetProperty("Distance").GetInt32() == 1 && run.GetProperty("AscensionLevel").GetInt32() == 0 &&
            !run.GetProperty("WasDevCheatUsed").GetBoolean(),
            "Expected a pinned native Rank 0 Ring 2 battle.");
        bool component = fixture.TryGetProperty("Boundary", out FixtureValue boundary) && boundary.GetString() == Boundary;
        Require(!component || modified && fixture.GetProperty("Schema").GetInt32() == 1 &&
            fixture.GetProperty("SourceArchive").GetString() == "full-battle.mt2f" &&
            fixture.GetProperty("SourceSHA256").GetString() == SourceHash && fixture.GetProperty("Spawns").GetArrayLength() == 2,
            "The collector component lost its pinned native source or exact birth rows.");
        EnemySpawnState root = (component ? fixture.GetProperty("Deployment") : Deployment(fixture)).Deserialize<EnemySpawnState>()!;
        Require(root.Turn == 0 && root.TreasureEnabled && root.Treasures.Count > 0 &&
            root.TreasuresRemaining == count &&
            root.Train.Context!.Relics!.Count(RelicMoreTreasureUnitsModel.IsOriginal) == (modified ? 1 : 0),
            "Wrong original collector count, pool or relic at deployment.");
        return root;
    }

    private static void SpawnProof(FixtureValue fixture, int count)
    {
        var births = new List<(int Before, int Actual)>();
        foreach (FixtureValue row in fixture.GetProperty("Spawns").EnumerateArray())
        {
            var before = row.GetProperty("Before").Deserialize<EnemySpawnState>()!;
            var actual = row.GetProperty("Actual").Deserialize<EnemySpawnState>()!;
            if (before.TreasuresRemaining == actual.TreasuresRemaining) continue;
            Require(row.GetProperty("IncludeTreasure").GetBoolean() && before.TreasuresRemaining == actual.TreasuresRemaining + 1,
                "Collector count changed outside one original treasure birth.");
            var existing = before.Train.Rooms.SelectMany(room => room.Units).Select(unit => unit.Id).ToHashSet();
            var pool = before.Treasures.Select(item => item.Unit.AssetKey).ToHashSet(StringComparer.Ordinal);
            Require(actual.Train.Rooms.SelectMany(room => room.Units).Count(unit => !existing.Contains(unit.Id) && pool.Contains(unit.AssetKey)) == 1,
                "Collector count changed without a new original pool actor.");
            births.Add((before.TreasuresRemaining, actual.TreasuresRemaining));
        }
        Require(births.SequenceEqual(Enumerable.Range(0, count).Reverse().Select(remaining => (remaining + 1, remaining))),
            "The recorded original collector births did not exhaust the expected count.");
        EnemySpawningChecks.Native(fixture);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

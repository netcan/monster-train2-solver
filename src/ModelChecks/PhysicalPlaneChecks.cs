using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PhysicalPlaneChecks
{
    internal static void Native(string path)
    {
        using var document = FixtureDocument.Read(path);
        var root = document.RootElement;
        Require(!document.HasTextSource && root.GetProperty("Schema").GetInt32() == 1 &&
            root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("Boundary").GetString() == "OriginalSetSpawnPointAcrossCopies" &&
            !root.GetProperty("GameplaySuppressed").GetBoolean() && !root.GetProperty("WholeBattleVerified").GetBoolean() &&
            root.GetProperty("Errors").GetArrayLength() == 0, "Physical copy provenance or native error gate failed.");
        var samples = root.GetProperty("Samples").EnumerateArray().ToArray();
        Require(samples.Length > 0, "No original physical copy operations observed.");
        bool crossesCopies = false, removesBirth = false, compactsCopy = false;
        var copies = new HashSet<int>();
        foreach (var sample in samples)
        {
            Verify(sample);
            var before = sample.GetProperty("Before").Deserialize<SpawnPointWorld>()!;
            foreach (var group in before.Groups.Where(group => group.PreviewCopyId != null)) copies.Add(group.PreviewCopyId!.Value);
            if (sample.TryGetProperty("Operation", out var operation) && operation.GetString() == "Compact")
            {
                compactsCopy = true;
                continue;
            }
            var actual = sample.GetProperty("Actual").Deserialize<SpawnPointWorld>()!;
            int id = sample.GetProperty("UnitId").GetInt32();
            var actor = before.Units.Single(unit => unit.UnitId == id);
            var next = actual.Units.Single(unit => unit.UnitId == id);
            crossesCopies |= actor.Current != null && next.Current != null && actor.Current.PreviewCopyId != next.Current.PreviewCopyId;
            removesBirth |= actor.SpawnedInPreview && actor.Current?.PreviewCopyId > 0 && next.Current == null;
        }
        Require(crossesCopies && removesBirth && compactsCopy && copies.Count > 1,
            "Native coverage lacks distinct copied lists, cross-plane assignment, copied compaction or preview birth detachment.");
        Parallel.For(0, 32, _ => { foreach (var sample in samples) Verify(sample); });
        Console.WriteLine($"NATIVE-PHYSICAL-PLANE-CHECKS PASS: {samples.Length} original assignments/compactions, {copies.Count} distinct copied lists, full ownership/reference states and 32 immutable branches; terminal aura-summon integration remains open.");
    }
    private static void Verify(FixtureValue sample)
    {
        Require(sample.GetProperty("Completed").GetBoolean() && sample.GetProperty("Difference").ValueKind == FixtureKind.Null,
            "Incomplete or differing native copied-point assignment.");
        var before = sample.GetProperty("Before").Deserialize<SpawnPointWorld>()!;
        var expected = sample.GetProperty("Actual").Deserialize<SpawnPointWorld>()!;
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        string operation = sample.TryGetProperty("Operation", out var kind) ? kind.GetString()! : "Set";
        int pivot = sample.TryGetProperty("Pivot", out var index) ? index.GetInt32() : -1;
        var result = SpawnPointModel.Apply(before, operation, sample.GetProperty("RoomIndex").GetInt32(),
            sample.GetProperty("Team").Deserialize<CombatTeam>(), sample.GetProperty("UnitId").GetInt32(),
            index: pivot,
            target: sample.GetProperty("Target").Deserialize<SpawnPointReference>(),
            previewCopyId: sample.GetProperty("PreviewCopyId").Deserialize<int?>());
        Require(result.Supported, "Copied point assignment unsupported: " + result.UnsupportedReason);
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(result.State, ModelJson.Options),
            JsonSerializer.Serialize(expected, ModelJson.Options));
        Require(difference == null, "Copied point assignment differs: " + difference);
        if (sample.TryGetProperty("PivotMoves", out var moves)) Require(result.PivotMoves == moves.GetInt32(), "Copied compaction changed the native pivot count.");
        Require(JsonSerializer.Serialize(before, ModelJson.Options) == parent, "Copied point branch mutated its parent.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
}

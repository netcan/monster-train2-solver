using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CharacterRemovalChecks
{
    internal static void Native(string path)
    {
        using var document = FixtureDocument.Read(path);
        var root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("Boundary").GetString() == "NativeCharacterRemovalApis" &&
            !root.GetProperty("GameplaySuppressed").GetBoolean() && root.GetProperty("Errors").GetArrayLength() == 0,
            "Native removal calibration provenance or error gate failed.");
        var samples = root.GetProperty("Samples").EnumerateArray().ToArray();
        foreach (var sample in samples) Verify(sample);
        var stages = samples.Where(item => item.GetProperty("Operation").GetString() == "SetStage").ToArray();
        Require(new[] { 1, 2, 3 }.All(stage => stages.Any(item => item.GetProperty("RequestedStage").GetInt32() == stage)),
            "Not all native destruction stages were observed.");
        var destroys = samples.Where(item => item.GetProperty("Operation").GetString() == "OnDestroy").ToArray();
        Require(destroys.Length >= 3 && destroys.Any(item => Before(item)[0].Information.Any(info =>
            info.References.Any(reference => reference.Name == "lastAttackerCharacter" && reference.Ids.Count > 0))) &&
            destroys.Any(item => Before(item)[0].Information.Any(info => info.Kind == "_previewStateInformation")),
            "Native destruction needs live attacker and preview-state reference coverage.");
        Require(new[] { "_primaryStateInformation", "_previewStateInformation", "_temporaryStateInformation" }.All(kind =>
            samples.Any(item => Before(item).Any(actor => actor.Information.Any(info => info.Kind == kind)))) &&
            stages.Where(item => item.GetProperty("RequestedStage").GetInt32() == 3).All(item =>
                item.GetProperty("After").Deserialize<CharacterRemovalState[]>()![0].WeakTargetId != 0) &&
            destroys.All(item => item.GetProperty("After").Deserialize<CharacterRemovalState[]>()![0].WeakTargetId == 0),
            "Scheduling must preserve weak references; actual destruction clears them and every information layer.");
        var queues = samples.Where(item => item.GetProperty("Operation").GetString() == "ProcessQueue").ToArray();
        Require(queues.Any(item => Before(item).Any(actor => actor.Stage == 1) && item.GetProperty("QueueIds").GetArrayLength() > 0),
            "A pending dissolve actor surviving a nonempty manager removal queue was not observed.");
        Require(stages.Any(begin => begin.GetProperty("RequestedStage").GetInt32() == 1 && stages.Any(queued =>
            queued.GetProperty("RequestedStage").GetInt32() == 2 &&
            queued.GetProperty("ActorId").GetInt32() == begin.GetProperty("ActorId").GetInt32() &&
            queued.GetProperty("Frame").GetInt32() - begin.GetProperty("Frame").GetInt32() >= 10)),
            "The actual minimum-ten-frame dissolve callback delay was not observed.");
        Parallel.For(0, 32, _ => { foreach (var sample in samples) Verify(sample); });
        Console.WriteLine($"NATIVE-CHARACTER-REMOVAL-CHECKS PASS: {samples.Length} original native API transitions, " +
            $"{destroys.Length} actual destructions, {queues.Length} manager queues, delayed callbacks, " +
            "pending actors survive queue processing, primary/preview/temporary references and 32 isolated branches; whole-battle scheduling remains open.");
    }
    private static CharacterRemovalState[] Before(FixtureValue sample) => sample.GetProperty("Before").Deserialize<CharacterRemovalState[]>()!;
    private static void Verify(FixtureValue sample)
    {
        Require(sample.GetProperty("Completed").GetBoolean() && sample.GetProperty("Difference").ValueKind == FixtureKind.Null,
            "Incomplete or differing native removal transition.");
        var before = Before(sample);
        int[] queued = sample.GetProperty("QueueIds").Deserialize<int[]>()!;
        string parent = JsonSerializer.Serialize(new { before, queued });
        string operation = sample.GetProperty("Operation").GetString()!;
        CharacterRemovalState[] predicted = operation == "ProcessQueue" ? CharacterRemovalModel.ProcessQueue(before, queued).ToArray() :
            new[] { operation == "OnDestroy" ? CharacterRemovalModel.CompleteDestruction(before[0]) :
                CharacterRemovalModel.SetStage(before[0], sample.GetProperty("RequestedStage").GetInt32()) };
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted),
            JsonSerializer.Serialize(sample.GetProperty("After").Deserialize<CharacterRemovalState[]>()!));
        Require(difference == null, $"{operation} frame {sample.GetProperty("Frame").GetInt32()}: {difference}");
        Require(JsonSerializer.Serialize(new { before, queued }) == parent, "Removal transition mutated its parent.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
}

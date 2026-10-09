using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PreviewReferenceChecks
{
    internal static void Native(string path)
    {
        using var document = FixtureDocument.Read(path);
        var root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("Boundary").GetString() == "OriginalSetCharacterPreviewStateOn" &&
            root.GetProperty("ModifierScenario").GetString() == "persistent-enchantment-summons" &&
            !root.GetProperty("GameplaySuppressed").GetBoolean() && !root.GetProperty("FullPreviewSimulationVerified").GetBoolean() &&
            root.GetProperty("Errors").GetArrayLength() == 0 && root.GetProperty("Mismatches").GetInt32() == 0,
            "Native preview restoration provenance or error gate failed.");
        var samples = root.GetProperty("Samples").EnumerateArray().ToArray();
        var births = root.GetProperty("Births").EnumerateArray().ToArray();
        Require(samples.Length == 19 && births.Length == 1, "Preview restoration observations are incomplete.");
        int birthId = births[0].GetProperty("UnitId").GetInt32();
        var origin = births[0].GetProperty("Primary").Deserialize<CombatUnit>()!;
        Require(origin.Id == birthId && origin.BaseAttack == 8 && origin.Health == 30 && origin.MaxHealth == 30 &&
            origin.SpawnerCardId > 0 && origin.IsSpawning == false && origin.Statuses.Count == 0 &&
            origin.Modifiers!.Upgrades.Count == 0 && origin.Modifiers.HealthFromUpgrades.Count == 0 &&
            origin.DeathState!.IsDestroyed == false && origin.DeathState.IsBeingRemoved == false &&
            births[0].GetProperty("TemporaryPoint").GetBoolean() && births[0].GetProperty("RoomIndex").GetInt32() == 0 &&
            births[0].GetProperty("PointIndex").GetInt32() == 2 && births[0].GetProperty("RemovalStage").GetInt32() == 3,
            "Native preview birth must retain its pre-preview primary origin and temporary spawn point.");
        bool clearedCache = false, preservedCache = false, restoredOnce = false, retainedAuraTarget = false;
        foreach (var sample in samples)
        {
            Verify(sample);
            var before = sample.GetProperty("Before").Deserialize<CombatUnit>()!;
            var preview = sample.GetProperty("Preview").Deserialize<CombatUnit>()!;
            var actual = sample.GetProperty("Actual").Deserialize<CombatUnit>()!;
            int[] removed = sample.GetProperty("RemovedPreviewUnitIds").Deserialize<int[]>()!;
            foreach (var trigger in before.Triggers)
            {
                var tested = preview.Triggers.Single(item => item.StateId == trigger.StateId);
                var restored = actual.Triggers.Single(item => item.StateId == trigger.StateId);
                restoredOnce |= trigger.Once && trigger.HasTriggered && !tested.HasTriggered && restored.HasTriggered;
                for (int index = 0; index < trigger.Effects.Count; index++)
                {
                    var previous = trigger.Effects[index];
                    var observed = tested.Effects[index];
                    var resulting = restored.Effects[index];
                    clearedCache |= previous.Summon?.FirstSpawnedUnitId > 0 && observed.Summon?.FirstSpawnedUnitId == birthId &&
                        removed.Contains(birthId) && resulting.Summon?.FirstSpawnedUnitId == 0;
                    preservedCache |= removed.Contains(birthId) && previous.Summon?.FirstSpawnedUnitId > 0 &&
                        previous.Summon.FirstSpawnedUnitId == observed.Summon?.FirstSpawnedUnitId &&
                        previous.Summon.FirstSpawnedUnitId == resulting.Summon?.FirstSpawnedUnitId;
                    retainedAuraTarget |= removed.Contains(birthId) && resulting.Enchantment?.State.PreviewTargets.Any(target => target.UnitId == birthId) == true;
                }
            }
        }
        Require(clearedCache && preservedCache && restoredOnce && retainedAuraTarget,
            "Native preview coverage lacks overwritten/live caches, primary once flags or retained temporary aura targets.");
        Parallel.For(0, 32, _ => { foreach (var sample in samples) Verify(sample); });
        Console.WriteLine($"NATIVE-PREVIEW-REFERENCE-CHECKS PASS: {samples.Length} complete actor restorations, {births.Length} temporary birth, " +
            "cleared overwritten cache, preserved live cache, primary once flags, retained aura targets and 32 immutable branches; full preview simulation remains open.");
    }
    private static void Verify(FixtureValue sample)
    {
        Require(sample.GetProperty("Completed").GetBoolean() && sample.GetProperty("Difference").ValueKind == FixtureKind.Null,
            "Incomplete or differing native preview restoration.");
        var before = sample.GetProperty("Before").Deserialize<CombatUnit>()!;
        var preview = sample.GetProperty("Preview").Deserialize<CombatUnit>()!;
        int[] removed = sample.GetProperty("RemovedPreviewUnitIds").Deserialize<int[]>()!;
        string parent = JsonSerializer.Serialize(new { before, preview, removed }, ModelJson.Options);
        var predicted = PreviewEffectsModel.Restore(before, preview, removed);
        var actual = sample.GetProperty("Actual").Deserialize<CombatUnit>()!;
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(predicted, ModelJson.Options),
            JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, $"Actor {before.Id}, frame {sample.GetProperty("Frame").GetInt32()}: {difference}");
        Require(JsonSerializer.Serialize(new { before, preview, removed }, ModelJson.Options) == parent,
            "Preview restoration mutated its parent.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
}

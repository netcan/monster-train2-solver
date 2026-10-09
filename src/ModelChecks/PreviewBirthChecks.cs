using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PreviewBirthChecks
{
    internal static void Native(string path)
    {
        using var document = FixtureDocument.Read(path);
        var root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 1 && root.GetProperty("GameVersion").GetString() == "2.2.1" &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("Boundary").GetString() == "OriginalPreviewBirthLifecycle" &&
            !root.GetProperty("GameplaySuppressed").GetBoolean() && !root.GetProperty("WholeBattleVerified").GetBoolean() &&
            root.GetProperty("Errors").GetArrayLength() == 0, "Preview birth provenance or native error gate failed.");
        var births = root.GetProperty("Births").EnumerateArray().ToArray();
        Require(births.Length > 0, "No native preview-born actors observed.");
        foreach (var birth in births) Verify(birth);
        Parallel.For(0, 32, _ => { foreach (var birth in births) Verify(birth); });
        Console.WriteLine($"NATIVE-PREVIEW-BIRTH-CHECKS PASS: {births.Length} native primary initializations, preview restorations and destructions, complete states and 32 immutable branches; full preview physical planes remain open.");
    }
    private static void Verify(FixtureValue birth)
    {
        var definition = birth.GetProperty("Definition").Deserialize<CardPlayRule>()!;
        var card = birth.GetProperty("SourceCard").Deserialize<CardInstanceState>();
        int id = birth.GetProperty("UnitId").GetInt32();
        var disabled = birth.GetProperty("DisabledAbilities").Deserialize<string[]>()!;
        var marker = birth.GetProperty("AbilityMarker").Deserialize<CombatStatus>()!;
        var preview = birth.GetProperty("Preview").Deserialize<CombatUnit>()!;
        string parent = JsonSerializer.Serialize(new { definition, card, disabled, marker, preview }, ModelJson.Options);
        CombatUnit primary = PreviewBirthModel.InitialPrimary(definition, card, id, disabled, marker);
        Compare(primary, birth.GetProperty("Primary").Deserialize<CombatUnit>()!, "Initial primary");
        var withOrigin = preview.WithDeathState(preview.DeathState!.WithPreviewPrimary(primary));
        var restored = PreviewBirthModel.Restore(withOrigin, [id]);
        Compare(restored, birth.GetProperty("Restored").Deserialize<CombatUnit>()!, "Restored primary");
        Compare(restored, birth.GetProperty("Destroyed").Deserialize<CombatUnit>()!, "Original OnDestroy");
        Require(primary.BaseAttack == 8 && primary.MaxHealth == 30 && preview.BaseAttack == 9 && preview.MaxHealth == 32 &&
            primary.Modifiers!.Upgrades.Count == 0 && preview.Modifiers!.Upgrades.Count > 0 &&
            primary.Statuses.Count == 0 && preview.Status("cardless")?.Stacks == 1 && restored.StatusImmunities.Contains("endless") &&
            restored.DeathState!.IsDestroyed == true && !restored.DeathState.IsBeingRemoved && !restored.DeathState.HasFinishedDying &&
            !restored.DeathState.HasStatisticsListener && restored.DeathState.StatisticsListenerOnce == false &&
            restored.Triggers.SelectMany(trigger => trigger.Effects).Any(effect => effect.Enchantment?.State.PreviewTargets.Count > 0) &&
            birth.GetProperty("TemporaryPoint").GetBoolean() && birth.GetProperty("RemovalStage").GetInt32() == 3,
            "Native preview birth lacks distinct primary/preview stats, shared immunity/maps or original removal scheduling.");
        Require(JsonSerializer.Serialize(new { definition, card, disabled, marker, preview }, ModelJson.Options) == parent,
            "Preview birth restoration mutated its parent.");
    }
    private static void Compare(CombatUnit expected, CombatUnit actual, string phase)
    {
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(expected, ModelJson.Options), JsonSerializer.Serialize(actual, ModelJson.Options));
        Require(difference == null, $"{phase} actor {expected.Id}: {difference}");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
}

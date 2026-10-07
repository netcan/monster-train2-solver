using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class DirectUnitUpgradeChecks
{
    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("DirectUnitUpgrades", out var records)) return;
        var cases = records.EnumerateArray().ToArray();
        foreach (var sample in cases) Verify(sample);
        if (fixture.GetProperty("ModifierScenario").GetString() == "direct-unit-upgrades")
        {
            var labels = cases.Select(sample => sample.GetProperty("Label").GetString()).ToHashSet();
            Require(new[] { "duplicate-first", "duplicate-second", "remove-first-with-caller-stats", "remove-second", "remove-missing-noop",
                "anonymous-add", "anonymous-fresh-removal", "anonymous-same-object-removal", "unique-noop", "clone-direct-add", "capacity-rejected",
                "equipment-limit-cap", "equipment-limit-remove", "negative-add", "negative-remove", "keyed-remove", "lethal-partial-add" }
                .All(labels.Contains), "Missing direct native API mechanism coverage.");
            var byLabel = cases.ToDictionary(sample => sample.GetProperty("Label").GetString()!);
            CombatUnit Before(string label) => byLabel[label].GetProperty("Before").Deserialize<RoomCombatState>()!.Units
                .Single(unit => unit.Id == byLabel[label].GetProperty("UnitId").GetInt32());
            CombatUnit After(string label) => byLabel[label].GetProperty("After").Deserialize<RoomCombatState>()!.Units
                .Single(unit => unit.Id == byLabel[label].GetProperty("UnitId").GetInt32());
            Require(After("duplicate-second").Modifiers!.Upgrades.Count == Before("duplicate-second").Modifiers!.Upgrades.Count + 1 &&
                After("remove-first-with-caller-stats").Modifiers!.Upgrades.Count == Before("remove-first-with-caller-stats").Modifiers!.Upgrades.Count - 1,
                "Native duplicate/single-copy paths were not reached.");
            Require(After("anonymous-fresh-removal").Modifiers!.Upgrades.Count == Before("anonymous-fresh-removal").Modifiers!.Upgrades.Count &&
                After("anonymous-same-object-removal").Modifiers!.Upgrades.Count == Before("anonymous-same-object-removal").Modifiers!.Upgrades.Count - 1,
                "Native anonymous descriptor/object identity paths were not reached.");
            Require(Before("clone-direct-add").Modifiers!.IsClone &&
                byLabel["clone-direct-add"].GetProperty("Upgrade").Deserialize<CardUpgradeModifier>()!.ExcludeFromClones &&
                After("clone-direct-add").Modifiers!.Upgrades.Count > Before("clone-direct-add").Modifiers!.Upgrades.Count,
                "Native direct clone-exclusion bypass was not reached.");
            Require(After("equipment-limit-cap").Modifiers!.EquipmentLimit == 4 && After("equipment-limit-remove").Modifiers!.EquipmentLimit == -2 &&
                Before("keyed-remove").Modifiers!.HealthFromUpgrades.Count == 1 && After("keyed-remove").Modifiers!.HealthFromUpgrades.Count == 0,
                "Native equipment-limit or attributed-health paths were not reached.");
            Require(!byLabel["lethal-partial-add"].GetProperty("After").Deserialize<RoomCombatState>()!.Units
                .Any(unit => unit.Id == byLabel["lethal-partial-add"].GetProperty("UnitId").GetInt32()), "Native partial-death path was not reached.");
            Parallel.For(0, 32, _ => { foreach (var sample in cases) Verify(sample); });
        }
        else if (fixture.GetProperty("ModifierScenario").GetString() is "equipment-overflow" or "equipment-abilities")
            Parallel.For(0, 32, _ => { foreach (var sample in cases) Verify(sample); });
        Console.WriteLine($"NATIVE-DIRECT-UPGRADE-CHECKS PASS: {cases.Length} native room/context transitions, independent descriptor/object identity and parent isolation.");
    }
    private static void Verify(FixtureValue sample)
    {
        var before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
        var after = sample.GetProperty("After").Deserialize<RoomCombatState>()!;
        var upgrade = sample.GetProperty("Upgrade").Deserialize<CardUpgradeModifier>()!;
        int? index = sample.GetProperty("AnonymousRemovalIndex").ValueKind == FixtureKind.Null ? null : sample.GetProperty("AnonymousRemovalIndex").GetInt32();
        string frozen = JsonSerializer.Serialize(before, ModelJson.Options);
        var result = UnitModifierModel.ApplyDirect(before, sample.GetProperty("UnitId").GetInt32(), upgrade,
            sample.GetProperty("Remove").GetBoolean(), sample.GetProperty("UpgradeId").GetString()!, index);
        Require(result.Supported, "Direct native API unsupported: " + result.UnsupportedReason);
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(result.State, ModelJson.Options), JsonSerializer.Serialize(after, ModelJson.Options));
        Require(difference == null, sample.GetProperty("Label").GetString() + ": " + difference);
        Require(JsonSerializer.Serialize(before, ModelJson.Options) == frozen, "Direct upgrade mutated its parent.");
    }
    private static void Require(bool passed, string message) { if (!passed) throw new InvalidOperationException(message); }
}

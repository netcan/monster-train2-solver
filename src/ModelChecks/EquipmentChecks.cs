using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class EquipmentChecks
{
    internal static void Native(FixtureValue fixture)
    {
        if (!fixture.TryGetProperty("EquipmentOperations", out var records)) return;
        var samples = records.EnumerateArray().ToArray();
        foreach (var sample in samples) Verify(sample);
        if (fixture.GetProperty("ModifierScenario").GetString() == "equipment")
        {
            Require(samples.Any(sample => !sample.GetProperty("Remove").GetBoolean() &&
                Host(sample, "Before").EquipmentCards!.Count >= Host(sample, "Before").Modifiers!.EquipmentLimit &&
                Host(sample, "Before").EquipmentCards!.Count > 0), "Native oldest-equipment replacement was not reached.");
            Require(samples.Any(sample => sample.GetProperty("Remove").GetBoolean() && sample.GetProperty("CardId").GetInt32() == 0 &&
                Host(sample, "Before").EquipmentCards!.Count > 1 && Host(sample, "After").EquipmentCards!.Count == 0),
                "Native reverse remove-all was not reached.");
            Require(samples.Any(sample => sample.GetProperty("Remove").GetBoolean() &&
                Host(sample, "Before").Modifiers!.Upgrades.Any(upgrade => upgrade.DataId.Length == 0 && upgrade.EquipmentSourceCardId.HasValue) &&
                Host(sample, "After").Modifiers!.Upgrades.Count(upgrade => upgrade.DataId.Length == 0 && upgrade.EquipmentSourceCardId.HasValue) <
                Host(sample, "Before").Modifiers!.Upgrades.Count(upgrade => upgrade.DataId.Length == 0 && upgrade.EquipmentSourceCardId.HasValue)),
                "Native permanent anonymous upgrade object removal was not reached.");
            Require(samples.Any(sample =>
            {
                var after = sample.GetProperty("After").Deserialize<RoomCombatState>()!;
                return after.Context!.OtherPiles!.Single(pile => pile.Name == "Standby").EquipmentConditions!.Any(condition =>
                    after.Units.Any(unit => unit.Id == condition.HostUnitId) && after.Context.FindCard(condition.CardId)!.EquippedUnitId == 0);
            }), "Native removed equipment waiting on its living original host was not reached.");
            Require(fixture.GetProperty("CardCycles").EnumerateArray().Any(sample =>
            {
                if (sample.GetProperty("Kind").GetString() != "Draw") return false;
                var context = sample.GetProperty("StandbyContext").Deserialize<CombatContext>()!;
                var living = sample.GetProperty("LivingUnitIds").EnumerateArray().Select(id => id.GetInt32()).ToHashSet();
                return context.OtherPiles!.Single(pile => pile.Name == "Standby").EquipmentConditions!.Any(condition =>
                    condition.ReturnToHand && !living.Contains(condition.HostUnitId));
            }), "Native global return after the original host's death was not reached.");
            var attached = samples.First(sample => !sample.GetProperty("Remove").GetBoolean());
            Require(!RoomCombatModel.ApplyEquipment(attached.GetProperty("After").Deserialize<RoomCombatState>()!,
                attached.GetProperty("UnitId").GetInt32(), attached.GetProperty("CardId").GetInt32(),
                attached.GetProperty("Definitions").Deserialize<BattlePlayRules>()!).Supported,
                "Unmodeled repeated raw attachment was silently treated as a no-op.");
            Parallel.For(0, 32, _ => { foreach (var sample in samples) Verify(sample); });
        }
        Console.WriteLine($"NATIVE-EQUIPMENT-CHECKS PASS: {samples.Length} independently compared native attachment/removal room/context transitions and parent isolation.");
    }

    private static CombatUnit Host(FixtureValue sample, string field) => sample.GetProperty(field).Deserialize<RoomCombatState>()!.Units
        .Single(unit => unit.Id == sample.GetProperty("UnitId").GetInt32());

    private static void Verify(FixtureValue sample)
    {
        Require(sample.GetProperty("Completed").GetBoolean(), "Incomplete native equipment operation.");
        var before = sample.GetProperty("Before").Deserialize<RoomCombatState>()!;
        var after = sample.GetProperty("After").Deserialize<RoomCombatState>()!;
        var definitions = sample.GetProperty("Definitions").Deserialize<BattlePlayRules>()!;
        string frozen = JsonSerializer.Serialize(before, ModelJson.Options);
        var result = RoomCombatModel.ApplyEquipment(before, sample.GetProperty("UnitId").GetInt32(), sample.GetProperty("CardId").GetInt32(),
            definitions, sample.GetProperty("Remove").GetBoolean());
        Require(result.Supported, "Native equipment operation unsupported: " + result.UnsupportedReason);
        string? difference = ModelJson.Difference(JsonSerializer.Serialize(result.State, ModelJson.Options), JsonSerializer.Serialize(after, ModelJson.Options));
        Require(difference == null, "Equipment unit=" + sample.GetProperty("UnitId").GetInt32() + " card=" + sample.GetProperty("CardId").GetInt32() +
            " remove=" + sample.GetProperty("Remove").GetBoolean() + ": " + difference);
        Require(JsonSerializer.Serialize(before, ModelJson.Options) == frozen, "Equipment operation mutated its parent.");
    }
    private static void Require(bool passed, string message) { if (!passed) throw new InvalidOperationException(message); }
}

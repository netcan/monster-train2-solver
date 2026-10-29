using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class GreenRecruitSoulsPreviewChecks
{
    private const string Reason = "Native rolling turn successor mismatch observation.";

    internal static void Native(FixtureValue fixture)
    {
        FixtureValue[] rows = NativeBranchObservationChecks.Replay(fixture, Reason, HistoricalMismatch);
        BattleTurnState before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatContext context = before.Spawn.Train.Context!;
        CombatUnit recruit = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 17);
        Require(rows.Length == 39 && before.Spawn.Turn == 9 && context.NextUnitId == 38 &&
            actual.Spawn.Train.Context!.NextUnitId == 38 && PreviewBirthModel.IsOriginalGreenRecruit(recruit, context),
            "Missing original Soulsmelter recruit boundary.");
        foreach (string control in new[] { "equipment", "origin", "death", "settlement" })
        {
            JsonNode changed = JsonSerializer.SerializeToNode(recruit, ModelJson.Options)!;
            if (control == "equipment") changed["EquipmentCards"]![0] = 999;
            if (control == "origin") changed["Triggers"]![2]!["Origin"]!["EquipmentCardId"] = 999;
            if (control == "death") changed["Triggers"]![1]!["Effects"]![0]!["Action"]!["Statuses"]![0]!["Stacks"] = 2;
            if (control == "settlement") changed["Triggers"]![2]!["Effects"]![0]!["Action"]!["Type"] = "unknown";
            Require(!PreviewBirthModel.IsOriginalGreenRecruit(changed.Deserialize<CombatUnit>(ModelJson.Options)!, context),
                "Uncalibrated Soulsmelter trigger accepted: " + control);
        }
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options), expected = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            BattleTurnResult result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == expected, "Concurrent successor differs.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Replay mutated its parent.");
        Console.WriteLine("GREEN-RECRUIT-SOULS-PREVIEW PASS: 39 native inputs, full successor/RNG, signature controls and four concurrent replays.");
    }

    private static void HistoricalMismatch(BattleTurnState old, BattleTurnState actual)
    {
        Require(old.Spawn.Train.Context!.NextUnitId == 39 && actual.Spawn.Train.Context!.NextUnitId == 38 &&
            old.Spawn.Train.Context.SpawnPoints!.Units.Any(unit => unit.UnitId == 38 && unit.SpawnedInPreview) &&
            actual.Spawn.Train.Context.SpawnPoints!.Units.All(unit => unit.UnitId != 38), "Original temporary identity mismatch was lost.");
        JsonNode projected = JsonNode.Parse(NativeBranchObservationChecks.Key(old))!;
        JsonNode observed = JsonNode.Parse(NativeBranchObservationChecks.Key(actual))!;
        projected["NextUnitId"] = observed["NextUnitId"]!.DeepClone();
        projected["Context"]!["NextUnitId"] = observed["Context"]!["NextUnitId"]!.DeepClone();
        projected["Context"]!["SpawnPoints"] = observed["Context"]!["SpawnPoints"]!.DeepClone();
        Require(JsonNode.DeepEquals(projected, observed), "Historical mismatch extends beyond temporary identity: " +
            ModelJson.Difference(projected.ToJsonString(), observed.ToJsonString()));
    }

    internal static void Pair(FixtureValue original, FixtureValue repaired, FixtureValue? repaired2 = null)
    {
        Native(original); Native(repaired);
        NativeBranchObservationChecks.Pair(original, repaired, originalPredictionPresent: true);
        if (repaired2.HasValue) { Native(repaired2.Value); NativeBranchObservationChecks.Pair(original, repaired2.Value, originalPredictionPresent: true); }
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

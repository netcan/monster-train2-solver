using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class WoundedHordePreviewIdentityChecks
{
    private const string Reason = "Native partial prefix successor mismatch observation.";
    internal static void Native(FixtureValue fixture, FixtureValue? replay = null, bool repairedPrediction = false)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason, historicalBoundaryMismatch: (historical, actual) =>
            Require(!repairedPrediction && historical.Spawn.Train.Context!.NextUnitId == 28 &&
                actual.Spawn.Train.Context!.NextUnitId == 27, "Missing original identity mismatch."));
        Require(rows.Length == 21 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing original input chain.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(actual.Spawn.Train.Context!.NextUnitId == 27 &&
            actual.Spawn.Train.Context.SpawnPoints!.Units.All(unit => unit.UnitId != 27), "Temporary unit survived native cleanup.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Concurrent successor differs: " + result.UnsupportedReason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "EndTurn mutated its parent.");
        foreach (string control in new[] { "MonsterChampionUnderlegion1", "Boss_Level8_Fragile", "EnchanterT2_Titanskin", "MonsterFunguy" })
        {
            JsonNode altered = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            void Rewrite(JsonNode? node)
            {
                if (node is JsonObject obj)
                {
                    if (obj["AssetKey"]?.GetValueKind() == JsonValueKind.String && obj["AssetKey"]!.GetValue<string>() == control)
                        obj["AssetKey"] = "unknown";
                    foreach (var entry in obj.ToArray()) Rewrite(entry.Value);
                }
                else if (node is JsonArray array) foreach (var child in array) Rewrite(child);
            }
            Rewrite(altered);
            var result = BattleTurnModel.EndTurn(altered.Deserialize<BattleTurnState>(ModelJson.Options)!);
            Require(!result.Supported || result.State!.Spawn.Train.Context!.NextUnitId != 27,
                "Unknown source incorrectly received identity cleanup: " + control);
        }
        if (replay.HasValue)
        {
            Native(replay.Value, repairedPrediction: true);
            NativeBranchObservationChecks.Pair(fixture, replay.Value, originalPredictionPresent: true);
        }
        Console.WriteLine("WOUNDED-HORDE-PREVIEW-IDENTITY PASS: 21 native inputs/full state/RNG, identity, four immutable branches and source controls; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

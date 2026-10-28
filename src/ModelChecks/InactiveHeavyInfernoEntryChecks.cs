using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InactiveHeavyInfernoEntryChecks
{
    private const string Reason = "Inactive enemy room-entry moon understanding requires native calibration.";
    internal static void Native(FixtureValue fixture, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        Require(rows.Length == 20 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing native prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        foreach (var (id, asset) in new[] { (18, "HeavyT3_Basic"), (19, "HeavyT3_PyreDamageIfUnblocked"), (20, "AttackerT3_Sweep") })
        {
            var old = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == id);
            var next = actual.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == id);
            Require(old.AssetKey == asset && old.MoonPhaseMask == 5 && next.MoonPhaseMask == 6 &&
                next.Modifiers!.Upgrades.Any(upgrade => upgrade.AssetKey == "InfernoRoom"), "Missing native room-entry witness: " + asset);
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Room entry differs: " + result.UnsupportedReason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "EndTurn mutated its parent.");
        foreach (string control in new[] { "HeavyT3_Basic", "HeavyT3_PyreDamageIfUnblocked", "AttackerT3_Sweep", "InfernoRoom" })
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
            Require(!BattleTurnModel.EndTurn(altered.Deserialize<BattleTurnState>(ModelJson.Options)!).Supported,
                "Unknown entry signature escaped its guard: " + control);
        }
        if (replay.HasValue) { Native(replay.Value); NativeBranchObservationChecks.Pair(fixture, replay.Value); }
        Console.WriteLine("INACTIVE-HEAVY-INFERNO-ENTRY PASS: 20 native inputs/full state/RNG, three enemy masks 5->6, four immutable branches and four signature guards; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

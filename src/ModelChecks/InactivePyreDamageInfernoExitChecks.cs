using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InactivePyreDamageInfernoExitChecks
{
    private const string Reason = "Inactive enemy room-exit moon understanding requires native calibration.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason, boundaryOutcome: RoomOutcome.PlayerDefeated);
        Require(rows.Length == 36 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing 36-input native chain.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        foreach (int id in new[] { 37, 38 })
        {
            var origin = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == id);
            var native = actual.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == id);
            Require(origin.AssetKey == "HeavyT3_PyreDamageIfUnblocked" && origin.MoonPhaseMask == 5 &&
                native.MoonPhaseMask == 6 && origin.Modifiers!.Upgrades.Any(upgrade => upgrade.AssetKey == "InfernoRoom") &&
                native.Modifiers!.Upgrades.All(upgrade => upgrade.AssetKey != "InfernoRoom"), "Missing native phase/room-exit witness.");
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && result.Outcome == RoomOutcome.PlayerDefeated &&
                NativeBranchObservationChecks.Key(result.State!, true) == NativeBranchObservationChecks.Key(actual, true),
                "Native exit differs: " + result.UnsupportedReason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "EndTurn mutated its parent.");
        foreach (string control in new[] { "Identity", "Payload", "Room" })
        {
            JsonNode node = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            void Rewrite(JsonNode? value)
            {
                if (value is JsonObject obj)
                {
                    if (obj["AssetKey"]?.GetValueKind() == JsonValueKind.String &&
                        obj["AssetKey"]!.GetValue<string>() == "HeavyT3_PyreDamageIfUnblocked" && obj["Id"]?.GetValue<int>() == 37)
                    {
                        if (control == "Identity") obj["AssetKey"] = "unknown";
                        if (control == "Payload") obj["Triggers"]![0]!["Effects"]![0]!["Value"] = 6;
                    }
                    if (control == "Room" && obj["AssetKey"]?.GetValueKind() == JsonValueKind.String &&
                        obj["AssetKey"]!.GetValue<string>() == "InfernoRoom" && obj["DataId"]?.GetValueKind() == JsonValueKind.String &&
                        obj["DataId"]!.GetValue<string>() == "378cea0c-14e9-45fb-9918-fb853bd04a7d") obj["DataId"] = "unknown";
                    foreach (var entry in obj.ToArray()) Rewrite(entry.Value);
                }
                else if (value is JsonArray array) foreach (var child in array) Rewrite(child);
            }
            Rewrite(node);
            Require(!BattleTurnModel.EndTurn(node.Deserialize<BattleTurnState>(ModelJson.Options)!).Supported,
                "Unknown enemy or room escaped its guard: " + control);
        }
        if (replay.HasValue) { Native(replay.Value); NativeBranchObservationChecks.Pair(source, replay.Value); }
        Console.WriteLine("INACTIVE-PYRE-DAMAGE-INFERNO-EXIT PASS: 36 native inputs/full state/RNG, two masks5->6, four immutable branches/three guards; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

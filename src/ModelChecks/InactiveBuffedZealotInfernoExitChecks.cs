using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InactiveBuffedZealotInfernoExitChecks
{
    private const string Reason = "Inactive enemy room-exit moon understanding requires native calibration.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason, boundaryOutcome: RoomOutcome.PlayerDefeated);
        Require(rows.Length == 45 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing 45-input native chain.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var origin = before.Spawn.Train.Rooms[2].Units.Single(unit => unit.Id == 30);
        var native = actual.Spawn.Train.Rooms[3].Units.Single(unit => unit.Id == 30);
        Require(origin is { AssetKey: "HeavyT2_Zealot", BaseAttack: 11, MoonPhaseMask: 5 } &&
            native.BaseAttack == 11 && native.MoonPhaseMask == 6 &&
            origin.Modifiers!.Upgrades.Any(upgrade => upgrade.AssetKey == "InfernoRoom") &&
            native.Modifiers!.Upgrades.All(upgrade => upgrade.AssetKey != "InfernoRoom"), "Missing buffed Zealot exit witness.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && result.Outcome == RoomOutcome.PlayerDefeated &&
                NativeBranchObservationChecks.Key(result.State!, true) == NativeBranchObservationChecks.Key(actual, true),
                "Native buffed Zealot exit differs: " + result.UnsupportedReason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "EndTurn mutated its parent.");
        foreach (string control in new[] { "Identity", "Attack", "Payload" })
        {
            JsonNode node = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            void Rewrite(JsonNode? value)
            {
                if (value is JsonObject obj)
                {
                    if (obj["AssetKey"]?.GetValueKind() == JsonValueKind.String &&
                        obj["AssetKey"]!.GetValue<string>() == "HeavyT2_Zealot" && obj["Id"]?.GetValue<int>() == 30)
                    {
                        if (control == "Identity") obj["AssetKey"] = "unknown";
                        if (control == "Attack") obj["BaseAttack"] = 12;
                        if (control == "Payload") obj["Triggers"]![0]!["Effects"]![0]!["Value"] = 2;
                    }
                    foreach (var entry in obj.ToArray()) Rewrite(entry.Value);
                }
                else if (value is JsonArray array) foreach (var child in array) Rewrite(child);
            }
            Rewrite(node);
            Require(!BattleTurnModel.EndTurn(node.Deserialize<BattleTurnState>(ModelJson.Options)!).Supported,
                "Unknown buffed Zealot escaped its guard: " + control);
        }
        if (replay.HasValue) { Native(replay.Value); NativeBranchObservationChecks.Pair(source, replay.Value); }
        Console.WriteLine("INACTIVE-BUFFED-ZEALOT-INFERNO-EXIT PASS: 45 native inputs/full state/RNG, attack11 and mask5->6, four immutable branches/three guards; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

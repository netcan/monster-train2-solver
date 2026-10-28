using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InactiveMultistrikeZealotInfernoExitChecks
{
    private const string Reason = "Inactive enemy room-exit moon understanding requires native calibration.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason, boundaryOutcome: RoomOutcome.PlayerDefeated);
        Require(rows.Length == 49 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing native chain.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var origin = before.Spawn.Train.Rooms[2].Units.Single(unit => unit.Id == 31);
        var native = actual.Spawn.Train.Rooms[3].Units.Single(unit => unit.Id == 31);
        Require(origin is { AssetKey: "HeavyT2_Zealot", BaseAttack: 7, MoonPhaseMask: 5,
            HordeDefinition: { Attack: 3, Health: 110 }, Modifiers.DamageBuff: 4 } && origin.Status("multistrike")?.Stacks == 1 &&
            native.BaseAttack == 7 && native.MoonPhaseMask == 6 &&
            origin.Modifiers!.Upgrades.Any(upgrade => upgrade.AssetKey == "InfernoRoom") &&
            native.Modifiers!.Upgrades.All(upgrade => upgrade.AssetKey != "InfernoRoom"), "Missing Zealot exit witness.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && result.Outcome == RoomOutcome.PlayerDefeated &&
                NativeBranchObservationChecks.Key(result.State!, true) == NativeBranchObservationChecks.Key(actual, true),
                "Native exit differs: " + result.UnsupportedReason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "EndTurn mutated its parent.");
        foreach (string control in new[] { "Identity", "Attack", "DamageBuff", "Horde", "Payload" })
        {
            JsonNode node = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            void Rewrite(JsonNode? value)
            {
                if (value is JsonObject obj)
                {
                    if (obj["AssetKey"]?.GetValueKind() == JsonValueKind.String &&
                        obj["AssetKey"]!.GetValue<string>() == "HeavyT2_Zealot" && obj["Id"]?.GetValue<int>() == 31)
                    {
                        if (control == "Identity") obj["AssetKey"] = "unknown";
                        if (control == "Attack") obj["BaseAttack"] = 8;
                        if (control == "DamageBuff") obj["Modifiers"]!["DamageBuff"] = 3;
                        if (control == "Horde") obj["HordeDefinition"]!["Attack"] = 4;
                        if (control == "Payload") obj["Triggers"]![0]!["Effects"]![0]!["Value"] = 2;
                    }
                    foreach (var entry in obj.ToArray()) Rewrite(entry.Value);
                }
                else if (value is JsonArray array) foreach (var child in array) Rewrite(child);
            }
            Rewrite(node);
            Require(!BattleTurnModel.EndTurn(node.Deserialize<BattleTurnState>(ModelJson.Options)!).Supported,
                "Unknown Zealot escaped its guard: " + control);
        }
        if (replay.HasValue) { Native(replay.Value); NativeBranchObservationChecks.Pair(source, replay.Value); }
        Console.WriteLine("INACTIVE-MULTISTRIKE-ZEALOT-INFERNO-EXIT PASS: 49 native inputs/full state/RNG, mask5->6, four immutable branches/five guards; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

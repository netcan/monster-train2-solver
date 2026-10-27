using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InactiveFullMoonInfernoExitChecks
{
    internal static void Native(FixtureValue source)
    {
        TrainCombatChecks.Native(source);
        var actions = source.GetProperty("Actions").EnumerateArray().ToArray();
        Require(actions.Length == 28, "Expected 28 original card actions.");
        foreach (var row in actions)
        {
            var result = BattleActionModel.PlayCard(row.GetProperty("Before").Deserialize<BattleTurnState>()!,
                row.GetProperty("Action").Deserialize<PlayCardAction>()!);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) ==
                NativeBranchObservationChecks.Key(row.GetProperty("Actual").Deserialize<BattleTurnState>()!), "Original card action differs: " + result.Reason);
        }
        var turns = source.GetProperty("Turns").EnumerateArray().ToArray();
        Require(turns.Length == 6, "Expected six original turn records.");
        for (int index = 0; index < turns.Length; index++)
        {
            var before = turns[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
            var actual = turns[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var old = turns[index].GetProperty("Predicted").GetProperty("State").Deserialize<BattleTurnState>()!;
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Native turn differs at " + index + ": " + (result.Supported ? ModelJson.Difference(NativeBranchObservationChecks.Key(result.State!), NativeBranchObservationChecks.Key(actual)) : result.UnsupportedReason));
            if (index < 5) Require(NativeBranchObservationChecks.Key(old) == NativeBranchObservationChecks.Key(actual), "Earlier historical turn changed.");
            else
            {
                foreach (int id in new[] { 28, 29 })
                {
                    var origin = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == id);
                    var prior = old.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == id);
                    var native = actual.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == id);
                    Require(origin.MoonPhaseMask == 5 && prior.MoonPhaseMask == 5 && native.MoonPhaseMask == 6 &&
                        origin.Modifiers!.Upgrades.Any(upgrade => upgrade.AssetKey == "InfernoRoom") &&
                        native.Modifiers!.Upgrades.All(upgrade => upgrade.AssetKey != "InfernoRoom"), "Missing native full-moon room-exit witness.");
                }
                JsonNode historical = JsonSerializer.SerializeToNode(old, ModelJson.Options)!;
                int corrected = 0;
                void Correct(JsonNode? node)
                {
                    if (node is JsonObject obj)
                    {
                        if (obj.ContainsKey("BaseAttack") && obj["Id"]?.GetValue<int>() is 28 or 29 && obj["MoonPhaseMask"]?.GetValue<int>() == 5)
                        { obj["MoonPhaseMask"] = 6; corrected++; }
                        foreach (var entry in obj.ToArray()) Correct(entry.Value);
                    }
                    else if (node is JsonArray array) foreach (var child in array) Correct(child);
                }
                Correct(historical);
                Require(corrected >= 2 && NativeBranchObservationChecks.Key(historical.Deserialize<BattleTurnState>(ModelJson.Options)!) ==
                    NativeBranchObservationChecks.Key(actual), "Historical difference extends beyond the native masks.");
                string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
                Parallel.For(0, 4, _ => Require(NativeBranchObservationChecks.Key(BattleTurnModel.EndTurn(before).State!) ==
                    NativeBranchObservationChecks.Key(actual), "Parallel EndTurn differs."));
                Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "EndTurn mutated parent.");
                foreach (string control in new[] { "ZealotIdentity", "ZealotPayload", "RoomIdentity" })
                {
                    JsonNode altered = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
                    void Rewrite(JsonNode? node)
                    {
                        if (node is JsonObject obj)
                        {
                            if (obj["AssetKey"]?.GetValueKind() == JsonValueKind.String && obj["AssetKey"]!.GetValue<string>() == "HeavyT2_Zealot" &&
                                obj.ContainsKey("BaseAttack") && obj["Id"]?.GetValue<int>() == 29)
                            {
                                if (control == "ZealotIdentity") obj["AssetKey"] = "unknown";
                                if (control == "ZealotPayload" && obj["Triggers"] is JsonArray { Count: > 0 } triggers)
                                    triggers[0]!["Effects"]![0]!["Value"] = 2;
                            }
                            if (control == "RoomIdentity" && obj["AssetKey"]?.GetValueKind() == JsonValueKind.String &&
                                obj["AssetKey"]!.GetValue<string>() == "InfernoRoom" && obj["DataId"]?.GetValueKind() == JsonValueKind.String &&
                                obj["DataId"]!.GetValue<string>() == "378cea0c-14e9-45fb-9918-fb853bd04a7d") obj["DataId"] = "unknown";
                            foreach (var entry in obj.ToArray()) Rewrite(entry.Value);
                        }
                        else if (node is JsonArray array) foreach (var child in array) Rewrite(child);
                    }
                    Rewrite(altered);
                    Require(!BattleTurnModel.EndTurn(altered.Deserialize<BattleTurnState>(ModelJson.Options)!).Supported,
                        "Unknown full-moon room exit signature escaped its guard: " + control);
                }
            }
        }
        Console.WriteLine("INACTIVE-FULLMOON-INFERNO-EXIT PASS: 28 actions/six turns and 12 train phases/full state/forecast/RNG, two native masks5->6, four immutable branches/three unknown guards; nested exchange remains separate.");
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

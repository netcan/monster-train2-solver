using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InactiveFragileInfernoExitChecks
{
    internal static void Native(FixtureValue source)
    {
        var actions = source.GetProperty("Actions").EnumerateArray().ToArray();
        Require(actions.Length == 15, "Expected fifteen original card actions.");
        foreach (var row in actions)
        {
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var action = row.GetProperty("Action").Deserialize<PlayCardAction>()!;
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Original card action differs: " + result.Reason);
        }
        var turns = source.GetProperty("Turns").EnumerateArray().ToArray();
        Require(turns.Length == 4, "Expected four original turn records.");
        for (int index = 0; index < turns.Length; index++)
        {
            var before = turns[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
            var actual = turns[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var predicted = turns[index].GetProperty("Predicted").GetProperty("State").Deserialize<BattleTurnState>()!;
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Native turn differs at " + index + ": " + (result.Supported ? ModelJson.Difference(NativeBranchObservationChecks.Key(result.State!), NativeBranchObservationChecks.Key(actual)) : result.UnsupportedReason));
            if (index < 3) Require(NativeBranchObservationChecks.Key(predicted) == NativeBranchObservationChecks.Key(actual), "Earlier historical turn changed.");
            else
            {
                var bossBefore = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.AssetKey == "Boss_Level8_Fragile");
                var bossActual = actual.Spawn.Train.Rooms[2].Units.Single(unit => unit.Id == bossBefore.Id);
                var bossOld = predicted.Spawn.Train.Rooms[2].Units.Single(unit => unit.Id == bossBefore.Id);
                Require(before.MoonPhaseTrackingActive == false && actual.MoonPhaseTrackingActive == false &&
                    bossBefore.MoonPhaseMask == 6 && bossOld.MoonPhaseMask == 6 && bossActual.MoonPhaseMask == 5 &&
                    bossBefore.Modifiers!.Upgrades.Any(upgrade => upgrade.AssetKey == "InfernoRoom") &&
                    bossActual.Modifiers!.Upgrades.All(upgrade => upgrade.AssetKey != "InfernoRoom"), "Missing native boss room exit and mask witness.");
                JsonNode historical = JsonSerializer.SerializeToNode(predicted, ModelJson.Options)!;
                int corrected = 0;
                void Correct(JsonNode? node)
                {
                    if (node is JsonObject obj)
                    {
                        if (obj.ContainsKey("BaseAttack") && obj["Id"]?.GetValue<int>() == bossBefore.Id &&
                            obj["AssetKey"]?.GetValue<string>() == "Boss_Level8_Fragile" && obj["MoonPhaseMask"]?.GetValue<int>() == 6)
                        { obj["MoonPhaseMask"] = 5; corrected++; }
                        foreach (var entry in obj.ToArray()) Correct(entry.Value);
                    }
                    else if (node is JsonArray array) foreach (var child in array) Correct(child);
                }
                Correct(historical);
                Require(corrected > 0 && NativeBranchObservationChecks.Key(historical.Deserialize<BattleTurnState>(ModelJson.Options)!) ==
                    NativeBranchObservationChecks.Key(actual), "Historical difference extends beyond the native boss mask.");
                string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
                Parallel.For(0, 4, _ => Require(NativeBranchObservationChecks.Key(BattleTurnModel.EndTurn(before).State!) ==
                    NativeBranchObservationChecks.Key(actual), "Parallel EndTurn differs."));
                Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "EndTurn mutated parent.");
                foreach (string control in new[] { "Identity", "UpgradeHealth", "MoonCallback" })
                {
                    JsonNode altered = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
                    void Rewrite(JsonNode? node)
                    {
                        if (node is JsonObject obj)
                        {
                            if (obj["AssetKey"]?.GetValueKind() == JsonValueKind.String && obj["AssetKey"]!.GetValue<string>() == "InfernoRoom")
                            {
                                if (control == "Identity" && obj["DataId"]?.GetValueKind() == JsonValueKind.String &&
                                    obj["DataId"]!.GetValue<string>() == "378cea0c-14e9-45fb-9918-fb853bd04a7d") obj["DataId"] = "unknown";
                                if (control == "UpgradeHealth" && obj["Stats"] is JsonObject stats) stats["Health"] = 1;
                            }
                            if (control == "MoonCallback" && obj["AssetKey"]?.GetValueKind() == JsonValueKind.String &&
                                obj["AssetKey"]!.GetValue<string>() == "Boss_Level8_Fragile" && obj["Triggers"] is JsonArray { Count: > 0 } triggers)
                                triggers[0]!["Kind"] = "OnMoonLit";
                            foreach (var entry in obj.ToArray()) Rewrite(entry.Value);
                        }
                        else if (node is JsonArray array) foreach (var child in array) Rewrite(child);
                    }
                    Rewrite(altered);
                    Require(!BattleTurnModel.EndTurn(altered.Deserialize<BattleTurnState>(ModelJson.Options)!).Supported,
                        "Unknown room exit signature escaped its guard: " + control);
                }
            }
        }
        Console.WriteLine("INACTIVE-FRAGILE-INFERNO-EXIT PASS: fifteen actions/four native turns/full state/forecast/RNG, original boss mask6->5, four immutable branches/three unknown guards.");
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

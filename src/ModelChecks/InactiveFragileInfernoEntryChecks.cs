using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InactiveFragileInfernoEntryChecks
{
    internal static void Native(FixtureValue source, FixtureValue? replay = null, string? sourceHash = null, bool newMoonEntry = false)
    {
        TrainCombatChecks.Native(source);
        var actions = source.GetProperty("Actions").EnumerateArray().ToArray();
        Require(actions.Length == (newMoonEntry ? 34 : 28), "Unexpected original card actions.");
        foreach (var row in actions)
        {
            var result = BattleActionModel.PlayCard(row.GetProperty("Before").Deserialize<BattleTurnState>()!,
                row.GetProperty("Action").Deserialize<PlayCardAction>()!);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) ==
                NativeBranchObservationChecks.Key(row.GetProperty("Actual").Deserialize<BattleTurnState>()!), "Original card action differs: " + result.Reason);
        }
        var turns = source.GetProperty("Turns").EnumerateArray().ToArray();
        Require(turns.Length == (newMoonEntry ? 8 : 7), "Unexpected original turn records.");
        for (int index = 0; index < turns.Length; index++)
        {
            var before = turns[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
            var actual = turns[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var old = turns[index].GetProperty("Predicted").GetProperty("State").Deserialize<BattleTurnState>()!;
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Native turn differs at " + index + ": " + (result.Supported ? ModelJson.Difference(NativeBranchObservationChecks.Key(result.State!), NativeBranchObservationChecks.Key(actual)) : result.UnsupportedReason));
            if (index < turns.Length - 1) Require(NativeBranchObservationChecks.Key(old) == NativeBranchObservationChecks.Key(actual), "Earlier historical turn changed.");
            else
            {
                var origin = before.Spawn.Train.Rooms[newMoonEntry ? 1 : 2].Units.Single(unit => unit.Id == 11);
                var prior = old.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 11);
                var native = actual.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 11);
                int oldMask = newMoonEntry ? 6 : 5;
                int nativeMask = newMoonEntry ? 5 : 6;
                Require(origin.AssetKey == "Boss_Level8_Fragile" && origin.MoonPhaseMask == oldMask && prior.MoonPhaseMask == oldMask && native.MoonPhaseMask == nativeMask &&
                    origin.Modifiers!.Upgrades.All(upgrade => upgrade.AssetKey != "InfernoRoom") &&
                    native.Modifiers!.Upgrades.Any(upgrade => upgrade.AssetKey == "InfernoRoom"), "Missing native boss room-entry witness.");
                JsonNode historical = JsonSerializer.SerializeToNode(old, ModelJson.Options)!;
                int corrected = 0;
                void Correct(JsonNode? node)
                {
                    if (node is JsonObject obj)
                    {
                        if (obj.ContainsKey("BaseAttack") && obj["Id"]?.GetValue<int>() == 11 && obj["MoonPhaseMask"]?.GetValue<int>() == oldMask)
                        { obj["MoonPhaseMask"] = nativeMask; corrected++; }
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
                foreach (string control in new[] { "BossIdentity", "RoomIdentity", "UpgradeHealth" })
                {
                    JsonNode altered = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
                    void Rewrite(JsonNode? node)
                    {
                        if (node is JsonObject obj)
                        {
                            if (control == "BossIdentity" && obj["AssetKey"]?.GetValueKind() == JsonValueKind.String &&
                                obj["AssetKey"]!.GetValue<string>() == "Boss_Level8_Fragile") obj["AssetKey"] = "unknown";
                            if (obj["AssetKey"]?.GetValueKind() == JsonValueKind.String && obj["AssetKey"]!.GetValue<string>() == "InfernoRoom")
                            {
                                if (control == "RoomIdentity" && obj["DataId"]?.GetValueKind() == JsonValueKind.String &&
                                    obj["DataId"]!.GetValue<string>() == "378cea0c-14e9-45fb-9918-fb853bd04a7d") obj["DataId"] = "unknown";
                                if (control == "UpgradeHealth" && obj["Stats"] is JsonObject stats) stats["Health"] = 1;
                            }
                            foreach (var entry in obj.ToArray()) Rewrite(entry.Value);
                        }
                        else if (node is JsonArray array) foreach (var child in array) Rewrite(child);
                    }
                    Rewrite(altered);
                    Require(!BattleTurnModel.EndTurn(altered.Deserialize<BattleTurnState>(ModelJson.Options)!).Supported,
                        "Unknown room-entry signature escaped its guard: " + control);
                }
            }
        }
        if (replay.HasValue)
        {
            string reason = newMoonEntry ? "Native inactive Fragile Inferno entry new-moon mask differs on the eighth turn." :
                "Native inactive Fragile Inferno entry moon mask differs on the seventh turn.";
            var rows = NativeBranchObservationChecks.Replay(replay.Value, reason);
            var decisions = source.GetProperty("RollingDecisions").EnumerateArray().ToArray();
            Require(rows.Length == (newMoonEntry ? 42 : 35) && decisions.Length == rows.Length &&
                replay.Value.GetProperty("Request").GetProperty("SourceSHA256").GetString() == sourceHash,
                "Independent native replay lost its original source or input count.");
            for (int index = 0; index < rows.Length; index++)
            {
                var row = rows[index]; var decision = decisions[index];
                Require(row.GetProperty("Action").ContentEquals(decision.GetProperty("Action")) &&
                    row.GetProperty("IsEndTurn").GetBoolean() == decision.GetProperty("IsEndTurn").GetBoolean() &&
                    NativeBranchObservationChecks.Key(row.GetProperty("Before").Deserialize<BattleTurnState>()!) ==
                        NativeBranchObservationChecks.Key(decision.GetProperty("Root").Deserialize<BattleTurnState>()!) &&
                    NativeBranchObservationChecks.Key(row.GetProperty("Actual").Deserialize<BattleTurnState>()!) ==
                        NativeBranchObservationChecks.Key(decision.GetProperty("Actual").Deserialize<BattleTurnState>()!),
                    "Independent native input/state/RNG differs at " + index);
            }
        }
        Console.WriteLine($"INACTIVE-FRAGILE-INFERNO-ENTRY PASS: {actions.Length} actions/{turns.Length} turns/train phases/full state/forecast/RNG, boss mask{(newMoonEntry ? "6->5" : "5->6")}, four immutable branches/three unknown guards; nested exchange remains separate; independentPair={replay.HasValue}");
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

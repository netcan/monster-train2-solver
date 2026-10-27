using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InactiveHeavyInfernoExitChecks
{
    internal static void Native(FixtureValue source, FixtureValue? replay = null, string? sourceHash = null)
    {
        foreach (var phase in source.GetProperty("TrainPhases").EnumerateArray())
        {
            var beforePhase = phase.GetProperty("Before").Deserialize<TrainCombatState>()!;
            var phaseResult = phase.GetProperty("Kind").GetString() == "Combat" ? TrainCombatModel.ResolveCombat(beforePhase) : TrainCombatModel.Ascend(beforePhase);
            Require(phaseResult.Supported, "Native train phase " + phase.GetProperty("Index").GetInt32() + " rejected: " + phaseResult.UnsupportedReason);
        }
        TrainCombatChecks.Native(source);
        var actions = source.GetProperty("Actions").EnumerateArray().ToArray();
        Require(actions.Length == 21, "Expected twenty-one original card actions.");
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
        Require(turns.Length == 5, "Expected five original turn records.");
        for (int index = 0; index < turns.Length; index++)
        {
            var before = turns[index].GetProperty("Before").Deserialize<BattleTurnState>()!;
            var actual = turns[index].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            var old = turns[index].GetProperty("Predicted").GetProperty("State").Deserialize<BattleTurnState>()!;
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Native turn differs at " + index + ": " + (result.Supported ? ModelJson.Difference(NativeBranchObservationChecks.Key(result.State!), NativeBranchObservationChecks.Key(actual)) : result.UnsupportedReason));
            if (index < 4) Require(NativeBranchObservationChecks.Key(old) == NativeBranchObservationChecks.Key(actual), "Earlier historical turn changed.");
            else
            {
                Require(before.MoonPhaseTrackingActive == false && actual.MoonPhaseTrackingActive == false, "Moon tracking changed.");
                foreach (int id in new[] { 21, 22 })
                {
                    var origin = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == id);
                    var prior = old.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == id);
                    var native = actual.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == id);
                    Require(origin.AssetKey == "HeavyT3_Basic" && origin.MoonPhaseMask == 6 && prior.MoonPhaseMask == 6 &&
                        native.MoonPhaseMask == 5 && origin.Modifiers!.Upgrades.Any(upgrade => upgrade.AssetKey == "InfernoRoom") &&
                        native.Modifiers!.Upgrades.All(upgrade => upgrade.AssetKey != "InfernoRoom"), "Missing native heavy room-exit witness.");
                }
                JsonNode historical = JsonSerializer.SerializeToNode(old, ModelJson.Options)!;
                int corrected = 0;
                void Correct(JsonNode? node)
                {
                    if (node is JsonObject obj)
                    {
                        if (obj.ContainsKey("BaseAttack") && obj["Id"]?.GetValue<int>() is 21 or 22 &&
                            obj["AssetKey"]?.GetValue<string>() == "HeavyT3_Basic" && obj["MoonPhaseMask"]?.GetValue<int>() == 6)
                        { obj["MoonPhaseMask"] = 5; corrected++; }
                        foreach (var entry in obj.ToArray()) Correct(entry.Value);
                    }
                    else if (node is JsonArray array) foreach (var child in array) Correct(child);
                }
                Correct(historical);
                Require(corrected >= 2 && NativeBranchObservationChecks.Key(historical.Deserialize<BattleTurnState>(ModelJson.Options)!) ==
                    NativeBranchObservationChecks.Key(actual), "Historical difference extends beyond the two native heavy masks.");
                string immutable = JsonSerializer.Serialize(before, ModelJson.Options);
                Parallel.For(0, 4, _ => Require(NativeBranchObservationChecks.Key(BattleTurnModel.EndTurn(before).State!) ==
                    NativeBranchObservationChecks.Key(actual), "Parallel EndTurn differs."));
                Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "EndTurn mutated parent.");
                foreach (string control in new[] { "Enemy", "EnemyAttack", "RoomIdentity", "UpgradeHealth" })
                {
                    JsonNode altered = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
                    void Rewrite(JsonNode? node)
                    {
                        if (node is JsonObject obj)
                        {
                            if (obj["AssetKey"]?.GetValueKind() == JsonValueKind.String && obj["AssetKey"]!.GetValue<string>() == "HeavyT3_Basic" &&
                                obj.ContainsKey("BaseAttack") && obj["Id"]?.GetValue<int>() is 21 or 22)
                            {
                                if (control == "Enemy") obj["AssetKey"] = "unknown";
                                if (control == "EnemyAttack") obj["BaseAttack"] = 9;
                            }
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
                        "Unknown room exit signature escaped its guard: " + control);
                }
            }
        }
        if (replay.HasValue)
        {
            const string reason = "Native inactive heavy Inferno exit moon masks differ on the fifth turn.";
            var rows = NativeBranchObservationChecks.Replay(replay.Value, reason);
            var decisions = source.GetProperty("RollingDecisions").EnumerateArray().ToArray();
            Require(rows.Length == 26 && decisions.Length == 26 &&
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
        Console.WriteLine("INACTIVE-HEAVY-INFERNO-EXIT PASS: 21 actions/five native turns/full state/forecast/RNG, two original heavy mask6->5, four immutable branches/four unknown guards; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

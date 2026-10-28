using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class TripleHordeRetiredCombatChecks
{
    private const string Reason = "Train combat floor 2: Combined troop upgrades and enchantment source ordering require native calibration.";

    internal static void Native(FixtureValue original, FixtureValue replay, FixtureValue phases,
        FixtureValue? independent = null, FixtureValue? second = null,
        FixtureValue? independentPhase = null, FixtureValue? secondPhase = null)
    {
        var rows = NativeBranchObservationChecks.Replay(original, Reason);
        Require(rows.Length == 21 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing original native prefix.");
        NativeBranchObservationChecks.Replay(replay, Reason);
        NativeBranchObservationChecks.Pair(original, replay);
        if (independent.HasValue)
        {
            NativeBranchObservationChecks.Replay(independent.Value, Reason);
            NativeBranchObservationChecks.Pair(original, independent.Value);
        }
        if (second.HasValue)
        {
            Require(independent.HasValue, "A second independent replay requires the first.");
            NativeBranchObservationChecks.Replay(second.Value, Reason);
            NativeBranchObservationChecks.Pair(independent!.Value, second.Value, originalPredictionPresent: true, originalPredictionMatched: true);
        }
        DualTraceHordeChecks.DiagnosePhase(phases);
        if (independentPhase.HasValue && secondPhase.HasValue)
        {
            DualTraceHordeChecks.DiagnosePhase(independentPhase.Value);
            DualTraceHordeChecks.DiagnosePhase(secondPhase.Value);
        }
        else Require(!independentPhase.HasValue && !secondPhase.HasValue, "Both independent phases are required.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && result.Outcome == RoomOutcome.Exchanged &&
                NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Retired combat birth differs: " + result.UnsupportedReason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "EndTurn mutated its parent.");
        foreach (string control in new[] { "ChampionHealth", "Sycophant", "Recruit", "Whelp", "RetiredSource" })
        {
            JsonNode changed = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            int targetId = control switch { "ChampionHealth" => 2, "Sycophant" => 5, "Recruit" => 21, "Whelp" => 22, _ => 15 };
            int replaced = 0;
            void Rewrite(JsonNode? node)
            {
                if (node is JsonObject obj)
                {
                    if (obj["Id"] is JsonValue id && id.TryGetValue(out int value) && value == targetId && obj["AssetKey"] is JsonValue)
                    {
                        if (control == "ChampionHealth") obj["Health"] = obj["Health"]!.GetValue<int>() - 1;
                        else if (control == "RetiredSource") obj["AssetKey"] = "unknown";
                        else obj["BaseAttack"] = obj["BaseAttack"]!.GetValue<int>() + 1;
                        replaced++;
                    }
                    foreach (var entry in obj.ToArray()) Rewrite(entry.Value);
                }
                else if (node is JsonArray array) foreach (var child in array) Rewrite(child);
            }
            Rewrite(changed);
            Require(replaced > 0, "Missing mutated unit: " + control);
            Require(!BattleTurnModel.EndTurn(changed.Deserialize<BattleTurnState>(ModelJson.Options)!).Supported,
                "Uncalibrated source escaped its guard: " + control);
        }
        Console.WriteLine("TRIPLE-HORDE-RETIRED-COMBAT PASS: 20 native prefix inputs/full successor and RNG, phase comparison, four immutable branches and five guards; newPair=" + second.HasValue);
    }

    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

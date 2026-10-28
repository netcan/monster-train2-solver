using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class LavaRetiredRecruitsPreviewChecks
{
    private const string Reason = "Battle preview floor 1: Combined troop upgrades and enchantment source ordering require native calibration.";

    internal static void Native(FixtureValue fixture, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        Require(rows.Length == 21 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing native chain.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(before.Spawn.Turn == 3 && actual.Spawn.Turn == 4 && actual.Spawn.Train.Context!.NextUnitId == 26,
            "Missing original preview identity boundary.");
        var writes = rows[^1].GetProperty("TroopUpdates").EnumerateArray().Where(update =>
            update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            update.GetProperty("Before").GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == 26)).ToArray();
        Require(writes.Length == 5, "Expected five native preview writes.");
        int[] healthBefore = [12, 21, 30, 39, 48], healthAfter = [21, 30, 39, 48, 57];
        for (int i = 0; i < writes.Length; i++)
        {
            var b = writes[i].GetProperty("Before"); var a = writes[i].GetProperty("After");
            var bu = b.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 26);
            var au = a.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 26);
            Require(b.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 6]) &&
                a.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 6]) &&
                bu.GetProperty("Health").GetInt32() == healthBefore[i] && au.GetProperty("Health").GetInt32() == healthAfter[i] &&
                bu.GetProperty("Upgrades").GetArrayLength() == i * 3 && au.GetProperty("Upgrades").GetArrayLength() == (i + 1) * 3,
                "Native preview write differs at " + i);
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Concurrent successor differs: " + result.UnsupportedReason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "EndTurn mutated its parent.");
        foreach (string control in new[] { "MonsterChampionUnderlegion1", "Boss_Level8_Fragile", "EnchanterT2_Titanskin", "MonsterSycophant", "LavaArmor", "MonsterGenerousHost", "MonsterSneezyWhelp" })
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
            Require(!result.Supported || NativeBranchObservationChecks.Key(result.State!) != NativeBranchObservationChecks.Key(actual),
                "Unknown source incorrectly received native result: " + control);
        }
        JsonNode binding = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
        int changedBindings = 0;
        void ChangeBinding(JsonNode? node)
        {
            if (node is JsonObject obj)
            {
                if (obj["DataId"]?.GetValueKind() == JsonValueKind.String &&
                    obj["DataId"]!.GetValue<string>() == "924ad6ab-3d60-4760-b38e-78f5cf09c1d5" &&
                    obj["EquippedUnitId"]?.GetValueKind() == JsonValueKind.Number && obj["EquippedUnitId"]!.GetValue<int>() == 6)
                { obj["EquippedUnitId"] = 999; changedBindings++; }
                foreach (var entry in obj.ToArray()) ChangeBinding(entry.Value);
            }
            else if (node is JsonArray array) foreach (var child in array) ChangeBinding(child);
        }
        ChangeBinding(binding);
        var unbound = BattleTurnModel.EndTurn(binding.Deserialize<BattleTurnState>(ModelJson.Options)!);
        Require(changedBindings > 0 && (!unbound.Supported || NativeBranchObservationChecks.Key(unbound.State!) != NativeBranchObservationChecks.Key(actual)),
            "Wrong equipment binding received the native result.");
        if (replay.HasValue) { Native(replay.Value); NativeBranchObservationChecks.Pair(fixture, replay.Value); }
        Console.WriteLine("LAVA-RETIRED-RECRUITS-PREVIEW PASS: 21 native inputs/full state/RNG, five writes, four immutable branches and source/binding controls; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class LavaSixWritesChecks
{
    internal static void Native(FixtureValue fixture, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture,
            "Battle preview floor 0: Combined troop upgrades and enchantment source ordering require native calibration.");
        Require(rows.Length == 25 && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Wrong native input count.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var writes = rows[^1].GetProperty("TroopUpdates").EnumerateArray().Where(update =>
            update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            update.GetProperty("Before").GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == 29)).ToArray();
        Require(writes.Length == 6, "Expected six native preview writes.");
        for (int i = 0; i < writes.Length; i++)
        {
            var b = writes[i].GetProperty("Before"); var a = writes[i].GetProperty("After");
            var bu = b.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 29);
            var au = a.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 29);
            Require(b.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 3, 27]) &&
                a.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 3, 27]) &&
                bu.GetProperty("Health").GetInt32() == 12 + i * 9 && au.GetProperty("Health").GetInt32() == 21 + i * 9 &&
                bu.GetProperty("Upgrades").GetArrayLength() == i * 3 && au.GetProperty("Upgrades").GetArrayLength() == (i + 1) * 3,
                "Native preview write differs at " + i);
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Concurrent successor differs: " + result.Reason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Card play mutated its parent.");
        foreach (string control in new[] { "MonsterChampionUnderlegion1", "Boss_Level8_Fragile", "EnchanterT2_Titanskin", "MonsterSycophant", "LavaArmor", "MonsterEnthusiasticFan", "MonsterGoldyWhelp" })
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
            var result = BattleActionModel.PlayCard(altered.Deserialize<BattleTurnState>(ModelJson.Options)!, action);
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
                    obj["EquippedUnitId"]?.GetValueKind() == JsonValueKind.Number && obj["EquippedUnitId"]!.GetValue<int>() == 3)
                { obj["EquippedUnitId"] = 999; changedBindings++; }
                foreach (var entry in obj.ToArray()) ChangeBinding(entry.Value);
            }
            else if (node is JsonArray array) foreach (var child in array) ChangeBinding(child);
        }
        ChangeBinding(binding);
        var unbound = BattleActionModel.PlayCard(binding.Deserialize<BattleTurnState>(ModelJson.Options)!, action);
        Require(changedBindings > 0 && (!unbound.Supported || NativeBranchObservationChecks.Key(unbound.State!) != NativeBranchObservationChecks.Key(actual)),
            "Wrong equipment binding received the native result.");
        if (replay.HasValue) { Native(replay.Value); NativeBranchObservationChecks.Pair(fixture, replay.Value); }
        Console.WriteLine("LAVA-SIX-WRITES PASS: 25 native inputs/full state/RNG, six writes, four immutable branches and source controls; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

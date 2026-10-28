using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class FourHordeEightWritesChecks
{
    private const string Reason = "Battle preview floor 1: Combined troop upgrades and enchantment source ordering require native calibration.";
    internal static void Native(FixtureValue fixture, FixtureValue? replay = null, bool retiredEnchanter = false)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, retiredEnchanter ?
            "Battle preview floor 2: Combined troop upgrades and enchantment source ordering require native calibration." : Reason);
        Require(rows.Length == (retiredEnchanter ? 18 : 19) && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing native chain.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(action.CardInstanceId == (retiredEnchanter ? 18 : 31) && action.RoomIndex == 1, "Missing native input.");
        var writes = rows[^1].GetProperty("TroopUpdates").EnumerateArray().Where(update =>
            update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            update.GetProperty("Before").GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == 22)).ToArray();
        Require(writes.Length == (retiredEnchanter ? 5 : 8), "Unexpected native preview write count.");
        for (int i = 0; i < writes.Length; i++)
        {
            var b = writes[i].GetProperty("Before"); var a = writes[i].GetProperty("After");
            var bu = b.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 22);
            var au = a.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 22);
            Require(b.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 5, 21]) &&
                a.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 5, 21]) &&
                bu.GetProperty("Health").GetInt32() == 16 + i * 12 && au.GetProperty("Health").GetInt32() == 28 + i * 12 &&
                bu.GetProperty("Upgrades").GetArrayLength() == i * 4 && au.GetProperty("Upgrades").GetArrayLength() == (i + 1) * 4,
                "Native preview write differs at " + i);
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Concurrent successor differs: " + result.Reason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Action mutated its parent.");
        foreach (string control in new[] { "MonsterChampionUnderlegion1", "Boss_Level8_Fragile", "EnchanterT2_Titanskin", "MonsterGenerousHost", "MonsterSycophant" })
        {
            JsonNode altered = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            int changedCount = 0;
            void Rewrite(JsonNode? node)
            {
                if (node is JsonObject obj)
                {
                    if (obj["AssetKey"]?.GetValueKind() == JsonValueKind.String && obj["AssetKey"]!.GetValue<string>() == control)
                    { obj["AssetKey"] = "unknown"; changedCount++; }
                    foreach (var entry in obj.ToArray()) Rewrite(entry.Value);
                }
                else if (node is JsonArray array) foreach (var child in array) Rewrite(child);
            }
            Rewrite(altered);
            var result = BattleActionModel.PlayCard(altered.Deserialize<BattleTurnState>(ModelJson.Options)!, action);
            Require(changedCount > 0 && (!result.Supported || NativeBranchObservationChecks.Key(result.State!) != NativeBranchObservationChecks.Key(actual)),
                "Unknown source matched calibrated successor: " + control);
        }
        if (retiredEnchanter)
        {
            JsonNode binding = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            int changed = 0;
            void ChangeBinding(JsonNode? node)
            {
                if (node is JsonObject obj)
                {
                    if (obj["DataId"]?.GetValueKind() == JsonValueKind.String && obj["DataId"]!.GetValue<string>() == "217d467c-77c2-481d-b78f-57aaf3dcd4a6" &&
                        obj["EquippedUnitId"]?.GetValueKind() == JsonValueKind.Number && obj["EquippedUnitId"]!.GetValue<int>() == 2)
                    { obj["EquippedUnitId"] = 999; changed++; }
                    foreach (var entry in obj.ToArray()) ChangeBinding(entry.Value);
                }
                else if (node is JsonArray array) foreach (var child in array) ChangeBinding(child);
            }
            ChangeBinding(binding);
            var unbound = BattleActionModel.PlayCard(binding.Deserialize<BattleTurnState>(ModelJson.Options)!, action);
            Require(changed > 0 && (!unbound.Supported || NativeBranchObservationChecks.Key(unbound.State!) != NativeBranchObservationChecks.Key(actual)),
                "Wrong equipment binding received the native result.");
            Require(before.Spawn.Train.Context!.NextUnitId == actual.Spawn.Train.Context!.NextUnitId,
                "Native did not reclaim the preview unit identifier.");
        }
        if (replay.HasValue) { Native(replay.Value, retiredEnchanter: retiredEnchanter); NativeBranchObservationChecks.Pair(fixture, replay.Value); }
        Console.WriteLine($"FOUR-HORDE-WRITES PASS: {rows.Length} native inputs/full state/RNG, {writes.Length} writes, four immutable branches and source controls; retiredEnchanter={retiredEnchanter}; independentPair={replay.HasValue}");
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

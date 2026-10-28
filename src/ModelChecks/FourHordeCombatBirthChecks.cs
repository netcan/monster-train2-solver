using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class FourHordeCombatBirthChecks
{
    private const string Reason = "Train combat floor 2: Combined troop upgrades and enchantment source ordering require native calibration.";

    internal static void Native(FixtureValue fixture, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        Require(rows.Length == 22 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing native end-turn chain.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var updates = rows[^1].GetProperty("TroopUpdates").EnumerateArray().ToArray();
        Require(updates.Length == 18, "Missing native troop updates.");
        for (int index = 0; index < 3; index++)
        {
            var old = updates[index].GetProperty("Before");
            var next = updates[index].GetProperty("After");
            int id = index == 2 ? 27 : 26;
            var oldUnit = old.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == id);
            var nextUnit = next.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == id);
            int count = index == 1 ? 4 : 0;
            Require(old.GetProperty("Preview").GetBoolean() == (index < 2) &&
                old.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 5, 21]) &&
                next.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual(index == 2 ? [2, 5, 27] : [2, 5, 21]) &&
                oldUnit.GetProperty("Horde").GetInt32() == 4 && oldUnit.GetProperty("Health").GetInt32() == 16 + count * 3 &&
                nextUnit.GetProperty("Health").GetInt32() == 28 + count * 3 &&
                oldUnit.GetProperty("Upgrades").GetArrayLength() == count && nextUnit.GetProperty("Upgrades").GetArrayLength() == count + 4,
                "Native birth write differs at " + index);
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Concurrent end turn differs: " + result.UnsupportedReason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "End turn mutated its parent.");
        foreach (string control in new[] { "MonsterChampionUnderlegion1", "Boss_Level8_Fragile", "EnchanterT2_Titanskin", "MonsterSycophant", "EquipmentBinding", "RetiredSpawn" })
        {
            JsonNode changed = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            int changes = 0;
            void Rewrite(JsonNode? node)
            {
                if (node is JsonObject obj)
                {
                    if (control == "EquipmentBinding" && obj["DataId"]?.GetValueKind() == JsonValueKind.String &&
                        obj["DataId"]!.GetValue<string>() == "217d467c-77c2-481d-b78f-57aaf3dcd4a6" &&
                        obj["EquippedUnitId"]?.GetValueKind() == JsonValueKind.Number && obj["EquippedUnitId"]!.GetValue<int>() == 2)
                    { obj["EquippedUnitId"] = 999; changes++; }
                    if (control == "RetiredSpawn" && obj["UnitId"]?.GetValueKind() == JsonValueKind.Number &&
                        obj["UnitId"]!.GetValue<int>() == 21 && obj["SpawnedInPreview"]?.GetValueKind() == JsonValueKind.False)
                    { obj["SpawnedInPreview"] = true; changes++; }
                    if (obj["AssetKey"]?.GetValueKind() == JsonValueKind.String && obj["AssetKey"]!.GetValue<string>() == control)
                    { obj["AssetKey"] = "unknown"; changes++; }
                    foreach (var entry in obj.ToArray()) Rewrite(entry.Value);
                }
                else if (node is JsonArray array) foreach (var child in array) Rewrite(child);
            }
            Rewrite(changed);
            var result = BattleTurnModel.EndTurn(changed.Deserialize<BattleTurnState>(ModelJson.Options)!);
            Require(changes > 0 && (!result.Supported || NativeBranchObservationChecks.Key(result.State!) != NativeBranchObservationChecks.Key(actual)),
                "Unknown source matched calibrated successor: " + control);
        }
        if (replay.HasValue) { Native(replay.Value); NativeBranchObservationChecks.Pair(fixture, replay.Value); }
        Console.WriteLine($"FOUR-HORDE-COMBAT-BIRTH PASS: {rows.Length} native inputs/full state/RNG, two preview writes and one combat birth, four immutable branches/six source controls; independentPair={replay.HasValue}");
    }

    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

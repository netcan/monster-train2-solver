using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class StewardCompanionSixHordePreviewChecks
{
    internal static void Native(FixtureValue fixture, FixtureValue? replay = null, bool threeHorde = false)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture,
            $"Battle preview floor {(threeHorde ? 0 : 2)}: Combined troop upgrades and enchantment source ordering require native calibration.");
        Require(rows.Length == 22 && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Wrong native input count.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var writes = rows[^1].GetProperty("TroopUpdates").EnumerateArray().Where(update =>
            update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            update.GetProperty("Before").GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == 26)).ToArray();
        Require(writes.Length == 4, "Expected four native preview writes.");
        int[] beforeHealth = threeHorde ? [12, 21, 30, 39] : [12, 21, 51, 69];
        int[] afterHealth = threeHorde ? [21, 30, 39, 48] : [21, 30, 69, 87];
        int[] beforeUpgrades = threeHorde ? [0, 3, 6, 9] : [0, 3, 9, 15];
        int[] afterUpgrades = threeHorde ? [3, 6, 9, 12] : [3, 6, 15, 21];
        for (int i = 0; i < writes.Length; i++)
        {
            var b = writes[i].GetProperty("Before"); var a = writes[i].GetProperty("After");
            var bu = b.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 26);
            var au = a.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 26);
            Require(b.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 16, 25]) &&
                a.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 16, 25]) &&
                bu.GetProperty("Health").GetInt32() == beforeHealth[i] && au.GetProperty("Health").GetInt32() == afterHealth[i] &&
                bu.GetProperty("Upgrades").GetArrayLength() == beforeUpgrades[i] && au.GetProperty("Upgrades").GetArrayLength() == afterUpgrades[i] &&
                bu.GetProperty("Horde").GetInt32() == (threeHorde || i < 2 ? 3 : 6), "Native preview write differs at " + i);
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Concurrent successor differs: " + result.Reason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Card play mutated its parent.");
        foreach (string control in new[] { "MonsterChampionUnderlegion1", "Boss_Level8_Fragile", "EnchanterT2_Titanskin", "TrainStewardSmall", "MonsterSycophant", "MonsterFunguy", "ExilesDiadem_Upgrade" })
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
                if (obj["DataId"]?.GetValueKind() == JsonValueKind.String && obj["DataId"]!.GetValue<string>() == "217d467c-77c2-481d-b78f-57aaf3dcd4a6" &&
                    obj["EquippedUnitId"]?.GetValueKind() == JsonValueKind.Number && obj["EquippedUnitId"]!.GetValue<int>() == 2)
                { obj["EquippedUnitId"] = 999; changedBindings++; }
                foreach (var entry in obj.ToArray()) ChangeBinding(entry.Value);
            }
            else if (node is JsonArray array) foreach (var child in array) ChangeBinding(child);
        }
        ChangeBinding(binding);
        var unbound = BattleActionModel.PlayCard(binding.Deserialize<BattleTurnState>(ModelJson.Options)!, action);
        Require(changedBindings > 0 && (!unbound.Supported || NativeBranchObservationChecks.Key(unbound.State!) != NativeBranchObservationChecks.Key(actual)),
            "Wrong equipment binding received the native result.");
        if (replay.HasValue) { Native(replay.Value, threeHorde: threeHorde); NativeBranchObservationChecks.Pair(fixture, replay.Value); }
        Console.WriteLine("STEWARD-COMPANION-HORDE-PREVIEW PASS: 22 native inputs/full state/RNG, four writes, four immutable branches and source/binding controls; threeHorde=" + threeHorde + "; independentPair=" + replay.HasValue);
    }

    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

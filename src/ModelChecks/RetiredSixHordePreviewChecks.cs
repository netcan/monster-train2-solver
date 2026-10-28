using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RetiredSixHordePreviewChecks
{
    internal static void Native(FixtureValue fixture, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture,
            "Battle preview floor 0: Combined troop upgrades and enchantment source ordering require native calibration.");
        Require(rows.Length == 24 && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Wrong native input count.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var updates = rows[^1].GetProperty("TroopUpdates").EnumerateArray().Where(update =>
            update.GetProperty("Before").GetProperty("Preview").GetBoolean()).ToArray();
        Require(updates.Length == 6 && updates[0].GetProperty("Before").GetProperty("Units").EnumerateArray().All(unit => unit.GetProperty("Id").GetInt32() != 23),
            "Missing retired original horde witness.");
        for (int i = 0; i < 5; i++)
        {
            var b = updates[i + 1].GetProperty("Before"); var a = updates[i + 1].GetProperty("After");
            var bu = b.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 30);
            var au = a.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 30);
            Require(b.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 5, 21, 23]) &&
                a.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 5, 21, 23]) &&
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
        foreach (string control in new[] { "MonsterChampionUnderlegion1", "Boss_Level8_Fragile", "EnchanterT2_Titanskin", "MonsterSycophant", "MonsterPricklyPuffball", "MonsterFunguy", "ExilesDiadem_Upgrade" })
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
        JsonNode point = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
        int changedPoints = 0;
        void ChangePoint(JsonNode? node)
        {
            if (node is JsonObject obj)
            {
                if (obj["UnitId"]?.GetValueKind() == JsonValueKind.Number && obj["UnitId"]!.GetValue<int>() == 23 &&
                    obj["SpawnedInPreview"]?.GetValueKind() == JsonValueKind.False)
                { obj["SpawnedInPreview"] = true; changedPoints++; }
                foreach (var entry in obj.ToArray()) ChangePoint(entry.Value);
            }
            else if (node is JsonArray array) foreach (var child in array) ChangePoint(child);
        }
        ChangePoint(point);
        var wrongPoint = BattleActionModel.PlayCard(point.Deserialize<BattleTurnState>(ModelJson.Options)!, action);
        Require(changedPoints > 0 && (!wrongPoint.Supported || NativeBranchObservationChecks.Key(wrongPoint.State!) != NativeBranchObservationChecks.Key(actual)),
            "Wrong cached spawn point received the native result.");
        if (replay.HasValue) { Native(replay.Value); NativeBranchObservationChecks.Pair(fixture, replay.Value); }
        Console.WriteLine("RETIRED-SIX-HORDE-PREVIEW PASS: 24 native inputs/full state/RNG, five writes, four immutable branches and seven source controls; independentPair=" + replay.HasValue);
    }

    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

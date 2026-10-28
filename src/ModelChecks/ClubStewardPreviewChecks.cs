using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ClubStewardPreviewChecks
{
    internal static void Native(FixtureValue fixture, FixtureValue? replay = null, bool upgradedChampion = false)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture,
            "Battle preview floor 2: Combined troop upgrades and enchantment source ordering require native calibration.");
        Require(rows.Length == (upgradedChampion ? 28 : 27) && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Wrong native input count.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var writes = rows[^1].GetProperty("TroopUpdates").EnumerateArray().Where(update =>
            update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            update.GetProperty("Before").GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == 30)).ToArray();
        Require(writes.Length == 6, "Expected six native preview writes.");
        for (int i = 0; i < writes.Length; i++)
        {
            var b = writes[i].GetProperty("Before"); var a = writes[i].GetProperty("After");
            var bu = b.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 30);
            var au = a.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 30);
            int[] expectedCache = upgradedChampion ? [2, 16, 22, 28] : [4, 16, 22, 29];
            Require(b.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual(expectedCache) &&
                a.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual(expectedCache) &&
                bu.GetProperty("Health").GetInt32() == 12 + i * 9 && au.GetProperty("Health").GetInt32() == 21 + i * 9 &&
                bu.GetProperty("Upgrades").GetArrayLength() == 1 + i * 3 && au.GetProperty("Upgrades").GetArrayLength() == 4 + i * 3 &&
                b.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == (upgradedChampion ? 2 : 4)).GetProperty("Health").GetInt32() == 58,
                "Native preview write differs at " + i);
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Concurrent successor differs: " + result.Reason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Card play mutated its parent.");
        foreach (string control in new[] { "MonsterChampionUnderlegion1", "Boss_Level8_Fragile", "EnchanterT2_Titanskin", "TrainStewardBig", "MonsterGenerousHost", "MonsterFunguy", RelicStartCombatAttachmentModel.Floor3UpgradeKey })
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
                    obj["DataId"]!.GetValue<string>() == "217d467c-77c2-481d-b78f-57aaf3dcd4a6" &&
                    obj["EquippedUnitId"]?.GetValueKind() == JsonValueKind.Number && obj["EquippedUnitId"]!.GetValue<int>() == (upgradedChampion ? 2 : 4))
                { obj["EquippedUnitId"] = 999; changedBindings++; }
                foreach (var entry in obj.ToArray()) ChangeBinding(entry.Value);
            }
            else if (node is JsonArray array) foreach (var child in array) ChangeBinding(child);
        }
        ChangeBinding(binding);
        var unbound = BattleActionModel.PlayCard(binding.Deserialize<BattleTurnState>(ModelJson.Options)!, action);
        Require(changedBindings > 0 && (!unbound.Supported || NativeBranchObservationChecks.Key(unbound.State!) != NativeBranchObservationChecks.Key(actual)),
            "Wrong equipment binding received the native result.");
        if (upgradedChampion)
        {
            JsonNode invalid = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            int changedPoints = 0;
            void ChangePoint(JsonNode? node)
            {
                if (node is JsonObject obj)
                {
                    if (obj["UnitId"]?.GetValueKind() == JsonValueKind.Number && obj["UnitId"]!.GetValue<int>() == 28 &&
                        obj["SpawnedInPreview"]?.GetValueKind() == JsonValueKind.False)
                    { obj["SpawnedInPreview"] = true; changedPoints++; }
                    foreach (var entry in obj.ToArray()) ChangePoint(entry.Value);
                }
                else if (node is JsonArray array) foreach (var child in array) ChangePoint(child);
            }
            ChangePoint(invalid);
            var changed = BattleActionModel.PlayCard(invalid.Deserialize<BattleTurnState>(ModelJson.Options)!, action);
            Require(changedPoints > 0 && (!changed.Supported || NativeBranchObservationChecks.Key(changed.State!) != NativeBranchObservationChecks.Key(actual)),
                "Changed missing target spawn point received the native result.");
        }
        if (replay.HasValue) { Native(replay.Value, upgradedChampion: upgradedChampion); NativeBranchObservationChecks.Pair(fixture, replay.Value); }
        Console.WriteLine($"CLUB-STEWARD-PREVIEW PASS: {rows.Length} native inputs/full state/RNG, six writes, four immutable branches and source/binding controls; upgradedChampion={upgradedChampion}; independentPair={replay.HasValue}");
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

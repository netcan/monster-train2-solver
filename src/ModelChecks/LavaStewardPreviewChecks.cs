using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class LavaStewardPreviewChecks
{
    private const string Reason = "Battle preview floor 0: Combined troop upgrades and enchantment source ordering require native calibration.";
    internal static void Native(FixtureValue fixture, FixtureValue? replay = null, bool fiveWrites = false, bool realHorde = false)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        Require(rows.Length == (realHorde ? 22 : fiveWrites ? 21 : 20) && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing native chain.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        int previewId = realHorde ? 27 : fiveWrites ? 26 : 25;
        Require(action.CardInstanceId == (realHorde ? 19 : fiveWrites ? 21 : 13) && action.RoomIndex == (realHorde ? 2 : 0) && actual.Spawn.Train.Context!.NextUnitId == previewId &&
            actual.Spawn.Train.Rooms[0].Units.Any(unit => unit.Id == 24 && unit.AssetKey == "TrainStewardSmall") &&
            (!(fiveWrites || realHorde) || actual.Spawn.Train.Rooms[0].Units.Any(unit => unit.Id == 25 && unit.AssetKey == "MonsterEnthusiasticFan")) &&
            before.Spawn.Train.Context!.CardRegistry!.Any(card => card.DataId == "924ad6ab-3d60-4760-b38e-78f5cf09c1d5" && card.EquippedUnitId == 6),
            "Missing steward or equipped source identity.");
        if (realHorde)
        {
            var birth = rows[^1].GetProperty("TroopUpdates").EnumerateArray().Single(update =>
                !update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
                update.GetProperty("Before").GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 6]) &&
                update.GetProperty("After").GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 6, 26]));
            var birthBefore = birth.GetProperty("Before").GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 26);
            var birthAfter = birth.GetProperty("After").GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 26);
            Require(birthBefore.GetProperty("Health").GetInt32() == 4 && birthAfter.GetProperty("Health").GetInt32() == 7 &&
                birthBefore.GetProperty("Upgrades").GetArrayLength() == 0 && birthAfter.GetProperty("Upgrades").GetArrayLength() == 1,
                "Missing actual horde birth write.");
        }
        var writes = rows[^1].GetProperty("TroopUpdates").EnumerateArray().Where(update =>
            update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            update.GetProperty("Before").GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == previewId)).ToArray();
        Require(writes.Length == (fiveWrites || realHorde ? 5 : 4), "Unexpected preview write count.");
        for (int i = 0; i < writes.Length; i++)
        {
            var b = writes[i].GetProperty("Before"); var a = writes[i].GetProperty("After");
            var bu = b.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == previewId);
            var au = a.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == previewId);
            int[] cache = realHorde ? [2, 6, 26] : [2, 6];
            Require(b.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual(cache) &&
                a.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual(cache) &&
                bu.GetProperty("Health").GetInt32() == 12 + i * 9 && au.GetProperty("Health").GetInt32() == 21 + i * 9 &&
                bu.GetProperty("Upgrades").GetArrayLength() == i * 3 && au.GetProperty("Upgrades").GetArrayLength() == (i + 1) * 3,
                "Preview write differs at " + i);
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Concurrent successor differs: " + result.Reason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Action mutated its parent.");
        foreach (string control in new[] { "MonsterChampionUnderlegion1", "Boss_Level8_Fragile", "EnchanterT2_Titanskin", "MonsterSycophant", "LavaArmor", "924ad6ab-3d60-4760-b38e-78f5cf09c1d5" })
        {
            JsonNode altered = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            int changedCount = 0;
            void Rewrite(JsonNode? node)
            {
                if (node is JsonObject obj)
                {
                    foreach (string field in new[] { "AssetKey", "DataId" })
                        if (obj[field]?.GetValueKind() == JsonValueKind.String && obj[field]!.GetValue<string>() == control)
                        { obj[field] = "unknown"; changedCount++; }
                    foreach (var entry in obj.ToArray()) Rewrite(entry.Value);
                }
                else if (node is JsonArray array) foreach (var child in array) Rewrite(child);
            }
            Rewrite(altered);
            var result = BattleActionModel.PlayCard(altered.Deserialize<BattleTurnState>(ModelJson.Options)!, action);
            Require(changedCount > 0 && (!result.Supported || NativeBranchObservationChecks.Key(result.State!) != NativeBranchObservationChecks.Key(actual)),
                "Unknown source matched calibrated successor: " + control);
        }
        JsonNode wrongBinding = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
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
        ChangeBinding(wrongBinding);
        var wrongBindingResult = BattleActionModel.PlayCard(wrongBinding.Deserialize<BattleTurnState>(ModelJson.Options)!, action);
        Require(changedBindings > 0 && (!wrongBindingResult.Supported || NativeBranchObservationChecks.Key(wrongBindingResult.State!) != NativeBranchObservationChecks.Key(actual)),
            "Wrong equipment binding matched calibrated successor.");
        if (replay.HasValue) { Native(replay.Value, fiveWrites: fiveWrites, realHorde: realHorde); NativeBranchObservationChecks.Pair(fixture, replay.Value); }
        Console.WriteLine("LAVA-STEWARD-PREVIEW PASS: native inputs/full state/RNG, preview writes, equipment binding, four immutable branches and source controls; fiveWrites=" + fiveWrites + "; realHorde=" + realHorde + "; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

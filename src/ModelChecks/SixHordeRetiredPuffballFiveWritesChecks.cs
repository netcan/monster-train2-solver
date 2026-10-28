using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class SixHordeRetiredPuffballFiveWritesChecks
{
    internal static void Native(FixtureValue fixture, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture,
            "Battle preview floor 0: Combined troop upgrades and enchantment source ordering require native calibration.");
        Require(rows.Length == 24 && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Wrong native input count.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var writes = rows[^1].GetProperty("TroopUpdates").EnumerateArray().Where(update =>
            update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            update.GetProperty("Before").GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == 29)).ToArray();
        Require(writes.Length == 5, "Expected five native preview writes.");
        int[] hpBefore = [12, 21, 51, 69, 87], hpAfter = [21, 30, 69, 87, 105];
        int[] upgradesBefore = [0, 3, 9, 15, 21], upgradesAfter = [3, 6, 15, 21, 27];
        for (int i = 0; i < writes.Length; i++)
        {
            var b = writes[i].GetProperty("Before"); var a = writes[i].GetProperty("After");
            var bu = b.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 29);
            var au = a.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 29);
            Require(b.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 7, 17]) &&
                a.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 7, 17]) &&
                bu.GetProperty("Health").GetInt32() == hpBefore[i] && au.GetProperty("Health").GetInt32() == hpAfter[i] &&
                bu.GetProperty("Upgrades").GetArrayLength() == upgradesBefore[i] && au.GetProperty("Upgrades").GetArrayLength() == upgradesAfter[i],
                "Native preview write differs at " + i);
        }
        Require(writes[2].GetProperty("Before").GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 2).GetProperty("Health").GetInt32() == 93 &&
            writes[3].GetProperty("Before").GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 2).GetProperty("Health").GetInt32() == 103,
            "Missing original champion preview healing boundary.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Concurrent successor differs: " + result.Reason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Card play mutated its parent.");
        foreach (string control in new[] { "MonsterChampionUnderlegion1", "Boss_Level8_Fragile", "EnchanterT2_Titanskin", "MonsterPricklyPuffball", "LavaArmor", "MonsterFunguy" })
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
        if (replay.HasValue) { Native(replay.Value); NativeBranchObservationChecks.Pair(fixture, replay.Value); }
        Console.WriteLine("SIX-HORDE-RETIRED-PUFFBALL-FIVE-WRITES PASS: 24 native inputs/full state/RNG, five writes, four immutable branches and source controls; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

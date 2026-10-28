using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ThirdTroopFragileTwoLiveChecks
{
    private const string Reason = "Combined troop upgrades and enchantment source ordering require native calibration.";
    internal static void Native(FixtureValue fixture, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        Require(rows.Length == 24 && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing native prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var context = actual.Spawn.Train.Context!;
        var frame = context.Enchantments!.Frame(context);
        Require(frame.EnchanterIds.SequenceEqual([11, 15, 25]) && !frame.Preview && action.CardInstanceId == 19 &&
            before.Spawn.Train.Context!.FindCard(19)?.DataId == "0c8ff108-478c-4c1c-9c49-f6a297b4e0d8", "Missing original spell and sources.");
        Require(!TroopAuraModel.OnlyFragileBuffSource(frame) && !TroopAuraModel.OnlyFragileBuffSource(frame, allowLiveCombat: true) &&
            TroopAuraModel.OnlyFragileBuffSource(frame, allowTwoLiveCombat: true), "Two live sources escaped their explicit guard.");
        var writes = rows[^1].GetProperty("TroopUpdates").EnumerateArray().Where(update =>
            !update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            update.GetProperty("Before").GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == 27)).ToArray();
        Require(writes.Length > 0 && writes[0].GetProperty("Before").GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 5]) &&
            writes[0].GetProperty("After").GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 5, 27]) &&
            writes[0].GetProperty("Before").GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 27).GetProperty("Health").GetInt32() == 4 &&
            writes[0].GetProperty("After").GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 27).GetProperty("Health").GetInt32() == 7,
            "Native newborn cache or health differs.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Two live source birth differs: " + result.Reason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "PlayCard mutated its parent.");
        foreach (string control in new[] { "MonsterChampionUnderlegion1", "Boss_Level8_Fragile", "EnchanterT2_Titanskin" })
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
            Require(!BattleActionModel.PlayCard(altered.Deserialize<BattleTurnState>(ModelJson.Options)!, action).Supported,
                "Unknown birth source escaped its guard: " + control);
        }
        if (replay.HasValue) { Native(replay.Value); NativeBranchObservationChecks.Pair(fixture, replay.Value); }
        Console.WriteLine("THIRD-TROOP-FRAGILE-TWO-LIVE PASS: 24 native inputs/full state/RNG, spell birth/cache, four immutable branches and source guards; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

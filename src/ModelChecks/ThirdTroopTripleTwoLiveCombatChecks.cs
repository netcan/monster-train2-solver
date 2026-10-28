using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ThirdTroopTripleTwoLiveCombatChecks
{
    private const string Reason = "Train combat floor 2: Combined troop upgrades and enchantment source ordering require native calibration.";
    internal static void Native(FixtureValue fixture, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        Require(rows.Length == 12 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing native prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var context = before.Spawn.Train.Context!;
        var frame = context.Enchantments!.Frame(context);
        Require(frame.EnchanterIds.SequenceEqual([11, 14, 21]) && !frame.Preview &&
            frame.Train.Rooms.SelectMany(room => room.Units).Count(unit => unit.AssetKey == "EnchanterT2_Titanskin" && unit.Health > 0) == 2 &&
            !TroopAuraModel.OnlyFragileBuffSource(frame) && TroopAuraModel.OnlyFragileBuffSource(frame, allowTwoLiveCombat: true),
            "Missing two-live combat source guard.");
        var first = rows[^1].GetProperty("TroopUpdates").EnumerateArray().First(update =>
            !update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            update.GetProperty("Before").GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == 22));
        var original = first.GetProperty("Before").GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 22);
        var changed = first.GetProperty("After").GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 22);
        Require(first.GetProperty("Before").GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 7]) &&
            first.GetProperty("After").GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 7, 22]) &&
            original.GetProperty("Horde").GetInt32() == 3 && original.GetProperty("Health").GetInt32() == 12 &&
            original.GetProperty("Upgrades").GetArrayLength() == 0 && changed.GetProperty("Health").GetInt32() == 21 &&
            changed.GetProperty("Upgrades").GetArrayLength() == 3, "Native birth/cache differs.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Two-live combat birth differs: " + result.UnsupportedReason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "EndTurn mutated its parent.");
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
            Require(!BattleTurnModel.EndTurn(altered.Deserialize<BattleTurnState>(ModelJson.Options)!).Supported,
                "Unknown source escaped its guard: " + control);
        }
        if (replay.HasValue) { Native(replay.Value); NativeBranchObservationChecks.Pair(fixture, replay.Value); }
        Console.WriteLine("THIRD-TROOP-TRIPLE-TWO-LIVE-COMBAT PASS: 12 native inputs/full state/RNG, birth/cache, four immutable branches and three source guards; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

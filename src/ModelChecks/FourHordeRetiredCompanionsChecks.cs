using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class FourHordeRetiredCompanionsChecks
{
    private const string Reason = "Battle preview floor 2: Combined troop upgrades and enchantment source ordering require native calibration.";
    internal static void Native(FixtureValue fixture, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        Require(rows.Length == 21 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing native chain.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var updates = rows[^1].GetProperty("TroopUpdates").EnumerateArray().ToArray();
        var birth = updates.Single(update => !update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            update.GetProperty("Before").GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 5, 21, 22]) &&
            update.GetProperty("After").GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 5, 21, 22, 23]));
        var birthBefore = birth.GetProperty("Before").GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 23);
        var birthAfter = birth.GetProperty("After").GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 23);
        Require(birthBefore.GetProperty("Health").GetInt32() == 12 && birthAfter.GetProperty("Health").GetInt32() == 21 &&
            birthBefore.GetProperty("Upgrades").GetArrayLength() == 0 && birthAfter.GetProperty("Upgrades").GetArrayLength() == 3,
            "Missing real triple horde birth write.");
        var writes = updates.Where(update => update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            update.GetProperty("Before").GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == 27)).ToArray();
        Require(writes.Length == 1, "Expected one native preview write.");
        var b = writes[0].GetProperty("Before"); var a = writes[0].GetProperty("After");
        var bu = b.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 27);
        var au = a.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 27);
        Require(b.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 5, 21, 22, 23]) &&
            a.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 5, 21, 22, 23]) &&
            bu.GetProperty("Health").GetInt32() == 16 && au.GetProperty("Health").GetInt32() == 28 &&
            bu.GetProperty("Upgrades").GetArrayLength() == 0 && au.GetProperty("Upgrades").GetArrayLength() == 4,
            "Four horde preview write differs.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Concurrent successor differs: " + result.UnsupportedReason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "EndTurn mutated its parent.");
        foreach (string control in new[] { "MonsterChampionUnderlegion1", "Boss_Level8_Fragile", "EnchanterT2_Titanskin", "MonsterGenerousHost", "MonsterSycophant", "MonsterSneezyWhelp" })
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
            var result = BattleTurnModel.EndTurn(altered.Deserialize<BattleTurnState>(ModelJson.Options)!);
            Require(changedCount > 0 && (!result.Supported || NativeBranchObservationChecks.Key(result.State!) != NativeBranchObservationChecks.Key(actual)),
                "Unknown source matched calibrated successor: " + control);
        }
        if (replay.HasValue) { Native(replay.Value); NativeBranchObservationChecks.Pair(fixture, replay.Value); }
        Console.WriteLine("FOUR-HORDE-RETIRED-COMPANIONS PASS: 21 native inputs/full state/RNG, real birth and preview writes, four immutable branches and source controls; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class SixHordeShortOwnerChecks
{
    private const string Reason = "Battle preview floor 2: Combined troop upgrades and enchantment source ordering require native calibration.";
    internal static void Native(FixtureValue fixture, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        Require(rows.Length == 21 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing native chain.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var writes = rows[^1].GetProperty("TroopUpdates").EnumerateArray().Where(update =>
            update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            update.GetProperty("Before").GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == 27)).ToArray();
        Require(writes.Length == 4, "Expected four native preview writes.");
        int[] healthBefore = [12, 21, 51, 69], healthAfter = [21, 30, 69, 87];
        int[] upgradesBefore = [0, 3, 9, 15], upgradesAfter = [3, 6, 15, 21];
        for (int i = 0; i < writes.Length; i++)
        {
            var b = writes[i].GetProperty("Before"); var a = writes[i].GetProperty("After");
            var bu = b.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 27);
            var au = a.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 27);
            var owner = b.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 2);
            Require(b.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 5, 21, 22, 23]) &&
                a.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 5, 21, 22, 23]) &&
                owner.GetProperty("Upgrades").Deserialize<string[]>()!.SequenceEqual(["4f70b7b7-b507-4931-9b14-e13dcd6cd59f", ""]) &&
                bu.GetProperty("Health").GetInt32() == healthBefore[i] && au.GetProperty("Health").GetInt32() == healthAfter[i] &&
                bu.GetProperty("Upgrades").GetArrayLength() == upgradesBefore[i] && au.GetProperty("Upgrades").GetArrayLength() == upgradesAfter[i],
                "Native preview write differs at " + i);
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Concurrent successor differs: " + result.UnsupportedReason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "EndTurn mutated its parent.");
        foreach (string control in new[] { "MonsterChampionUnderlegion1", "Boss_Level8_Fragile", "EnchanterT2_Titanskin", "ExilesDiadem_Upgrade", "MonsterSycophant", "MonsterGenerousHost", "MonsterSneezyWhelp" })
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
        Console.WriteLine("SIX-HORDE-SHORT-OWNER PASS: 21 native inputs/full state/RNG, four writes, four immutable branches and seven source controls; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

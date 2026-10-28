using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class FourHordeGoldyPreviewChecks
{
    private const string Reason = "Battle preview floor 2: Combined troop upgrades and enchantment source ordering require native calibration.";

    internal static void Native(FixtureValue fixture, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        Require(rows.Length == 20 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing native turn boundary.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var updates = rows[^1].GetProperty("TroopUpdates").EnumerateArray().ToArray();
        var birth = updates.Single(update => !update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            update.GetProperty("Before").GetProperty("Units").EnumerateArray().Any(unit =>
                unit.GetProperty("Id").GetInt32() == 23 && unit.GetProperty("Health").GetInt32() == 12) &&
            update.GetProperty("After").GetProperty("Units").EnumerateArray().Any(unit =>
                unit.GetProperty("Id").GetInt32() == 23 && unit.GetProperty("Health").GetInt32() == 21));
        var preview = updates.Single(update => update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            update.GetProperty("Before").GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == 27));
        Require(birth.GetProperty("Before").GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 5, 12, 13]) &&
            preview.GetProperty("Before").GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 5, 12, 13, 23]),
            "Unexpected birth caches.");
        var newborn = preview.GetProperty("Before").GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 27);
        var strengthened = preview.GetProperty("After").GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 27);
        Require(newborn.GetProperty("Horde").GetInt32() == 4 && newborn.GetProperty("Health").GetInt32() == 16 &&
            strengthened.GetProperty("Health").GetInt32() == 28 && strengthened.GetProperty("Upgrades").GetArrayLength() == 4,
            "Unexpected four-horde preview write.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Concurrent successor differs: " + result.UnsupportedReason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "EndTurn mutated its parent.");
        foreach (string control in new[] { "MonsterChampionUnderlegion1", "Boss_Level8_Fragile", "EnchanterT2_Titanskin",
            "ExilesDiadem_Upgrade", "MonsterSycophant", "MonsterGenerousHost", "MonsterGoldyWhelp" })
        {
            JsonNode altered = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            int changed = 0;
            void Rewrite(JsonNode? node)
            {
                if (node is JsonObject obj)
                {
                    if (obj["AssetKey"]?.GetValueKind() == JsonValueKind.String && obj["AssetKey"]!.GetValue<string>() == control)
                    { obj["AssetKey"] = "unknown"; changed++; }
                    foreach (var entry in obj.ToArray()) Rewrite(entry.Value);
                }
                else if (node is JsonArray array) foreach (var child in array) Rewrite(child);
            }
            Rewrite(altered);
            var result = BattleTurnModel.EndTurn(altered.Deserialize<BattleTurnState>(ModelJson.Options)!);
            Require(changed > 0 && (!result.Supported || NativeBranchObservationChecks.Key(result.State!) != NativeBranchObservationChecks.Key(actual)),
                "Unknown source matched calibrated successor: " + control);
        }
        if (replay.HasValue) { Native(replay.Value); NativeBranchObservationChecks.Pair(fixture, replay.Value); }
        Console.WriteLine("FOUR-HORDE-GOLDY-PREVIEW PASS: 20 native inputs/full state/RNG, real birth, preview write, four immutable branches and source controls; independentPair=" + replay.HasValue);
    }

    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ThirdTroopWhelpCombatChecks
{
    private const string Reason = "Combined troop upgrades and enchantment source ordering require native calibration.";
    internal static void Native(FixtureValue fixture, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        Require(rows.Length == 15 && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing native prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(action.CardInstanceId == 20 && before.Spawn.Train.Context!.FindCard(20)?.DataId == "eef8abd9-3550-4da6-a8bd-d5f63e9d2b16" &&
            actual.Spawn.Train.Context!.Enchantments!.EnchanterIds.SequenceEqual([11, 15]), "Missing original unit and enemy sources.");
        var first = rows[^1].GetProperty("TroopUpdates").EnumerateArray().First(update =>
            !update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            update.GetProperty("Before").GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == 19));
        var newborn = first.GetProperty("After").GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 19);
        Require(first.GetProperty("Before").GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 6]) &&
            first.GetProperty("After").GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 6, 19]) &&
            newborn.GetProperty("Asset").GetString() == "MonsterSneezyWhelp" && newborn.GetProperty("Health").GetInt32() == 1 &&
            newborn.GetProperty("Upgrades").GetArrayLength() == 0, "Native zero-horde cache write differs.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Whelp cache birth differs: " + result.Reason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "PlayCard mutated its parent.");
        foreach (string control in new[] { "MonsterChampionUnderlegion1", "Boss_Level8_Fragile", "EnchanterT2_Titanskin", "MonsterSneezyWhelp", "Ability" })
        {
            JsonNode altered = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            int changedCount = 0;
            void Rewrite(JsonNode? node)
            {
                if (node is JsonObject obj)
                {
                    if (obj["AssetKey"]?.GetValueKind() == JsonValueKind.String && obj["AssetKey"]!.GetValue<string>() == control)
                    { obj["AssetKey"] = "unknown"; changedCount++; }
                    if (control == "Ability" && obj["AssetKey"]?.GetValueKind() == JsonValueKind.String &&
                        obj["AssetKey"]!.GetValue<string>() == "MonsterSneezyWhelp" && obj["Ability"] is JsonObject ability)
                    { ability["DataId"] = "unknown"; changedCount++; }
                    foreach (var entry in obj.ToArray()) Rewrite(entry.Value);
                }
                else if (node is JsonArray array) foreach (var child in array) Rewrite(child);
            }
            Rewrite(altered);
            Require(changedCount > 0 && !BattleActionModel.PlayCard(altered.Deserialize<BattleTurnState>(ModelJson.Options)!, action).Supported,
                "Unknown cache source escaped its guard: " + control);
        }
        if (replay.HasValue) { Native(replay.Value); NativeBranchObservationChecks.Pair(fixture, replay.Value); }
        Console.WriteLine("THIRD-TROOP-WHELP-COMBAT PASS: 15 native inputs/full state/RNG, zero-horde cache, four immutable branches and five source guards; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

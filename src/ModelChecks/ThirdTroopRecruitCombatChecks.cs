using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ThirdTroopRecruitCombatChecks
{
    private const string Reason = "Combined troop upgrades and enchantment source ordering require native calibration.";
    internal static void Native(FixtureValue fixture, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        Require(rows.Length == 15 && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing native prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(action.CardInstanceId == 15 && before.Spawn.Train.Context!.FindCard(15)?.DataId == "c052c567-1e3c-4f36-a6cd-89b0648b9475" &&
            actual.Spawn.Train.Context!.Enchantments!.EnchanterIds.SequenceEqual([11, 15]), "Missing original unit and enemy sources.");
        var first = rows[^1].GetProperty("TroopUpdates").EnumerateArray().First(update =>
            !update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            update.GetProperty("Before").GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == 19));
        var newborn = first.GetProperty("After").GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 19);
        Require(first.GetProperty("Before").GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 6]) &&
            first.GetProperty("After").GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 6, 19]) &&
            newborn.GetProperty("Asset").GetString() == "MonsterGenerousHost" && newborn.GetProperty("Health").GetInt32() == 5 &&
            newborn.GetProperty("Upgrades").GetArrayLength() == 0, "Native zero-horde cache write differs.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Recruit cache birth differs: " + result.Reason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "PlayCard mutated its parent.");
        foreach (string control in new[] { "MonsterChampionUnderlegion1", "Boss_Level8_Fragile", "EnchanterT2_Titanskin", "MonsterGenerousHost" })
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
                "Unknown cache source escaped its guard: " + control);
        }
        if (replay.HasValue) { Native(replay.Value); NativeBranchObservationChecks.Pair(fixture, replay.Value); }
        Console.WriteLine("THIRD-TROOP-RECRUIT-COMBAT PASS: 15 native inputs/full state/RNG, zero-horde cache, four immutable branches and four source guards; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ThirdTroopFightClubRetiredPreviewChecks
{
    private const string Reason = "Battle preview floor 0: Combined troop upgrades and enchantment source ordering require native calibration.";
    internal static void Native(FixtureValue fixture, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        Require(rows.Length == 25 && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing native chain.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var horde = actual.Spawn.Train.Rooms[2].Units.Single(unit => unit.Id == 26);
        Require(action.CardInstanceId == 16 && action.RoomIndex == 2 && actual.Spawn.Train.Context!.NextUnitId == 28 &&
            horde is { AssetKey: "MonsterFunguy", BaseAttack: 27, Health: 28 } && horde.Status("horde")?.Stacks == 4 &&
            horde.Modifiers!.Upgrades.Count == 5 && horde.Modifiers.Upgrades[^1].DataId == RelicStartCombatAttachmentModel.Floor3UpgradeId,
            "Missing Fight Club and existing horde identity.");
        var writes = rows[^1].GetProperty("TroopUpdates").EnumerateArray().Where(update =>
            update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            update.GetProperty("Before").GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == 28) &&
            update.GetProperty("After").GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == 28)).ToArray();
        Require(writes.Length == 6, "Expected six preview writes.");
        for (int i = 0; i < writes.Length; i++)
        {
            var b = writes[i].GetProperty("Before"); var a = writes[i].GetProperty("After");
            var bu = b.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 28);
            var au = a.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 28);
            Require(b.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 6, 26]) &&
                a.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 6, 26]) &&
                bu.GetProperty("Health").GetInt32() == 12 + 9 * i && au.GetProperty("Health").GetInt32() == 21 + 9 * i &&
                bu.GetProperty("Upgrades").GetArrayLength() == 1 + 3 * i && au.GetProperty("Upgrades").GetArrayLength() == 4 + 3 * i,
                "Native preview write differs at " + i);
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Preview differs: " + result.Reason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "PlayCard mutated its parent.");
        foreach (string control in new[] { "MonsterChampionUnderlegion1", "Boss_Level8_Fragile", "EnchanterT2_Titanskin", "MonsterFunguy" })
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
            Require(!result.Supported || result.State!.Spawn.Train.Context!.NextUnitId != 28,
                "Unknown source incorrectly received identity cleanup: " + control);
        }
        if (replay.HasValue) { Native(replay.Value); NativeBranchObservationChecks.Pair(fixture, replay.Value); }
        Console.WriteLine("THIRD-TROOP-FIGHTCLUB-RETIRED-PREVIEW PASS: 25 native inputs/full state/RNG, six writes, identity, four immutable branches/four source controls; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

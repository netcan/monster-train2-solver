using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class FightClubWhelpDeathChecks
{
    private const string Reason = "Battle preview floor 2: Combined troop upgrades and enchantment source ordering require native calibration.";

    internal static void Native(FixtureValue fixture, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        Require(rows.Length == 23 && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing native chain.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(action.CardInstanceId == 25 && action.RoomIndex == 2 && action.TargetUnitId == 22 &&
            before.Spawn.Train.Rooms[2].Units.Any(unit => unit.Id == 22 && unit.AssetKey == "MonsterSneezyWhelp" && unit.Health == 1) &&
            actual.Spawn.Train.Rooms[2].Units.All(unit => unit.Id != 22), "Missing targeted whelp death.");
        var updates = rows[^1].GetProperty("TroopUpdates").EnumerateArray().ToArray();
        var removal = updates.Single(update => !update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            update.GetProperty("Before").GetProperty("Cache").GetArrayLength() == 5);
        Require(removal.GetProperty("Before").GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 5, 21, 22, 23]) &&
            removal.GetProperty("After").GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 5, 21, 23]), "Missing whelp cache removal.");
        var writes = updates.Where(update => update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            update.GetProperty("Before").GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == 27)).ToArray();
        Require(writes.Length == 2, "Expected two preview writes.");
        int[] healthBefore = [12, 21], healthAfter = [21, 30], upgradesBefore = [1, 4], upgradesAfter = [4, 7];
        for (int i = 0; i < writes.Length; i++)
        {
            var b = writes[i].GetProperty("Before"); var a = writes[i].GetProperty("After");
            var bu = b.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 27);
            var au = a.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 27);
            Require(b.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 5, 21, 23]) &&
                a.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 5, 21, 23]) &&
                bu.GetProperty("Health").GetInt32() == healthBefore[i] && au.GetProperty("Health").GetInt32() == healthAfter[i] &&
                bu.GetProperty("Upgrades").GetArrayLength() == upgradesBefore[i] && au.GetProperty("Upgrades").GetArrayLength() == upgradesAfter[i] &&
                bu.GetProperty("Upgrades")[0].GetString() == RelicStartCombatAttachmentModel.Floor3UpgradeId, "Preview write differs at " + i);
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Concurrent successor differs: " + result.Reason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Action mutated its parent.");
        foreach (string control in new[] { "MonsterChampionUnderlegion1", "Boss_Level8_Fragile", "EnchanterT2_Titanskin", "ExilesDiadem_Upgrade", "FightClub", "MonsterSycophant", "MonsterGenerousHost", "MonsterSneezyWhelp" })
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
        Console.WriteLine("FIGHT-CLUB-WHELP-DEATH PASS: 23 native inputs/full state/RNG, cache removal, two writes, four immutable branches and source controls; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

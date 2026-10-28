using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class SixHordeFightClubCompanionsChecks
{
    private const string Reason = "Battle preview floor 2: Combined troop upgrades and enchantment source ordering require native calibration.";
    internal static void Native(FixtureValue fixture, FixtureValue? replay = null, bool fiveWrites = false)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, fiveWrites ? Reason.Replace("floor 2", "floor 0") : Reason);
        Require(rows.Length == (fiveWrites ? 24 : 22) && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing native chain.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        int unitId = fiveWrites ? 29 : 27;
        Require(action.CardInstanceId == (fiveWrites ? 19 : 16) && action.RoomIndex == (fiveWrites ? 0 : 2) && actual.Spawn.Train.Context!.NextUnitId == unitId,
            "Missing native Fight Club input or preview identity.");
        var writes = rows[^1].GetProperty("TroopUpdates").EnumerateArray().Where(update =>
            update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            update.GetProperty("Before").GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == unitId)).ToArray();
        Require(writes.Length == (fiveWrites ? 5 : 3), "Wrong native preview write count.");
        int[] healthBefore = fiveWrites ? [12, 21, 51, 69, 87] : [12, 21, 51];
        int[] healthAfter = fiveWrites ? [21, 30, 69, 87, 105] : [21, 30, 69];
        int[] upgradesBefore = fiveWrites ? [1, 4, 10, 16, 22] : [1, 4, 10];
        int[] upgradesAfter = fiveWrites ? [4, 7, 16, 22, 28] : [4, 7, 16];
        for (int i = 0; i < writes.Length; i++)
        {
            var b = writes[i].GetProperty("Before"); var a = writes[i].GetProperty("After");
            var bu = b.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == unitId);
            var au = a.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == unitId);
            Require(b.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 5, 21, 22, 23]) &&
                a.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 5, 21, 22, 23]) &&
                bu.GetProperty("Health").GetInt32() == healthBefore[i] && au.GetProperty("Health").GetInt32() == healthAfter[i] &&
                bu.GetProperty("Upgrades").GetArrayLength() == upgradesBefore[i] && au.GetProperty("Upgrades").GetArrayLength() == upgradesAfter[i] &&
                bu.GetProperty("Upgrades")[0].GetString() == RelicStartCombatAttachmentModel.Floor3UpgradeId,
                "Native preview write differs at " + i);
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
        if (replay.HasValue) { Native(replay.Value, fiveWrites: fiveWrites); NativeBranchObservationChecks.Pair(fixture, replay.Value); }
        Console.WriteLine($"SIX-HORDE-FIGHT-CLUB-COMPANIONS PASS: {rows.Length} native inputs/full state/RNG, {writes.Length} writes, four immutable branches and source controls; independentPair={replay.HasValue}");
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

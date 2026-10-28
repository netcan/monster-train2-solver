using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ThirdTroopFightClubMixedChecks
{
    private const string Reason = "Combined troop upgrades and enchantment source ordering require native calibration.";
    internal static void Native(FixtureValue fixture, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        Require(rows.Length == 28 && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing native prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        var context = actual.Spawn.Train.Context!;
        var frame = context.Enchantments!.Frame(context);
        Require(frame.EnchanterIds.SequenceEqual([11, 15, 25]) && !frame.Preview &&
            frame.RetainedUnits.Any(actor => actor.Unit.Id == 15 && actor.Unit.DeathState?.IsDestroyed == true && actor.RoomIndex == -1),
            "Missing live and retired native sources.");
        Require(!TroopAuraModel.OnlyFragileBuffSource(frame, allowLiveCombat: true) &&
            TroopAuraModel.OnlyFragileBuffSource(frame, allowMixedCombat: true), "Mixed combat source escaped its explicit guard.");
        var writes = rows[^1].GetProperty("TroopUpdates").EnumerateArray().Where(update =>
            !update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            update.GetProperty("Before").GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == 31)).ToArray();
        var first = writes.First();
        var nativeBefore = first.GetProperty("Before").GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 31);
        var nativeAfter = first.GetProperty("After").GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 31);
        Require(first.GetProperty("Before").GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 5]) &&
            first.GetProperty("After").GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 5, 31]) &&
            nativeBefore.GetProperty("Horde").GetInt32() == 3 && nativeBefore.GetProperty("Health").GetInt32() == 12 &&
            nativeBefore.GetProperty("Upgrades").EnumerateArray().Single().GetString() == "f93ac9f7-45d2-47a4-a159-a1b326bd047f" &&
            nativeAfter.GetProperty("Health").GetInt32() == 21 && nativeAfter.GetProperty("Upgrades").GetArrayLength() == 4,
            "Native FightClub troop birth differs.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "FightClub mixed birth differs: " + result.Reason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "PlayCard mutated its parent.");
        foreach (string control in new[] { "MonsterChampionUnderlegion1", "Boss_Level8_Fragile", "EnchanterT2_Titanskin", "FightClub" })
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
                "Unknown FightClub source escaped its guard: " + control);
        }
        if (replay.HasValue) { Native(replay.Value); NativeBranchObservationChecks.Pair(fixture, replay.Value); }
        Console.WriteLine("THIRD-TROOP-FIGHTCLUB-MIXED PASS: 28 native inputs/full state/RNG, FightClub birth/cache, four immutable branches and four source guards; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

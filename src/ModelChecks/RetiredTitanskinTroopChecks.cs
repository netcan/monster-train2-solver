using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RetiredTitanskinTroopChecks
{
    private const string Reason = "Combined troop upgrades and enchantment source ordering require native calibration.";

    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 36 && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing 36-input native chain.");
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(action.CardInstanceId == 26 && action.RoomIndex == 2, "Unexpected native action.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var world = before.Spawn.Train.Context!.Enchantments!;
        Require(world.EnchanterIds.SequenceEqual([11, 15, 23]) &&
            world.RetainedUnits.Count(actor => actor.Unit.Id is 15 or 23 && actor.RoomIndex == -1 &&
                actor.Unit.AssetKey == "EnchanterT2_Titanskin" && actor.Unit.DeathState?.IsDestroyed == true) == 2,
            "Missing the two retired native enchanters.");
        Require(TroopAuraModel.OnlyFragileBuffSource(world.Frame(before.Spawn.Train.Context!)), "Native source signature rejected.");
        var writes = rows[^1].GetProperty("TroopUpdates").EnumerateArray().Where(update =>
            !update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            update.GetProperty("Before").GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == 39)).ToArray();
        Require(writes.Length >= 2, "Missing native primary troop updates.");
        var first = writes[0].GetProperty("Before");
        var after = writes[0].GetProperty("After");
        var unitBefore = first.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 39);
        var unitAfter = after.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 39);
        Require(first.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 5]) &&
            unitBefore.GetProperty("Health").GetInt32() == 4 && unitAfter.GetProperty("Health").GetInt32() == 7 &&
            after.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 5, 39]), "Native troop write changed.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var played = BattleActionModel.PlayCard(before, action);
            Require(played.Supported && NativeBranchObservationChecks.Key(played.State!) == NativeBranchObservationChecks.Key(actual),
                "Retired Titanskin branch differs from native: " + played.Reason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Branch mutated its parent.");
        foreach (string control in new[] { "RetiredIdentity", "RetiredRoom", "RetiredDestroyed", "RetiredStatus" })
        {
            var node = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            void Rewrite(JsonNode? value)
            {
                if (value is JsonObject obj)
                {
                    if (obj["Unit"] is JsonObject unit && unit["Id"]?.GetValue<int>() == 15 && obj.ContainsKey("RoomIndex"))
                    {
                        if (control == "RetiredIdentity") unit["AssetKey"] = "unknown";
                        if (control == "RetiredRoom") obj["RoomIndex"] = 1;
                        if (control == "RetiredDestroyed") unit["DeathState"]!["IsDestroyed"] = false;
                        if (control == "RetiredStatus") unit["Triggers"]![0]!["Effects"]![0]!["Enchantment"]!["StatusPool"]![0]!["Stacks"] = 6;
                    }
                    foreach (var entry in obj.ToArray()) Rewrite(entry.Value);
                }
                else if (value is JsonArray array) foreach (var child in array) Rewrite(child);
            }
            Rewrite(node);
            var altered = node.Deserialize<BattleTurnState>(ModelJson.Options)!;
            var context = altered.Spawn.Train.Context!;
            Require(!TroopAuraModel.OnlyFragileBuffSource(context.Enchantments!.Frame(context)),
                "Unknown retired enchanter escaped its source guard: " + control);
        }
        if (replay.HasValue) { Native(replay.Value); NativeBranchObservationChecks.Pair(source, replay.Value); }
        Console.WriteLine("RETIRED-TITANSKIN-TROOP PASS: 36 native inputs/full state/RNG, HP4->7, four immutable branches/four guards; independentPair=" + replay.HasValue);
    }

    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

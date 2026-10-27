using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ThirdTroopFragileBuffChecks
{
    private const string Reason = "Combined troop upgrades and enchantment source ordering require native calibration.";

    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 9 && !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing nine-input native chain.");
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(action.CardInstanceId == 14 && action.RoomIndex == 2, "Unexpected native action.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var writes = rows[^1].GetProperty("TroopUpdates").EnumerateArray().Where(update =>
            !update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            update.GetProperty("Before").GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == 12)).ToArray();
        Require(writes.Length >= 2, "Missing primary troop updates.");
        var first = writes[0].GetProperty("Before");
        var after = writes[0].GetProperty("After");
        var unitBefore = first.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 12);
        var unitAfter = after.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 12);
        Require(first.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 5]) &&
            unitBefore.GetProperty("Health").GetInt32() == 4 && unitAfter.GetProperty("Health").GetInt32() == 7 &&
            unitBefore.GetProperty("Horde").GetInt32() == 1 && unitAfter.GetProperty("Horde").GetInt32() == 1 &&
            unitAfter.GetProperty("Upgrades").EnumerateArray().Select(value => value.GetString()).SequenceEqual([CardUnderlegionTroopUpgradeModel.ThirdAuraUpgradeId]),
            "Native primary troop write changed.");
        var repeated = writes[^1].GetProperty("Before");
        Require(repeated.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 5, 12]) &&
            repeated.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 12).GetProperty("Health").GetInt32() == 7,
            "Native primary cache did not retain the write.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var played = BattleActionModel.PlayCard(before, action);
            Require(played.Supported && NativeBranchObservationChecks.Key(played.State!) == NativeBranchObservationChecks.Key(actual),
                "Third-tier/Fragile branch differs from native: " + played.Reason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Branch mutated its parent.");
        foreach (string control in new[] { "Health", "Identity", "Subtype", "PlayerAura", "UnknownEnemy", "Buff" })
        {
            var node = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            Rewrite(node);
            var played = BattleActionModel.PlayCard(node.Deserialize<BattleTurnState>(ModelJson.Options)!, action);
            Require(!played.Supported && played.Reason!.Contains(Reason),
                "Unknown signature escaped ordering guard: " + control + ": " + played.Reason);
            void Rewrite(JsonNode? value)
            {
                if (value is JsonObject obj)
                {
                    if (obj["TroopAura"] is JsonObject aura)
                    {
                        if (control == "Health") aura["Upgrade"]!["Stats"]!["Health"] = 4;
                        if (control == "Identity") aura["Upgrade"]!["DataId"] = "unknown";
                        if (control == "Subtype") aura["Subtype"] = "SubtypesData_FunGuy";
                    }
                    if (obj["Enchantment"] is JsonObject enchantment)
                    {
                        if (control == "PlayerAura") enchantment["Targeting"]!["AllowPlayer"] = true;
                        if (control == "Buff" && enchantment["StatusPool"] is JsonArray { Count: > 0 } statuses)
                            statuses[0]!["Stacks"] = 4;
                    }
                    if (control == "UnknownEnemy" && obj["AssetKey"]?.GetValueKind() == JsonValueKind.String &&
                        obj["AssetKey"]!.GetValue<string>() == "Boss_Level8_Fragile") obj["AssetKey"] = "unknown";
                    foreach (var entry in obj.ToArray()) Rewrite(entry.Value);
                }
                else if (value is JsonArray array) foreach (var child in array) Rewrite(child);
            }
        }
        if (replay.HasValue) { Native(replay.Value); NativeBranchObservationChecks.Pair(source, replay.Value); }
        Console.WriteLine("THIRD-TROOP-FRAGILE PASS: nine native inputs/full state/forecast/RNG, primary HP4->7 and cache, four immutable branches/six unknown guards; independentPair=" + replay.HasValue);
    }

    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

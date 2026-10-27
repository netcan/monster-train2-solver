using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ChampionTroopTitanskinChecks
{
    private const string Reason = "Battle preview floor 2: Combined troop upgrades and enchantment source ordering require native calibration.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(source, Reason);
        Require(rows.Length == 13 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing complete champion/Titanskin prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(before.Spawn.Turn == 2 && actual.Spawn.Turn == 3 &&
            before.Spawn.Train.Context!.Enchantments!.EnchanterIds.SequenceEqual([10]) &&
            actual.Spawn.Train.Context!.Enchantments!.EnchanterIds.SequenceEqual([10, 16]) &&
            actual.Spawn.Train.Rooms[2].Units.Single(unit => unit.Id == 2).Modifiers!.TroopAura!.Targets.SequenceEqual([2, 4]),
            "Lost original tier-two champion cache or enemy Titanskin sources.");
        var writes = rows[^1].GetProperty("TroopUpdates").EnumerateArray().Where(update =>
            update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            update.GetProperty("Before").GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == 18)).ToArray();
        Require(writes.Length == 3, "Missing native repeated preview writes.");
        int[] healthBefore = [8, 12, 28], healthAfter = [12, 16, 36], horde = [2, 2, 4], upgrades = [0, 2, 6];
        for (int index = 0; index < writes.Length; index++)
        {
            var left = writes[index].GetProperty("Before"); var right = writes[index].GetProperty("After");
            var first = left.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 18);
            var last = right.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == 18);
            Require(left.GetProperty("Room").GetInt32() == 2 && right.GetProperty("Preview").GetBoolean() &&
                left.GetProperty("Cache").Deserialize<int[]>()!.SequenceEqual([2, 4]) &&
                left.GetProperty("Cache").ContentEquals(right.GetProperty("Cache")) &&
                first.GetProperty("Health").GetInt32() == healthBefore[index] && first.GetProperty("MaxHealth").GetInt32() == healthBefore[index] &&
                last.GetProperty("Health").GetInt32() == healthAfter[index] && last.GetProperty("MaxHealth").GetInt32() == healthAfter[index] &&
                first.GetProperty("Horde").GetInt32() == horde[index] && last.GetProperty("Horde").GetInt32() == horde[index] &&
                first.GetProperty("Upgrades").GetArrayLength() == upgrades[index] &&
                last.GetProperty("Upgrades").GetArrayLength() == upgrades[index] + horde[index] &&
                last.GetProperty("Upgrades").EnumerateArray().All(value => value.GetString() == CardUnderlegionTroopUpgradeModel.SecondAuraUpgradeId),
                "Native preview lost per-Horde HP, repeated writes or unchanged primary cache.");
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var ended = BattleTurnModel.EndTurn(before);
            Require(ended.Supported && NativeBranchObservationChecks.Key(ended.State!) == NativeBranchObservationChecks.Key(actual),
                "Champion/Titanskin branch changed complete native successor/forecast/RNG.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Champion/Titanskin preview mutated its parent.");
        foreach (string control in new[] { "Health", "Identity", "Subtype", "PlayerAura", "UnknownEnemy" })
        {
            var node = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
            Rewrite(node);
            var ended = BattleTurnModel.EndTurn(node.Deserialize<BattleTurnState>(ModelJson.Options)!);
            Require(!ended.Supported && ended.UnsupportedReason!.Contains(TroopEnchantmentNoOpChecks.Reason),
                "Unknown champion/enchantment signature escaped ordering guard: " + control + ": " + ended.UnsupportedReason);
            void Rewrite(JsonNode? value)
            {
                if (value is JsonObject obj)
                {
                    if (obj["TroopAura"] is JsonObject aura)
                    {
                        if (control == "Health") aura["Upgrade"]!["Stats"]!["Health"] = 3;
                        if (control == "Identity") aura["Upgrade"]!["DataId"] = "unknown";
                        if (control == "Subtype") aura["Subtype"] = "SubtypesData_FunGuy";
                    }
                    if (control == "PlayerAura" && obj["Enchantment"] is JsonObject enchantment)
                        enchantment["Targeting"]!["AllowPlayer"] = true;
                    if (control == "UnknownEnemy" && obj["AssetKey"]?.GetValueKind() == JsonValueKind.String &&
                        obj["AssetKey"]!.GetValue<string>() == "EnchanterT2_Titanskin") obj["AssetKey"] = "unknown";
                    foreach (var entry in obj.ToArray()) Rewrite(entry.Value);
                }
                else if (value is JsonArray array) foreach (var child in array) Rewrite(child);
            }
        }
        if (replay.HasValue) { Native(replay.Value); NativeBranchObservationChecks.Pair(source, replay.Value); }
        Console.WriteLine("CHAMPION-TROOP-TITANSKIN PASS:13 native inputs/full state/forecast/RNG; preview HP8/12/16 and28/36, unchanged primary cache; four immutable branches/five unknown guards; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

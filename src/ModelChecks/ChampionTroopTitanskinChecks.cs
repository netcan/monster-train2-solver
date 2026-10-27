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
        Require(rows.Length is 13 or 16 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing complete champion/Titanskin prefix.");
        bool threeSources = rows.Length == 16;
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(before.Spawn.Turn == (threeSources ? 3 : 2) && actual.Spawn.Turn == (threeSources ? 4 : 3) &&
            before.Spawn.Train.Context!.Enchantments!.EnchanterIds.SequenceEqual(threeSources ? [10, 17] : [10]) &&
            actual.Spawn.Train.Context!.Enchantments!.EnchanterIds.SequenceEqual(threeSources ? [10, 17, 21] : [10, 16]) &&
            actual.Spawn.Train.Rooms[2].Units.Single(unit => unit.Id == 2).Modifiers!.TroopAura!.Targets.SequenceEqual([2, 4]),
            "Lost original tier-two champion cache or enemy Titanskin sources.");
        int birthId = threeSources ? 22 : 18;
        var writes = rows[^1].GetProperty("TroopUpdates").EnumerateArray().Where(update =>
            update.GetProperty("Before").GetProperty("Preview").GetBoolean() &&
            update.GetProperty("Before").GetProperty("Units").EnumerateArray().Any(unit => unit.GetProperty("Id").GetInt32() == birthId)).ToArray();
        Require(writes.Length == (threeSources ? 4 : 3), "Missing native repeated preview writes.");
        int[] healthBefore = threeSources ? [8, 12, 16, 20] : [8, 12, 28];
        int[] healthAfter = threeSources ? [12, 16, 20, 24] : [12, 16, 36];
        int[] horde = threeSources ? [2, 2, 2, 2] : [2, 2, 4], upgrades = threeSources ? [0, 2, 4, 6] : [0, 2, 6];
        for (int index = 0; index < writes.Length; index++)
        {
            var left = writes[index].GetProperty("Before"); var right = writes[index].GetProperty("After");
            var first = left.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == birthId);
            var last = right.GetProperty("Units").EnumerateArray().Single(unit => unit.GetProperty("Id").GetInt32() == birthId);
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
        Console.WriteLine("CHAMPION-TROOP-TITANSKIN PASS:" + rows.Length + " native inputs/full state/forecast/RNG; " +
            (threeSources ? "three original source identities, preview HP8/12/16/20/24" : "preview HP8/12/16 and28/36") +
            ", unchanged primary cache; four immutable branches/five unknown guards; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

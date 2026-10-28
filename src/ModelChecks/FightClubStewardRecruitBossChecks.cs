using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class FightClubStewardRecruitBossChecks
{
    internal static void Native(FixtureValue fixture, FixtureValue? replay = null, bool woundedSteward = false, bool woundedCompanions = false)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture,
            "Train combat floor 2: Boss kill preview: Combined troop upgrades and enchantment source ordering require native calibration.");
        Require(rows.Length == (woundedSteward ? 29 : 28) && rows[^1].GetProperty("IsEndTurn").GetBoolean() &&
            rows[^1].GetProperty("TroopUpdates").GetArrayLength() == 0, "Wrong native chain or unexpected internal writes.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var born = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == (woundedSteward ? 31 : 30));
        Require(born is { AssetKey: "MonsterFunguy", Health: 21, MaxHealth: 21 } && born.Status("horde")?.Stacks == 3 &&
            actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit is { Id: 11, AssetKey: "Boss_Level8_Fragile" } && unit.Health == (woundedCompanions ? 2910 : woundedSteward ? 2873 : 2870)) &&
            (!woundedCompanions || actual.Spawn.Train.Rooms.Any(room => room.RoomIndex == 0 && room.Units.Any(unit => unit is { Id: 31, AssetKey: "MonsterFunguy", Health: 16, MaxHealth: 16 }))),
            "Native combat birth or boss state differs.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Concurrent successor differs: " + result.UnsupportedReason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "EndTurn mutated its parent.");
        foreach (string control in new[] { "MonsterChampionUnderlegion1", "Boss_Level8_Fragile", "EnchanterT2_Titanskin", "MonsterFunguy", "ExilesDiadem_Upgrade", "FightClub" }.Concat(woundedCompanions ? ["MonsterPricklyPuffball", "MonsterSycophant"] : woundedSteward ? ["TrainStewardBig"] : ["TrainStewardBig", "MonsterGenerousHost"]))
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
            var result = BattleTurnModel.EndTurn(altered.Deserialize<BattleTurnState>(ModelJson.Options)!);
            Require(!result.Supported || NativeBranchObservationChecks.Key(result.State!) != NativeBranchObservationChecks.Key(actual),
                "Unknown source incorrectly received native result: " + control);
        }
        JsonNode binding = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
        int changed = 0;
        void ChangeBinding(JsonNode? node)
        {
            if (node is JsonObject obj)
            {
                if (obj["DataId"]?.GetValueKind() == JsonValueKind.String && obj["DataId"]!.GetValue<string>() == "217d467c-77c2-481d-b78f-57aaf3dcd4a6" &&
                    obj["EquippedUnitId"]?.GetValueKind() == JsonValueKind.Number && obj["EquippedUnitId"]!.GetValue<int>() == (woundedSteward || woundedCompanions ? 2 : 4))
                { obj["EquippedUnitId"] = 999; changed++; }
                foreach (var entry in obj.ToArray()) ChangeBinding(entry.Value);
            }
            else if (node is JsonArray array) foreach (var child in array) ChangeBinding(child);
        }
        ChangeBinding(binding);
        var unbound = BattleTurnModel.EndTurn(binding.Deserialize<BattleTurnState>(ModelJson.Options)!);
        Require(changed > 0 && (!unbound.Supported || NativeBranchObservationChecks.Key(unbound.State!) != NativeBranchObservationChecks.Key(actual)),
            "Wrong equipment binding received the native result.");
        if (replay.HasValue) { Native(replay.Value, woundedSteward: woundedSteward, woundedCompanions: woundedCompanions); NativeBranchObservationChecks.Pair(fixture, replay.Value); }
        Console.WriteLine($"FIGHT-CLUB-STEWARD-BOSS PASS: {rows.Length} native inputs/full state/RNG, combat birth, four immutable branches and source/binding controls; internal writes not captured; woundedSteward={woundedSteward}; woundedCompanions={woundedCompanions}; independentPair={replay.HasValue}");
    }

    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

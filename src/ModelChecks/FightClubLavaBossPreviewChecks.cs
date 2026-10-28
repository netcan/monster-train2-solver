using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class FightClubLavaBossPreviewChecks
{
    private const string Reason = "Train combat floor 2: Boss kill preview: Combined troop upgrades and enchantment source ordering require native calibration.";

    internal static void Native(FixtureValue fixture, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        Require(rows.Length == 28 && rows[^1].GetProperty("IsEndTurn").GetBoolean() &&
            rows[^1].GetProperty("TroopUpdates").GetArrayLength() == 0,
            "Missing native chain or unexpected internal write capture.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var oldBoss = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.AssetKey == "Boss_Level8_Fragile");
        var boss = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.AssetKey == "Boss_Level8_Fragile");
        var birth = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 30);
        var companion = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 6);
        Require(before.Spawn.Turn == 4 && actual.Spawn.Turn == 5 && oldBoss.Health == 2892 && boss.Health == 2892 &&
            birth is { AssetKey: "MonsterFunguy", Health: 21, MaxHealth: 21, BaseAttack: 24, Modifiers.Upgrades.Count: 4 } &&
            birth.Status("horde")?.Stacks == 3 && companion is { AssetKey: "MonsterSycophant", Health: 35, MaxHealth: 35 },
            "Missing actual birth or unchanged boss witness.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Concurrent successor differs: " + result.UnsupportedReason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "EndTurn mutated its parent.");
        foreach (string control in new[] { "MonsterChampionUnderlegion1", "Boss_Level8_Fragile", "EnchanterT2_Titanskin", "MeatShield_Upgrade", "FightClub", "LavaArmor", "MonsterSycophant" })
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
        if (replay.HasValue) { Native(replay.Value); NativeBranchObservationChecks.Pair(fixture, replay.Value); }
        Console.WriteLine("FIGHT-CLUB-LAVA-BOSS-PREVIEW PASS: 28 native inputs/full state/RNG, actual birth, internal writes not captured, four immutable branches and seven source controls; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

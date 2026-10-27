using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ChampionMeatShieldBossPreviewChecks
{
    private const string Reason = "Native preview identity differs after input37.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        Check(source, true);
        if (replay.HasValue)
        {
            Check(replay.Value, false);
            NativeBranchObservationChecks.Pair(source, replay.Value, originalPredictionPresent: true);
        }
        Console.WriteLine("CHAMPION-MEAT-SHIELD-BOSS-PREVIEW PASS:37 native inputs/full state/forecast/RNG; no temporary35 allocation before real Funguy35/wave36..38; four immutable branches; independentPair=" + replay.HasValue);
    }

    private static void Check(FixtureValue fixture, bool historical)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason, historical ? Historical : null);
        Require(rows.Length == 37 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing original thirty-seven-input boundary.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(before.Spawn.Turn == 6 && actual.Spawn.Turn == 7 && before.Spawn.NextUnitId == 35 && actual.Spawn.NextUnitId == 39 &&
            actual.Spawn.Train.Rooms[2].Units.Single(unit => unit.Id == 35) is
                { AssetKey: "MonsterFunguy", SpawnerCardId: 0, Health: 12, MaxHealth: 12 } &&
            actual.Spawn.Train.Rooms[0].Units.Take(3).Select(unit => unit.Id).SequenceEqual([36, 37, 38]) &&
            actual.Spawn.Train.Context!.SpawnPoints!.Units.All(point => !point.SpawnedInPreview || point.UnitId != 35),
            "Lost native main birth, next wave and exact identity allocation.");
        var current = fixture.GetProperty("Request").GetProperty("Root").Deserialize<BattleTurnState>()!;
        foreach (var row in rows)
        {
            current = BattlePlannerChecks.Replay(current, [new BattleSearchStep(row.GetProperty("Action").Deserialize<PlayCardAction>(),
                row.GetProperty("IsEndTurn").GetBoolean())], out var outcome);
            Require(outcome == RoomOutcome.Exchanged && NativeBranchObservationChecks.Key(current) ==
                NativeBranchObservationChecks.Key(row.GetProperty("Actual").Deserialize<BattleTurnState>()!), "Continuous native prefix changed state/RNG.");
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var ended = BattleTurnModel.EndTurn(before);
            Require(ended.Supported && NativeBranchObservationChecks.Key(ended.State!) == NativeBranchObservationChecks.Key(actual),
                "Boss preview lifetime changed the complete native successor/forecast/RNG.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Boss preview changed its immutable parent.");
    }

    private static void Historical(BattleTurnState old, BattleTurnState actual)
    {
        Require(old.Spawn.NextUnitId == 40 && actual.Spawn.NextUnitId == 39 &&
            old.Spawn.Train.Rooms[2].Units.Any(unit => unit.Id == 36 && unit.AssetKey == "MonsterFunguy"),
            "Preserve original temporary35 before primary36 allocation.");
        var node = JsonNode.Parse(NativeBranchObservationChecks.Key(old))!;
        var points = node["Context"]!["SpawnPoints"]!["Units"]!.AsArray();
        var phantom = points.Single(point => point!["UnitId"]!.GetValue<int>() == 35)!;
        Require(phantom["SpawnedInPreview"]!.GetValue<bool>() && phantom["Current"] == null && phantom["LastKnown"] == null,
            "Historical temporary35 retained a physical placement.");
        points.Remove(phantom);
        Rewrite(node);
        Require(ModelJson.Difference(node.ToJsonString(), NativeBranchObservationChecks.Key(actual)) == null,
            "Historical difference extends beyond the single temporary identity: " +
                ModelJson.Difference(node.ToJsonString(), NativeBranchObservationChecks.Key(actual)));

        static void Rewrite(JsonNode? value)
        {
            if (value is JsonObject obj)
            {
                foreach (var entry in obj.ToArray())
                {
                    bool identity = entry.Key is "UnitId" or "NextUnitId" or "LastSpawnedUnitId" or "LastAttackerId" or "LastFeederId" ||
                        entry.Key == "Id" && obj.ContainsKey("AssetKey");
                    if (identity && entry.Value is JsonValue number && number.TryGetValue<int>(out int id) && id >= 36)
                        obj[entry.Key] = id - 1;
                    else if (entry.Value is JsonArray ids && (entry.Key == "Occupants" || entry.Key == "Targets" && obj.ContainsKey("Upgrade")))
                    {
                        for (int index = 0; index < ids.Count; index++)
                            if (ids[index] is JsonValue item && item.TryGetValue<int>(out int target) && target >= 36) ids[index] = target - 1;
                    }
                    else Rewrite(entry.Value);
                }
            }
            else if (value is JsonArray array) foreach (var child in array) Rewrite(child);
        }
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}

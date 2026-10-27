using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class GreenRecruitBossPreviewChecks
{
    private const string Reason = "Native Green Recruit boss preview identity differs after input48.";
    internal static void Native(string sourcePath, string? replayPath = null)
    {
        using var source = ModelJson.ReadFixture(sourcePath);
        var fixture = source.RootElement;
        var rows = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(rows.Length == 48 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing original48-input rejection.");
        var before = rows[^1].GetProperty("Root").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var historical = rows[^1].GetProperty("Expected").Deserialize<BattleTurnState>()!;
        Require(before.Spawn.Turn == 8 && before.Spawn.NextUnitId == 47 && actual.Spawn.Turn == 9 &&
            actual.Spawn.NextUnitId == 49 && historical.Spawn.NextUnitId == 51 &&
            before.Spawn.Train.Rooms[0].Units.Count(unit => unit.AssetKey == "MonsterGenerousHost") == 2,
            "Missing paired Green Recruit deaths before real47/48 allocation.");
        var old = JsonNode.Parse(NativeBranchObservationChecks.Key(historical))!;
        var points = old["Context"]!["SpawnPoints"]!["Units"]!.AsArray();
        foreach (int id in new[] { 47, 49 })
        {
            var phantom = points.Single(point => point!["UnitId"]!.GetValue<int>() == id)!;
            Require(phantom["SpawnedInPreview"]!.GetValue<bool>() && phantom["Current"] == null && phantom["LastKnown"] == null,
                "Historical preview retained a physical placement.");
            points.Remove(phantom);
        }
        Rewrite(old);
        Require(ModelJson.Difference(old.ToJsonString(), NativeBranchObservationChecks.Key(actual)) == null,
            "Historical boundary differs beyond the two unobserved preview identities.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var ended = BattleTurnModel.EndTurn(before);
            Require(ended.Supported && NativeBranchObservationChecks.Key(ended.State!) == NativeBranchObservationChecks.Key(actual),
                "Green Recruit boss preview changed the complete native successor/forecast/RNG.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Green Recruit preview mutated its parent.");
        // Check the raw inner exchanges as well as the stable end-of-turn result.
        foreach (var pair in new[] { (Stage: 71, Birth: 47), (Stage: 73, Birth: 48) })
        {
            var observed = fixture.GetProperty("Stages")[pair.Stage].GetProperty("Actual").Deserialize<RoomCombatState>()!;
            Require(observed.Units.Single(unit => unit.Id == pair.Birth) is
                { AssetKey: "MonsterFunguy", SpawnerCardId: 0, Health: 16, MaxHealth: 16 } birth && birth.Status("horde")?.Stacks == 4 &&
                !observed.Context!.SpawnPoints!.Units.Single(point => point.UnitId == pair.Birth).SpawnedInPreview,
                "Missing actual four-stack recruit fallback in its first native exchange.");
        }
        RoomCombatChecks.NativeObservedRooms(sourcePath);
        TrainCombatChecks.Native(fixture);
        RollingAdviceChecks.ObservedSuccessors(fixture);
        if (replayPath != null)
        {
            using var replay = ModelJson.ReadFixture(replayPath);
            var request = replay.RootElement.GetProperty("Request");
            Require(request.GetProperty("SourceSHA256").GetString() ==
                Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(sourcePath))).ToLowerInvariant(), "Independent replay changed its source.");
            var independent = NativeBranchObservationChecks.Replay(replay.RootElement, Reason);
            Require(independent.Length == rows.Length, "Independent replay changed input count.");
            for (int index = 0; index < rows.Length; index++)
            {
                Require(NativeBranchObservationChecks.Key(rows[index].GetProperty("Root").Deserialize<BattleTurnState>()!) ==
                    NativeBranchObservationChecks.Key(independent[index].GetProperty("Before").Deserialize<BattleTurnState>()!) &&
                    NativeBranchObservationChecks.Key(rows[index].GetProperty("Actual").Deserialize<BattleTurnState>()!) ==
                    NativeBranchObservationChecks.Key(independent[index].GetProperty("Actual").Deserialize<BattleTurnState>()!) &&
                    rows[index].GetProperty("Action").ContentEquals(independent[index].GetProperty("Action")) &&
                    rows[index].GetProperty("IsEndTurn").ContentEquals(independent[index].GetProperty("IsEndTurn")),
                    "Independent native state/input differs at " + index);
            }
        }
        Console.WriteLine("GREEN-RECRUIT-BOSS-PREVIEW PASS:48 actual native inputs, all raw rooms/train phases/full state/RNG, real four-stack47/48, four immutable branches; independentPair=" + (replayPath != null));
    }

    private static void Rewrite(JsonNode? node)
    {
        if (node is JsonObject obj)
        foreach (var item in obj.ToArray())
        {
            bool identity = item.Key is "UnitId" or "NextUnitId" or "LastSpawnedUnitId" or "LastAttackerId" or "LastFeederId" ||
                item.Key == "Id" && obj.ContainsKey("AssetKey");
            if (identity && item.Value is JsonValue number && number.TryGetValue<int>(out int id) && id >= 48)
                obj[item.Key] = id == 48 ? 47 : id - 2;
            else Rewrite(item.Value);
        }
        else if (node is JsonArray array) foreach (var child in array) Rewrite(child);
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}

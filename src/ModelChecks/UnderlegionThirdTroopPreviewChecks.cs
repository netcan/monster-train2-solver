using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UnderlegionThirdTroopPreviewChecks
{
    internal const string Reason = "Native third-tier champion preview identity differs after input44.";
    internal static void Native(string path, string? replayPath = null)
    {
        using var document = ModelJson.ReadFixture(path);
        var source = document.RootElement;
        var rows = source.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(rows.Length == 44 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Wrong third-tier champion preview boundary.");
        var before = rows[^1].GetProperty("Root").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var historical = rows[^1].GetProperty("Expected").Deserialize<BattleTurnState>()!;
        var champion = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.AssetKey == "MonsterChampionUnderlegion1");
        Require(before.Spawn.Turn == 6 && before.Spawn.NextUnitId == 28 && actual.Spawn.Turn == 7 &&
            actual.Spawn.NextUnitId == 31 && historical.Spawn.NextUnitId == 33 && champion.Id == 2 &&
            champion.Triggers.Count == 1 && champion.Triggers[0].Effects.Single().Action is
                { Value: 3, Statuses: [{ Id: "horde", Stacks: 3 }] } &&
            champion.Modifiers!.TroopAura!.Upgrade.DataId == CardUnderlegionTroopUpgradeModel.ThirdAuraUpgradeId,
            "Missing natural three-stack champion and unobserved31/32 identities.");
        var old = JsonNode.Parse(NativeBranchObservationChecks.Key(historical))!;
        var points = old["Context"]!["SpawnPoints"]!["Units"]!.AsArray();
        foreach (int id in new[] { 31, 32 })
        {
            var phantom = points.Single(point => point!["UnitId"]!.GetValue<int>() == id)!;
            Require(phantom["SpawnedInPreview"]!.GetValue<bool>() && phantom["Current"] == null && phantom["LastKnown"] == null,
                "Historical preview retained physical placement.");
            points.Remove(phantom);
        }
        Rewrite(old);
        Require(ModelJson.Difference(old.ToJsonString(), NativeBranchObservationChecks.Key(actual)) == null,
            "Historical boundary differs beyond two unobserved preview identities.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Third-tier preview differs from complete native state/forecast/RNG.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Third-tier preview mutated parent.");
        Require(PreviewBirthModel.IsOriginalChampionEnemyDeath(champion, before.Spawn.Train.Context!), "Original signature not admitted.");
        foreach (string control in new[] { "Amount", "Status", "Fallback", "Spawner" })
        {
            var node = JsonSerializer.SerializeToNode(champion, ModelJson.Options)!;
            var action = node["Triggers"]![0]!["Effects"]![0]!["Action"]!;
            if (control == "Amount") action["Value"] = 4;
            if (control == "Status") action["Statuses"]![0]!["Stacks"] = 4;
            if (control == "Fallback") action["SpawnFallbackUnit"]!["BaseAttack"] = 4;
            if (control == "Spawner") node["SpawnerCardId"] = 0;
            Require(!PreviewBirthModel.IsOriginalChampionEnemyDeath(node.Deserialize<CombatUnit>(ModelJson.Options)!, before.Spawn.Train.Context!),
                "Unknown preview signature admitted: " + control);
        }
        RoomCombatChecks.NativeObservedRooms(path);
        TrainCombatChecks.Native(source);
        RollingAdviceChecks.ObservedSuccessors(source);
        if (replayPath != null)
        {
            using var replay = ModelJson.ReadFixture(replayPath);
            Require(replay.RootElement.GetProperty("Request").GetProperty("SourceSHA256").GetString() ==
                Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant(), "Replay changed original source.");
            var repeated = NativeBranchObservationChecks.Replay(replay.RootElement, Reason);
            Require(repeated.Length == rows.Length, "Replay changed input count.");
            for (int index = 0; index < rows.Length; index++)
            {
                Require(NativeBranchObservationChecks.Key(rows[index].GetProperty("Root").Deserialize<BattleTurnState>()!) ==
                    NativeBranchObservationChecks.Key(repeated[index].GetProperty("Before").Deserialize<BattleTurnState>()!) &&
                    NativeBranchObservationChecks.Key(rows[index].GetProperty("Actual").Deserialize<BattleTurnState>()!) ==
                    NativeBranchObservationChecks.Key(repeated[index].GetProperty("Actual").Deserialize<BattleTurnState>()!) &&
                    rows[index].GetProperty("Action").ContentEquals(repeated[index].GetProperty("Action")) &&
                    rows[index].GetProperty("IsEndTurn").ContentEquals(repeated[index].GetProperty("IsEndTurn")),
                    "Independent native state/input differs at " + index);
            }
        }
        Console.WriteLine("UNDERLEGION-THIRD-TROOP-PREVIEW PASS:44 native inputs/full successors/forecasts/RNG, raw room/train phases, four immutable branches/four signature controls; unobserved31/32 do not consume identities; independentPair=" + (replayPath != null));
    }
    private static void Rewrite(JsonNode? node)
    {
        if (node is JsonObject obj)
        foreach (var item in obj.ToArray())
        {
            if (item.Key == "NextUnitId" && item.Value is JsonValue value && value.TryGetValue<int>(out int id) && id == 33)
                obj[item.Key] = 31;
            else Rewrite(item.Value);
        }
        else if (node is JsonArray array) foreach (var child in array) Rewrite(child);
    }
    private static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
}

using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UnderlegionThirdTroopPreviewChecks
{
    internal const string Reason = "Native third-tier champion preview identity differs after input44.";
    internal const string BossReason = "Native Dusk Mantle Green Recruit boss preview identity differs after input50.";
    internal static void Native(string path, string? replayPath = null)
    {
        using var document = ModelJson.ReadFixture(path);
        var source = document.RootElement;
        var rows = source.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        bool boss = rows.Length == 50;
        Require((rows.Length == 44 || boss) && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Wrong third-tier champion preview boundary.");
        var before = rows[^1].GetProperty("Root").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var historical = rows[^1].GetProperty("Expected").Deserialize<BattleTurnState>()!;
        var champion = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.AssetKey == "MonsterChampionUnderlegion1");
        Require(before.Spawn.Turn == (boss ? 7 : 6) && before.Spawn.NextUnitId == (boss ? 31 : 28) && actual.Spawn.Turn == (boss ? 8 : 7) &&
            actual.Spawn.NextUnitId == (boss ? 32 : 31) && historical.Spawn.NextUnitId == 33 && champion.Id == 2 &&
            champion.Triggers.Count == 1 && champion.Triggers[0].Effects.Single().Action is
                { Value: 3, Statuses: [{ Id: "horde", Stacks: 3 }] } &&
            champion.Modifiers!.TroopAura!.Upgrade.DataId == CardUnderlegionTroopUpgradeModel.ThirdAuraUpgradeId,
            "Missing natural three-stack champion and unobserved preview identities.");
        var old = JsonNode.Parse(NativeBranchObservationChecks.Key(historical))!;
        var points = old["Context"]!["SpawnPoints"]!["Units"]!.AsArray();
        foreach (int id in boss ? new[] { 31 } : new[] { 31, 32 })
        {
            var phantom = points.Single(point => point!["UnitId"]!.GetValue<int>() == id)!;
            Require(phantom["SpawnedInPreview"]!.GetValue<bool>() && phantom["Current"] == null && phantom["LastKnown"] == null,
                "Historical preview retained physical placement.");
            points.Remove(phantom);
        }
        Rewrite(old, boss);
        Require(ModelJson.Difference(old.ToJsonString(), NativeBranchObservationChecks.Key(actual)) == null,
            "Historical boundary differs beyond unobserved preview identities: " + ModelJson.Difference(old.ToJsonString(), NativeBranchObservationChecks.Key(actual)));
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        if (boss) Require(PreviewBirthModel.IsOriginalGreenRecruit(before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 18),
            before.Spawn.Train.Context!), "Original Dusk Mantle recruit signature not admitted.");
        var first = BattleTurnModel.EndTurn(before);
        Require(first.Supported && NativeBranchObservationChecks.Key(first.State!) == NativeBranchObservationChecks.Key(actual),
            "Third-tier native boundary differs: " + (first.Supported
                ? ModelJson.Difference(NativeBranchObservationChecks.Key(first.State!), NativeBranchObservationChecks.Key(actual))
                : first.UnsupportedReason));
        Parallel.For(0, 4, _ =>
        {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Third-tier preview differs from complete native state/forecast/RNG.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Third-tier preview mutated parent.");
        Require(PreviewBirthModel.IsOriginalChampionEnemyDeath(champion, before.Spawn.Train.Context!), "Original signature not admitted.");
        if (boss)
        {
            var recruit = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 18);
            Require(recruit.EquipmentCards.SequenceEqual([38]) && recruit.Triggers.Count == 3 &&
                PreviewBirthModel.IsOriginalGreenRecruit(recruit, before.Spawn.Train.Context!),
                "Missing original Dusk Mantle38/Green Recruit18 witness.");
            foreach (string control in new[] { "Amount", "Card", "Origin", "Lost", "Silence" })
            {
                var node = JsonSerializer.SerializeToNode(recruit, ModelJson.Options)!;
                if (control == "Amount") node["Triggers"]![1]!["Effects"]![0]!["Action"]!["Statuses"]![0]!["Stacks"] = 6;
                if (control == "Card") node["EquipmentCards"]![0] = 99999;
                if (control == "Origin") node["Triggers"]![1]!["Origin"]!["UpgradeId"] = "unknown";
                if (control == "Lost") node["Triggers"]![2]!["Kind"] = "OnMoonLit";
                if (control == "Silence") node["Triggers"]![2]!["IgnoreSilence"] = false;
                Require(!PreviewBirthModel.IsOriginalGreenRecruit(node.Deserialize<CombatUnit>(ModelJson.Options)!, before.Spawn.Train.Context!),
                    "Unknown Dusk Mantle signature admitted: " + control);
            }
        }
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
            var repeated = NativeBranchObservationChecks.Replay(replay.RootElement, boss ? BossReason : Reason);
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
        Console.WriteLine((boss ? "DUSK-MANTLE-RECRUIT-PREVIEW" : "UNDERLEGION-THIRD-TROOP-PREVIEW") + " PASS:" + rows.Length +
            " native inputs/full successors/forecasts/RNG, raw room/train phases, four immutable branches/" + (boss ? "nine" : "four") +
            " signature controls; unobserved preview births do not consume identities; independentPair=" + (replayPath != null));
    }
    private static void Rewrite(JsonNode? node, bool boss)
    {
        if (node is JsonObject obj)
        foreach (var item in obj.ToArray())
        {
            if (item.Key == "NextUnitId" && item.Value is JsonValue value && value.TryGetValue<int>(out int id) && id == 33)
                obj[item.Key] = boss ? 32 : 31;
            else if (boss && (item.Key is "UnitId" or "LastSpawnedUnitId" or "LastAttackerId" or "LastFeederId" ||
                    item.Key == "Id" && obj.ContainsKey("AssetKey")) &&
                item.Value is JsonValue identity && identity.TryGetValue<int>(out int unitId) && unitId == 32)
                obj[item.Key] = 31;
            else Rewrite(item.Value, boss);
        }
        else if (node is JsonArray array) foreach (var child in array) Rewrite(child, boss);
    }
    private static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
}

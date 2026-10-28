using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class StewardWhelpPreviewIdChecks
{
    internal static void Native(FixtureValue source, FixtureValue replay)
    {
        var rows = NativeBranchObservationChecks.Replay(source, "Native partial prefix successor mismatch observation.", (old, actual) =>
        {
            Require(old.Spawn.NextUnitId == 28 && actual.Spawn.NextUnitId == 27 &&
                old.Spawn.Train.Context!.SpawnPoints!.Units.Single(point => point.UnitId == 27) is
                    { SpawnedInPreview: true, Current: null, LastKnown: null } &&
                actual.Spawn.Train.Context!.SpawnPoints!.Units.All(point => point.UnitId < 27),
                "Missing historical preview allocator mismatch.");
            JsonNode normalized = JsonNode.Parse(NativeBranchObservationChecks.Key(old))!;
            normalized["NextUnitId"] = 27;
            normalized["Context"]!["NextUnitId"] = 27;
            var points = normalized["Context"]!["SpawnPoints"]!["Units"]!.AsArray();
            points.Remove(points.Single(point => point!["UnitId"]!.GetValue<int>() == 27));
            Require(ModelJson.Difference(normalized.ToJsonString(), NativeBranchObservationChecks.Key(actual)) == null,
                "Historical mismatch contains more than the temporary identity.");
        });
        var repeated = NativeBranchObservationChecks.Replay(replay, "Native partial prefix successor mismatch observation.");
        NativeBranchObservationChecks.Pair(source, replay, originalPredictionPresent: true);
        Require(rows.Length == 22 && repeated.Length == rows.Length && rows[^1].GetProperty("IsEndTurn").GetBoolean(),
            "Missing original 22-input boundary.");
        var current = source.GetProperty("Request").GetProperty("Root").Deserialize<BattleTurnState>()!;
        for (int i = 0; i < rows.Length; i++)
        {
            var before = rows[i].GetProperty("Before").Deserialize<BattleTurnState>()!;
            var actual = rows[i].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Require(NativeBranchObservationChecks.Key(current) == NativeBranchObservationChecks.Key(before) &&
                NativeBranchObservationChecks.Key(before) == NativeBranchObservationChecks.Key(repeated[i].GetProperty("Before").Deserialize<BattleTurnState>()!) &&
                NativeBranchObservationChecks.Key(actual) == NativeBranchObservationChecks.Key(repeated[i].GetProperty("Actual").Deserialize<BattleTurnState>()!) &&
                rows[i].GetProperty("Action").ContentEquals(repeated[i].GetProperty("Action")) &&
                rows[i].GetProperty("IsEndTurn").ContentEquals(repeated[i].GetProperty("IsEndTurn")),
                "Independent native input or successor differs at " + i);
            bool end = rows[i].GetProperty("IsEndTurn").GetBoolean();
            var action = rows[i].GetProperty("Action").Deserialize<PlayCardAction>();
            current = BattlePlannerChecks.Replay(before, [new BattleSearchStep(action, end)], out var outcome);
            Require(outcome == RoomOutcome.Exchanged && NativeBranchObservationChecks.Key(current) == NativeBranchObservationChecks.Key(actual),
                "Repaired native successor differs at " + i);
        }
        var last = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var boundary = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        string parent = JsonSerializer.Serialize(boundary, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(boundary);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(last),
                "Concurrent end turn differs: " + result.UnsupportedReason);
        });
        Require(parent == JsonSerializer.Serialize(boundary, ModelJson.Options), "End turn mutated its parent.");
        foreach (string control in new[] { "MonsterChampionUnderlegion1", "TrainStewardBig", "MonsterSneezyWhelp", "Boss_Level8_Fragile", "EnchanterT2_Titanskin" })
        {
            JsonNode altered = JsonSerializer.SerializeToNode(boundary, ModelJson.Options)!;
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
            Require(!result.Supported || NativeBranchObservationChecks.Key(result.State!) != NativeBranchObservationChecks.Key(last),
                "Unknown source incorrectly received native result: " + control);
        }
        JsonNode binding = JsonSerializer.SerializeToNode(boundary, ModelJson.Options)!;
        int changedBindings = 0;
        void ChangeBinding(JsonNode? node)
        {
            if (node is JsonObject obj)
            {
                if (obj["DataId"]?.GetValueKind() == JsonValueKind.String && obj["DataId"]!.GetValue<string>() == "217d467c-77c2-481d-b78f-57aaf3dcd4a6" &&
                    obj["EquippedUnitId"]?.GetValueKind() == JsonValueKind.Number && obj["EquippedUnitId"]!.GetValue<int>() == 2)
                { obj["EquippedUnitId"] = 999; changedBindings++; }
                foreach (var entry in obj.ToArray()) ChangeBinding(entry.Value);
            }
            else if (node is JsonArray array) foreach (var child in array) ChangeBinding(child);
        }
        ChangeBinding(binding);
        var unbound = BattleTurnModel.EndTurn(binding.Deserialize<BattleTurnState>(ModelJson.Options)!);
        Require(changedBindings > 0 && (!unbound.Supported || unbound.State!.Spawn.NextUnitId == 28),
            "Wrong equipment binding discarded the preview identity.");
        Console.WriteLine("STEWARD-WHELP-PREVIEW-ID PASS: 22 native inputs/full state/RNG, historical allocator mismatch, four immutable branches, five source controls and equipment binding; independent native replay passed.");
    }

    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

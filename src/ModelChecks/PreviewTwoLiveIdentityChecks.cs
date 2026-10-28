using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PreviewTwoLiveIdentityChecks
{
    private const string Reason = "Native temporary preview birth identity differs with two live Titanskin sources.";
    internal static void Native(FixtureValue source, FixtureValue? replay = null)
    {
        Require(!source.GetProperty("NativeCompleted").GetBoolean(), "Expected the original partial capture.");
        var rows = source.GetProperty("Rows").EnumerateArray().ToArray();
        Require(rows.Length == 21 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing original identity mismatch.");
        var current = source.GetProperty("Request").GetProperty("Root").Deserialize<BattleTurnState>()!;
        for (int index = 0; index < rows.Length; index++)
        {
            var row = rows[index];
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            bool end = row.GetProperty("IsEndTurn").GetBoolean();
            var action = row.GetProperty("Action").Deserialize<PlayCardAction>();
            Require(NativeBranchObservationChecks.Key(current) == NativeBranchObservationChecks.Key(before), "Native prefix root differs at " + index);
            current = BattlePlannerChecks.Replay(before, [new BattleSearchStep(action, end)], out var outcome);
            Require(outcome == RoomOutcome.Exchanged && NativeBranchObservationChecks.Key(current) == NativeBranchObservationChecks.Key(actual),
                "Repaired native successor differs at " + index);
        }
        var boundary = rows[^1];
        var root = boundary.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actualBoundary = boundary.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var historical = boundary.GetProperty("Expected").Deserialize<BattleTurnState>()!;
        Require(historical.Spawn.Train.Context!.NextUnitId == 27 && actualBoundary.Spawn.Train.Context!.NextUnitId == 26,
            "Missing historical identity difference.");
        JsonNode corrected = JsonSerializer.SerializeToNode(historical, ModelJson.Options)!;
        int correctedCount = 0;
        int removedPoints = 0;
        void Correct(JsonNode? node)
        {
            if (node is JsonObject obj)
            {
                if (obj["NextUnitId"]?.GetValueKind() == JsonValueKind.Number && obj["NextUnitId"]!.GetValue<int>() == 27)
                { obj["NextUnitId"] = 26; correctedCount++; }
                if (obj["SpawnPoints"] is JsonObject points && points["Units"] is JsonArray units)
                    for (int index = units.Count - 1; index >= 0; index--)
                        if (units[index] is JsonObject unit && unit["UnitId"]?.GetValue<int>() == 26 &&
                            unit["SpawnedInPreview"]?.GetValue<bool>() == true && unit["Current"] == null && unit["LastKnown"] == null)
                        { units.RemoveAt(index); removedPoints++; }
                foreach (var entry in obj.ToArray()) Correct(entry.Value);
            }
            else if (node is JsonArray array) foreach (var child in array) Correct(child);
        }
        Correct(corrected);
        string correctedKey = NativeBranchObservationChecks.Key(corrected.Deserialize<BattleTurnState>(ModelJson.Options)!);
        string actualKey = NativeBranchObservationChecks.Key(actualBoundary);
        Require(correctedCount > 0 && removedPoints > 0 && correctedKey == actualKey, "Historical mismatch extends beyond the preview identity: " + ModelJson.Difference(correctedKey, actualKey));
        var context = actualBoundary.Spawn.Train.Context!;
        var frame = context.Enchantments!.Frame(context);
        var preview = new EnchantmentCombatState(frame.Train, frame.RetainedUnits, frame.EnchanterIds,
            frame.AllowUpdates, frame.Updating, true, frame.TestRng, frame.TemporaryPreviewUnitIds);
        Require(frame.EnchanterIds.SequenceEqual([11, 15, 25]) && !TroopAuraModel.OnlyFragileBuffSource(preview) &&
            TroopAuraModel.OnlyFragileBuffSource(preview, allowTwoLivePreview: true) &&
            root.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == 21 &&
                PreviewBirthModel.IsOriginalGreenRecruit(unit, root.Spawn.Train.Context!)), "Missing scoped native observer signature.");
        var unknown = new EnchantmentCombatState(frame.Train, frame.RetainedUnits, [11, 15, 25, 23],
            frame.AllowUpdates, frame.Updating, true, frame.TestRng, frame.TemporaryPreviewUnitIds);
        Require(!TroopAuraModel.OnlyFragileBuffSource(unknown, allowTwoLivePreview: true), "Unknown preview observer escaped its guard.");
        JsonNode altered = JsonSerializer.SerializeToNode(root, ModelJson.Options)!;
        void RenameRecruit(JsonNode? node)
        {
            if (node is JsonObject obj)
            {
                if (obj["AssetKey"]?.GetValueKind() == JsonValueKind.String && obj["AssetKey"]!.GetValue<string>() == "MonsterGenerousHost")
                    obj["AssetKey"] = "unknown";
                foreach (var entry in obj.ToArray()) RenameRecruit(entry.Value);
            }
            else if (node is JsonArray array) foreach (var child in array) RenameRecruit(child);
        }
        RenameRecruit(altered);
        var changed = BattleTurnModel.EndTurn(altered.Deserialize<BattleTurnState>(ModelJson.Options)!);
        Require(!changed.Supported || changed.State!.Spawn.Train.Context!.NextUnitId != 26,
            "Unknown recruit incorrectly received the preview identity correction.");
        string parent = JsonSerializer.Serialize(root, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(root);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actualBoundary),
                "Preview identity differs: " + result.UnsupportedReason);
        });
        Require(parent == JsonSerializer.Serialize(root, ModelJson.Options), "EndTurn mutated its parent.");
        if (replay.HasValue)
        {
            var repeated = NativeBranchObservationChecks.Replay(replay.Value, Reason);
            Require(repeated.Length == rows.Length, "Independent input count differs.");
            for (int index = 0; index < rows.Length; index++)
                foreach (string field in new[] { "Before", "Actual" })
                    Require(NativeBranchObservationChecks.Key(rows[index].GetProperty(field).Deserialize<BattleTurnState>()!) ==
                        NativeBranchObservationChecks.Key(repeated[index].GetProperty(field).Deserialize<BattleTurnState>()!),
                        "Independent native " + field + " differs at " + index);
        }
        Console.WriteLine("PREVIEW-TWO-LIVE-IDENTITY PASS: 21 native inputs/full state/RNG, historical allocator difference, four immutable branches; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InactiveHeavyInfernoNewMoonEntryChecks
{
    private const string Reason = "Inactive enemy room-entry moon understanding requires native calibration.";

    internal static void Native(FixtureValue fixture, FixtureValue? replay = null)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason, boundaryOutcome: RoomOutcome.PlayerDefeated);
        Require(rows.Length == 48 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing native prefix.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        foreach (int id in new[] { 46, 47 })
        {
            var old = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == id);
            var next = actual.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == id);
            Require(old.AssetKey == "HeavyT3_PyreDamageIfUnblocked" && old.MoonPhaseMask == 6 && next.MoonPhaseMask == 5 &&
                next.Modifiers!.Upgrades.Any(upgrade => upgrade.AssetKey == "InfernoRoom"), "Missing new moon entry witness: " + id);
        }
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && result.Outcome == RoomOutcome.PlayerDefeated &&
                NativeBranchObservationChecks.Key(result.State!, true) == NativeBranchObservationChecks.Key(actual, true),
                "New moon room entry differs: " + result.UnsupportedReason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "EndTurn mutated its parent.");
        foreach (string control in new[] { "HeavyT3_PyreDamageIfUnblocked", "InfernoRoom" })
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
            Require(!BattleTurnModel.EndTurn(altered.Deserialize<BattleTurnState>(ModelJson.Options)!).Supported,
                "Unknown entry signature escaped its guard: " + control);
        }
        JsonNode actor = JsonSerializer.SerializeToNode(before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 46), ModelJson.Options)!;
        RoomAttachmentUnitSyncResult Move(CombatUnit unit)
        {
            var context = before.Spawn.Train.Context!;
            var prior = new TrainCombatState([new(0, false, [unit], [], context), new(1, false, [], [], context)], [], 7, context);
            var moved = new TrainCombatState([new(0, false, [], [], context), new(1, false, [unit], [], context)], [], 7, context);
            return RoomAttachmentUnitModel.SynchronizeMovement(prior, moved);
        }
        var calibrated = Move(actor.Deserialize<CombatUnit>(ModelJson.Options)!);
        Require(calibrated.Supported && calibrated.State!.Rooms[1].Units.Single().MoonPhaseMask == 5,
            "Original isolated new moon entry differs: " + calibrated.UnsupportedReason);
        foreach (string control in new[] { "AssetKey", "BaseAttack", "MaxHealth", "FireCount" })
        {
            JsonNode unit = actor.DeepClone();
            if (control == "AssetKey") unit[control] = "HeavyT3_Basic";
            else if (control == "FireCount") unit["Triggers"]![0]!["FireCount"] = 2;
            else unit[control] = unit[control]!.GetValue<int>() + 1;
            Require(!Move(unit.Deserialize<CombatUnit>(ModelJson.Options)!).Supported,
                "Changed enemy signature escaped its isolated movement guard: " + control);
        }
        if (replay.HasValue) { Native(replay.Value); NativeBranchObservationChecks.Pair(fixture, replay.Value); }
        Console.WriteLine("INACTIVE-HEAVY-INFERNO-NEW-MOON-ENTRY PASS: 47 native inputs/terminal state/RNG, two enemy masks 6->5, four immutable branches and six guards; independentPair=" + replay.HasValue);
    }

    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

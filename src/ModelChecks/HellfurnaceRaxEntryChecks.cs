using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HellfurnaceRaxEntryChecks
{
    private const string Reason = "Inactive enemy room-entry moon understanding requires native calibration.";

    internal static void Native(FixtureValue fixture)
    {
        FixtureValue[] rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        BattleTurnState before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        RoomCapacityAttachmentState furnace = before.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 2).Attachments!.Single();
        CombatUnit unit = before.Spawn.Train.Rooms[1].Units.Single(item => item.Id == 30);
        CombatUnit moved = actual.Spawn.Train.Rooms[2].Units.Single(item => item.Id == 30);
        Require(rows.Length == 40 && before.Spawn.Turn == 8 && actual.Spawn.Turn == 9 &&
            unit.Health == 1056 && unit.MoonPhaseMask == 6 && moved.Health == 1054 && moved.MoonPhaseMask == 5,
            "Native Rax entry boundary changed.");
        JsonNode mutant = JsonSerializer.SerializeToNode(unit, ModelJson.Options)!;
        bool Accept() => RoomAttachmentUnitModel.OriginalHellfurnaceEntry(furnace, mutant.Deserialize<CombatUnit>(ModelJson.Options)!, 1);
        Require(Accept(), "Native Rax entry rejected.");
        mutant["Health"] = 0; Require(!Accept(), "Dead Rax accepted.");
        mutant["Health"] = unit.Health; mutant["SpawnerCardId"] = 1; Require(!Accept(), "Unknown spawning card accepted.");
        mutant["SpawnerCardId"] = 0; mutant["BaseAttack"] = unit.BaseAttack + 1; Require(!Accept(), "Unknown attack accepted.");
        mutant["BaseAttack"] = unit.BaseAttack; mutant["Triggers"]!.AsArray()[0]!["FireCount"] = 2;
        Require(!Accept(), "Unknown trigger accepted.");
        Require(!RoomAttachmentUnitModel.OriginalHellfurnaceEntry(furnace, unit, 2) &&
            !RoomAttachmentUnitModel.OriginalHellfurnaceEntry(furnace, unit, 3), "Unobserved Rax entry phase accepted.");
        JsonNode altered = JsonSerializer.SerializeToNode(furnace, ModelJson.Options)!;
        altered["DataId"] = "unknown";
        Require(!RoomAttachmentUnitModel.OriginalHellfurnaceEntry(altered.Deserialize<RoomCapacityAttachmentState>(ModelJson.Options)!, unit, 1),
            "Unknown attachment accepted.");
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options), expected = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            BattleTurnResult result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == expected, "Concurrent Rax entry differs.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Rax entry mutated its parent.");
        Console.WriteLine("HELLFURNACE-RAX-ENTRY PASS: 40 native inputs, complete successor/RNG, identity guards and four concurrent replays.");
    }

    internal static void Pair(FixtureValue original, FixtureValue repaired, FixtureValue? repaired2 = null, FixtureValue? original2 = null)
    {
        Native(original); Native(repaired);
        NativeBranchObservationChecks.Pair(original, repaired);
        if (repaired2.HasValue) { Native(repaired2.Value); NativeBranchObservationChecks.Pair(original, repaired2.Value); }
        if (original2.HasValue) { Native(original2.Value); NativeBranchObservationChecks.Pair(original2.Value, repaired); }
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

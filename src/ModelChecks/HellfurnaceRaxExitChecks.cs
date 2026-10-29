using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HellfurnaceRaxExitChecks
{
    private const string Reason = "Inactive enemy room-exit moon understanding requires native calibration.";

    internal static void Native(FixtureValue fixture)
    {
        FixtureValue[] rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        BattleTurnState before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        RoomCapacityAttachmentState furnace = before.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).Attachments!.Single();
        CombatUnit unit = before.Spawn.Train.Rooms[0].Units.Single(item => item.Id == 30);
        CombatUnit moved = actual.Spawn.Train.Rooms[1].Units.Single(item => item.Id == 30);
        Require(rows.Length == 36 && before.Spawn.Turn == 7 && actual.Spawn.Turn == 8 &&
            unit.Health == 1100 && unit.MoonPhaseMask == 5 && moved.Health == 1056 && moved.MoonPhaseMask == 6,
            "Native Rax exit boundary changed.");
        JsonNode mutant = JsonSerializer.SerializeToNode(unit, ModelJson.Options)!;
        bool Accept() => RoomAttachmentUnitModel.OriginalHellfurnaceExit(furnace, mutant.Deserialize<CombatUnit>(ModelJson.Options)!, 2);
        Require(Accept(), "Native Rax exit rejected.");
        mutant["Health"] = 0; Require(!Accept(), "Dead Rax accepted.");
        mutant["Health"] = unit.Health; mutant["SpawnerCardId"] = 1; Require(!Accept(), "Unknown spawning card accepted.");
        mutant["SpawnerCardId"] = 0; mutant["BaseAttack"] = unit.BaseAttack + 1; Require(!Accept(), "Unknown attack accepted.");
        mutant["BaseAttack"] = unit.BaseAttack; mutant["Triggers"]!.AsArray()[0]!["FireCount"] = 2;
        Require(!Accept(), "Unknown trigger accepted.");
        Require(!RoomAttachmentUnitModel.OriginalHellfurnaceExit(furnace, unit, 1) &&
            !RoomAttachmentUnitModel.OriginalHellfurnaceExit(furnace, unit, 3), "Unobserved Rax exit phase accepted.");
        JsonNode altered = JsonSerializer.SerializeToNode(furnace, ModelJson.Options)!;
        altered["DataId"] = "unknown";
        Require(!RoomAttachmentUnitModel.OriginalHellfurnaceExit(altered.Deserialize<RoomCapacityAttachmentState>(ModelJson.Options)!, unit, 2),
            "Unknown attachment accepted.");
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options), expected = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            BattleTurnResult result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == expected, "Concurrent Rax exit differs.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Rax exit mutated its parent.");
        Console.WriteLine("HELLFURNACE-RAX-EXIT PASS: 36 native inputs, complete successor/RNG, identity guards and four concurrent replays.");
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

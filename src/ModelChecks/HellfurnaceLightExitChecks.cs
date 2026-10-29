using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HellfurnaceLightExitChecks
{
    private const string Reason = "Inactive enemy room-exit moon understanding requires native calibration.";

    internal static void Native(FixtureValue fixture)
    {
        FixtureValue[] rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        BattleTurnState before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        RoomCapacityAttachmentState furnace = before.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).Attachments!.Single();
        Require(rows.Length == 24 && before.Spawn.Turn == 4 && actual.Spawn.Turn == 5, "Missing Hellfurnace light exit boundary.");
        foreach (int id in new[] { 17, 18, 19 })
        {
            CombatUnit unit = before.Spawn.Train.Rooms[0].Units.Single(item => item.Id == id);
            CombatUnit moved = actual.Spawn.Train.Rooms[1].Units.Single(item => item.Id == id);
            Require(unit.MoonPhaseMask == 6 && moved.MoonPhaseMask == 5 && moved.Health == (id == 17 ? 282 : unit.Health),
                "Native Hellfurnace light exit changed for " + id);
            JsonNode mutant = JsonSerializer.SerializeToNode(unit, ModelJson.Options)!;
            bool Accept() => RoomAttachmentUnitModel.OriginalHellfurnaceExit(furnace, mutant.Deserialize<CombatUnit>(ModelJson.Options)!, 1);
            Require(Accept(), "Native Hellfurnace light exit rejected for " + id);
            mutant["Health"] = 0; Require(!Accept(), "Dead unit accepted.");
            mutant["Health"] = unit.Health; mutant["SpawnerCardId"] = 1; Require(!Accept(), "Unknown spawning card accepted.");
            mutant["SpawnerCardId"] = 0; mutant["BaseAttack"] = unit.BaseAttack + 1; Require(!Accept(), "Unknown attack accepted.");
            if (id != 17)
                Require(!RoomAttachmentUnitModel.OriginalHellfurnaceExit(furnace, unit, 3), "Unknown sweep phase accepted.");
        }
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options), expected = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            BattleTurnResult result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == expected, "Concurrent Hellfurnace light exit differs.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Hellfurnace light exit mutated its parent.");
        Console.WriteLine("HELLFURNACE-LIGHT-EXIT PASS: 24 native inputs, complete successor/RNG, identity guards and four concurrent replays.");
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

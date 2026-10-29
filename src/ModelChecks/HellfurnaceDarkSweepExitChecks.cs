using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HellfurnaceDarkSweepExitChecks
{
    private const string Reason = "Inactive enemy room-exit moon understanding requires native calibration.";

    internal static void Native(FixtureValue fixture)
    {
        FixtureValue[] rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        BattleTurnState before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        RoomCapacityAttachmentState furnace = before.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).Attachments!.Single();
        Require(rows.Length == 28 && before.Spawn.Turn == 5 && actual.Spawn.Turn == 6, "Missing Hellfurnace dark sweep exit boundary.");
        foreach (int id in new[] { 23, 24 })
        {
            CombatUnit unit = before.Spawn.Train.Rooms[0].Units.Single(item => item.Id == id);
            CombatUnit moved = actual.Spawn.Train.Rooms[1].Units.Single(item => item.Id == id);
            Require(unit.MoonPhaseMask == 5 && moved.MoonPhaseMask == 6 && moved.Health == 5, "Native sweep exit changed for " + id);
            JsonNode mutant = JsonSerializer.SerializeToNode(unit, ModelJson.Options)!;
            bool Accept() => RoomAttachmentUnitModel.OriginalHellfurnaceExit(furnace, mutant.Deserialize<CombatUnit>(ModelJson.Options)!, 2);
            Require(Accept(), "Native sweep exit rejected for " + id);
            mutant["Health"] = 0; Require(!Accept(), "Dead unit accepted.");
            mutant["Health"] = unit.Health; mutant["SpawnerCardId"] = 1; Require(!Accept(), "Unknown spawning card accepted.");
            mutant["SpawnerCardId"] = 0; mutant["BaseAttack"] = unit.BaseAttack + 1; Require(!Accept(), "Unknown attack accepted.");
            Require(!RoomAttachmentUnitModel.OriginalHellfurnaceExit(furnace, unit, 3), "Unknown moon phase accepted.");
        }
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options), expected = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            BattleTurnResult result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == expected, "Concurrent Hellfurnace sweep exit differs.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Hellfurnace sweep exit mutated its parent.");
        Console.WriteLine("HELLFURNACE-DARK-SWEEP-EXIT PASS: 28 native inputs, complete successor/RNG, identity guards and four concurrent replays.");
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

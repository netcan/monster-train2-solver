using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MushyRaxMovementChecks
{
    private const string Reason = "Inactive enemy room-exit moon understanding requires native calibration.";

    internal static void Native(FixtureValue fixture)
    {
        FixtureValue[] rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        BattleTurnState before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(rows.Length == 43 && before.Spawn.Turn == 7 && actual.Spawn.Turn == 8, "Missing native Rax movement boundary.");
        RoomCapacityAttachmentState attachment = before.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).Attachments!.Single();
        foreach (int id in new[] { 28, 29, 30 })
        {
            CombatUnit source = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == id);
            CombatUnit moved = actual.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == id);
            Require(source.MoonPhaseMask == 5 && moved.MoonPhaseMask == 6 && moved.Health == (id == 28 ? 200 : source.Health), "Native exit changed for " + id);
            JsonNode unit = JsonSerializer.SerializeToNode(source, ModelJson.Options)!;
            bool Accept() => RoomAttachmentUnitModel.OriginalMushyExit(attachment, unit.Deserialize<CombatUnit>(ModelJson.Options)!);
            Require(Accept(), "Native signature rejected for " + id);
            unit["SpawnerCardId"] = 1;
            Require(!Accept(), "Unknown spawning card accepted for " + id);
            unit["SpawnerCardId"] = 0;
            unit["BaseAttack"] = source.BaseAttack + 1;
            Require(!Accept(), "Unknown attack signature accepted for " + id);
            unit["BaseAttack"] = source.BaseAttack;
            if (id == 28) unit["Triggers"]![0]!["Effects"]![0]!["Value"] = 6;
            else if (id == 29) unit["Triggers"]![0]!["Effects"]![1]!["Action"]!["Statuses"]![0]!["Stacks"] = 21;
            else unit["Statuses"]![0]!["Stacks"] = 2;
            Require(!Accept(), "Unknown native effect accepted for " + id);
        }
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options), expected = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            BattleTurnResult result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == expected, "Concurrent Rax movement differs.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Rax movement mutated its parent.");
        Console.WriteLine("MUSHY-RAX-MOVEMENT PASS: 43 native inputs, complete successor/RNG, signature controls and four concurrent replays.");
    }

    internal static void Pair(FixtureValue original, FixtureValue repaired, FixtureValue? repaired2 = null)
    {
        Native(original); Native(repaired);
        NativeBranchObservationChecks.Pair(original, repaired);
        if (repaired2.HasValue) { Native(repaired2.Value); NativeBranchObservationChecks.Pair(original, repaired2.Value); }
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

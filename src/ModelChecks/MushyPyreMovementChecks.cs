using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MushyPyreMovementChecks
{
    private const string Reason = "Inactive enemy room-exit moon understanding requires native calibration.";

    internal static void Native(FixtureValue fixture)
    {
        FixtureValue[] rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        BattleTurnState before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(rows.Length == 34 && before.Spawn.Turn == 7 && actual.Spawn.Turn == 8, "Missing native movement boundary.");
        CombatUnit heavy = before.Spawn.Train.Rooms[2].Units.Single(unit => unit.Id == 22);
        Require(heavy.AssetKey == "HeavyT2_Basic" && heavy.Health == 51 && heavy.MaxHealth == 120 && heavy.MoonPhaseMask == 5,
            "Missing wounded native heavy before combat.");
        Require(!actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == 22),
            "Native heavy did not leave for the pyre.");
        Require(actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == 8).MoonPhaseMask == 5,
            "Native boss exit mask changed.");
        RoomCapacityAttachmentState attachment = before.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 2).Attachments!.Single();
        foreach (int id in new[] { 8, 22 })
        {
            CombatUnit source = before.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.Id == id);
            JsonNode unit = JsonSerializer.SerializeToNode(source, ModelJson.Options)!;
            unit["Health"] = id == 22 ? 11 : 1653;
            Require(RoomAttachmentUnitModel.OriginalMushyExit(attachment, unit.Deserialize<CombatUnit>(ModelJson.Options)!), "Native signature rejected for " + id);
            unit["Health"] = (int)unit["Health"]! + 1;
            Require(!RoomAttachmentUnitModel.OriginalMushyExit(attachment, unit.Deserialize<CombatUnit>(ModelJson.Options)!), "Uncalibrated health accepted for " + id);
        }
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options), expected = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            BattleTurnResult result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == expected, "Concurrent pyre movement differs.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Pyre movement mutated its parent.");
        Console.WriteLine("MUSHY-PYRE-MOVEMENT PASS: 34 native inputs, complete successor/RNG, health controls and four concurrent replays.");
    }

    internal static void Pair(FixtureValue original, FixtureValue repaired, FixtureValue? repaired2 = null)
    {
        Native(original); Native(repaired);
        NativeBranchObservationChecks.Pair(original, repaired);
        if (repaired2.HasValue) { Native(repaired2.Value); NativeBranchObservationChecks.Pair(original, repaired2.Value); }
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

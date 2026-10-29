using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MushyFeederHealthChecks
{
    private const string Reason = "Inactive enemy room-exit moon understanding requires native calibration.";

    internal static void Native(FixtureValue fixture)
    {
        FixtureValue[] rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        BattleTurnState before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(rows.Length == 35 && before.Spawn.Turn == 8 && actual.Spawn.Turn == 9, "Missing native feeder boundary.");
        CombatUnit feeder = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 37);
        Require(feeder.AssetKey == "Boss_Level6_FeedDamageShield" && feeder.Health == 60 && feeder.MoonPhaseMask == 6, "Missing native feeder.");
        CombatUnit moved = actual.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == 37);
        Require(moved.Health == 12 && moved.MoonPhaseMask == 5, "Native feeder exit changed.");
        RoomCapacityAttachmentState attachment = before.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).Attachments!.Single();
        JsonNode unit = JsonSerializer.SerializeToNode(feeder, ModelJson.Options)!;
        bool Accept() => RoomAttachmentUnitModel.OriginalMushyExit(attachment, unit.Deserialize<CombatUnit>(ModelJson.Options)!);
        foreach (int health in new[] { 1, 12, 20, 40, 60 }) { unit["Health"] = health; Require(Accept(), "Valid feeder health rejected."); }
        foreach (int health in new[] { 0, 61 }) { unit["Health"] = health; Require(!Accept(), "Invalid feeder health accepted."); }
        unit["Health"] = 12;
        unit["SpawnerCardId"] = 1;
        Require(!Accept(), "Unknown feeder spawning card accepted.");
        unit["SpawnerCardId"] = 0;
        unit["Triggers"]![0]!["Effects"]![1]!["Value"] = 21;
        Require(!Accept(), "Unknown feeder trigger accepted.");
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options), expected = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            BattleTurnResult result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == expected, "Concurrent feeder movement differs.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Feeder movement mutated its parent.");
        Console.WriteLine("MUSHY-FEEDER-HEALTH PASS: 35 native inputs, complete successor/RNG, signature controls and four concurrent replays.");
    }

    internal static void Pair(FixtureValue original, FixtureValue repaired, FixtureValue? repaired2 = null)
    {
        Native(original); Native(repaired);
        NativeBranchObservationChecks.Pair(original, repaired);
        if (repaired2.HasValue) { Native(repaired2.Value); NativeBranchObservationChecks.Pair(original, repaired2.Value); }
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

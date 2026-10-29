using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MushyHeavyT3ExitChecks
{
    private const string Reason = "Inactive enemy room-exit moon understanding requires native calibration.";

    internal static void Native(FixtureValue fixture)
    {
        FixtureValue[] rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        BattleTurnState before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CombatUnit heavy = before.Spawn.Train.Rooms[2].Units.Single(unit => unit.Id == 16);
        RoomCapacityAttachmentState mushy = before.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 2).Attachments!.Single();
        Require(rows.Length == 37 && before.Spawn.Turn == 6 && actual.Spawn.Turn == 7 &&
            heavy is { AssetKey: "HeavyT3_Basic", Health: 153, MoonPhaseMask: 6 } &&
            actual.Spawn.Train.Rooms.All(room => room.Units.All(unit => unit.Id != 16)) &&
            actual.Spawn.Train.Rooms[2].Units.Count(unit => unit.Id is 20 or 21 && unit.MoonPhaseMask == 5) == 2,
            "Missing native wounded HeavyT3 exit boundary.");
        JsonNode unit = JsonSerializer.SerializeToNode(heavy, ModelJson.Options)!;
        bool Accept() => RoomAttachmentUnitModel.OriginalMushyExit(mushy, unit.Deserialize<CombatUnit>(ModelJson.Options)!);
        Require(Accept(), "Native wounded HeavyT3 rejected.");
        unit["Health"] = 70; Require(Accept(), "Native post-combat health rejected.");
        unit["Health"] = 0; Require(!Accept(), "Dead HeavyT3 accepted.");
        unit["Health"] = 326; Require(!Accept(), "Invalid HeavyT3 health accepted.");
        unit["Health"] = 70; unit["SpawnerCardId"] = 1; Require(!Accept(), "Unknown spawning card accepted.");
        unit["SpawnerCardId"] = 0; unit["BaseAttack"] = 9; Require(!Accept(), "Unknown attack signature accepted.");
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options), expected = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            BattleTurnResult result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == expected, "Concurrent HeavyT3 exit differs.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "HeavyT3 exit mutated its parent.");
        Console.WriteLine("MUSHY-HEAVY-T3-EXIT PASS: 37 native inputs, complete successor/RNG, identity guards and four concurrent replays.");
    }

    internal static void Pair(FixtureValue original, FixtureValue repaired, FixtureValue? repaired2 = null)
    {
        Native(original); Native(repaired);
        NativeBranchObservationChecks.Pair(original, repaired);
        if (repaired2.HasValue) { Native(repaired2.Value); NativeBranchObservationChecks.Pair(original, repaired2.Value); }
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

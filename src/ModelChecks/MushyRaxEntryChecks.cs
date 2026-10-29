using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MushyRaxEntryChecks
{
    private const string Reason = "Inactive enemy room-entry moon understanding requires native calibration.";

    internal static void Native(FixtureValue fixture)
    {
        FixtureValue[] rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        BattleTurnState before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        RoomCapacityAttachmentState mushy = before.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 1).Attachments!.Single();
        CombatUnit source = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == 29);
        CombatUnit moved = actual.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == 29);
        Require(rows.Length == 40 && before.Spawn.Turn == 7 && actual.Spawn.Turn == 8 &&
            source is { AssetKey: "Boss_Level7_Rax", Health: 1100, MoonPhaseMask: 5 } &&
            moved is { Health: 1038, MoonPhaseMask: 6 } &&
            actual.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == 28) is { Health: 90, MoonPhaseMask: 6 },
            "Missing native Rax dark entry boundary.");
        JsonNode unit = JsonSerializer.SerializeToNode(source, ModelJson.Options)!;
        bool Accept() => RoomAttachmentUnitModel.OriginalMushyDarkEntry(mushy, unit.Deserialize<CombatUnit>(ModelJson.Options)!);
        Require(Accept(), "Native Rax entry rejected.");
        unit["Health"] = 1038; Require(Accept(), "Native post-combat health rejected.");
        unit["Health"] = 0; Require(!Accept(), "Dead Rax accepted.");
        unit["Health"] = 1101; Require(!Accept(), "Invalid Rax health accepted.");
        unit["Health"] = 1038; unit["SpawnerCardId"] = 1; Require(!Accept(), "Unknown spawning card accepted.");
        unit["SpawnerCardId"] = 0; unit["BaseAttack"] = 10; Require(!Accept(), "Unknown attack accepted.");
        unit["BaseAttack"] = 9; unit["Triggers"]![0]!["Effects"]![1]!["Action"]!["Statuses"]![0]!["Stacks"] = 21;
        Require(!Accept(), "Unknown native effect accepted.");
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options), expected = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            BattleTurnResult result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == expected, "Concurrent Rax entry differs.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Rax entry mutated its parent.");
        Console.WriteLine("MUSHY-RAX-ENTRY PASS: 40 native inputs, complete successor/RNG, signature guards and four concurrent replays.");
    }

    internal static void Pair(FixtureValue original, FixtureValue repaired, FixtureValue? repaired2 = null)
    {
        Native(original); Native(repaired);
        NativeBranchObservationChecks.Pair(original, repaired);
        if (repaired2.HasValue) { Native(repaired2.Value); NativeBranchObservationChecks.Pair(original, repaired2.Value); }
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

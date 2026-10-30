using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class HellfurnaceDarkSweepEntryChecks
{
    private const string Reason = "Inactive enemy room-entry moon understanding requires native calibration.";

    internal static void Native(FixtureValue fixture)
    {
        FixtureValue[] rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        BattleTurnState before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        RoomCapacityAttachmentState furnace = before.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 2).Attachments!.Single();
        CombatUnit unit = before.Spawn.Train.Rooms[1].Units.Single(item => item.Id == 14);
        CombatUnit moved = actual.Spawn.Train.Rooms[2].Units.Single(item => item.Id == 14);
        Require(rows.Length == 23 && before.Spawn.Turn == 3 && actual.Spawn.Turn == 4 &&
            before.Spawn.Train.Context.QueryFrame is { MoonPhaseTrackingActive: false, MoonPhase: 2 } &&
            unit is { AssetKey: "AttackerT3_Sweep", Health: 5, MoonPhaseMask: 5 } &&
            moved is { Health: 5, MoonPhaseMask: 6 }, "Native dark sweep entry changed.");
        JsonNode mutant = JsonSerializer.SerializeToNode(unit, ModelJson.Options)!;
        bool Accept() => RoomAttachmentUnitModel.OriginalHellfurnaceEntry(furnace, mutant.Deserialize<CombatUnit>(ModelJson.Options)!, 2);
        Require(Accept(), "Native sweep entry signature rejected.");
        mutant["Health"] = 0; Require(!Accept(), "Dead unit accepted.");
        mutant["Health"] = unit.Health; mutant["SpawnerCardId"] = 1; Require(!Accept(), "Unknown spawning card accepted.");
        mutant["SpawnerCardId"] = 0; mutant["BaseAttack"] = unit.BaseAttack + 1; Require(!Accept(), "Unknown attack accepted.");
        JsonNode attachment = JsonSerializer.SerializeToNode(furnace, ModelJson.Options)!;
        attachment["StatusStackBonuses"]![0]!["Amount"] = 4;
        Require(!RoomAttachmentUnitModel.OriginalHellfurnaceEntry(attachment.Deserialize<RoomCapacityAttachmentState>(ModelJson.Options)!, unit, 2),
            "Unknown room status bonus accepted.");
        Require(!RoomAttachmentUnitModel.OriginalHellfurnaceEntry(furnace, unit, 3), "Unknown moon phase accepted.");
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options), expected = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            BattleTurnResult result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == expected, "Concurrent dark sweep entry differs.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Dark sweep entry mutated its parent.");
        Console.WriteLine("HELLFURNACE-DARK-SWEEP-ENTRY PASS: 23 native inputs, complete successor/RNG, signature guards and four concurrent replays.");
    }

    internal static void Pair(FixtureValue original, FixtureValue repaired, FixtureValue repaired2, FixtureValue original2)
    {
        Native(original); Native(original2); Native(repaired); Native(repaired2);
        NativeBranchObservationChecks.Pair(original, repaired);
        NativeBranchObservationChecks.Pair(original2, repaired2);
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

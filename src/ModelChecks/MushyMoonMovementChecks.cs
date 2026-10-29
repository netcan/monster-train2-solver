using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class MushyMoonMovementChecks
{
    private const string Reason = "Inactive enemy room-exit moon understanding requires native calibration.";

    internal static void Native(FixtureValue fixture)
    {
        FixtureValue[] rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        BattleTurnState before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(rows.Length == 26 && before.Spawn.Turn == 5 && actual.Spawn.Turn == 6, "Missing native movement boundary.");
        CombatUnit[] enemies = actual.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => unit.Team == CombatTeam.Enemy).ToArray();
        Require(enemies.Single(unit => unit.Id == 24).MoonPhaseMask == 6 && enemies.Single(unit => unit.Id == 25).MoonPhaseMask == 6 &&
            enemies.Single(unit => unit.Id == 8).MoonPhaseMask == 5, "Native room exit/entry masks changed.");
        RoomCapacityAttachmentState attachment = before.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0).Attachments!.Single();
        Require(RoomAttachmentUnitModel.OriginalMushy(attachment), "Missing original Mushy Room signature.");
        foreach (string control in new[] { "room", "upgrade", "status", "target" })
        {
            JsonNode changed = JsonSerializer.SerializeToNode(attachment, ModelJson.Options)!;
            if (control == "room") changed["DataId"] = "unknown";
            if (control == "upgrade") changed["CharacterUpgrades"]![0]!["Stats"]!["Damage"] = 1;
            if (control == "status") changed["PostCombat"]!["Effects"]![0]!["Statuses"]![0]!["Stacks"] = 4;
            if (control == "target") changed["PostCombat"]!["Effects"]![0]!["AllowEnemy"] = false;
            Require(!RoomAttachmentUnitModel.OriginalMushy(changed.Deserialize<RoomCapacityAttachmentState>(ModelJson.Options)!),
                "Uncalibrated Mushy Room accepted: " + control);
        }
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options), expected = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            BattleTurnResult result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == expected, "Concurrent moon movement differs.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Moon movement mutated its parent.");
        Console.WriteLine("MUSHY-MOON-MOVEMENT PASS: 26 native inputs, exit/entry masks, full successor/RNG, signature controls and four concurrent replays.");
    }

    internal static void Pair(FixtureValue original, FixtureValue repaired, FixtureValue? repaired2 = null)
    {
        Native(original); Native(repaired);
        NativeBranchObservationChecks.Pair(original, repaired);
        if (repaired2.HasValue) { Native(repaired2.Value); NativeBranchObservationChecks.Pair(original, repaired2.Value); }
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

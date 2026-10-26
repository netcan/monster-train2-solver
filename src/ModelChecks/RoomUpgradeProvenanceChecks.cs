using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RoomUpgradeProvenanceChecks
{
    internal static void Native(FixtureValue counterexample, FixtureValue repaired)
    {
        var original = counterexample.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        var rows = NativeBranchObservationChecks.Replay(repaired,
            "Retained room upgrade provenance survives attachment replacement.");
        Require(original.Length == 36 && rows.Length == 36, "Missing36-input native counterexample/replay.");
        for (int i = 0; i < rows.Length; i++)
        {
            foreach (var pair in new[] { ("Root", "Before"), ("Expected", "Actual") })
                Require(Key(original[i].GetProperty(pair.Item1)) == Key(rows[i].GetProperty(pair.Item2)),
                    "Fresh capture changed original " + pair.Item1 + " at " + i);
            Require(original[i].GetProperty("Action").ContentEquals(rows[i].GetProperty("Action")) &&
                original[i].GetProperty("IsEndTurn").ContentEquals(rows[i].GetProperty("IsEndTurn")),
                "Fresh native process changed input at " + i);
        }
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(before.Spawn.Turn == 5 && action.CardInstanceId == 25 && action.RoomIndex == 0 &&
            before.Spawn.Train.Context!.FindCard(25)!.DataId == "6bc6afc8-a113-4f03-b090-d4436f41d822" &&
            after.Spawn.Train.Context!.RoomCapacities!.Single(room => room.RoomIndex == 0)
                .Attachments!.Any(attachment => attachment.AssetKey == "FightClub"), "Wrong room replacement boundary.");
        var retained = after.Spawn.Train.Context!.Enchantments!.RetainedUnits.Single(actor => actor.Unit.Id == 24).Unit;
        Require(retained.Health == 0 && retained.AssetKey == "AttackerT3_Sweep" && retained.DeathState!.IsDestroyed == true,
            "Missing original retained dead enemy.");
        // Diagnose the immutable historical archive only. Production comparisons
        // continue to compare every field; no runtime normalization is allowed.
        var oldActual = JsonSerializer.SerializeToNode(original[^1].GetProperty("Actual").ToObjectGraph())!;
        int changes = 0;
        var train = oldActual["Spawn"]!["Train"]!;
        foreach (var context in new[] { train["Context"]! }.Concat(train["Rooms"]!.AsArray().Select(room => room!["Context"]!)))
        {
            var unit = context["Enchantments"]!["RetainedUnits"]!.AsArray()
                .Single(actor => actor!["Unit"]!["Id"]!.GetValue<int>() == 24)!["Unit"]!;
            var upgrade = unit["Modifiers"]!["Upgrades"]![0]!;
            Require(upgrade["DataId"]!.GetValue<string>() == "68d8696e-ec20-425e-b23d-8a0b9da0dfaf" &&
                upgrade["AssetKey"]!.GetValue<string>() == "EnchantedChamber" &&
                upgrade["ExternalInteractions"]!.AsArray().Count == 1 &&
                upgrade["ExternalInteractions"]![0]!.GetValue<string>() == "Upgrade room modifiers",
                "Historical discrepancy changed beyond the attachment origin marker.");
            upgrade["ExternalInteractions"] = new JsonArray(); changes++;
        }
        Require(changes == 5 && NativeBranchObservationChecks.Key(oldActual.Deserialize<BattleTurnState>(ModelJson.Options)!) ==
            NativeBranchObservationChecks.Key(after), "Historical capture differs beyond five copies of one provenance marker.");
        Require(Key(original[^1].GetProperty("Actual")) != NativeBranchObservationChecks.Key(after),
            "Original counterexample must retain its capture discrepancy.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ =>
        {
            var branch = BattlePlannerChecks.Replay(before, [new BattleSearchStep(action, false)], out var outcome);
            Require(outcome == RoomOutcome.Exchanged && NativeBranchObservationChecks.Key(branch) == NativeBranchObservationChecks.Key(after),
                "Detached room replacement branch differs.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Room replacement changed parent.");
        Console.WriteLine("ROOM-UPGRADE-PROVENANCE PASS:36 unchanged native inputs/full state/forecast/RNG; dead unit24 retains attachment origin; original five marker discrepancies preserved; four immutable branches.");
    }

    private static string Key(FixtureValue state) => NativeBranchObservationChecks.Key(state.Deserialize<BattleTurnState>()!);
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

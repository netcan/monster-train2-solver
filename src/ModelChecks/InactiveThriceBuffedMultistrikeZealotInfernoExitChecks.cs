using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InactiveThriceBuffedMultistrikeZealotInfernoExitChecks
{
    private const string Reason = "Inactive enemy room-exit moon understanding requires native calibration.";

    internal static void Native(FixtureValue fixture, FixtureValue? replay = null, bool twelveBuffs = false)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason,
            boundaryOutcome: twelveBuffs ? RoomOutcome.PlayerDefeated : RoomOutcome.Exchanged);
        Require(rows.Length == (twelveBuffs ? 52 : 37) && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing native chain.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var origin = before.Spawn.Train.Rooms[twelveBuffs ? 2 : 0].Units.Single(unit => unit.Id == (twelveBuffs ? 32 : 31));
        var native = actual.Spawn.Train.Rooms[twelveBuffs ? 3 : 1].Units.Single(unit => unit.Id == (twelveBuffs ? 32 : 31));
        Require(origin is { AssetKey: "HeavyT2_Zealot", MoonPhaseMask: 5,
            HordeDefinition: { Attack: 3, Health: 110 } } &&
            (twelveBuffs ? origin is { BaseAttack: 15, Health: 90, Modifiers.DamageBuff: 12 } : origin is { BaseAttack: 4, Modifiers.DamageBuff: 1 }) &&
            origin.Status("multistrike")?.Stacks == 1 && origin.Status("pyregel")?.Stacks == (twelveBuffs ? 10 : 2) &&
            (twelveBuffs ? native is { BaseAttack: 15, Health: 30, MoonPhaseMask: 6, Modifiers.DamageBuff: 12 } : native is { BaseAttack: 6, MoonPhaseMask: 6, Modifiers.DamageBuff: 3 }) &&
            origin.Modifiers!.Upgrades.Any(upgrade => upgrade.AssetKey == "InfernoRoom") &&
            native.Modifiers!.Upgrades.All(upgrade => upgrade.AssetKey != "InfernoRoom"), "Missing Zealot exit witness.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && result.Outcome == (twelveBuffs ? RoomOutcome.PlayerDefeated : RoomOutcome.Exchanged) &&
                NativeBranchObservationChecks.Key(result.State!, twelveBuffs) == NativeBranchObservationChecks.Key(actual, twelveBuffs),
                "Native exit differs: " + result.UnsupportedReason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "EndTurn mutated its parent.");
        JsonNode actorNode = JsonSerializer.SerializeToNode(origin, ModelJson.Options)!;
        actorNode["BaseAttack"] = twelveBuffs ? 15 : 6;
        actorNode["Modifiers"]!["DamageBuff"] = twelveBuffs ? 12 : 3;
        RoomAttachmentUnitSyncResult Move(CombatUnit unit)
        {
            var context = before.Spawn.Train.Context!;
            int floor = twelveBuffs ? 2 : 0;
            var prior = new TrainCombatState([new(floor, false, [unit], [], context), new(floor + 1, false, [], [], context)], [], 7, context);
            var moved = new TrainCombatState([new(floor, false, [], [], context), new(floor + 1, false, [unit], [], context)], [], 7, context);
            return RoomAttachmentUnitModel.SynchronizeMovement(prior, moved);
        }
        var calibrated = Move(actorNode.Deserialize<CombatUnit>(ModelJson.Options)!);
        Require(calibrated.Supported && calibrated.State!.Rooms[1].Units.Single().MoonPhaseMask == 6,
            "Original isolated movement did not refresh moon understanding: " + calibrated.UnsupportedReason);
        foreach (string control in new[] { "Identity", "Attack", "DamageBuff", "Horde", "Payload", "Multistrike" })
        {
            JsonNode node = actorNode.DeepClone();
            if (control == "Identity") node["AssetKey"] = "unknown";
            if (control == "Attack") node["BaseAttack"] = 8;
            if (control == "DamageBuff") node["Modifiers"]!["DamageBuff"] = 2;
            if (control == "Horde") node["HordeDefinition"]!["Attack"] = 4;
            if (control == "Payload") node["Triggers"]![0]!["Effects"]![0]!["Value"] = 2;
            if (control == "Multistrike") node["Statuses"]!.AsArray().Single(status => status!["Id"]!.GetValue<string>() == "multistrike")!["Stacks"] = 2;
            Require(!Move(node.Deserialize<CombatUnit>(ModelJson.Options)!).Supported,
                "Unknown Zealot escaped its isolated movement guard: " + control);
        }
        if (replay.HasValue) { Native(replay.Value, twelveBuffs: twelveBuffs); NativeBranchObservationChecks.Pair(fixture, replay.Value); }
        Console.WriteLine($"INACTIVE-BUFFED-MULTISTRIKE-ZEALOT-INFERNO-EXIT PASS: {rows.Length} native inputs/full state/RNG, mask5->6, four immutable branches/six isolated guards; twelveBuffs={twelveBuffs}; independentPair={replay.HasValue}");
    }

    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

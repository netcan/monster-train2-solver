using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InactiveOnceBuffedMultistrikeZealotInfernoExitChecks
{
    private const string Reason = "Inactive enemy room-exit moon understanding requires native calibration.";
    internal static void Native(FixtureValue fixture, FixtureValue? replay = null, bool alternative = false)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        Require(rows.Length == (alternative ? 29 : 25) && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Missing native chain.");
        var before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        int id = alternative ? 30 : 27;
        var origin = before.Spawn.Train.Rooms[0].Units.Single(unit => unit.Id == id);
        var native = actual.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == id);
        Require(origin is { AssetKey: "HeavyT2_Zealot", BaseAttack: 3, MoonPhaseMask: 5,
            HordeDefinition: { Attack: 3, Health: 110 }, Modifiers.DamageBuff: 0 } && origin.Status("multistrike")?.Stacks == 1 &&
            native is { BaseAttack: 4, MoonPhaseMask: 6, Modifiers.DamageBuff: 1 } &&
            origin.Modifiers!.Upgrades.Any(upgrade => upgrade.AssetKey == "InfernoRoom") &&
            native.Modifiers!.Upgrades.All(upgrade => upgrade.AssetKey != "InfernoRoom"), "Missing Zealot exit witness.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        Parallel.For(0, 4, _ => {
            var result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && result.Outcome == RoomOutcome.Exchanged &&
                NativeBranchObservationChecks.Key(result.State!) == NativeBranchObservationChecks.Key(actual),
                "Native exit differs: " + result.UnsupportedReason);
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "EndTurn mutated its parent.");
        JsonNode actorNode = JsonSerializer.SerializeToNode(origin, ModelJson.Options)!;
        actorNode["BaseAttack"] = 4;
        actorNode["Modifiers"]!["DamageBuff"] = 1;
        RoomAttachmentUnitSyncResult Move(CombatUnit unit)
        {
            var context = before.Spawn.Train.Context!;
            var prior = new TrainCombatState([new(0, false, [unit], [], context), new(1, false, [], [], context)], [], 7, context);
            var moved = new TrainCombatState([new(0, false, [], [], context), new(1, false, [unit], [], context)], [], 7, context);
            return RoomAttachmentUnitModel.SynchronizeMovement(prior, moved);
        }
        var calibrated = Move(actorNode.Deserialize<CombatUnit>(ModelJson.Options)!);
        Require(calibrated.Supported && calibrated.State!.Rooms[1].Units.Single().MoonPhaseMask == 6,
            "Original isolated movement did not refresh moon understanding: " + calibrated.UnsupportedReason);
        foreach (string control in new[] { "Identity", "Attack", "DamageBuff", "Horde", "Payload" })
        {
            JsonNode node = actorNode.DeepClone();
            if (control == "Identity") node["AssetKey"] = "unknown";
            if (control == "Attack") node["BaseAttack"] = 8;
            if (control == "DamageBuff") node["Modifiers"]!["DamageBuff"] = 2;
            if (control == "Horde") node["HordeDefinition"]!["Attack"] = 4;
            if (control == "Payload") node["Triggers"]![0]!["Effects"]![0]!["Value"] = 2;
            Require(!Move(node.Deserialize<CombatUnit>(ModelJson.Options)!).Supported,
                "Unknown Zealot escaped its isolated movement guard: " + control);
        }
        if (replay.HasValue) { Native(replay.Value, alternative: alternative); NativeBranchObservationChecks.Pair(fixture, replay.Value); }
        Console.WriteLine("INACTIVE-ONCE-BUFFED-MULTISTRIKE-ZEALOT-INFERNO-EXIT PASS: " + rows.Length + " native inputs/full state/RNG, attack4 and mask5->6, four immutable branches/five isolated guards; independentPair=" + replay.HasValue);
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}

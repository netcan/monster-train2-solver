using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InactiveNewMoonZealotBumpChecks
{
    private const string Reason = "Inactive movement moon understanding requires native calibration.";

    internal static void Native(FixtureValue fixture)
    {
        FixtureValue[] rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        BattleTurnState before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatUnit unit = before.Spawn.Train.Rooms[0].Units.Single(item => item.Id == 6);
        CombatUnit moved = actual.Spawn.Train.Rooms[1].Units.Single(item => item.Id == 6);
        Require(rows.Length == 6 && before.Spawn.Train.Context!.QueryFrame is { MoonPhaseTrackingActive: false, MoonPhase: 1 } &&
            unit is { AssetKey: "HeavyT2_Zealot", MoonPhaseMask: 6, Health: 110, MaxHealth: 110, Attack: 3 } && moved.MoonPhaseMask == 5,
            "Missing the native inactive new-moon zealot movement.");
        JsonNode mutant = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
        JsonNode enemy = mutant["Spawn"]!["Train"]!["Rooms"]![0]!["Units"]!.AsArray().Single(item => item!["Id"]!.GetValue<int>() == 6)!;
        enemy["AssetKey"] = "UnknownHeavy";
        BattleActionResult rejected = BattleActionModel.PlayCard(mutant.Deserialize<BattleTurnState>(ModelJson.Options)!, action);
        Require(!rejected.Supported && rejected.Reason == Reason, "Unknown enemy movement must remain guarded.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options), expected = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            BattleActionResult result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == expected, "Concurrent zealot movement differs.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Movement mutated its parent.");
        Console.WriteLine("INACTIVE-NEW-MOON-ZEALOT PASS: 6 native inputs, complete successor/RNG, unknown enemy guard and four immutable branches.");
    }

    internal static void Pair(FixtureValue original, FixtureValue repaired, FixtureValue repaired2, FixtureValue original2)
    {
        Native(original); Native(original2); Native(repaired); Native(repaired2);
        NativeBranchObservationChecks.Pair(original, repaired);
        NativeBranchObservationChecks.Pair(original2, repaired2);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

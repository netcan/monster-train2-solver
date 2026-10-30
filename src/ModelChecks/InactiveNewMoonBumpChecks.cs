using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InactiveNewMoonBumpChecks
{
    private const string Reason = "Inactive movement moon understanding requires native calibration.";

    internal static void Native(FixtureValue fixture)
    {
        FixtureValue[] rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        BattleTurnState before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatUnit champion = before.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == 2);
        CombatUnit moved = actual.Spawn.Train.Rooms[2].Units.Single(unit => unit.Id == 2);
        Require(rows.Length == 36 && before.Spawn.Train.Context!.QueryFrame is { MoonPhaseTrackingActive: false, MoonPhase: 1 } &&
            champion is { AssetKey: "MonsterChampionHellhornedHornbreaker", MoonPhaseMask: 6, Health: 62 } && moved.MoonPhaseMask == 5,
            "Missing the native inactive new-moon champion movement.");
        JsonNode mutant = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
        JsonNode original = mutant["Spawn"]!["Train"]!["Rooms"]![1]!["Units"]!.AsArray().Single(item => item!["Id"]!.GetValue<int>() == 2)!;
        original["AssetKey"] = "UnknownChampion";
        BattleActionResult rejected = BattleActionModel.PlayCard(mutant.Deserialize<BattleTurnState>(ModelJson.Options)!, action);
        Require(!rejected.Supported && rejected.Reason == Reason, "Unknown movement signature must remain guarded.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options), expected = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            BattleActionResult result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == expected, "Concurrent movement differs.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Movement mutated its parent.");
        Console.WriteLine("INACTIVE-NEW-MOON-BUMP PASS: 36 native inputs, complete successor/RNG, unknown signature guard and four immutable branches.");
    }

    internal static void Pair(FixtureValue original, FixtureValue repaired, FixtureValue repaired2, FixtureValue original2)
    {
        Native(original); Native(original2); Native(repaired); Native(repaired2);
        NativeBranchObservationChecks.Pair(original, repaired);
        NativeBranchObservationChecks.Pair(original2, repaired2);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

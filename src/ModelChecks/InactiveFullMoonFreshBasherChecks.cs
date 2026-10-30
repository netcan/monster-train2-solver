using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InactiveFullMoonFreshBasherChecks
{
    private const string Reason = "Inactive movement moon understanding requires native calibration.";

    internal static void Native(FixtureValue fixture)
    {
        FixtureValue[] rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        BattleTurnState before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatUnit unit = before.Spawn.Train.Rooms[0].Units.Single(item => item.Id == 3);
        CombatUnit moved = actual.Spawn.Train.Rooms[1].Units.Single(item => item.Id == 3);
        Require(rows.Length == 5 && before.Spawn.Train.Context!.QueryFrame is { MoonPhaseTrackingActive: false, MoonPhase: 2 } &&
            unit is { AssetKey: "MonsterBasher", MoonPhaseMask: 5, Health: 80, MaxHealth: 80, Attack: 74 } && moved.MoonPhaseMask == 6,
            "Missing native fresh inactive full-moon Basher movement.");
        JsonNode mutant = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
        JsonNode original = mutant["Spawn"]!["Train"]!["Rooms"]![0]!["Units"]!.AsArray().Single(item => item!["Id"]!.GetValue<int>() == 3)!;
        original["AssetKey"] = "UnknownUnit";
        BattleActionResult rejected = BattleActionModel.PlayCard(mutant.Deserialize<BattleTurnState>(ModelJson.Options)!, action);
        Require(!rejected.Supported, "Unknown unit movement must remain guarded.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options), expected = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            BattleActionResult result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == expected, "Concurrent Basher movement differs.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Basher movement mutated its parent.");
        Console.WriteLine("INACTIVE-FULL-MOON-FRESH-BASHER PASS: 5 native inputs, complete successor/RNG, unknown unit guard and four immutable branches.");
    }

    internal static void Pair(FixtureValue original, FixtureValue repaired, FixtureValue repaired2, FixtureValue original2)
    {
        Native(original); Native(original2); Native(repaired); Native(repaired2);
        NativeBranchObservationChecks.Pair(original, repaired);
        NativeBranchObservationChecks.Pair(original2, repaired2);
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

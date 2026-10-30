using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InactiveBumpMoonChecks
{
    private const string Reason = "Native rolling action successor mismatch observation.";

    internal static void Native(FixtureValue fixture)
    {
        FixtureValue[] rows = NativeBranchObservationChecks.Replay(fixture, Reason, HistoricalMismatch);
        BattleTurnState before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatUnit unit = before.Spawn.Train.Rooms[0].Units.Single(item => item.Id == 2);
        CombatUnit moved = actual.Spawn.Train.Rooms[1].Units.Single(item => item.Id == 2);
        Require(rows.Length == 18 && before.Spawn.Turn == 3 &&
            before.Spawn.Train.Context!.QueryFrame is { MoonPhaseTrackingActive: false, MoonPhase: 2 } &&
            unit is { AssetKey: "MonsterChampionHellhornedHornbreaker", MoonPhaseMask: 5 } && moved.MoonPhaseMask == 6,
            "Missing original inactive-moon champion movement.");
        JsonNode mutant = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
        JsonNode champion = mutant["Spawn"]!["Train"]!["Rooms"]![0]!["Units"]!.AsArray().Single(item => item!["Id"]!.GetValue<int>() == 2)!;
        champion["AssetKey"] = "UnknownChampion";
        BattleActionResult rejected = BattleActionModel.PlayCard(mutant.Deserialize<BattleTurnState>(ModelJson.Options)!, action);
        Require(!rejected.Supported && rejected.Reason == "Inactive movement moon understanding requires native calibration.",
            "Unknown inactive movement signature must remain guarded.");
        string immutable = JsonSerializer.Serialize(before, ModelJson.Options), expected = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            BattleActionResult result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == expected, "Concurrent inactive movement differs.");
        });
        Require(immutable == JsonSerializer.Serialize(before, ModelJson.Options), "Inactive movement mutated its parent.");
        Console.WriteLine("INACTIVE-BUMP-MOON PASS: 18 native inputs, complete successor/RNG, unknown signature guard and four concurrent replays.");
    }

    private static void HistoricalMismatch(BattleTurnState historical, BattleTurnState actual)
    {
        CombatUnit old = historical.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == 2);
        CombatUnit observed = actual.Spawn.Train.Rooms[1].Units.Single(unit => unit.Id == 2);
        Require(old.MoonPhaseMask == 5 && observed.MoonPhaseMask == 6, "Historical prediction did not retain the old mask.");
        JsonNode corrected = JsonSerializer.SerializeToNode(historical, ModelJson.Options)!;
        JsonNode champion = corrected["Spawn"]!["Train"]!["Rooms"]![1]!["Units"]!.AsArray().Single(item => item!["Id"]!.GetValue<int>() == 2)!;
        champion["MoonPhaseMask"] = 6;
        JsonNode cached = corrected["Spawn"]!["Train"]!["Context"]!["Enchantments"]!["Rooms"]![1]!["Units"]!.AsArray()
            .Single(item => item!["Id"]!.GetValue<int>() == 2)!;
        Require(cached["MoonPhaseMask"]!.GetValue<int>() == 5, "Historical enchantment world did not retain the old mask.");
        cached["MoonPhaseMask"] = 6;
        string correctedKey = NativeBranchObservationChecks.Key(corrected.Deserialize<BattleTurnState>(ModelJson.Options)!);
        string actualKey = NativeBranchObservationChecks.Key(actual);
        Require(correctedKey == actualKey,
            "Historical movement mismatch changed more than the champion moon mask: " + ModelJson.Difference(correctedKey, actualKey));
    }

    internal static void Pair(FixtureValue original, FixtureValue repaired, FixtureValue repaired2, FixtureValue original2)
    {
        Native(original); Native(original2); Native(repaired); Native(repaired2);
        NativeBranchObservationChecks.Pair(original, repaired, originalPredictionPresent: true);
        NativeBranchObservationChecks.Pair(original2, repaired2, originalPredictionPresent: true);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

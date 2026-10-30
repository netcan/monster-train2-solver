using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class UnitPurgeRelicChecks
{
    private const string Reason = "Train combat floor 1: Unit-card purge with relic callbacks requires native calibration.";

    internal static void Native(FixtureValue fixture)
    {
        FixtureValue[] rows = NativeBranchObservationChecks.Replay(fixture, Reason);
        BattleTurnState before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var beforePiles = before.Spawn.Train.Context!.OtherPiles!;
        var afterPiles = actual.Spawn.Train.Context!.OtherPiles!;
        Require(rows.Length == 33 && rows[^1].GetProperty("IsEndTurn").GetBoolean() &&
            beforePiles.Single(p => p.Name == "Standby").Cards.Any(c => c.InstanceId == 5) &&
            !afterPiles.Single(p => p.Name == "Standby").Cards.Any(c => c.InstanceId == 5) &&
            afterPiles.Single(p => p.Name == "Purged").Cards.Any(c => c.InstanceId == 5) &&
            afterPiles.Single(p => p.Name == "Exhausted").Cards.Any(c => c.InstanceId == 29),
            "Missing native unit purge and equipment exhaustion.");

        JsonNode mutant = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
        mutant["Spawn"]!["Train"]!["Context"]!["Relics"]![0]!["EffectTypes"]!.AsArray().Add("RelicEffectDrawOnCardPurged");
        CombatContext mutatedContext = mutant.Deserialize<BattleTurnState>(ModelJson.Options)!.Spawn.Train.Context!;
        CombatContext? rejected = UnitCardPurgeModel.Apply(mutatedContext, 5, out string? reason);
        Require(rejected == null && reason == "Unit-card purge with relic callbacks requires native calibration.",
            "Uncalibrated purge callback must remain guarded: " + reason);

        string parent = JsonSerializer.Serialize(before, ModelJson.Options), expected = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            BattleTurnResult result = BattleTurnModel.EndTurn(before);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == expected, "Concurrent unit purge differs.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Unit purge mutated its parent.");
        Console.WriteLine("UNIT-PURGE-RELIC PASS: 33 native inputs, complete successor/RNG, callback guard and four immutable branches.");
    }

    internal static void Pair(FixtureValue original, FixtureValue repaired, FixtureValue repaired2, FixtureValue original2)
    {
        Native(original); Native(original2); Native(repaired); Native(repaired2);
        NativeBranchObservationChecks.Pair(original, repaired);
        NativeBranchObservationChecks.Pair(original2, repaired2);
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

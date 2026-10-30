using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CopyImpTextCacheChecks
{
    private const string Reason = "Native rolling action successor mismatch observation.";

    internal static void Native(FixtureValue fixture)
    {
        FixtureValue[] rows = NativeBranchObservationChecks.Replay(fixture, Reason, HistoricalMismatch);
        BattleTurnState before = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = rows[^1].GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext context = before.Spawn.Train.Context!;
        Require(rows.Length == 41 && before.Spawn.Turn == 10 && action.CardInstanceId == 14 &&
            context.NextCardId == 29 && !context.Statistics!.StoredCards!.Contains(29) &&
            actual.Spawn.Train.Context!.Statistics!.StoredCards!.Contains(29) &&
            context.Cards.Hand.Any(card => card.InstanceId == 16) &&
            context.FindCard(16)!.DamageScalingTraits!.Single().Query.Type == "SubtypeInDeck",
            "Missing original copied-card and Imp text query boundary.");
        string parent = JsonSerializer.Serialize(before, ModelJson.Options);
        string expected = NativeBranchObservationChecks.Key(actual);
        Parallel.For(0, 4, _ =>
        {
            BattleActionResult result = BattleActionModel.PlayCard(before, action);
            Require(result.Supported && NativeBranchObservationChecks.Key(result.State!) == expected,
                "Concurrent copied-card cache replay differs.");
        });
        Require(parent == JsonSerializer.Serialize(before, ModelJson.Options), "Replay mutated its parent.");
        JsonNode mutant = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
        JsonNode impCard = mutant["Spawn"]!["Train"]!["Context"]!["CardInstances"]!.AsArray()
            .Single(item => item!["InstanceId"]!.GetValue<int>() == 16)!;
        impCard["DamageScalingTraits"]![0]!["Query"]!["Type"] = "Gold";
        BattleActionResult control = BattleActionModel.PlayCard(mutant.Deserialize<BattleTurnState>(ModelJson.Options)!, action);
        Require(control.Supported && !control.State!.Spawn.Train.Context!.Statistics!.StoredCards!.Contains(29),
            "A different damage query must not use the calibrated cache refresh.");
        Console.WriteLine("COPY-IMP-TEXT-CACHE PASS: 41 native inputs, complete successor/RNG, historical cache difference and four immutable branches.");
    }

    private static void HistoricalMismatch(BattleTurnState historical, BattleTurnState actual)
    {
        JsonNode corrected = JsonSerializer.SerializeToNode(historical, ModelJson.Options)!;
        JsonArray stored = corrected["Spawn"]!["Train"]!["Context"]!["Statistics"]!["StoredCards"]!.AsArray();
        Require(!stored.Any(value => value!.GetValue<int>() == 29), "Original prediction already has card 29.");
        stored.Add(29);
        string correctedKey = NativeBranchObservationChecks.Key(corrected.Deserialize<BattleTurnState>(ModelJson.Options)!);
        string actualKey = NativeBranchObservationChecks.Key(actual);
        Require(correctedKey == actualKey, "Historical difference extends beyond card 29: " + ModelJson.Difference(correctedKey, actualKey));
    }

    internal static void Pair(FixtureValue original, FixtureValue repaired, FixtureValue repaired2, FixtureValue original2)
    {
        Native(original); Native(original2); Native(repaired); Native(repaired2);
        NativeBranchObservationChecks.Pair(original, repaired, originalPredictionPresent: true);
        NativeBranchObservationChecks.Pair(original2, repaired2, originalPredictionPresent: true);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

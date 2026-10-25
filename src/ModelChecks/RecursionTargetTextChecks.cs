using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RecursionTargetTextChecks
{
    private const string Reason = "Rolling native state differs after decision 20: $.Context.Statistics.StoredCards.Length";
    internal static void Native(FixtureValue fixture)
    {
        var rows = NativeBranchObservationChecks.Replay(fixture, Reason, HistoricalMismatch);
        var last = rows[^1];
        var before = last.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var after = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        var context = after.Spawn.Train.Context!;
        Require(rows.Length == 20 && before.Spawn.Turn == 2 && after.Spawn.Turn == 3 &&
            last.GetProperty("IsEndTurn").GetBoolean() && before.CanonicalDecisionReferences &&
            before.Spawn.Train.Context!.NextCardId == 33 && context.NextCardId == 34,
            "Missing the natural20-input turn2/3 generated-card boundary.");
        Require(context.Cards.Hand.Any(card => card.InstanceId == 33 && card.DataId == "356a00a4-010b-462b-b683-2ef296d076a8") &&
            context.Statistics!.StoredCards!.Contains(33) &&
            !before.Spawn.Train.Context.Statistics!.StoredCards!.Contains(33), "New Sting33 must enter the native statistics cache.");
        Require(context.Cards.Hand.Select(card => context.FindCard(card.InstanceId)!.MaskDescriptor)
            .Any(descriptor => descriptor?.BaseTraits.Any(trait => trait.RuntimeType == "CardTraitShowCardTargets") == true &&
                descriptor.Definition.MainEffectNames.Contains("CardEffectRecursion")), "Missing the visible recursion target-count query.");
        Console.WriteLine("RECURSION-TARGET-TEXT PASS:20 native inputs/full successors/RNG; visible recursion target text materializes newly generated Sting33; immutable parents.");
    }
    private static void HistoricalMismatch(BattleTurnState old, BattleTurnState actual)
    {
        JsonNode projected = JsonNode.Parse(NativeBranchObservationChecks.Key(old))!;
        JsonNode observed = JsonNode.Parse(NativeBranchObservationChecks.Key(actual))!;
        JsonArray stored = projected["Context"]!["Statistics"]!["StoredCards"]!.AsArray();
        Require(!stored.Any(value => value!.GetValue<int>() == 33) &&
            actual.Spawn.Train.Context!.Statistics!.StoredCards!.Contains(33), "Preserve the original missing statistics entry33.");
        stored.Add(33);
        Require(JsonNode.DeepEquals(projected, observed), "Historical target-text mismatch extends beyond the new stored-card entry.");
    }
    internal static void Pair(FixtureValue source, FixtureValue replay)
    {
        Native(source); Native(replay);
        NativeBranchObservationChecks.Pair(source, replay, originalPredictionPresent: true);
        Console.WriteLine("RECURSION-TARGET-TEXT-PAIR PASS:original missing cache entry and independent repaired native forecast preserved.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

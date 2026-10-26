using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class AbilityPresentationChecks
{
    internal static void Native(FixtureValue fixture, FixtureValue? repaired = null)
    {
        Require(fixture.GetProperty("EvidenceKind").GetString() == "NativeAdviceRootMismatch", "Wrong publication evidence.");
        var published = fixture.GetProperty("PublishedRoot").Deserialize<BattleTurnState>()!;
        var actual = fixture.GetProperty("ActualRoot").Deserialize<BattleTurnState>()!;
        CombatContext before = published.Spawn.Train.Context!, after = actual.Spawn.Train.Context!;
        Require(published.Spawn.Turn == 6 && published.SelectedRoom == 0 && before.NextCardId == 31 &&
            before.AbilityCardCache is { Count: 0 } && after.NextCardId == 32 && after.AbilityCardCache is { Count: 1 } &&
            after.AbilityCardCache[0].InstanceId == 31 && after.AbilityCardCache[0].DataId == "d1ab03ed-fe63-4eca-b68e-3090524f325e" &&
            before.CardRegistry!.Count == 30 && after.CardRegistry!.Count == 31 &&
            after.CardInstances!.All(card => card.InstanceId != 31), "Lost the native cold skill-cache allocation.");
        // Verify the complete counterexample, including all aliased room
        // contexts. Only presentation's three allocator/cache fields changed.
        JsonNode restored = JsonSerializer.SerializeToNode(actual, ModelJson.Options)!;
        JsonNode prior = JsonSerializer.SerializeToNode(before, ModelJson.Options)!;
        var train = restored["Spawn"]!["Train"]!;
        foreach (JsonNode context in new[] { train["Context"]! }.Concat(train["Rooms"]!.AsArray().Select(room => room!["Context"]!)))
            foreach (string field in new[] { "NextCardId", "CardRegistry", "AbilityCardCache" })
                context[field] = prior[field]!.DeepClone();
        Require(ModelJson.Difference(JsonSerializer.Serialize(published, ModelJson.Options), restored.ToJsonString()) == null,
            "Native presentation changed more than its cold cache factory.");
        using var keys = new BattleStateKeys();
        Require(keys.Key(published) != keys.Key(actual), "The real allocator/cache mutation must remain keyed.");
        if (repaired.HasValue)
        {
            Require(repaired.Value.GetProperty("NativeCompleted").GetBoolean(), "Original native prefix did not finish.");
            var render = repaired.Value.GetProperty("SkillPresentation");
            var renderRoot = render.GetProperty("Before").Deserialize<BattleTurnState>()!;
            Require(ModelJson.Difference(JsonSerializer.Serialize(published, ModelJson.Options),
                    JsonSerializer.Serialize(renderRoot, ModelJson.Options)) == null &&
                render.GetProperty("Before").ContentEquals(render.GetProperty("After")) &&
                render.GetProperty("Steps").GetArrayLength() == 5 && render.GetProperty("Outcome").GetInt32() == (int)RoomOutcome.BattleWon &&
                render.GetProperty("Expanded").GetString()!.Contains("【单位技能】") &&
                render.GetProperty("SearchesBefore").ContentEquals(render.GetProperty("SearchesAfter")) &&
                render.GetProperty("GenerationBefore").ContentEquals(render.GetProperty("GenerationAfter")),
                "Original winning suffix did not render both modes without native state/RNG/planning changes.");
        }
        Console.WriteLine("ABILITY-PRESENTATION PASS: exact native cold-cache mutation remains keyed; complete restored state/RNG; repairedNative=" + repaired.HasValue);
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

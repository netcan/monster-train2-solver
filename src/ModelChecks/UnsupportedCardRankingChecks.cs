using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Model;

internal static class UnsupportedCardRankingChecks
{
    internal static void Run(MonsterTrain2Poju.Fixtures.FixtureValue fixture)
    {
        var root = fixture.GetProperty("Root").Deserialize<BattleTurnState>()!;
        const string guard = "Uncalibrated ranking control upgrade";
        var node = JsonSerializer.SerializeToNode(root, ModelJson.Options)!;
        var cards = node["Spawn"]!["Train"]!["Context"]!["CardInstances"]!.AsArray();
        var champion = cards.Single(card => card!["InstanceId"]!.GetValue<int>() == 3)!;
        Require(champion["MaskDescriptor"] != null && root.Spawn.Train.Context!.Cards.Hand.Any(card => card.InstanceId == 3),
            "Control requires the actual masked deployment champion in hand.");
        champion["ExternalInteractions"] = new JsonArray(guard);
        var source = node.Deserialize<BattleTurnState>(ModelJson.Options)!;
        string parent = JsonSerializer.Serialize(source, ModelJson.Options);
        var instance = source.Spawn.Train.Context!.FindCard(3)!;
        var definition = source.PlayRules!.Cards.Single(rule => rule.DataId == instance.DataId);
        Require(CardModifierModel.Resolve(definition, instance).ExternalInteractions.Any(value => value.Contains(guard)),
            "Ranking discarded an unsupported mask guard.");
        var rejected = BattleActionModel.PlayCard(source, new PlayCardAction(3, 2, 0));
        Require(!rejected.Supported && rejected.Rejection == ActionRejection.Unsupported && Guarded(rejected.Reason!),
            "Unsupported champion was accepted or misreported as illegal: " + rejected.Rejection + ": " + rejected.Reason);
        for (int room = 0; room < 3; room++)
        {
            var choice = BattleTacticalPolicy.Choose(source, out var omitted, room);
            Require(choice?.Action.CardInstanceId != 3 && omitted.Any(attempt => Guarded(attempt.Reason)),
                "Tactical ranking lost the guarded candidate.");
        }
        var plan = ParallelBattlePlanner.Plan(source, new BattlePlannerOptions(maxNodes: 32, maxTurns: 1,
            maxActionsPerTurn: 4, timeLimit: TimeSpan.FromSeconds(1), useTacticalSeed: true), 2);
        Require(plan.UnsupportedAttempts.Any(attempt => Guarded(attempt.Reason)) &&
            !plan.Steps.Any(step => !step.IsEndTurn && step.Action?.CardInstanceId == 3),
            "Parallel search accepted or hid an unsupported champion.");
        Require(parent == JsonSerializer.Serialize(source, ModelJson.Options), "Ranking mutated the native parent.");
        Console.WriteLine("UNSUPPORTED-CARD-RANKING PASS: actual deployment; explicit guarded rejection; three tactical floors; two-worker search; immutable parent.");
        static bool Guarded(string reason) => reason.Contains(guard) || reason.Contains("Upgrade room modifiers");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

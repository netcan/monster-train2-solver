using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PlannerDeploymentChecks
{
    internal static void Native(FixtureValue fixture)
    {
        BattleTurnState root = fixture.GetProperty("Root").Deserialize<BattleTurnState>()!;
        string original = JsonSerializer.Serialize(root);
        for (int room = 0; room < 3; room++)
        {
            SupportedActionCandidate? first = BattleHeuristicPolicy.Choose(root, out var omitted, room);
            Require(first != null && first.Action.RoomIndex == room && omitted.Count == 0,
                "Deployment preference did not rank a legal native placement.");
        }
        using var keys = new BattleStateKeys();
        var options = new BattlePlannerOptions(64, 3, 8, timeLimit: TimeSpan.FromSeconds(30), preferredDeploymentRoom: 1);
        BattlePlanResult plan = BattlePlanner.Plan(root, keys.Key, options);
        Require(plan.CompletedHorizon && plan.Steps.Count > 0 && plan.Steps[0].Action?.RoomIndex == 1 &&
            plan.ExpandedNodes <= options.MaxNodes && plan.UnsupportedAttempts.Count == 0,
            "The bounded planner lost its preferred deployment continuation.");
        BattleTurnState replayed = BattlePlannerChecks.Replay(root, plan.Steps, out RoomOutcome outcome);
        Require(JsonSerializer.Serialize(replayed) == JsonSerializer.Serialize(plan.State) && outcome == plan.Outcome &&
            JsonSerializer.Serialize(root) == original, "Deployment plan changed its source or failed replay.");
        Require(BattleHeuristicChecks.Forecast(fixture, 1) == RoomOutcome.BattleWon,
            "The captured middle-floor heuristic winning continuation regressed.");
        Console.WriteLine("PLANNER-DEPLOYMENT PASS: three legal placements, bounded middle-floor plan, complete model winning replay and immutable root; native victory acceptance is separate.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}

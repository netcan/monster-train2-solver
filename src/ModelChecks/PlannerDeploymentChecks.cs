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
        BattleTurnState mid = fixture.GetProperty("MidTurnRoot").Deserialize<BattleTurnState>()!;
        string midOriginal = JsonSerializer.Serialize(mid);
        int[] spikes = mid.Spawn.Train.Context!.Cards.Hand.Where(card => card.DataId ==
            "ab3c2bfa-0ad4-4be0-a050-293f4866bd65").Select(card => card.InstanceId).ToArray();
        using var midKeys = new BattleStateKeys();
        BattlePlanResult continued = BattlePlanner.Plan(mid, midKeys.Key,
            new BattlePlannerOptions(11, 3, 8, timeLimit: TimeSpan.FromSeconds(30), preferredDeploymentRoom: 1));
        Require(continued.CompletedHorizon && continued.Steps.TakeWhile(step => !step.IsEndTurn)
            .Any(step => step.Action != null && spikes.Contains(step.Action.CardInstanceId)),
            "Premature ability/draw forecasting consumed the budget before the seed's X card.");
        BattleTurnState midReplay = BattlePlannerChecks.Replay(mid, continued.Steps, out var midOutcome);
        Require(JsonSerializer.Serialize(midReplay) == JsonSerializer.Serialize(continued.State) && midOutcome == continued.Outcome &&
            JsonSerializer.Serialize(mid) == midOriginal, "Current-turn seed failed replay or source isolation.");
        Require(BattleHeuristicChecks.Forecast(fixture, 1) == RoomOutcome.BattleWon,
            "The captured middle-floor heuristic winning continuation regressed.");
        Console.WriteLine("PLANNER-DEPLOYMENT PASS: three legal placements, bounded middle-floor plan, complete model winning replay and immutable root; native victory acceptance is separate.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}

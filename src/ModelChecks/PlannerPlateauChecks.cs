using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PlannerPlateauChecks
{
    internal static void Native(FixtureValue fixture)
    {
        var root = fixture.GetProperty("Root").Deserialize<BattleTurnState>()!;
        string parent = JsonSerializer.Serialize(root);
        BattleTurnResult idle = BattleTurnModel.EndTurn(root);
        Require(idle.Supported, idle.UnsupportedReason ?? "Baseline EndTurn failed.");
        for (int turn = 1; turn < 3; turn++)
        {
            idle = BattleTurnModel.EndTurn(idle.State!);
            Require(idle.Supported, idle.UnsupportedReason ?? "Baseline continuation failed.");
        }
        double score = BattlePlanner.Evaluate(idle.State!, idle.Outcome);
        int hp = idle.State!.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health;
        Require(score == -hp * 1000d + 499, "Missing native high-health enemy score plateau.");
        // A node bound makes this regression repeatable while native workers
        // contend for CPU; the interactive three-second deadline is separate.
        var options = new BattlePlannerOptions(64, 3, 8, timeLimit: TimeSpan.FromSeconds(30));
        var plan = BattlePlanner.Plan(root, state => JsonSerializer.Serialize(state), options);
        Require(plan.CompletedHorizon && plan.Steps.Count > 1 && !plan.Steps[0].IsEndTurn && plan.Score <= score,
            "The planner must retain useful plays when the public positional score reaches its cap.");
        var replayed = BattlePlannerChecks.Replay(root, plan.Steps, out RoomOutcome outcome);
        Require(JsonSerializer.Serialize(replayed) == JsonSerializer.Serialize(plan.State) && outcome == plan.Outcome &&
            JsonSerializer.Serialize(root) == parent, "Plateau recommendation failed replay or changed its root.");
        Console.WriteLine($"PLANNER-PLATEAU PASS: useful first play retained; baseline={score}, plan={plan.Score}.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}

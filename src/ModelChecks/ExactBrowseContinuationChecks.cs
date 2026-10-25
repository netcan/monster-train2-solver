using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ExactBrowseContinuationChecks
{
    internal static void Native(FixtureValue fixture)
    {
        Require(fixture.GetProperty("NativeWon").GetBoolean(), "Expected the original native winning battle.");
        var rows = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        var start = rows[45];
        var root = start.GetProperty("Root").Deserialize<BattleTurnState>()!;
        var steps = start.GetProperty("PlanSteps").EnumerateArray().Select(step =>
            new BattleSearchStep(step.GetProperty("Action").Deserialize<PlayCardAction>(), step.GetProperty("IsEndTurn").GetBoolean())).ToArray();
        var final = start.GetProperty("Planned").Deserialize<BattleTurnState>()!;
        var outcome = start.GetProperty("PlannedOutcome").Deserialize<RoomOutcome>();
        Require(rows.Length == 58 && root.Spawn.Turn == 7 && steps.Length == 13 && outcome == RoomOutcome.BattleWon,
            "Wrong original winning suffix.");
        var plan = new BattlePlanResult(final, outcome, steps, BattlePlanner.Evaluate(final, outcome),
            steps.Count(step => step.IsEndTurn), 1, 0, 0, TimeSpan.Zero, false, false, false, false, true,
            [], null, searchedToBattleEnd: true);
        int searches = 0;
        using var keys = new BattleStateKeys();
        using var session = new BattleAdviceSession((_, _) =>
        {
            Require(++searches == 1, "An exact captured continuation restarted search.");
            return plan;
        });
        session.Observe(root, keys.Key(root)); Pump(session);
        for (int index = 0; index < 2; index++)
        {
            var action = rows[45 + index].GetProperty("Action").Deserialize<PlayCardAction>()!;
            session.BrowseRoom(action.RoomIndex);
            var actual = rows[46 + index].GetProperty("Root").Deserialize<BattleTurnState>()!;
            session.InvalidateForInput(); session.Observe(actual, keys.Key(actual)); Pump(session);
            Require(session.Current!.ReusedWinningContinuation && searches == 1 &&
                session.Current.Plan.Steps.Count == steps.Length - index - 1,
                "Captured winning suffix was not reused after an advised input.");
        }
        Console.WriteLine("EXACT-BROWSE-CONTINUATION PASS:original13-step winning suffix; native Mix and cross-floor spell checkpoints; one search; exact full-state/RNG reuse.");
    }
    private static void Pump(BattleAdviceSession session)
    {
        Require(SpinWait.SpinUntil(() => { session.Poll(); return session.Status is BattleAdviceStatus.Ready or BattleAdviceStatus.Unavailable; }, 60000),
            "Continuation worker did not finish.");
        Require(session.Status == BattleAdviceStatus.Ready, session.Error ?? "Continuation unavailable.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

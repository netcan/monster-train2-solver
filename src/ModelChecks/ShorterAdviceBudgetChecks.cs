using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ShorterAdviceBudgetChecks
{
    internal static void Run(FixtureValue fixture)
    {
        Require(!fixture.GetProperty("SourceRunCompleted").GetBoolean() &&
            fixture.GetProperty("SourceError").GetString()!.Contains("turns=2 nodes=3") &&
            fixture.GetProperty("PureCaptures").GetInt32() > 0, "Missing the original live player budget failure.");
        BattleTurnState root = fixture.GetProperty("Root").Deserialize<BattleTurnState>()!;
        Require(root.Spawn.Turn == 2 && root.Spawn.OuterBoss?.AssetKey == "Boss_Level8_Fragile",
            "Wrong native budget-failure boundary.");
        using var keys = new BattleStateKeys();
        string immutable = JsonSerializer.Serialize(root);
        BattlePlannerOptions Options(int nodes, bool shorter, int turns = 3) => new(nodes, turns, 1,
            timeLimit: TimeSpan.FromSeconds(30), allowShorterHorizon: shorter);
        foreach (int nodes in new[] { 1, 2 })
        {
            BattlePlanResult strict = BattlePlanner.Plan(root, keys.Key, Options(nodes, false));
            Require(!strict.CompletedHorizon && !strict.UsedShorterHorizon, "Strict callers accepted a truncated horizon.");
            Reject(strict);
            BattlePlanResult bounded = BattlePlanner.Plan(root, keys.Key, Options(nodes, true));
            Require(!bounded.CompletedHorizon && bounded.UsedShorterHorizon && bounded.RequestedTurns == 3 &&
                bounded.TurnsCompleted == nodes && bounded.ReachedNodeLimit && bounded.Steps.Last().IsEndTurn &&
                bounded.PolicyBaselineScore == null && bounded.Outcome == RoomOutcome.Exchanged,
                "Budget fallback did not retain a complete, explicitly shorter turn window.");
            BattleAdviceSession.Verify(root, bounded);
            BattleTurnState replayed = root;
            foreach (BattleSearchStep step in bounded.Steps)
            {
                Require(step.IsEndTurn, "Node controls should contain only complete EndTurns.");
                BattleTurnResult next = BattleTurnModel.EndTurn(replayed);
                Require(next.Supported, next.UnsupportedReason ?? "Unsupported shorter forecast.");
                replayed = next.State!;
            }
            Require(keys.Key(replayed) == keys.Key(bounded.State), "Shorter forecast changed complete state or RNG.");
            var tampered = new BattlePlanResult(root, bounded.Outcome, bounded.Steps, bounded.Score,
                bounded.TurnsCompleted, bounded.ExpandedNodes, 0, 0, bounded.Elapsed, false, false, true, false, false,
                [], null, 3, true);
            Reject(tampered);
        }
        BattlePlanResult complete = BattlePlanner.Plan(root, keys.Key, Options(1, true, 1));
        Require(complete.CompletedHorizon && !complete.UsedShorterHorizon, "A complete requested horizon was mislabeled.");
        BattleAdviceSession.Verify(root, complete);
        BattleTurnState deployment = fixture.GetProperty("DeploymentRoot").Deserialize<BattleTurnState>()!;
        BattlePlanResult partial = BattlePlanner.Plan(deployment, keys.Key, new BattlePlannerOptions(1, 3, 1,
            timeLimit: TimeSpan.FromSeconds(30), useTacticalSeed: true, allowShorterHorizon: true));
        Require(partial.Steps.Count == 1 && !partial.Steps[0].IsEndTurn && partial.TurnsCompleted == 0 &&
            !partial.CompletedHorizon && !partial.UsedShorterHorizon, "A truncated deployment action became a shorter turn window.");
        bool partialRejected = false;
        try { BattleAdviceSession.Verify(deployment, partial); }
        catch (InvalidOperationException) { partialRejected = true; }
        Require(partialRejected, "A zero-turn forecast reached player advice.");
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        BattlePlanResult cancelled = BattlePlanner.Plan(root, keys.Key, Options(2, true), cancellationToken: cancellation.Token);
        Require(cancelled.Cancelled && !cancelled.UsedShorterHorizon, "Cancellation published a shorter forecast.");
        Reject(cancelled);
        using (var session = new BattleAdviceSession(Options(2, true)))
        {
            session.Observe(root, keys.Key(root));
            Require(SpinWait.SpinUntil(() => { session.Poll(); return session.Status != BattleAdviceStatus.Planning; }, 30000),
                "Shorter player forecast did not finish.");
            Require(session.Status == BattleAdviceStatus.Ready && session.Current!.Plan.UsedShorterHorizon &&
                !session.Current.ReusedWinningContinuation, "Player advice failed to publish its verified shorter window.");
        }
        Require(JsonSerializer.Serialize(root) == immutable, "Budget forecasts mutated their native root.");
        Console.WriteLine("SHORTER-ADVICE-BUDGET PASS nativeFragileRoot=true strictFailurePreserved=true completeWindows=1,2 requested=3 fullStateRngReplay=true cancelledAndTamperedRejected=true playerPublication=true immutableRoot=true");

        void Reject(BattlePlanResult plan)
        {
            bool rejected = false;
            try { BattleAdviceSession.Verify(root, plan); }
            catch (InvalidOperationException) { rejected = true; }
            Require(rejected, "Invalid or unrequested truncated advice was published.");
        }
    }

    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}

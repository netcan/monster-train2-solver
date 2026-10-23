using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OrdinaryAdviceBudgetChecks
{
    internal static void NativeRecovery(FixtureValue failure, FixtureValue source)
    {
        BattleTurnState root = failure.GetProperty("Root").Deserialize<BattleTurnState>()!;
        using var keys = new BattleStateKeys();
        string original = keys.Key(root);
        FixtureValue recovered = source.GetProperty("RollingDecisions").EnumerateArray().Single(row =>
            keys.Key(row.GetProperty("Root").Deserialize<BattleTurnState>()!) == original);
        FixtureValue[] steps = recovered.GetProperty("PlanSteps").EnumerateArray().ToArray();
        Require(source.GetProperty("NativeWon").GetBoolean() &&
            failure.GetProperty("SourceError").GetString()!.Contains("turns=0 nodes=5") &&
            recovered.GetProperty("Nodes").GetInt32() == 6 && steps.Length == 6 &&
            steps.Take(5).All(step => !step.GetProperty("IsEndTurn").GetBoolean()) && steps[^1].GetProperty("IsEndTurn").GetBoolean() &&
            recovered.GetProperty("RequestedHorizon").GetInt32() == 3 && recovered.GetProperty("TurnsPlanned").GetInt32() == 1 &&
            recovered.GetProperty("PlannedOutcome").Deserialize<RoomOutcome>() == RoomOutcome.BattleWon &&
            recovered.GetProperty("Timing").GetProperty("SearchMs").GetDouble() >= 3000 &&
            !recovered.GetProperty("ReusedContinuation").GetBoolean(),
            "Missing native complete victory at the exact original ordinary-prefix timeout root.");
        Console.WriteLine("ORDINARY-ADVICE-NATIVE-RECOVERY PASS: exact full-state/RNG failure root, five legal actions plus one complete EndTurn, real short-budget player victory.");
    }

    internal static void Run(FixtureValue fixture)
    {
        Require(!fixture.GetProperty("SourceRunCompleted").GetBoolean() &&
            fixture.GetProperty("EvidenceKind").GetString() == "NativePlayerBudgetFailure" &&
            fixture.GetProperty("SourceError").GetString()!.Contains("turns=0 nodes=5") &&
            fixture.GetProperty("PureCaptures").GetInt32() > 0, "Missing the original ordinary-prefix native budget failure.");
        BattleTurnState root = fixture.GetProperty("Root").Deserialize<BattleTurnState>()!;
        Require(root.Spawn.Turn == 8 && root.Spawn.OuterBoss?.AssetKey == "Boss_Level8_Fragile" &&
            root.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health == 7,
            "Wrong native ordinary-prefix budget boundary.");
        using var keys = new BattleStateKeys();
        string original = JsonSerializer.Serialize(root);
        BattlePlannerOptions Options(int nodes, bool shorter) => new(nodes, 3, 1,
            timeLimit: TimeSpan.FromSeconds(3), preferredDeploymentRoom: 0, allowShorterHorizon: shorter);
        BattlePlanResult Slow(bool shorter, int delayAt)
        {
            int evaluations = 0;
            return BattlePlanner.Plan(root, keys.Key, Options(128, shorter), (state, outcome) =>
            {
                if (++evaluations == delayAt) Thread.Sleep(3100);
                return BattlePlanner.Evaluate(state, outcome);
            });
        }
        foreach (int count in new[] { 1, 2 })
        {
            BattlePlanResult bounded = Slow(true, count + 1);
            Require(bounded.TimedOut && !bounded.Cancelled && bounded.ExpandedNodes == count + 1 &&
                bounded.TurnsCompleted == 1 && bounded.Steps.Count == count + 1 &&
                bounded.Steps.Take(count).All(step => !step.IsEndTurn) && bounded.Steps[^1].IsEndTurn &&
                (bounded.CompletedHorizon || bounded.UsedShorterHorizon),
                "Ordinary prefix lost its complete first turn after the soft deadline.");
            BattleAdviceSession.Verify(root, bounded);
            BattleTurnState replayed = BattlePlannerChecks.Replay(root, bounded.Steps, out RoomOutcome outcome);
            Require(keys.Key(replayed) == keys.Key(bounded.State) && outcome == bounded.Outcome,
                "Ordinary prefix fallback failed complete-state/RNG replay.");
        }
        BattlePlanResult strict = Slow(false, 2);
        Require(strict.TimedOut && strict.TurnsCompleted == 0 && !strict.CompletedHorizon && !strict.UsedShorterHorizon,
            "Strict ordinary callers accepted an unrequested shorter window.");
        Reject(strict);
        BattlePlanResult limited = BattlePlanner.Plan(root, keys.Key, new BattlePlannerOptions(4, 3, 1,
            timeLimit: TimeSpan.FromSeconds(30), preferredDeploymentRoom: 0, allowShorterHorizon: true));
        Require(limited.ReachedNodeLimit && limited.ExpandedNodes == 4 && limited.Steps.Count == 4 &&
            limited.TurnsCompleted == 0 && !limited.CompletedHorizon && !limited.UsedShorterHorizon,
            "Ordinary prefix fallback bypassed the hard node limit.");
        Reject(limited);
        using var cancellation = new CancellationTokenSource();
        int scores = 0;
        BattlePlanResult cancelled = BattlePlanner.Plan(root, keys.Key, Options(128, true), (state, outcome) =>
        {
            if (++scores == 2) cancellation.Cancel();
            return BattlePlanner.Evaluate(state, outcome);
        }, cancellation.Token);
        Require(cancelled.Cancelled && cancelled.ExpandedNodes == 1 && cancelled.TurnsCompleted == 0 && !cancelled.UsedShorterHorizon,
            "Ordinary prefix fallback ignored cancellation.");
        Reject(cancelled);
        Require(JsonSerializer.Serialize(root) == original, "Ordinary budget checks mutated their native root.");
        Console.WriteLine("ORDINARY-ADVICE-BUDGET PASS: native turn8 root, one/two-action complete fallbacks, exact state/RNG replay; strict, hard-node and cancellation controls preserved.");

        void Reject(BattlePlanResult plan)
        {
            bool rejected = false;
            try { BattleAdviceSession.Verify(root, plan); }
            catch (InvalidOperationException) { rejected = true; }
            Require(rejected, "An incomplete ordinary forecast reached advice publication.");
        }
    }

    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}

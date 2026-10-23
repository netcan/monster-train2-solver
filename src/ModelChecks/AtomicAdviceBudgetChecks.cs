using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class AtomicAdviceBudgetChecks
{
    internal static void Run(FixtureValue fixture)
    {
        Require(!fixture.GetProperty("SourceRunCompleted").GetBoolean() &&
            fixture.GetProperty("EvidenceKind").GetString() == "NativePlayerBudgetFailure" &&
            fixture.GetProperty("SourceError").GetString()!.Contains("turns=0 nodes=1") &&
            fixture.GetProperty("PureCaptures").GetInt32() > 0, "Missing the original live zero-turn budget failure.");
        BattleTurnState root = fixture.GetProperty("Root").Deserialize<BattleTurnState>()!;
        Require(root.Spawn.Turn == 5 && root.Spawn.OuterBoss?.AssetKey == "Boss_Level6_AngelDuo_Entropic_Variant1_ResolveHealth",
            "Wrong native atomic-budget boundary.");
        string immutable = JsonSerializer.Serialize(root, ModelJson.Options);
        using var keys = new BattleStateKeys();
        BattlePlannerOptions Options(int nodes, bool shorter) => new(nodes, 2, 1,
            timeLimit: TimeSpan.FromMilliseconds(50), useTacticalSeed: true, allowShorterHorizon: shorter);
        BattlePlanResult Slow(bool shorter, int nodes)
        {
            int evaluations = 0;
            return BattlePlanner.Plan(root, keys.Key, Options(nodes, shorter), (state, outcome) =>
            {
                // Reproduce a completed atomic candidate returning after the
                // soft deadline, independently of host/JIT/native runtime speed.
                if (++evaluations == 2) Thread.Sleep(60);
                return BattlePlanner.Evaluate(state, outcome);
            });
        }
        BattlePlanResult bounded = Slow(true, 2);
        Require(bounded.TimedOut && !bounded.Cancelled && bounded.ExpandedNodes == 2 && bounded.TurnsCompleted == 1 &&
            bounded.Steps.Count == 2 && !bounded.Steps[0].IsEndTurn && bounded.Steps[1].IsEndTurn &&
            (bounded.CompletedHorizon || bounded.UsedShorterHorizon),
            "A completed first candidate lost its complete first-turn forecast at the soft deadline.");
        BattleAdviceSession.Verify(root, bounded);
        BattleTurnState actual = BattlePlannerChecks.Replay(root, bounded.Steps, out RoomOutcome outcome);
        Require(keys.Key(actual) == keys.Key(bounded.State) && outcome == bounded.Outcome,
            "Atomic budget fallback failed independent complete-state/RNG replay.");
        BattlePlanResult strict = Slow(false, 128);
        Require(strict.TimedOut && strict.TurnsCompleted == 0 && !strict.CompletedHorizon && !strict.UsedShorterHorizon,
            "A strict caller accepted an unrequested shorter horizon.");
        Reject(strict);
        BattlePlanResult nodeLimited = Slow(true, 1);
        Require(nodeLimited.ReachedNodeLimit && nodeLimited.ExpandedNodes == 1 && nodeLimited.TurnsCompleted == 0 &&
            !nodeLimited.CompletedHorizon && !nodeLimited.UsedShorterHorizon, "Atomic fallback bypassed the hard node budget.");
        Reject(nodeLimited);
        using var cancellation = new CancellationTokenSource();
        int scores = 0;
        BattlePlanResult cancelled = BattlePlanner.Plan(root, keys.Key, Options(128, true), (state, result) =>
        {
            if (++scores == 2) cancellation.Cancel();
            return BattlePlanner.Evaluate(state, result);
        }, cancellation.Token);
        Require(cancelled.Cancelled && cancelled.ExpandedNodes == 1 && cancelled.TurnsCompleted == 0 &&
            !cancelled.CompletedHorizon && !cancelled.UsedShorterHorizon, "Atomic fallback ignored cancellation.");
        Reject(cancelled);
        Require(JsonSerializer.Serialize(root, ModelJson.Options) == immutable, "Atomic budget fallback mutated its native root.");
        Console.WriteLine("ATOMIC-ADVICE-BUDGET PASS: exact native zero-turn failure, completed first-turn fallback, independent full-state/RNG replay, hard node cap, strict horizon and cancellation controls.");

        void Reject(BattlePlanResult plan)
        {
            bool rejected = false;
            try { BattleAdviceSession.Verify(root, plan); }
            catch (InvalidOperationException) { rejected = true; }
            Require(rejected, "Invalid atomic budget forecast reached advice publication.");
        }
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}

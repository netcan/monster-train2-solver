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
        CheckBeam(fixture.GetProperty("DeploymentRoot").Deserialize<BattleTurnState>()!, keys);
        Require(JsonSerializer.Serialize(root, ModelJson.Options) == immutable, "Atomic budget fallback mutated its native root.");
        Console.WriteLine("ATOMIC-ADVICE-BUDGET PASS: native first-turn fallback, soft deadline during beam scoring, full-state/RNG replay, hard node cap, strict horizon and cancellation.");

        void Reject(BattlePlanResult plan)
        {
            bool rejected = false;
            try { BattleAdviceSession.Verify(root, plan); }
            catch (InvalidOperationException) { rejected = true; }
            Require(rejected, "Invalid atomic budget forecast reached advice publication.");
        }
    }

    private static void CheckBeam(BattleTurnState root, BattleStateKeys keys)
    {
        var expected = BattleActionModel.EnumerateActions(root).SupportedCandidates
            .Select(candidate => keys.Key(candidate.Result.State!)).ToArray();
        Require(expected.Length > 1, "The native beam root must have multiple legal inputs.");
        var options = new BattlePlannerOptions(32, 2, 2, maxSteps: 1);
        var expired = RunAfterFirstBeamCandidate(cancel: false);
        var cancelled = RunAfterFirstBeamCandidate(cancel: true);
        Require(expired.Plan.TimedOut && !expired.Plan.Cancelled && expired.Plan.BeamExpandedNodes == 1 &&
            expired.Scored.SequenceEqual(expected), "The soft deadline dropped already-simulated beam candidates.");
        Require(cancelled.Plan.Cancelled && !cancelled.Plan.TimedOut && cancelled.Plan.BeamExpandedNodes == 1 &&
            cancelled.Scored.SequenceEqual(expected.Take(1)), "Beam scoring continued after cancellation.");
        Require(expired.Plan.ExpandedNodes == cancelled.Plan.ExpandedNodes &&
            expired.Plan.SearchMetrics.SimulatedCandidates == cancelled.Plan.SearchMetrics.SimulatedCandidates &&
            expired.Plan.SearchMetrics.SimulatedEndTurns == cancelled.Plan.SearchMetrics.SimulatedEndTurns,
            "Draining already-simulated candidates started more transitions after the soft deadline.");

        (BattlePlanResult Plan, List<string> Scored) RunAfterFirstBeamCandidate(bool cancel)
        {
            using var source = new CancellationTokenSource();
            bool beam = false;
            bool expired = false;
            var scored = new List<string>();
            BattlePlanResult plan = BattlePlanner.PlanCore(root, state =>
            {
                // The first state key requested by PlanCore admits its beam root.
                beam = true;
                return keys.Key(state);
            }, options, (state, outcome) =>
            {
                if (beam)
                {
                    scored.Add(keys.Key(state));
                    if (cancel) source.Cancel();
                    else expired = true;
                }
                return BattlePlanner.Evaluate(state, outcome);
            }, source.Token, optimizeTransitions: true, timeLimitReached: () => expired);
            return (plan, scored);
        }
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}

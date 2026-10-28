using System.Diagnostics;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ExhaustedAdviceChecks
{
    internal static void NativePair(FixtureValue before, FixtureValue after)
    {
        foreach (FixtureValue sample in new[] { before, after })
        {
            Require(sample.GetProperty("EvidenceKind").GetString() == "NativeAdviceResearch", "Not a native advice sample.");
            BattleTurnState previous = sample.GetProperty("PreviousRoot").Deserialize<BattleTurnState>()!;
            BattleTurnState current = sample.GetProperty("ActualRoot").Deserialize<BattleTurnState>()!;
            FixtureValue[] steps = sample.GetProperty("PreviousPlan").GetProperty("Steps").EnumerateArray().ToArray();
            FixtureValue timing = sample.GetProperty("Timing");
            Require(steps.Length == 1 && steps[0].GetProperty("IsEndTurn").GetBoolean() &&
                current.Spawn.Turn > previous.Spawn.Turn &&
                sample.GetProperty("CurrentSearches").GetInt32() == sample.GetProperty("PreviousSearches").GetInt32() + 1 &&
                timing.GetProperty("SearchWorkers").GetInt32() == 2 && timing.GetProperty("SearchMs").GetDouble() > 0 &&
                sample.GetProperty("CurrentPlan").GetProperty("Steps").EnumerateArray().Any(),
                "Sample did not cross an exhausted-plan boundary into a fresh parallel search.");
        }
        foreach (string property in new[] { "PreviousRoot", "ActualRoot" })
            Require(before.GetProperty(property).ContentEquals(after.GetProperty(property)), "Native samples differ in " + property);
        foreach (string property in new[] { "Steps", "State", "Outcome" })
            Require(before.GetProperty("PreviousPlan").GetProperty(property).ContentEquals(after.GetProperty("PreviousPlan").GetProperty(property)),
                "Native previous plans differ in " + property);
        Console.WriteLine($"EXHAUSTED-ADVICE NATIVE PAIR PASS: identical roots and previous plan, fresh parallel searches; verificationMs={before.GetProperty("Timing").GetProperty("VerificationMs").GetDouble():F1}/{after.GetProperty("Timing").GetProperty("VerificationMs").GetDouble():F1} (sample only).");
    }

    internal static void Run(FixtureValue fixture)
    {
        FixtureValue[] rows = fixture.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        FixtureValue last = rows[14];
        BattleTurnState root = last.GetProperty("Root").Deserialize<BattleTurnState>()!;
        BattleTurnState next = last.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        BattleSearchStep[] steps = last.GetProperty("PlanSteps").EnumerateArray().Select(step =>
            new BattleSearchStep(step.GetProperty("Action").Deserialize<PlayCardAction>(), step.GetProperty("IsEndTurn").GetBoolean())).ToArray();
        Require(steps.Length == 1 && steps[0].IsEndTurn && next.Spawn.Turn > root.Spawn.Turn,
            "Missing the recorded exhausted turn boundary.");
        BattleTurnState final = BattlePlannerChecks.Replay(root, steps, out RoomOutcome outcome);
        using (var keys = new BattleStateKeys(ignorePreviewCopyAllocator: true))
            Require(outcome == RoomOutcome.Exchanged && keys.Key(final) == keys.Key(next), "Recorded native final turn differs.");
        BattlePlanResult Plan(BattleSearchStep[] values) => new(final, outcome, values, 0, 1, 0, 0, 0, TimeSpan.Zero,
            false, false, false, false, true, Array.Empty<UnsupportedActionAttempt>(), null);
        string Key(BattleTurnState state) { using var keys = new BattleStateKeys(); return keys.Key(state); }
        BattleAdvice Advice(BattleSearchStep[] values) => new(root, Key(root), Plan(values), TimeSpan.Zero,
            TimeSpan.Zero, TimeSpan.Zero, Stopwatch.GetTimestamp());
        BattleAdvice candidate = Advice(steps);
        Require(BattleAdviceSession.ConsumedFinalTurn(candidate, next) && !BattleAdviceSession.ConsumedFinalTurn(candidate, root),
            "Final end-turn fast path missed its turn boundary or accepted the same turn.");
        Require(!BattleAdviceSession.ConsumedFinalTurn(Advice([steps[0], steps[0]]), next) &&
            !BattleAdviceSession.ConsumedFinalTurn(Advice([new BattleSearchStep(new PlayCardAction(1, 0), false)]), next),
            "The fast path skipped a remaining multi-step plan or card action.");
        var oldPath = Stopwatch.StartNew();
        bool sameBattle = SelectionAdviceReuse.SameBattle(root, next);
        VerifiedAdviceContinuation? oldMatch = SelectionAdviceReuse.MatchAfterObservations(candidate,
            Array.Empty<SelectionAdviceReuse.Observation>(), next, CancellationToken.None);
        bool rebased = SelectionAdviceReuse.TryRebase(candidate, next, true, CancellationToken.None, out _, out _);
        oldPath.Stop();
        Require(oldMatch == null && !sameBattle && !rebased,
            "The original matching path unexpectedly retained an executable continuation.");

        int searches = 0;
        using var session = new BattleAdviceSession((_, _) => Interlocked.Increment(ref searches) == 1 ? Plan(steps) :
            throw new InvalidOperationException("Fresh search reached after the final turn."));
        session.Observe(root, Key(root)); Pump(session, BattleAdviceStatus.Ready);
        session.InvalidateForInput(); session.Observe(next, Key(next)); Pump(session, BattleAdviceStatus.Unavailable);
        Require(searches == 2 && session.Current == null && session.Error == "Fresh search reached after the final turn.",
            "The exhausted plan was published or fresh search was not requested.");
        Console.WriteLine($"EXHAUSTED-ADVICE PASS: recorded native end turn, fresh search, same-turn and remaining-step controls; oldReuseMs={oldPath.Elapsed.TotalMilliseconds:F1}.");
    }

    private static void Pump(BattleAdviceSession session, BattleAdviceStatus expected)
    {
        Require(SpinWait.SpinUntil(() => { session.Poll(); return session.Status is BattleAdviceStatus.Ready or BattleAdviceStatus.Unavailable; }, 45000) &&
            session.Status == expected, "Unexpected advice status: " + session.Status + "; " + session.Error);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OrdinaryAdviceChecks
{
    internal static void NativePair(FixtureValue source, FixtureValue replay)
    {
        BattleAdviceChecks.Native(source, requireBoss: false);
        RollingAdviceChecks.FrozenPair(source, replay);
        foreach (FixtureValue fixture in new[] { source, replay }) TrainCombatChecks.Native(fixture);
        NativeReuse(source, replay);
    }

    internal static void NativeReuse(FixtureValue source, FixtureValue replay)
    {
        FixtureValue overlay = source.GetProperty("PlayerAdvisor").GetProperty("Overlay");
        Require(overlay.GetProperty("Horizon").GetInt32() == 3 &&
            overlay.GetProperty("TimeLimitMs").GetInt32() == 3000 && !overlay.GetProperty("TacticalSeed").GetBoolean(),
            "Ordinary continuation acceptance requires actual player defaults.");
        FixtureValue[] decisions = source.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        FixtureValue[] frozen = replay.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        int ordinary = 0, newTurnSearches = 0;
        for (int index = 0; index < decisions.Length; index++)
        {
            FixtureValue current = decisions[index];
            bool reused = current.GetProperty("ReusedContinuation").GetBoolean();
            bool winning = current.GetProperty("ReusedWinningContinuation").GetBoolean();
            Require(!winning || reused, "Winning reuse lost its general continuation flag.");
            Require(reused == frozen[index].GetProperty("ReusedContinuation").GetBoolean() &&
                winning == frozen[index].GetProperty("ReusedWinningContinuation").GetBoolean(),
                "Frozen replay changed the continuation flags.");
            if (index == 0) { Require(!reused, "The first native publication bypassed search."); continue; }
            FixtureValue previous = decisions[index - 1];
            bool previousWin = previous.GetProperty("PlannedOutcome").Deserialize<RoomOutcome>() == RoomOutcome.BattleWon;
            if (previous.GetProperty("IsEndTurn").GetBoolean() && !previousWin)
            {
                Require(!reused && current.GetProperty("Timing").GetProperty("SearchMs").GetDouble() > 0,
                    "A new native turn reused a shrinking nonwinning horizon.");
                newTurnSearches++;
            }
            if (!reused) continue;
            FixtureValue[] before = previous.GetProperty("PlanSteps").EnumerateArray().ToArray();
            FixtureValue[] after = current.GetProperty("PlanSteps").EnumerateArray().ToArray();
            Require(before.Length == after.Length + 1 && after.Select((step, offset) =>
                    step.ContentEquals(before[offset + 1])).All(equal => equal) &&
                previous.GetProperty("PlannedOutcome").ContentEquals(current.GetProperty("PlannedOutcome")) &&
                previous.GetProperty("Planned").ContentEquals(current.GetProperty("Planned")) &&
                current.GetProperty("Nodes").GetInt32() == 0 &&
                current.GetProperty("Timing").GetProperty("SearchMs").GetDouble() == 0 &&
                current.GetProperty("PolicyBaselineScore").ValueKind == FixtureKind.Null,
                "Retained native advice changed its verified suffix, forecast or search work.");
            if (winning) continue;
            Require(!previous.GetProperty("IsEndTurn").GetBoolean() &&
                current.GetProperty("PlannedOutcome").Deserialize<RoomOutcome>() == RoomOutcome.Exchanged,
                "Ordinary advice crossed an EndTurn or claimed a terminal forecast.");
            foreach (string field in new[] { "RequestedHorizon", "TurnsPlanned", "UsedShorterHorizon" })
                Require(previous.GetProperty(field).ContentEquals(current.GetProperty(field)),
                    "Ordinary native continuation changed the original lookahead: " + field);
            ordinary++;
        }
        Require(ordinary >= 3 && newTurnSearches > 0, "Missing ordinary reuse and a fresh new-turn search witness.");
        Console.WriteLine($"PLAYER-ORDINARY-ADVICE PASS: {ordinary} same-turn continuations with zero search; {newTurnSearches} fresh new-turn searches; exact native suffixes, original lookahead, full forecasts/state/RNG and independent replay.");
    }

    internal static void Run(FixtureValue fixture)
    {
        FixtureValue record = fixture.GetProperty("RollingDecisions").EnumerateArray().First(row =>
            row.GetProperty("PlannedOutcome").Deserialize<RoomOutcome>() == RoomOutcome.Exchanged &&
            !row.GetProperty("IsEndTurn").GetBoolean());
        BattleTurnState root = record.GetProperty("Root").Deserialize<BattleTurnState>()!;
        BattleSearchStep[] steps = record.GetProperty("PlanSteps").EnumerateArray().Select(item => new BattleSearchStep(
            item.GetProperty("Action").ValueKind == FixtureKind.Null ? null : item.GetProperty("Action").Deserialize<PlayCardAction>(),
            item.GetProperty("IsEndTurn").GetBoolean())).ToArray();
        bool shorter = record.GetProperty("UsedShorterHorizon").GetBoolean();
        var plan = new BattlePlanResult(record.GetProperty("Planned").Deserialize<BattleTurnState>()!, RoomOutcome.Exchanged,
            steps, record.GetProperty("Score").GetDouble(), record.GetProperty("TurnsPlanned").GetInt32(), 10, 0, 0,
            TimeSpan.FromSeconds(3), shorter, false, false, false, !shorter, [], null,
            record.GetProperty("RequestedHorizon").GetInt32(), shorter);
        int endTurn = Array.FindIndex(steps, step => step.IsEndTurn);
        Require(endTurn >= 2 && plan.TurnsCompleted >= 2, "A multi-action, multi-turn nonwinning native forecast is required.");
        BattleAdviceSession.Verify(root, plan);
        BattleTurnState state = root;
        var following = new List<BattleTurnState>();
        foreach (BattleSearchStep step in steps)
        {
            state = step.IsEndTurn ? BattleTurnModel.EndTurn(state).State! : BattleActionModel.PlayCard(state, step.Action!).State!;
            Require(state != null, "Unsupported original native forecast."); following.Add(state!);
        }
        using (var keys = new BattleStateKeys(ignorePreviewCopyAllocator: true))
            Require(keys.Key(following[0]) == keys.Key(record.GetProperty("Actual").Deserialize<BattleTurnState>()!),
                "The first native input differs from the retained ordinary forecast.");
        string original = Key(root);
        int calls = 0;
        BattlePlanResult Search(BattleTurnState candidate, CancellationToken token)
        {
            calls++;
            if (Key(candidate) != original) throw new InvalidOperationException("Fresh forecast required.");
            return plan;
        }
        using (var session = new BattleAdviceSession(Search))
        {
            session.Observe(root, original); Pump(session);
            Require(!session.Current!.ReusedContinuation, "Initial publication bypassed search.");
            for (int index = 1; index <= endTurn; index++)
            {
                state = following[index - 1]; long generation = session.Generation;
                session.InvalidateForInput(); Require(session.Current == null, "A play retained visible stale advice.");
                session.Observe(state, Key(state)); Pump(session);
                BattleAdvice advice = session.Current!;
                Require(calls == 1 && session.SearchCount == 1 && session.Generation > generation &&
                    advice.ReusedContinuation && !advice.ReusedWinningContinuation && !advice.ReusedSelection &&
                    advice.RootKey == Key(state) && advice.Plan.ExpandedNodes == 0 && advice.Plan.Elapsed == TimeSpan.Zero &&
                    advice.Plan.Steps.Count == steps.Length - index && advice.Plan.TurnsCompleted == plan.TurnsCompleted &&
                    advice.Plan.RequestedTurns == plan.RequestedTurns && advice.Plan.UsedShorterHorizon == shorter,
                    "A verified ordinary input restarted search, lost its lookahead or claimed victory.");
                BattleAdviceSession.Verify(state, advice.Plan);
            }
            session.InvalidateForInput(); session.Observe(following[endTurn], Key(following[endTurn]));
            Pump(session, BattleAdviceStatus.Unavailable);
            Require(calls == 2 && session.Current == null, "A new turn reused a shrinking ordinary lookahead.");
        }
        foreach (bool explicitReset in new[] { false, true })
        {
            using var session = new BattleAdviceSession(Search);
            session.Observe(root, original); Pump(session);
            int before = calls;
            if (explicitReset) session.Invalidate(); else session.InvalidateForInput();
            state = explicitReset ? following[0] : Copy(following[0], energy: following[0].Energy + 1);
            session.Observe(state, Key(state)); Pump(session, BattleAdviceStatus.Unavailable);
            Require(calls == before + 1 && session.Current == null, "Reset or resource divergence reused ordinary advice.");
        }
        using (var session = new BattleAdviceSession(Search))
        {
            session.Observe(root, original); Pump(session);
            session.InvalidateForInput(); session.Observe(following[1], Key(following[1])); Pump(session);
            Require(session.Current!.ReusedContinuation, "Several forward inputs could not reuse ordinary advice.");
            int before = calls;
            session.InvalidateForInput(); session.Observe(following[0], Key(following[0])); Pump(session, BattleAdviceStatus.Unavailable);
            Require(calls == before + 1, "A backward move reused an obsolete ordinary prefix.");
        }
        BattleRngStream[] changedRng = following[0].RngStreams.Select((stream, index) => index == 0 ?
            new BattleRngStream(stream.Name, stream.Seed, new UnityRng(stream.State.S0 ^ 1, stream.State.S1,
                stream.State.S2, stream.State.S3)) : stream).ToArray();
        using (var session = new BattleAdviceSession(Search))
        {
            session.Observe(root, original); Pump(session); int before = calls;
            state = Copy(following[0], rng: changedRng);
            session.InvalidateForInput(); session.Observe(state, Key(state)); Pump(session, BattleAdviceStatus.Unavailable);
            Require(calls == before + 1, "Different native RNG reused ordinary advice.");
        }
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        int cancelledCalls = 0;
        using (var session = new BattleAdviceSession((_, _) =>
        {
            if (++cancelledCalls > 1) throw new InvalidOperationException("Cancelled forecast needs new search.");
            entered.Set(); if (!release.Wait(10000)) throw new TimeoutException(); return plan;
        }))
        {
            try
            {
                session.Observe(root, original); Require(entered.Wait(10000), "Worker did not start.");
                session.InvalidateForInput(); session.Observe(following[0], Key(following[0])); release.Set();
                Pump(session, BattleAdviceStatus.Unavailable);
                Require(cancelledCalls == 2 && session.Current == null, "Cancelled work installed an ordinary proof.");
            }
            finally { release.Set(); }
        }
        var shorterPlan = new BattlePlanResult(plan.State, plan.Outcome, plan.Steps, plan.Score, plan.TurnsCompleted,
            10, 0, 0, TimeSpan.FromSeconds(3), true, false, true, true, false, [], null, plan.TurnsCompleted + 1, true);
        int shorterCalls = 0;
        using (var session = new BattleAdviceSession((_, _) => { shorterCalls++; return shorterPlan; }))
        {
            session.Observe(root, original); Pump(session);
            session.InvalidateForInput(); session.Observe(following[0], Key(following[0])); Pump(session);
            BattlePlanResult retained = session.Current!.Plan;
            Require(shorterCalls == 1 && session.Current.ReusedContinuation && retained.UsedShorterHorizon &&
                !retained.CompletedHorizon && retained.TimedOut && retained.ReachedNodeLimit && retained.ReachedActionLimit &&
                retained.RequestedTurns == shorterPlan.RequestedTurns && retained.TurnsCompleted == shorterPlan.TurnsCompleted,
                "Retained shorter advice lost its actual horizon or budget limitations.");
            BattleAdviceSession.Verify(following[0], retained);
        }
        Require(Key(root) == original, "Ordinary continuation mutated its native root.");
        Console.WriteLine($"ORDINARY-ADVICE PASS: {endTurn} native-forecast plays, one search, exact roots/full replay, original {plan.TurnsCompleted}-turn lookahead, new-turn replan, shorter-horizon flags, reset/resources/RNG/backward/cancellation rejection.");
    }

    private static BattleTurnState Copy(BattleTurnState state, int? energy = null, BattleRngStream[]? rng = null) =>
        new(state.Spawn, energy ?? state.Energy, state.EnergyPerTurn, state.DrawPerTurn, state.ForgePoints,
            state.DragonsHoard, state.MoonPhase, rng ?? state.RngStreams.ToArray(), state.OtherPiles, state.ExternalInteractions,
            state.PlayRules, state.BattlePreviewEnabled, state.UiRngIsolated, state.CanonicalDecisionReferences,
            state.CanonicalPhysicalReferences, state.SelectedRoom, state.MoonPhaseTrackingActive);
    private static string Key(BattleTurnState state) { using var keys = new BattleStateKeys(); return keys.Key(state); }
    private static void Pump(BattleAdviceSession session, BattleAdviceStatus status = BattleAdviceStatus.Ready)
    {
        Require(SpinWait.SpinUntil(() => { session.Poll(); return session.Status is BattleAdviceStatus.Ready or BattleAdviceStatus.Unavailable; }, 30000),
            "Ordinary continuation worker did not finish.");
        Require(session.Status == status, "Ordinary continuation failed: " + session.Error);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

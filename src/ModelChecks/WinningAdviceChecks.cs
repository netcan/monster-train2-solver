using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class WinningAdviceChecks
{
    internal static void Run(FixtureValue fixture)
    {
        FixtureValue record = fixture.GetProperty("PlannerRuns").EnumerateArray().Single();
        BattleTurnState root = record.GetProperty("Root").Deserialize<BattleTurnState>()!;
        BattleTurnState[] native = record.GetProperty("Actual").EnumerateArray()
            .Select(value => value.Deserialize<BattleTurnState>()!).ToArray();
        BattleSearchStep[] steps = record.GetProperty("Steps").EnumerateArray().Select(step => new BattleSearchStep(
            step.GetProperty("Action").ValueKind == FixtureKind.Null ? null : step.GetProperty("Action").Deserialize<PlayCardAction>(),
            step.GetProperty("IsEndTurn").GetBoolean())).ToArray();
        Require(steps.Length > 2 && native.Length == steps.Length && fixture.GetProperty("NativeWon").GetBoolean(),
            "A complete multi-step native winning fixture is required.");
        var plan = new BattlePlanResult(record.GetProperty("Planned").Deserialize<BattleTurnState>()!, RoomOutcome.BattleWon,
            steps, record.GetProperty("Score").GetDouble(), steps.Count(step => step.IsEndTurn),
            record.GetProperty("Nodes").GetInt32(), 0, 0, TimeSpan.Zero, false, false, false, false, true,
            Array.Empty<UnsupportedActionAttempt>(), null);
        string original = Key(root);
        BattleTurnState predictedFirst = steps[0].IsEndTurn ? BattleTurnModel.EndTurn(root).State! :
            BattleActionModel.PlayCard(root, steps[0].Action!).State!;
        using (var keys = new BattleStateKeys(ignorePreviewCopyAllocator: true))
            Require(keys.Key(predictedFirst) == keys.Key(native[0]),
                "Native successor changed more than the unused preview allocator.");
        int calls = 0;
        BattlePlanResult Search(BattleTurnState state, CancellationToken token)
        {
            Interlocked.Increment(ref calls);
            if (Key(state) != original) throw new InvalidOperationException("Fresh search required for a different state.");
            return plan;
        }
        using (var session = new BattleAdviceSession(Search))
        {
            session.Observe(root, original); Pump(session, BattleAdviceStatus.Ready);
            Require(!session.Current!.ReusedWinningContinuation && calls == 1, "Initial advice bypassed search or verification.");
            for (int index = 1; index < steps.Length; index++)
            {
                long generation = session.Generation;
                session.InvalidateForInput();
                Require(session.Current == null && session.Status == BattleAdviceStatus.Idle, "Input left stale advice visible.");
                BattleTurnState state = native[index - 1];
                session.Observe(state, Key(state)); Pump(session, BattleAdviceStatus.Ready);
                BattleAdvice advice = session.Current!;
                Require(advice.ReusedWinningContinuation && calls == 1 && advice.Plan.ExpandedNodes == 0 &&
                    advice.Plan.Outcome == RoomOutcome.BattleWon && advice.Plan.Steps.Count == steps.Length - index &&
                    advice.Plan.TurnsCompleted == steps.Skip(index).Count(step => step.IsEndTurn) &&
                    ReferenceEquals(advice.Root, state) && advice.RootKey == Key(state) && session.Generation > generation,
                    "A native successor lost its independently verified winning continuation at step " + index);
                Require(advice.Plan.PolicyBaselineScore == null && Key(advice.Plan.State) == Key(plan.State),
                    "Continuation retained a root-specific baseline or changed its proven final state.");
            }
            session.InvalidateForInput();
            session.Observe(native.Last(), Key(native.Last())); Pump(session, BattleAdviceStatus.Unavailable);
            Require(session.Current == null && calls == 2, "Terminal state exposed an empty cached plan.");
        }

        // The native follow-up state is changed in only one field at a time.
        // Neither a different resource nor any different RNG stream may reuse
        // a winning forecast, even when its visible board still matches.
        BattleTurnState following = native[0];
        BattleRngStream[] rng = following.RngStreams.Select((stream, index) => index == 0
            ? new BattleRngStream(stream.Name, stream.Seed ^ 1, stream.State) : stream).ToArray();
        BattleRngStream[] rngState = following.RngStreams.Select((stream, index) => index == 0
            ? new BattleRngStream(stream.Name, stream.Seed, new UnityRng(stream.State.S0 ^ 1, stream.State.S1,
                stream.State.S2, stream.State.S3)) : stream).ToArray();
        Require(rng.Length > 0, "Missing full native RNG streams.");
        foreach (BattleTurnState changed in new[] { Copy(following, energy: following.Energy + 1),
            Copy(following, rng: rng), Copy(following, rng: rngState) })
        {
            using var session = new BattleAdviceSession(Search);
            session.Observe(root, original); Pump(session, BattleAdviceStatus.Ready);
            int before = calls;
            session.InvalidateForInput(); session.Observe(changed, Key(changed)); Pump(session, BattleAdviceStatus.Unavailable);
            Require(session.Current == null && calls == before + 1, "Divergence did not request a fresh search.");
        }
        using (var session = new BattleAdviceSession(Search))
        {
            session.Observe(root, original); Pump(session, BattleAdviceStatus.Ready);
            int before = calls;
            session.InvalidateForInput(); session.Observe(native[2], Key(native[2])); Pump(session, BattleAdviceStatus.Ready);
            Require(calls == before && session.Current!.Plan.Steps.Count == steps.Length - 3,
                "Several settled inputs could not advance along the verified line.");
            session.InvalidateForInput(); session.Observe(following, Key(following)); Pump(session, BattleAdviceStatus.Unavailable);
            Require(calls == before + 1, "A backward move reused an obsolete winning prefix.");
        }
        using (var session = new BattleAdviceSession(Search))
        {
            session.Observe(root, original); Pump(session, BattleAdviceStatus.Ready);
            int before = calls;
            session.Invalidate(); session.Observe(following, Key(following)); Pump(session, BattleAdviceStatus.Unavailable);
            Require(session.Current == null && calls == before + 1, "Explicit refresh/undo retained the old winning proof.");
        }
        using (var session = new BattleAdviceSession(Search))
        {
            session.Observe(root, original); Pump(session, BattleAdviceStatus.Ready);
            session.InvalidateForInput();
            session.Observe(following, original); Pump(session, BattleAdviceStatus.Unavailable);
            Require(session.Current == null && session.Error!.Contains("retained winning continuation"),
                "A supplied key bypassed independent verification of the captured state.");
        }
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        int staleCalls = 0;
        using (var session = new BattleAdviceSession((_, _) =>
        {
            if (Interlocked.Increment(ref staleCalls) != 1) throw new InvalidOperationException("New search after cancellation.");
            entered.Set(); if (!release.Wait(10000)) throw new TimeoutException();
            return plan;
        }))
        {
            try
            {
                session.Observe(root, original); Require(entered.Wait(10000), "Winning search did not start.");
                session.InvalidateForInput(); session.Observe(following, Key(following)); release.Set();
                Pump(session, BattleAdviceStatus.Unavailable);
                Require(staleCalls == 2 && session.Current == null, "Cancelled winning work leaked a proof into another generation.");
            }
            finally { release.Set(); }
        }
        Require(Key(root) == original, "Continuation handling mutated the native root.");
        PreviewAllocatorKeys(following.Spawn.Train.Context!.SpawnPoints!);
        Console.WriteLine($"WINNING-ADVICE PASS nativeSteps={steps.Length} retained={steps.Length - 1}: full native roots/RNG, new generations, divergent resources/RNG, explicit reset, forged keys, cancellation and immutable roots.");
    }

    private static void PreviewAllocatorKeys(BattleSpawnPoints original)
    {
        Require(original.NextPreviewCopyId > 0, "Missing native preview allocator.");
        var advanced = new BattleSpawnPoints(original.Groups, original.Units, original.NextPreviewCopyId + 8);
        using var full = new BattleStateKeys();
        using var continuation = new BattleStateKeys(ignorePreviewCopyAllocator: true);
        Require(full.SnapshotKey(original) != full.SnapshotKey(advanced) &&
            continuation.SnapshotKey(original) == continuation.SnapshotKey(advanced),
            "Continuation normalization changed ordinary full-state keys or retained an unused allocator number.");
        foreach (int? counter in new int?[] { null, 0, -1 })
            Require(continuation.SnapshotKey(original) != continuation.SnapshotKey(
                new BattleSpawnPoints(original.Groups, original.Units, counter)),
                "A missing or invalid preview allocator matched a winning proof.");
        Require(continuation.SnapshotKey(original) != continuation.SnapshotKey(
            new BattleSpawnPoints(original.Groups, original.Units.Skip(1).ToArray(), original.NextPreviewCopyId)),
            "Existing physical references were omitted from winning proof keys.");
    }
    private static BattleTurnState Copy(BattleTurnState state, int? energy = null, BattleRngStream[]? rng = null) =>
        new(state.Spawn, energy ?? state.Energy, state.EnergyPerTurn, state.DrawPerTurn, state.ForgePoints,
            state.DragonsHoard, state.MoonPhase, rng ?? state.RngStreams.ToArray(), state.OtherPiles, state.ExternalInteractions,
            state.PlayRules, state.BattlePreviewEnabled, state.UiRngIsolated, state.CanonicalDecisionReferences,
            state.CanonicalPhysicalReferences, state.SelectedRoom, state.MoonPhaseTrackingActive);
    private static string Key(BattleTurnState state) { using var keys = new BattleStateKeys(); return keys.Key(state); }
    private static void Pump(BattleAdviceSession session, BattleAdviceStatus expected)
    {
        Require(SpinWait.SpinUntil(() => { session.Poll(); return session.Status is BattleAdviceStatus.Ready or BattleAdviceStatus.Unavailable; }, 30000),
            "Winning advice worker did not finish.");
        Require(session.Status == expected, "Unexpected winning advice status: " + session.Status + "; " + session.Error);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

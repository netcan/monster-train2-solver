using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class FloorBrowseAdviceChecks
{
    internal static void LiveScroll(FixtureValue fixture)
    {
        SelectionBoundary(fixture);
        FixtureValue accepted = fixture.GetProperty("LiveAcceptance"), overlay = accepted.GetProperty("Overlay");
        FixtureValue[] observations = fixture.GetProperty("Observations").EnumerateArray().ToArray();
        long searches = observations[0].GetProperty("Observation").GetProperty("Searches").GetInt64();
        long generation = observations[0].GetProperty("Observation").GetProperty("Generation").GetInt64();
        Require(observations.Length == 7 && accepted.GetProperty("SessionSearches").GetInt64() == searches &&
            accepted.GetProperty("Generation").GetInt64() == generation && overlay.GetProperty("PurityFailures").GetInt32() == 0 &&
            overlay.GetProperty("Failures").GetInt32() == 0, "Live scrolling changed search work/generation or failed native purity checks.");
        using var keys = new BattleStateKeys(ignorePreviewCopyAllocator: true);
        foreach (FixtureValue observation in observations)
        {
            BattleTurnState before = observation.GetProperty("Before").Deserialize<BattleTurnState>()!;
            BattleTurnState after = observation.GetProperty("After").Deserialize<BattleTurnState>()!;
            BattleTurnResult selected = BattleTurnModel.SelectRoom(before, after.SelectedRoom);
            Require(selected.Supported && keys.Key(selected.State!) == keys.Key(after) &&
                observation.GetProperty("Observation").GetProperty("Searches").GetInt64() == searches &&
                observation.GetProperty("Observation").GetProperty("Generation").GetInt64() == generation,
                "A live wheel transition differs from complete modeled callbacks/state/RNG or starts a new search.");
        }
        FixtureValue[] reused = overlay.GetProperty("Publications").EnumerateArray()
            .Where(row => row.GetProperty("ReusedSelection").GetBoolean()).ToArray();
        Require(reused.Length == 7 && reused.All(row => row.GetProperty("Generation").GetInt64() == generation &&
            row.GetProperty("SearchMs").GetDouble() == 0), "Missing seven zero-search publications from the real game.");
        BattleTurnState last = observations.Last().GetProperty("After").Deserialize<BattleTurnState>()!;
        Require(keys.Key(last) == keys.Key(accepted.GetProperty("Root").Deserialize<BattleTurnState>()!),
            "The settled live game changed after its final verified scroll.");
        Console.WriteLine($"LIVE-FLOOR-SCROLL PASS: seven native wheel inputs after play, searches={searches} generation={generation} unchanged, exact full native callbacks/state/RNG and zero purity failures.");
    }

    internal static void SelectionBoundary(FixtureValue fixture)
    {
        BattleTurnState before = fixture.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = fixture.GetProperty("After").Deserialize<BattleTurnState>()!;
        using var keys = new BattleStateKeys(ignorePreviewCopyAllocator: true);
        BattleTurnResult selected = BattleTurnModel.SelectRoom(before, after.SelectedRoom);
        Require(selected.Supported, "Native selection is unsupported: " + selected.UnsupportedReason);
        Require(keys.Key(selected.State!) == keys.Key(after), "Modeled selection differs from the full native state/RNG.");
        Require(keys.SnapshotKey(before.Spawn.Train.Context!.CardInstances) !=
            keys.SnapshotKey(after.Spawn.Train.Context!.CardInstances), "Missing native selection-sensitive card upgrades.");
        Require(SelectionAdviceReuse.SameBattle(before, after) && SelectionAdviceReuse.SameBattle(after, before),
            "Native selection callbacks are still classified as a fresh gameplay input.");
        BattleTurnResult ended = BattleTurnModel.EndTurn(before);
        Require(ended.Supported, ended.UnsupportedReason!);
        var plan = new BattlePlanResult(ended.State!, ended.Outcome, [new BattleSearchStep(null, true)],
            BattlePlanner.Evaluate(ended.State!, ended.Outcome), 1, 1, 0, 0, TimeSpan.FromSeconds(3),
            false, false, false, false, true, [], null, 1);
        if (fixture.TryGetProperty("Advice", out FixtureValue advice) && advice.ValueKind != FixtureKind.Null)
        {
            FixtureValue native = advice.GetProperty("Plan");
            BattleSearchStep[] steps = native.GetProperty("Steps").EnumerateArray().Select(step => new BattleSearchStep(
                step.GetProperty("Action").ValueKind == FixtureKind.Null ? null : step.GetProperty("Action").Deserialize<PlayCardAction>(),
                step.GetProperty("IsEndTurn").GetBoolean())).ToArray();
            // The archived advice is an older model prediction, not a future
            // native capture. Replay its exact inputs with the current model;
            // the real native wheel transitions above remain authoritative.
            BattleTurnState forecast = before;
            RoomOutcome outcome = RoomOutcome.Exchanged;
            foreach (BattleSearchStep step in steps)
            {
                if (step.IsEndTurn)
                {
                    BattleTurnResult next = BattleTurnModel.EndTurn(forecast);
                    Require(next.Supported, next.UnsupportedReason!);
                    forecast = next.State!; outcome = next.Outcome;
                }
                else
                {
                    BattleActionResult next = BattleActionModel.PlayCard(forecast, step.Action!);
                    Require(next.Supported, next.Reason!);
                    forecast = next.State!; outcome = next.Outcome;
                }
            }
            Require(outcome == native.GetProperty("Outcome").Deserialize<RoomOutcome>(), "Archived floor-scroll inputs changed their forecast outcome.");
            plan = new BattlePlanResult(forecast, outcome, steps, BattlePlanner.Evaluate(forecast, outcome),
                native.GetProperty("TurnsCompleted").GetInt32(), 1, 0, 0, TimeSpan.FromSeconds(3),
                native.GetProperty("TimedOut").GetBoolean(), false, native.GetProperty("ReachedNodeLimit").GetBoolean(),
                native.GetProperty("ReachedActionLimit").GetBoolean(), native.GetProperty("CompletedHorizon").GetBoolean(),
                [], null, native.GetProperty("RequestedTurns").GetInt32(), native.GetProperty("UsedShorterHorizon").GetBoolean(),
                native.GetProperty("SearchedToBattleEnd").GetBoolean(), native.GetProperty("ReachedStepLimit").GetBoolean(),
                native.GetProperty("MaxSteps").GetInt32());
        }
        DeferredBrowsing(before, plan);
        int searches = 0;
        using var session = new BattleAdviceSession((_, _) =>
        {
            if (++searches != 1) throw new InvalidOperationException("Native floor selection restarted search.");
            return plan;
        });
        session.Observe(before, Key(before)); Pump(session);
        long generation = session.Generation;
        foreach (BattleTurnState root in new[] { after, before, after })
        {
            session.Observe(session.PrepareObservationAsync(root).GetAwaiter().GetResult()); Pump(session);
            Require(searches == 1 && session.Generation == generation && session.Current!.ReusedSelection,
                "Native selection-sensitive browsing restarted or cancelled the existing forecast.");
            BattleAdviceSession.Verify(root, session.Current.Plan);
        }
        Require(!SelectionAdviceReuse.SameBattle(before, Copy(after, energy: after.Energy + 1)),
            "Selection projection hid an unrelated energy change.");
        BattleRngStream[] rng = after.RngStreams.Select((stream, i) => i == 0 ?
            new BattleRngStream(stream.Name, stream.Seed, new UnityRng(stream.State.S0 ^ 1, stream.State.S1,
                stream.State.S2, stream.State.S3)) : stream).ToArray();
        Require(!SelectionAdviceReuse.SameBattle(before, Copy(after, rng: rng)), "Selection projection hid native RNG divergence.");
        Console.WriteLine($"FLOOR-SELECTION PASS: live Conduit/card trackers, exact full native selection transition, three switches with one search, {plan.Steps.Count}-step forecast replay and energy/RNG divergence rejection.");
    }

    private static void DeferredBrowsing(BattleTurnState root, BattlePlanResult plan)
    {
        int searches = 0;
        using var session = new BattleAdviceSession((_, _) => { searches++; return plan; });
        session.Observe(root, Key(root)); Pump(session);
        BattleAdvice original = session.Current!;
        long generation = session.Generation;
        BattleTurnState projected = root;
        foreach (int room in new[] { 2, 1, 0, 1, 2, 1, 0 })
        {
            session.BrowseRoom(room);
            Require(ReferenceEquals(session.Current, original) && session.Generation == generation &&
                session.Status == BattleAdviceStatus.Ready && searches == 1,
                "A view event changed the publication, scheduled a worker, or searched.");
            BattleTurnResult selected = BattleTurnModel.SelectRoom(projected, room);
            Require(selected.Supported, selected.UnsupportedReason!); projected = selected.State!;
        }
        Require(session.MatchesCurrentRoot(projected), "Recorded selection callbacks differ from the complete viewed root.");
        Require(!session.MatchesCurrentRoot(Copy(projected, energy: projected.Energy + 1)),
            "Deferred browsing hid an unrelated gameplay change.");
        BattleRngStream[] changedRng = projected.RngStreams.Select((stream, index) => index == 0 ?
            new BattleRngStream(stream.Name, stream.Seed, new UnityRng(stream.State.S0 ^ 1,
                stream.State.S1, stream.State.S2, stream.State.S3)) : stream).ToArray();
        Require(!session.MatchesCurrentRoot(Copy(projected, rng: changedRng)), "Deferred browsing hid RNG divergence.");
        BattleSearchStep first = plan.Steps[0];
        BattleTurnState after;
        if (first.IsEndTurn)
        { BattleTurnResult next = BattleTurnModel.EndTurn(projected); Require(next.Supported, next.UnsupportedReason!); after = next.State!; }
        else
        { BattleActionResult next = BattleActionModel.PlayCard(projected, first.Action!); Require(next.Supported, next.Reason!); after = next.State!; }
        if (plan.Steps.Count > 1)
        {
            session.InvalidateForInput(); session.Observe(after, Key(after)); Pump(session);
            Require(searches == 1 && session.Current!.ReusedContinuation && session.Current.Plan.Steps.Count == plan.Steps.Count - 1,
                "Following advice after deferred browsing lost its verified suffix.");
            BattleAdviceSession.Verify(after, session.Current.Plan);
        }
        session.Invalidate(); session.Observe(root, Key(root)); Pump(session);
        Require(searches == 2 && session.MatchesCurrentRoot(root), "Manual reset retained an obsolete browsing history.");
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        using var inFlight = new BattleAdviceSession((_, token) =>
        {
            entered.Set();
            if (!release.Wait(10000)) throw new TimeoutException();
            token.ThrowIfCancellationRequested(); return plan;
        });
        try
        {
            inFlight.Observe(root, Key(root)); Require(entered.Wait(10000), "Search did not start.");
            long runningGeneration = inFlight.Generation;
            inFlight.BrowseRoom(1); inFlight.BrowseRoom(2);
            Require(inFlight.Status == BattleAdviceStatus.Planning && inFlight.Generation == runningGeneration &&
                inFlight.SearchCount == 1, "Browsing interfered with an in-flight search.");
            release.Set(); Pump(inFlight);
            Require(ReferenceEquals(inFlight.Current!.Root, root) && inFlight.SearchCount == 1,
                "Browsing replaced the in-flight result or triggered a second worker.");
        }
        finally { release.Set(); }
        Console.WriteLine("DEFERRED-BROWSING PASS: seven O(1) view events preserve the exact publication, then a real input matches and verifies its full remaining state/RNG.");
    }

    internal static void NativePair(FixtureValue source, FixtureValue replay)
    {
        Selection(source, replay);
        BattleAdviceChecks.Native(source, requireBoss: false);
        RollingAdviceChecks.FrozenPair(source, replay);
        foreach (FixtureValue fixture in new[] { source, replay }) TrainCombatChecks.Native(fixture);
        Console.WriteLine("PLAYER-FLOOR-BROWSE PASS: 7 native user floor switches, ready/in-flight reuse, zero new searches, live full roots, complete forecasts/state/RNG and fresh frozen replay.");
    }

    internal static void Selection(FixtureValue source, FixtureValue replay)
    {
        FixtureValue player = source.GetProperty("PlayerAdvisor"), overlay = player.GetProperty("Overlay");
        FixtureValue browse = player.GetProperty("FloorBrowse");
        Require(player.GetProperty("Assembly").GetString() == "MonsterTrain2Poju.Advisor" &&
            browse.GetProperty("Passed").GetBoolean() && browse.GetProperty("DuringSearch").GetBoolean() &&
            browse.GetProperty("Switches").GetInt32() == 7 &&
            replay.GetProperty("PlayerAdvisor").GetProperty("ReplayedFloorSwitches").GetInt32() == 7 &&
            overlay.GetProperty("Horizon").GetInt32() == 3 && overlay.GetProperty("TimeLimitMs").GetInt32() == 3000 &&
            !overlay.GetProperty("TacticalSeed").GetBoolean(), "Missing actual default-budget player browsing and fresh native replay.");
        FixtureValue[] reused = overlay.GetProperty("Publications").EnumerateArray()
            .Where(row => row.TryGetProperty("ReusedSelection", out FixtureValue flag) && flag.GetBoolean()).ToArray();
        Require(reused.Length == 7 && overlay.GetProperty("SelectionReuses").GetInt32() == 7 &&
            reused.Select(row => row.GetProperty("Generation").GetInt64()).Distinct().Count() == 1 &&
            reused.All(row => row.GetProperty("SearchMs").GetDouble() == 0),
            "Floor switches changed the active generation or searched instead of replaying the existing line.");
        if (overlay.TryGetProperty("CrossTurnContinuation", out FixtureValue crossTurn) && crossTurn.GetBoolean())
            Require(browse.GetProperty("WheelInput").GetBoolean(), "New scrolling acceptance bypassed the native wheel path.");
    }

    internal static void Run(FixtureValue fixture)
    {
        FixtureValue record = fixture.GetProperty("PlannerRuns").EnumerateArray().First();
        BattleTurnState root = record.GetProperty("Root").Deserialize<BattleTurnState>()!;
        var steps = new List<BattleSearchStep>();
        BattleTurnState final = root;
        RoomOutcome outcome = RoomOutcome.Exchanged;
        foreach (FixtureValue item in record.GetProperty("Steps").EnumerateArray())
        {
            var step = new BattleSearchStep(item.GetProperty("Action").ValueKind == FixtureKind.Null ? null :
                item.GetProperty("Action").Deserialize<PlayCardAction>(), item.GetProperty("IsEndTurn").GetBoolean());
            steps.Add(step);
            if (step.IsEndTurn)
            {
                BattleTurnResult next = BattleTurnModel.EndTurn(final);
                Require(next.Supported, next.UnsupportedReason!); final = next.State!; outcome = next.Outcome;
                break;
            }
            BattleActionResult played = BattleActionModel.PlayCard(final, step.Action!);
            Require(played.Supported, played.Reason!); final = played.State!; outcome = played.Outcome;
        }
        Require(steps.Count > 0 && steps.Last().IsEndTurn && outcome == RoomOutcome.Exchanged,
            "A complete nonwinning forecast is required to test reuse independently of winning continuations.");
        var plan = new BattlePlanResult(final, outcome, steps, BattlePlanner.Evaluate(final, outcome), 1,
            10, 0, 0, TimeSpan.FromSeconds(3), false, false, false, false, true, [], null, 1);
        string original = Key(root);
        int calls = 0;
        using (var session = new BattleAdviceSession((_, _) => { calls++; return plan; }))
        {
            session.Observe(session.PrepareObservationAsync(root).GetAwaiter().GetResult()); Pump(session);
            long generation = session.Generation;
            var watch = Stopwatch.StartNew();
            BattleTurnState selected = root;
            var calibrated = new Dictionary<int, (BattleTurnState Root, BattleTurnState Final)>();
            foreach (int floor in new[] { 1, 2, 0, 2, 1, 0 })
            {
                selected = Select(selected, floor);
                session.Observe(session.PrepareObservationAsync(selected).GetAwaiter().GetResult()); Pump(session);
                Require(calls == 1 && session.SearchCount == 1 && session.Generation == generation &&
                    session.Current!.RootKey == Key(selected) && session.Current.ReusedSelection &&
                    session.Current.Plan.ExpandedNodes == 0 && session.Current.Plan.Elapsed == TimeSpan.Zero,
                    "Browsing restarted search, changed generation, or published an obsolete native root.");
                BattleAdviceSession.Verify(selected, session.Current.Plan);
                calibrated[floor] = (selected, session.Current.Plan.State);
            }
            foreach (int floor in new[] { 2, 1, 0 })
            {
                var prior = calibrated[floor];
                session.Observe(session.PrepareObservationAsync(prior.Root).GetAwaiter().GetResult()); Pump(session);
                Require(calls == 1 && ReferenceEquals(prior.Final, session.Current!.Plan.State),
                    "Returning to an identical calibrated snapshot replayed the whole forecast.");
                BattleAdviceSession.Verify(prior.Root, session.Current.Plan);
            }
            Console.WriteLine($"FLOOR-BROWSE ready: 6 switches, searches=1, rebaseTotalMs={watch.Elapsed.TotalMilliseconds:F1}.");
            session.InvalidateForInput(); session.Observe(root, original); Pump(session);
            Require(calls == 1 && session.Generation > generation && session.Current!.ReusedContinuation,
                "A settlement boundary with the same full root discarded verified advice.");
            session.Invalidate(); session.Observe(root, original); Pump(session);
            Require(calls == 2, "Explicit refresh reused the cached browsing forecast.");
        }
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        int inFlightCalls = 0;
        using (var session = new BattleAdviceSession((_, token) =>
        {
            inFlightCalls++; entered.Set();
            if (!release.Wait(10000)) throw new TimeoutException();
            Require(!token.IsCancellationRequested, "Browsing cancelled the running search.");
            return plan;
        }))
        {
            try
            {
                session.Observe(session.PrepareObservationAsync(root).GetAwaiter().GetResult());
                Require(entered.Wait(10000), "Worker did not start.");
                long generation = session.Generation;
                BattleTurnState selected = root;
                foreach (int floor in new[] { 1, 2, 0, 1 })
                { selected = Select(selected, floor); session.Observe(session.PrepareObservationAsync(selected).GetAwaiter().GetResult()); }
                release.Set(); Pump(session);
                Require(inFlightCalls == 1 && session.Generation == generation && session.Current!.ReusedSelection &&
                    session.Current.RootKey == Key(selected), "Running search failed to follow the latest floor selection.");
            }
            finally { release.Set(); }
        }
        BattleRngStream[] changedRng = root.RngStreams.Select((stream, index) => index == 0 ?
            new BattleRngStream(stream.Name, stream.Seed, new UnityRng(stream.State.S0 ^ 1, stream.State.S1,
                stream.State.S2, stream.State.S3)) : stream).ToArray();
        CardPlayRule rule = root.PlayRules!.Cards.First();
        JsonNode changedRule = JsonNode.Parse(JsonSerializer.Serialize(rule))!;
        changedRule[nameof(CardPlayRule.Cost)] = rule.Cost + 1;
        var costRules = new BattlePlayRules(root.PlayRules.Rooms,
            root.PlayRules.Cards.Select((card, index) => index == 0 ? changedRule.Deserialize<CardPlayRule>()! : card).ToArray(),
            root.PlayRules.StatusRules, root.PlayRules.UnitCopyCatalog, root.PlayRules.Elixirs, root.PlayRules.ForgeUpgrade);
        foreach (BattleTurnState changed in new[] { Copy(root, root.Energy + 1), Copy(root, rng: changedRng), Copy(root, rules: costRules) })
        {
            int divergentCalls = 0;
            using var session = new BattleAdviceSession((_, _) => ++divergentCalls == 1 ? plan :
                throw new InvalidOperationException("Fresh gameplay search."));
            session.Observe(session.PrepareObservationAsync(root).GetAwaiter().GetResult()); Pump(session);
            long generation = session.Generation;
            session.Observe(session.PrepareObservationAsync(changed).GetAwaiter().GetResult());
            Pump(session, BattleAdviceStatus.Unavailable);
            Require(session.Current == null && divergentCalls == 2 && session.Generation > generation,
                "A resource, card cost or RNG change reused the floor-browsing line.");
        }
        Require(Key(root) == original, "Browsing mutated its source.");
        Console.WriteLine("FLOOR-BROWSE PASS: nonwinning advice, ready/in-flight switches, exact live roots, independent replay, real inputs, resource/cost/RNG divergence and immutable captures.");
    }

    private static BattleTurnState Select(BattleTurnState root, int floor)
    {
        BattleTurnResult selected = BattleTurnModel.SelectRoom(root, floor);
        Require(selected.Supported && SelectionAdviceReuse.SameBattle(root, selected.State!),
            "Fixture selection changes gameplay: " + selected.UnsupportedReason);
        return selected.State!;
    }
    private static BattleTurnState Copy(BattleTurnState state, int? energy = null, BattleRngStream[]? rng = null, BattlePlayRules? rules = null) =>
        new(state.Spawn, energy ?? state.Energy, state.EnergyPerTurn, state.DrawPerTurn, state.ForgePoints,
            state.DragonsHoard, state.MoonPhase, rng ?? state.RngStreams.ToArray(), state.OtherPiles, state.ExternalInteractions,
            rules ?? state.PlayRules, state.BattlePreviewEnabled, state.UiRngIsolated, state.CanonicalDecisionReferences,
            state.CanonicalPhysicalReferences, state.SelectedRoom, state.MoonPhaseTrackingActive);
    private static string Key(BattleTurnState state) { using var keys = new BattleStateKeys(); return keys.Key(state); }
    private static void Pump(BattleAdviceSession session, BattleAdviceStatus status = BattleAdviceStatus.Ready)
    {
        Require(SpinWait.SpinUntil(() => { session.Poll(); return session.Status is BattleAdviceStatus.Ready or BattleAdviceStatus.Unavailable; }, 30000),
            "Floor browsing worker did not finish.");
        Require(session.Status == status, "Floor browsing failed: " + session.Error);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

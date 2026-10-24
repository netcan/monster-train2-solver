using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class FloorBrowseAdviceChecks
{
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
            foreach (int floor in new[] { 1, 2, 0, 2, 1, 0 })
            {
                selected = Select(selected, floor);
                session.Observe(session.PrepareObservationAsync(selected).GetAwaiter().GetResult()); Pump(session);
                Require(calls == 1 && session.SearchCount == 1 && session.Generation == generation &&
                    session.Current!.RootKey == Key(selected) && session.Current.ReusedSelection &&
                    session.Current.Plan.ExpandedNodes == 0 && session.Current.Plan.Elapsed == TimeSpan.Zero,
                    "Browsing restarted search, changed generation, or published an obsolete native root.");
                BattleAdviceSession.Verify(selected, session.Current.Plan);
            }
            Console.WriteLine($"FLOOR-BROWSE ready: 6 switches, searches=1, rebaseTotalMs={watch.Elapsed.TotalMilliseconds:F1}.");
            session.InvalidateForInput(); session.Observe(root, original); Pump(session);
            Require(calls == 2 && session.Generation > generation, "A real input reused browsing advice.");
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
            session.Observe(root, original); Pump(session);
            long generation = session.Generation;
            session.Observe(changed, Key(changed)); Pump(session, BattleAdviceStatus.Unavailable);
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

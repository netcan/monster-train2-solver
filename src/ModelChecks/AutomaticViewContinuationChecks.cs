using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class AutomaticViewContinuationChecks
{
    internal static void Native(FixtureValue mismatch, FixtureValue battle)
    {
        RollingAdviceChecks.ObservedSuccessors(battle);
        Require(battle.GetProperty("NativeWon").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }.All(name => battle.GetProperty(name).GetInt32() == 0),
            "Automatic selection needs a complete accepted native win.");
        var rows = battle.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        var root = rows[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
        var observed = mismatch.GetProperty("PublishedRoot").Deserialize<BattleTurnState>()!;
        var actual = mismatch.GetProperty("ActualRoot").Deserialize<BattleTurnState>()!;
        var steps = rows.Select(row => new BattleSearchStep(row.GetProperty("Action").Deserialize<PlayCardAction>(),
            row.GetProperty("IsEndTurn").GetBoolean())).ToArray();
        var final = BattlePlannerChecks.Replay(root, steps, out RoomOutcome outcome);
        Require(rows.Length == 26 && steps[3].IsEndTurn && outcome == RoomOutcome.BattleWon &&
            BattleTurnChecks.Comparable(final, true) == BattleTurnChecks.Comparable(rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!, true),
            "Missing complete native winning route through the automatic selection boundary.");
        var plan = new BattlePlanResult(final, outcome, steps, BattlePlanner.Evaluate(final, outcome), steps.Count(step => step.IsEndTurn),
            1, 0, 0, TimeSpan.Zero, false, false, false, false, true, [], null, searchedToBattleEnd: true);
        string parent = JsonSerializer.Serialize(observed, ModelJson.Options);
        using var keys = new BattleStateKeys();
        using var session = new BattleAdviceSession((_, _) => plan);
        session.Observe(root, keys.Key(root)); Pump(session);
        var original = session.Current!;
        session.InvalidateForInput();
        session.Observe(observed, keys.Key(observed)); Pump(session);
        Require(session.SearchCount == 1 && session.Current!.ReusedContinuation && session.Current.Plan.Steps.Count == 22 &&
            session.Current.Plan.Outcome == RoomOutcome.BattleWon,
            "Automatic turn focus restarted search or lost the complete winning suffix.");
        BattleAdvice focused = session.Current!;
        Require(SelectionAdviceReuse.TryRebase(focused, actual, true, default, out BattlePlanResult rebased,
            out VerifiedAdviceContinuation? proof) && proof?.ReusedVerifiedSteps == 21,
            "Native automatic focus did not rejoin the full-state proof after one replayed input.");
        var fromActual = BattlePlannerChecks.Replay(actual, rebased.Steps, out RoomOutcome actualOutcome);
        Require(keys.Key(fromActual) == keys.Key(rebased.State) && actualOutcome == rebased.Outcome,
            "Rejoined suffix changed the full forecast state or RNG.");
        long generation = session.Generation;
        session.BrowseRoom(actual.SelectedRoom, force: true);
        Require(session.MatchesCurrentRoot(actual) && session.SearchCount == 1 && session.Generation == generation,
            "Late native view lost the unchanged winning continuation.");
        JsonNode changed = JsonSerializer.SerializeToNode(observed, ModelJson.Options)!;
        changed["Energy"] = observed.Energy + 1;
        Require(SelectionAdviceReuse.MatchAfterObservations(original, [], changed.Deserialize<BattleTurnState>(ModelJson.Options)!, default) == null,
            "Automatic view reuse hid a gameplay change.");
        changed = JsonSerializer.SerializeToNode(observed, ModelJson.Options)!;
        var words = changed["RngStreams"]!.AsArray()[0]!["State"]!.AsObject();
        var word = words.First();
        words[word.Key] = unchecked(word.Value!.GetValue<uint>() + 1);
        Require(SelectionAdviceReuse.MatchAfterObservations(original, [], changed.Deserialize<BattleTurnState>(ModelJson.Options)!, default) == null,
            "Automatic view reuse hid RNG divergence.");
        Require(parent == JsonSerializer.Serialize(observed, ModelJson.Options), "Continuation changed its native parent.");
        Console.WriteLine("AUTOMATIC-VIEW-CONTINUATION PASS: 26 native successors; first EndTurn auto-focus reuses 21 of 22 verified inputs; one search; full state and RNG match; energy/RNG divergence rejected.");
    }

    private static void Pump(BattleAdviceSession session)
    {
        Require(SpinWait.SpinUntil(() => { session.Poll(); return session.Status != BattleAdviceStatus.Planning; }, 30000),
            "Automatic view worker did not finish.");
        Require(session.Status == BattleAdviceStatus.Ready, session.Error ?? "Continuation unavailable.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

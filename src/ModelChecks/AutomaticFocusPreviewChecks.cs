using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class AutomaticFocusPreviewChecks
{
    internal static void Native(FixtureValue mismatch, FixtureValue prefix)
    {
        RollingAdviceChecks.ObservedSuccessors(prefix);
        var published = mismatch.GetProperty("PublishedRoot").Deserialize<BattleTurnState>()!;
        var actual = mismatch.GetProperty("ActualRoot").Deserialize<BattleTurnState>()!;
        var observations = mismatch.GetProperty("Observations").EnumerateArray().ToArray();
        Require(published.Spawn.Turn == 1 && published.SelectedRoom == 2 && actual.SelectedRoom == 1 &&
            observations.Length == 1 && observations[0].GetProperty("Room").GetInt32() == 1 &&
            observations[0].GetProperty("Root").ValueKind == FixtureKind.Null, "Missing original recorded automatic focus.");
        using var keys = new BattleStateKeys(ignorePreviewCopyAllocator: true);
        var preview = BattleTurnModel.SelectRoom(published, 1);
        var focus = BattleTurnModel.SelectRoom(published, 1, refreshPreview: false);
        Require(preview.Supported && focus.Supported && keys.Key(preview.State!) != keys.Key(actual) &&
            keys.Key(focus.State!) == keys.Key(actual) && preview.State!.Spawn.Train.Context!.NextUnitId == 12 &&
            actual.Spawn.Train.Context!.NextUnitId == 11, "Original preview allocation counterexample disappeared.");
        var row = prefix.GetProperty("RollingDecisions").EnumerateArray().Last();
        var root = row.GetProperty("Root").Deserialize<BattleTurnState>()!;
        var steps = row.GetProperty("PlanSteps").EnumerateArray().Select(step => new BattleSearchStep(
            step.GetProperty("Action").Deserialize<PlayCardAction>(), step.GetProperty("IsEndTurn").GetBoolean())).ToArray();
        var final = BattlePlannerChecks.Replay(root, steps, out RoomOutcome outcome);
        Require(steps.Length == 14 && steps[0].IsEndTurn, "Missing retained fourteen-input plan.");
        var plan = new BattlePlanResult(final, outcome, steps, BattlePlanner.Evaluate(final, outcome),
            steps.Count(step => step.IsEndTurn), 1, 0, 0, TimeSpan.Zero, false, false, false, false, true, [], null,
            requestedTurns: steps.Count(step => step.IsEndTurn));
        using var session = new BattleAdviceSession((_, _) => plan);
        using var fullKeys = new BattleStateKeys();
        session.Observe(root, fullKeys.Key(root)); Pump(session);
        session.InvalidateForInput(); session.Observe(published, fullKeys.Key(published)); Pump(session);
        long generation = session.Generation;
        session.BrowseRoom(1, force: true);
        Require(session.MatchesCurrentRoot(actual) && session.SearchCount == 1 && session.Generation == generation,
            "Recorded focus rejected the unchanged battle or restarted search.");
        var advice = session.Current!;
        var nextStep = advice.Plan.Steps[0];
        Require(!nextStep.IsEndTurn && nextStep.Action != null, "Expected first next-turn card play.");
        var next = BattleActionModel.PlayCard(actual, nextStep.Action!);
        Require(next.Supported, next.Reason!);
        session.InvalidateForInput(); session.Observe(next.State!, fullKeys.Key(next.State!)); Pump(session);
        Require(session.SearchCount == 1 && session.Current!.ReusedContinuation && session.Current.Plan.Steps.Count == 12,
            "Playing after automatic focus lost the verified suffix.");
        BattleAdviceSession.Verify(next.State!, session.Current!.Plan);
        foreach (string control in new[] { "Energy", "Allocator", "Rng" })
        {
            JsonNode changed = JsonSerializer.SerializeToNode(actual, ModelJson.Options)!;
            if (control == "Energy") changed["Energy"] = actual.Energy + 1;
            if (control == "Allocator") changed["Spawn"]!["Train"]!["Context"]!["NextUnitId"] = 99;
            if (control == "Rng")
            {
                var words = changed["RngStreams"]!.AsArray()[0]!["State"]!.AsObject();
                var word = words.First(); words[word.Key] = unchecked(word.Value!.GetValue<uint>() + 1);
            }
            Require(SelectionAdviceReuse.MatchAfterObservations(advice,
                [new SelectionAdviceReuse.Observation(1)], changed.Deserialize<BattleTurnState>(ModelJson.Options)!, default) == null,
                "Automatic focus hid divergence: " + control);
        }
        Console.WriteLine("AUTOMATIC-FOCUS-PREVIEW PASS: original seven native inputs; exact focus-only allocation guard; next play retains full12-step suffix with one search; energy/allocator/RNG rejected.");
    }
    private static void Pump(BattleAdviceSession session)
    {
        Require(SpinWait.SpinUntil(() => { session.Poll(); return session.Status != BattleAdviceStatus.Planning; }, 30000),
            "Automatic focus worker did not finish.");
        Require(session.Status == BattleAdviceStatus.Ready, session.Error ?? "Automatic focus unavailable.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

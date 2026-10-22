using System.Text.Json;
using System.Text.Json.Nodes;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class BattlePlannerChecks
{
    internal static void TacticalSeed(FixtureValue fixture, bool naturalRing7 = false)
    {
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(fixture.GetProperty(counter).GetInt32() == 0, "Tactical seed requires accepted native input: " + counter);
        BattleTurnState root = naturalRing7
            ? fixture.GetProperty("Actions")[0].GetProperty("Before").Deserialize<BattleTurnState>()!
            : fixture.GetProperty("RollingDecisions")[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
        Require(root.Spawn.Turn == 0 && (naturalRing7
            ? root.PlayRules!.Cards.Any(rule => rule.AssetKey == "BanishedChamp1" && rule.SpawnUnit != null) &&
                fixture.GetProperty("Actions").GetArrayLength() == 40 && fixture.GetProperty("Turns").GetArrayLength() == 8
            : root.Spawn.QueuedOuterBoss?.Boss.AssetKey == "Boss_Level8_AllyBuffer"),
            "Missing accepted natural deployment input.");
        string original = Key(root);
        using var keys = new BattleStateKeys();
        var options = new BattlePlannerOptions(256, 12, 8, 12, TimeSpan.FromSeconds(30), 2, useTacticalSeed: true);
        BattlePlanResult plan = BattlePlanner.Plan(root, keys.Key, options);
        Require(plan.Outcome == RoomOutcome.BattleWon && plan.CompletedHorizon && plan.ExpandedNodes <= options.MaxNodes &&
            plan.TurnsCompleted <= options.MaxTurns && plan.UnsupportedAttempts.Count == 0,
            "Bounded tactical seed did not find the supported complete winning continuation.");
        BattleTurnState replayed = Replay(root, plan.Steps, out RoomOutcome outcome);
        Require(outcome == RoomOutcome.BattleWon && Key(replayed) == Key(plan.State) &&
            BattlePlanner.Evaluate(replayed, outcome) == plan.Score && Key(root) == original,
            "Tactical plan changed its root or failed independent complete-state/RNG replay.");
        BattlePlanResult cancelled = BattlePlanner.Plan(root, keys.Key, options, cancellationToken: new CancellationToken(true));
        Require(cancelled.Cancelled && cancelled.ExpandedNodes == 0 && cancelled.Steps.Count == 0 && !cancelled.CompletedHorizon,
            "Tactical seed ignored pre-cancellation.");
        BattlePlanResult limited = BattlePlanner.Plan(root, keys.Key,
            new BattlePlannerOptions(1, 12, 8, timeLimit: TimeSpan.FromSeconds(30), useTacticalSeed: true));
        Require(limited.ReachedNodeLimit && limited.ExpandedNodes == 1 && limited.Steps.Count == 1,
            "Tactical seed escaped the planner's shared node budget.");
        Require(!limited.Steps[0].IsEndTurn && limited.Steps[0].Action != null && Key(root) == original,
            "A tactical deployment cut short by its budget must retain its legal deployment prefix without changing the root.");
        if (naturalRing7)
        {
            BattlePlanResult partial = BattlePlanner.Plan(root, keys.Key,
                new BattlePlannerOptions(60, 12, 8, timeLimit: TimeSpan.FromSeconds(30), useTacticalSeed: true));
            BattleTurnState partialReplay = Replay(root, partial.Steps, out RoomOutcome partialOutcome);
            Require(partial.ReachedNodeLimit && partial.ExpandedNodes == 60 && partial.CompletedHorizon &&
                partial.Steps.Count > 1 && !partial.Steps[0].IsEndTurn && partialOutcome == RoomOutcome.PlayerDefeated &&
                Key(partialReplay) == Key(partial.State) && Key(root) == original,
                "A truncated tactical route must retain a complete verified deployment forecast instead of an empty-tower defeat.");
            var later = fixture.GetProperty("Turns")[0].GetProperty("Actual").Deserialize<BattleTurnState>()!;
            string laterKey = Key(later);
            BattlePlanResult laterLimited = BattlePlanner.Plan(later, keys.Key,
                new BattlePlannerOptions(16, 12, 8, timeLimit: TimeSpan.FromSeconds(30), useTacticalSeed: true));
            BattleTurnState laterReplay = Replay(later, laterLimited.Steps, out RoomOutcome laterOutcome);
            Require(laterLimited.CompletedHorizon && laterLimited.Steps.Count > 0 &&
                laterOutcome == laterLimited.Outcome && Key(laterReplay) == Key(laterLimited.State) && Key(later) == laterKey,
                "Tactical advice must preserve a complete verified fallback after deployment too.");
        }
        Require(!new BattlePlannerOptions().UseTacticalSeed, "Existing planner defaults must remain opt-in.");
        Console.WriteLine($"TACTICAL-PLANNER PASS battle={(naturalRing7 ? "Ring7" : "AllyBuffer")} steps={plan.Steps.Count} turns={plan.TurnsCompleted} nodes={plan.ExpandedNodes} " +
            $"elapsedMs={plan.Elapsed.TotalMilliseconds:F1}: complete model victory, independent replay, immutable native root and shared cancellation/node budgets; native winning acceptance remains separate.");
    }

    internal static void NativePlan(FixtureValue fixture)
    {
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(fixture.GetProperty(counter).GetInt32() == 0, "Native planner capture has nonzero " + counter);
        Require(fixture.GetProperty("TerminalEffectsSettled").GetBoolean(), "Native battle did not settle.");
        FixtureValue record = fixture.GetProperty("PlannerRuns").EnumerateArray().Single();
        Require(record.GetProperty("Completed").GetBoolean() && record.GetProperty("Difference").ValueKind == FixtureKind.Null,
            "The native game did not complete every planned step.");
        BattleTurnState state = record.GetProperty("Root").Deserialize<BattleTurnState>()!;
        FixtureValue[] steps = record.GetProperty("Steps").EnumerateArray().ToArray();
        FixtureValue[] actual = record.GetProperty("Actual").EnumerateArray().ToArray();
        FixtureValue[] expected = record.GetProperty("Expected").EnumerateArray().ToArray();
        RoomOutcome[] outcomes = record.GetProperty("Outcomes").Deserialize<RoomOutcome[]>()!;
        Require(steps.Length > 0 && actual.Length == steps.Length && expected.Length == steps.Length && outcomes.Length == steps.Length,
            "Planner step evidence is incomplete.");
        int turns = 0;
        RoomOutcome outcome = RoomOutcome.Exchanged;
        for (int index = 0; index < steps.Length; index++)
        {
            bool end = steps[index].GetProperty("IsEndTurn").GetBoolean();
            PlayCardAction? action = end ? null : steps[index].GetProperty("Action").Deserialize<PlayCardAction>()!;
            Require(end == (steps[index].GetProperty("Action").ValueKind == FixtureKind.Null), "Invalid frozen plan step.");
            state = Replay(state, new[] { new BattleSearchStep(action, end) }, out outcome);
            if (end) turns++;
            bool terminal = outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated or RoomOutcome.Stalemate;
            string wanted = BattleTurnChecks.Comparable(state, terminal);
            Require(outcome == outcomes[index] && wanted == BattleTurnChecks.Comparable(actual[index].Deserialize<BattleTurnState>()!, terminal) &&
                wanted == BattleTurnChecks.Comparable(expected[index].Deserialize<BattleTurnState>()!, terminal),
                "Independent planner/native replay differs at step " + index);
        }
        bool finished = outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated or RoomOutcome.Stalemate;
        Require(finished || turns == record.GetProperty("Horizon").GetInt32(), "Native plan stopped short of its horizon.");
        Require(BattleTurnChecks.Comparable(state, finished) == BattleTurnChecks.Comparable(record.GetProperty("Planned").Deserialize<BattleTurnState>()!, finished),
            "Native final planner state differs.");
        Require(BattlePlanner.Evaluate(state, outcome) == record.GetProperty("Score").GetDouble() &&
            record.GetProperty("Score").GetDouble() <= record.GetProperty("BaselineScore").GetDouble(), "Planner score/baseline differs.");
        if (record.TryGetProperty("PolicyBaselineScore", out FixtureValue policy) && policy.ValueKind == FixtureKind.Number)
            Require(record.GetProperty("Score").GetDouble() <= policy.GetDouble(), "Native recommendation lost the fixed-policy seed.");
        Console.WriteLine($"NATIVE-PLANNER PASS mode={record.GetProperty("Mode").GetString()} steps={steps.Length} turns={turns} " +
            $"score={record.GetProperty("Score").GetDouble()} baseline={record.GetProperty("BaselineScore").GetDouble()} " +
            $"pyre={state.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health}");
    }

    internal static void Pair(FixtureValue searched, FixtureValue replayed)
    {
        NativePlan(searched); NativePlan(replayed);
        FixtureValue first = searched.GetProperty("PlannerRuns").EnumerateArray().Single(), second = replayed.GetProperty("PlannerRuns").EnumerateArray().Single();
        Require(second.GetProperty("Mode").GetString() == "replay" && second.GetProperty("Nodes").GetInt32() == 0,
            "The fresh process must execute the frozen plan without searching again.");
        Require(first.GetProperty("Steps").ContentEquals(second.GetProperty("Steps")), "Fresh native process used a different plan.");
        foreach (string name in new[] { "Root", "Planned" })
            Require(BattleTurnChecks.Comparable(first.GetProperty(name).Deserialize<BattleTurnState>()!) ==
                BattleTurnChecks.Comparable(second.GetProperty(name).Deserialize<BattleTurnState>()!), "Fresh native " + name + " differs.");
        Console.WriteLine("NATIVE-PLANNER-PAIR PASS: identical root, frozen actions, final full state and RNG in independent native processes.");
    }

    internal static void Native(FixtureValue fixture, int turn, int horizon)
    {
        FixtureValue[] rows = fixture.GetProperty("Actions").EnumerateArray().ToArray();
        BattleTurnState root = rows.Select(row => row.GetProperty("Before").Deserialize<BattleTurnState>()!)
            .First(state => state.Spawn.Turn >= turn);
        string original = Key(root);
        SupportedActionCandidate? lazy = BattleActionModel.ChooseUnitSpellAndJunkCandidate(root, out _);
        PlayCardAction? full = BattleActionModel.ChooseUnitSpellAndJunkPlay(root);
        Require(JsonSerializer.Serialize(lazy?.Action) == JsonSerializer.Serialize(full), "Lazy policy changed the existing policy choice.");
        if (lazy != null) Require(Key(lazy.Result.State!) == Key(BattleActionModel.PlayCard(root, lazy.Action).State!),
            "Lazy policy successor differs from applying its selected action.");
        using var keys = new BattleStateKeys();
        using var independentKeys = new BattleStateKeys();
        Require(keys.Key(root) == independentKeys.Key(rows.Select(row => row.GetProperty("Before").Deserialize<BattleTurnState>()!)
            .First(state => state.Spawn.Turn >= turn)), "Independent equivalent snapshots must have equal keys.");
        string rootKey = keys.Key(root);
        foreach (string field in new[] { "Energy", "SelectedRoom", "Rng", "History" })
        {
            JsonNode changed = JsonNode.Parse(original)!;
            if (field == "Rng") changed["RngStreams"]![0]!["State"]!["S0"] = root.RngStreams[0].State.S0 ^ 1u;
            else if (field == "History") changed["Spawn"]!["Train"]!["Context"]!["Statistics"]!["HandCardsPlayedLastTurn"] = new JsonArray(123456);
            else changed[field] = changed[field]!.GetValue<int>() + 1;
            BattleTurnState different = changed.Deserialize<BattleTurnState>()!;
            Require(keys.Key(different) != rootKey, "State deduplication omitted " + field + ".");
        }
        var options = new BattlePlannerOptions(maxNodes: 256, maxTurns: horizon, beamWidth: 8,
            timeLimit: TimeSpan.FromSeconds(3));
        BattlePlanResult plan = BattlePlanner.Plan(root, keys.Key, options);
        Require(plan.CompletedHorizon && plan.Steps.Count > 0 && plan.ExpandedNodes <= options.MaxNodes,
            "No bounded complete-horizon recommendation.");
        Require(plan.UnsupportedAttempts.Count == 0, "Planner encountered unsupported branches: " +
            string.Join("; ", plan.UnsupportedAttempts.Select(attempt => attempt.Reason)));
        BattleTurnState replayed = Replay(root, plan.Steps, out RoomOutcome outcome);
        Require(Key(replayed) == Key(plan.State) && outcome == plan.Outcome, "Planned path does not reproduce its complete state.");
        Require(Key(root) == original, "Planning mutated its source snapshot.");

        BattleTurnState baseline = root;
        RoomOutcome baselineOutcome = RoomOutcome.Exchanged;
        for (int i = 0; i < horizon; i++)
        {
            BattleTurnResult ended = BattleTurnModel.EndTurn(baseline);
            Require(ended.Supported, ended.UnsupportedReason ?? "Baseline failed.");
            baseline = ended.State!; baselineOutcome = ended.Outcome;
            if (baselineOutcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated or RoomOutcome.Stalemate) break;
        }
        Require(plan.Score <= BattlePlanner.Evaluate(baseline, baselineOutcome), "Planner lost its executable no-card fallback.");
        Require(!plan.PolicyBaselineScore.HasValue || plan.Score <= plan.PolicyBaselineScore.Value,
            "Planner lost its completed fixed-policy seed.");

        var cancelled = new CancellationToken(true);
        BattlePlanResult stopped = BattlePlanner.Plan(root, Key, options, cancellationToken: cancelled);
        Require(stopped.Cancelled && stopped.ExpandedNodes == 0 && stopped.Steps.Count == 0 && !stopped.CompletedHorizon,
            "Pre-cancellation must return an unchanged incomplete root.");
        BattlePlanResult expired = BattlePlanner.Plan(root, Key,
            new BattlePlannerOptions(timeLimit: TimeSpan.FromTicks(1)));
        Require(expired.TimedOut && expired.ExpandedNodes == 0 && !expired.CompletedHorizon,
            "Expired budgets must be reported without claiming a complete plan.");
        BattlePlanResult limited = BattlePlanner.Plan(root, Key,
            new BattlePlannerOptions(1, horizon, 1, timeLimit: TimeSpan.FromSeconds(3)));
        Require(limited.ExpandedNodes == 1 && limited.ReachedNodeLimit && limited.Steps.Count == 1,
            "Fallback must consume the same node budget as alternatives.");
        Require(BattlePlanner.Evaluate(root, RoomOutcome.BattleWon) < BattlePlanner.Evaluate(root, RoomOutcome.Exchanged) &&
            BattlePlanner.Evaluate(root, RoomOutcome.Exchanged) < BattlePlanner.Evaluate(root, RoomOutcome.PlayerDefeated),
            "Survival must never outrank a win or lose to a known defeat.");
        Console.WriteLine($"BATTLE-PLANNER PASS turn={root.Spawn.Turn} horizon={horizon} completed={plan.TurnsCompleted} " +
            $"nodes={plan.ExpandedNodes} duplicates={plan.DuplicateStates} beamPruned={plan.BeamPruned} " +
            $"elapsedMs={plan.Elapsed.TotalMilliseconds:F1} timeout={plan.TimedOut} score={plan.Score} " +
            $"baseline={BattlePlanner.Evaluate(baseline, baselineOutcome)} policy={plan.PolicyBaselineScore} steps={plan.Steps.Count} outcome={plan.Outcome}");
    }

    internal static BattleTurnState Replay(BattleTurnState root, IReadOnlyList<BattleSearchStep> steps, out RoomOutcome outcome)
    {
        outcome = RoomOutcome.Exchanged;
        foreach (BattleSearchStep step in steps)
        {
            if (step.IsEndTurn)
            {
                BattleTurnResult result = BattleTurnModel.EndTurn(root);
                Require(result.Supported, result.UnsupportedReason ?? "Planned EndTurn rejected.");
                root = result.State!; outcome = result.Outcome;
            }
            else
            {
                BattleActionResult result = BattleActionModel.PlayCard(root, step.Action!);
                Require(result.Supported, result.Reason ?? "Planned play rejected.");
                root = result.State!; outcome = result.Outcome;
            }
        }
        return root;
    }

    internal static string Key(BattleTurnState state) => JsonSerializer.Serialize(state);
    private static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
}

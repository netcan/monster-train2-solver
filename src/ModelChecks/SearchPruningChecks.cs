using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class SearchPruningChecks
{
    internal static void Run(string[] paths)
    {
        BattleTurnState? opening = null;
        foreach (string path in paths)
        {
            using var fixture = ModelJson.ReadFixture(path);
            BattleTurnState root = fixture.RootElement.GetProperty("Root").Deserialize<BattleTurnState>()!;
            if (root.Spawn.Turn == 0) opening = root;
            using var keys = new BattleStateKeys();
            string parent = keys.Key(root);
            var workspace = new BattleSearchWorkspace();
            var expected = BattleActionModel.EnumerateActions(root);
            var actual = workspace.Enumerate(root);
            Require(expected.SupportedCandidates.Count == actual.SupportedCandidates.Count &&
                JsonSerializer.Serialize(expected.UnsupportedAttempts) == JsonSerializer.Serialize(actual.UnsupportedAttempts),
                "Filtering changed the legal frontier or unsupported diagnostics: " + path);
            for (int index = 0; index < expected.SupportedCandidates.Count; index++)
            {
                var left = expected.SupportedCandidates[index]; var right = actual.SupportedCandidates[index];
                Require(JsonSerializer.Serialize(left.Action) == JsonSerializer.Serialize(right.Action) &&
                    left.Result.Outcome == right.Result.Outcome && keys.Key(left.Result.State!) == keys.Key(right.Result.State!),
                    "Filtering changed candidate order, full state or RNG: " + path);
            }
            Require(ReferenceEquals(actual, workspace.Enumerate(root)), "Repeated enumeration was simulated again.");
            if (actual.SupportedCandidates.Count > 0)
            {
                var action = actual.SupportedCandidates[0].Action;
                int before = workspace.Metrics.SimulatedCandidates;
                Require(ReferenceEquals(workspace.Play(root, action), actual.SupportedCandidates[0].Result) &&
                    workspace.Metrics.SimulatedCandidates == before, "Tactical/frontier action was simulated twice.");
                var clone = JsonSerializer.Deserialize<BattleTurnState>(JsonSerializer.Serialize(root), ModelJson.Options)!;
                workspace.Play(clone, action);
                Require(workspace.Metrics.SimulatedCandidates == before + 1, "Distinct state references shared an unchecked transition.");
                // Complex choice payloads cannot alias an otherwise equal simple input.
                var complex = new PlayCardAction(action.CardInstanceId, action.RoomIndex, action.PlayerPosition, action.TargetUnitId,
                    recursionSelections: new[] { new CardRecursionSelection(99, new[] { 1 }) });
                workspace.Play(root, complex); workspace.Play(root, complex);
                Require(workspace.Metrics.SimulatedCandidates == before + 3, "Complex choices entered the incomplete simple-input cache.");
                foreach (var changed in new[] {
                    new PlayCardAction(action.CardInstanceId, action.RoomIndex, action.PlayerPosition, action.TargetUnitId, targetSpawnPointIndex: 0),
                    new PlayCardAction(action.CardInstanceId, action.RoomIndex, action.PlayerPosition, action.TargetUnitId, activatorRoomIndex: 0) })
                {
                    var plain = BattleActionModel.PlayCard(root, changed); var cached = workspace.Play(root, changed);
                    Require(plain.Rejection == cached.Rejection && plain.Reason == cached.Reason &&
                        (plain.State == null ? cached.State == null : keys.Key(plain.State) == keys.Key(cached.State!)),
                        "Activator-room or spawn-point inputs collided.");
                }
            }
            var ended = workspace.EndTurn(root);
            Require(ReferenceEquals(ended, workspace.EndTurn(root)) && workspace.Metrics.CachedEndTurns == 1 &&
                keys.Key(root) == parent, "EndTurn cache missed or changed native parent/RNG.");
            var metrics = workspace.Metrics;
            Require(metrics.CandidateRequests == metrics.SimulatedCandidates + metrics.CachedCandidates + metrics.PrefilteredCandidates,
                "Candidate metrics do not account for all requested transitions.");
            Console.WriteLine($"SEARCH-FRONTIER PASS {Path.GetFileName(path)} legal={actual.SupportedCandidates.Count} simulated={metrics.SimulatedCandidates} cached={metrics.CachedCandidates} filtered={metrics.PrefilteredCandidates} fullStateRngEqual=true");
        }
        Require(opening != null, "Missing real playable opening for controlled search comparison.");
        using var searchKeys = new BattleStateKeys();
        var options = new BattlePlannerOptions(128, beamWidth: 8, timeLimit: TimeSpan.FromSeconds(60),
            useTacticalSeed: true, allowShorterHorizon: true, searchToBattleEnd: true);
        // Fixed logical node budget and alternating order separate saved work
        // from merely exploring fewer branches or warming up the runtime.
        var results = new List<(bool Optimized, BattlePlanResult Plan)>();
        foreach (bool optimized in new[] { false, true, true, false })
        {
            var plan = BattlePlanner.PlanCore(opening!, searchKeys.Key, options, null, default, optimized);
            Require(!plan.TimedOut && plan.ExpandedNodes == options.MaxNodes, "Comparison did not finish its fixed node budget.");
            BattleAdviceSession.Verify(opening!, plan);
            if (results.Count > 0)
                Require(plan.Score == results[0].Plan.Score && plan.Outcome == results[0].Plan.Outcome &&
                    searchKeys.Key(plan.State) == searchKeys.Key(results[0].Plan.State) &&
                    JsonSerializer.Serialize(plan.Steps) == JsonSerializer.Serialize(results[0].Plan.Steps),
                    "Memoization changed the fixed-budget recommendation, state or RNG.");
            results.Add((optimized, plan));
            var m = plan.SearchMetrics;
            Console.WriteLine($"SEARCH-AB optimized={optimized} elapsedMs={plan.Elapsed.TotalMilliseconds:F0} nodes={plan.ExpandedNodes} simulated={m.SimulatedCandidates} cached={m.CachedCandidates} filtered={m.PrefilteredCandidates} endTurns={m.SimulatedEndTurns} cachedEndTurns={m.CachedEndTurns} cachedPolicies={m.CachedPolicies} duplicates={plan.DuplicateStates} beamPruned={plan.BeamPruned}");
        }
        var baseline = results.First(x => !x.Optimized).Plan.SearchMetrics;
        var improved = results.First(x => x.Optimized).Plan.SearchMetrics;
        Require(improved.SimulatedCandidates < baseline.SimulatedCandidates && improved.SimulatedEndTurns < baseline.SimulatedEndTurns &&
            improved.PrefilteredCandidates > 0 && improved.CachedPolicies > 0, "Optimization did not eliminate real repeated/illegal work.");
        double oldMs = results.Where(x => !x.Optimized).Average(x => x.Plan.Elapsed.TotalMilliseconds);
        double newMs = results.Where(x => x.Optimized).Average(x => x.Plan.Elapsed.TotalMilliseconds);
        Console.WriteLine($"SEARCH-PRUNING PASS baselineMs={oldMs:F0} optimizedMs={newMs:F0} savedCandidates={baseline.SimulatedCandidates-improved.SimulatedCandidates} savedEndTurns={baseline.SimulatedEndTurns-improved.SimulatedEndTurns} identicalPlanStateRng=true");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

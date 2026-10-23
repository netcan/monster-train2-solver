using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class BattleHeuristicChecks
{
    private static BattleTurnState CapturedRoot(FixtureValue fixture) =>
        (fixture.TryGetProperty("Root", out var root) ? root :
            fixture.GetProperty("RollingDecisions")[0].GetProperty("Root")).Deserialize<BattleTurnState>()!;

    internal static void DeploymentForecast(FixtureValue fixture)
    {
        BattleTurnState root = CapturedRoot(fixture);
        if (root.Spawn.Turn != 0) throw new InvalidOperationException("Deployment input required.");
        string original = JsonSerializer.Serialize(root);
        using var keys = new BattleStateKeys();
        foreach (int floor in new[] { 0, 1, 2 })
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            BattleTurnState state = root;
            RoomOutcome outcome = RoomOutcome.Exchanged;
            var steps = new List<BattleSearchStep>();
            for (int index = 0; index <= 12; index++)
            {
                var candidate = index == 12 ? null : BattleTacticalPolicy.Choose(state, out _, floor);
                var step = new BattleSearchStep(candidate?.Action, candidate == null);
                if (candidate != null) { state = candidate.Result.State!; outcome = candidate.Result.Outcome; }
                else EndTurn();
                steps.Add(step);
                if (step.IsEndTurn || outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated or RoomOutcome.Stalemate) break;
            }
            while (steps.Count(step => step.IsEndTurn) < 3 &&
                outcome is not (RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated or RoomOutcome.Stalemate))
            {
                var step = new BattleSearchStep(null, true);
                EndTurn(); steps.Add(step);
            }
            double forecastMs = watch.Elapsed.TotalMilliseconds;
            BattleTurnState replayed = BattlePlannerChecks.Replay(root, steps, out RoomOutcome replayOutcome);
            if (keys.Key(replayed) != keys.Key(state) || replayOutcome != outcome || JsonSerializer.Serialize(root) != original)
                throw new InvalidOperationException("Deployment forecast replay or source isolation failed.");
            var units = state.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => unit.Health > 0).ToArray();
            double position = units.Sum(unit => unit.IsPyre ? 0d : unit.Team == CombatTeam.Enemy ?
                unit.Health + (double)(unit.Status("armor")?.Stacks ?? 0) :
                -(Math.Max(0d, unit.Attack) * (1d + Math.Max(0, unit.Status("multistrike")?.Stacks ?? 0)) +
                unit.Health * .1 + Math.Max(0, unit.Status("armor")?.Stacks ?? 0) * .1));
            Console.WriteLine($"DEPLOYMENT-FORECAST floor={floor} score={BattlePlanner.Evaluate(state, outcome)} " +
                $"position={position} inputs={steps.Count} outcome={outcome} forecastMs={forecastMs:F1} elapsedMs={watch.Elapsed.TotalMilliseconds:F1}; model only.");

            void EndTurn()
            {
                BattleTurnResult ended = BattleTurnModel.EndTurn(state);
                if (!ended.Supported) throw new InvalidOperationException(ended.UnsupportedReason);
                state = ended.State!; outcome = ended.Outcome;
                int completed = steps.Count(step => step.IsEndTurn) + 1;
                if (completed < 3)
                {
                    var current = state.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => unit.Health > 0);
                    double position = current.Sum(unit => unit.IsPyre ? 0d : unit.Team == CombatTeam.Enemy ?
                        unit.Health + (double)(unit.Status("armor")?.Stacks ?? 0) :
                        -(Math.Max(0d, unit.Attack) * (1d + Math.Max(0, unit.Status("multistrike")?.Stacks ?? 0)) +
                            unit.Health * .1 + Math.Max(0, unit.Status("armor")?.Stacks ?? 0) * .1));
                    Console.WriteLine($"DEPLOYMENT-PREFIX floor={floor} turns={completed} score={BattlePlanner.Evaluate(state, outcome)} " +
                        $"position={position} forecastMs={watch.Elapsed.TotalMilliseconds:F1}");
                    if (completed == 2)
                        Console.WriteLine("DEPLOYMENT-ALLIES " + string.Join(";", state.Spawn.Train.Rooms.SelectMany(room =>
                            room.Units.Where(unit => unit.Team == CombatTeam.Player && !unit.IsPyre && unit.Health > 0)
                                .Select(unit => $"{room.RoomIndex}:{unit.AssetKey}:{unit.Attack}/{unit.Health}/{unit.MaxHealth}"))));
                }
            }
        }
        var plan = BattlePlanner.Plan(root, keys.Key, new BattlePlannerOptions(256, 3, 8,
            timeLimit: TimeSpan.FromSeconds(3), allowShorterHorizon: true));
        BattleAdviceSession.Verify(root, plan);
        if (JsonSerializer.Serialize(root) != original) throw new InvalidOperationException("Deployment planning changed the root.");
        Console.WriteLine($"DEPLOYMENT-PLAN room={plan.Steps[0].Action?.RoomIndex} turns={plan.TurnsCompleted} " +
            $"score={plan.Score} nodes={plan.ExpandedNodes} ms={plan.Elapsed.TotalMilliseconds:F1}");
    }

    internal static RoomOutcome RollingForecast(FixtureValue fixture, int maxNodes, int preferredDeploymentRoom = 2,
        bool retainCurrentTurn = false)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        BattleTurnState root = CapturedRoot(fixture);
        string original = JsonSerializer.Serialize(root);
        BattleTurnState state = root;
        var steps = new List<BattleSearchStep>();
        int nodes = 0;
        for (int decision = 0; steps.Count < 200; decision++)
        {
            using var keys = new BattleStateKeys();
            BattlePlanResult plan = BattlePlanner.Plan(state, keys.Key,
                new BattlePlannerOptions(maxNodes, 3, 8, timeLimit: TimeSpan.FromSeconds(3),
                    preferredDeploymentRoom: preferredDeploymentRoom, allowShorterHorizon: retainCurrentTurn));
            if ((!plan.CompletedHorizon && !plan.UsedShorterHorizon) || plan.Steps.Count == 0)
                throw new InvalidOperationException("No complete rolling forecast.");
            if (plan.Cancelled || plan.ExpandedNodes > maxNodes || plan.RequestedTurns != 3 || plan.TurnsCompleted > 3)
                throw new InvalidOperationException("Rolling forecast exceeded its shared budget or requested horizon.");
            BattleAdviceSession.Verify(state, plan);
            int count = !retainCurrentTurn ? 1 : plan.Outcome == RoomOutcome.BattleWon ? plan.Steps.Count :
                plan.Outcome == RoomOutcome.Exchanged ? plan.Steps.TakeWhile(step => !step.IsEndTurn).Count() + 1 : 1;
            BattleSearchStep[] prefix = plan.Steps.Take(count).ToArray();
            if (decision == 0) Console.WriteLine($"ROLLING-DEPLOYMENT room={prefix[0].Action?.RoomIndex} turns={plan.TurnsCompleted} nodes={plan.ExpandedNodes}");
            state = BattlePlannerChecks.Replay(state, prefix, out RoomOutcome outcome);
            steps.AddRange(prefix); nodes += plan.ExpandedNodes;
            if (prefix.Last().IsEndTurn)
                Console.WriteLine($"ROLLING-FORECAST-TURN turn={state.Spawn.Turn} outcome={outcome} pyre={state.Spawn.Train.Rooms.SelectMany(room => room.Units).SingleOrDefault(unit => unit.IsPyre)?.Health ?? 0} decisions={steps.Count}");
            if (outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated or RoomOutcome.Stalemate)
            {
                BattleTurnState replayed = BattlePlannerChecks.Replay(root, steps, out RoomOutcome replayOutcome);
                if (JsonSerializer.Serialize(replayed) != JsonSerializer.Serialize(state) || replayOutcome != outcome ||
                    JsonSerializer.Serialize(root) != original) throw new InvalidOperationException("Rolling forecast replay or source isolation failed.");
                Console.WriteLine($"ROLLING-FORECAST outcome={outcome} decisions={steps.Count} nodes={nodes} searches={decision + 1} retainCurrentTurn={retainCurrentTurn} elapsedMs={watch.Elapsed.TotalMilliseconds:F1}; model-only diagnosis, native scheduling may choose another bounded path.");
                return outcome;
            }
        }
        throw new InvalidOperationException("Rolling forecast exceeded 200 decisions.");
    }

    // Fast offline strategy diagnosis from a captured native root. This does
    // not count as a native victory or independent native replay acceptance.
    internal static RoomOutcome Forecast(FixtureValue fixture, int preferredDeploymentRoom = 2)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        BattleTurnState root = CapturedRoot(fixture);
        string original = JsonSerializer.Serialize(root);
        BattleTurnState state = root;
        RoomOutcome outcome = RoomOutcome.Exchanged;
        var steps = new List<BattleSearchStep>();
        var omittedReasons = new HashSet<string>();
        for (int decision = 0; decision < 200; decision++)
        {
            var chosen = BattleHeuristicPolicy.Choose(state, out var omitted, preferredDeploymentRoom);
            foreach (var attempt in omitted) omittedReasons.Add(attempt.Reason);
            if (chosen != null)
            {
                state = chosen.Result.State!; outcome = chosen.Result.Outcome;
                steps.Add(new BattleSearchStep(chosen.Action, false));
            }
            else
            {
                var ended = BattleTurnModel.EndTurn(state);
                if (!ended.Supported) throw new InvalidOperationException(ended.UnsupportedReason);
                state = ended.State!; outcome = ended.Outcome;
                steps.Add(new BattleSearchStep(null, true));
            }
            if (outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated or RoomOutcome.Stalemate)
            {
                var replay = BattlePlannerChecks.Replay(root, steps, out var replayOutcome);
                if (JsonSerializer.Serialize(replay) != JsonSerializer.Serialize(state) || outcome != replayOutcome ||
                    JsonSerializer.Serialize(root) != original) throw new InvalidOperationException("Heuristic forecast replay or source immutability failed.");
                Console.WriteLine($"HEURISTIC-FORECAST outcome={outcome} decisions={steps.Count} score={BattlePlanner.Evaluate(state, outcome)} elapsedMs={watch.Elapsed.TotalMilliseconds:F1} omittedReasons={string.Join("; ", omittedReasons)}; model-only diagnosis.");
                return outcome;
            }
        }
        throw new InvalidOperationException("Heuristic forecast exceeded 200 decisions.");
    }
}

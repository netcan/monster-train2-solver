using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static partial class OfflineBattleSearch
{
    // Counterexample investigation only. Keep improvements between sweeps so
    // two useful changes can compose instead of restarting every trial at the
    // original route. A result is still a model prediction requiring native replay.
    internal static void Improve(string input, string output, int milliseconds, int maxPasses = 4,
        string? prediction = null, int startDecision = 0, bool exploreCardOrder = false, bool retainSuffix = false,
        int secondActionWidth = 0, bool improveWinningRoute = false, int perDecisionCandidateLimit = 0,
        int thirdActionWidth = 0, int diversificationWidth = 0, int maxDecisionCount = 256, bool preferLaterDefeat = false)
    {
        Require(milliseconds is >= 100 and <= 600000 && maxPasses is >= 1 and <= 16,
            "Expected100..600000ms and1..16 improvement passes.");
        Require(secondActionWidth is >= 0 and <= 32, "Expected0..32 second-action alternatives.");
        Require(thirdActionWidth is >= 0 and <= 16 && (thirdActionWidth == 0 || secondActionWidth > 0),
            "Expected0..16 third-action alternatives and a positive second-action width for triples.");
        Require(perDecisionCandidateLimit is >= 0 and <= 256, "Expected0..256 candidates per decision;0 keeps exhaustive local enumeration.");
        Require(diversificationWidth is >= 0 and <= 16 && maxDecisionCount is >= 1 and <= 256 &&
            (diversificationWidth == 0 || retainSuffix && perDecisionCandidateLimit > 0),
            "Expected0..16 alternative routes and1..256 decisions; diversification requires a bounded retained suffix.");
        Require(!preferLaterDefeat || !improveWinningRoute, "Survival investigation requires a losing baseline.");
        if (File.Exists(output)) throw new IOException("Output already exists: " + output);
        string unsupportedOutput = output + ".unsupported.mt2f";
        if (retainSuffix && File.Exists(unsupportedOutput)) throw new IOException("Output already exists: " + unsupportedOutput);
        if (diversificationWidth > 0 && maxPasses == 1)
            Console.WriteLine("IMPROVE-DIVERSIFY WARNING: one pass cannot explore retained alternative routes; use at least two passes.");
        using var document = FixtureDocument.Read(input);
        FixtureValue trace = document.RootElement;
        RequireNativeInput(trace);
        using var predicted = prediction == null ? null : FixtureDocument.Read(prediction);
        if (predicted != null)
        {
            Verify(prediction!);
            Require(predicted.RootElement.GetProperty("EvidenceKind").GetString() == "ModelPrediction" &&
                predicted.RootElement.GetProperty("SourceSHA256").GetString() ==
                    Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(input))).ToLowerInvariant(),
                "Prediction must be independently verified and reference this exact native input.");
        }
        BattleTurnState root = DeploymentRoot(trace);
        Require(root.Spawn.Turn == 0, "Improvement requires a native deployment root.");
        var route = (predicted?.RootElement ?? trace).GetProperty("RollingDecisions").EnumerateArray().Select(row => new Step {
            Action = row.GetProperty("Action").Deserialize<PlayCardAction>(),
            IsEndTurn = row.GetProperty("IsEndTurn").GetBoolean() }).ToArray();
        Require(route.Length is > 0 and <= 256 && startDecision >= 0 && startDecision < route.Length,
            "Expected a bounded complete native battle and valid starting decision.");
        using var keys = new BattleStateKeys();
        string immutable = keys.Key(root);
        Require(predicted == null || keys.Key(predicted.RootElement.GetProperty("Root").Deserialize<BattleTurnState>()!) == immutable,
            "Prediction changed the native deployment root.");
        // These states come exclusively from this model-owned replay. Keep the
        // unchanged prefix after a local improvement instead of simulating the
        // whole incumbent again on every sweep. Final export still requires an
        // independent replay from the original deployment root.
        var prefixes = new List<BattleTurnState>();
        BattleTurnState final = Replay(route, out RoomOutcome outcome, prefixes);
        Require(!improveWinningRoute || outcome == RoomOutcome.BattleWon,
            "Winning-route improvement requires an already verified complete model win.");
        double best = ImprovementMerit(final, outcome);
        Require(!preferLaterDefeat || outcome == RoomOutcome.PlayerDefeated, "Survival investigation requires a complete losing baseline.");
        // Keep the best complete route independently of the route currently
        // being explored. A useful pair of deployment changes can make a
        // worse complete loss after the first change. A bounded frontier lets
        // the second change be investigated without exporting that regression.
        var bestRoute = route;
        var bestFinal = final;
        var bestOutcome = outcome;
        var frontier = new List<PolicyAlternative>();
        var exploredRoutes = new HashSet<string>(StringComparer.Ordinal);
        if (diversificationWidth > 0) exploredRoutes.Add(JsonSerializer.Serialize(route));
        int diversifiedRoutes = 0;
        var watch = Stopwatch.StartNew();
        var tried = new HashSet<string>(StringComparer.Ordinal);
        int candidates = 0, completed = 0, unsupported = 0, improvements = 0;
        int pairCandidates = 0, pairImprovements = 0;
        int tripleCandidates = 0, tripleImprovements = 0;
        int visitedDecisions = 0, lastDecision = -1;
        int retainedSteps = 0, skippedSuffixInputs = 0, retargetedSuffixInputs = 0;
        var reasons = new HashSet<string>(StringComparer.Ordinal);
        void Unsupported(string reason, PlayCardAction? action, BattleTurnState failedRoot, IReadOnlyList<Step> failedPrefix)
        {
            unsupported++;
            if (!retainSuffix) return;
            if (reasons.Add(reason)) Console.WriteLine($"IMPROVE-UNSUPPORTED action={JsonSerializer.Serialize(action)} reason={reason}");
            if (File.Exists(unsupportedOutput)) return;
            string? reproduced = action == null ? BattleTurnModel.EndTurn(failedRoot).UnsupportedReason :
                BattleActionModel.PlayCard(failedRoot, action).Reason;
            if (reproduced != reason) return;
            BattleTurnState replay = root;
            RoomOutcome replayOutcome = RoomOutcome.Exchanged;
            foreach (Step step in failedPrefix)
            {
                Require(!Terminal(replayOutcome), "Failure prefix follows a terminal input.");
                replay = Apply(replay, step.Action, step.IsEndTurn, out replayOutcome);
            }
            Require(keys.Key(replay) == keys.Key(failedRoot) && keys.Key(root) == immutable,
                "Failure prefix changed its native root or failed to reach the rejected action.");
            using var failure = NativeFixtureCapture.Capture(new { Schema = 1, EvidenceKind = "ModelUnsupportedBranch",
                SourceArchive = Path.GetFileName(input),
                SourceSHA256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(input))).ToLowerInvariant(),
                Root = root, Prefix = failedPrefix, FailedRoot = failedRoot, Action = action, IsEndTurn = action == null,
                Reason = reason, NativeValidationRequired = true });
            using var stream = new FileStream(unsupportedOutput, FileMode.CreateNew, FileAccess.Write);
            failure.Write(stream);
            Console.WriteLine($"IMPROVE-BRANCH prefix={failedPrefix.Count} output={unsupportedOutput}; native observation required.");
        }
        bool Expired() => watch.ElapsedMilliseconds >= milliseconds;
        for (int pass = 0; pass < maxPasses && (outcome != RoomOutcome.BattleWon || improveWinningRoute) && !Expired(); pass++)
        {
            bool improved = false;
            for (int index = startDecision; index < Math.Min(prefixes.Count, startDecision + maxDecisionCount) && !Expired(); index++)
            {
                visitedDecisions++; lastDecision = index;
                int decisionCandidates = 0;
                bool DecisionBudgetReached() => perDecisionCandidateLimit > 0 && decisionCandidates >= perDecisionCandidateLimit;
                BattleTurnState entry = prefixes[index];
                Step[] prefix = route.Take(index).ToArray();
                string parent = keys.Key(entry);
                Step recorded = route[index];
                var legal = BattleActionModel.EnumerateActions(entry, exploreCardOrder ? null : recorded.Action?.CardInstanceId,
                    includeAbilities: exploreCardOrder || recorded.Action == null || recorded.Action.ActivatorUnitId != 0);
                foreach (var attempt in legal.UnsupportedAttempts)
                    Unsupported(attempt.Reason ?? "Unsupported alternative.", attempt.Action, entry, prefix);
                string recordedAction = JsonSerializer.Serialize(recorded.Action);
                var alternatives = legal.SupportedCandidates.Where(candidate =>
                    JsonSerializer.Serialize(candidate.Action) != recordedAction &&
                    (exploreCardOrder || recorded.Action == null || candidate.Action.ActivatorUnitId == recorded.Action.ActivatorUnitId))
                    .Select(candidate => (Step: new Step { Action = candidate.Action },
                        State: candidate.Result.State!, Outcome: candidate.Result.Outcome)).ToList();
                if (!recorded.IsEndTurn)
                {
                    BattleTurnResult ended = BattleTurnModel.EndTurn(entry);
                    if (ended.Supported) alternatives.Add((new Step { IsEndTurn = true }, ended.State!, ended.Outcome));
                    else Unsupported(ended.UnsupportedReason ?? "Unsupported EndTurn.", null, entry, prefix);
                }
                // A small decision budget must reach different cards before
                // spending its quota on one card's targets and second inputs.
                // Rank already-owned successors, keeping the full neighborhood
                // available after one best single input per card or ability.
                var ordered = alternatives.OrderBy(candidate =>
                    BattlePlanner.Evaluate(candidate.State, candidate.Outcome))
                    .ThenBy(candidate => perDecisionCandidateLimit > 0 ? NeighborPositionCost(candidate.State) : 0).ToArray();
                var singles = perDecisionCandidateLimit == 0 ? Array.Empty<Step>() : ordered
                    .GroupBy(candidate => (candidate.Step.Action?.CardInstanceId ?? -1,
                        candidate.Step.Action?.ActivatorUnitId ?? 0))
                    .Select(group => group.First().Step).ToArray();
                // Neither input has to improve the route alone.
                // Every successor is model-owned and the final export is still
                // independently replayed from the exact native deployment.
                IEnumerable<(Step[] Steps, BattleTurnState[] States, RoomOutcome Outcome)> Neighbors()
                {
                    foreach (var first in ordered.Where(candidate => singles.Contains(candidate.Step)))
                    {
                        if (Expired() || DecisionBudgetReached()) yield break;
                        yield return (new[] { first.Step }, new[] { first.State }, first.Outcome);
                    }
                    foreach (var first in perDecisionCandidateLimit == 0 ? alternatives : ordered.AsEnumerable())
                    {
                        if (Expired() || DecisionBudgetReached()) yield break;
                        if (!singles.Contains(first.Step))
                            yield return (new[] { first.Step }, new[] { first.State }, first.Outcome);
                        if (secondActionWidth == 0 || Expired() || DecisionBudgetReached() || first.Step.IsEndTurn || Terminal(first.Outcome) || prefix.Length >= 255) continue;
                        var firstPrefix = prefix.Append(first.Step).ToArray();
                        string firstKey = keys.Key(first.State);
                        var next = BattleActionModel.EnumerateActions(first.State);
                        foreach (var attempt in next.UnsupportedAttempts)
                            Unsupported(attempt.Reason ?? "Unsupported second input.", attempt.Action, first.State, firstPrefix);
                        foreach (var second in next.SupportedCandidates.OrderBy(candidate =>
                            BattlePlanner.Evaluate(candidate.Result.State!, candidate.Result.Outcome))
                            .ThenBy(candidate => perDecisionCandidateLimit > 0 ? NeighborPositionCost(candidate.Result.State!) : 0).Take(secondActionWidth))
                        {
                            if (Expired() || DecisionBudgetReached()) break;
                            yield return (new[] { first.Step, new Step { Action = second.Action } },
                                new[] { first.State, second.Result.State! }, second.Result.Outcome);
                            if (thirdActionWidth == 0 || Expired() || DecisionBudgetReached() ||
                                Terminal(second.Result.Outcome) || prefix.Length >= 254) continue;
                            BattleTurnState secondState = second.Result.State!;
                            string secondKey = keys.Key(secondState);
                            var secondPrefix = firstPrefix.Append(new Step { Action = second.Action }).ToArray();
                            var thirds = BattleActionModel.EnumerateActions(secondState);
                            foreach (var attempt in thirds.UnsupportedAttempts)
                                Unsupported(attempt.Reason ?? "Unsupported third input.", attempt.Action, secondState, secondPrefix);
                            foreach (var third in thirds.SupportedCandidates.OrderBy(candidate =>
                                BattlePlanner.Evaluate(candidate.Result.State!, candidate.Result.Outcome))
                                .ThenBy(candidate => NeighborPositionCost(candidate.Result.State!)).Take(thirdActionWidth))
                            {
                                if (Expired() || DecisionBudgetReached()) break;
                                yield return (new[] { first.Step, new Step { Action = second.Action }, new Step { Action = third.Action } },
                                    new[] { first.State, secondState, third.Result.State! }, third.Result.Outcome);
                            }
                            if (!Expired() && !DecisionBudgetReached())
                            {
                                BattleTurnResult thirdEnd = BattleTurnModel.EndTurn(secondState);
                                if (thirdEnd.Supported) yield return (new[] { first.Step, new Step { Action = second.Action }, new Step { IsEndTurn = true } },
                                    new[] { first.State, secondState, thirdEnd.State! }, thirdEnd.Outcome);
                                else Unsupported(thirdEnd.UnsupportedReason ?? "Unsupported third EndTurn.", null, secondState, secondPrefix);
                            }
                            Require(keys.Key(secondState) == secondKey, "Third input mutated its parent.");
                        }
                        if (!Expired() && !DecisionBudgetReached())
                        {
                            BattleTurnResult ended = BattleTurnModel.EndTurn(first.State);
                            if (ended.Supported) yield return (new[] { first.Step, new Step { IsEndTurn = true } },
                                new[] { first.State, ended.State! }, ended.Outcome);
                            else Unsupported(ended.UnsupportedReason ?? "Unsupported second EndTurn.", null, first.State, firstPrefix);
                        }
                        Require(keys.Key(first.State) == firstKey, "Second input mutated its parent.");
                    }
                }
                foreach (var alternative in Neighbors())
                {
                    if (Expired() || DecisionBudgetReached()) break;
                    var steps = prefix.Concat(alternative.Steps).ToList();
                    if (!tried.Add(JsonSerializer.Serialize(steps))) continue;
                    candidates++; decisionCandidates++;
                    if (alternative.Steps.Length == 2) pairCandidates++;
                    if (alternative.Steps.Length == 3) tripleCandidates++;
                    BattleTurnState branch = alternative.States[^1];
                    var tailStates = alternative.States.ToList();
                    RoomOutcome result = alternative.Outcome;
                    int actions = steps.AsEnumerable().Reverse().TakeWhile(step => !step.IsEndTurn).Count();
                    int suffix = index + 1;
                    while (!Terminal(result) && steps.Count < 256 && !Expired())
                    {
                        // Preserve earlier useful refinements when their inputs
                        // remain legal in the same turn. A changed EndTurn skips
                        // the old turn's remainder; a deferred EndTurn completes
                        // the current turn before resuming later recorded inputs.
                        // Reapply every action, never splice recorded snapshots.
                        bool replayed = false;
                        while (retainSuffix && suffix < route.Length &&
                            prefixes[suffix].Spawn.Turn <= branch.Spawn.Turn && !Expired())
                        {
                            BattleTurnState originalEntry = prefixes[suffix];
                            int originalTurn = originalEntry.Spawn.Turn;
                            Step next = route[suffix++];
                            if (originalTurn < branch.Spawn.Turn || !next.IsEndTurn && actions >= 18)
                            { skippedSuffixInputs++; continue; }
                            if (next.IsEndTurn)
                            {
                                BattleTurnResult ended = BattleTurnModel.EndTurn(branch);
                                if (!ended.Supported)
                                { Unsupported(ended.UnsupportedReason ?? "Unsupported EndTurn.", null, branch, steps); skippedSuffixInputs++; continue; }
                                branch = ended.State!; result = ended.Outcome; actions = 0;
                            }
                            else
                            {
                                PlayCardAction? retained = RetainAction(originalEntry, branch, next.Action!);
                                if (retained == null) { skippedSuffixInputs++; continue; }
                                if (!ReferenceEquals(retained, next.Action))
                                {
                                    next = new Step { Action = retained };
                                    retargetedSuffixInputs++;
                                }
                                BattleActionResult played = BattleActionModel.PlayCard(branch, retained);
                                if (!played.Supported)
                                {
                                    // Earlier changes may consume a selected
                                    // recursion target or move its host. A stale
                                    // recorded input is skipped, not exported as
                                    // evidence of a missing legal model branch.
                                    skippedSuffixInputs++;
                                    continue;
                                }
                                branch = played.State!; result = played.Outcome; actions++;
                            }
                            steps.Add(next); tailStates.Add(branch); retainedSteps++; replayed = true; break;
                        }
                        if (replayed) continue;
                        int floor = branch.Spawn.Train.Rooms.FirstOrDefault(room => room.Units.Any(unit =>
                            unit.Team == CombatTeam.Player && unit.Health > 0 && unit.Modifiers?.IsChampion == true))?.RoomIndex ?? 2;
                        SupportedActionCandidate? chosen = null;
                        if (actions < 18)
                        {
                            chosen = BattleTacticalPolicy.Choose(branch, out var omitted, floor);
                            foreach (var attempt in omitted)
                                Unsupported(attempt.Reason ?? "Unsupported tactical continuation.", attempt.Action, branch, steps);
                        }
                        if (chosen != null)
                        {
                            branch = chosen.Result.State!; result = chosen.Result.Outcome;
                            steps.Add(new Step { Action = chosen.Action }); tailStates.Add(branch); actions++;
                        }
                        else
                        {
                            BattleTurnResult ended = BattleTurnModel.EndTurn(branch);
                            if (!ended.Supported)
                            { Unsupported(ended.UnsupportedReason ?? "Unsupported EndTurn.", null, branch, steps); break; }
                            branch = ended.State!; result = ended.Outcome;
                            steps.Add(new Step { IsEndTurn = true }); tailStates.Add(branch); actions = 0;
                        }
                    }
                    Require(keys.Key(entry) == parent && keys.Key(root) == immutable, "Improvement mutated its parent.");
                    if (!Terminal(result)) continue;
                    completed++;
                    double score = ImprovementMerit(branch, result);
                    bool better = preferLaterDefeat ? BetterSurvival(result, branch.Spawn.Turn, score,
                        bestOutcome, bestFinal.Spawn.Turn, best) : score < best;
                    if (diversificationWidth > 0 && !better)
                    {
                        Step[] candidateRoute = steps.ToArray();
                        string identity = JsonSerializer.Serialize(candidateRoute);
                        if (!exploredRoutes.Contains(identity) && !frontier.Any(item => item.Identity == identity))
                        {
                            frontier.Add(new PolicyAlternative(identity, score, candidateRoute,
                                prefixes.Take(index + 1).Concat(tailStates.Take(tailStates.Count - 1)).ToArray(), branch, result));
                            frontier = frontier.OrderByDescending(item => preferLaterDefeat && item.Outcome == RoomOutcome.BattleWon)
                                .ThenByDescending(item => preferLaterDefeat ? item.Final.Spawn.Turn : 0)
                                .ThenBy(item => item.Merit).ThenBy(item => item.Route.Length)
                                .Take(diversificationWidth).ToList();
                        }
                    }
                    if (!better) continue;
                    best = score; final = branch; outcome = result; route = steps.ToArray();
                    prefixes = prefixes.Take(index + 1).Concat(tailStates.Take(tailStates.Count - 1)).ToList();
                    Require(prefixes.Count == route.Length, "Improved prefix cache lost a model-owned successor.");
                    bestRoute = route; bestFinal = final; bestOutcome = outcome;
                    if (diversificationWidth > 0)
                    {
                        string identity = JsonSerializer.Serialize(route);
                        exploredRoutes.Add(identity);
                        frontier.RemoveAll(item => item.Identity == identity);
                    }
                    // With a retained suffix, an identical input prefix can
                    // now finish differently against the improved remainder.
                    // Dedupe only within one incumbent, not across changed tails.
                    if (retainSuffix) tried.Clear();
                    improvements++; improved = true;
                    if (alternative.Steps.Length == 2) pairImprovements++;
                    if (alternative.Steps.Length == 3) tripleImprovements++;
                    Console.WriteLine($"IMPROVE-POLICY pass={pass} decision={index} outcome={outcome} merit={best:F3} " +
                        $"inputs={route.Length} changedInputs={alternative.Steps.Length} candidates={candidates} elapsedMs={watch.Elapsed.TotalMilliseconds:F1}");
                    break;
                }
                if (retainSuffix) Console.WriteLine($"IMPROVE-PROGRESS decision={index} candidates={candidates} completed={completed} retainedSteps={retainedSteps} skippedSuffixInputs={skippedSuffixInputs} elapsedMs={watch.Elapsed.TotalMilliseconds:F1}");
                if (improved) break;
            }
            // A bounded sweep may leave untried neighbors at every decision.
            // Later passes continue past the already-tried inputs, even when
            // the preceding pass found no strictly better complete route.
            if (!improved && diversificationWidth > 0 && pass + 1 < maxPasses && !Expired() && frontier.Count > 0)
            {
                PolicyAlternative alternative = frontier[0]; frontier.RemoveAt(0);
                Require(alternative.Prefixes.Length == alternative.Route.Length,
                    "Diversified route lost a model-owned prefix.");
                route = alternative.Route; prefixes = alternative.Prefixes.ToList();
                final = alternative.Final; outcome = alternative.Outcome;
                exploredRoutes.Add(alternative.Identity); tried.Clear(); diversifiedRoutes++;
                Console.WriteLine($"IMPROVE-DIVERSIFY pass={pass} merit={alternative.Merit:F3} best={best:F3} " +
                    $"inputs={route.Length} frontier={frontier.Count} elapsedMs={watch.Elapsed.TotalMilliseconds:F1}");
            }
            if (!improved && perDecisionCandidateLimit == 0) break;
        }
        // A complete losing improvement is useful for another investigation,
        // but is explicitly exported with PlayerDefeated, never called a win.
        // Independent replay and freezing do not consume the search budget.
        watch.Stop();
        if (improvements > 0)
        {
            BattleTurnState independent = Replay(bestRoute, out RoomOutcome replayOutcome);
            Require(replayOutcome == bestOutcome && keys.Key(independent) == keys.Key(bestFinal) && keys.Key(root) == immutable,
                "Independent improvement replay changed its complete state/RNG.");
            Freeze(input, JsonSerializer.Serialize(bestRoute), output);
        }
        Console.WriteLine($"IMPROVE-POLICY foundWin={bestOutcome == RoomOutcome.BattleWon} improvements={improvements} " +
            $"startDecision={startDecision} exploreCardOrder={exploreCardOrder} candidates={candidates} completed={completed} unsupported={unsupported} " +
            $"retainSuffix={retainSuffix} improveWinningRoute={improveWinningRoute} preferLaterDefeat={preferLaterDefeat} bestTurn={bestFinal.Spawn.Turn} secondActionWidth={secondActionWidth} thirdActionWidth={thirdActionWidth} perDecisionCandidateLimit={perDecisionCandidateLimit} diverseFirstCards={perDecisionCandidateLimit > 0} diversificationWidth={diversificationWidth} diversifiedRoutes={diversifiedRoutes} maxDecisionCount={maxDecisionCount} visitedDecisions={visitedDecisions} lastDecision={lastDecision} pairCandidates={pairCandidates} pairImprovements={pairImprovements} tripleCandidates={tripleCandidates} tripleImprovements={tripleImprovements} retainedSteps={retainedSteps} skippedSuffixInputs={skippedSuffixInputs} retargetedSuffixInputs={retargetedSuffixInputs} timedOut={Expired()} elapsedMs={watch.Elapsed.TotalMilliseconds:F1} exported={improvements > 0}; model prediction only.");

        BattleTurnState Replay(IEnumerable<Step> steps, out RoomOutcome terminal, List<BattleTurnState>? collect = null)
        {
            BattleTurnState state = root; terminal = RoomOutcome.Exchanged;
            foreach (Step step in steps)
            {
                Require(!Terminal(terminal) && step.IsEndTurn == (step.Action == null), "Invalid improvement input sequence.");
                collect?.Add(state);
                state = Apply(state, step.Action, step.IsEndTurn, out terminal);
            }
            Require(Terminal(terminal), "Incomplete improvement route.");
            return state;
        }
    }

    private sealed record PolicyAlternative(string Identity, double Merit, Step[] Route,
        BattleTurnState[] Prefixes, BattleTurnState Final, RoomOutcome Outcome);

    internal static bool BetterSurvival(RoomOutcome candidate, int candidateTurn, double candidateMerit,
        RoomOutcome incumbent, int incumbentTurn, double incumbentMerit)
    {
        if (candidate == RoomOutcome.BattleWon && incumbent != RoomOutcome.BattleWon) return true;
        if (candidate != RoomOutcome.BattleWon && incumbent == RoomOutcome.BattleWon) return false;
        if (candidate != RoomOutcome.BattleWon && candidateTurn != incumbentTurn) return candidateTurn > incumbentTurn;
        return candidateMerit < incumbentMerit;
    }

    // Earlier deployment changes can assign the old numeric actor ID to a
    // different card. Preserve a selected card's identity, including its exact
    // instance, before reusing an input from the incumbent suffix. Cardless
    // births and enemy waves have no unique card identity: do not guess a new
    // actor when their original ID no longer denotes the same kind of unit.
    internal static PlayCardAction? RetainAction(BattleTurnState original, BattleTurnState branch, PlayCardAction action)
    {
        var originalActors = original.Spawn.Train.Rooms.SelectMany(room => room.Units).ToArray();
        var actors = branch.Spawn.Train.Rooms.SelectMany(room => room.Units.Select(unit => (Room: room.RoomIndex, Unit: unit)))
            .Where(actor => actor.Unit.Health > 0 && actor.Unit.DeathState?.IsDestroyed != true).ToArray();
        (int Room, CombatUnit Unit)? Resolve(int id)
        {
            CombatUnit? selected = originalActors.SingleOrDefault(unit => unit.Id == id);
            if (selected == null) return null;
            bool SameCard(CombatUnit unit) => unit.AssetKey == selected.AssetKey && unit.Team == selected.Team &&
                unit.SpawnerCardId == selected.SpawnerCardId;
            var exact = actors.Where(actor => actor.Unit.Id == id && SameCard(actor.Unit)).ToArray();
            if (exact.Length == 1) return exact[0];
            if (selected.SpawnerCardId <= 0) return null;
            var matches = actors.Where(actor => SameCard(actor.Unit)).ToArray();
            return matches.Length == 1 ? matches[0] : null;
        }
        var target = action.TargetUnitId == 0 ? null : Resolve(action.TargetUnitId);
        var activator = action.ActivatorUnitId == 0 ? null : Resolve(action.ActivatorUnitId);
        if (action.TargetUnitId != 0 && target == null || action.ActivatorUnitId != 0 && activator == null) return null;
        int room = target?.Room ?? action.RoomIndex;
        int targetId = target?.Unit.Id ?? 0, activatorId = activator?.Unit.Id ?? 0;
        int? activatorRoom = action.ActivatorRoomIndex.HasValue ? activator?.Room ?? action.ActivatorRoomIndex : null;
        if (room == action.RoomIndex && targetId == action.TargetUnitId && activatorId == action.ActivatorUnitId &&
            activatorRoom == action.ActivatorRoomIndex) return action;
        return new PlayCardAction(action.CardInstanceId, room, action.PlayerPosition, targetId, activatorId,
            action.TargetSpawnPointIndex, action.RecursionSelections, action.MixSelections, activatorRoom);
    }

    // Only breaks equal capped public scores. Keep win/Pyre ordering above
    // this local material estimate, and preserve unlimited legacy enumeration.
    private static double NeighborPositionCost(BattleTurnState state) =>
        state.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => unit.Health > 0 && !unit.IsPyre)
            .Sum(unit => unit.Team == CombatTeam.Enemy ? unit.Health + (double)(unit.Statuses.FirstOrDefault(status => status.Id == "armor")?.Stacks ?? 0) :
                -(Math.Max(0d, unit.Attack) * unit.Attacks + unit.Health * .1 + Math.Max(0, unit.Statuses.FirstOrDefault(status => status.Id == "armor")?.Stacks ?? 0) * .1));

    internal static double ImprovementMerit(BattleTurnState state, RoomOutcome result)
    {
        if (result == RoomOutcome.BattleWon) return BattlePlanner.Evaluate(state, result);
        var enemies = state.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Where(unit => unit.Team == CombatTeam.Enemy && unit.Health > 0).ToArray();
        double bosses = enemies.Where(unit => unit.IsBoss == true)
            .Sum(unit => unit.Health / (double)Math.Max(1, unit.MaxHealth));
        // Dying before a boss spawns does not count as defeating that boss.
        // Unresolved wave groups conservatively retain their largest boss count.
        bosses += state.Spawn.Waves.Skip(state.Spawn.Phase).Sum(wave => wave.Candidates
            .Select(group => group.Units.Count(unit => unit.Unit.IsBoss == true)).DefaultIfEmpty(0).Max());
        if (state.Spawn.QueuedOuterBoss != null || state.Spawn.QueuedTrueFinalBoss != null) bosses++;
        // Diagnostic ordering among complete losses only. The production
        // win/Pyre objective stays unchanged; this is no lower bound or proof.
        return 1e12 + bosses * 100000d + enemies.Sum(unit => (double)unit.Health) * .001 - state.Spawn.Turn;
    }
}

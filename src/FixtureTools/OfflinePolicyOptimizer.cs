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
    // original loss. A result is still a model prediction requiring native replay.
    internal static void Improve(string input, string output, int milliseconds, int maxPasses = 4,
        string? prediction = null, int startDecision = 0, bool exploreCardOrder = false, bool retainSuffix = false)
    {
        Require(milliseconds is >= 100 and <= 600000 && maxPasses is >= 1 and <= 16,
            "Expected100..600000ms and1..16 improvement passes.");
        if (File.Exists(output)) throw new IOException("Output already exists: " + output);
        string unsupportedOutput = output + ".unsupported.mt2f";
        if (retainSuffix && File.Exists(unsupportedOutput)) throw new IOException("Output already exists: " + unsupportedOutput);
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
        BattleTurnState final = Replay(route, out RoomOutcome outcome);
        double best = ImprovementMerit(final, outcome), initial = best;
        var watch = Stopwatch.StartNew();
        var tried = new HashSet<string>(StringComparer.Ordinal);
        int candidates = 0, completed = 0, unsupported = 0, improvements = 0;
        int retainedSteps = 0, skippedSuffixInputs = 0;
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
        for (int pass = 0; pass < maxPasses && outcome != RoomOutcome.BattleWon && !Expired(); pass++)
        {
            bool improved = false;
            BattleTurnState current = root;
            var prefix = new List<Step>();
            // Rebuild prefixes after each accepted improvement. Never splice
            // captures with native preview allocations into model-owned states.
            var prefixes = new List<(BattleTurnState State, Step[] Steps)>();
            foreach (Step step in route)
            {
                prefixes.Add((current, prefix.ToArray()));
                current = Apply(current, step.Action, step.IsEndTurn, out _); prefix.Add(step);
            }
            for (int index = startDecision; index < prefixes.Count && !Expired(); index++)
            {
                var entry = prefixes[index];
                string parent = keys.Key(entry.State);
                Step recorded = route[index];
                var legal = BattleActionModel.EnumerateActions(entry.State, exploreCardOrder ? null : recorded.Action?.CardInstanceId,
                    includeAbilities: exploreCardOrder || recorded.Action == null || recorded.Action.ActivatorUnitId != 0);
                foreach (var attempt in legal.UnsupportedAttempts)
                    Unsupported(attempt.Reason ?? "Unsupported alternative.", attempt.Action, entry.State, entry.Steps);
                string recordedAction = JsonSerializer.Serialize(recorded.Action);
                var alternatives = legal.SupportedCandidates.Where(candidate =>
                    JsonSerializer.Serialize(candidate.Action) != recordedAction &&
                    (exploreCardOrder || recorded.Action == null || candidate.Action.ActivatorUnitId == recorded.Action.ActivatorUnitId))
                    .Select(candidate => (Step: new Step { Action = candidate.Action },
                        State: candidate.Result.State!, Outcome: candidate.Result.Outcome)).ToList();
                if (!recorded.IsEndTurn)
                {
                    BattleTurnResult ended = BattleTurnModel.EndTurn(entry.State);
                    if (ended.Supported) alternatives.Add((new Step { IsEndTurn = true }, ended.State!, ended.Outcome));
                    else Unsupported(ended.UnsupportedReason ?? "Unsupported EndTurn.", null, entry.State, entry.Steps);
                }
                foreach (var alternative in alternatives)
                {
                    if (Expired()) break;
                    var steps = new List<Step>(entry.Steps) { alternative.Step };
                    if (!tried.Add(JsonSerializer.Serialize(steps))) continue;
                    candidates++;
                    BattleTurnState branch = alternative.State;
                    RoomOutcome result = alternative.Outcome;
                    int actions = alternative.Step.IsEndTurn ? 0 : entry.Steps.Reverse().TakeWhile(step => !step.IsEndTurn).Count() + 1;
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
                            prefixes[suffix].State.Spawn.Turn <= branch.Spawn.Turn && !Expired())
                        {
                            int originalTurn = prefixes[suffix].State.Spawn.Turn;
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
                                BattleActionResult played = BattleActionModel.PlayCard(branch, next.Action!);
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
                            steps.Add(next); retainedSteps++; replayed = true; break;
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
                            steps.Add(new Step { Action = chosen.Action }); actions++;
                        }
                        else
                        {
                            BattleTurnResult ended = BattleTurnModel.EndTurn(branch);
                            if (!ended.Supported)
                            { Unsupported(ended.UnsupportedReason ?? "Unsupported EndTurn.", null, branch, steps); break; }
                            branch = ended.State!; result = ended.Outcome;
                            steps.Add(new Step { IsEndTurn = true }); actions = 0;
                        }
                    }
                    Require(keys.Key(entry.State) == parent && keys.Key(root) == immutable, "Improvement mutated its parent.");
                    if (!Terminal(result)) continue;
                    completed++;
                    double score = ImprovementMerit(branch, result);
                    if (score >= best) continue;
                    best = score; final = branch; outcome = result; route = steps.ToArray();
                    improvements++; improved = true;
                    Console.WriteLine($"IMPROVE-POLICY pass={pass} decision={index} outcome={outcome} merit={best:F3} " +
                        $"inputs={route.Length} candidates={candidates} elapsedMs={watch.Elapsed.TotalMilliseconds:F1}");
                    break;
                }
                if (retainSuffix) Console.WriteLine($"IMPROVE-PROGRESS decision={index} candidates={candidates} completed={completed} retainedSteps={retainedSteps} skippedSuffixInputs={skippedSuffixInputs} elapsedMs={watch.Elapsed.TotalMilliseconds:F1}");
                if (improved) break;
            }
            if (!improved) break;
        }
        // A complete losing improvement is useful for another investigation,
        // but is explicitly exported with PlayerDefeated, never called a win.
        if (best < initial)
        {
            BattleTurnState independent = Replay(route, out RoomOutcome replayOutcome);
            Require(replayOutcome == outcome && keys.Key(independent) == keys.Key(final) && keys.Key(root) == immutable,
                "Independent improvement replay changed its complete state/RNG.");
            Freeze(input, JsonSerializer.Serialize(route), output);
        }
        Console.WriteLine($"IMPROVE-POLICY foundWin={outcome == RoomOutcome.BattleWon} improvements={improvements} " +
            $"startDecision={startDecision} exploreCardOrder={exploreCardOrder} candidates={candidates} completed={completed} unsupported={unsupported} " +
            $"retainSuffix={retainSuffix} retainedSteps={retainedSteps} skippedSuffixInputs={skippedSuffixInputs} timedOut={Expired()} elapsedMs={watch.Elapsed.TotalMilliseconds:F1} exported={best < initial}; model prediction only.");

        BattleTurnState Replay(IEnumerable<Step> steps, out RoomOutcome terminal)
        {
            BattleTurnState state = root; terminal = RoomOutcome.Exchanged;
            foreach (Step step in steps)
            {
                Require(!Terminal(terminal) && step.IsEndTurn == (step.Action == null), "Invalid improvement input sequence.");
                state = Apply(state, step.Action, step.IsEndTurn, out terminal);
            }
            Require(Terminal(terminal), "Incomplete improvement route.");
            return state;
        }
    }

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

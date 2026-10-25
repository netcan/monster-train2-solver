using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static partial class OfflineBattleSearch
{
    // Explore a recorded counterexample without launching a game for every
    // placement. These are local policy alternatives, never an optimality proof.
    internal static void Refine(string input, string output, int milliseconds, int startDecision, int? lastRequestedDecision)
    {
        Require(milliseconds is >= 100 and <= 600000, "Expected 100..600000ms.");
        if (File.Exists(output)) throw new IOException("Output already exists: " + output);
        string unsupportedOutput = output + ".unsupported.mt2f";
        if (File.Exists(unsupportedOutput)) throw new IOException("Output already exists: " + unsupportedOutput);
        using var document = FixtureDocument.Read(input);
        FixtureValue trace = document.RootElement;
        RequireNativeInput(trace);
        FixtureValue[] rows = trace.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(rows.Length is > 0 and <= 256 && startDecision >= 0 && startDecision < rows.Length,
            "Expected a recorded battle and a valid starting decision.");
        int lastRequested = lastRequestedDecision ?? rows.Length - 1;
        Require(lastRequested >= startDecision && lastRequested < rows.Length, "Invalid inclusive last decision.");
        BattleTurnState root = DeploymentRoot(trace);
        Require(root.Spawn.Turn == 0, "Policy refinement requires the original deployment root.");
        using var keys = new BattleStateKeys();
        string immutable = keys.Key(root);
        var prefixes = new List<(BattleTurnState State, Step[] Steps, string Key)>();
        var prefix = new List<Step>();
        BattleTurnState current = root;
        RoomOutcome outcome = RoomOutcome.Exchanged;
        // Reapply native inputs to model-owned prefixes. Native UI captures
        // can advance unused preview allocators between inputs, so those raw
        // snapshots are not interchangeable with immutable model states.
        // Full native comparison remains the separate source-only checker.
        foreach (FixtureValue row in rows)
        {
            Require(!Terminal(outcome), "An input follows a terminal outcome.");
            prefixes.Add((current, prefix.ToArray(), keys.Key(current)));
            var step = new Step { Action = row.GetProperty("Action").Deserialize<PlayCardAction>(),
                IsEndTurn = row.GetProperty("IsEndTurn").GetBoolean() };
            Require(step.IsEndTurn == (step.Action == null), "Invalid recorded input.");
            current = Apply(current, step.Action, step.IsEndTurn, out outcome);
            BattleTurnState actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            Require(outcome == (RoomOutcome)row.GetProperty("Outcome").GetInt32() &&
                keys.SnapshotKey(current.RngStreams) == keys.SnapshotKey(actual.RngStreams) &&
                keys.SnapshotKey(current.Spawn.Rng) == keys.SnapshotKey(actual.Spawn.Rng),
                "Recorded outcome or RNG differs from the current model.");
            prefix.Add(step);
        }
        Require(Terminal(outcome) && keys.Key(root) == immutable, "Incomplete or mutable native baseline.");
        var watch = Stopwatch.StartNew();
        int candidates = 0, completed = 0, unsupported = 0, lastDecision = startDecision;
        var reasons = new HashSet<string>(StringComparer.Ordinal);
        void Unsupported(string reason, int index, PlayCardAction? action, BattleTurnState failedRoot, IReadOnlyList<Step> failedPrefix)
        {
            unsupported++;
            if (reasons.Add(reason)) Console.WriteLine($"REFINE-UNSUPPORTED decision={index} action={JsonSerializer.Serialize(action)} reason={reason}");
            if (File.Exists(unsupportedOutput)) return;
            BattleTurnState replay = root;
            RoomOutcome replayOutcome = RoomOutcome.Exchanged;
            foreach (Step step in failedPrefix)
            {
                Require(!Terminal(replayOutcome), "Failure prefix follows a terminal input.");
                replay = Apply(replay, step.Action, step.IsEndTurn, out replayOutcome);
            }
            using var independent = new BattleStateKeys();
            Require(independent.Key(replay) == independent.Key(failedRoot) && independent.Key(root) == immutable,
                "Failure prefix changed its root or did not reach the rejected branch.");
            string? reproduced = action == null ? BattleTurnModel.EndTurn(replay).UnsupportedReason :
                BattleActionModel.PlayCard(replay, action).Reason;
            Require(reproduced == reason, "Extracted failure does not reproduce its exact rejection.");
            using var failure = NativeFixtureCapture.Capture(new { Schema = 1, EvidenceKind = "ModelUnsupportedBranch",
                SourceArchive = Path.GetFileName(input),
                SourceSHA256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(input))).ToLowerInvariant(),
                Root = root, Prefix = failedPrefix, FailedRoot = failedRoot, Action = action, IsEndTurn = action == null,
                Reason = reason, NativeValidationRequired = true });
            using var stream = new FileStream(unsupportedOutput, FileMode.CreateNew, FileAccess.Write);
            failure.Write(stream);
            Console.WriteLine($"REFINE-BRANCH prefix={failedPrefix.Count} output={unsupportedOutput}; native observation required.");
        }
        bool stopped = false;
        for (int index = startDecision; index <= lastRequested && !stopped; index++)
        {
            lastDecision = index;
            var entry = prefixes[index];
            Step recorded = prefix[index];
            string recordedKey = JsonSerializer.Serialize(recorded.Action);
            // Change the chosen card's target/position, or defer it to the next
            // turn. At an EndTurn boundary, try any remaining legal input.
            BattleActionEnumeration legal = BattleActionModel.EnumerateActions(entry.State,
                recorded.Action?.CardInstanceId, includeAbilities: recorded.Action == null || recorded.Action.ActivatorUnitId != 0);
            foreach (UnsupportedActionAttempt attempt in legal.UnsupportedAttempts)
                Unsupported(attempt.Reason ?? "Unsupported alternative.", index, attempt.Action, entry.State, entry.Steps);
            var firstSteps = legal.SupportedCandidates
                .Where(candidate => JsonSerializer.Serialize(candidate.Action) != recordedKey &&
                    (recorded.Action == null || candidate.Action.ActivatorUnitId == recorded.Action.ActivatorUnitId))
                .Select(candidate => new Step { Action = candidate.Action }).ToList();
            if (!recorded.IsEndTurn) firstSteps.Insert(0, new Step { IsEndTurn = true });
            foreach (Step first in firstSteps)
            {
                if (watch.ElapsedMilliseconds >= milliseconds) { stopped = true; break; }
                candidates++;
                var steps = new List<Step>(entry.Steps) { first };
                BattleTurnState branch;
                if (first.IsEndTurn)
                {
                    BattleTurnResult ended = BattleTurnModel.EndTurn(entry.State);
                    if (!ended.Supported) { Unsupported(ended.UnsupportedReason ?? "Unsupported EndTurn.", index, null, entry.State, entry.Steps); continue; }
                    branch = ended.State!; outcome = ended.Outcome;
                }
                else
                {
                    BattleActionResult played = BattleActionModel.PlayCard(entry.State, first.Action!);
                    Require(played.Supported, "Enumerated action became unsupported.");
                    branch = played.State!; outcome = played.Outcome;
                }
                int actions = first.IsEndTurn ? 0 : entry.Steps.Reverse().TakeWhile(step => !step.IsEndTurn).Count() + 1;
                while (!Terminal(outcome) && steps.Count < 256 && watch.ElapsedMilliseconds < milliseconds)
                {
                    int room = branch.Spawn.Train.Rooms.FirstOrDefault(item => item.Units.Any(unit =>
                        unit.Team == CombatTeam.Player && !unit.IsPyre && unit.Health > 0 && unit.Modifiers?.IsChampion == true))?.RoomIndex ?? 2;
                    SupportedActionCandidate? choice = null;
                    if (actions < 18)
                    {
                        choice = BattleTacticalPolicy.Choose(branch, out IReadOnlyList<UnsupportedActionAttempt> omitted, room);
                        foreach (UnsupportedActionAttempt attempt in omitted)
                            Unsupported(attempt.Reason ?? "Unsupported tactical continuation.", index, attempt.Action, branch, steps);
                    }
                    if (choice != null)
                    {
                        branch = choice.Result.State!; outcome = choice.Result.Outcome;
                        steps.Add(new Step { Action = choice.Action }); actions++;
                    }
                    else
                    {
                        BattleTurnResult ended = BattleTurnModel.EndTurn(branch);
                        if (!ended.Supported) { Unsupported(ended.UnsupportedReason ?? "Unsupported EndTurn.", index, null, branch, steps); break; }
                        branch = ended.State!; outcome = ended.Outcome;
                        steps.Add(new Step { IsEndTurn = true }); actions = 0;
                    }
                }
                using (var integrity = new BattleStateKeys())
                    Require(integrity.Key(root) == immutable && integrity.Key(entry.State) == entry.Key,
                        "Refinement mutated a parent.");
                if (Terminal(outcome)) completed++;
                if (outcome != RoomOutcome.BattleWon) continue;
                double searchMs = watch.Elapsed.TotalMilliseconds;
                Freeze(input, JsonSerializer.Serialize(steps), output);
                Console.WriteLine($"REFINE-POLICY found=true changedDecision={index} candidates={candidates} completed={completed} " +
                    $"unsupported={unsupported} searchMs={searchMs:F1} exportVerifyMs={watch.Elapsed.TotalMilliseconds - searchMs:F1}; " +
                    "model prediction, native validation required.");
                return;
            }
            Console.WriteLine($"REFINE-PROGRESS decision={index} candidates={candidates} completed={completed} elapsedMs={watch.Elapsed.TotalMilliseconds:F1}");
        }
        Console.WriteLine($"REFINE-POLICY found=false lastDecision={lastDecision} candidates={candidates} completed={completed} " +
            $"unsupported={unsupported} timedOut={stopped || watch.ElapsedMilliseconds >= milliseconds} " +
            $"elapsedMs={watch.Elapsed.TotalMilliseconds:F1}; no win in these local alternatives, not proof of impossibility.");
    }
}

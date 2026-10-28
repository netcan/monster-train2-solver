using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static partial class OfflineBattleSearch
{
    // Explore several simultaneous placement changes without requiring each
    // single move to improve a complete route. Never export a truncated rollout.
    internal static void DeploymentGrid(string input, string prediction, string output, int milliseconds, int maxCandidates,
        int? firstRoom = null, bool spread = false, int startSample = 0, bool improveWinningRoute = false,
        string? unsupportedReasonFilter = null, bool spreadOrder = false, bool strengthRollout = false)
    {
        Require(milliseconds is >= 100 and <= 600000 && maxCandidates is >= 1 and <= 4096,
            "Expected100..600000ms and1..4096 deployment candidates.");
        Require(firstRoom == null || firstRoom is >= 0 and <= 2, "Expected a first-unit room in0..2.");
        Require(startSample is >= 0 and <= 1000000 && (spread || startSample == 0), "Expected a spread sample offset in0..1000000.");
        Require(!spreadOrder || spread, "Deployment order sampling requires spread sampling.");
        Require(unsupportedReasonFilter == null || !string.IsNullOrWhiteSpace(unsupportedReasonFilter), "Expected a nonempty unsupported reason filter.");
        if (File.Exists(output)) throw new IOException("Output already exists: " + output);
        string unsupportedOutput = output + ".unsupported.mt2f";
        if (File.Exists(unsupportedOutput)) throw new IOException("Output already exists: " + unsupportedOutput);
        using var source = FixtureDocument.Read(input);
        RequireNativeInput(source.RootElement);
        Verify(prediction);
        using var incumbent = FixtureDocument.Read(prediction);
        var fixture = incumbent.RootElement;
        Require(fixture.GetProperty("EvidenceKind").GetString() == "ModelPrediction" &&
            fixture.GetProperty("SourceSHA256").GetString() == Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(input))).ToLowerInvariant(),
            "Deployment baseline must bind the exact native archive.");
        var root = DeploymentRoot(source.RootElement);
        using var keys = new BattleStateKeys();
        string immutable = keys.Key(root);
        Require(root.Spawn.Turn == 0 && keys.Key(fixture.GetProperty("Root").Deserialize<BattleTurnState>()!) == immutable,
            "Deployment baseline changed the original native root.");
        var route = fixture.GetProperty("RollingDecisions").EnumerateArray().Select(row => new Step {
            Action = row.GetProperty("Action").Deserialize<PlayCardAction>(),
            IsEndTurn = row.GetProperty("IsEndTurn").GetBoolean() }).ToArray();
        var opening = route.TakeWhile(step => !step.IsEndTurn).ToArray();
        bool UnitPlay(Step step) => step.Action is { ActivatorUnitId: 0 } action &&
            root.PlayRules!.Cards.FirstOrDefault(rule => rule.DataId == root.Spawn.Train.Context!.CardInstances!
                .FirstOrDefault(card => card.InstanceId == action.CardInstanceId)?.DataId)?.SpawnUnit != null;
        var deployment = opening.TakeWhile(UnitPlay).ToArray();
        var closing = opening.Skip(deployment.Length).ToArray();
        Require(deployment.Length is >= 1 and <= 32 && closing.Length <= 32 && closing.All(step =>
            step.Action is { ActivatorUnitId: 0 } && !UnitPlay(step)),
            "Expected one to32 initial unit plays, optionally followed by up to32 fixed ordinary card inputs.");
        var bestFinal = fixture.GetProperty("Final").Deserialize<BattleTurnState>()!;
        var bestOutcome = (RoomOutcome)fixture.GetProperty("Outcome").GetInt32();
        Require(bestOutcome == (improveWinningRoute ? RoomOutcome.BattleWon : RoomOutcome.PlayerDefeated),
            improveWinningRoute ? "Winning deployment investigation requires a complete winning baseline." :
                "Deployment investigation requires a complete losing baseline.");
        double best = ImprovementMerit(bestFinal, bestOutcome), initial = best;
        var bestRoute = route;
        var watch = Stopwatch.StartNew();
        int candidates = 0, completed = 0, unsupported = 0, duplicates = 0, improvements = 0, samples = 0, nextSample = startSample;
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var reasons = new HashSet<string>(StringComparer.Ordinal);
        bool Expired() => watch.ElapsedMilliseconds >= milliseconds;
        bool Stop() => Expired() || candidates >= maxCandidates || !improveWinningRoute && bestOutcome == RoomOutcome.BattleWon;
        void Rejected(string? reason, PlayCardAction? action, BattleTurnState failedRoot, IReadOnlyList<Step> failedPrefix)
        {
            unsupported++;
            if (reasons.Add(reason ?? "Unknown")) Console.WriteLine("DEPLOYMENT-UNSUPPORTED " + reason);
            if (File.Exists(unsupportedOutput) || unsupportedReasonFilter != null &&
                !(reason?.Contains(unsupportedReasonFilter, StringComparison.OrdinalIgnoreCase) ?? false)) return;
            string? reproduced = action == null ? BattleTurnModel.EndTurn(failedRoot).UnsupportedReason :
                BattleActionModel.PlayCard(failedRoot, action).Reason;
            // Enumerators can report a whole-state guard without a specific
            // action. Keep that diagnostic, but only export an exact boundary.
            if (reproduced != reason) return;
            var replay = root;
            RoomOutcome replayOutcome = RoomOutcome.Exchanged;
            foreach (var step in failedPrefix)
            {
                Require(!Terminal(replayOutcome), "Deployment failure prefix follows a terminal outcome.");
                replay = Apply(replay, step.Action, step.IsEndTurn, out replayOutcome);
            }
            Require(keys.Key(replay) == keys.Key(failedRoot) && keys.Key(root) == immutable,
                "Deployment failure prefix changed the native root or failed to reach its boundary.");
            using var failure = MonsterTrain2Poju.Capture.NativeFixtureCapture.Capture(new { Schema = 1,
                EvidenceKind = "ModelUnsupportedBranch", SourceArchive = Path.GetFileName(input),
                SourceSHA256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(input))).ToLowerInvariant(),
                Root = root, Prefix = failedPrefix, FailedRoot = failedRoot, Action = action, IsEndTurn = action == null,
                Reason = reason, NativeValidationRequired = true });
            using var stream = new FileStream(unsupportedOutput, FileMode.CreateNew, FileAccess.Write);
            failure.Write(stream);
            Console.WriteLine($"DEPLOYMENT-BRANCH prefix={failedPrefix.Count} turn={failedRoot.Spawn.Turn} output={unsupportedOutput}; native observation required.");
        }
        IEnumerable<(BattleTurnState State, Step[] Prefix)> Deployments(BattleTurnState state, int index, List<Step> prefix)
        {
            if (Stop()) yield break;
            if (index < deployment.Length)
            {
                var choices = BattleActionModel.EnumerateActions(state, deployment[index].Action!.CardInstanceId, includeAbilities: false);
                foreach (var rejected in choices.UnsupportedAttempts) Rejected(rejected.Reason, rejected.Action, state, prefix);
                string parent = keys.Key(state);
                foreach (var choice in choices.SupportedCandidates.OrderByDescending(choice => choice.Action.RoomIndex)
                    .ThenBy(choice => choice.Action.PlayerPosition).Where(choice => index != 0 || firstRoom == null || choice.Action.RoomIndex == firstRoom))
                {
                    if (Stop()) break;
                    if (choice.Result.Outcome != RoomOutcome.Exchanged) continue;
                    prefix.Add(new Step { Action = choice.Action });
                    foreach (var leaf in Deployments(choice.Result.State!, index + 1, prefix)) yield return leaf;
                    prefix.RemoveAt(prefix.Count - 1);
                    Require(keys.Key(state) == parent, "Deployment child changed its parent.");
                }
                yield break;
            }
            yield return (state, prefix.ToArray());
        }
        bool Visit(BattleTurnState state, Step[] placement)
        {
            if (!visited.Add(keys.Key(state))) { duplicates++; return true; }
            candidates++;
            var steps = placement.ToList();
            var current = state;
            var outcome = RoomOutcome.Exchanged;
            // Keep the original spell sequence after the joint placements.
            // Each spell is reapplied to this exact branch; invalid targets or
            // costs discard the candidate, and unknown mechanics retain their
            // reproducible native boundary. Never substitute another target.
            foreach (var step in closing)
            {
                var result = BattleActionModel.PlayCard(current, step.Action!);
                if (!result.Supported)
                {
                    if (result.Rejection == ActionRejection.Unsupported) Rejected(result.Reason, step.Action, current, steps);
                    return true;
                }
                current = result.State!; outcome = result.Outcome;
                steps.Add(step);
                if (Terminal(outcome)) break;
            }
            int played = steps.Count;
            int floor = current.Spawn.Train.Rooms.FirstOrDefault(room => room.Units.Any(unit =>
                unit.Team == CombatTeam.Player && unit.Health > 0 && unit.Modifiers?.IsChampion == true))?.RoomIndex ?? 2;
            while (!Terminal(outcome) && steps.Count < 256 && !Expired())
            {
                SupportedActionCandidate? choice = null;
                if (played < 18)
                {
                    IReadOnlyList<UnsupportedActionAttempt> omitted;
                    choice = strengthRollout ? BattleHeuristicPolicy.Choose(current, out omitted, floor) :
                        BattleTacticalPolicy.Choose(current, out omitted, floor);
                    foreach (var rejected in omitted) Rejected(rejected.Reason, rejected.Action, current, steps);
                }
                if (choice != null)
                {
                    current = choice.Result.State!; outcome = choice.Result.Outcome;
                    steps.Add(new Step { Action = choice.Action }); played++;
                }
                else
                {
                    var ended = BattleTurnModel.EndTurn(current);
                    if (!ended.Supported) { Rejected(ended.UnsupportedReason, null, current, steps); return true; }
                    current = ended.State!; outcome = ended.Outcome;
                    steps.Add(new Step { IsEndTurn = true }); played = 0;
                }
            }
            if (!Terminal(outcome)) return false;
            completed++;
            double score = ImprovementMerit(current, outcome);
            if (score >= best) return true;
            best = score; bestFinal = current; bestOutcome = outcome; bestRoute = steps.ToArray(); improvements++;
            Console.WriteLine($"DEPLOYMENT-IMPROVED candidate={candidates} outcome={outcome} steps={steps.Count} merit={best:R} " +
                "placements=" + JsonSerializer.Serialize(placement.Select(step => step.Action)));
            return true;
        }
        if (spread)
        {
            // A depth-first stream leaves the early units fixed for many
            // expensive rollouts. Walk legal choices with a deterministic
            // mixed-radix index instead.137 is coprime to every possible
            // room/position choice count, spreading early and late placements
            // across the budget without materializing a tree of battle states.
            for (int sample = startSample; sample < startSample + maxCandidates * 16 && !Stop(); sample++)
            {
                samples++;
                int code = sample * (spreadOrder ? 397 : 137);
                var sampleDeployment = deployment;
                if (spreadOrder)
                {
                    // Factoradic sampling visits every ordering of up to six
                    // distinct units before repeating; room choices use a
                    // separate deterministic stream.
                    int orderCode = sample * 137;
                    var remaining = deployment.ToList();
                    sampleDeployment = new Step[deployment.Length];
                    for (int i = 0; i < sampleDeployment.Length; i++)
                    {
                        int choice = orderCode % remaining.Count;
                        orderCode /= remaining.Count;
                        sampleDeployment[i] = remaining[choice];
                        remaining.RemoveAt(choice);
                    }
                }
                var state = root;
                var placement = new List<Step>();
                var parents = new List<(BattleTurnState State, string Key)>();
                bool covered = false;
                for (int index = 0; index < sampleDeployment.Length && !Stop(); index++)
                {
                    parents.Add((state, keys.Key(state)));
                    var legal = BattleActionModel.EnumerateActions(state, sampleDeployment[index].Action!.CardInstanceId, includeAbilities: false);
                    foreach (var rejected in legal.UnsupportedAttempts) Rejected(rejected.Reason, rejected.Action, state, placement);
                    var choices = legal.SupportedCandidates.Where(choice => choice.Result.Outcome == RoomOutcome.Exchanged &&
                            (index != 0 || firstRoom == null || choice.Action.RoomIndex == firstRoom))
                        .OrderByDescending(choice => choice.Action.RoomIndex).ThenBy(choice => choice.Action.PlayerPosition).ToArray();
                    if (choices.Length == 0) break;
                    var chosen = choices[code % choices.Length];
                    code /= choices.Length;
                    placement.Add(new Step { Action = chosen.Action });
                    state = chosen.Result.State!;
                }
                if (placement.Count == deployment.Length && !Stop())
                {
                    if (sample - startSample < 6) Console.WriteLine($"DEPLOYMENT-SPREAD sample={sample} placements=" +
                        JsonSerializer.Serialize(placement.Select(step => step.Action)));
                    covered = Visit(state, placement.ToArray());
                }
                else if (!Stop()) covered = true; // No legal complete placement for this sample.
                foreach (var parent in parents)
                    Require(keys.Key(parent.State) == parent.Key, "Spread deployment changed its parent.");
                // Retry a budget-truncated sample instead of silently skipping
                // its unfinished route when the next batch resumes.
                nextSample = covered ? sample + 1 : sample;
                if (!covered) break;
            }
        }
        else if (firstRoom.HasValue)
        {
            foreach (var leaf in Deployments(root, 0, new List<Step>()))
            {
                if (Stop()) break;
                Visit(leaf.State, leaf.Prefix);
            }
        }
        else
        {
            // A depth-first first floor can consume the whole budget. Keep one
            // lazy deployment stream per initial placement and visit them in
            // turn, so the middle and bottom cores receive the same opportunity.
            var openings = BattleActionModel.EnumerateActions(root, deployment[0].Action!.CardInstanceId, includeAbilities: false);
            foreach (var rejected in openings.UnsupportedAttempts) Rejected(rejected.Reason, rejected.Action, root, Array.Empty<Step>());
            var streams = openings.SupportedCandidates.Where(choice => choice.Result.Outcome == RoomOutcome.Exchanged)
                .OrderByDescending(choice => choice.Action.RoomIndex).ThenBy(choice => choice.Action.PlayerPosition)
                .Select(choice => Deployments(choice.Result.State!, 1,
                    new List<Step> { new Step { Action = choice.Action } }).GetEnumerator()).ToArray();
            try
            {
                bool advanced;
                do
                {
                    advanced = false;
                    foreach (var stream in streams)
                    {
                        if (Stop()) break;
                        if (!stream.MoveNext()) continue;
                        advanced = true;
                        Visit(stream.Current.State, stream.Current.Prefix);
                    }
                } while (advanced && !Stop());
            }
            finally { foreach (var stream in streams) stream.Dispose(); }
        }
        // Independent export validation has its own cost; it must not turn a
        // completed bounded investigation into a reported search timeout.
        watch.Stop();
        Require(keys.Key(root) == immutable, "Deployment grid changed its native parent.");
        if (best < initial)
        {
            // Freeze independently reapplies every input from deployment and
            // verifies the full terminal state, forecasts and RNG before export.
            Freeze(input, JsonSerializer.Serialize(bestRoute), output);
            using var exported = FixtureDocument.Read(output);
            Require(keys.Key(exported.RootElement.GetProperty("Final").Deserialize<BattleTurnState>()!) == keys.Key(bestFinal),
                "Independent deployment export changed its terminal state/RNG.");
        }
        Console.WriteLine($"DEPLOYMENT-GRID foundWin={bestOutcome == RoomOutcome.BattleWon} units={deployment.Length} candidates={candidates} " +
            $"completed={completed} duplicates={duplicates} unsupported={unsupported} improvements={improvements} fixedOpeningCards={closing.Length} " +
            $"firstRoom={firstRoom?.ToString() ?? "any"} timedOut={Expired()} candidateLimit={candidates >= maxCandidates} elapsedMs={watch.Elapsed.TotalMilliseconds:F1} exported={best < initial} improveWinningRoute={improveWinningRoute} rolloutPolicy={(strengthRollout ? "strength" : "tactical")}" +
            (spread ? $" startSample={startSample} nextSample={nextSample} spreadSamples={samples} sampleLimit={samples >= maxCandidates * 16} spreadOrder={spreadOrder}" : "") + "; model prediction only.");
    }
}

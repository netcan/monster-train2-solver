using System.Diagnostics;
using System.Security.Cryptography;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

// Generates candidates cheaply from one native deployment snapshot. These are
// model predictions; only a fresh game process can establish native acceptance.
internal static class OfflineBattleSearch
{
    // Reuse recorded forecasts before spending another search/native battle.
    // This exports a replayable failure boundary, never a native successor.
    internal static void ExtractUnsupported(string input, string output, string reason)
    {
        if (File.Exists(output)) throw new IOException("Output already exists: " + output);
        Require(!string.IsNullOrWhiteSpace(reason), "An exact rejection reason is required.");
        using var source = FixtureDocument.Read(input);
        FixtureValue[] rows = source.RootElement.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(rows.Length is > 0 and <= 256, "Expected bounded recorded decisions.");
        BattleTurnState root = rows[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
        using var keys = new BattleStateKeys();
        string immutable = keys.Key(root);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var prefix = new List<Step>();
        foreach (FixtureValue row in rows)
        {
            BattleTurnState current = row.GetProperty("Root").Deserialize<BattleTurnState>()!;
            var forecastPrefix = new List<Step>(prefix);
            RoomOutcome outcome = RoomOutcome.Exchanged;
            foreach (FixtureValue stepValue in row.GetProperty("PlanSteps").EnumerateArray())
            {
                if (visited.Add(keys.Key(current)))
                {
                    UnsupportedActionAttempt? failure = BattleActionModel.EnumerateActions(current).UnsupportedAttempts
                        .FirstOrDefault(attempt => attempt.Reason == reason);
                    if (failure != null)
                    {
                        Require(failure.Action != null && forecastPrefix.Count <= 256, "Failure boundary exceeds the native input bound.");
                        BattleTurnState replay = root;
                        outcome = RoomOutcome.Exchanged;
                        foreach (Step step in forecastPrefix)
                        {
                            Require(!Terminal(outcome), "Failure prefix follows a terminal result.");
                            replay = Apply(replay, step.Action, step.IsEndTurn, out outcome);
                        }
                        using var independent = new BattleStateKeys();
                        Require(independent.Key(replay) == independent.Key(current) && independent.Key(root) == immutable,
                            "Failure prefix differs or mutated its deployment root.");
                        BattleActionResult reproduced = BattleActionModel.PlayCard(replay, failure.Action!);
                        Require(!reproduced.Supported && reproduced.Rejection == ActionRejection.Unsupported && reproduced.Reason == reason,
                            "The extracted action does not reproduce its unsupported boundary.");
                        using var document = NativeFixtureCapture.Capture(new { Schema = 1, EvidenceKind = "ModelUnsupportedBranch",
                            SourceArchive = Path.GetFileName(input), SourceSHA256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(input))).ToLowerInvariant(),
                            Root = root, Prefix = forecastPrefix, FailedRoot = current, failure.Action, failure.Reason,
                            NativeValidationRequired = true });
                        using var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write);
                        document.Write(stream);
                        Console.WriteLine($"UNSUPPORTED-BRANCH prefix={forecastPrefix.Count} turn={current.Spawn.Turn} roots={visited.Count} action=" +
                            System.Text.Json.JsonSerializer.Serialize(failure.Action) + " reason=" + reason);
                        return;
                    }
                }
                Require(!Terminal(outcome), "Recorded forecast follows a terminal result.");
                Step next = new Step { Action = stepValue.GetProperty("Action").Deserialize<PlayCardAction>(),
                    IsEndTurn = stepValue.GetProperty("IsEndTurn").GetBoolean() };
                current = Apply(current, next.Action, next.IsEndTurn, out outcome);
                forecastPrefix.Add(next);
            }
            prefix.Add(new Step { Action = row.GetProperty("Action").Deserialize<PlayCardAction>(),
                IsEndTurn = row.GetProperty("IsEndTurn").GetBoolean() });
        }
        throw new InvalidDataException($"No matching unsupported action in {visited.Count} recorded forecast roots: {reason}");
    }

    internal static void Single(string input, string output)
    {
        if (File.Exists(output)) throw new IOException("Output already exists: " + output);
        using var document = FixtureDocument.Read(input);
        FixtureValue request = document.RootElement;
        Require(request.GetProperty("Schema").GetInt32() == 1, "Unknown planner request schema.");
        BattleTurnState root = request.GetProperty("Root").Deserialize<BattleTurnState>()!;
        int horizon = request.GetProperty("Horizon").GetInt32(), milliseconds = request.GetProperty("Milliseconds").GetInt32();
        int floor = request.GetProperty("DeploymentRoom").GetInt32();
        Require(horizon is >= 1 and <= 16 && milliseconds is >= 100 and <= 30000 && floor is >= 0 and <= 2,
            "External planner request exceeds the native option bounds.");
        using var keys = new BattleStateKeys();
        string rootKey = keys.Key(root);
        BattlePlanResult plan = BattlePlanner.Plan(root, keys.Key, new BattlePlannerOptions(256, horizon, 8, 12,
            TimeSpan.FromMilliseconds(milliseconds), floor, request.GetProperty("TacticalSeed").GetBoolean()));
        Require(plan.CompletedHorizon && !plan.Cancelled && plan.Steps.Count > 0, "No complete bounded external plan.");
        var verification = Stopwatch.StartNew();
        BattleTurnState replayed = root;
        RoomOutcome outcome = RoomOutcome.Exchanged;
        foreach (BattleSearchStep step in plan.Steps)
            replayed = Apply(replayed, step.Action, step.IsEndTurn, out outcome);
        using var independent = new BattleStateKeys();
        string finalKey = independent.Key(replayed);
        Require(outcome == plan.Outcome && finalKey == independent.Key(plan.State) && independent.Key(root) == rootKey &&
            plan.Score == BattlePlanner.Evaluate(replayed, outcome) &&
            (!plan.PolicyBaselineScore.HasValue || plan.Score <= plan.PolicyBaselineScore.Value),
            "External plan failed independent full-state/RNG replay or mutated its root.");
        verification.Stop();
        using var result = NativeFixtureCapture.Capture(new { Schema = 1, Verified = true, Root = root, RootKey = rootKey, FinalKey = finalKey,
            ModelSearchMs = plan.Elapsed.TotalMilliseconds, ModelVerificationMs = verification.Elapsed.TotalMilliseconds,
            Plan = new { plan.State, plan.Outcome, plan.Steps, plan.Score, plan.TurnsCompleted, plan.ExpandedNodes,
                plan.DuplicateStates, plan.BeamPruned, plan.TimedOut, plan.Cancelled, plan.ReachedNodeLimit,
                plan.ReachedActionLimit, plan.CompletedHorizon, plan.UnsupportedAttempts, plan.PolicyBaselineScore } });
        using var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write);
        result.Write(stream);
    }

    internal static void Policy(string input, string output, int floor, bool strength = false)
    {
        if (floor is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(floor));
        using var source = FixtureDocument.Read(input);
        BattleTurnState current = DeploymentRoot(source.RootElement);
        var steps = new List<Step>();
        RoomOutcome outcome = RoomOutcome.Exchanged;
        int played = 0;
        while (!Terminal(outcome) && steps.Count < 256)
        {
            SupportedActionCandidate? choice = played >= 18 ? null : strength
                ? BattleHeuristicPolicy.Choose(current, out _, floor) : BattleTacticalPolicy.Choose(current, out _, floor);
            var step = new Step { Action = choice?.Action, IsEndTurn = choice == null };
            current = Apply(current, step.Action, step.IsEndTurn, out outcome);
            steps.Add(step); played = step.IsEndTurn ? 0 : played + 1;
        }
        Freeze(input, System.Text.Json.JsonSerializer.Serialize(steps), output);
    }
    internal static void Freeze(string input, string actionsJson, string output)
    {
        if (File.Exists(output)) throw new IOException("Output already exists: " + output);
        using var source = FixtureDocument.Read(input);
        RequireNativeInput(source.RootElement);
        BattleTurnState root = DeploymentRoot(source.RootElement);
        Require(root.Spawn.Turn == 0, "Frozen whole-battle actions require the native deployment root.");
        Step[] steps = System.Text.Json.JsonSerializer.Deserialize<Step[]>(actionsJson)!;
        Require(steps.Length is > 0 and <= 256, "Expected 1..256 frozen actions.");
        var decisions = new List<Decision>();
        BattleTurnState current = root;
        RoomOutcome outcome = RoomOutcome.Exchanged;
        foreach (Step step in steps)
        {
            Require(!Terminal(outcome) && step.IsEndTurn == (step.Action == null), "Invalid action or action after terminal outcome.");
            BattleTurnState next = Apply(current, step.Action, step.IsEndTurn, out outcome);
            decisions.Add(new Decision { Root = current, Action = step.Action, IsEndTurn = step.IsEndTurn,
                Expected = next, Outcome = outcome, PlanSteps = new[] { step }, Planned = next, PlannedOutcome = outcome,
                Score = BattlePlanner.Evaluate(next, outcome), Nodes = 0, Generation = decisions.Count + 1 });
            current = next;
        }
        Require(Terminal(outcome), "Frozen actions end before the battle's terminal outcome.");
        using (var document = NativeFixtureCapture.Capture(new { Schema = 1, EvidenceKind = "ModelPrediction",
            SourceArchive = Path.GetFileName(input), SourceSHA256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(input))).ToLowerInvariant(),
            Strategy = "external-frozen-actions", Root = root, Final = current, Outcome = outcome,
            RollingDecisions = decisions }))
        using (var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write)) document.Write(stream);
        Verify(output);
    }
    internal static void Run(string input, string output, int horizon, int milliseconds, int floor, bool tactical = false,
        bool playerContinuation = false)
    {
        if (horizon is < 1 or > 16 || milliseconds is < 1 or > 60000 || floor is < 0 or > 2)
            throw new ArgumentOutOfRangeException(nameof(horizon), "Horizon 1..16, budget 1..60000 ms and room 0..2 required.");
        if (File.Exists(output)) throw new IOException("Output already exists: " + output);
        using var source = FixtureDocument.Read(input);
        FixtureValue trace = source.RootElement;
        RequireNativeInput(trace);
        BattleTurnState root = DeploymentRoot(trace);
        Require(root.Spawn.Turn == 0, "Offline whole-battle planning requires the native deployment root.");
        using var rootKeys = new BattleStateKeys();
        string immutable = rootKeys.Key(root);
        var options = new BattlePlannerOptions(256, horizon, 8, 12, TimeSpan.FromMilliseconds(milliseconds), floor, tactical,
            allowShorterHorizon: playerContinuation);
        using var advisor = playerContinuation ? new BattleAdviceSession(options) : null;
        var decisions = new List<Decision>();
        var unsupported = new HashSet<string>(StringComparer.Ordinal);
        var timer = Stopwatch.StartNew();
        BattleTurnState current = root;
        RoomOutcome outcome = RoomOutcome.Exchanged;
        while (!Terminal(outcome) && decisions.Count < 256)
        {
            using var keys = new BattleStateKeys();
            BattleAdvice? advice = null;
            BattlePlanResult plan;
            if (advisor == null) plan = BattlePlanner.Plan(current, keys.Key, options);
            else
            {
                advisor.Observe(current, keys.Key(current));
                var wait = Stopwatch.StartNew();
                while (advisor.Status == BattleAdviceStatus.Planning && wait.Elapsed < options.TimeLimit + TimeSpan.FromSeconds(30))
                { Thread.Sleep(1); advisor.Poll(); }
                Require(advisor.Status == BattleAdviceStatus.Ready && advisor.Current != null,
                    "Player continuation did not produce verified advice: " + advisor.Error);
                advice = advisor.Current;
                plan = advice!.Plan;
            }
            foreach (UnsupportedActionAttempt attempt in plan.UnsupportedAttempts) unsupported.Add(attempt.Reason);
            Require(plan.Steps.Count > 0, "Planner produced no executable first step.");
            BattleSearchStep step = plan.Steps[0];
            BattleTurnState next = Apply(current, step.Action, step.IsEndTurn, out outcome);
            decisions.Add(new Decision { Root = current, Action = step.Action, IsEndTurn = step.IsEndTurn,
                Expected = next, Outcome = outcome, PlanSteps = plan.Steps.Select(item => new Step { Action = item.Action, IsEndTurn = item.IsEndTurn }).ToArray(), Planned = plan.State, PlannedOutcome = plan.Outcome,
                Score = plan.Score, PolicyBaselineScore = plan.PolicyBaselineScore, Nodes = plan.ExpandedNodes, Generation = decisions.Count + 1,
                ReusedContinuation = advice?.ReusedContinuation == true,
                ReusedWinningContinuation = advice?.ReusedWinningContinuation == true });
            current = next;
            advisor?.InvalidateForInput();
            if (step.IsEndTurn) Console.WriteLine($"OFFLINE-TURN turn={current.Spawn.Turn} decisions={decisions.Count} outcome={outcome} " +
                $"pyre={current.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => unit.IsPyre).Select(unit => unit.Health).FirstOrDefault()}");
        }
        Require(Terminal(outcome), "Offline search exceeded 256 decisions without a terminal result.");
        using var freshKeys = new BattleStateKeys();
        Require(immutable == freshKeys.Key(root), "Planning mutated its native root.");
        using (var document = NativeFixtureCapture.Capture(new { Schema = 1, EvidenceKind = "ModelPrediction",
            SourceArchive = Path.GetFileName(input), SourceSHA256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(input))).ToLowerInvariant(),
            Horizon = horizon, MillisecondsPerDecision = milliseconds, DeploymentRoom = floor, TacticalSeed = tactical,
            PlayerContinuation = playerContinuation,
            Root = root, Final = current, Outcome = outcome, ElapsedSeconds = timer.Elapsed.TotalSeconds,
            UnsupportedReasons = unsupported.Order(StringComparer.Ordinal).ToArray(), RollingDecisions = decisions }))
        using (var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write)) document.Write(stream);
        Verify(output);
        Console.WriteLine($"OFFLINE-PLAN outcome={outcome} decisions={decisions.Count} searches={decisions.Count(row => row.Nodes > 0)} " +
            $"ordinaryReuses={decisions.Count(row => row.ReusedContinuation && !row.ReusedWinningContinuation)} winningReuses={decisions.Count(row => row.ReusedWinningContinuation)} " +
            $"playerContinuation={playerContinuation} unsupported={unsupported.Count} seconds={timer.Elapsed.TotalSeconds:F2} output={output}");
    }

    internal static void Verify(string input)
    {
        using var document = FixtureDocument.Read(input);
        FixtureValue trace = document.RootElement;
        Require(trace.GetProperty("Schema").GetInt32() == 1 && trace.GetProperty("EvidenceKind").GetString() == "ModelPrediction",
            "Expected an offline prediction, not native acceptance evidence.");
        using var keys = new BattleStateKeys();
        BattleTurnState current = trace.GetProperty("Root").Deserialize<BattleTurnState>()!;
        BattleTurnState root = current;
        string immutable = keys.Key(root);
        RoomOutcome outcome = RoomOutcome.Exchanged;
        FixtureValue[] decisions = trace.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(decisions.Length is > 0 and <= 256, "Missing bounded frozen decisions.");
        foreach (FixtureValue row in decisions)
        {
            Require(!Terminal(outcome) && keys.Key(current) == keys.Key(row.GetProperty("Root").Deserialize<BattleTurnState>()!),
                "Frozen model root differs or follows a terminal outcome.");
            bool end = row.GetProperty("IsEndTurn").GetBoolean();
            Require(end == (row.GetProperty("Action").ValueKind == FixtureKind.Null), "Invalid frozen first step.");
            BattleTurnState forecast = current;
            RoomOutcome forecastOutcome = RoomOutcome.Exchanged;
            FixtureValue[] forecastSteps = row.GetProperty("PlanSteps").EnumerateArray().ToArray();
            Require(forecastSteps.Length > 0 && forecastSteps[0].GetProperty("IsEndTurn").GetBoolean() == end &&
                forecastSteps[0].GetProperty("Action").ContentEquals(row.GetProperty("Action")),
                "Frozen first step differs from its forecast.");
            foreach (FixtureValue step in forecastSteps)
            {
                Require(!Terminal(forecastOutcome) && step.GetProperty("IsEndTurn").GetBoolean() ==
                    (step.GetProperty("Action").ValueKind == FixtureKind.Null), "Invalid frozen forecast step.");
                forecast = Apply(forecast, step.GetProperty("Action").ValueKind == FixtureKind.Null ? null :
                    step.GetProperty("Action").Deserialize<PlayCardAction>(), step.GetProperty("IsEndTurn").GetBoolean(), out forecastOutcome);
            }
            Require(forecastOutcome == (RoomOutcome)row.GetProperty("PlannedOutcome").GetInt32() &&
                keys.Key(forecast) == keys.Key(row.GetProperty("Planned").Deserialize<BattleTurnState>()!),
                "Frozen complete forecast differs.");
            current = Apply(current, end ? null : row.GetProperty("Action").Deserialize<PlayCardAction>(), end, out outcome);
            Require(outcome == (RoomOutcome)row.GetProperty("Outcome").GetInt32() &&
                keys.Key(current) == keys.Key(row.GetProperty("Expected").Deserialize<BattleTurnState>()!), "Frozen successor differs.");
        }
        Require(Terminal(outcome) && outcome == (RoomOutcome)trace.GetProperty("Outcome").GetInt32() &&
            keys.Key(current) == keys.Key(trace.GetProperty("Final").Deserialize<BattleTurnState>()!), "Frozen terminal state differs.");
        using var freshKeys = new BattleStateKeys();
        Require(immutable == freshKeys.Key(root), "Frozen replay mutated its root.");
        Console.WriteLine($"OFFLINE-VERIFY PASS: {decisions.Length} frozen successors/full forecasts, terminal outcome and complete state/RNG; native validation required.");
    }

    internal static void VerifyNative(string prediction, string native)
    {
        Verify(prediction);
        using var source = FixtureDocument.Read(prediction);
        using var replay = FixtureDocument.Read(native);
        FixtureValue trace = replay.RootElement;
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(trace.GetProperty(counter).GetInt32() == 0, "Fresh native replay has nonzero " + counter);
        Require(trace.GetProperty("TerminalEffectsSettled").GetBoolean(), "Fresh native replay did not settle.");
        FixtureValue[] first = source.RootElement.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        FixtureValue[] second = trace.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        Require(first.Length == second.Length, "Fresh native replay used a different decision count.");
        for (int index = 0; index < first.Length; index++)
        {
            Require(second[index].GetProperty("Mode").GetString() == "replay" && second[index].GetProperty("Nodes").GetInt32() == 0 &&
                second[index].GetProperty("Actual").ValueKind == FixtureKind.Object &&
                second[index].GetProperty("Difference").ValueKind == FixtureKind.Null, "Fresh native decision is incomplete or differs.");
            foreach (string name in new[] { "Root", "Action", "IsEndTurn", "Expected", "Outcome", "PlanSteps", "Planned", "PlannedOutcome",
                "Score", "PolicyBaselineScore", "Generation" })
                Require(first[index].GetProperty(name).ContentEquals(second[index].GetProperty(name)),
                    "Fresh native replay changed frozen decision " + index + " field " + name);
        }
        Require(trace.GetProperty("NativeWon").GetBoolean() ==
            ((RoomOutcome)source.RootElement.GetProperty("Outcome").GetInt32() == RoomOutcome.BattleWon), "Native terminal outcome differs.");
        Console.WriteLine($"OFFLINE-NATIVE PASS: {first.Length} unchanged frozen decisions and zero-node fresh native replay, four zero failure counters and matching terminal outcome.");
    }

    private static BattleTurnState DeploymentRoot(FixtureValue trace)
    {
        if (IsDeploymentObservation(trace))
        {
            FixtureValue player = trace.GetProperty("PlayerAdvisor");
            FixtureValue overlay = player.GetProperty("Overlay");
            BattleTurnState root = trace.GetProperty("Root").Deserialize<BattleTurnState>()!;
            Require(trace.GetProperty("Schema").GetInt32() == 1 && root.Spawn.Turn == 0 &&
                player.GetProperty("Assembly").GetString() == "MonsterTrain2Poju.Advisor" &&
                overlay.GetProperty("Captures").GetInt32() > 0 && overlay.GetProperty("PurityFailures").GetInt32() == 0 &&
                overlay.GetProperty("Failures").GetInt32() == 0 && !overlay.GetProperty("InTestScenario").GetBoolean(),
                "Offline planning needs a pure original deployment observation in the actual player's identity space.");
            return root;
        }
        // Player capture and development capture own separate instance IDs.
        // A rolling replay must retain the namespace used by its driver.
        if (trace.TryGetProperty("RollingDecisions", out FixtureValue rolling) && rolling.GetArrayLength() > 0)
            return rolling[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
        FixtureValue[] actions = trace.GetProperty("Actions").EnumerateArray().ToArray();
        return (actions.Length > 0 ? actions[0] : trace.GetProperty("Turns")[0])
            .GetProperty("Before").Deserialize<BattleTurnState>()!;
    }

    private static bool IsDeploymentObservation(FixtureValue trace) =>
        trace.TryGetProperty("EvidenceKind", out FixtureValue kind) && kind.GetString() == "NativeDeploymentObservation";

    private static void RequireNativeInput(FixtureValue trace)
    {
        if (IsDeploymentObservation(trace)) { DeploymentRoot(trace); return; }
        foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(trace.GetProperty(counter).GetInt32() == 0, "Native input has nonzero " + counter);
        Require(trace.GetProperty("TerminalEffectsSettled").GetBoolean(), "Native input did not settle.");
    }

    private static BattleTurnState Apply(BattleTurnState source, PlayCardAction? action, bool end, out RoomOutcome outcome)
    {
        if (end)
        {
            BattleTurnResult result = BattleTurnModel.EndTurn(source);
            Require(result.Supported, "Unsupported EndTurn: " + result.UnsupportedReason);
            outcome = result.Outcome; return result.State!;
        }
        BattleActionResult played = BattleActionModel.PlayCard(source, action!);
        Require(played.Supported, "Unsupported action: " + played.Reason);
        outcome = played.Outcome; return played.State!;
    }
    private static bool Terminal(RoomOutcome outcome) => outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated or RoomOutcome.Stalemate;
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
    private sealed class Decision
    {
        public BattleTurnState Root { get; set; } = null!;
        public PlayCardAction? Action { get; set; }
        public bool IsEndTurn { get; set; }
        public BattleTurnState Expected { get; set; } = null!;
        public RoomOutcome Outcome { get; set; }
        public IReadOnlyList<Step> PlanSteps { get; set; } = null!;
        public BattleTurnState Planned { get; set; } = null!;
        public RoomOutcome PlannedOutcome { get; set; }
        public double Score { get; set; }
        public double? PolicyBaselineScore { get; set; }
        public int Nodes { get; set; }
        public bool ReusedContinuation { get; set; }
        public bool ReusedWinningContinuation { get; set; }
        public long Generation { get; set; }
    }
    private sealed class Step
    {
        public PlayCardAction? Action { get; set; }
        public bool IsEndTurn { get; set; }
    }
}

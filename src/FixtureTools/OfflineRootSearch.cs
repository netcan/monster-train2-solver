using System.Diagnostics;
using System.Security.Cryptography;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

// Reproduce one captured decision with the Windows player's whole-battle
// defaults, without spending a native run or rolling through an entire battle.
internal static class OfflineRootSearch
{
    internal static void Run(string input, string output, int milliseconds, int decision = 0, int workers = 2)
    {
        if (File.Exists(output)) throw new IOException("Output already exists: " + output);
        Require(milliseconds is >= 100 and <= 30000 && decision >= 0 && workers is 1 or 2,
            "Expected 100..30000ms, a nonnegative decision index and one or two workers.");
        using var source = FixtureDocument.Read(input);
        FixtureValue trace = source.RootElement;
        BattleTurnState root;
        if (trace.TryGetProperty("RollingDecisions", out FixtureValue rows))
        {
            Require(decision < rows.GetArrayLength(), "Decision index is outside the recorded battle.");
            root = rows[decision].GetProperty("Root").Deserialize<BattleTurnState>()!;
        }
        else
        {
            Require(decision == 0, "A single-root capture only accepts decision zero.");
            root = trace.GetProperty("Root").Deserialize<BattleTurnState>()!;
        }
        using var keys = new BattleStateKeys();
        string parent = keys.Key(root);
        var options = new BattlePlannerOptions(256, 3, 8, 12, TimeSpan.FromMilliseconds(milliseconds),
            allowShorterHorizon: true, searchToBattleEnd: true, maxSteps: 256, stopAtBestPyreHealth: true);
        BattlePlanResult plan = ParallelBattlePlanner.Plan(root, options, workers);
        Require(!plan.Cancelled && (plan.CompletedHorizon || plan.UsedShorterHorizon) && plan.Steps.Count > 0,
            $"No complete turn within budget: turns={plan.TurnsCompleted} nodes={plan.ExpandedNodes}.");
        using (var result = NativeFixtureCapture.Capture(new {
            Schema = 1, EvidenceKind = "ModelRootPrediction", NativeValidationRequired = true,
            SourceArchive = Path.GetFileName(input),
            SourceSHA256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(input))).ToLowerInvariant(),
            ModelSHA256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(BattlePlanner).Assembly.Location))).ToLowerInvariant(),
            SourceDecisionIndex = decision, Milliseconds = milliseconds, RequestedWorkers = workers,
            Options = options, Root = root, RootKey = parent, Plan = plan }))
        {
            // Verify in memory before creating an artifact, then verify the
            // independent archive reader in a separate invocation if desired.
            Verify(result.RootElement);
            using var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write);
            result.Write(stream);
        }
        Console.WriteLine($"PLAN-ROOT outcome={plan.Outcome} complete={plan.CompletedHorizon} partial={plan.UsedShorterHorizon} " +
            $"turn={root.Spawn.Turn} forecastTurns={plan.TurnsCompleted} steps={plan.Steps.Count} nodes={plan.ExpandedNodes} " +
            $"workers={plan.SearchWorkers} searchMs={plan.Elapsed.TotalMilliseconds:F1} output={output}; model prediction only.");
    }

    internal static void Verify(string input)
    {
        using var document = FixtureDocument.Read(input);
        Verify(document.RootElement);
    }

    private static void Verify(FixtureValue fixture)
    {
        Require(fixture.GetProperty("Schema").GetInt32() == 1 &&
            fixture.GetProperty("EvidenceKind").GetString() == "ModelRootPrediction" &&
            fixture.GetProperty("NativeValidationRequired").GetBoolean(), "Not an offline root prediction.");
        BattleTurnState root = fixture.GetProperty("Root").Deserialize<BattleTurnState>()!;
        FixtureValue plan = fixture.GetProperty("Plan");
        FixtureValue options = fixture.GetProperty("Options");
        FixtureValue[] steps = plan.GetProperty("Steps").EnumerateArray().ToArray();
        int milliseconds = fixture.GetProperty("Milliseconds").GetInt32();
        int requestedWorkers = fixture.GetProperty("RequestedWorkers").GetInt32();
        int[] threads = plan.GetProperty("WorkerThreadIds").EnumerateArray().Select(value => value.GetInt32()).ToArray();
        Require(milliseconds is >= 100 and <= 30000 && requestedWorkers is 1 or 2 &&
            TimeSpan.Parse(options.GetProperty("TimeLimit").GetString()!, System.Globalization.CultureInfo.InvariantCulture) == TimeSpan.FromMilliseconds(milliseconds) &&
            threads.Length > 0 && threads.Length <= requestedWorkers && threads.Distinct().Count() == threads.Length &&
            threads.Length == plan.GetProperty("SearchWorkers").GetInt32(), "Prediction changed its requested time or worker bounds.");
        Require(!plan.GetProperty("Cancelled").GetBoolean() &&
            (plan.GetProperty("CompletedHorizon").GetBoolean() || plan.GetProperty("UsedShorterHorizon").GetBoolean()) &&
            plan.GetProperty("SearchedToBattleEnd").GetBoolean() && plan.GetProperty("MaxSteps").GetInt32() == 256 &&
            plan.GetProperty("ExpandedNodes").GetInt32() is > 0 and <= 256 && steps.Length is > 0 and <= 256,
            "Prediction lost its completion, whole-battle or shared budget bounds.");
        Require(options.GetProperty("MaxNodes").GetInt32() == 256 && options.GetProperty("MaxTurns").GetInt32() == 3 &&
            options.GetProperty("BeamWidth").GetInt32() == 8 && options.GetProperty("MaxActionsPerTurn").GetInt32() == 12 &&
            options.GetProperty("PreferredDeploymentRoom").GetInt32() == 2 && !options.GetProperty("UseTacticalSeed").GetBoolean() &&
            options.GetProperty("AllowShorterHorizon").GetBoolean() && options.GetProperty("SearchToBattleEnd").GetBoolean() &&
            options.GetProperty("StopAtBestPyreHealth").GetBoolean() && options.GetProperty("MaxSteps").GetInt32() == 256,
            "Prediction does not use the documented player defaults.");
        using var keys = new BattleStateKeys();
        string parent = keys.Key(root);
        Require(parent == fixture.GetProperty("RootKey").GetString(), "Recorded root fingerprint differs.");
        var watch = Stopwatch.StartNew();
        BattleTurnState state = root;
        RoomOutcome outcome = RoomOutcome.Exchanged;
        foreach (FixtureValue step in steps)
        {
            Require(!Terminal(outcome), "A step follows the terminal outcome.");
            bool end = step.GetProperty("IsEndTurn").GetBoolean();
            PlayCardAction? action = step.GetProperty("Action").Deserialize<PlayCardAction>();
            Require(end == (action == null), "Action/EndTurn pair differs.");
            if (end)
            {
                BattleTurnResult next = BattleTurnModel.EndTurn(state);
                Require(next.Supported, next.UnsupportedReason ?? "Unsupported EndTurn.");
                state = next.State!; outcome = next.Outcome;
            }
            else
            {
                BattleActionResult next = BattleActionModel.PlayCard(state, action!);
                Require(next.Supported, next.Reason ?? "Unsupported action.");
                state = next.State!; outcome = next.Outcome;
            }
        }
        Require((int)outcome == plan.GetProperty("Outcome").GetInt32() &&
            keys.Key(state) == keys.Key(plan.GetProperty("State").Deserialize<BattleTurnState>()!) &&
            keys.Key(root) == parent && BattlePlanner.Evaluate(state, outcome) == plan.GetProperty("Score").GetDouble() &&
            steps.Count(step => step.GetProperty("IsEndTurn").GetBoolean()) == plan.GetProperty("TurnsCompleted").GetInt32() &&
            plan.GetProperty("CompletedHorizon").GetBoolean() == Terminal(outcome) &&
            plan.GetProperty("UsedShorterHorizon").GetBoolean() != Terminal(outcome) &&
            (!plan.GetProperty("UsedShorterHorizon").GetBoolean() || !Terminal(outcome) &&
                steps[^1].GetProperty("IsEndTurn").GetBoolean()),
            "Prediction failed independent full-state/RNG replay, score, horizon or immutable-root checks.");
        int hp = state.Spawn.Train.Rooms.SelectMany(room => room.Units).Where(unit => unit.IsPyre)
            .Select(unit => unit.Health).FirstOrDefault();
        Console.WriteLine($"VERIFY-ROOT PASS steps={steps.Length} outcome={outcome} pyre={hp} replayMs={watch.Elapsed.TotalMilliseconds:F1}; model prediction only.");
    }

    private static bool Terminal(RoomOutcome outcome) => outcome is RoomOutcome.BattleWon or RoomOutcome.PlayerDefeated or RoomOutcome.Stalemate;
    private static void Require(bool condition, string reason) { if (!condition) throw new InvalidDataException(reason); }
}

using System.Security.Cryptography;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ExactBattleCommands
{
    internal static void Run(string input, string output, int nodes, int milliseconds, int? horizon)
    {
        if (File.Exists(output)) throw new IOException("Output already exists: " + output);
        using var fixture = FixtureDocument.Read(input);
        var source = fixture.RootElement;
        BattleTurnState root;
        if (source.TryGetProperty("Root", out var requestRoot)) root = requestRoot.Deserialize<BattleTurnState>()!;
        else
        {
            foreach (string counter in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
                Require(source.GetProperty(counter).GetInt32() == 0, "Unaccepted native input: " + counter);
            var actions = source.GetProperty("Actions").EnumerateArray().ToArray();
            root = (actions.Length > 0 ? actions[0] : source.GetProperty("Turns")[0]).GetProperty("Before").Deserialize<BattleTurnState>()!;
        }
        using var initialKeys = new BattleStateKeys();
        string rootKey = initialKeys.Key(root);
        var result = ExactBattleSolver.Minimize(root, options: new ExactBattleOptions(nodes, TimeSpan.FromMilliseconds(milliseconds), horizon));
        string finalKey = Replay(root, rootKey, result.Steps, horizon, result.State, result.Outcome, result.Score);
        using var document = NativeFixtureCapture.Capture(new { Schema = 1, EvidenceKind = "ModelPrediction", Verified = true,
            ProofScope = "EnumeratedDeterministicModel", SourceArchive = Path.GetFileName(input),
            SourceSHA256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(input))).ToLowerInvariant(),
            Root = root, RootKey = rootKey, FinalKey = finalKey,
            Options = new { MaxNodes = nodes, Milliseconds = milliseconds, TurnHorizon = horizon },
            Result = result });
        using var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write); document.Write(stream);
        Console.WriteLine($"EXACT-BATTLE verified=true terminalOptimal={result.CertifiedTerminalOptimal} " +
            $"horizonOptimal={result.CertifiedHorizonOptimal} nodes={result.ExpandedNodes} terminals={result.TerminalLeaves} " +
            $"unsupported={result.UnsupportedAttempts.Count} outcome={result.Outcome} score={result.Score}");
    }

    internal static void Verify(string input)
    {
        using var fixture = FixtureDocument.Read(input);
        var source = fixture.RootElement;
        Require(source.GetProperty("Schema").GetInt32() == 1 &&
            source.GetProperty("EvidenceKind").GetString() == "ModelPrediction" &&
            source.GetProperty("ProofScope").GetString() == "EnumeratedDeterministicModel",
            "Expected a deterministic model prediction archive.");
        var result = source.GetProperty("Result");
        var steps = result.GetProperty("Steps").Deserialize<BattleSearchStep[]>()!;
        var horizon = source.GetProperty("Options").GetProperty("TurnHorizon");
        string finalKey = Replay(source.GetProperty("Root").Deserialize<BattleTurnState>()!,
            source.GetProperty("RootKey").GetString()!, steps,
            horizon.ValueKind == FixtureKind.Null ? null : horizon.GetInt32(),
            result.GetProperty("State").Deserialize<BattleTurnState>()!,
            result.GetProperty("Outcome").Deserialize<RoomOutcome>(), result.GetProperty("Score").GetDouble());
        Require(finalKey == source.GetProperty("FinalKey").GetString(), "Archived final key differs from replay.");
        Console.WriteLine($"EXACT-VERIFY PASS: {steps.Length} model steps, full state/RNG and score match.");
    }

    private static string Replay(BattleTurnState root, string rootKey, IReadOnlyList<BattleSearchStep> steps,
        int? horizon, BattleTurnState expected, RoomOutcome expectedOutcome, double score)
    {
        using var startingKeys = new BattleStateKeys();
        Require(startingKeys.Key(root) == rootKey, "Archived root key differs from the initial state.");
        Require(horizon is not < 0, "Invalid EndTurn horizon.");
        var replay = root;
        RoomOutcome outcome = RoomOutcome.Exchanged;
        int turns = 0;
        foreach (var step in steps)
        {
            Require(outcome is RoomOutcome.Exchanged or RoomOutcome.Cleared, "Exact prediction continues after a resolved outcome.");
            Require(turns < (horizon ?? int.MaxValue), "Exact prediction continues beyond its EndTurn horizon.");
            Require(step.IsEndTurn == (step.Action == null), "Exact prediction step must contain one input.");
            if (step.IsEndTurn)
            {
                var next = BattleTurnModel.EndTurn(replay, step.RelentlessDecisions);
                Require(next.Supported, next.UnsupportedReason ?? "Unsupported EndTurn.");
                replay = next.State!;
                outcome = next.Outcome;
                turns++;
            }
            else
            {
                var next = BattleActionModel.PlayCard(replay, step.Action!);
                Require(next.Supported, next.Reason ?? "Unsupported action.");
                replay = next.State!;
                outcome = next.Outcome;
            }
        }
        using var independent = new BattleStateKeys();
        string finalKey = independent.Key(replay);
        Require(independent.Key(root) == rootKey && finalKey == independent.Key(expected) &&
            outcome == expectedOutcome && BattlePlanner.Evaluate(replay, outcome) == score,
            "Exact result failed independent full-state/RNG replay or changed its parent.");
        return finalKey;
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

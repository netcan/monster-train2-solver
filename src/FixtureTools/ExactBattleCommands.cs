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
        var replay = root; RoomOutcome outcome = RoomOutcome.Exchanged;
        foreach (var step in result.Steps)
        {
            if (step.IsEndTurn)
            {
                var next = BattleTurnModel.EndTurn(replay); Require(next.Supported, next.UnsupportedReason ?? "Unsupported EndTurn.");
                replay = next.State!; outcome = next.Outcome;
            }
            else
            {
                var next = BattleActionModel.PlayCard(replay, step.Action!); Require(next.Supported, next.Reason ?? "Unsupported action.");
                replay = next.State!; outcome = next.Outcome;
            }
        }
        using var independent = new BattleStateKeys();
        Require(independent.Key(root) == rootKey && independent.Key(replay) == independent.Key(result.State) &&
            outcome == result.Outcome && BattlePlanner.Evaluate(replay, outcome) == result.Score,
            "Exact result failed independent full-state/RNG replay or changed its parent.");
        using var document = NativeFixtureCapture.Capture(new { Schema = 1, EvidenceKind = "ModelPrediction", Verified = true,
            ProofScope = "EnumeratedDeterministicModel", SourceArchive = Path.GetFileName(input),
            SourceSHA256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(input))).ToLowerInvariant(),
            Root = root, RootKey = rootKey, FinalKey = independent.Key(replay),
            Options = new { MaxNodes = nodes, Milliseconds = milliseconds, TurnHorizon = horizon },
            Result = result });
        using var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write); document.Write(stream);
        Console.WriteLine($"EXACT-BATTLE verified=true terminalOptimal={result.CertifiedTerminalOptimal} " +
            $"horizonOptimal={result.CertifiedHorizonOptimal} nodes={result.ExpandedNodes} terminals={result.TerminalLeaves} " +
            $"unsupported={result.UnsupportedAttempts.Count} outcome={result.Outcome} score={result.Score}");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

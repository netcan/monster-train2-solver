using System.Security.Cryptography;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class InactiveFragileInfernoExitRequest
{
    private const string Reason = "Native inactive Fragile Inferno exit moon mask differs on the fourth turn.";
    private sealed record Step(PlayCardAction? Action, bool IsEndTurn);

    internal static void Extract(string input, string output, bool heavy = false)
    {
        if (File.Exists(output)) throw new IOException("Output already exists: " + output);
        using var source = FixtureDocument.Read(input);
        var decisions = source.RootElement.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        var turns = source.RootElement.GetProperty("Turns").EnumerateArray().ToArray();
        Require(decisions.Length == (heavy ? 26 : 19) && turns.Length == (heavy ? 5 : 4) &&
            turns[^1].GetProperty("Difference").GetString() == "Decision state, resources, card piles or gameplay RNG differ",
            "Expected the original incomplete native mask mismatch.");
        var root = decisions[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
        var current = root;
        var prefix = new List<Step>();
        using var keys = new BattleStateKeys();
        foreach (var row in decisions.SkipLast(1))
        {
            var captured = row.GetProperty("Root").Deserialize<BattleTurnState>()!;
            var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            bool end = row.GetProperty("IsEndTurn").GetBoolean();
            var action = row.GetProperty("Action").Deserialize<PlayCardAction>();
            Require(keys.Key(current) == keys.Key(captured), "Recorded decision root changed.");
            if (end)
            {
                var result = BattleTurnModel.EndTurn(current);
                Require(result.Supported && result.Outcome == RoomOutcome.Exchanged, "Recorded turn cannot be replayed: " + result.UnsupportedReason);
                current = result.State!;
            }
            else
            {
                var result = BattleActionModel.PlayCard(current, action!);
                Require(result.Supported && result.Outcome == RoomOutcome.Exchanged, "Recorded card cannot be replayed: " + result.Reason);
                current = result.State!;
            }
            Require(keys.Key(current) == keys.Key(actual), "Recorded decision successor differs from native.");
            prefix.Add(new Step(action, end));
        }
        var failedRoot = decisions[^1].GetProperty("Root").Deserialize<BattleTurnState>()!;
        var expected = decisions[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(decisions[^1].GetProperty("IsEndTurn").GetBoolean(), "Recorded final decision changed.");
        var boundary = BattleTurnModel.EndTurn(current);
        Require(keys.Key(current) == keys.Key(failedRoot), "Recorded last decision and captured turn root differ.");
        Require(boundary.Supported && boundary.Outcome == RoomOutcome.Exchanged, "Repaired boundary was rejected: " + boundary.UnsupportedReason);
        Require(keys.Key(boundary.State!) == keys.Key(expected), "Repaired boundary differs from the original native successor.");
        using var request = MonsterTrain2Poju.Capture.NativeFixtureCapture.Capture(new {
            Schema = 1, EvidenceKind = "ModelUnsupportedBranch", SourceArchive = Path.GetFileName(input),
            SourceSHA256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(input))).ToLowerInvariant(),
            Root = root, Prefix = prefix, FailedRoot = failedRoot, Action = (PlayCardAction?)null,
            IsEndTurn = true, Reason = heavy ? "Native inactive heavy Inferno exit moon masks differ on the fifth turn." : Reason,
            NativeValidationRequired = true });
        using var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write);
        request.Write(stream);
        Console.WriteLine($"INACTIVE-INFERNO-REQUEST PASS: {prefix.Count} native prefix decisions and repaired turn replayed; independent native validation required.");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

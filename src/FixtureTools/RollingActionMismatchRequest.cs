using System.Security.Cryptography;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RollingActionMismatchRequest
{
    private sealed record Step(PlayCardAction? Action, bool IsEndTurn);

    internal static void Extract(string input, string output)
    {
        if (File.Exists(output)) throw new IOException("Output already exists: " + output);
        using var source = FixtureDocument.Read(input);
        FixtureValue[] rows = source.RootElement.GetProperty("RollingDecisions").EnumerateArray().ToArray();
        FixtureValue[] actions = source.RootElement.GetProperty("Actions").EnumerateArray().ToArray();
        Require(rows.Length is > 0 and < 256 && actions.Length > 0, "Expected a bounded native action prefix.");
        FixtureValue failure = actions[^1];
        Require(failure.GetProperty("Difference").GetString() == "Card play decision state differs" &&
            failure.GetProperty("Predicted").GetProperty("Supported").GetBoolean(), "Expected a supported action with native state mismatch.");
        BattleTurnState root = rows[0].GetProperty("Root").Deserialize<BattleTurnState>()!;
        BattleTurnState current = root;
        using var keys = new BattleStateKeys();
        var prefix = new List<Step>();
        foreach (FixtureValue row in rows.Take(rows.Length - 1))
        {
            Require(keys.Key(current) == keys.Key(row.GetProperty("Root").Deserialize<BattleTurnState>()!), "Native prefix root differs.");
            bool end = row.GetProperty("IsEndTurn").GetBoolean();
            PlayCardAction? action = row.GetProperty("Action").Deserialize<PlayCardAction>();
            if (end)
            {
                BattleTurnResult result = BattleTurnModel.EndTurn(current);
                Require(result.Supported && result.Outcome == RoomOutcome.Exchanged, "Native prefix turn rejected: " + result.UnsupportedReason);
                current = result.State!;
            }
            else
            {
                BattleActionResult result = BattleActionModel.PlayCard(current, action!);
                Require(result.Supported && result.Outcome == RoomOutcome.Exchanged, "Native prefix action rejected: " + result.Reason);
                current = result.State!;
            }
            Require(keys.Key(current) == keys.Key(row.GetProperty("Actual").Deserialize<BattleTurnState>()!), "Native prefix successor differs at " + prefix.Count + ".");
            prefix.Add(new Step(action, end));
        }
        BattleTurnState before = rows[^1].GetProperty("Root").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = rows[^1].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction boundary = failure.GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(rows[^1].GetProperty("Action").ContentEquals(failure.GetProperty("Action")) &&
            !rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Rolling and action boundaries differ.");
        Require(keys.Key(current) == keys.Key(before), "Native mismatch root differs from matched prefix.");
        BattleActionResult predicted = BattleActionModel.PlayCard(current, boundary);
        Require(predicted.Supported && keys.Key(predicted.State!) != keys.Key(actual), "Mismatch no longer reproduced.");
        const string reason = "Native rolling action successor mismatch observation.";
        using var request = NativeFixtureCapture.Capture(new { Schema = 1, EvidenceKind = "ModelUnsupportedBranch",
            SourceArchive = Path.GetFileName(input), SourceSHA256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(input))).ToLowerInvariant(),
            Root = root, Prefix = prefix, FailedRoot = current, Action = boundary, IsEndTurn = false, Reason = reason,
            NativeValidationRequired = true });
        using var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write);
        request.Write(stream);
        Console.WriteLine("ROLLING-ACTION-MISMATCH PASS: " + prefix.Count + " matched prefix inputs; boundary requires independent native observation.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

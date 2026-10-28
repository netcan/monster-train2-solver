using System.Security.Cryptography;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PreviewIdMismatchRequest
{
    internal const string Reason = "Native temporary preview birth identity differs with two live Titanskin sources.";
    private sealed record Step(PlayCardAction? Action, bool IsEndTurn);
    internal static void Extract(string input, string output)
    {
        if (File.Exists(output)) throw new IOException("Output already exists: " + output);
        using var source = FixtureDocument.Read(input);
        var fixture = source.RootElement;
        Require(!fixture.GetProperty("NativeCompleted").GetBoolean(), "Expected the original partial native capture.");
        var rows = fixture.GetProperty("Rows").EnumerateArray().ToArray();
        Require(rows.Length == 21 && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Expected the 21-input mismatch.");
        var root = fixture.GetProperty("Request").GetProperty("Root").Deserialize<BattleTurnState>()!;
        var current = root;
        var prefix = new List<Step>();
        using var keys = new BattleStateKeys();
        foreach (var row in rows)
        {
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var actual = row.GetProperty("Actual").Deserialize<BattleTurnState>()!;
            bool end = row.GetProperty("IsEndTurn").GetBoolean();
            var action = row.GetProperty("Action").Deserialize<PlayCardAction>();
            Require(keys.Key(current) == keys.Key(before), "Recorded input root differs.");
            if (end)
            {
                var result = BattleTurnModel.EndTurn(current);
                Require(result.Supported && result.Outcome == RoomOutcome.Exchanged, "Recorded turn rejected: " + result.UnsupportedReason);
                current = result.State!;
            }
            else
            {
                var result = BattleActionModel.PlayCard(current, action!);
                Require(result.Supported && result.Outcome == RoomOutcome.Exchanged, "Recorded action rejected: " + result.Reason);
                current = result.State!;
            }
            Require(keys.Key(current) == keys.Key(actual), "Repaired successor differs from native.");
            if (row.Equals(rows[^1])) break;
            prefix.Add(new Step(action, end));
        }
        var failed = rows[^1].GetProperty("Before").Deserialize<BattleTurnState>()!;
        var old = rows[^1].GetProperty("Expected").Deserialize<BattleTurnState>()!;
        Require(old.Spawn.Train.Context!.NextUnitId == 27 && current.Spawn.Train.Context!.NextUnitId == 26,
            "Missing historical identity mismatch.");
        using var request = NativeFixtureCapture.Capture(new { Schema = 1, EvidenceKind = "ModelUnsupportedBranch",
            SourceArchive = Path.GetFileName(input), SourceSHA256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(input))).ToLowerInvariant(),
            Root = root, Prefix = prefix, FailedRoot = failed, Action = (PlayCardAction?)null,
            IsEndTurn = true, Reason, NativeValidationRequired = true });
        using var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write);
        request.Write(stream);
        Console.WriteLine("PREVIEW-ID-REQUEST PASS: 20 native prefix inputs and repaired mismatch; independent native validation required.");
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidDataException(reason); }
}

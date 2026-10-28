using System.Security.Cryptography;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PreviewIdMismatchRequest
{
    internal const string Reason = "Native temporary preview birth identity differs with two live Titanskin sources.";
    internal const string ExistingHordeReason = "Native temporary preview birth identity differs with existing champion horde and two live Titanskin sources.";
    private sealed record Step(PlayCardAction? Action, bool IsEndTurn);
    internal static void Extract(string input, string output) => Extract(input, output, 21, 27, 26, Reason);
    internal static void ExtractExistingHorde(string input, string output) => Extract(input, output, 11, 23, 22, ExistingHordeReason);
    private static void Extract(string input, string output, int expectedRows, int oldNextId, int nativeNextId, string reason)
    {
        if (File.Exists(output)) throw new IOException("Output already exists: " + output);
        using var source = FixtureDocument.Read(input);
        var fixture = source.RootElement;
        Require(!fixture.GetProperty("NativeCompleted").GetBoolean(), "Expected the original partial native capture.");
        var rows = fixture.GetProperty("Rows").EnumerateArray().ToArray();
        Require(rows.Length == expectedRows && rows[^1].GetProperty("IsEndTurn").GetBoolean(), "Unexpected identity mismatch prefix.");
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
        Require(old.Spawn.Train.Context!.NextUnitId == oldNextId && current.Spawn.Train.Context!.NextUnitId == nativeNextId,
            "Missing historical identity mismatch.");
        using var request = NativeFixtureCapture.Capture(new { Schema = 1, EvidenceKind = "ModelUnsupportedBranch",
            SourceArchive = Path.GetFileName(input), SourceSHA256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(input))).ToLowerInvariant(),
            Root = root, Prefix = prefix, FailedRoot = failed, Action = (PlayCardAction?)null,
            IsEndTurn = true, Reason = reason, NativeValidationRequired = true });
        using var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write);
        request.Write(stream);
        Console.WriteLine("PREVIEW-ID-REQUEST PASS: " + (expectedRows - 1) + " native prefix inputs and repaired mismatch; independent native validation required.");
    }
    internal static void Refresh(string input, string native, string output) => Refresh(input, native, output, Reason, 21, false);
    internal static void RefreshExistingHorde(string input, string native, string output) => Refresh(input, native, output, ExistingHordeReason, 11, true);
    private static void Refresh(string input, string native, string output, string identityReason, int expectedRows, bool expectedEndTurn)
    {
        if (File.Exists(output)) throw new IOException("Output already exists: " + output);
        using var original = FixtureDocument.Read(input);
        using var observation = FixtureDocument.Read(native);
        var request = original.RootElement;
        var rows = observation.RootElement.GetProperty("Rows").EnumerateArray().ToArray();
        var steps = request.GetProperty("Prefix").EnumerateArray().ToArray();
        Require(observation.RootElement.GetProperty("NativeCompleted").GetBoolean() && rows.Length == expectedRows && steps.Length == rows.Length &&
            observation.RootElement.GetProperty("Request").GetProperty("Reason").GetString() == identityReason,
            "Expected the independently validated identity prefix.");
        var root = request.GetProperty("Root").Deserialize<BattleTurnState>()!;
        var current = root;
        var prefix = new List<Step>();
        using var keys = new BattleStateKeys();
        for (int index = 0; index < rows.Length; index++)
        {
            var row = rows[index]; var step = steps[index];
            bool end = step.GetProperty("IsEndTurn").GetBoolean();
            var action = step.GetProperty("Action").Deserialize<PlayCardAction>();
            Require(row.GetProperty("Action").ContentEquals(step.GetProperty("Action")) &&
                row.GetProperty("IsEndTurn").GetBoolean() == end &&
                keys.Key(current) == keys.Key(row.GetProperty("Before").Deserialize<BattleTurnState>()!),
                "Independent prefix input differs at " + index);
            if (end)
            {
                var result = BattleTurnModel.EndTurn(current);
                Require(result.Supported && result.Outcome == RoomOutcome.Exchanged, "Prefix turn rejected: " + result.UnsupportedReason);
                current = result.State!;
            }
            else
            {
                var result = BattleActionModel.PlayCard(current, action!);
                Require(result.Supported && result.Outcome == RoomOutcome.Exchanged, "Prefix action rejected: " + result.Reason);
                current = result.State!;
            }
            Require(keys.Key(current) == keys.Key(row.GetProperty("Actual").Deserialize<BattleTurnState>()!),
                "Independent prefix successor differs at " + index);
            prefix.Add(new Step(action, end));
        }
        bool boundaryEnd = request.GetProperty("IsEndTurn").GetBoolean();
        var boundaryAction = request.GetProperty("Action").Deserialize<PlayCardAction>();
        string reason = request.GetProperty("Reason").GetString()!;
        Require(boundaryEnd == expectedEndTurn && (boundaryEnd ? boundaryAction == null &&
            BattleTurnModel.EndTurn(current).UnsupportedReason == reason : boundaryAction != null &&
            BattleActionModel.PlayCard(current, boundaryAction).Reason == reason), "Original target no longer reproduces its exact boundary.");
        using var refreshed = NativeFixtureCapture.Capture(new { Schema = 1, EvidenceKind = "ModelUnsupportedBranch",
            SourceArchive = Path.GetFileName(input), SourceSHA256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(input))).ToLowerInvariant(),
            Root = root, Prefix = prefix, FailedRoot = current, Action = boundaryAction, IsEndTurn = boundaryEnd, Reason = reason,
            NativeValidationRequired = true });
        using var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write);
        refreshed.Write(stream);
        Console.WriteLine("PREVIEW-ID-REFRESH PASS: " + expectedRows + " independently native-matched inputs and original protected target; native validation required.");
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidDataException(reason); }
}

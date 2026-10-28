using System.Security.Cryptography;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class PreviewIdMismatchRequest
{
    internal const string Reason = "Native temporary preview birth identity differs with two live Titanskin sources.";
    internal const string ExistingHordeReason = "Native temporary preview birth identity differs with existing champion horde and two live Titanskin sources.";
    private sealed record Step(PlayCardAction? Action, bool IsEndTurn);
    internal static void RefreshNativePrefix(string input, string native, string output)
    {
        if (File.Exists(output)) throw new IOException("Output already exists: " + output);
        using var source = FixtureDocument.Read(input);
        using var observation = FixtureDocument.Read(native);
        var request = source.RootElement;
        var captured = observation.RootElement;
        Require(request.GetProperty("EvidenceKind").GetString() == "ModelUnsupportedBranch" &&
            captured.GetProperty("EvidenceKind").GetString() == "NativeUnsupportedBranchObservation" &&
            captured.GetProperty("NativeCompleted").GetBoolean() &&
            captured.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4", "Expected a completed original native observation.");
        var steps = request.GetProperty("Prefix").EnumerateArray().ToArray();
        var rows = captured.GetProperty("Rows").EnumerateArray().ToArray();
        Require(rows.Length > 0 && rows.Length <= steps.Length, "Native observation must cover a nonempty prefix.");
        var root = request.GetProperty("Root").Deserialize<BattleTurnState>()!;
        var current = root;
        using var keys = new BattleStateKeys();
        Require(keys.Key(root) == keys.Key(captured.GetProperty("Request").GetProperty("Root").Deserialize<BattleTurnState>()!), "Native root differs.");
        var prefix = new List<Step>();
        for (int index = 0; index < steps.Length; index++)
        {
            var step = steps[index];
            bool end = step.GetProperty("IsEndTurn").GetBoolean();
            var action = step.GetProperty("Action").Deserialize<PlayCardAction>();
            if (index < rows.Length)
                Require(rows[index].GetProperty("Action").ContentEquals(step.GetProperty("Action")) &&
                    rows[index].GetProperty("IsEndTurn").GetBoolean() == end &&
                    keys.Key(current) == keys.Key(rows[index].GetProperty("Before").Deserialize<BattleTurnState>()!), "Native input differs at " + index);
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
            if (index < rows.Length)
                Require(keys.Key(current) == keys.Key(rows[index].GetProperty("Actual").Deserialize<BattleTurnState>()!), "Native successor differs at " + index);
            prefix.Add(new Step(action, end));
        }
        bool boundaryEnd = request.GetProperty("IsEndTurn").GetBoolean();
        var boundaryAction = request.GetProperty("Action").Deserialize<PlayCardAction>();
        string reason = request.GetProperty("Reason").GetString()!;
        Require(boundaryEnd ? boundaryAction == null && BattleTurnModel.EndTurn(current).UnsupportedReason == reason :
            boundaryAction != null && BattleActionModel.PlayCard(current, boundaryAction).Reason == reason,
            "Original protected target no longer reproduces its rejection.");
        using var refreshed = NativeFixtureCapture.Capture(new { Schema = 1, EvidenceKind = "ModelUnsupportedBranch",
            SourceArchive = Path.GetFileName(input), SourceSHA256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(input))).ToLowerInvariant(),
            Root = root, Prefix = prefix, FailedRoot = current, Action = boundaryAction, IsEndTurn = boundaryEnd, Reason = reason,
            NativeValidationRequired = true });
        using var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write);
        refreshed.Write(stream);
        Console.WriteLine("NATIVE-PREFIX-REFRESH PASS: " + rows.Length + " independently matched inputs; " + (steps.Length - rows.Length) +
            " model-only inputs and the protected target require native observation.");
    }
    internal static void ObservePartial(string input, string output)
    {
        if (File.Exists(output)) throw new IOException("Output already exists: " + output);
        using var source = FixtureDocument.Read(input);
        var fixture = source.RootElement;
        Require(fixture.GetProperty("EvidenceKind").GetString() == "NativeIncompleteBranchObservation" &&
            !fixture.GetProperty("NativeCompleted").GetBoolean(), "Expected an incomplete native observation.");
        var rows = fixture.GetProperty("Rows").EnumerateArray().ToArray();
        Require(rows.Length > 0, "Expected a mismatching input.");
        var root = fixture.GetProperty("Request").GetProperty("Root").Deserialize<BattleTurnState>()!;
        var current = root;
        var prefix = new List<Step>();
        using var keys = new BattleStateKeys();
        foreach (var row in rows.Take(rows.Length - 1))
        {
            Require(keys.Key(current) == keys.Key(row.GetProperty("Before").Deserialize<BattleTurnState>()!), "Recorded input root differs.");
            bool end = row.GetProperty("IsEndTurn").GetBoolean();
            var action = row.GetProperty("Action").Deserialize<PlayCardAction>();
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
            Require(keys.Key(current) == keys.Key(row.GetProperty("Actual").Deserialize<BattleTurnState>()!), "Recorded successor differs from native.");
            prefix.Add(new Step(action, end));
        }
        var failed = rows[^1];
        Require(keys.Key(current) == keys.Key(failed.GetProperty("Before").Deserialize<BattleTurnState>()!), "Mismatch root differs.");
        Require(keys.Key(failed.GetProperty("Expected").Deserialize<BattleTurnState>()!) !=
            keys.Key(failed.GetProperty("Actual").Deserialize<BattleTurnState>()!), "Final input does not contain a model mismatch.");
        using var request = NativeFixtureCapture.Capture(new { Schema = 1, EvidenceKind = "ModelUnsupportedBranch",
            SourceArchive = Path.GetFileName(input), SourceSHA256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(input))).ToLowerInvariant(),
            Root = root, Prefix = prefix, FailedRoot = current, Action = failed.GetProperty("Action").Deserialize<PlayCardAction>(),
            IsEndTurn = failed.GetProperty("IsEndTurn").GetBoolean(), Reason = "Native partial prefix successor mismatch observation.", NativeValidationRequired = true });
        using var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write);
        request.Write(stream);
        Console.WriteLine("PARTIAL-MISMATCH-REQUEST PASS: " + prefix.Count + " native-matched prefix inputs; boundary requires independent native observation.");
    }
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

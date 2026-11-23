using System.Security.Cryptography;
using MonsterTrain2Poju.Capture;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class LifemotherMixedRequest
{
    internal static void Extract(string sourcePath, string outputPath, int targetId)
    {
        if (targetId is not (3 or 14)) throw new ArgumentOutOfRangeException(nameof(targetId));
        using var input = File.OpenRead(sourcePath);
        string sha = Convert.ToHexString(SHA256.HashData(input)).ToLowerInvariant();
        if (sha != "02fd04bd37210d6e3bc947b13377d97015394bd39747612b27324b183ee164e9")
            throw new InvalidDataException("Expected the accepted original Lifemother Debuffs source battle.");
        input.Position = 0;
        using var source = FixtureDocument.Read(input);
        var root = source.RootElement;
        var actions = root.GetProperty("Actions").EnumerateArray().Take(7).ToArray();
        var turns = root.GetProperty("Turns").EnumerateArray().Take(3).ToArray();
        var rows = actions.Select(row => (Row: row, End: false)).Concat(turns.Select(row => (Row: row, End: true)))
            .OrderBy(input => input.Row.GetProperty("Before").GetProperty("Spawn").GetProperty("Turn").GetInt32())
            .ThenBy(input => input.End).ThenBy(input => input.Row.GetProperty("Index").GetInt32()).ToArray();
        var origin = rows[0].Row.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState current = origin;
        using var keys = new BattleStateKeys(ignorePreviewCopyAllocator: true);
        var prefix = new List<BattleSearchStep>();
        foreach (var (row, end) in rows)
        {
            var before = row.GetProperty("Before").Deserialize<BattleTurnState>()!;
            var action = end ? null : row.GetProperty("Action").Deserialize<PlayCardAction>();
            if (keys.Key(before) != keys.Key(current))
            {
                // The normal native runner focuses the target room as a separate UI operation.
                if (action == null || before.SelectedRoom != action.RoomIndex || before.SelectedRoom == current.SelectedRoom)
                    throw new InvalidDataException("Unexpected recorded prefix focus at " + prefix.Count);
                BattleTurnResult focus = BattleTurnModel.SelectRoom(current, before.SelectedRoom);
                if (!focus.Supported || keys.Key(focus.State!) != keys.Key(before))
                    throw new InvalidDataException("Recorded room selection did not replay at " + prefix.Count);
            }
            var step = new BattleSearchStep(action, end);
            current = end ? BattleTurnModel.EndTurn(current).State! : BattleActionModel.PlayCard(current, action!).State!;
            if (current == null || keys.Key(current) != keys.Key(row.GetProperty("Actual").Deserialize<BattleTurnState>()!))
                throw new InvalidDataException("Recorded prefix successor differed at " + prefix.Count);
            prefix.Add(step);
        }
        string reason = targetId == 3 ? "Battle preview floor 2: Lifemother death transfer requires observed Malaise-only status effects." :
            "Battle preview floor 2: Lifemother death transfer requires an observed ordered debuff set.";
        var actionCandidate = new PlayCardAction(31, 2, targetUnitId: targetId);
        BattleActionResult candidate = BattleActionModel.PlayCard(current, actionCandidate);
        if (!candidate.Supported && (candidate.Rejection != ActionRejection.Unsupported || candidate.Reason != reason))
            throw new InvalidDataException("Original Torch alternative was rejected at a different boundary: " + candidate.Reason);
        using var request = NativeFixtureCapture.Capture(new { Schema = 1, EvidenceKind = "ModelUnsupportedBranch",
            // The registered fixture keeps the same bytes under a different name.
            // Preserve the native receipt's original source name.
            SourceArchive = "full-battle.mt2f",
            SourceSHA256 = sha,
            SourceGameModuleMvid = NativeBranchIdentity.SourceModule(root), Root = origin, Prefix = prefix.ToArray(),
            FailedRoot = current, Action = actionCandidate, IsEndTurn = false, Reason = reason, NativeValidationRequired = true });
        using var output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write);
        request.Write(output);
        Console.WriteLine($"LIFEMOTHER-MIXED prefix={prefix.Count} turn={current.Spawn.Turn} card=31 target={targetId}.");
    }
}

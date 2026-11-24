using System.Security.Cryptography;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class RegionRunAlternateChecks
{
    internal static void Native(string sourcePath, string receiptPath)
    {
        using var sourceStream = File.OpenRead(sourcePath);
        string sourceHash = Convert.ToHexString(SHA256.HashData(sourceStream));
        sourceStream.Position = 0;
        using var source = FixtureDocument.Read(sourceStream);
        using var receipt = ModelJson.ReadFixture(receiptPath);
        FixtureValue witness = receipt.RootElement;
        Require(witness.GetProperty("Boundary").GetString() == "NativeRegionRunAlternate" &&
            witness.GetProperty("NativeCompleted").GetBoolean() &&
            witness.GetProperty("CaptureFailures").GetInt32() == 0 && witness.GetProperty("Pending").GetInt32() == 0 &&
            string.Equals(witness.GetProperty("SourceSHA256").GetString(), sourceHash, StringComparison.OrdinalIgnoreCase),
            "Expected a settled original region alternate for the supplied battle archive.");
        foreach (string field in new[] { "GameModuleMvid", "NativeRunInput" })
            Require(witness.GetProperty(field).ContentEquals(source.RootElement.GetProperty(field)), "Alternate lineage differs in " + field);

        int index = witness.GetProperty("DecisionIndex").GetInt32();
        FixtureValue original = source.RootElement.GetProperty("Actions")[index];
        var before = witness.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var action = witness.GetProperty("Action").Deserialize<PlayCardAction>()!;
        var originalAction = original.GetProperty("Action").Deserialize<PlayCardAction>()!;
        Require(witness.GetProperty("PrefixPlays").GetInt32() == index &&
            witness.GetProperty("PrefixEndTurns").GetInt32() == before.Spawn.Turn &&
            action.CardInstanceId == originalAction.CardInstanceId &&
            (action.RoomIndex != originalAction.RoomIndex || action.TargetUnitId != originalAction.TargetUnitId),
            "The original prefix or alternate target differs.");

        BattleActionResult played = BattleActionModel.PlayCard(before, action);
        Require(played.Supported && played.Outcome == witness.GetProperty("ActualOutcome").Deserialize<RoomOutcome>(),
            played.Reason ?? "The alternate paid action outcome differs.");
        BattleTurnState after = RecordedBattleChecks.RecordedBoundary(played.State!, witness.GetProperty("Actual"),
            terminal: false, "After alternate play", trackPreviewPosition: true);
        FixtureValue turn = witness.GetProperty("EndTurn");
        BattleTurnState turnRoot = RecordedBattleChecks.RecordedBoundary(after, turn.GetProperty("Before"),
            terminal: false, "Before alternate EndTurn", trackPreviewPosition: true);
        BattleTurnResult ended = BattleTurnModel.EndTurn(turnRoot, RecordedBattleChecks.Decisions(turn));
        Require(ended.Supported && ended.Outcome == turn.GetProperty("ActualOutcome").Deserialize<RoomOutcome>(),
            ended.UnsupportedReason ?? "The alternate EndTurn outcome differs.");
        var next = RecordedBattleChecks.RecordedBoundary(ended.State!, turn.GetProperty("Actual"),
            terminal: false, "After alternate EndTurn", trackPreviewPosition: true);
        var owners = next.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Where(unit => unit.Statuses.Any(status => status.Id == "hivemind")).ToArray();
        Require(next.Spawn.Turn == 6 && owners.Length == 7 &&
            owners.Sum(unit => unit.Statuses.Single(status => status.Id == "hivemind").Stacks) == 29 &&
            owners.All(unit => unit.Modifiers?.Upgrades.SingleOrDefault(upgrade =>
                upgrade.DataId == "RoomStateHivemindModifierUpgrade")?.Stats.Damage == 29),
            "Expected the original seven surviving holders with a refreshed +29 cache.");
        Console.WriteLine("NATIVE-REGION-ALTERNATE PASS: exact paid action, EndTurn, next-turn seven owners at +29, and RNG.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

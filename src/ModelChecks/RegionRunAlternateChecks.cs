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
        BattleTurnState next = ReplayEndTurn(after, witness.GetProperty("EndTurn"));
        if (action is { RoomIndex: 2, TargetUnitId: 19 })
        {
            CheckHivemind(next, turn: 6, holders: 7, total: 29, bossStacks: 3);
        }
        else
        {
            Require(action is { RoomIndex: 1, TargetUnitId: 27 }, "Unexpected original alternate target.");
            CheckHivemind(next, turn: 6, holders: 6, total: 23, bossStacks: 6);
            next = ReplayEndTurn(next, witness.GetProperty("SecondEndTurn"));
            CheckHivemind(next, turn: 7, holders: 3, total: 16, bossStacks: 12);
        }
        Console.WriteLine($"NATIVE-REGION-ALTERNATE PASS room={action.RoomIndex} target={action.TargetUnitId} " +
            $"turn={next.Spawn.Turn}: exact continuous paid action, EndTurn successors, owner caches, and RNG.");
    }

    private static BattleTurnState ReplayEndTurn(BattleTurnState previous, FixtureValue turn)
    {
        BattleTurnState turnRoot = RecordedBattleChecks.RecordedBoundary(previous, turn.GetProperty("Before"),
            terminal: false, "Before alternate EndTurn", trackPreviewPosition: true);
        BattleTurnResult ended = BattleTurnModel.EndTurn(turnRoot, RecordedBattleChecks.Decisions(turn));
        Require(ended.Supported && ended.Outcome == turn.GetProperty("ActualOutcome").Deserialize<RoomOutcome>(),
            ended.UnsupportedReason ?? "The alternate EndTurn outcome differs.");
        return RecordedBattleChecks.RecordedBoundary(ended.State!, turn.GetProperty("Actual"),
            terminal: false, "After alternate EndTurn", trackPreviewPosition: true);
    }

    private static void CheckHivemind(BattleTurnState state, int turn, int holders, int total, int bossStacks)
    {
        var owners = state.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Where(unit => unit.Statuses.Any(status => status.Id == "hivemind")).ToArray();
        Require(state.Spawn.Turn == turn && owners.Length == holders &&
            owners.Sum(unit => unit.Statuses.Single(status => status.Id == "hivemind").Stacks) == total &&
            owners.Single(unit => unit.IsBoss == true).Statuses.Single(status => status.Id == "hivemind").Stacks == bossStacks &&
            owners.All(unit => unit.Modifiers?.Upgrades.SingleOrDefault(upgrade =>
                upgrade.DataId == "RoomStateHivemindModifierUpgrade")?.Stats.Damage == total),
            $"Expected turn {turn} with {holders} original holders at +{total} and boss Hivemind {bossStacks}.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

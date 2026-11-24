using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalDominionDeckUpgradeChecks
{
    internal static void Native(FixtureValue witness)
    {
        Require(Text(witness, "GameVersion") == Game231FixtureCatalog.Version &&
            Text(witness, "GameModuleMvid") == Game231FixtureCatalog.Module &&
            Text(witness, "Boundary") == "OriginalPaidDominionMothersRemnant" &&
            Text(witness, "OwnerId") == DominionDeckUpgradeModel.OwnerId &&
            Text(witness, "MotherId") == RelicCopyCardOnCardPlayModel.MothersRemnant3Id,
            "Expected the pinned original paid unit copy.");
        FixtureValue callbackBefore = witness.GetProperty("CallbackBefore"), callbackAfter = witness.GetProperty("CallbackAfter");
        FixtureValue beforeStreams = callbackBefore.GetProperty("Rng"), afterStreams = callbackAfter.GetProperty("Rng");
        UnityRng rewardsBefore = Rng(beforeStreams), rewardsAfter = Rng(afterStreams);
        Require(rewardsAfter.Equals(rewardsBefore.Next()) && new[] { "Battle", "CardDraw", "BattleTest" }.All(stream =>
            beforeStreams.GetProperty(stream).ContentEquals(afterStreams.GetProperty(stream))),
            "The native permanent card callback should advance only Rewards RNG once.");

        var root = witness.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var action = witness.GetProperty("Action").Deserialize<PlayCardAction>()!;
        var context = root.Spawn.Train.Context!;
        Require(context.Resources?.Dominion is { } pool && pool.RewardsRng.Equals(rewardsBefore) &&
            pool.Choices.Select(item => item.DataId).SequenceEqual(witness.GetProperty("PoolUpgradeIds").EnumerateArray()
                .Select(row => row[0].GetString())), "The root must contain the captured ordered upgrade pool and Rewards state.");

        var played = BattleActionModel.PlayCard(root, action);
        Require(played.Supported, played.Reason ?? "Expected the original paid Steward deployment.");
        BattleTurnState after = RecordedBattleChecks.RecordedBoundary(played.State!, witness.GetProperty("Actual"),
            terminal: false, "original paid Dominion copy", trackPreviewPosition: true);
        var afterContext = after.Spawn.Train.Context!;
        int copyId = witness.GetProperty("Copy").GetProperty("Id").GetInt32();
        Require(afterContext.Resources?.Dominion?.RewardsRng.Equals(rewardsAfter) == true &&
            afterContext.FindCard(copyId)?.Permanent.Upgrades.Any(upgrade => upgrade.DataId == Text(witness, "AddedUpgrade")) == true &&
            afterContext.Resources.VisibleDeckCount == context.Resources!.VisibleDeckCount + 1,
            "The copied card, permanent deck count and Rewards cursor must match the native callback.");
        var candidates = BattleActionModel.EnumerateActions(root, action.CardInstanceId, includeAbilities: false);
        Require(!candidates.HasUnsupportedAttempts && candidates.SupportedActions.Any(candidate =>
            candidate.RoomIndex == action.RoomIndex), "The original paid unit copy must be available to the search.");

        using var keys = new BattleStateKeys();
        string rootKey = keys.Key(root), successorKey = keys.Key(played.State!);
        Parallel.For(0, 16, _ =>
        {
            var branch = BattleActionModel.PlayCard(root, action);
            using var branchKeys = new BattleStateKeys();
            Require(branch.Supported && branchKeys.Key(branch.State!) == successorKey, "An independent Dominion branch diverged.");
        });
        using var finalKeys = new BattleStateKeys();
        Require(finalKeys.Key(root) == rootKey, "The parent Dominion state changed during branch replay.");
        Console.WriteLine("NATIVE-DOMINION-COPY PASS: original paid permanent unit copy, full successor and Rewards state, 16 independent branches.");
    }

    private static UnityRng Rng(FixtureValue streams)
    {
        var words = streams.GetProperty("Rewards");
        return new UnityRng(words[0].GetUInt32(), words[1].GetUInt32(), words[2].GetUInt32(), words[3].GetUInt32());
    }

    private static string? Text(FixtureValue state, string field) => state.GetProperty(field).GetString();
    private static void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
}

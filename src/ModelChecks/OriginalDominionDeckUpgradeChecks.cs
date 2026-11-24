using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalDominionDeckUpgradeChecks
{
    internal static void Savagery(FixtureValue witness)
    {
        FixtureValue owner = witness.GetProperty("Owner");
        Require(Text(witness, "GameVersion") == Game231FixtureCatalog.Version &&
            Text(witness, "GameModuleMvid") == Game231FixtureCatalog.Module &&
            Text(witness, "Boundary") == "OriginalSavageryChampionDeckUpgrade" &&
            Text(owner, "Id") == StartingUnitDeckUpgradeModel.SavageryId && Text(owner, "Key") == StartingUnitDeckUpgradeModel.SavageryKey &&
            Text(owner, "Effect") == StartingUnitDeckUpgradeModel.Effect && Text(owner, "Selection") == "SelectAll" &&
            Text(owner, "Filter") == "OnlyChampions" && witness.GetProperty("AllPoolChoicesAppended").GetBoolean(),
            "Expected the original Savagery champion-only ordered upgrade pool.");
        var resources = witness.GetProperty("Resources").Deserialize<BattleResourceState>()!;
        var relic = new CombatRelicState(Text(owner, "Id")!, Text(owner, "Key")!, [Text(owner, "Effect")!],
            isCovenant: false, disallowedInPlacementPhase: false);
        var context = new CombatContext(new CardCycleState([], [], [], default, 0, []), default, 0, 0, 10,
            relics: [relic], resources: resources);
        Require(StartingUnitDeckUpgradeModel.Validate(context) == null && resources.Savagery?.RewardsRng == null &&
            resources.Savagery!.Choices.Select(choice => choice.DataId).SequenceEqual(witness.GetProperty("OriginalPool").EnumerateArray()
                .Select(row => Text(row.GetProperty("Effects")[0], "UpgradeId"))) &&
            StartingUnitDeckUpgradeModel.Validate(context.WithResources(null)) != null &&
            witness.GetProperty("BeforeAcquireRng").ContentEquals(witness.GetProperty("AfterAcquireRng")),
            "The captured Savagery pool must match the original ordered choices and require no RNG.");
        FixtureValue[] cards = witness.GetProperty("Cards").EnumerateArray().ToArray();
        Require(cards.Length == 3 && cards.Select(row => Text(row, "Type")).SequenceEqual(new[] { "Monster", "Monster", "Spell" }) &&
            cards.Select(row => row.GetProperty("Champion").GetBoolean()).SequenceEqual(new[] { true, false, false }),
            "Missing native champion, ordinary unit or spell control.");
        foreach (FixtureValue row in cards)
        {
            var mask = row.GetProperty("CardMask").Deserialize<CardMaskDescriptor>()!;
            var initial = new CardModifiers(CardModifiers.Empty().Offsets,
                row.GetProperty("BaselineModifiers").Deserialize<CardUpgradeModifier[]>()!, persistentHealth: 0, externalInteractions: []);
            var candidate = new CardInstanceState(1, Text(row, "Id")!, initial, CardModifiers.Empty(), 0, 0, 0, [], maskDescriptor: mask);
            string? error = StartingUnitDeckUpgradeModel.Apply(context, candidate, out var upgraded, out var updated);
            Require(error == null, error ?? "Unable to apply the original Savagery pool.");
            var actual = row.GetProperty("OwnedModifiers").Deserialize<CardUpgradeModifier[]>()!;
            string? difference = ModelJson.Difference(JsonSerializer.Serialize(upgraded.Permanent.Upgrades, ModelJson.Options),
                JsonSerializer.Serialize(actual, ModelJson.Options));
            Require(ReferenceEquals(context, updated) && difference == null &&
                row.GetProperty("OwnedRngBefore").ContentEquals(row.GetProperty("OwnedRngAfter")) &&
                row.GetProperty("BaselineRngBefore").ContentEquals(row.GetProperty("BaselineRngAfter")),
                "Native Savagery upgrades or deterministic RNG differ: " + difference);
        }
        Console.WriteLine("NATIVE-SAVAGERY-DECK-UPGRADE CHECK PASS: three ordered champion upgrades, two ineligible controls, unchanged RNG.");
    }

    internal static void Native(FixtureValue witness)
    {
        Require(Text(witness, "GameVersion") == Game231FixtureCatalog.Version &&
            Text(witness, "GameModuleMvid") == Game231FixtureCatalog.Module &&
            Text(witness, "Boundary") == "OriginalPaidDominionMothersRemnant" &&
            Text(witness, "OwnerId") == StartingUnitDeckUpgradeModel.DominionId &&
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

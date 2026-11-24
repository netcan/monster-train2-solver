using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using MonsterTrain2Poju.NativeCasePlans;

internal static class NativeAdvisorRestartChecks
{
    internal static void Native(FixtureValue receipt, bool undo = false)
    {
        string evidence = undo ? "NativeAdvisorUndo" : "NativeAdvisorRestart";
        Require(Number(receipt, "Schema") == 1 && Text(receipt, "EvidenceKind") == evidence &&
            Text(receipt, "GameVersion") == "2.3.1" && Text(receipt, "GameModuleMvid") == Game231FixtureCatalog.Module &&
            Text(receipt, "Assembly") == "MonsterTrain2Poju.Advisor" && Version.TryParse(Text(receipt, "Version"), out _),
            "Expected the release Advisor running in pinned native game 2.3.1.");

        var opening = State(receipt, "Opening");
        var publishedOpening = State(receipt, "OpeningPublication");
        var restored = State(receipt, "Restored");
        var publishedRestored = State(receipt, "RestoredPublication");
        var input = receipt.GetProperty("Input");
        var paidBefore = State(input, "Before");
        var paidAfter = State(input, "Actual");
        var paidPublication = State(receipt, "PaidPublication");
        var action = input.GetProperty("Action").Deserialize<PlayCardAction>()!;

        using var keys = new BattleStateKeys(ignorePreviewCopyAllocator: true);
        string initial = keys.Key(opening);
        int expectedTurn = undo ? 1 : 0;
        Require(opening.Spawn.Turn == expectedTurn && restored.Spawn.Turn == expectedTurn, "The restored decision has the wrong turn.");
        Require(initial == keys.Key(publishedOpening) && keys.Key(restored) == keys.Key(publishedRestored),
            "Each publication must correspond to its own captured game state.");
        string? restoredDifference = undo ? UndoDecisionComparison.Difference(opening, restored) :
            initial == keys.Key(restored) ? null : "Restart changed the opening decision.";
        Require(restoredDifference == null, "Opening and restored decisions disagree: " + restoredDifference);
        // The development probe and the release observer allocate their card IDs
        // independently. Replay the paid action only in its original identity space.
        Require(DecisionInputsEqual(opening, paidBefore) && DecisionInputsEqual(paidAfter, paidPublication),
            "The paid input trace and release publication differ in hand, resources or RNG.");
        var spent = BattleActionModel.PlayCard(paidBefore, action);
        Require(spent.Supported && keys.Key(spent.State!) == keys.Key(paidAfter) && keys.Key(paidPublication) != initial &&
            paidAfter.Spawn.Train.Context!.Cards.Hand.All(token => token.InstanceId != action.CardInstanceId),
            "The native card did not leave the hand and produce a distinct verified publication. " + spent.Reason);
        if (undo) VerifyUndoBoundary(receipt, opening, keys);
        else Require(paidAfter.Energy < paidBefore.Energy && paidAfter.Spawn.Train.Rooms.SelectMany(room => room.Units)
            .Any(unit => unit.Team == CombatTeam.Player && unit.SpawnerCardId == action.CardInstanceId),
            "Restart fixture needs the original Ember payment and deployment.");

        foreach (string field in new[] { "OpeningRootKey", "PaidRootKey", "RestoredRootKey" })
            Require(Text(receipt, field) is { Length: 64 } hash && hash.All(Uri.IsHexDigit), "Missing issued runtime key " + field);
        string actionName = undo ? "Undo" : "Restart";
        Require(Text(receipt, "OpeningRootKey") != Text(receipt, "PaidRootKey") &&
            receipt.GetProperty("PriorPublicationRetired").GetBoolean() &&
            Number(receipt, actionName + "CountAfter") == Number(receipt, actionName + "CountBefore") + 1 &&
            Number(receipt, "Run" + actionName + "CountAfter") == Number(receipt, "Run" + actionName + "CountBefore") + 1 &&
            Number(receipt, "GenerationAfter") > Number(receipt, "GenerationBefore") &&
            Number(receipt, "SearchesAfter") > Number(receipt, "SearchesBefore") &&
            Number(receipt, "PublicationsAfter") > Number(receipt, "PublicationsBefore") &&
            Number(receipt, "AcceptedCardInputs") == 1 && Number(receipt, "Failures") == 0 && Number(receipt, "PurityFailures") == 0,
            "The prior publication was not retired before one native restoration and a fresh search.");

        var first = receipt.GetProperty("RestoredFirstStep").Deserialize<BattleSearchStep>()!;
        Require(first.IsEndTurn ? BattleTurnModel.EndTurn(restored).Supported :
            first.Action != null && BattleActionModel.PlayCard(restored, first.Action).Supported,
            "The restored publication begins with an unsupported operation.");
        Console.WriteLine($"NATIVE-ADVISOR-{actionName.ToUpperInvariant()} PASS: accepted card, exact restored root/RNG, retired publication and fresh advice.");
    }

    private static void VerifyUndoBoundary(FixtureValue receipt, BattleTurnState opening, BattleStateKeys keys)
    {
        FixtureValue endTurn = receipt.GetProperty("FirstEndTurn");
        var before = State(endTurn, "Before");
        var after = State(endTurn, "Actual");
        var advanced = BattleTurnModel.EndTurn(before);
        Require(before.Spawn.Turn == 0 && after.Spawn.Turn == 1 && advanced.Supported &&
            keys.Key(advanced.State!) == keys.Key(after) && DecisionInputsEqual(after, opening),
            "Undo fixture needs the original deployment EndTurn and first ordinary decision. " + advanced.UnsupportedReason);
        int openingEntries = Number(receipt, "ReplayEntriesAtOpening");
        Require(Number(receipt, "ReplayEntriesBeforeUndo") == openingEntries + 1 &&
            Number(receipt, "ReplayEntriesAfterUndo") == openingEntries && Number(receipt, "UndoEntriesToTrim") == 1,
            "Original UndoTurn did not trim exactly the last accepted card entry.");
    }

    private static bool DecisionInputsEqual(BattleTurnState left, BattleTurnState right) =>
        left.Spawn.Turn == right.Spawn.Turn && left.SelectedRoom == right.SelectedRoom && left.Energy == right.Energy &&
        left.Spawn.Train.Context!.Cards.Hand.Select(token => token.DataId).SequenceEqual(
            right.Spawn.Train.Context!.Cards.Hand.Select(token => token.DataId)) &&
        left.RngStreams.Select(stream => (stream.Name, stream.Seed, stream.State)).SequenceEqual(
            right.RngStreams.Select(stream => (stream.Name, stream.Seed, stream.State)));

    private static BattleTurnState State(FixtureValue value, string name) => value.GetProperty(name).Deserialize<BattleTurnState>()!;
    private static string? Text(FixtureValue value, string name) => value.GetProperty(name).GetString();
    private static int Number(FixtureValue value, string name) => value.GetProperty(name).GetInt32();
    private static void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
}

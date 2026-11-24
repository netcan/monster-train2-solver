using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class NativeAdvisorRestartChecks
{
    internal static void Native(FixtureValue receipt)
    {
        Require(Number(receipt, "Schema") == 1 && Text(receipt, "EvidenceKind") == "NativeAdvisorRestart" &&
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
        Require(opening.Spawn.Turn == 0 && restored.Spawn.Turn == 0, "Expected deployment before and after restart.");
        foreach ((string label, BattleTurnState snapshot) in new[] { ("opening publication", publishedOpening),
            ("restored", restored), ("restored publication", publishedRestored) })
            Require(initial == keys.Key(snapshot), "Opening and " + label + " disagree: " +
                ModelJson.Difference(System.Text.Json.JsonSerializer.Serialize(opening, ModelJson.Options),
                    System.Text.Json.JsonSerializer.Serialize(snapshot, ModelJson.Options)));
        // The development probe and the release observer allocate their card IDs
        // independently. Replay the paid action only in its original identity space.
        Require(DecisionInputsEqual(opening, paidBefore) && DecisionInputsEqual(paidAfter, paidPublication),
            "The paid input trace and release publication differ in hand, resources or RNG.");
        var spent = BattleActionModel.PlayCard(paidBefore, action);
        Require(spent.Supported && paidAfter.Energy < paidBefore.Energy &&
            keys.Key(spent.State!) == keys.Key(paidAfter) && keys.Key(paidPublication) != initial &&
            paidAfter.Spawn.Train.Rooms.SelectMany(room => room.Units)
                .Any(unit => unit.Team == CombatTeam.Player && unit.SpawnerCardId == action.CardInstanceId),
            "The native paid deployment did not produce a distinct verified publication. " + spent.Reason);

        foreach (string field in new[] { "OpeningRootKey", "PaidRootKey", "RestoredRootKey" })
            Require(Text(receipt, field) is { Length: 64 } hash && hash.All(Uri.IsHexDigit), "Missing issued runtime key " + field);
        Require(Text(receipt, "OpeningRootKey") != Text(receipt, "PaidRootKey") &&
            receipt.GetProperty("PriorPublicationRetired").GetBoolean() &&
            Number(receipt, "RestartCountAfter") == Number(receipt, "RestartCountBefore") + 1 &&
            Number(receipt, "RunRestartCountAfter") == Number(receipt, "RunRestartCountBefore") + 1 &&
            Number(receipt, "GenerationAfter") > Number(receipt, "GenerationBefore") &&
            Number(receipt, "SearchesAfter") > Number(receipt, "SearchesBefore") &&
            Number(receipt, "PublicationsAfter") > Number(receipt, "PublicationsBefore") &&
            Number(receipt, "AcceptedCardInputs") == 1 && Number(receipt, "Failures") == 0 && Number(receipt, "PurityFailures") == 0,
            "The prior publication was not retired before one native restart and a fresh search.");

        var first = receipt.GetProperty("RestoredFirstStep").Deserialize<BattleSearchStep>()!;
        Require(first.IsEndTurn ? BattleTurnModel.EndTurn(restored).Supported :
            first.Action != null && BattleActionModel.PlayCard(restored, first.Action).Supported,
            "The restored publication begins with an unsupported operation.");
        Console.WriteLine("NATIVE-ADVISOR-RESTART PASS: paid deployment, exact restored root/RNG, retired publication and fresh advice.");
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

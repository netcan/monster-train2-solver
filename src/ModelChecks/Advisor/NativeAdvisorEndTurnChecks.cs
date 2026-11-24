using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using MonsterTrain2Poju.NativeCasePlans;

internal static class NativeAdvisorEndTurnChecks
{
    internal static void Native(FixtureValue receipt)
    {
        Require(Number(receipt, "Schema") == 1 && Text(receipt, "EvidenceKind") == "NativeAdvisorEndTurnConfirmation" &&
            Text(receipt, "GameVersion") == "2.3.1" && Text(receipt, "GameModuleMvid") == Game231FixtureCatalog.Module &&
            Text(receipt, "Assembly") == "MonsterTrain2Poju.Advisor", "Expected the pinned native release Advisor receipt.");

        FixtureValue before = receipt.GetProperty("Before");
        FixtureValue canceled = receipt.GetProperty("AfterCancel");
        FixtureValue accepted = receipt.GetProperty("Accepted");
        FixtureValue after = receipt.GetProperty("After");
        var opening = State(before, "Root");
        var publishedAfter = State(after, "Root");
        using var keys = new BattleStateKeys();
        Require(opening.Spawn.Turn == 0 && before.GetProperty("Root").ContentEquals(canceled.GetProperty("Root")) &&
            Number(before, "Generation") == Number(canceled, "Generation") &&
            Number(before, "Searches") == Number(canceled, "Searches") &&
            Number(before, "AcceptedEndTurns") == 0 && Number(canceled, "AcceptedEndTurns") == 0,
            "Cancel changed the opening decision, generation, search or accepted input count.");
        Require(accepted.GetProperty("PriorAdviceRetired").GetBoolean() &&
            Number(accepted, "Generation") > Number(canceled, "Generation") &&
            Number(after, "Generation") >= Number(accepted, "Generation") &&
            Number(accepted, "AcceptedEndTurns") == 1 && Number(after, "AcceptedEndTurns") == 1 &&
            new[] { before, canceled, accepted, after }.All(snapshot => Number(snapshot, "CardInputs") == 0),
            "The confirmed native EndTurn did not retire the prior advice and record exactly one input.");

        FixtureValue endTurn = receipt.GetProperty("EndTurn");
        var nativeBefore = State(endTurn, "Before");
        var nativeAfter = State(endTurn, "Actual");
        var predicted = BattleTurnModel.EndTurn(nativeBefore);
        Require(predicted.Supported && keys.Key(predicted.State!) == keys.Key(nativeAfter),
            "The recorded native EndTurn did not replay exactly. " + predicted.UnsupportedReason);
        // The release Advisor and development trace each keep their own runtime IDs.
        var predictedPublication = BattleTurnModel.EndTurn(opening);
        Require(publishedAfter.Spawn.Turn == 1 && predictedPublication.Supported &&
            keys.Key(predictedPublication.State!) == keys.Key(publishedAfter),
            "The published decision did not match its exact successor. " + predictedPublication.UnsupportedReason);
        Console.WriteLine("NATIVE-ADVISOR-END-TURN PASS: canceled prompt kept the verified root and search; one native EndTurn retired advice and replayed exactly.");
    }

    private static BattleTurnState State(FixtureValue value, string name) => value.GetProperty(name).Deserialize<BattleTurnState>()!;
    private static string? Text(FixtureValue value, string name) => value.GetProperty(name).GetString();
    private static long Number(FixtureValue value, string name) => value.GetProperty(name).GetInt64();
    private static void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
}

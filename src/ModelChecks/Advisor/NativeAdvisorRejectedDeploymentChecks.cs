using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;
using MonsterTrain2Poju.NativeCasePlans;

internal static class NativeAdvisorRejectedDeploymentChecks
{
    internal static void Native(FixtureValue receipt)
    {
        TrueFinalBossChecks.Require(Number(receipt, "Schema") == 1 &&
            Text(receipt, "EvidenceKind") == "NativeAdvisorRejectedDeployment" &&
            Text(receipt, "GameVersion") == "2.3.1" && Text(receipt, "GameModuleMvid") == Game231FixtureCatalog.Module &&
            Text(receipt, "Assembly") == "MonsterTrain2Poju.Advisor", "Expected the pinned native release Advisor receipt.");
        FixtureValue before = receipt.GetProperty("Before");
        FixtureValue input = receipt.GetProperty("Input");
        FixtureValue after = receipt.GetProperty("After");
        var root = before.GetProperty("Root").Deserialize<BattleTurnState>()!;
        TrueFinalBossChecks.Require(root.Spawn.Turn == 0 &&
            root.Spawn.Train.Context!.Cards.Hand.Any(card => card.DataId == Text(input, "CardDataId")) &&
            Number(input, "Room") is >= 0 and < 3 && Text(input, "DropTeam") == "Heroes" &&
            !input.GetProperty("Played").GetBoolean() &&
            before.GetProperty("Root").ContentEquals(after.GetProperty("Root")) &&
            Number(input, "Generation") == Number(before, "Generation") && Number(before, "Searches") > 0 &&
            Number(input, "Searches") == Number(before, "Searches") && Number(after, "Searches") == Number(before, "Searches") &&
            Number(before, "CardInputs") == 0 && Number(after, "CardInputs") == 0,
            "The rejected deployment changed the decision, immediately retired advice or restarted its search.");
        Console.WriteLine("NATIVE-ADVISOR-REJECTED-DEPLOYMENT PASS: native rejection kept advice, the original search and exact state/RNG.");
    }

    private static string? Text(FixtureValue value, string name) => value.GetProperty(name).GetString();
    private static long Number(FixtureValue value, string name) => value.GetProperty(name).GetInt64();
}

using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class GoldieChecks
{
    internal static void Native(string path)
    {
        using var archive = ModelJson.ReadFixture(path);
        var root = archive.RootElement;
        var definition = root.GetProperty("OriginalGoldieDefinition");
        int tier = Int(definition, "Tier");
        Require(root.GetProperty("OriginalGoldiePrepared").GetBoolean() &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            definition.ContentEquals(root.GetProperty("OriginalGoldieDefinitionAfter")) &&
            tier is >= 1 and <= 3 && Int(definition, "CoinsPerUnit") == 10 * tier &&
            definition.GetProperty("Stat").GetString() == "UnmodifiedPlayedCost", "Missing unchanged original Goldie soul.");
        var callbacks = root.GetProperty("OriginalGoldieCallbacks").EnumerateArray().ToArray();
        Require(callbacks.All(row => row.GetProperty("Completed").GetBoolean()), "Original Goldie callback failed to settle.");
        var reward = callbacks.Single(row => row.GetProperty("Kind").GetString() == "PlayedDiscard" && row.GetProperty("WasPlayed").GetBoolean());
        var cast = callbacks.Single(row => row.GetProperty("Kind").GetString() == "OnCastReward");
        var before = reward.GetProperty("Before"); var after = reward.GetProperty("After");
        var witness = root.GetProperty("OriginalGoldieWitness");
        Require(Int(before, "UnmodifiedPlayedCost") == 3 && Int(after, "Gold") - Int(before, "Gold") == tier * 30 &&
            Int(cast.GetProperty("Before"), "Gold") == Int(cast.GetProperty("After"), "Gold") &&
            witness.GetProperty("RetainReturn").GetProperty("InHand").GetBoolean(),
            "Original Goldie did not grant gold during played discard or return the card next turn.");
        CardCycleChecks.Native(root);
        SolverStageChecks.Native(path);
        Console.WriteLine($"GOLDIE PASS tier={tier} paid=3 reward={tier * 30}; original return and complete search successors agree.");
    }

    private static int Int(FixtureValue value, string key) => value.GetProperty(key).GetInt32();
    private static void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
}

using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OpeningPyreDamageChecks
{
    internal static void Native(string path)
    {
        using var archive = ModelJson.ReadFixture(path);
        var root = archive.RootElement;
        var definition = root.GetProperty("OriginalSmallDeckDamageDefinition");
        string? owner = definition.GetProperty("Id").GetString();
        Require(root.GetProperty("OriginalSmallDeckDamagePrepared").GetBoolean() &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            definition.ContentEquals(root.GetProperty("OriginalSmallDeckDamageDefinitionAfter")) &&
            definition.GetProperty("ControlledAcquisition").GetBoolean() &&
            owner is RelicOpeningPyreDamageModel.Threshold30 or RelicOpeningPyreDamageModel.Threshold15,
            "Missing original Endless small-deck opening damage definition.");

        var callbacks = root.GetProperty("OriginalSmallDeckDamageCallbacks").EnumerateArray().ToArray();
        var test = callbacks.Single(row => row.GetProperty("Kind").GetString() == "Test");
        var application = callbacks.Single(row => row.GetProperty("Kind").GetString() == "Apply");
        var before = application.GetProperty("Before");
        var after = application.GetProperty("After");
        var damageCall = application.GetProperty("DamageCalls").EnumerateArray().Single();
        int amount = (Int(definition, "Threshold") - Int(before, "DrawPileCount")) * Int(definition, "PerCard");
        int health = Int(after, "SaveHP");
        Require(callbacks.All(row => row.GetProperty("Completed").GetBoolean() && Int(row.GetProperty("Before"), "Turn") == 0) &&
            test.GetProperty("Passed").GetBoolean() && test.GetProperty("Before").ContentEquals(test.GetProperty("After")) &&
            amount > 0 && Int(damageCall, "Amount") == -amount && damageCall.GetProperty("TriggerDamageTakenRelicEffects").GetBoolean() &&
            health > 0 && Int(before, "SaveHP") - health == amount,
            "Native opening count, signed Pyre damage, or settle state differ.");

        var first = root.GetProperty("Actions").EnumerateArray().First().GetProperty("Before").Deserialize<BattleTurnState>()!;
        var context = first.Spawn.Train.Context!;
        Require(first.Spawn.Turn == 0 && first.Spawn.Train.Rooms.SelectMany(room => room.Units).Single(unit => unit.IsPyre).Health == health &&
            context.Resources?.OpeningPyreDamageSettled == true && RelicModel.Validate(context) == null,
            "The first solver decision did not carry the native opening Pyre health.");
        CardCycleChecks.Native(root);
        SolverStageChecks.Native(path);
        Console.WriteLine($"OPENING-PYRE-DAMAGE PASS threshold={Int(definition, "Threshold")} damage={amount} remaining={health}.");
    }

    private static int Int(FixtureValue value, string key) => value.GetProperty(key).GetInt32();
    private static void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
}

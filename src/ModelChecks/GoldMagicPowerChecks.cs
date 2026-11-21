using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class GoldMagicPowerChecks
{
    internal static void Native(string path)
    {
        using var archive = ModelJson.ReadFixture(path);
        var root = archive.RootElement;
        var definition = root.GetProperty("OriginalMagicPowerPerGoldDefinition");
        Require(root.GetProperty("OriginalMagicPowerPerGoldPrepared").GetBoolean() &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            definition.ContentEquals(root.GetProperty("OriginalMagicPowerPerGoldDefinitionAfter")) &&
            definition.GetProperty("Id").GetString() == RelicGoldMagicPowerModel.MutatorId,
            "Missing unchanged original gold magic-power mutator.");
        var callbacks = root.GetProperty("OriginalMagicPowerPerGoldCallbacks").EnumerateArray().ToArray();
        Require(callbacks.All(row => row.GetProperty("Completed").GetBoolean()) && callbacks
            .Where(row => row.GetProperty("Kind").GetString() == "Test")
            .All(row => row.GetProperty("Passed").GetBoolean() && row.GetProperty("Before").ContentEquals(row.GetProperty("After"))),
            "The native gold magic-power predicate changed state.");
        var applied = callbacks.Where(row => row.GetProperty("Kind").GetString() == "Apply")
            .Select(row => row.GetProperty("After")).ToArray();
        Check(applied.Single(row => Int(row, "Turn") == 0), 100, 1, requireGenerated: false);
        Check(root.GetProperty("OriginalMagicPowerPerGoldAfterPayment"), 50, 1, requireGenerated: true);
        Check(applied.First(row => Int(row, "Turn") > 0 && Int(row, "Gold") == 50), 50, 0, requireGenerated: true);
        CardCycleChecks.Native(root);
        SolverStageChecks.Native(path);
        Console.WriteLine("GOLD-MAGIC-POWER PASS: 100 gold / +1, payment to 50 / +1, next draw / +0; same-ID generated control excluded.");
    }

    private static void Check(FixtureValue frame, int gold, int amount, bool requireGenerated)
    {
        var permanent = frame.GetProperty("Permanent");
        var upgrade = permanent.GetProperty("Upgrades").EnumerateArray().Single();
        Require(Int(frame, "Gold") == gold && permanent.GetProperty("InDeck").GetBoolean() &&
            permanent.GetProperty("Eligible").GetBoolean() && permanent.GetProperty("TrackedByRelic").GetBoolean() &&
            upgrade.GetProperty("Id").GetString() == RelicGoldMagicPowerModel.UpgradeId && upgrade.GetProperty("Unique").GetBoolean() &&
            Int(upgrade, "Damage") == amount && Int(upgrade, "Heal") == amount,
            "Unexpected native permanent-card gold magic-power modifier.");
        if (!requireGenerated) return;
        var generated = frame.GetProperty("Generated");
        Require(generated.GetProperty("DataId").GetString() == permanent.GetProperty("DataId").GetString() &&
            !generated.GetProperty("InDeck").GetBoolean() && !generated.GetProperty("TrackedByRelic").GetBoolean() &&
            generated.GetProperty("Upgrades").GetArrayLength() == 0,
            "The same-definition generated card entered the permanent-deck callback.");
    }

    private static int Int(FixtureValue value, string key) => value.GetProperty(key).GetInt32();
    private static void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
}

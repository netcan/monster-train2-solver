using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class BeeziebubChecks
{
    internal static void Native(string path)
    {
        using var archive = ModelJson.ReadFixture(path);
        var root = archive.RootElement;
        var definition = root.GetProperty("OriginalBeeziebubDefinition");
        int tier = Int(definition, "Tier"), drawCount = Int(definition, "DrawCount");
        Require(root.GetProperty("OriginalBeeziebubPrepared").GetBoolean() &&
            root.GetProperty("GameModuleMvid").GetString() == "bf93f060-beda-46b1-b808-95ebf1e9a99d" &&
            definition.ContentEquals(root.GetProperty("OriginalBeeziebubDefinitionAfter")) &&
            tier is >= 1 and <= 3 && drawCount == (tier == 3 ? 5 : 3), "Missing unchanged original Beeziebub soul.");
        int spellId = Int(root.GetProperty("OriginalBeeziebubWitness"), "SpellId");
        var rows = root.GetProperty("OriginalBeeziebubCallbacks").EnumerateArray().ToArray();
        Require(rows.All(row => row.GetProperty("Completed").GetBoolean()), "Original Beeziebub callback failed to settle.");
        var cast = rows.Single(row => row.GetProperty("Kind").GetString() == "OnCast");
        int hp = Int(cast.GetProperty("Before"), "SaveHP");
        Require(hp > 0 && Int(cast.GetProperty("After"), "SaveHP") == hp - (int)Math.Floor(0.1f * hp),
            "Original cast did not match remaining Pyre health damage.");
        var draw = rows.Single(row => row.GetProperty("Kind").GetString() == "DrawOnPlay" && row.GetProperty("WasPlayed").GetBoolean());
        var oldIds = draw.GetProperty("Before").GetProperty("Hand").EnumerateArray().Select(card => Int(card, "Id")).ToHashSet();
        var drawn = draw.GetProperty("After").GetProperty("Hand").EnumerateArray().Where(card => !oldIds.Contains(Int(card, "Id"))).ToArray();
        Require(drawn.Length == drawCount && drawn.All(card => Int(card, "Id") != spellId), "Original played discard drew the wrong cards.");
        if (tier > 1)
        {
            string? upgradeId = definition.GetProperty("DrawUpgrade").GetProperty("Id").GetString();
            Require(drawn.Any(card => card.GetProperty("TempUpgradeIds").EnumerateArray().Any(id => id.GetString() == upgradeId)),
                "Original draw upgrade did not reach the recorded drawn cards.");
        }
        var purge = rows.Single(row => row.GetProperty("Kind").GetString() == "SelfPurge" && row.GetProperty("WasPlayed").GetBoolean());
        Require(purge.GetProperty("Before").GetProperty("InSavedDeck").GetBoolean() &&
            !purge.GetProperty("After").GetProperty("InSavedDeck").GetBoolean(), "Original SelfPurge did not remove the played spell from the native deck.");
        CardCycleChecks.Native(root);
        SolverStageChecks.Native(path);
        Console.WriteLine($"BEEZIEBUB PASS tier={tier} draw={drawCount}; native cast damage, draw upgrades and self-purge agree.");
    }

    private static int Int(FixtureValue value, string key) => value.GetProperty(key).GetInt32();
    private static void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
}

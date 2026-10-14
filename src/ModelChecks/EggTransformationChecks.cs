using MonsterTrain2Poju.Fixtures;

internal static class EggTransformationChecks
{
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue root = document.RootElement;
        Require(root.GetProperty("ModifierScenario").GetString() == "spawn-status-relics-egg-transformation" &&
            root.GetProperty("NativeWon").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                .All(name => root.GetProperty(name).GetInt32() == 0),
            "Egg transformation requires a complete native win with zero failures.");
        Require(!root.GetProperty("Actions")[0].GetProperty("Before").GetProperty("BattlePreviewEnabled").GetBoolean(),
            "Egg transformation should isolate native combat previews.");

        int births = 0;
        foreach (FixtureValue record in root.GetProperty("RelicSpawnStatuses").EnumerateArray())
        {
            int id = record.GetProperty("UnitId").GetInt32();
            FixtureValue before = record.GetProperty("Before").GetProperty("Units").EnumerateArray()
                .First(unit => unit.GetProperty("Id").GetInt32() == id);
            FixtureValue after = record.GetProperty("After").GetProperty("Units").EnumerateArray()
                .First(unit => unit.GetProperty("Id").GetInt32() == id);
            if (!after.GetProperty("AssetKey").GetString()!.StartsWith("TrainSteward", StringComparison.Ordinal)) continue;
            Require(Stacks(before, "hatch") == 5 && Stacks(after, "hatch") == 2 &&
                Stacks(after, "armor") - Stacks(before, "armor") == 35 &&
                after.GetProperty("Triggers").EnumerateArray().Any(trigger =>
                    trigger.GetProperty("Kind").GetString() == "OnRemoveHatch") &&
                after.GetProperty("Triggers").EnumerateArray().Any(trigger =>
                    trigger.GetProperty("Kind").GetString() == "OnHatched"),
                "Original Egg relic birth or hatch triggers differ.");
            births++;
        }
        Require(births >= 2, "Egg transformation lacks repeated original relic births.");

        FixtureValue[] hatches = root.GetProperty("TriggeredSummons").EnumerateArray()
            .Where(record => record.GetProperty("Kind").GetString() == "OnHatched").ToArray();
        Require(hatches.Length >= 2 && hatches.All(record =>
            record.GetProperty("Completed").GetBoolean() &&
            record.GetProperty("Error").ValueKind == FixtureKind.Null &&
            record.GetProperty("RuleBefore").GetProperty("CharacterId").GetString() ==
                "e2f7a12a-b33b-4dbe-b95b-73f39cdf3a70" &&
            record.GetProperty("ActorAfter").GetProperty("SacrificeCardId").GetInt32() ==
                record.GetProperty("SourceCardId").GetInt32() &&
            record.GetProperty("After").GetProperty("Units").EnumerateArray().Any(unit =>
                unit.GetProperty("AssetKey").GetString() == "MonsterKinhostPupa")),
            "Native OnHatched sacrifice and Pupa spawn evidence is incomplete.");

        BattleTurnChecks.Native(root);
        RoomCombatChecks.NativeRoomOnly(path);
        BattleSearchChecks.Native(root);
        Console.WriteLine($"NATIVE-EGG-TRANSFORMATION-CHECKS PASS: {births} Egg births, {hatches.Length} hatches, " +
            "original Pupa summon and independent battle/search replay.");
    }

    private static int Stacks(FixtureValue unit, string id) => unit.GetProperty("Statuses").EnumerateArray()
        .Where(status => status.GetProperty("Id").GetString() == id)
        .Select(status => status.GetProperty("Stacks").GetInt32()).DefaultIfEmpty(0).Single();

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

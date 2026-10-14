using MonsterTrain2Poju.Fixtures;

internal static class OriginalEggCardChecks
{
    private const string CardId = "057f7f77-df4b-4981-9e6d-533a6e667f39";
    private const string PupaId = "e2f7a12a-b33b-4dbe-b95b-73f39cdf3a70";

    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        FixtureValue root = document.RootElement;
        Require(root.GetProperty("ModifierScenario").GetString() == "original-egg-card" &&
            root.GetProperty("NativeWon").GetBoolean() &&
            new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" }
                .All(name => root.GetProperty(name).GetInt32() == 0),
            "Original Egg card requires a complete native win with zero failures.");

        FixtureValue action = root.GetProperty("Actions")[0];
        FixtureValue before = action.GetProperty("Before");
        FixtureValue context = before.GetProperty("Spawn").GetProperty("Train").GetProperty("Context");
        int cardInstanceId = action.GetProperty("Action").GetProperty("CardInstanceId").GetInt32();
        Require(!before.GetProperty("BattlePreviewEnabled").GetBoolean() &&
            action.GetProperty("Action").GetProperty("RoomIndex").GetInt32() == 2 &&
            context.GetProperty("Cards").GetProperty("Hand").EnumerateArray().Any(card =>
                card.GetProperty("InstanceId").GetInt32() == cardInstanceId &&
                card.GetProperty("DataId").GetString() == CardId) &&
            context.GetProperty("CardRegistry").EnumerateArray().Any(card =>
                card.GetProperty("InstanceId").GetInt32() == cardInstanceId &&
                card.GetProperty("DataId").GetString() == CardId),
            "First action must play unchanged Kinhost Vessel from hand in the upper room.");
        Require(context.GetProperty("RoomCapacities").EnumerateArray().Any(room =>
                room.GetProperty("RoomIndex").GetInt32() == 2 &&
                room.GetProperty("Corruption").GetInt32() == 8 &&
                room.GetProperty("MaxCorruption").GetInt32() == 8),
            "Original Egg card needs eight native Corruption for natural hatching.");

        FixtureValue egg = action.GetProperty("Actual").GetProperty("Spawn").GetProperty("Train")
            .GetProperty("Rooms")[2].GetProperty("Units").EnumerateArray()
            .Single(unit => unit.GetProperty("AssetKey").GetString() == "MonsterKinhostVessel");
        Require(egg.GetProperty("Statuses").EnumerateArray().Any(status =>
                status.GetProperty("Id").GetString() == "hatch" &&
                status.GetProperty("Stacks").GetInt32() == 8) &&
            egg.GetProperty("Triggers").EnumerateArray().Any(trigger =>
                trigger.GetProperty("Kind").GetString() == "OnRemoveHatch") &&
            egg.GetProperty("Triggers").EnumerateArray().Any(trigger =>
                trigger.GetProperty("Kind").GetString() == "OnHatched"),
            "Original card must spawn its unchanged Hatch 8 unit and triggers.");

        FixtureValue[] hatches = root.GetProperty("TriggeredSummons").EnumerateArray()
            .Where(record => record.GetProperty("Kind").GetString() == "OnHatched" &&
                record.GetProperty("SourceCardId").GetInt32() == cardInstanceId &&
                record.GetProperty("ActorBefore").GetProperty("AssetKey").GetString() == "MonsterKinhostVessel" &&
                record.GetProperty("RuleBefore").GetProperty("CharacterId").GetString() == PupaId &&
                record.GetProperty("ActorAfter").GetProperty("SacrificeCardId").GetInt32() == cardInstanceId &&
                record.GetProperty("After").GetProperty("Units").EnumerateArray().Any(unit =>
                    unit.GetProperty("AssetKey").GetString() == "MonsterKinhostPupa") &&
                record.GetProperty("Completed").GetBoolean() &&
                record.GetProperty("Error").ValueKind == FixtureKind.Null).ToArray();
        Require(hatches.Length >= 1, "Native OnHatched sacrifice and Pupa spawn witness is missing.");

        BattleTurnChecks.Native(root);
        RoomCombatChecks.NativeRoomOnly(path);
        BattleSearchChecks.Native(root);
        Console.WriteLine($"NATIVE-ORIGINAL-EGG-CARD-CHECKS PASS: {hatches.Length} natural hatches, " +
            "original card and independent battle/search replay.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

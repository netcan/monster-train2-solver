using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalDoubleFriendlyHealthChecks
{
    internal static void Native(FixtureValue witness)
    {
        FixtureValue owner = witness.GetProperty("Owner");
        Require(witness.GetProperty("Schema").GetInt32() == 1 &&
            witness.GetProperty("Boundary").GetString() == "OriginalDoubleFriendlyUnitHealthUnhealed" &&
            witness.GetProperty("GameVersion").GetString() == Game231FixtureCatalog.Version &&
            witness.GetProperty("GameModuleMvid").GetString() == Game231FixtureCatalog.Module &&
            owner.GetProperty("Id").GetString() == RelicPercentageStatModel.UnhealedOwnerId &&
            owner.GetProperty("AssetKey").GetString() == RelicPercentageStatModel.UnhealedOwnerKey &&
            owner.GetProperty("Type").GetString() == RelicPercentageStatModel.UnhealedEffect &&
            owner.GetProperty("Percent").GetInt32() == 100 && owner.GetProperty("Team").GetString() == "Monsters" &&
            owner.GetProperty("Trigger").GetString() == "OnSpawn" && owner.GetProperty("Conditions").GetInt32() == 0 &&
            !owner.GetProperty("DisallowInDeploymentPhase").GetBoolean() && !owner.GetProperty("CanApplyInPreviewMode").GetBoolean(),
            "Expected the pinned unhealed percentage health mutator.");

        FixtureValue birth = witness.GetProperty("Birth");
        FixtureValue birthBefore = birth.GetProperty("Before");
        FixtureValue birthAfter = birth.GetProperty("After");
        Require(birthBefore.GetProperty("Health").GetInt32() == 25 && birthAfter.GetProperty("Health").GetInt32() == 25 &&
            birthBefore.GetProperty("MaxHealth").GetInt32() == 25 && birthAfter.GetProperty("MaxHealth").GetInt32() == 50 &&
            birthBefore.GetProperty("TemporaryHealth").GetInt32() == 0 && birthAfter.GetProperty("TemporaryHealth").GetInt32() == 25 &&
            birthBefore.GetProperty("GameplayRng").ContentEquals(birthAfter.GetProperty("GameplayRng")),
            "Original birth callback must increase maximum and source-card health without healing or drawing RNG.");

        var before = witness.GetProperty("Before").Deserialize<BattleTurnState>()!;
        var action = witness.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext context = before.Spawn.Train.Context!;
        Require(context.Relics?.Count(RelicPercentageStatModel.IsOriginalUnhealed) == 1 &&
            context.FindCard(action.CardInstanceId)?.DataId == "d14a50f3-728d-43e1-87f0-ef1b013f6678" &&
            birth.GetProperty("CardId").GetInt32() == action.CardInstanceId, "Expected the captured original Train Steward and relic.");

        BattleActionResult played = BattleActionModel.PlayCard(before, action);
        Require(played.Supported, played.Reason ?? "The original Train Steward placement must be supported.");
        RecordedBattleChecks.RecordedBoundary(played.State!, witness.GetProperty("After"), terminal: false,
            "Original unhealed percentage health placement", trackPreviewPosition: true);

        BattleActionEnumeration candidates = BattleActionModel.EnumerateActions(before, action.CardInstanceId, includeAbilities: false);
        Require(!candidates.HasUnsupportedAttempts && candidates.SupportedActions.Any(candidate => candidate.RoomIndex == action.RoomIndex),
            "The original Train Steward placement must be available to the search.");
        Console.WriteLine("NATIVE-DOUBLE-FRIENDLY-HEALTH PASS: 25/25 to 25/50, +25 source-card health, exact paid successor and RNG.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

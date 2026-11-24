using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalLittleFadeEndlessChecks
{
    private const string Champion = "d2a5feba-4ecd-4d5d-a206-ec2ebfb96667";
    private const string Rank = "738b4ac0-d62b-4d79-8340-e6dc154b1b5f";
    private const string Upgrade = "f5b3eaea-c18e-4ffc-ab45-2bedb8da557e";

    internal static void Native(FixtureValue receipt)
    {
        Require(Number(receipt, "Schema") == 1 && Text(receipt, "Boundary") == "OriginalLittleFadeEndlessCost" &&
            Text(receipt, "GameVersion") == "2.3.1" && Text(receipt, "GameModuleMvid") == Game231FixtureCatalog.Module &&
            Text(receipt, "ChampionId") == Champion && Text(receipt, "RankId") == Rank && Text(receipt, "UpgradeId") == Upgrade,
            "Expected the original attached Little Fade rank from game 2.3.1.");
        var effect = receipt.GetProperty("AuthoredEffect").Deserialize<CardActionEffect>()!;
        Require(effect is { Type: "UnitUpgrade", Target: "Self", Lifetime: "TemporaryUntilEndOfBattle",
            Upgrade: { DataId: Upgrade, Stats: { Damage: 15, Health: 1, Cost: 0 } } },
            "The original OnDeath rank must add 15 attack and 1 health without a cost modifier.");

        var deploymentBefore = State("DeploymentBefore");
        var deployment = receipt.GetProperty("Deployment").Deserialize<PlayCardAction>()!;
        var deployed = BattleActionModel.PlayCard(deploymentBefore, deployment);
        Require(deployed.Supported, "The original Little Fade deployment was rejected: " + deployed.Reason);
        Compare(deployed.State!, State("DeploymentAfter"), "deployment");

        var before = State("Before");
        var after = State("After");
        int actor = Number(receipt, "ActorId"), card = Number(receipt, "CardInstanceId");
        var callback = receipt.GetProperty("DeathUpgrade");
        Require(card == deployment.CardInstanceId && Text(callback, "Trigger") == "OnDeath" &&
            Number(callback, "SourceActorId") == actor && callback.GetProperty("Targets").Deserialize<int[]>()!.SequenceEqual(new[] { actor }) &&
            callback.GetProperty("UpgradesBefore").GetArrayLength() == 0 &&
            callback.GetProperty("UpgradesAfter").Deserialize<string[]>()!.SequenceEqual(new[] { Upgrade }) &&
            Number(callback, "CostBefore") == Number(callback, "CostAfter") &&
            Number(receipt, "CostBeforeDeath") == Number(receipt, "CostAfterReturn") && receipt.GetProperty("SameCardInHand").GetBoolean() &&
            receipt.GetProperty("TemporaryUpgrades").Deserialize<string[]>()!.SequenceEqual(new[] { Upgrade }),
            "The native death callback or Endless return lost the original card, cost or upgrade.");
        Require(Number(callback, "AttackAfter") == Number(callback, "AttackBefore") + 15 &&
            Number(callback, "HealthAfter") == Number(callback, "HealthBefore") + 1 &&
            Number(receipt, "AttackAfterReturn") == Number(receipt, "AttackBeforeDeath") + 15 &&
            Number(receipt, "HealthAfterReturn") == Number(receipt, "HealthBeforeDeath") + 1,
            "The native attack and health increases must persist across the hand return.");
        Require(before.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == actor && unit.SpawnerCardId == card) &&
            after.Spawn.Train.Rooms.SelectMany(room => room.Units).All(unit => unit.Id != actor) &&
            after.Spawn.Train.Context!.Cards.Hand.Any(token => token.InstanceId == card && token.DataId == Champion),
            "The native decision boundary must show the actor replaced by its original card in hand.");

        var settled = BattleTurnModel.EndTurn(before);
        Require(settled.Supported && after.Spawn.Turn == before.Spawn.Turn + 1, "The original death turn was rejected: " + settled.UnsupportedReason);
        Compare(settled.State!, after, "death and Endless return");
        Console.WriteLine("ORIGINAL-LITTLE-FADE-ENDLESS PASS: exact deployment, OnDeath upgrade, Endless return and native RNG.");

        BattleTurnState State(string name) => receipt.GetProperty(name).Deserialize<BattleTurnState>()!;
    }

    private static void Compare(BattleTurnState predicted, BattleTurnState native, string action)
    {
        string? difference = ModelJson.Difference(BattleTurnChecks.Comparable(predicted), BattleTurnChecks.Comparable(native));
        Require(difference == null, "Little Fade " + action + " differs: " + difference);
    }

    private static string? Text(FixtureValue value, string name) => value.GetProperty(name).GetString();
    private static int Number(FixtureValue value, string name) => value.GetProperty(name).GetInt32();
    private static void Require(bool condition, string message) => TrueFinalBossChecks.Require(condition, message);
}

using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalLittleFadeEndlessChecks
{
    private const string Champion = "d2a5feba-4ecd-4d5d-a206-ec2ebfb96667";
    private const string Rank = "738b4ac0-d62b-4d79-8340-e6dc154b1b5f";
    private const string Upgrade = "f5b3eaea-c18e-4ffc-ab45-2bedb8da557e";
    private const string FriendlyRank = "eb4f67b9-f3ad-40db-9212-4ed647e17296";
    private const string FriendlyUpgrade = "42849eec-c299-40ce-a344-3fc490ee052b";

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

    internal static void NativeBuffFriendly(FixtureValue receipt)
    {
        Require(Number(receipt, "Schema") == 1 && Text(receipt, "Boundary") == "OriginalLittleFadeBuffFriendly" &&
            Text(receipt, "GameVersion") == "2.3.1" && Text(receipt, "GameModuleMvid") == Game231FixtureCatalog.Module &&
            Text(receipt, "ChampionId") == Champion && Text(receipt, "RankId") == FriendlyRank && Text(receipt, "UpgradeId") == FriendlyUpgrade,
            "Expected the installed original Little Fade friendly-buff rank.");
        int actor = Number(receipt, "ActorId"), recipient = Number(receipt, "RecipientId");
        var callbacks = receipt.GetProperty("Callbacks").EnumerateArray().ToArray();
        Require(callbacks.Select(row => Text(row, "Kind")).SequenceEqual(new[] { "OnDeathUpgrade", "OnDeathEphemeralGrant", "Ephemeral" }) &&
            callbacks.Select(row => Number(row, "Sequence")).SequenceEqual(new[] { 1, 2, 3 }) &&
            callbacks.All(row => row.GetProperty("Targets").Deserialize<int[]>()!.SequenceEqual(new[] { recipient })) &&
            callbacks.Take(2).All(row => Number(row, "SourceActorId") == actor),
            "Expected the two ordered native death callbacks and the recipient's Ephemeral callback.");
        var original = Unit(callbacks[0], "BeforeUnits");
        var upgraded = Unit(callbacks[0], "AfterUnits");
        Require(upgraded.BaseAttack == original.BaseAttack + 5 && upgraded.MaxHealth == original.MaxHealth + 5 &&
            Stacks(Unit(callbacks[1], "BeforeUnits")) == 0 && Stacks(Unit(callbacks[1], "AfterUnits")) == 2 &&
            Stacks(Unit(callbacks[2], "AfterUnits")) == 1,
            "The original same-floor upgrade, Ephemeral grant or countdown differs.");

        var placements = receipt.GetProperty("Deployments").EnumerateArray().ToArray();
        var turns = receipt.GetProperty("EndTurns").EnumerateArray().ToArray();
        Require(placements.Length == 2 && turns.Length == 2, "Expected both native deployments and two EndTurns.");
        BattleTurnState state = Read(placements[0], "Before");
        foreach (var placement in placements)
        {
            Compare(state, Read(placement, "Before"), "continuous deployment input");
            var played = BattleActionModel.PlayCard(state, placement.GetProperty("Input").Deserialize<PlayCardAction>()!);
            Require(played.Supported, "Original same-floor deployment was rejected: " + played.Reason);
            state = played.State!;
            Compare(state, Read(placement, "After"), "same-floor deployment");
        }
        foreach (var turn in turns)
        {
            Compare(state, Read(turn, "Before"), "continuous turn input");
            var ended = BattleTurnModel.EndTurn(state);
            Require(ended.Supported, "Original friendly-buff EndTurn was rejected: " + ended.UnsupportedReason);
            state = ended.State!;
            Compare(state, Read(turn, "After"), "friendly-buff EndTurn");
        }
        Require(state.Spawn.Train.Rooms.SelectMany(room => room.Units).All(unit => unit.Id != actor) &&
            state.Spawn.Train.Rooms.SelectMany(room => room.Units).Any(unit => unit.Id == recipient && Stacks(unit) == 1),
            "The native death and recipient countdown did not persist in the settled decision.");
        Console.WriteLine("ORIGINAL-LITTLE-FADE-BUFF-FRIENDLY PASS: two placements, two EndTurns, ordered callbacks and native RNG.");

        static BattleTurnState Read(FixtureValue node, string field) => node.GetProperty(field).Deserialize<BattleTurnState>()!;
        static CombatUnit Unit(FixtureValue node, string field) => node.GetProperty(field).Deserialize<CombatUnit[]>()!.Single();
        static int Stacks(CombatUnit unit) => unit.Status("ephemeral")?.Stacks ?? 0;
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

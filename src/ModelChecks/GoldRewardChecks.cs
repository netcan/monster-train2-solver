using MonsterTrain2Poju.Model;

internal static class GoldRewardChecks
{
    internal static void Run()
    {
        var rng = UnityRng.Seed(21);
        var context = new CombatContext(new CardCycleState([], [], [], rng, 0, []), rng, 10, 1, 10);
        var gold = new CombatEffect("CardEffectRewardGold", 2, 0, "HandPile", 0, [], false);
        var trigger = new CombatTrigger("PostCombat", false, false, false, 1, [gold]);
        var unit = new CombatUnit(1, "reward", CombatTeam.Player, 1, 5, 5, true, false, false, [], [trigger]);
        var root = new RoomCombatState(0, false, [unit], [], context);
        var actual = RoomCombatModel.Resolve(root);
        Require(actual.Supported && actual.State!.Context!.Gold == 15 && actual.Events.Single(e => e.Kind == "Gold").Amount == 5,
            "Small rewards were added before rounding.");
        Require(RoomCombatModel.Resolve(new(0, false, [unit], [], context, preview: true)).State!.Context!.Gold == 10,
            "A preview changed the gold balance.");
        Require(gold.Destination == "" && new CombatEffect("CardEffectAddBattleCard", 0, 0, "HandPile", 1, ["junk"], false).Destination == "HandPile",
            "Gold parameters were mistaken for pile destinations.");
        Require(GoldRewardModel.Adjust(25, roundIncrement: 10) == 20 && GoldRewardModel.Adjust(35, roundIncrement: 10) == 40 &&
            GoldRewardModel.Adjust(-2) == -2 && GoldRewardModel.Adjust(2, false) == 2,
            "Reward rounding mode or non-reward/negative handling differs.");
        Require(context.Gold == 10 && !trigger.HasTriggered, "Reward effects mutated the parent.");
        Console.WriteLine("GOLD-REWARD-CHECKS PASS: minimum rewards, ties to even, non-reward amounts, preview exclusion and pile semantics.");
    }
    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        int count = 0;
        foreach (var sample in document.RootElement.GetProperty("Samples").EnumerateArray())
        {
            Require(GoldRewardModel.Adjust(sample.GetProperty("Amount").GetInt32(), sample.GetProperty("IsReward").GetBoolean(),
                sample.GetProperty("RoundIncrement").GetInt32()) == sample.GetProperty("Actual").GetInt32(),
                "Gold reward native calibration differs.");
            count++;
        }
        Console.WriteLine($"NATIVE-GOLD-REWARD-CHECKS PASS: {count} native reward calculations.");
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}

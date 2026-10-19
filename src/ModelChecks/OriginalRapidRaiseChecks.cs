using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalRapidRaiseChecks
{
    private const string CardId = "5b88c130-70b3-42cc-9903-4d245b4f946f";
    private const string RaiseId = "8e99fcbd-5415-4bc9-9133-7131b0fb33a6";
    private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";

    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids) => OriginalRapidRaiseScenarioChooser.Choose(source, ids);

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 267 &&
            root.GetProperty("ModifierScenario").GetString() == "original-rapid-raise" &&
            root.GetProperty("OriginalRapidRaisePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Rapid Raise witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        int[] ids = root.GetProperty("OriginalRapidRaiseCardIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 4 && actions.Length == 2 &&
            actions[0].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[0] &&
            actions[1].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[1],
            "The native policy must summon its anchor before playing exactly one Rapid Raise.");

        FixtureValue entry = actions[1];
        BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction played = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext beforeContext = before.Spawn.Train.Context!;
        CombatContext afterContext = after.Spawn.Train.Context!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == CardId);
        CardTraitValue[] traits = beforeContext.FindCard(ids[1])!.MaskDescriptor!.BaseTraits.ToArray();
        CardPileState beforeExhausted = beforeContext.OtherPiles!.Single(pile => pile.Name == "Exhausted");
        CardPileState afterExhausted = afterContext.OtherPiles!.Single(pile => pile.Name == "Exhausted");
        CardUpgradeModifier[] raises = ids.Skip(2).Select(id => afterContext.FindCard(id)!.Temporary.Upgrades.Single(item => item.DataId == RaiseId)).ToArray();

        Require(rule.Cost == 0 && rule.Effects.Count == 1 && rule.Effects[0].Type == "Recursion" &&
            rule.Effects[0].Target == "Exhaust" && rule.Effects[0].Value == 2 &&
            rule.Effects[0].Recursion!.SelectionMode == "RandomToHand" && rule.Effects[0].Recursion.CardType == "Monster" &&
            rule.Effects[0].Upgrade!.DataId == RaiseId && rule.Effects[0].Upgrade.Stats.Damage == 5 &&
            rule.Effects[0].Upgrade.Stats.Health == 5 && rule.Effects[0].Upgrade.Stats.Cost == -99 &&
            played.RecursionSelections.Count == 0 && beforeExhausted.Cards.Count(card => card.DataId == StewardId) == 2 &&
            ids.Skip(2).All(id => beforeExhausted.Cards.Any(card => card.InstanceId == id)) &&
            traits.Select(trait => trait.RuntimeType).SequenceEqual(new[] { "CardTraitShowCardTargets", "CardTraitExhaustState" }) &&
            traits.All(trait => trait.DeclaredName == trait.RuntimeType && trait.DefinitionParamInt == 0 && trait.ParamInt == 0),
            "Rapid Raise must retain its unchanged random-two Monster-from-Exhaust rule, Raise upgrade and source traits.");
        Require(before.Energy == after.Energy && ids.Skip(2).All(id =>
                beforeExhausted.Cards.Any(card => card.InstanceId == id) && afterExhausted.Cards.All(card => card.InstanceId != id) &&
                afterContext.Cards.Hand.Any(card => card.InstanceId == id && card.DataId == StewardId)) &&
            raises.All(upgrade => upgrade.Stats.Damage == 5 && upgrade.Stats.Health == 5 && upgrade.Stats.Cost == -99 &&
                upgrade.Statuses.Any(status => status.Id == "ephemeral" && status.Stacks == 1)) &&
            afterExhausted.Cards.Any(card => card.InstanceId == ids[1] && card.DataId == CardId),
            "Rapid Raise did not return both Exhausted Monsters to hand, apply Raise and Ephemeral to each, and Exhaust itself without spending Ember.");

        RecordedBattleChecks.Native(root);
        System.Console.WriteLine("ORIGINAL-RAPID-RAISE PASS: unchanged zero-cost random recursion returns both Exhausted Monsters with Raise +5/+5, -99 cost and Ephemeral, then Exhausts itself.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new System.InvalidOperationException(message);
    }
}

internal static class OriginalRapidRaiseScenarioChooser
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        CombatContext context = source.Spawn.Train.Context!;
        if (context.Statistics!.Value(ids[0], "TimesPlayed", "ThisBattle") == 0)
        {
            var summon = new PlayCardAction(ids[0], 0, playerPosition: 0);
            if (BattleActionModel.PlayCard(source, summon).Supported) return summon;
        }
        CardPileState exhausted = context.OtherPiles!.Single(pile => pile.Name == "Exhausted");
        if (context.Statistics.Value(ids[1], "TimesPlayed", "ThisBattle") == 0 &&
            exhausted.Cards.Any(card => card.InstanceId == ids[2]) && exhausted.Cards.Any(card => card.InstanceId == ids[3]))
        {
            var cast = new PlayCardAction(ids[1], 0);
            if (BattleActionModel.PlayCard(source, cast).Supported) return cast;
        }
        return null;
    }
}

using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalStarterReformChecks
{
    private const string CardId = "fa394e19-4279-4e43-b0b3-75da045fc1eb";
    private const string RaiseId = "8e99fcbd-5415-4bc9-9133-7131b0fb33a6";
    private const string StewardId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";

    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids) => OriginalStarterReformScenarioChooser.Choose(source, ids);

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("Schema").GetInt32() == 266 &&
            root.GetProperty("ModifierScenario").GetString() == "original-starter-reform" &&
            root.GetProperty("OriginalStarterReformPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Starter Reform witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        int[] ids = root.GetProperty("OriginalStarterReformCardIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 3 && actions.Length == 2 &&
            actions[0].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[0] &&
            actions[1].GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[1],
            "The native policy must summon its anchor before playing exactly one Starter Reform.");

        FixtureValue entry = actions[1];
        BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction played = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext beforeContext = before.Spawn.Train.Context!;
        CombatContext afterContext = after.Spawn.Train.Context!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == CardId);
        CardTraitValue trait = beforeContext.FindCard(ids[1])!.MaskDescriptor!.BaseTraits.Single();
        CardInstanceState raised = afterContext.FindCard(ids[2])!;
        CardUpgradeModifier upgrade = raised.Temporary.Upgrades.Single(item => item.DataId == RaiseId);
        CardPileState beforeExhausted = beforeContext.OtherPiles!.Single(pile => pile.Name == "Exhausted");
        CardPileState afterExhausted = afterContext.OtherPiles!.Single(pile => pile.Name == "Exhausted");

        Require(rule.Cost == 1 && rule.Effects.Count == 1 && rule.Effects[0].Type == "Recursion" &&
            rule.Effects[0].Target == "Exhaust" && rule.Effects[0].Value == 1 &&
            rule.Effects[0].Recursion!.SelectionMode == "RandomToHand" && rule.Effects[0].Recursion.CardType == "Monster" &&
            rule.Effects[0].Upgrade!.DataId == RaiseId && rule.Effects[0].Upgrade.Stats.Damage == 5 &&
            rule.Effects[0].Upgrade.Stats.Health == 5 && rule.Effects[0].Upgrade.Stats.Cost == -99 &&
            played.RecursionSelections.Count == 0 && beforeExhausted.Cards.Count(card => card.DataId == StewardId) == 1 &&
            trait.RuntimeType == "CardTraitShowCardTargets" && trait.DeclaredName == trait.RuntimeType &&
            trait.DefinitionParamInt == 0 && trait.ParamInt == 0,
            "Starter Reform must retain its unchanged random Monster-from-Exhaust rule, Raise upgrade and UI-only source trait.");
        Require(before.Energy - after.Energy == 1 && beforeExhausted.Cards.Any(card => card.InstanceId == ids[2]) &&
            afterExhausted.Cards.All(card => card.InstanceId != ids[2]) &&
            afterContext.Cards.Hand.Any(card => card.InstanceId == ids[2] && card.DataId == StewardId) &&
            upgrade.Statuses.Any(status => status.Id == "ephemeral" && status.Stacks == 1) &&
            afterContext.Cards.Discard.Any(card => card.InstanceId == ids[1]),
            "Starter Reform did not return its sole Exhausted Monster to hand, pay one Ember, apply Raise and Ephemeral, and discard itself.");

        RecordedBattleChecks.Native(root);
        System.Console.WriteLine("ORIGINAL-STARTER-REFORM PASS: unchanged one-cost random Monster recursion returns the sole Exhaust target with Raise +5/+5, -99 cost and Ephemeral.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new System.InvalidOperationException(message);
    }
}

internal static class OriginalStarterReformScenarioChooser
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        CombatContext context = source.Spawn.Train.Context!;
        if (context.Statistics!.Value(ids[0], "TimesPlayed", "ThisBattle") == 0)
        {
            var summon = new PlayCardAction(ids[0], 0, playerPosition: 0);
            if (BattleActionModel.PlayCard(source, summon).Supported) return summon;
        }
        if (context.Statistics.Value(ids[1], "TimesPlayed", "ThisBattle") == 0 &&
            context.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == ids[2]))
        {
            var cast = new PlayCardAction(ids[1], 0);
            if (BattleActionModel.PlayCard(source, cast).Supported) return cast;
        }
        return null;
    }
}

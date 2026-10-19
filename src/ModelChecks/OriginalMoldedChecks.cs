using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalMoldedChecks
{
    private const string CardId = "dea99a2f-48c9-4246-ad44-01e84f46171e";
    private const string RaiseId = "8e99fcbd-5415-4bc9-9133-7131b0fb33a6";
    private const string MonsterId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
    private const string SpellControlId = "f954f70d-e83b-4c64-9827-89e5770a56ef";

    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids) => OriginalMoldedScenarioChooser.Choose(source, ids);

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalMoldedPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Molded witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        int[] ids = root.GetProperty("OriginalMoldedCardIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 4 && actions.Length >= 2, "Missing Molded, anchor or controlled Exhaust targets.");
        int anchorIndex = System.Array.FindIndex(actions, entry => entry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[0]);
        int moldedIndex = System.Array.FindIndex(actions, entry => entry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[1]);
        Require(anchorIndex >= 0 && moldedIndex > anchorIndex &&
            actions.Count(entry => entry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[1]) == 1,
            "The native policy must summon its anchor before playing exactly one Molded.");

        FixtureValue action = actions[moldedIndex];
        BattleTurnState before = action.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = action.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction played = action.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == CardId);
        CardActionEffect effect = rule.Effects.Single();
        CardInstanceState raised = after.Spawn.Train.Context!.FindCard(ids[2])!;
        CardUpgradeModifier upgrade = raised.Temporary.Upgrades.Single(item => item.DataId == RaiseId);

        Require(rule.Cost == 1 && effect.Type == "Recursion" && effect.Target == "Exhaust" && effect.Value == 1 &&
            effect.Recursion!.SelectionMode == "ChooseToHand" && effect.Recursion.CardType == "Monster" &&
            effect.Upgrade?.DataId == RaiseId && played.RecursionSelections.Count == 1 &&
            played.RecursionSelections[0].EffectIndex == 0 && played.RecursionSelections[0].CardInstanceIds.SequenceEqual(new[] { ids[2] }) &&
            before.Spawn.Train.Context.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == ids[2]) &&
            before.Spawn.Train.Context.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == ids[3] && card.DataId == SpellControlId) &&
            after.Spawn.Train.Context.Cards.Hand.Any(card => card.InstanceId == ids[2] && card.DataId == MonsterId) &&
            after.Spawn.Train.Context.Cards.Discard.Any(card => card.InstanceId == ids[1]) &&
            after.Spawn.Train.Context.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.All(card => card.InstanceId != ids[2]) &&
            after.Spawn.Train.Context.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == ids[3]) &&
            before.Energy - after.Energy == 1 && upgrade.AssetKey == "Raise" && upgrade.Stats.Damage == 5 && upgrade.Stats.Health == 5 &&
            upgrade.Statuses.Any(status => status.Id == "ephemeral" && status.Stacks == 1),
            "Molded must return only its selected Monster from Exhaust and apply the unchanged Raise upgrade.");

        RecordedBattleChecks.Native(root);
        System.Console.WriteLine("ORIGINAL-MOLDED PASS: unchanged one-cost play chooses the Exhausted Monster, leaves the Spell filter control, and applies Raise +5/+5 with Ephemeral.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new System.InvalidOperationException(message);
    }
}

internal static class OriginalMoldedScenarioChooser
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
            var cast = new PlayCardAction(ids[1], 0,
                recursionSelections: [new CardRecursionSelection(0, [ids[2]])]);
            if (BattleActionModel.PlayCard(source, cast).Supported) return cast;
        }
        return BattleActionModel.ChooseUnitSpellAndJunkPlay(source, card => card.InstanceId != ids[1]);
    }
}

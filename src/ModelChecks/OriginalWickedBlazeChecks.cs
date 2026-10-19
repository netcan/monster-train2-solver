using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalWickedBlazeChecks
{
    private const string CardId = "d930a4fe-1419-4c21-9ef2-fda4e25ee90b";
    private const string UpgradeId = "4e5ac8f8-0616-4135-8240-84b336019635";
    private const string MonsterId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";
    private const string SpellControlId = "f954f70d-e83b-4c64-9827-89e5770a56ef";

    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids) => OriginalWickedBlazeScenarioChooser.Choose(source, ids);

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalWickedBlazePrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Wicked Blaze witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        int[] ids = root.GetProperty("OriginalWickedBlazeCardIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 4 && actions.Length >= 2, "Missing Wicked Blaze, anchor or controlled Exhaust targets.");
        int anchorIndex = System.Array.FindIndex(actions, entry => entry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[0]);
        int spellIndex = System.Array.FindIndex(actions, entry => entry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[1]);
        Require(anchorIndex >= 0 && spellIndex > anchorIndex &&
            actions.Count(entry => entry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[1]) == 1,
            "The native policy must summon its anchor before playing exactly one Wicked Blaze.");

        FixtureValue action = actions[spellIndex];
        BattleTurnState before = action.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = action.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction played = action.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == CardId);
        CardActionEffect effect = rule.Effects.Single();
        CardInstanceState raised = after.Spawn.Train.Context!.FindCard(ids[2])!;
        CardUpgradeModifier upgrade = raised.Temporary.Upgrades.Single(item => item.DataId == UpgradeId);

        Require(rule.Cost == 2 && effect.Type == "Recursion" && effect.Target == "Exhaust" && effect.Value == 1 &&
            effect.Recursion!.SelectionMode == "ChooseToHand" && effect.Recursion.CardType == "Monster" &&
            effect.Upgrade?.DataId == UpgradeId && played.RecursionSelections.Count == 1 &&
            played.RecursionSelections[0].EffectIndex == 0 && played.RecursionSelections[0].CardInstanceIds.SequenceEqual(new[] { ids[2] }) &&
            before.Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == ids[2]) &&
            before.Spawn.Train.Context.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == ids[3] && card.DataId == SpellControlId) &&
            after.Spawn.Train.Context.Cards.Hand.Any(card => card.InstanceId == ids[2] && card.DataId == MonsterId) &&
            after.Spawn.Train.Context.Cards.Discard.Any(card => card.InstanceId == ids[1]) &&
            after.Spawn.Train.Context.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.All(card => card.InstanceId != ids[2]) &&
            after.Spawn.Train.Context.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == ids[3]) &&
            before.Energy - after.Energy == 2 && upgrade.AssetKey == "RaisePlusWickedBlaze" &&
            upgrade.Stats.Damage == 15 && upgrade.Stats.Health == 5 &&
            upgrade.Statuses.Any(status => status.Id == "ephemeral" && status.Stacks == 1),
            "Wicked Blaze must return only its selected Monster and apply the unchanged +15/+5 Ephemeral upgrade.");

        RecordedBattleChecks.Native(root);
        System.Console.WriteLine("ORIGINAL-WICKED-BLAZE PASS: unchanged two-cost play returns the selected Exhausted Monster and applies RaisePlusWickedBlaze +15/+5 with Ephemeral.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new System.InvalidOperationException(message);
    }
}

internal static class OriginalWickedBlazeScenarioChooser
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
        return BattleActionModel.ChooseUnitSpellAndJunkPlay(source,
            card => card.InstanceId != ids[1] && card.InstanceId != ids[2]);
    }
}

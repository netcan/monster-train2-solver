using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalBrokenMemoriesChecks
{
    private const string CardId = "f954f70d-e83b-4c64-9827-89e5770a56ef";
    private const string SpellTargetId = "dea99a2f-48c9-4246-ad44-01e84f46171e";
    private const string MonsterId = "d14a50f3-728d-43e1-87f0-ef1b013f6678";

    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids) => OriginalBrokenMemoriesScenarioChooser.Choose(source, ids);

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalBrokenMemoriesPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Broken Memories witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        int[] ids = root.GetProperty("OriginalBrokenMemoriesCardIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 4 && actions.Length >= 2, "Missing Broken Memories, anchor or controlled Exhaust targets.");
        int anchorIndex = System.Array.FindIndex(actions, entry => entry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[0]);
        int spellIndex = System.Array.FindIndex(actions, entry => entry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[1]);
        Require(anchorIndex >= 0 && spellIndex > anchorIndex &&
            actions.Count(entry => entry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[1]) == 1,
            "The native policy must summon its anchor before playing exactly one Broken Memories.");

        FixtureValue action = actions[spellIndex];
        BattleTurnState before = action.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = action.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction played = action.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == CardId);
        CardActionEffect effect = rule.Effects.Single();
        CardInstanceState source = before.Spawn.Train.Context!.FindCard(ids[1])!;

        Require(rule.Cost == 0 && effect.Type == "Recursion" && effect.Target == "Exhaust" && effect.Value == 0 &&
            effect.Recursion!.SelectionMode == "ChooseToDeck" && effect.Recursion.CardType == "Spell" && effect.Upgrade == null &&
            played.RecursionSelections.Count == 1 && played.RecursionSelections[0].EffectIndex == 0 &&
            played.RecursionSelections[0].CardInstanceIds.SequenceEqual(new[] { ids[2] }) && CardExhaustModel.HasConsume(source) &&
            before.Spawn.Train.Context!.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == ids[2] && card.DataId == SpellTargetId) &&
            before.Spawn.Train.Context.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == ids[3] && card.DataId == MonsterId) &&
            after.Spawn.Train.Context!.Cards.Draw.Any(card => card.InstanceId == ids[2] && card.DataId == SpellTargetId) &&
            after.Spawn.Train.Context.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == ids[1]) &&
            after.Spawn.Train.Context.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == ids[3]) &&
            after.Spawn.Train.Context.OtherPiles.Single(pile => pile.Name == "Exhausted").Cards.All(card => card.InstanceId != ids[2]) &&
            before.Energy == after.Energy,
            "Broken Memories must return only the selected Spell to Draw, leave the Monster filter control and exhaust itself for free.");

        RecordedBattleChecks.Native(root);
        System.Console.WriteLine("ORIGINAL-BROKEN-MEMORIES PASS: unchanged free play chooses the Exhausted Spell into Draw, leaves the Monster control and exhausts itself.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new System.InvalidOperationException(message);
    }
}

internal static class OriginalBrokenMemoriesScenarioChooser
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

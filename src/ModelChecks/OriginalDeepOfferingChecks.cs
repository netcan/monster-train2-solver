using System.Text.Json;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class OriginalDeepOfferingChecks
{
    private const string CardId = "37eaa51b-62cf-4cbc-971f-6d208fd07366";

    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids) => OriginalDeepOfferingScenarioChooser.Choose(source, ids);

    internal static void Native(FixtureValue root)
    {
        Require(root.GetProperty("OriginalDeepOfferingPrepared").GetBoolean() && root.GetProperty("NativeWon").GetBoolean() &&
            root.GetProperty("TerminalEffectsSettled").GetBoolean(), "Missing completed original Deep Offering witness.");
        foreach (string key in new[] { "CaptureFailures", "Mismatches", "Unsupported", "Pending" })
            Require(root.GetProperty(key).GetInt32() == 0, "Nonzero " + key);

        int[] ids = root.GetProperty("OriginalDeepOfferingCardIds").Deserialize<int[]>()!;
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(ids.Length == 3 && actions.Length >= 2, "Missing Deep Offering, anchor or discard control.");
        int anchorIndex = Array.FindIndex(actions, entry => entry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[0]);
        int offeringIndex = Array.FindIndex(actions, entry => entry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[1]);
        Require(anchorIndex >= 0 && offeringIndex > anchorIndex &&
            actions.Count(entry => entry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32() == ids[1]) == 1,
            "The native policy must summon its anchor before playing exactly one Deep Offering.");

        BattleTurnState before = actions[offeringIndex].GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState after = actions[offeringIndex].GetProperty("Actual").Deserialize<BattleTurnState>()!;
        CardPlayRule rule = before.PlayRules!.Cards.Single(card => card.DataId == CardId);
        CardInstanceState offering = before.Spawn.Train.Context!.FindCard(ids[1])!;
        int[] drawn = before.Spawn.Train.Context.Cards.Draw.Select(card => card.InstanceId)
            .Where(id => after.Spawn.Train.Context!.Cards.Hand.Any(card => card.InstanceId == id)).ToArray();
        Require(rule.Cost == 3 && rule.Effects.Select(effect => effect.Type).SequenceEqual(new[] { "DiscardHand", "Draw" }) &&
            rule.Effects[1].Value == 5 && CardExhaustModel.HasConsume(offering) &&
            before.Energy - after.Energy == 3,
            "Deep Offering's original cost, ordered discard/draw effects, five-card draw or Exhaust trait changed.");
        Require(before.Spawn.Train.Context.Cards.Hand.Any(card => card.InstanceId == ids[2]) &&
            after.Spawn.Train.Context!.Cards.Discard.Any(card => card.InstanceId == ids[2]) && drawn.Length == 5 &&
            after.Spawn.Train.Context.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == ids[1]),
            "Deep Offering must discard the held control, draw five cards and exhaust itself.");

        RecordedBattleChecks.Native(root);
        Console.WriteLine("ORIGINAL-DEEP-OFFERING PASS: original three-cost play discards a held control, draws five, exhausts, and replays the native battle.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

internal static class OriginalDeepOfferingScenarioChooser
{
    internal static PlayCardAction? Choose(BattleTurnState source, int[] ids)
    {
        CombatContext context = source.Spawn.Train.Context!;
        if (context.Statistics!.Value(ids[0], "TimesPlayed", "ThisBattle") == 0)
        {
            var summon = new PlayCardAction(ids[0], 0, playerPosition: 0);
            if (BattleActionModel.PlayCard(source, summon).Supported) return summon;
        }
        if (context.Statistics.Value(ids[1], "TimesPlayed", "ThisBattle") == 0)
        {
            var cast = new PlayCardAction(ids[1], 0);
            if (BattleActionModel.PlayCard(source, cast).Supported) return cast;
        }
        return BattleActionModel.ChooseUnitSpellAndJunkPlay(source);
    }
}

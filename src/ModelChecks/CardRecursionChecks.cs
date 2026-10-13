using System;
using System.Linq;
using MonsterTrain2Poju.Model;

internal static class CardRecursionChecks
{
    public static void Run()
    {
        var cards = new CardCycleState([new(50, "played")], [new(20, "drawn")], [], UnityRng.Seed(41), 2, []);
        CardToken[] exhausted = [new(1, "spell-a"), new(2, "unit"), new(3, "spell-b"), new(4, "spell-c")];
        CardPileState[] piles = [new("Exhausted", exhausted), new("Eaten", [new(5, "spell-d")])];
        var definitions = new BattlePlayRules([], [
            Rule("spell-a", "Spell"), Rule("unit", "Monster"), Rule("spell-b", "Spell"),
            Rule("spell-c", "Spell"), Rule("spell-d", "Spell")]);
        CardPileState[] standby = [new("Standby", exhausted,
            exhaustConditions: exhausted.Select(card => new CardExhaustStandbyCondition(card.InstanceId, card.InstanceId)).ToArray()),
            .. piles];
        CardActionEffect randomHand = Effect("RandomToHand", "Exhaust", "Spell", value: 2);
        IReadOnlyList<int> eligible = CardRecursionModel.EligibleCards(cards, standby, randomHand, definitions, 4, out string? error);
        Require(error == null && eligible.SequenceEqual([1, 3]), "Type filtering or played-card exclusion failed.");

        ShuffleResult<int> expected = cards.Rng.Shuffle(eligible);
        CardRecursionResult handResult = CardRecursionModel.Apply(cards, standby, cards.Rng, randomHand, definitions, 4, 4);
        Require(handResult.Supported && handResult.Cards!.Hand.Take(2).Select(card => card.InstanceId)
                .SequenceEqual(expected.Items) && handResult.Cards.Hand[2].InstanceId == 50 &&
            handResult.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Select(card => card.InstanceId)
                .SequenceEqual([2, 4]) && handResult.OtherPiles!.Single(pile => pile.Name == "Standby").Cards
                .Select(card => card.InstanceId).SequenceEqual([2, 4]) &&
            handResult.RestoredCardIds.OrderBy(id => id).SequenceEqual([1, 3]) &&
            handResult.BattleRng!.Value.Equals(expected.State) && cards.Hand.Count == 1 && standby[0].Cards.Count == 4,
            "Random-to-hand did not restore shared Exhaust/Standby membership, selection order, RNG, or parent state.");

        CardActionEffect randomDeck = Effect("RandomToDeck", "Eaten", "Invalid", value: 1);
        ShuffleResult<int> deckShuffle = cards.Rng.Shuffle([5]);
        CardRecursionResult deckResult = CardRecursionModel.Apply(cards, piles, cards.Rng, randomDeck, definitions, 50, 4);
        Require(deckResult.Supported && deckResult.Cards!.Draw.Select(card => card.InstanceId).SequenceEqual([20, 5]) &&
            deckResult.OtherPiles!.Single(pile => pile.Name == "Eaten").Cards.Count == 0 &&
            deckResult.BattleRng!.Value.Equals(deckShuffle.State),
            "Random-to-deck did not restore the selected card to the top of the draw pile.");

        CardActionEffect noOverflow = Effect("RandomToHand", "Exhaust", "Invalid", value: 2, disallowOverflow: true);
        CardRecursionResult fullHand = CardRecursionModel.Apply(cards, piles, cards.Rng, noOverflow, definitions, 50, 1);
        Require(fullHand.Supported && fullHand.Cards!.Hand.Count == 1 &&
            fullHand.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Count == exhausted.Length,
            "Disallow-overflow recursion moved cards while the hand was full.");

        CardActionEffect chosen = Effect("ChooseToHand", "Exhaust", "Invalid", value: 1);
        CardRecursionResult unsupportedMode = CardRecursionModel.Apply(cards, piles, cards.Rng, chosen, definitions, 50, 4);
        CardActionEffect filtered = new("Recursion", "Exhaust", 1, false, false, [],
            recursion: new CardRecursionRule("RandomToHand", "Invalid", "", false, false, true, false));
        CardActionEffect requiresHandSpace = new("Recursion", "Exhaust", 1, false, false, [],
            recursion: new CardRecursionRule("RandomToHand", "Invalid", "", true, false, false, false));
        CardRecursionResult unsupportedFilter = CardRecursionModel.Apply(cards, piles, cards.Rng, filtered, definitions, 50, 4);
        CardRecursionResult unsupportedHandSpace = CardRecursionModel.Apply(cards, piles, cards.Rng, requiresHandSpace, definitions, 50, 4);
        Require(!unsupportedMode.Supported && unsupportedMode.UnsupportedReason!.Contains("selection mode", StringComparison.Ordinal) &&
            !unsupportedFilter.Supported && unsupportedFilter.UnsupportedReason!.Contains("card-filter", StringComparison.Ordinal) &&
            !unsupportedHandSpace.Supported && unsupportedHandSpace.UnsupportedReason!.Contains("hand space", StringComparison.Ordinal),
            "Unsupported recursion modes, card filters, or hand-space conditions were silently accepted.");

        CardToken retained = new(6, "retained-spell");
        var keepStandby = new CardPileState("Standby", [retained], exhaustConditions:
            [new CardExhaustStandbyCondition(retained.InstanceId, null, keepInStandBy: true)]);
        var keepContext = new CombatContext(new([], [], [], cards.Rng, 0, []), cards.Rng, 0, 7, 4,
            otherPiles: [keepStandby, new CardPileState("Exhausted", [retained])]);
        CombatContext returned = CardExhaustModel.ReturnReady(keepContext);
        Require(CardPileModel.Validate(keepStandby) == null &&
            CardPileModel.ValidateMembership(keepContext.Cards, keepContext.OtherPiles!, keepContext.NextCardId) == null &&
            returned.OtherPiles!.Single(pile => pile.Name == "Standby").Cards.Single().InstanceId == retained.InstanceId &&
            returned.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Single().InstanceId == retained.InstanceId &&
            returned.Cards.Discard.Count == 0,
            "KeepInStandBy exhaustion must preserve both native pile aliases across return processing.");
        var malformedKeepStandby = new CardPileState("Standby", [retained], exhaustConditions:
            [new CardExhaustStandbyCondition(retained.InstanceId, retained.InstanceId, keepInStandBy: true)]);
        Require(CardPileModel.Validate(malformedKeepStandby) != null,
            "KeepInStandBy must not carry a Consume exhaustion reference.");

        Console.WriteLine("PASS card recursion pile selection, random restoration, and guarded modes");
    }

    private static CardPlayRule Rule(string dataId, string cardType) =>
        new(dataId, dataId, 0, "Null", "Discard", null, [], cardType: cardType);

    private static CardActionEffect Effect(string mode, string target, string cardType, int value, bool disallowOverflow = false) =>
        new("Recursion", target, value, false, false, [], recursion: new CardRecursionRule(
            mode, cardType, "", false, disallowOverflow, false, false));

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

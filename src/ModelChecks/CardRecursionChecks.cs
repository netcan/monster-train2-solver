using System;
using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CardRecursionChecks
{
    public static void SearchChoices(FixtureValue fixture, string selectionMode)
    {
        const string recursionCardId = "0e50f7bc-c7c4-46b5-98a4-33d32194d7b8";
        BattleTurnState? captured = null;
        foreach (FixtureValue action in fixture.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = action.GetProperty("Before").Deserialize<BattleTurnState>()!;
            if (before.PlayRules?.Cards.Any(rule => rule.DataId == recursionCardId) == true &&
                before.Spawn.Train.Context!.Cards.Hand.Any(card => card.DataId == recursionCardId) &&
                before.Spawn.Train.Context.OtherPiles?.Any(pile => pile.Name == "Exhausted" && pile.Cards.Count > 1) == true)
            {
                captured = before;
                break;
            }
        }
        Require(captured != null, "The native recursion fixture has no captured decision with eligible cards in hand and Exhaust.");

        BattleTurnState root = captured!;
        CardPlayRule original = root.PlayRules!.Cards.Single(rule => rule.DataId == recursionCardId);
        CardActionEffect[] effects = original.Effects.Select(effect => effect.Type != "Recursion" ? effect :
            new CardActionEffect(effect.Type, effect.Target, effect.Value, effect.AllowEnemy, effect.AllowPlayer,
                effect.Statuses, effect.Upgrade, effect.Lifetime, effect.Tests, effect.Range, effect.Filters,
                recursion: new CardRecursionRule(selectionMode, effect.Recursion!.CardType, effect.Recursion.Subtype,
                    false, false, effect.Recursion.HasCardFilter, effect.Recursion.HasUpgrade))).ToArray();
        var selectedRule = new CardPlayRule(original.DataId, original.AssetKey, original.Cost, original.Effect,
            original.Destination, original.SpawnUnit, original.ExternalInteractions, effects, original.UpgradeInteractions,
            original.HandDiscardInteractions, original.HandConsumeInteractions, original.CostType, original.Equipment,
            original.Ability, original.Summon, original.CardType, original.IsAnyAbility, original.CanPlayInEngineRoom);
        CardPlayRule[] cards = root.PlayRules.Cards.Select(rule => rule.DataId == recursionCardId ? selectedRule : rule).ToArray();
        var rules = new BattlePlayRules(root.PlayRules.Rooms, cards, root.PlayRules.StatusRules, root.PlayRules.UnitCopyCatalog);
        var choiceRoot = new BattleTurnState(root.Spawn, root.Energy, root.EnergyPerTurn, root.DrawPerTurn,
            root.ForgePoints, root.DragonsHoard, root.MoonPhase, root.RngStreams, root.OtherPiles,
            root.ExternalInteractions, rules, root.BattlePreviewEnabled, root.UiRngIsolated,
            root.CanonicalDecisionReferences, root.CanonicalPhysicalReferences, root.SelectedRoom,
            root.MoonPhaseTrackingActive);
        CardToken sourceCard = choiceRoot.Spawn.Train.Context!.Cards.Hand.Single(card => card.DataId == recursionCardId);
        CardActionEffect recursion = selectedRule.Effects.Single(effect => effect.Type == "Recursion");
        IReadOnlyList<int> eligible = CardRecursionModel.EligibleCards(choiceRoot.Spawn.Train.Context.Cards,
            choiceRoot.Spawn.Train.Context.OtherPiles, recursion, rules, sourceCard.InstanceId, out string? error);
        Require(error == null && eligible.Count > 1, "The native recursion fixture did not provide multiple eligible cards.");

        BattleActionEnumeration frontier = BattleActionModel.EnumerateActions(choiceRoot);
        SupportedActionCandidate[] branches = frontier.SupportedCandidates.Where(candidate =>
            candidate.Action.CardInstanceId == sourceCard.InstanceId && candidate.Action.RoomIndex == 0 &&
            candidate.Action.RecursionSelections.Count == 1).ToArray();
        int[] selectedIds = branches.Select(candidate => candidate.Action.RecursionSelections.Single().CardInstanceIds.Single()).ToArray();
        Require(selectedIds.SequenceEqual(eligible), "The search frontier did not emit one ordered action for every eligible recursion card.");
        foreach (SupportedActionCandidate branch in branches)
        {
            int selectedId = branch.Action.RecursionSelections.Single().CardInstanceIds.Single();
            CombatContext result = branch.Result.State!.Spawn.Train.Context!;
            bool destinationMatches = selectionMode == "ChooseToDeck"
                ? result.Cards.Draw.Any(card => card.InstanceId == selectedId) &&
                    !result.Cards.Hand.Any(card => card.InstanceId == selectedId)
                : selectionMode == "ChooseToHand" && result.Cards.Hand.Any(card => card.InstanceId == selectedId) &&
                    !result.Cards.Draw.Any(card => card.InstanceId == selectedId);
            Require(destinationMatches &&
                !result.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == selectedId) &&
                result.BattleRng.Equals(choiceRoot.Spawn.Train.Context.BattleRng),
                "A chosen recursion action did not restore exactly its selected card to the correct pile without consuming Battle RNG.");
        }
        Console.WriteLine($"CARD-RECURSION-CHOICE-CHECKS PASS: {selectionMode}, {branches.Length} distinct choice actions from a native decision state.");
    }

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

        CardActionEffect chosen = Effect("ChooseToHand", "Exhaust", "Spell", value: 0);
        IReadOnlyList<int> chooseEligible = CardRecursionModel.EligibleCards(cards, standby, chosen, definitions, 50, out error);
        IReadOnlyList<IReadOnlyList<int>> choices = CardRecursionModel.EnumerateChosenCards(chooseEligible);
        CardRecursionResult missingChoice = CardRecursionModel.Apply(cards, standby, cards.Rng, chosen, definitions, 50, 4);
        CardRecursionResult chosenHand = CardRecursionModel.Apply(cards, standby, cards.Rng, chosen, definitions, 50, 4, [3]);
        CardActionEffect chosenDeck = Effect("ChooseToDeck", "Eaten", "Invalid", value: 0);
        CardRecursionResult chosenToDeck = CardRecursionModel.Apply(cards, piles, cards.Rng, chosenDeck, definitions, 50, 4, [5]);
        CardActionEffect filtered = new("Recursion", "Exhaust", 1, false, false, [],
            recursion: new CardRecursionRule("RandomToHand", "Invalid", "", false, false, true, false));
        CardActionEffect requiresHandSpace = new("Recursion", "Exhaust", 1, false, false, [],
            recursion: new CardRecursionRule("RandomToHand", "Invalid", "", true, false, false, false));
        CardRecursionResult unsupportedFilter = CardRecursionModel.Apply(cards, piles, cards.Rng, filtered, definitions, 50, 4);
        CardRecursionResult unsupportedHandSpace = CardRecursionModel.Apply(cards, piles, cards.Rng, requiresHandSpace, definitions, 50, 4);
        Require(chooseEligible.SequenceEqual([1, 3, 4]) && choices.Select(choice => choice.Single()).SequenceEqual([1, 3, 4]) &&
            !missingChoice.Supported && missingChoice.UnsupportedReason!.Contains("selected card", StringComparison.Ordinal) &&
            chosenHand.Supported && chosenHand.Cards!.Hand.Select(card => card.InstanceId).SequenceEqual([3, 50]) &&
            chosenHand.BattleRng!.Value.Equals(cards.Rng) &&
            chosenHand.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Select(card => card.InstanceId).SequenceEqual([1, 2, 4]) &&
            chosenHand.OtherPiles!.Single(pile => pile.Name == "Standby").Cards.Select(card => card.InstanceId).SequenceEqual([1, 2, 4]) &&
            chosenToDeck.Supported && chosenToDeck.Cards!.Draw.Select(card => card.InstanceId).SequenceEqual([20, 5]) &&
            chosenToDeck.BattleRng!.Value.Equals(cards.Rng),
            "Choose recursion did not enumerate eligible single-card branches, restore the selected card, or preserve RNG.");
        Require(
            !unsupportedFilter.Supported && unsupportedFilter.UnsupportedReason!.Contains("card-filter", StringComparison.Ordinal) &&
            !unsupportedHandSpace.Supported && unsupportedHandSpace.UnsupportedReason!.Contains("hand space", StringComparison.Ordinal),
            "Unsupported recursion filters or random hand-space conditions were silently accepted.");

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

        Console.WriteLine("PASS random and chosen card recursion, candidate branches, pile restoration, and guarded modes");
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

using System;
using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class CardRecursionChecks
{
    public static void SearchChoices(FixtureValue fixture, string selectionMode, int expectedCandidateCount = 6)
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
                    false, false, effect.Recursion.HasCardFilter, effect.Recursion.HasUpgrade,
                    effect.Recursion.CardFilter))).ToArray();
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
            choiceRoot.Spawn.Train.Context.OtherPiles, recursion, rules, sourceCard.InstanceId, out string? error,
            choiceRoot.Spawn.Train.Context.CardRegistry);
        Require(error == null && eligible.Count == expectedCandidateCount,
            $"The native recursion fixture exposed {eligible.Count} eligible cards; expected {expectedCandidateCount}.");

        BattleActionEnumeration frontier = BattleActionModel.EnumerateActions(choiceRoot);
        SupportedActionCandidate[] branches = frontier.SupportedCandidates.Where(candidate =>
            candidate.Action.CardInstanceId == sourceCard.InstanceId && candidate.Action.RoomIndex == 0 &&
            candidate.Action.RecursionSelections.Count == 1).ToArray();
        int[] selectedIds = branches.Select(candidate => candidate.Action.RecursionSelections.Single().CardInstanceIds.Single()).ToArray();
        Require(selectedIds.SequenceEqual(eligible) && branches.Length == expectedCandidateCount,
            "The search frontier did not emit one ordered action for every filter-eligible recursion card.");
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
            if (recursion.Upgrade != null)
            {
                int[] upgradedIds = result.CardRegistry!.Where(card => card.Temporary.Upgrades.Any(upgrade =>
                    upgrade.DataId == recursion.Upgrade.DataId && upgrade.Stats.Damage == recursion.Upgrade.Stats.Damage))
                    .Select(card => card.InstanceId).ToArray();
                Require(upgradedIds.SequenceEqual([selectedId]),
                    "A chosen recursion upgrade did not stay on exactly the card selected by this search branch.");
            }
        }
        Console.WriteLine($"CARD-RECURSION-CHOICE-CHECKS PASS: {selectionMode}, {branches.Length} distinct choice actions from a native decision state.");
    }

    public static void SearchUpgrade(FixtureValue fixture)
    {
        string scenario = fixture.GetProperty("ModifierScenario").GetString()!;
        bool filteredChoice = scenario == "card-effect-recursion-upgrade-filter-choice";
        bool chooseToDeck = scenario is "card-effect-recursion-upgrade-choice" or "card-effect-recursion-upgrade-filter-choice";
        bool toHand = scenario == "card-effect-recursion-upgrade-hand";
        int expectedSchema = filteredChoice ? 152 : chooseToDeck ? 151 : toHand ? 150 : 149;
        Require(fixture.GetProperty("Schema").GetInt32() == expectedSchema &&
            scenario == (filteredChoice ? "card-effect-recursion-upgrade-filter-choice" :
                chooseToDeck ? "card-effect-recursion-upgrade-choice" :
                toHand ? "card-effect-recursion-upgrade-hand" : "card-effect-recursion-upgrade"),
            "The fixture is not the native recursion-upgrade scenario.");
        const string recursionCardId = "0e50f7bc-c7c4-46b5-98a4-33d32194d7b8";
        const string upgradeId = "f2f8c03b-327d-4f3d-8cc1-f9b2a20e4ff6";
        FixtureValue? evidence = null;
        foreach (FixtureValue action in fixture.GetProperty("Actions").EnumerateArray())
        {
            BattleTurnState before = action.GetProperty("Before").Deserialize<BattleTurnState>()!;
            CardToken? played = before.Spawn.Train.Context!.Cards.Hand.FirstOrDefault(card =>
                card.InstanceId == action.GetProperty("Action").GetProperty("CardInstanceId").GetInt32());
            CardPlayRule? definition = played == null ? null : before.PlayRules?.Cards.FirstOrDefault(rule => rule.DataId == played.DataId);
            if (played?.DataId == recursionCardId && definition?.Effects.Any(effect =>
                effect.Type == "Recursion" && effect.Upgrade?.DataId == upgradeId) == true)
            {
                evidence = action;
                break;
            }
        }
        Require(evidence != null, "The native recursion-upgrade fixture has no matching WurmkinEtchings action.");

        FixtureValue entry = evidence!.Value;
        BattleTurnState root = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        CardToken sourceToken = root.Spawn.Train.Context!.Cards.Hand.Single(card =>
            card.DataId == recursionCardId && card.InstanceId == entry.GetProperty("Action").GetProperty("CardInstanceId").GetInt32());
        CardActionEffect recursionEffect = root.PlayRules!.Cards.Single(rule => rule.DataId == sourceToken.DataId)
            .Effects.Single(effect => effect.Type == "Recursion");
        Require(recursionEffect.Recursion!.SelectionMode == (chooseToDeck ? "ChooseToDeck" :
                toHand ? "RandomToHandWithUpgrades" : "RandomToDeck") &&
            recursionEffect.Upgrade?.DataId == upgradeId && recursionEffect.Recursion.RequiresRoomInHand == toHand &&
            recursionEffect.Recursion.DisallowOverflowToDrawPile == toHand,
            "The captured action does not contain the expected native recursion-upgrade mode.");
        Require(!toHand || root.Spawn.Train.Context!.Cards.Hand.Count == root.Spawn.Train.Context.MaxHandSize,
            "The native upgraded-recursion overflow case did not start with a full hand.");
        PlayCardAction actionToPlay = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
        BattleActionResult predicted = BattleActionModel.PlayCard(root, actionToPlay);
        BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        Require(predicted.Supported, "Recursion with a simple temporary upgrade was rejected: " + predicted.Reason);
        string? difference = ModelJson.Difference(BattleTurnChecks.Comparable(predicted.State!),
            BattleTurnChecks.Comparable(actual));
        Require(difference == null, "Recursion-upgrade prediction differs from native action: " + difference);

        string spellId = "fb0683b1-20ac-4dd2-b580-14b502e182f4";
        int[] candidates = entry.GetProperty("Before").GetProperty("OtherPiles").EnumerateArray()
            .Where(pile => pile.GetProperty("Name").GetString() == "Exhausted")
            .SelectMany(pile => pile.GetProperty("Cards").EnumerateArray())
            .Where(card => card.GetProperty("DataId").GetString() == spellId)
            .Select(card => card.GetProperty("InstanceId").GetInt32()).ToArray();
        int[] handIds = actual.Spawn.Train.Context!.Cards.Hand.Select(card => card.InstanceId).ToArray();
        int[] drawIds = actual.Spawn.Train.Context!.Cards.Draw.Select(card => card.InstanceId).ToArray();
        int[] returnedHand = candidates.Where(handIds.Contains).ToArray();
        int[] returnedDraw = candidates.Where(drawIds.Contains).ToArray();
        int[] returned = toHand ? returnedHand.Concat(returnedDraw).Distinct().ToArray() : returnedDraw;
        int expectedReturned = toHand ? 2 : 1;
        CardInstanceState[] upgraded = candidates.Select(id => actual.Spawn.Train.Context!.FindCard(id)!)
            .Where(card => card.Temporary.Upgrades.Any(upgrade => upgrade.DataId == upgradeId && upgrade.Stats.Damage == 3)).ToArray();
        Require(returned.Length == expectedReturned &&
            (toHand ? returnedHand.Length == 1 && returnedDraw.Length == 1 : returnedHand.Length == 0 && returnedDraw.Length == 1) &&
            upgraded.Length == expectedReturned && upgraded.Select(card => card.InstanceId).OrderBy(id => id).SequenceEqual(returned.OrderBy(id => id)) &&
            candidates.Where(id => !returned.Contains(id)).All(id => actual.Spawn.Train.Context!.FindCard(id)!.Temporary.Upgrades
                .All(upgrade => upgrade.DataId != upgradeId)) &&
            (!chooseToDeck || actionToPlay.RecursionSelections.Count == 1 &&
                actionToPlay.RecursionSelections[0].CardInstanceIds.SequenceEqual([returned[0]])),
            "Native recursion did not upgrade exactly the returned Spell or match the chosen card identity.");
        if (chooseToDeck) SearchChoices(fixture, "ChooseToDeck", expectedCandidateCount: filteredChoice ? 5 : 6);
        Console.WriteLine($"CARD-RECURSION-UPGRADE-CHECKS PASS: native {(chooseToDeck ? "chosen" : "random")} selection, {(toHand ? "hand plus draw-pile overflow" : "draw pile")} destination, temporary +3 Damage, returned-card identity, and untouched candidates.");
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
        var spellMask = new CardUpgradeMaskRule(cardType: "Spell");
        CardActionEffect filteredChoice = new("Recursion", "Exhaust", 1, false, false, [],
            recursion: new CardRecursionRule("ChooseToDeck", "Invalid", "", false, false, true, false, spellMask));
        CardInstanceState[] maskRegistry = exhausted.Select(card =>
        {
            bool monster = card.DataId == "unit";
            var descriptor = new CardMaskDescriptor(new CardMaskDefinition(card.DataId, monster ? "Monster" : "Spell",
                "Common", monster, monster, monster, [], [], [], [], null, false, monster ? 1 : 0, 0, false, false, false),
                1, [], []);
            return new CardInstanceState(card.InstanceId, card.DataId, CardModifiers.Empty(), CardModifiers.Empty(),
                0, 0, 0, [], maskDescriptor: descriptor);
        }).ToArray();
        IReadOnlyList<int> filteredEligible = CardRecursionModel.EligibleCards(cards, piles, filteredChoice,
            definitions, 50, out error, maskRegistry);
        CardRecursionResult filteredDeck = CardRecursionModel.Apply(cards, piles, cards.Rng, filteredChoice,
            definitions, 50, 4, [3], maskRegistry);
        IReadOnlyList<int> missingMaskMetadata = CardRecursionModel.EligibleCards(cards, piles, filteredChoice,
            definitions, 50, out string? missingMaskError);
        CardActionEffect filtered = new("Recursion", "Exhaust", 1, false, false, [],
            recursion: new CardRecursionRule("RandomToHand", "Invalid", "", false, false, true, false));
        CardActionEffect requiresHandSpace = new("Recursion", "Exhaust", 1, false, false, [],
            recursion: new CardRecursionRule("RandomToHand", "Invalid", "", true, false, false, false));
        CardRecursionResult unsupportedFilter = CardRecursionModel.Apply(cards, piles, cards.Rng, filtered, definitions, 50, 4);
        ShuffleResult<int> requiresHandSpaceShuffle = cards.Rng.Shuffle([1, 2, 3, 4]);
        CardRecursionResult requiresHandSpaceResult = CardRecursionModel.Apply(cards, piles, cards.Rng,
            requiresHandSpace, definitions, 50, 4);
        var recursionUpgrade = new CardUpgradeModifier("recursion-damage", "RecursionDamage",
            new CardStatModifier(damage: 3), [], false, false, false, 0, 0, [],
            maskMetadata: new CardUpgradeMaskMetadata([], false, false, false, false));
        CardActionEffect filteredChoiceUpgrade = new("Recursion", "Exhaust", 1, false, false, [], recursionUpgrade,
            recursion: new CardRecursionRule("ChooseToDeck", "Invalid", "", false, false, true, true, spellMask));
        CardActionEffect randomDeckUpgrade = new("Recursion", "Exhaust", 1, false, false, [], recursionUpgrade,
            recursion: new CardRecursionRule("RandomToDeck", "Spell", "", false, false, false, true));
        CardActionEffect chosenDeckUpgrade = new("Recursion", "Exhaust", 1, false, false, [], recursionUpgrade,
            recursion: new CardRecursionRule("ChooseToDeck", "Spell", "", false, false, false, true));
        CardInstanceState[] upgradeRegistry = exhausted.Select(card => CardInstanceState.Empty(card.InstanceId, card.DataId)).ToArray();
        CardRecursionResult filteredUpgradedDeck = CardRecursionModel.Apply(cards, piles, cards.Rng,
            filteredChoiceUpgrade, definitions, 50, 4, [3], maskRegistry);
        ShuffleResult<int> expectedUpgradeChoice = cards.Rng.Shuffle([1, 3, 4]);
        CardRecursionResult upgradedDeck = CardRecursionModel.Apply(cards, piles, cards.Rng, randomDeckUpgrade,
            definitions, 50, 4, cardRegistry: upgradeRegistry);
        CardRecursionResult chosenUpgradedDeck = CardRecursionModel.Apply(cards, piles, cards.Rng, chosenDeckUpgrade,
            definitions, 50, 4, [3], upgradeRegistry);
        CardInstanceState upgradedInstance = upgradedDeck.ChangedCards.Single();
        CardActionEffect randomHandWithUpgrade = new("Recursion", "Exhaust", 1, false, false, [], recursionUpgrade,
            recursion: new CardRecursionRule("RandomToHandWithUpgrades", "Spell", "", false, false, false, true));
        CardRecursionResult upgradedHand = CardRecursionModel.Apply(cards, standby, cards.Rng, randomHandWithUpgrade,
            definitions, 50, 4, cardRegistry: upgradeRegistry);
        CardActionEffect upgradedHandNoOverflow = new("Recursion", "Exhaust", 1, false, false, [], recursionUpgrade,
            recursion: new CardRecursionRule("RandomToHandWithUpgrades", "Spell", "", true, true, false, true));
        ShuffleResult<int> expectedFullHandUpgrade = cards.Rng.Shuffle([1, 3, 4]);
        CardRecursionResult upgradedFullHand = CardRecursionModel.Apply(cards, standby, cards.Rng,
            upgradedHandNoOverflow, definitions, 50, 1, cardRegistry: upgradeRegistry);
        CardRecursionResult missingUpgradeRegistry = CardRecursionModel.Apply(cards, piles, cards.Rng, randomDeckUpgrade,
            definitions, 50, 4);
        CardRecursionResult emptyUpgradeTarget = CardRecursionModel.Apply(cards,
            [new CardPileState("Exhausted", []), new CardPileState("Eaten", [new CardToken(5, "spell-d")])],
            cards.Rng, randomDeckUpgrade, definitions, 50, 4);
        Require(chooseEligible.SequenceEqual([1, 3, 4]) && choices.Select(choice => choice.Single()).SequenceEqual([1, 3, 4]) &&
            !missingChoice.Supported && missingChoice.UnsupportedReason!.Contains("selected card", StringComparison.Ordinal) &&
            chosenHand.Supported && chosenHand.Cards!.Hand.Select(card => card.InstanceId).SequenceEqual([3, 50]) &&
            chosenHand.BattleRng!.Value.Equals(cards.Rng) &&
            chosenHand.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Select(card => card.InstanceId).SequenceEqual([1, 2, 4]) &&
            chosenHand.OtherPiles!.Single(pile => pile.Name == "Standby").Cards.Select(card => card.InstanceId).SequenceEqual([1, 2, 4]) &&
            chosenToDeck.Supported && chosenToDeck.Cards!.Draw.Select(card => card.InstanceId).SequenceEqual([20, 5]) &&
            chosenToDeck.BattleRng!.Value.Equals(cards.Rng) &&
            filteredEligible.SequenceEqual([1, 3, 4]) && filteredDeck.Supported &&
            filteredDeck.Cards!.Draw.Select(card => card.InstanceId).SequenceEqual([20, 3]) &&
            filteredDeck.OtherPiles!.Single(pile => pile.Name == "Exhausted").Cards.Select(card => card.InstanceId)
                .SequenceEqual([1, 2, 4]) && filteredDeck.BattleRng!.Value.Equals(cards.Rng) &&
            filteredUpgradedDeck.Supported && filteredUpgradedDeck.RestoredCardIds.SequenceEqual([3]) &&
            filteredUpgradedDeck.ChangedCards.Single().InstanceId == 3 &&
            filteredUpgradedDeck.ChangedCards.Single().Temporary.Upgrades.Single().Stats.Damage == 3 &&
            filteredUpgradedDeck.BattleRng!.Value.Equals(cards.Rng) &&
            missingMaskMetadata.Count == 0 && missingMaskError?.Contains("mask metadata", StringComparison.Ordinal) == true,
            "Choose recursion did not enumerate eligible single-card branches, restore the selected card, or preserve RNG.");
        Require(
            !unsupportedFilter.Supported && unsupportedFilter.UnsupportedReason!.Contains("card-filter", StringComparison.Ordinal) &&
            requiresHandSpaceResult.Supported && requiresHandSpaceResult.RestoredCardIds.SequenceEqual([requiresHandSpaceShuffle.Items[0]]) &&
            requiresHandSpaceResult.Cards!.Hand[0].InstanceId == requiresHandSpaceShuffle.Items[0] &&
            requiresHandSpaceResult.BattleRng!.Value.Equals(requiresHandSpaceShuffle.State),
            "Random-to-hand recursion with the native RequiresRoomInHand gate did not restore its selected card.");
        Require(upgradedDeck.Supported && upgradedDeck.RestoredCardIds.SequenceEqual([expectedUpgradeChoice.Items[0]]) &&
            upgradedDeck.Cards!.Draw.Select(card => card.InstanceId).SequenceEqual([20, expectedUpgradeChoice.Items[0]]) &&
            upgradedInstance.InstanceId == expectedUpgradeChoice.Items[0] && upgradedInstance.Temporary.Upgrades.Single().Stats.Damage == 3 &&
            upgradedHand.Supported && upgradedHand.Cards!.Hand.Select(card => card.InstanceId)
                .SequenceEqual([expectedUpgradeChoice.Items[0], 50]) && upgradedHand.Cards.Draw.Single().InstanceId == 20 &&
            upgradedHand.ChangedCards.Single().InstanceId == expectedUpgradeChoice.Items[0] &&
            upgradedFullHand.Supported && upgradedFullHand.RestoredCardIds.SequenceEqual([expectedFullHandUpgrade.Items[0]]) &&
            upgradedFullHand.Cards!.Hand.Select(card => card.InstanceId).SequenceEqual([50]) &&
            upgradedFullHand.Cards.Draw.Select(card => card.InstanceId).SequenceEqual([20, expectedFullHandUpgrade.Items[0]]) &&
            upgradedFullHand.ChangedCards.Single().InstanceId == expectedFullHandUpgrade.Items[0] &&
            upgradedFullHand.ChangedCards.Single().Temporary.Upgrades.Single().Stats.Damage == 3 &&
            chosenUpgradedDeck.Supported && chosenUpgradedDeck.RestoredCardIds.SequenceEqual([3]) &&
            chosenUpgradedDeck.Cards!.Draw.Select(card => card.InstanceId).SequenceEqual([20, 3]) &&
            chosenUpgradedDeck.ChangedCards.Single().InstanceId == 3 &&
            chosenUpgradedDeck.ChangedCards.Single().Temporary.Upgrades.Single().Stats.Damage == 3 &&
            chosenUpgradedDeck.BattleRng!.Value.Equals(cards.Rng) &&
            upgradedInstance.Permanent.Upgrades.Count == 0 && upgradeRegistry.All(card => card.Temporary.Upgrades.Count == 0) &&
            !missingUpgradeRegistry.Supported && missingUpgradeRegistry.UnsupportedReason!.Contains("card registry", StringComparison.Ordinal) &&
            emptyUpgradeTarget.Supported && emptyUpgradeTarget.RestoredCardIds.Count == 0 && emptyUpgradeTarget.ChangedCards.Count == 0,
            "Recursion did not add its temporary upgrade only to the selected returned branch card.");

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

        Console.WriteLine("PASS random and chosen card recursion, candidate branches, pile restoration, temporary upgrades, and RequiresRoomInHand");
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

using System;
using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ChooseDiscardChecks
{
    private const string ScrapId = "bc4cd099-93df-4771-ae72-4984fc50ab9f";
    private const string FoundryLineId = "26651f53-95d0-4802-b710-51d23bf89bbd";
    private const string ScrapMetalId = "d4f0ff05-1fe1-483c-adda-928a8b05808c";
    private const string OneCostId = "fb0683b1-20ac-4dd2-b580-14b502e182f4";
    private const string TwoCostId = "25f84315-da10-4915-aa5d-e3f1a38124b4";

    public static void Native(FixtureValue root)
    {
        Require(root.GetProperty("ModifierScenario").GetString() == "card-effect-choose-discard",
            "This fixture did not run the original choose-discard scenario.");
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(actions.Length > 0 && actions[0].GetProperty("ScenarioAction").GetBoolean(),
            "The original StarterScrap was not the first native action.");
        FixtureValue entry = actions[0];
        BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext input = before.Spawn.Train.Context!;
        CombatContext output = actual.Spawn.Train.Context!;
        CardToken source = input.Cards.Hand.Single(card => card.InstanceId == action.CardInstanceId);
        Require(source.DataId == ScrapId && action.RecursionSelections.Count == 1 &&
            action.RecursionSelections[0].EffectIndex == 0 &&
            action.RecursionSelections[0].CardInstanceIds.Count == 1,
            "The original StarterScrap choice was not captured by card identity.");
        int selectedId = action.RecursionSelections[0].CardInstanceIds[0];
        Require(input.Cards.Hand.Single(card => card.InstanceId == selectedId).DataId == TwoCostId &&
            input.Cards.Hand.Any(card => card.DataId == OneCostId),
            "The native choice did not distinguish the one- and two-cost candidates.");
        CardActionEffect effect = before.PlayRules!.Cards.Single(card => card.DataId == ScrapId).Effects.Single();
        Require(effect.Type == "ChooseDiscard" && effect.Value == 3 && effect.ChooseDiscard?.Condition == 5 &&
            effect.ChooseDiscard.GainEmberForCost && !effect.ChooseDiscard.HasReplacementPool,
            "The captured card differs from the original smelt definition.");
        Require(input.QueryFrame?.ForgePoints == 0 && output.QueryFrame?.ForgePoints == 2 &&
            input.QueryFrame.Energy == 4 && output.QueryFrame?.Energy == 5 &&
            output.OtherPiles?.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == selectedId) == true &&
            output.Cards.Hand.Any(card => card.InstanceId == selectedId) == false,
            "The native smelt did not grant exactly two Forge Points and two Ember before consuming the chosen card.");
        BattleActionResult replay = BattleActionModel.PlayCard(before, action);
        Require(replay.Supported && BattleTurnChecks.Comparable(replay.State!) == BattleTurnChecks.Comparable(actual),
            "The recorded smelt choice does not replay to the native action state: " + replay.Reason);

        int[] eligible = CardChooseDiscardModel.EligibleCards(input, effect, action.CardInstanceId, out string? error).ToArray();
        Require(error == null && eligible.Length >= 2 && eligible.Contains(selectedId),
            "The model did not retain multiple legal hand choices.");
        SupportedActionCandidate[] choices = BattleActionModel.EnumerateActions(before).SupportedCandidates
            .Where(candidate => candidate.Action.CardInstanceId == action.CardInstanceId &&
                candidate.Action.RoomIndex == action.RoomIndex && candidate.Action.RecursionSelections.Count == 1)
            .ToArray();
        Require(choices.Select(candidate => candidate.Action.RecursionSelections[0].CardInstanceIds.Single())
                .SequenceEqual(eligible),
            "Search did not enumerate every eligible hand-card identity in order.");
        foreach (SupportedActionCandidate choice in choices)
        {
            int cardId = choice.Action.RecursionSelections[0].CardInstanceIds.Single();
            CombatContext branch = choice.Result.State!.Spawn.Train.Context!;
            int cost = input.FindCard(cardId)!.RawCostOverride ?? input.FindCard(cardId)!.MaskDescriptor!.BaseCost;
            Require(branch.QueryFrame?.ForgePoints == cost &&
                branch.OtherPiles?.Single(pile => pile.Name == "Exhausted").Cards.Any(card => card.InstanceId == cardId) == true &&
                input.Cards.Hand.Any(card => card.InstanceId == cardId),
                "A smelt branch used the wrong card, Forge amount or source state.");
        }
        Console.WriteLine("CHOOSE-DISCARD-NATIVE PASS: original StarterScrap cost-2 smelt and " +
            choices.Length + " ordered choice branches match native state.");
    }

    public static void NativeOthers(FixtureValue root)
    {
        string scenario = root.GetProperty("ModifierScenario").GetString()!;
        bool refined = scenario == "card-effect-choose-discard-refine";
        bool persistent = scenario == "card-effect-choose-discard-persistent";
        Require(scenario is "card-effect-choose-discard-others" or
            "card-effect-choose-discard-others-policy" or "card-effect-choose-discard-refine" or
            "card-effect-choose-discard-persistent",
            "This fixture did not run the original keep-one smelt scenario.");
        FixtureValue[] actions = root.GetProperty("Actions").EnumerateArray().ToArray();
        Require(actions.Length > 0 && actions[0].GetProperty("ScenarioAction").GetBoolean(),
            "The original FoundryLine was not the first native action.");
        FixtureValue entry = actions[0];
        BattleTurnState before = entry.GetProperty("Before").Deserialize<BattleTurnState>()!;
        BattleTurnState actual = entry.GetProperty("Actual").Deserialize<BattleTurnState>()!;
        PlayCardAction action = entry.GetProperty("Action").Deserialize<PlayCardAction>()!;
        CombatContext input = before.Spawn.Train.Context!;
        CombatContext output = actual.Spawn.Train.Context!;
        Require(input.Cards.Hand.Single(card => card.InstanceId == action.CardInstanceId).DataId == FoundryLineId &&
            action.RecursionSelections.Count == 1 && action.RecursionSelections[0].EffectIndex == 0 &&
            action.RecursionSelections[0].CardInstanceIds.Count == 1,
            "The original FoundryLine choice was not captured by card identity.");
        int keptId = action.RecursionSelections[0].CardInstanceIds[0];
        Require(input.Cards.Hand.Single(card => card.InstanceId == keptId).DataId ==
                (persistent ? ScrapMetalId : TwoCostId) &&
            input.Cards.Hand.Any(card => card.DataId == OneCostId),
            "The native keep-one choice did not distinguish the candidate costs.");
        CardActionEffect effect = before.PlayRules!.Cards.Single(card => card.DataId == FoundryLineId).Effects.Single();
        Require(effect.Type == "ChooseDiscard" && effect.Value == 2 && effect.ChooseDiscard?.Condition == 5 &&
            effect.ChooseDiscard.GainEmberForCost && !effect.ChooseDiscard.HasReplacementPool,
            "The captured card differs from the original FoundryLine definition.");
        if (refined || persistent)
        {
            CardToken scrap = input.Cards.Hand.Single(card => card.DataId == ScrapMetalId);
            CardInstanceState original = input.FindCard(scrap.InstanceId)!;
            Require(original.MaskDescriptor?.BaseCost == 0 &&
                original.MaskDescriptor.BaseTraits.Single(trait => trait.RuntimeType == "CardTraitRefine").ParamInt == 3 &&
                original.MaskDescriptor.BaseTraits.Any(trait => trait.RuntimeType == "CardTraitUnplayable") &&
                original.Permanent.Upgrades.Count == 0 && original.Temporary.Upgrades.Count == 0,
                "The original unupgraded ScrapMetal Refine=3 witness changed.");
            BattleActionEnumeration frontier = BattleActionModel.EnumerateActions(before);
            Require(before.PlayRules.Rooms.All(room => BattleActionModel.PlayCard(before,
                    new PlayCardAction(scrap.InstanceId, room.RoomIndex)).Rejection == ActionRejection.Illegal) &&
                frontier.UnsupportedAttempts.All(attempt =>
                    attempt.Action?.CardInstanceId != scrap.InstanceId) &&
                frontier.SupportedActions.All(candidate =>
                    candidate.CardInstanceId != scrap.InstanceId),
                "Original Unplayable ScrapMetal leaked into the supported or unmodeled action frontier.");
            Require(before.ExternalInteractions.Contains("CardTraitPersistent Discard callback") &&
                before.ExternalInteractions.Contains("CardTraitPersistent discard routing") &&
                (persistent ? actual.ExternalInteractions.Contains("CardTraitPersistent discard routing") :
                    actual.ExternalInteractions.All(interaction =>
                        !interaction.StartsWith("CardTraitPersistent", StringComparison.Ordinal))),
                "Persistent discard metadata did not follow ScrapMetal's hand membership.");
        }
        int SmeltCost(CardToken card) => (input.FindCard(card.InstanceId)!.RawCostOverride ??
            input.FindCard(card.InstanceId)!.MaskDescriptor!.BaseCost) +
            (card.DataId == ScrapMetalId ? 3 : 0);
        CardToken[] consumed = input.Cards.Hand.Where(card => card.InstanceId != action.CardInstanceId &&
            card.InstanceId != keptId).ToArray();
        int forge = consumed.Sum(SmeltCost);
        int playedCost = before.PlayRules.Cards.Single(card => card.DataId == FoundryLineId).Cost;
        Require(consumed.Length >= 2 && forge > 0 &&
            output.QueryFrame?.ForgePoints == input.QueryFrame?.ForgePoints + forge &&
            output.QueryFrame?.Energy == input.QueryFrame?.Energy - playedCost + forge &&
            output.Cards.Hand.Any(card => card.InstanceId == keptId) &&
            consumed.All(card => output.Cards.Hand.All(item => item.InstanceId != card.InstanceId) &&
                output.OtherPiles?.Single(pile => pile.Name == "Exhausted").Cards.Any(item =>
                    item.InstanceId == card.InstanceId) == true),
            "Native FoundryLine did not keep one card and consume all other hand cards for their total Forge cost.");
        BattleActionResult replay = BattleActionModel.PlayCard(before, action);
        Require(replay.Supported && BattleTurnChecks.Comparable(replay.State!) == BattleTurnChecks.Comparable(actual),
            "The recorded FoundryLine choice does not replay to the native action state: " + replay.Reason);
        int[] eligible = CardChooseDiscardModel.EligibleCards(input, effect, action.CardInstanceId, out string? error).ToArray();
        SupportedActionCandidate[] choices = BattleActionModel.EnumerateActions(before).SupportedCandidates
            .Where(candidate => candidate.Action.CardInstanceId == action.CardInstanceId &&
                candidate.Action.RoomIndex == action.RoomIndex && candidate.Action.RecursionSelections.Count == 1)
            .ToArray();
        Require(error == null && eligible.Length >= 2 && eligible.Contains(keptId) &&
            choices.Select(candidate => candidate.Action.RecursionSelections[0].CardInstanceIds.Single())
                .SequenceEqual(eligible),
            "Search did not enumerate every eligible kept-card identity in order.");
        foreach (SupportedActionCandidate choice in choices)
        {
            int kept = choice.Action.RecursionSelections[0].CardInstanceIds.Single();
            CombatContext branch = choice.Result.State!.Spawn.Train.Context!;
            int expected = input.Cards.Hand.Where(card => card.InstanceId != action.CardInstanceId &&
                card.InstanceId != kept).Sum(SmeltCost);
            Require(branch.QueryFrame?.ForgePoints == input.QueryFrame?.ForgePoints + expected &&
                branch.Cards.Hand.Any(card => card.InstanceId == kept),
                "A FoundryLine branch kept the wrong card or granted the wrong Forge amount.");
        }
        if (refined || persistent)
        {
            int scrapId = input.Cards.Hand.Single(card => card.DataId == ScrapMetalId).InstanceId;
            BattleTurnState keptScrap = choices.Single(choice =>
                choice.Action.RecursionSelections[0].CardInstanceIds.Single() == scrapId).Result.State!;
            BattleTurnResult retained = BattleTurnModel.EndTurn(keptScrap);
            Require(keptScrap.ExternalInteractions.Contains("CardTraitPersistent discard routing") &&
                retained.Supported && retained.State!.Spawn.Train.Context!.Cards.Hand.Any(card =>
                    card.InstanceId == scrapId),
                "Keeping ScrapMetal failed to retain it through EndTurn: " + retained.UnsupportedReason);
        }
        if (persistent)
        {
            FixtureValue[] turns = root.GetProperty("Turns").EnumerateArray().ToArray();
            Require(turns.Length == 7 && turns.Take(6).All(turn =>
                turn.GetProperty("Actual").Deserialize<BattleTurnState>()!.Spawn.Train.Context!.Cards.Hand
                    .Any(card => card.InstanceId == keptId)),
                "Original Persistent ScrapMetal did not remain in hand across the native decision turns.");
        }
        if (root.GetProperty("ModifierScenario").GetString() == "card-effect-choose-discard-others-policy")
        {
            FixtureValue stealthStage = root.GetProperty("Stages").EnumerateArray()
                .Single(stage => stage.GetProperty("Index").GetInt32() == 24);
            RoomCombatState start = stealthStage.GetProperty("Before").Deserialize<RoomCombatState>()!;
            RoomCombatState end = stealthStage.GetProperty("Actual").Deserialize<RoomCombatState>()!;
            Require(stealthStage.GetProperty("Kind").GetString() == "Room" &&
                start.Units.Count == 1 && start.Units[0].AssetKey == "TrainStewardSmall" &&
                start.Units[0].Status("stealth")?.Stacks == 2 &&
                end.Units.Single().Status("stealth")?.Stacks == 1 &&
                start.Context?.QueryFrame?.SelectedRoomIndex == 1 &&
                end.Context?.QueryFrame?.SelectedRoomIndex == 0,
                "Native empty-floor post-combat Stealth consumption or room focus changed.");
            RoomCombatResult stealth = RoomCombatModel.Resolve(start);
            Require(stealth.Supported &&
                System.Text.Json.JsonSerializer.Serialize(stealth.State!.Units) ==
                System.Text.Json.JsonSerializer.Serialize(end.Units) &&
                stealth.State.Context?.QueryFrame?.SelectedRoomIndex == 0,
                "The independent post-combat model did not consume one Stealth stack and focus its floor.");
        }
        Console.WriteLine("CHOOSE-DISCARD-OTHERS-NATIVE PASS: original FoundryLine kept one card, consumed " +
            consumed.Length + " others" + (refined ? " including Refine-3 ScrapMetal" :
                persistent ? " while keeping Persistent ScrapMetal" : "") +
            " and enumerated " + choices.Length + " ordered search branches.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

using System;
using System.Linq;
using MonsterTrain2Poju.Fixtures;
using MonsterTrain2Poju.Model;

internal static class ChooseDiscardChecks
{
    private const string ScrapId = "bc4cd099-93df-4771-ae72-4984fc50ab9f";
    private const string FoundryLineId = "26651f53-95d0-4802-b710-51d23bf89bbd";
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
        Require(root.GetProperty("ModifierScenario").GetString() == "card-effect-choose-discard-others",
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
        Require(input.Cards.Hand.Single(card => card.InstanceId == keptId).DataId == TwoCostId &&
            input.Cards.Hand.Any(card => card.DataId == OneCostId),
            "The native keep-one choice did not distinguish the candidate costs.");
        CardActionEffect effect = before.PlayRules!.Cards.Single(card => card.DataId == FoundryLineId).Effects.Single();
        Require(effect.Type == "ChooseDiscard" && effect.Value == 2 && effect.ChooseDiscard?.Condition == 5 &&
            effect.ChooseDiscard.GainEmberForCost && !effect.ChooseDiscard.HasReplacementPool,
            "The captured card differs from the original FoundryLine definition.");
        CardToken[] consumed = input.Cards.Hand.Where(card => card.InstanceId != action.CardInstanceId &&
            card.InstanceId != keptId).ToArray();
        int forge = consumed.Sum(card => input.FindCard(card.InstanceId)!.RawCostOverride ??
            input.FindCard(card.InstanceId)!.MaskDescriptor!.BaseCost);
        Require(consumed.Length >= 2 && forge > 0 &&
            output.QueryFrame?.ForgePoints == input.QueryFrame?.ForgePoints + forge &&
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
                card.InstanceId != kept).Sum(card => input.FindCard(card.InstanceId)!.RawCostOverride ??
                    input.FindCard(card.InstanceId)!.MaskDescriptor!.BaseCost);
            Require(branch.QueryFrame?.ForgePoints == input.QueryFrame?.ForgePoints + expected &&
                branch.Cards.Hand.Any(card => card.InstanceId == kept),
                "A FoundryLine branch kept the wrong card or granted the wrong Forge amount.");
        }
        Console.WriteLine("CHOOSE-DISCARD-OTHERS-NATIVE PASS: original FoundryLine kept one card, consumed " +
            consumed.Length + " others and enumerated " + choices.Length + " ordered search branches.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
